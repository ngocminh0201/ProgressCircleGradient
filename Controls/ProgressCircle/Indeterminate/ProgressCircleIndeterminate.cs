using Microsoft.UI;
using Microsoft.UI.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using ProgressCircleGradient.Brushes;
using ProgressCircleGradient.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

namespace ProgressCircleGradient.Controls.ProgressCircle
{
    public class ProgressCircleIndeterminate : ProgressCircle
    {
        #region Constants
        private const string PART_ROOT_GRID_NAME = "PART_RootGrid";
        private const string PART_TEXT_NAME = "PART_text";
        private const string PART_STORYBOARD_NAME = "RotateAnimation";

        private const string PART_ELLIPSEPOINT = "PART_EllipsePoint";
        private const string PART_ELLIPSE01 = "PART_Ellipse01";
        private const string PART_ELLIPSE02 = "PART_Ellipse02";
        private const string PART_ELLIPSE03 = "PART_Ellipse03";

        private const string ELLIPSE_INDETERMINATE_KEY = "#387AFF";
        private const string VARIANT_ELLIPSE_INDETERMINATE_KEY = "3DCC87";

        private const double ELLIPSE_BASE_SIZE = 4.5;
        private const double ELLIPE_BASE_MIN_OFFSET = 1.5;
        private const double ELLIPE_BASE_MAX_OFFSET = 6.5;
        private const double ELLIPE_BASE_DISPLACEMENT = 5;
        private const double ELLIPE_BASE_DISPLACEMENT_REVERSE = -4;

        // ----- ConicGradientBrush sampling (must match ConicGradientBrush.cs) -----
        // In ConicGradientBrush.BuildConicGradientPixelsPremultipliedBGRA:
        // angleDeg = (angleDeg + initialAngleOffset(-46.2) + AngleOffsetDeg) % 360
        private const float CONIC_INITIAL_ANGLE_OFFSET_DEG = -46.2f;

        private readonly struct ConicStop
        {
            public readonly float AngleDeg;
            public readonly byte A, R, G, B;
            public ConicStop(float angleDeg, byte a, byte r, byte g, byte b)
            {
                AngleDeg = angleDeg;
                A = a; R = r; G = g; B = b;
            }
        }

        private static readonly ConicStop[] ConicStops = new[]
        {
            new ConicStop( 25.2f, 0x99, 0x38, 0x7A, 0xFF), // #387AFF @ 60% (7%)
            new ConicStop( 72.0f, 0xE6, 0x3C, 0xB9, 0xA2), // #3CB9A2 @ 90% (20%)
            new ConicStop(136.8f, 0xE6, 0x3D, 0xCC, 0x87), // #3DCC87 @ 90% (38%)
            new ConicStop(208.8f, 0xE6, 0x38, 0x7A, 0xFF), // #387AFF @ 90% (58%)
            new ConicStop(306.0f, 0x99, 0x3B, 0xA3, 0xC3), // #3BA3C3 @ 60% (85%)
            new ConicStop(345.6f, 0x99, 0x3D, 0xCC, 0x87), // #3DCC87 @ 60% (96%)
        };

        private const int GRIDSIZE_XL = 90;
        private const int GRIDSIZE_LG = 60;
        private const int GRIDSIZE_MD = 48;
        private const int GRIDSIZE_SM = 24;
        private const int GRIDSIZE_ST = 16;
        #endregion

        #region Variables
        private Grid _rootGrid;
        private TextBlock _text;
        private Storyboard _rotateAnimation;
        private Ellipse _ellipsePoint, _ellipse01, _ellipse02, _ellipse03;

        private Brush _elipseIndeterminateBrushDefault = ColorsHelpers.ConvertColorHex(ELLIPSE_INDETERMINATE_KEY);
        private Brush _variantElipseIndeterminateBrushDefault = ColorsHelpers.ConvertColorHex(VARIANT_ELLIPSE_INDETERMINATE_KEY);

        private ThemeSettings _themeSettings;
        private long _visibilityPropertyRegisterToken;

        private int _dotBrushUpdateGeneration;
        private bool _isUsingFrozenConicColors;

        private readonly List<ProgressCircleIndeterminateModel> _progressCircleIndeterminateModels = new()
        {
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.XLarge, Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 3.75, GridSize = 90 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Large,  Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 2.5,  GridSize = 60 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Medium, Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 2.0,  GridSize = 48 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Small,  Orientation = ProgressCircleIndeterminateOrientation.Horizontal, Scale = 1.0,  GridSize = 24 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.SmallTitle, Orientation = ProgressCircleIndeterminateOrientation.Horizontal, Scale = 0.67, GridSize = 16 },
        };
        #endregion

        #region Depedency Properties
        public new Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly new DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(ProgressCircleIndeterminate),
                new PropertyMetadata(new SolidColorBrush(Colors.Transparent), OnForegroundPropertyChanged));

        public Brush PointForeground
        {
            get => (Brush)GetValue(PointForegroundProperty);
            set => SetValue(PointForegroundProperty, value);
        }

        public static readonly DependencyProperty PointForegroundProperty =
            DependencyProperty.Register(nameof(PointForeground), typeof(Brush), typeof(ProgressCircleIndeterminate),
                new PropertyMetadata(new SolidColorBrush(Colors.Transparent), OnPointForegroundPropertyChanged));

        internal double EllipseDiameter
        {
            get => (double)GetValue(EllipseDiameterProperty);
            set => SetValue(EllipseDiameterProperty, value);
        }

        internal static readonly DependencyProperty EllipseDiameterProperty =
            DependencyProperty.Register(nameof(EllipseDiameter), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));

        internal double EllipseDisplacementPosition
        {
            get => (double)GetValue(EllipseDisplacementPositionProperty);
            set => SetValue(EllipseDisplacementPositionProperty, value);
        }

        internal static readonly DependencyProperty EllipseDisplacementPositionProperty =
            DependencyProperty.Register(nameof(EllipseDisplacementPosition), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));

        internal double EllipseNegativeDisplacement
        {
            get => (double)GetValue(EllipseNegativeDisplacementProperty);
            set => SetValue(EllipseNegativeDisplacementProperty, value);
        }

        internal static readonly DependencyProperty EllipseNegativeDisplacementProperty =
            DependencyProperty.Register(nameof(EllipseNegativeDisplacement), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));

        internal double EllipseMinOffset
        {
            get => (double)GetValue(EllipseMinOffsetProperty);
            set => SetValue(EllipseMinOffsetProperty, value);
        }

        internal static readonly DependencyProperty EllipseMinOffsetProperty =
            DependencyProperty.Register(nameof(EllipseMinOffset), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));

        internal double EllipseMaxOffset
        {
            get => (double)GetValue(EllipseMaxOffsetProperty);
            set => SetValue(EllipseMaxOffsetProperty, value);
        }

        internal static readonly DependencyProperty EllipseMaxOffsetProperty =
            DependencyProperty.Register(nameof(EllipseMaxOffset), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));
        #endregion

        #region Constructors
        public ProgressCircleIndeterminate()
        {
            DefaultStyleKey = typeof(ProgressCircleIndeterminate);
            Loaded += ProgressCircleIndeterminate_Loaded;
            Unloaded += ProgressCircleIndeterminate_Unloaded;
        }
        #endregion

        #region Override Methods
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _rootGrid = (Grid)GetTemplateChild(PART_ROOT_GRID_NAME);
            _text = (TextBlock)GetTemplateChild(PART_TEXT_NAME);

            _ellipsePoint = (Ellipse)GetTemplateChild(PART_ELLIPSEPOINT);
            _ellipse01 = (Ellipse)GetTemplateChild(PART_ELLIPSE01);
            _ellipse02 = (Ellipse)GetTemplateChild(PART_ELLIPSE02);
            _ellipse03 = (Ellipse)GetTemplateChild(PART_ELLIPSE03);

            _rotateAnimation = (Storyboard)GetTemplateChild(PART_STORYBOARD_NAME);

            UpdateProgressCircleLayout();
            UpdateCircleScale();

            // Apply dot colors based on the current Foreground/PointForeground.
            // If either property is a ConicGradientBrush, all 4 dots will "sample" colors at t=0
            // and then keep those colors while rotating.
            UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
        }

        protected override void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateCircleScale();
            UpdateProgressCircleLayout();

            // Size changes => sampling points change.
            UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
        }
        #endregion

        #region Event Handlers
        private void ProgressCircleIndeterminate_Loaded(object sender, RoutedEventArgs e)
        {
            if (_themeSettings == null && XamlRoot?.ContentIslandEnvironment != null)
            {
                var myWindowId = XamlRoot.ContentIslandEnvironment.AppWindowId;
                _themeSettings = ThemeSettings.CreateForWindowId(myWindowId);
                if (_themeSettings != null)
                {
                    _themeSettings.Changed += ThemeSettings_Changed;
                }
            }

            if (_visibilityPropertyRegisterToken == 0)
            {
                _visibilityPropertyRegisterToken = RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityPropertyChanged);
            }

            UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
        }

        private void ProgressCircleIndeterminate_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_visibilityPropertyRegisterToken != 0)
            {
                UnregisterPropertyChangedCallback(VisibilityProperty, _visibilityPropertyRegisterToken);
                _visibilityPropertyRegisterToken = 0;
            }

            if (_themeSettings != null)
            {
                _themeSettings.Changed -= ThemeSettings_Changed;
            }

            _rotateAnimation?.Stop();
        }

        private void ThemeSettings_Changed(ThemeSettings sender, object args)
        {
            // If we're freezing colors from ConicGradientBrush, theme doesn't matter here.
            if (_isUsingFrozenConicColors)
                return;

            // Re-apply defaults if Foreground/PointForeground is Transparent.
            ApplyNormalDotBrushes();
        }

        private static void OnForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircleIndeterminate self)
            {
                self._elipseIndeterminateBrushDefault = (Brush)e.NewValue;
                self.UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
            }
        }

        private static void OnPointForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircleIndeterminate self)
            {
                self._variantElipseIndeterminateBrushDefault = (Brush)e.NewValue;
                self.UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
            }
        }

        private static void OnVisibilityPropertyChanged(DependencyObject d, DependencyProperty dp)
        {
            if (d is ProgressCircleIndeterminate self)
            {
                if (self.Visibility == Visibility.Collapsed)
                {
                    self._rotateAnimation?.Stop();
                }
                else
                {
                    // Ensure correct colors are applied before resuming.
                    self.UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
                }
            }
        }
        #endregion

        #region Private Methods
        private void UpdateCircleScale()
        {
            var progressDefinition = _progressCircleIndeterminateModels.FirstOrDefault(x => x.Size == Size);
            if (progressDefinition == null)
                return;

            SetTextAlignment(progressDefinition.Orientation);

            EllipseDiameter = ELLIPSE_BASE_SIZE * progressDefinition.Scale;
            EllipseMinOffset = ELLIPE_BASE_MIN_OFFSET * progressDefinition.Scale;
            EllipseMaxOffset = ELLIPE_BASE_MAX_OFFSET * progressDefinition.Scale;
            EllipseDisplacementPosition = ELLIPE_BASE_DISPLACEMENT * progressDefinition.Scale;
            EllipseNegativeDisplacement = ELLIPE_BASE_DISPLACEMENT_REVERSE * progressDefinition.Scale;
        }

        private void SetTextAlignment(ProgressCircleIndeterminateOrientation orientation)
        {
            if (_progressCircleTextAlingmentDictionary.TryGetValue(orientation, out string alignment))
            {
                VisualStateManager.GoToState(this, alignment, true);
            }
        }

        private void UpdateProgressCircleLayout()
        {
            var progressDefinition = _progressCircleIndeterminateModels.FirstOrDefault(x => x.Size == Size);
            if (progressDefinition == null)
                return;

            UpdateRootGridSize(progressDefinition);
            UpdateFontSizeMessage(progressDefinition);
        }

        private void UpdateRootGridSize(ProgressCircleIndeterminateModel progressDefinition)
        {
            if (_rootGrid == null)
                return;

            _rootGrid.Width = progressDefinition.GridSize;
            _rootGrid.Height = progressDefinition.GridSize;
        }

        private void UpdateFontSizeMessage(ProgressCircleIndeterminateModel progressDefinition)
        {
            if (_text == null)
                return;

            var fontSizeToken = progressDefinition.GridSize switch
            {
                GRIDSIZE_XL => 13,
                GRIDSIZE_LG => 13,
                GRIDSIZE_MD => 12,
                GRIDSIZE_SM => 11,
                GRIDSIZE_ST => 11,
                _ => 13
            };

            _text.FontSize = fontSizeToken;
        }

        private void BeginAnimationIfVisible()
        {
            if (Visibility != Visibility.Collapsed)
            {
                _rotateAnimation?.Begin();
            }
        }

        private void ResetAnimationToInitialFrame()
        {
            // Ensure we sample colors at storyboard time = 0.
            if (_rootGrid?.RenderTransform is RotateTransform rt)
            {
                rt.Angle = 0;
            }

            if (_ellipse01?.RenderTransform is TranslateTransform t1)
            {
                t1.X = 0;
                t1.Y = 0;
            }
            if (_ellipsePoint?.RenderTransform is TranslateTransform t2)
            {
                t2.X = 0;
                t2.Y = 0;
            }
            if (_ellipse02?.RenderTransform is TranslateTransform t3)
            {
                t3.X = 0;
                t3.Y = 0;
            }
            if (_ellipse03?.RenderTransform is TranslateTransform t4)
            {
                t4.X = 0;
                t4.Y = 0;
            }
        }

        private void UpdateDotBrushesAndMaybeRestartAnimation(bool restartAnimation)
        {
            _dotBrushUpdateGeneration++;

            if (_ellipse01 == null || _ellipse02 == null || _ellipse03 == null || _ellipsePoint == null)
                return;

            if (restartAnimation)
            {
                _rotateAnimation?.Stop();
                ResetAnimationToInitialFrame();
            }

            if (TryApplyFrozenConicColors())
            {
                _isUsingFrozenConicColors = true;
            }
            else
            {
                _isUsingFrozenConicColors = false;
                ApplyNormalDotBrushes();
            }

            if (restartAnimation)
            {
                BeginAnimationIfVisible();
            }
        }

        private ConicGradientBrush GetConicBrushSourceOrNull()
        {
            // Allow the user to set the conic brush via either Foreground or PointForeground.
            // (So your sample XAML that sets PointForeground will still work.)
            if (PointForeground is ConicGradientBrush c1)
                return c1;
            if (Foreground is ConicGradientBrush c2)
                return c2;
            return null;
        }

        private bool TryApplyFrozenConicColors()
        {
            var conic = GetConicBrushSourceOrNull();
            if (conic == null)
                return false;

            // Determine the coordinate space used by the ConicGradientBrush.
            // The brush is stretched to fill the target bounds, so sampling by angle is stable.
            double w = (_rootGrid != null && _rootGrid.ActualWidth > 0) ? _rootGrid.ActualWidth : (_rootGrid?.Width ?? 0);
            double h = (_rootGrid != null && _rootGrid.ActualHeight > 0) ? _rootGrid.ActualHeight : (_rootGrid?.Height ?? 0);

            if (w <= 0 || h <= 0)
            {
                // Fallback to the model's intended size.
                var def = _progressCircleIndeterminateModels.FirstOrDefault(x => x.Size == Size);
                if (def != null)
                {
                    w = def.GridSize;
                    h = def.GridSize;
                }
                else
                {
                    w = h = 24;
                }
            }

            double cx = w * 0.5;
            double cy = h * 0.5;

            double d = EllipseDiameter;
            double m = EllipseMinOffset;

            // Centers at storyboard t=0 (TranslateTransform = 0)
            // Top, Right(point), Bottom, Left
            var top = new Windows.Foundation.Point(cx, m + d * 0.5);
            var right = new Windows.Foundation.Point(w - m - d * 0.5, cy);
            var bottom = new Windows.Foundation.Point(cx, h - m - d * 0.5);
            var left = new Windows.Foundation.Point(m + d * 0.5, cy);

            Color cTop = SampleConicColorAtPoint(conic, top, cx, cy);
            Color cRight = SampleConicColorAtPoint(conic, right, cx, cy);
            Color cBottom = SampleConicColorAtPoint(conic, bottom, cx, cy);
            Color cLeft = SampleConicColorAtPoint(conic, left, cx, cy);

            // Freeze: each dot becomes a SolidColorBrush so it keeps the same color while moving.
            _ellipse01.Fill = new SolidColorBrush(cTop);
            _ellipsePoint.Fill = new SolidColorBrush(cRight);
            _ellipse02.Fill = new SolidColorBrush(cBottom);
            _ellipse03.Fill = new SolidColorBrush(cLeft);

            return true;
        }

        private void ApplyNormalDotBrushes()
        {
            Brush normalBrush = ResolveBrushOrDefault(_elipseIndeterminateBrushDefault, ELLIPSE_INDETERMINATE_KEY);
            Brush pointBrush = ResolveBrushOrDefault(_variantElipseIndeterminateBrushDefault, VARIANT_ELLIPSE_INDETERMINATE_KEY);

            // 3 dots
            if (_ellipse01 != null) _ellipse01.Fill = normalBrush;
            if (_ellipse02 != null) _ellipse02.Fill = normalBrush;
            if (_ellipse03 != null) _ellipse03.Fill = normalBrush;

            // point dot
            if (_ellipsePoint != null) _ellipsePoint.Fill = pointBrush;
        }

        private static Brush ResolveBrushOrDefault(Brush brush, string defaultHex)
        {
            if (brush is SolidColorBrush scb && scb.Color == Colors.Transparent)
            {
                return ColorsHelpers.ConvertColorHex(defaultHex);
            }

            return brush ?? ColorsHelpers.ConvertColorHex(defaultHex);
        }

        private static Color SampleConicColorAtPoint(ConicGradientBrush brush, Windows.Foundation.Point p, double cx, double cy)
        {
            float px = (float)(p.X - cx);
            float py = (float)(p.Y - cy);

            // Must match ConicGradientBrush:
            // 0° at 6 o'clock (down), increasing clockwise => atan2(-x, y)
            float rad = MathF.Atan2(-px, py);
            if (rad < 0)
                rad += MathF.PI * 2f;

            float angleDeg = rad * (180f / MathF.PI);

            angleDeg = (angleDeg + CONIC_INITIAL_ANGLE_OFFSET_DEG + brush.AngleOffsetDeg) % 360f;
            if (angleDeg < 0)
                angleDeg += 360f;

            EvaluateConicColorPremultiplied(angleDeg, out byte a, out byte rP, out byte gP, out byte bP);

            if (a == 0)
                return Colors.Transparent;

            // Un-premultiply so SolidColorBrush renders the same visual color.
            float af = a / 255f;
            byte r = (byte)Math.Clamp((int)MathF.Round(rP / af), 0, 255);
            byte g = (byte)Math.Clamp((int)MathF.Round(gP / af), 0, 255);
            byte b = (byte)Math.Clamp((int)MathF.Round(bP / af), 0, 255);

            return Color.FromArgb(a, r, g, b);
        }

        private static void EvaluateConicColorPremultiplied(float angleDeg, out byte aOut, out byte rPOut, out byte gPOut, out byte bPOut)
        {
            angleDeg %= 360f;
            if (angleDeg < 0)
                angleDeg += 360f;

            ConicStop prev, next;
            float prevAngle, nextAngle;

            if (angleDeg < ConicStops[0].AngleDeg)
            {
                prev = ConicStops[^1];
                next = ConicStops[0];
                prevAngle = prev.AngleDeg - 360f;
                nextAngle = next.AngleDeg;
            }
            else if (angleDeg >= ConicStops[^1].AngleDeg)
            {
                prev = ConicStops[^1];
                next = ConicStops[0];
                prevAngle = prev.AngleDeg;
                nextAngle = next.AngleDeg + 360f;
            }
            else
            {
                int i = 0;
                for (; i < ConicStops.Length - 1; i++)
                {
                    if (ConicStops[i].AngleDeg <= angleDeg && angleDeg < ConicStops[i + 1].AngleDeg)
                        break;
                }

                prev = ConicStops[i];
                next = ConicStops[i + 1];
                prevAngle = prev.AngleDeg;
                nextAngle = next.AngleDeg;
            }

            float t = (angleDeg - prevAngle) / (nextAngle - prevAngle);
            t = Math.Clamp(t, 0f, 1f);

            float ap0 = prev.A / 255f;
            float ap1 = next.A / 255f;

            // Premultiplied interpolation (must match the brush rendering)
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
        #endregion
    }
}
