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

        // Defaults (used when user didn't set Foreground/PointForeground)
        private const string ELLIPSE_INDETERMINATE_KEY = "#387AFF";
        private const string VARIANT_ELLIPSE_INDETERMINATE_KEY = "#3DCC87";

        // VI reference (PDF uses LG: component 60x60, dot 14x14, distances 20dp & 11dp from center)
        private const double VI_LG_GRID_SIZE = 60.0;
        private const double VI_LG_DOT_SIZE = 14.0;
        private const double VI_LG_OUTER_DISTANCE = 20.0;
        // Animation spec: Phase1 moves 11dp toward center from the 20dp outer position (=> 9dp from center at 650ms)
        private const double VI_LG_INNER_DISTANCE = 9.0;

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
        private bool _isUsingFrozenAngularColors;

        private readonly List<ProgressCircleIndeterminateModel> _progressCircleIndeterminateModels = new()
        {
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.XLarge, Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 3.75, GridSize = 90 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Large,  Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 2.5,  GridSize = 60 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Medium, Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 2.0,  GridSize = 48 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Small,  Orientation = ProgressCircleIndeterminateOrientation.Horizontal, Scale = 1.0,  GridSize = 24 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.SmallTitle, Orientation = ProgressCircleIndeterminateOrientation.Horizontal, Scale = 0.67, GridSize = 16 },
        };
        #endregion

        #region Dependency Properties
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

        // Re-purposed for VI distances:
        //  - EllipseMaxOffset  : outer distance (20dp @ LG)
        //  - EllipseMinOffset  : phase1 distance from center (9dp @ LG)
        //  - EllipseNegativeDisplacement : -outer
        //  - EllipseNegativeMinOffset    : -inner
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

        internal double EllipseNegativeDisplacement
        {
            get => (double)GetValue(EllipseNegativeDisplacementProperty);
            set => SetValue(EllipseNegativeDisplacementProperty, value);
        }

        internal static readonly DependencyProperty EllipseNegativeDisplacementProperty =
            DependencyProperty.Register(nameof(EllipseNegativeDisplacement), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));

        internal double EllipseNegativeMinOffset
        {
            get => (double)GetValue(EllipseNegativeMinOffsetProperty);
            set => SetValue(EllipseNegativeMinOffsetProperty, value);
        }

        internal static readonly DependencyProperty EllipseNegativeMinOffsetProperty =
            DependencyProperty.Register(nameof(EllipseNegativeMinOffset), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));

        // Keep these for backward-compat (not used by new template, but keeping to avoid breaking external bindings)
        internal double EllipseDisplacementPosition
        {
            get => (double)GetValue(EllipseDisplacementPositionProperty);
            set => SetValue(EllipseDisplacementPositionProperty, value);
        }

        internal static readonly DependencyProperty EllipseDisplacementPositionProperty =
            DependencyProperty.Register(nameof(EllipseDisplacementPosition), typeof(double), typeof(ProgressCircleIndeterminate), new PropertyMetadata(default(double)));
        #endregion

        #region Constructors
        public ProgressCircleIndeterminate() : base()
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

            UpdateDotBrushesAndMaybeRestartAnimation(restartAnimation: true);
        }

        protected override void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateCircleScale();
            UpdateProgressCircleLayout();

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
            if (_isUsingFrozenAngularColors)
                return;

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

            // Scale factor uses LG as baseline (60x60).
            double scaleFactor = progressDefinition.GridSize / VI_LG_GRID_SIZE;

            EllipseDiameter = VI_LG_DOT_SIZE * scaleFactor;

            EllipseMaxOffset = VI_LG_OUTER_DISTANCE * scaleFactor;   // 20dp @ LG
            EllipseMinOffset = VI_LG_INNER_DISTANCE * scaleFactor;   // 9dp @ LG (after moving 11dp toward center)

            EllipseNegativeDisplacement = -EllipseMaxOffset;         // -outer
            EllipseNegativeMinOffset = -EllipseMinOffset;            // -phase1 target

            // Keep old props consistent (not used by new template)
            EllipseDisplacementPosition = EllipseMaxOffset;
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

            _text.FontSize = progressDefinition.GridSize switch
            {
                GRIDSIZE_XL => 13,
                GRIDSIZE_LG => 13,
                GRIDSIZE_MD => 12,
                GRIDSIZE_SM => 11,
                GRIDSIZE_ST => 11,
                _ => 13
            };
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
            // Reset transforms for root (Scale + Rotate)
            if (_rootGrid?.RenderTransform is TransformGroup tg)
            {
                foreach (var t in tg.Children)
                {
                    if (t is RotateTransform rt) rt.Angle = 0;
                    if (t is ScaleTransform st) { st.ScaleX = 1; st.ScaleY = 1; }
                }
            }
            else if (_rootGrid?.RenderTransform is RotateTransform rtOnly)
            {
                rtOnly.Angle = 0;
            }

            // Reset dot transforms to "outer" positions (20dp @ LG, scaled).
            if (_ellipse01?.RenderTransform is TranslateTransform t1)
            {
                t1.X = 0;
                t1.Y = EllipseNegativeDisplacement; // top = -outer
            }
            if (_ellipsePoint?.RenderTransform is TranslateTransform t2)
            {
                t2.X = EllipseMaxOffset; // right = +outer
                t2.Y = 0;
            }
            if (_ellipse02?.RenderTransform is TranslateTransform t3)
            {
                t3.X = 0;
                t3.Y = EllipseMaxOffset; // bottom = +outer
            }
            if (_ellipse03?.RenderTransform is TranslateTransform t4)
            {
                t4.X = EllipseNegativeDisplacement; // left = -outer
                t4.Y = 0;
            }

            if (_ellipse01 != null) _ellipse01.Opacity = 1;
            if (_ellipsePoint != null) _ellipsePoint.Opacity = 1;
            if (_ellipse02 != null) _ellipse02.Opacity = 1;
            if (_ellipse03 != null) _ellipse03.Opacity = 1;
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

            if (TryApplyFrozenAngularColors())
            {
                _isUsingFrozenAngularColors = true;
            }
            else
            {
                _isUsingFrozenAngularColors = false;
                ApplyNormalDotBrushes();
            }

            if (restartAnimation)
            {
                BeginAnimationIfVisible();
            }
        }

        private AngularGradientBrush? GetAngularBrushSourceOrNull()
        {
            if (PointForeground is AngularGradientBrush c1)
                return c1;
            if (Foreground is AngularGradientBrush c2)
                return c2;
            return null;
        }

        private bool TryApplyFrozenAngularColors()
        {
            if (GetAngularBrushSourceOrNull() == null)
                return false;

            double w = (_rootGrid != null && _rootGrid.ActualWidth > 0) ? _rootGrid.ActualWidth : (_rootGrid?.Width ?? 0);
            double h = (_rootGrid != null && _rootGrid.ActualHeight > 0) ? _rootGrid.ActualHeight : (_rootGrid?.Height ?? 0);

            if (w <= 0 || h <= 0)
            {
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
            double outer = EllipseMaxOffset;

            // Sample 4 points at "outer" positions
            var top = new Windows.Foundation.Point(cx, cy - outer);
            var right = new Windows.Foundation.Point(cx + outer, cy);
            var bottom = new Windows.Foundation.Point(cx, cy + outer);
            var left = new Windows.Foundation.Point(cx - outer, cy);

            Color cTop = AngularGradientBrush.SampleColorAtPoint(top, cx, cy);
            Color cRight = AngularGradientBrush.SampleColorAtPoint(right, cx, cy);
            Color cBottom = AngularGradientBrush.SampleColorAtPoint(bottom, cx, cy);
            Color cLeft = AngularGradientBrush.SampleColorAtPoint(left, cx, cy);

            _ellipse01.Fill = new SolidColorBrush(cRight);
            _ellipsePoint.Fill = new SolidColorBrush(cTop);
            _ellipse02.Fill = new SolidColorBrush(cLeft);
            _ellipse03.Fill = new SolidColorBrush(cBottom);

            return true;
        }

        private void ApplyNormalDotBrushes()
        {
            Brush normalBrush = ResolveBrushOrDefault(_elipseIndeterminateBrushDefault, ELLIPSE_INDETERMINATE_KEY);
            Brush pointBrush = ResolveBrushOrDefault(_variantElipseIndeterminateBrushDefault, VARIANT_ELLIPSE_INDETERMINATE_KEY);

            if (_ellipse01 != null) _ellipse01.Fill = normalBrush;
            if (_ellipse02 != null) _ellipse02.Fill = normalBrush;
            if (_ellipse03 != null) _ellipse03.Fill = normalBrush;

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
