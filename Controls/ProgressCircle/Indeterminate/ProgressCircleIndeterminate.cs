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
            UnregisterPropertyChangedCallback(VisibilityProperty, _visibilityPropertyRegisterToken);

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

            // Use the ConicGradientBrush.SampleColorAtPoint to sample colors at these points
            Color cTop = ConicGradientBrush.SampleColorAtPoint(top, cx, cy);
            Color cRight = ConicGradientBrush.SampleColorAtPoint(right, cx, cy);
            Color cBottom = ConicGradientBrush.SampleColorAtPoint(bottom, cx, cy);
            Color cLeft = ConicGradientBrush.SampleColorAtPoint(left, cx, cy);

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
        #endregion
    }
}
