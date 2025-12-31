using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.Graphics.DirectX;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;
using Windows.UI;

namespace ProgressCircleGradient.Brushes
{
    public sealed partial class AngularGradientBrush : XamlCompositionBrushBase
    {
        private const int FixedResolution = 1024;
        private const float InitialAngleOffset = -46.2f;

        private Compositor? _compositor;
        private CompositionGraphicsDevice? _graphicsDevice;
        private CompositionDrawingSurface? _surface;
        private CompositionSurfaceBrush? _surfaceBrush;

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

        protected override void OnConnected()
        {
            if (CompositionBrush != null)
                return;

            _compositor = CompositionTarget.GetCompositorForCurrentThread();

            var canvasDevice = CanvasDevice.GetSharedDevice();
            _graphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(_compositor, canvasDevice);

            CreateOrResizeSurface();
            Redraw();

            _surfaceBrush = _compositor.CreateSurfaceBrush(_surface);
            _surfaceBrush.Stretch = CompositionStretch.Fill;
            CompositionBrush = _surfaceBrush;
        }

        protected override void OnDisconnected()
        {
            CompositionBrush = null;

            _surfaceBrush?.Dispose();
            _surfaceBrush = null;

            _surface = null;

            _graphicsDevice?.Dispose();
            _graphicsDevice = null;

            _compositor = null;
        }

        private void CreateOrResizeSurface()
        {
            _surface = _graphicsDevice!.CreateDrawingSurface(
                new Size(FixedResolution, FixedResolution),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);
        }

        private void Redraw()
        {
            if (_surface == null)
                return;

            var bytes = BuildAngularGradientPixelsPremultipliedBGRA(FixedResolution, FixedResolution);
            var device = CanvasDevice.GetSharedDevice();

            using var bitmap = CanvasBitmap.CreateFromBytes(
                device,
                bytes,
                FixedResolution,
                FixedResolution,
                (Windows.Graphics.DirectX.DirectXPixelFormat)DirectXPixelFormat.B8G8R8A8UIntNormalized);

            using var ds = CanvasComposition.CreateDrawingSession(_surface);
            ds.Clear(new Color { A = 0, R = 0, G = 0, B = 0 });
            ds.DrawImage(bitmap);
        }

        /// <summary>
        /// Sample the Angular gradient color at a specific point in the coordinate space.
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

        private static byte[] BuildAngularGradientPixelsPremultipliedBGRA(int width, int height)
        {
            var buffer = new byte[width * height * 4];

            float cx = width * 0.5f;
            float cy = height * 0.5f;

            int idx = 0;
            for (int y = 0; y < height; y++)
            {
                float py = (y + 0.5f) - cy;

                for (int x = 0; x < width; x++)
                {
                    float px = (x + 0.5f) - cx;

                    float rad = MathF.Atan2(-px, py);
                    if (rad < 0) 
                        rad += MathF.PI * 2f;

                    float angleDeg = rad * (180f / MathF.PI);
                    angleDeg = (angleDeg + InitialAngleOffset) % 360f;
                    if (angleDeg < 0) 
                        angleDeg += 360f;

                    EvaluateColorAtAnglePremultiplied(angleDeg, out byte a, out byte rP, out byte gP, out byte bP);

                    buffer[idx++] = bP;
                    buffer[idx++] = gP;
                    buffer[idx++] = rP;
                    buffer[idx++] = a;
                }
            }

            return buffer;
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
