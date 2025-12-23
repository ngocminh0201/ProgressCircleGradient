using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.Graphics.DirectX;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;

namespace ProgressCircleGradient.Brushes
{
    /// <summary>
    /// Conic/Sweep/Angular gradient brush (Figma "Angular") rendered into a CompositionDrawingSurface via Win2D.
    /// Angles are defined with 0° at 6 o'clock and increasing clockwise (like your Figma description).
    /// </summary>
    public sealed class ConicGradientBrush : XamlCompositionBrushBase
    {
        public static readonly DependencyProperty ResolutionProperty =
            DependencyProperty.Register(
                nameof(Resolution),
                typeof(int),
                typeof(ConicGradientBrush),
                new PropertyMetadata(256, OnAnyParamChanged));

        public static readonly DependencyProperty AngleOffsetDegProperty =
            DependencyProperty.Register(
                nameof(AngleOffsetDeg),
                typeof(float),
                typeof(ConicGradientBrush),
                new PropertyMetadata(0f, OnAnyParamChanged));

        /// <summary>Offscreen bitmap resolution (square).</summary>
        public int Resolution
        {
            get => (int)GetValue(ResolutionProperty);
            set => SetValue(ResolutionProperty, value);
        }

        /// <summary>
        /// Extra rotation (degrees). Use only if you need to match a rotated Figma handle precisely.
        /// </summary>
        public float AngleOffsetDeg
        {
            get => (float)GetValue(AngleOffsetDegProperty);
            set => SetValue(AngleOffsetDegProperty, value);
        }

        private static void OnAnyParamChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ConicGradientBrush)d).RebuildIfConnected();
        }

        private Compositor? _compositor;
        private CompositionGraphicsDevice? _graphicsDevice;
        private CompositionDrawingSurface? _surface;
        private CompositionSurfaceBrush? _surfaceBrush;

        private readonly struct Stop
        {
            public readonly float AngleDeg;
            public readonly byte A, R, G, B;
            public Stop(float angleDeg, byte a, byte r, byte g, byte b)
            {
                AngleDeg = angleDeg;
                A = a; R = r; G = g; B = b;
            }
        }

        const float delta = 0f;
        // Your exact Figma stops
        private static readonly Stop[] Stops = new[]
        {
            new Stop(  7f + delta, 0x99, 0x38, 0x7A, 0xFF), // #387AFF @ 60%
            new Stop( 38f + delta, 0xE6, 0x3C, 0xB9, 0xA2), // #3CB9A2 @ 90%
            //new Stop( 65f + delta, 0xE6, 0x3D, 0xCC, 0x87), // #3DCC87 @ 90%
            //new Stop( 123f + delta, 0xE6, 0x38, 0x7A, 0xFF), // #387AFF @ 90%
            //new Stop( 208f + delta, 0x99, 0x3B, 0xA3, 0xC3), // #3BA3C3 @ 60%
            //new Stop( 304f + delta, 0x99, 0x3D, 0xCC, 0x87), // #3DCC87 @ 60%
        };

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

        private void RebuildIfConnected()
        {
            if (CompositionBrush == null || _graphicsDevice == null)
                return;

            CreateOrResizeSurface();
            Redraw();

            if (_surfaceBrush != null && _surface != null)
                _surfaceBrush.Surface = _surface;
        }

        private void CreateOrResizeSurface()
        {
            int res = Math.Clamp(Resolution, 32, 2048);

            _surface = _graphicsDevice!.CreateDrawingSurface(
                new Size(res, res),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);
        }

        private void Redraw()
        {
            if (_surface == null)
                return;

            int res = Math.Clamp(Resolution, 32, 2048);
            var bytes = BuildConicGradientPixelsPremultipliedBGRA(res, res, AngleOffsetDeg);

            var device = CanvasDevice.GetSharedDevice();
            using var bitmap = CanvasBitmap.CreateFromBytes(
                device,
                bytes,
                res,
                res,
                (Windows.Graphics.DirectX.DirectXPixelFormat)DirectXPixelFormat.B8G8R8A8UIntNormalized);

            using var ds = CanvasComposition.CreateDrawingSession(_surface);
            ds.Clear(new Windows.UI.Color { A = 0, R = 0, G = 0, B = 0 });
            ds.DrawImage(bitmap);
        }

        /// <summary>
        /// Output: premultiplied BGRA bytes for B8G8R8A8.
        /// </summary>
        private static byte[] BuildConicGradientPixelsPremultipliedBGRA(int width, int height, float angleOffsetDeg)
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

                    // ✅ FIX QUAN TRỌNG:
                    // 0° ở 6h (down) và TĂNG THEO CHIỀU KIM ĐỒNG HỒ => từ 6h quay về phía 7h/8h/9h (bên trái).
                    // Với screen coords (x phải +, y xuống +), muốn clockwise-from-down => dùng atan2(-x, y).
                    float rad = MathF.Atan2(-px, py);
                    if (rad < 0) rad += MathF.PI * 2f;

                    float angleDeg = rad * (180f / MathF.PI);
                    angleDeg = (angleDeg + angleOffsetDeg) % 360f;

                    EvaluateColorAtAnglePremultiplied(angleDeg, out byte a, out byte rP, out byte gP, out byte bP);

                    // BGRA
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
            if (angleDeg < 0) angleDeg += 360f;

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
