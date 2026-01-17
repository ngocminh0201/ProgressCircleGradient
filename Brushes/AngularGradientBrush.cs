using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.Graphics.DirectX;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;
using Windows.UI;

namespace ProgressCircleGradient.Brushes
{
    public sealed partial class AngularGradientBrush : XamlCompositionBrushBase
    {
        // ========== CONFIG ==========
        private const int FixedResolution = 1024;

        // Góc offset theo Figma như bạn đang dùng (degree)
        private const float InitialAngleOffset = -46.2f;

        // Thời gian để quay 1 vòng (giây). Bạn chỉnh số này nếu muốn nhanh/chậm hơn.
        private const float SpinDurationSeconds = 1.7f;

        // FPS redraw (30fps là đủ mượt với tốc độ quay này)
        private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(33);

        // LUT: 0.1 degree / step
        private const int LutStepsPerDegree = 10;
        private const int LutSize = 360 * LutStepsPerDegree;

        private Compositor? _compositor;
        private CompositionGraphicsDevice? _graphicsDevice;
        private CompositionDrawingSurface? _surface;
        private CompositionSurfaceBrush? _surfaceBrush;

        // Animation (CPU redraw) state
        private DispatcherQueueTimer? _timer;
        private DateTimeOffset _lastTickUtc;
        private float _spinDeg;

        // Cached data for fast redraw
        private float[]? _baseAnglesDeg; // per pixel, [0..360)
        private byte[]? _pixelBuffer;    // BGRA premultiplied
        private CanvasBitmap? _bitmap;   // reused bitmap (no re-create per frame)

        private readonly struct Stop(float angleDeg, byte a, byte r, byte g, byte b)
        {
            public readonly float AngleDeg = angleDeg;
            public readonly byte A = a, R = r, G = g, B = b;
        }

        private static readonly Stop[] Stops =
        [
            new Stop(25.2f, 0x99, 0x38, 0x7A, 0xFF),
            new Stop(72.0f, 0xE6, 0x3C, 0xB9, 0xA2),
            new Stop(136.8f, 0xE6, 0x3D, 0xCC, 0x87),
            new Stop(208.8f, 0xE6, 0x38, 0x7A, 0xFF),
            new Stop(306.0f, 0x99, 0x3B, 0xA3, 0xC3),
            new Stop(345.6f, 0x99, 0x3D, 0xCC, 0x87),
        ];

        // LUT màu premultiplied BGRA packed into uint: 0xAARRGGBB? (ta lưu BGRA bytes riêng nên pack theo BGRA)
        private static readonly uint[] _lutPremulBGRA = BuildLutPremulBGRA();

        protected override void OnConnected()
        {
            if (CompositionBrush != null)
                return;

            _compositor = CompositionTarget.GetCompositorForCurrentThread();

            var canvasDevice = CanvasDevice.GetSharedDevice();
            _graphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(_compositor, canvasDevice);

            CreateOrResizeSurface();
            EnsureCachesAndBitmap(canvasDevice);

            // draw initial frame
            _spinDeg = 0f;
            Redraw();

            _surfaceBrush = _compositor.CreateSurfaceBrush(_surface);
            _surfaceBrush.Stretch = CompositionStretch.Fill;
            CompositionBrush = _surfaceBrush;

            StartSpin();
        }

        protected override void OnDisconnected()
        {
            StopSpin();

            CompositionBrush = null;

            _surfaceBrush?.Dispose();
            _surfaceBrush = null;

            _bitmap?.Dispose();
            _bitmap = null;

            _pixelBuffer = null;
            _baseAnglesDeg = null;

            _surface = null;

            _graphicsDevice?.Dispose();
            _graphicsDevice = null;

            _compositor = null;
        }

        private void StartSpin()
        {
            if (_timer != null)
                return;

            var dq = DispatcherQueue.GetForCurrentThread();
            _timer = dq.CreateTimer();
            _timer.Interval = TickInterval;
            _timer.IsRepeating = true;
            _timer.Tick += OnTick;

            _lastTickUtc = DateTimeOffset.UtcNow;
            _timer.Start();
        }

        private void StopSpin()
        {
            if (_timer == null)
                return;

            _timer.Tick -= OnTick;
            _timer.Stop();
            _timer = null;
        }

        private void OnTick(DispatcherQueueTimer sender, object args)
        {
            var now = DateTimeOffset.UtcNow;
            var dt = (float)(now - _lastTickUtc).TotalSeconds;
            _lastTickUtc = now;

            // clockwise: +deg
            float degPerSec = 360f / SpinDurationSeconds;
            _spinDeg = (_spinDeg - dt * degPerSec) % 360f;
            if (_spinDeg < 0f) _spinDeg += 360f;


            Redraw();
        }

        private void CreateOrResizeSurface()
        {
            _surface = _graphicsDevice!.CreateDrawingSurface(
                new Size(FixedResolution, FixedResolution),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);
        }

        private void EnsureCachesAndBitmap(CanvasDevice device)
        {
            int count = FixedResolution * FixedResolution;

            if (_baseAnglesDeg == null || _baseAnglesDeg.Length != count)
            {
                _baseAnglesDeg = BuildBaseAnglesDeg(FixedResolution, FixedResolution);
            }

            if (_pixelBuffer == null || _pixelBuffer.Length != count * 4)
            {
                _pixelBuffer = new byte[count * 4];
            }

            // create bitmap once (we will update it by SetPixelBytes each frame)
            if (_bitmap == null)
            {
                // init buffer with first frame so bitmap has valid data
                FillPixelBufferFromBaseAngles(_spinDeg);

                _bitmap = CanvasBitmap.CreateFromBytes(
                    device,
                    _pixelBuffer,
                    FixedResolution,
                    FixedResolution,
                    (Windows.Graphics.DirectX.DirectXPixelFormat)DirectXPixelFormat.B8G8R8A8UIntNormalized);
            }
        }

        private void Redraw()
        {
            if (_surface == null)
                return;

            var device = CanvasDevice.GetSharedDevice();
            EnsureCachesAndBitmap(device);

            FillPixelBufferFromBaseAngles(_spinDeg);

            // update bitmap without recreating
            _bitmap!.SetPixelBytes(_pixelBuffer);

            using var ds = CanvasComposition.CreateDrawingSession(_surface);
            ds.Clear(new Color { A = 0, R = 0, G = 0, B = 0 });
            ds.DrawImage(_bitmap);
        }

        private void FillPixelBufferFromBaseAngles(float spinDeg)
        {
            if (_pixelBuffer == null || _baseAnglesDeg == null)
                return;

            int idx = 0;
            int lutMask = LutSize; // we will mod by LutSize

            for (int i = 0; i < _baseAnglesDeg.Length; i++)
            {
                float a = _baseAnglesDeg[i] + spinDeg;
                if (a >= 360f) a -= 360f;

                int lutIndex = (int)(a * LutStepsPerDegree);
                // lutIndex in [0..3599], but keep safe
                lutIndex %= lutMask;

                uint c = _lutPremulBGRA[lutIndex];

                // unpack premultiplied BGRA
                _pixelBuffer[idx++] = (byte)(c & 0xFF);         // B
                _pixelBuffer[idx++] = (byte)((c >> 8) & 0xFF);  // G
                _pixelBuffer[idx++] = (byte)((c >> 16) & 0xFF); // R
                _pixelBuffer[idx++] = (byte)((c >> 24) & 0xFF); // A
            }
        }

        private static float[] BuildBaseAnglesDeg(int width, int height)
        {
            var baseAngles = new float[width * height];

            float cx = width * 0.5f;
            float cy = height * 0.5f;

            int k = 0;
            for (int y = 0; y < height; y++)
            {
                float py = (y + 0.5f) - cy;

                for (int x = 0; x < width; x++)
                {
                    float px = (x + 0.5f) - cx;

                    // same math you had: 0° at 6h, clockwise
                    float rad = MathF.Atan2(-px, py);
                    if (rad < 0)
                        rad += MathF.PI * 2f;

                    float angleDeg = rad * (180f / MathF.PI);
                    angleDeg = (angleDeg + InitialAngleOffset) % 360f;
                    if (angleDeg < 0)
                        angleDeg += 360f;

                    baseAngles[k++] = angleDeg;
                }
            }

            return baseAngles;
        }

        private static uint[] BuildLutPremulBGRA()
        {
            var lut = new uint[LutSize];

            for (int i = 0; i < LutSize; i++)
            {
                float angle = i / (float)LutStepsPerDegree;

                EvaluateColorAtAnglePremultiplied(angle, out byte a, out byte rP, out byte gP, out byte bP);

                // pack BGRA (premultiplied)
                uint packed =
                    (uint)(bP) |
                    ((uint)gP << 8) |
                    ((uint)rP << 16) |
                    ((uint)a << 24);

                lut[i] = packed;
            }

            return lut;
        }

        /// <summary>
        /// Sample the Angular gradient color at a specific point in the coordinate space (STATIC snapshot logic).
        /// Lưu ý: brush ngoài UI đang xoay theo thời gian; hàm này không nhận "spinDeg", nên chỉ phục vụ debug/tham khảo.
        /// </summary>
        public static Color SampleColorAtPoint(Point point, double centerX, double centerY)
        {
            float px = (float)(point.X - centerX);
            float py = (float)(point.Y - centerY);

            float rad = MathF.Atan2(-px, py);
            if (rad < 0)
                rad += MathF.PI * 2f;

            float angleDeg = rad * (180f / MathF.PI);
            angleDeg = (angleDeg + InitialAngleOffset) % 360f;
            if (angleDeg < 0)
                angleDeg += 360f;

            EvaluateColorAtAnglePremultiplied(angleDeg, out byte a, out byte rP, out byte gP, out byte bP);

            if (a == 0)
                return Colors.Transparent;

            float af = a / 255f;
            byte r = (byte)Math.Clamp((int)MathF.Round(rP / af), 0, 255);
            byte g = (byte)Math.Clamp((int)MathF.Round(gP / af), 0, 255);
            byte b = (byte)Math.Clamp((int)MathF.Round(bP / af), 0, 255);

            return Color.FromArgb(a, r, g, b);
        }

        private static void EvaluateColorAtAnglePremultiplied(float angleDeg, out byte aOut, out byte rPOut, out byte gPOut, out byte bPOut)
        {
            angleDeg %= 360f;
            if (angleDeg < 0)
                angleDeg += 360f;

            Stop prev, next;
            float prevAngle, nextAngle;

            if (angleDeg < Stops[0].AngleDeg)
            {
                prev = Stops[^1];
                next = Stops[0];
                prevAngle = prev.AngleDeg - 360f;
                nextAngle = next.AngleDeg;
            }
            else if (angleDeg >= Stops[^1].AngleDeg)
            {
                prev = Stops[^1];
                next = Stops[0];
                prevAngle = prev.AngleDeg;
                nextAngle = next.AngleDeg + 360f;
            }
            else
            {
                int i = 0;
                for (; i < Stops.Length - 1; i++)
                {
                    if (Stops[i].AngleDeg <= angleDeg && angleDeg < Stops[i + 1].AngleDeg)
                        break;
                }
                prev = Stops[i];
                next = Stops[i + 1];
                prevAngle = prev.AngleDeg;
                nextAngle = next.AngleDeg;
            }

            float t = (angleDeg - prevAngle) / (nextAngle - prevAngle);
            t = Math.Clamp(t, 0f, 1f);

            float ap0 = prev.A / 255f;
            float ap1 = next.A / 255f;

            float r0 = (prev.R / 255f) * ap0;
            float g0 = (prev.G / 255f) * ap0;
            float b0 = (prev.B / 255f) * ap0;

            float r1 = (next.R / 255f) * ap1;
            float g1 = (next.G / 255f) * ap1;
            float b1 = (next.B / 255f) * ap1;

            float a = Lerp(ap0, ap1, t);
            float rP = Lerp(r0, r1, t);
            float gP = Lerp(g0, g1, t);
            float bP = Lerp(b0, b1, t);

            aOut = (byte)Math.Clamp((int)MathF.Round(a * 255f), 0, 255);
            rPOut = (byte)Math.Clamp((int)MathF.Round(rP * 255f), 0, 255);
            gPOut = (byte)Math.Clamp((int)MathF.Round(gP * 255f), 0, 255);
            bPOut = (byte)Math.Clamp((int)MathF.Round(bP * 255f), 0, 255);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
