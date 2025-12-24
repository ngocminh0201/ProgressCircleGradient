using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using ProgressCircleGradient.Helpers;
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
        private const string PART_ELLIPSE03 = "PART_Ellipse02";
        private const string PART_ELLIPSE04 = "PART_Ellipse03";

        private const string ELLIPSE_INDETERMINATE_KEY = "#387AFF";
        private const string VARIANT_ELLIPSE_INDETERMINATE_KEY = "3DCC87";

        private const double ELLIPSE_BASE_SIZE = 4.5;
        private const double ELLIPE_BASE_MIN_OFFSET = 1.5;
        private const double ELLIPE_BASE_MAX_OFFSET = 6.5;
        private const double ELLIPE_BASE_DISPLACEMENT = 5;
        private const double ELLIPE_BASE_DISPLACEMENT_REVERSE = -4;

        private const string TEXT_VERTICAL_ALINGMENT_STATE = "TextVerticalAlignment";
        private const string TEXT_HORIZONTAL_ALINGMENT_STATE = "TextHorizontalAlignment";

        private const string PART_TEXT_FONTSIZE_MS = "OneUISizeMS";
        private const string PART_TEXT_FONTSIZE_SM = "OneUISizeSM";
        private const string PART_TEXT_FONTSIZE_XS = "OneUISizeXS";
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
        private long _visibilityPropertyRegisterToken = 0;
        private readonly List<ProgressCircleIndeterminateModel> _progressCircleIndeterminateModels = new List<ProgressCircleIndeterminateModel> {

            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.XLarge, Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 3.75, GridSize = 90 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Large,Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 2.5, GridSize = 60 },
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Medium,Orientation = ProgressCircleIndeterminateOrientation.Vertical, Scale = 2.0, GridSize = 48},
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.Small,Orientation = ProgressCircleIndeterminateOrientation.Horizontal, Scale = 1, GridSize = 24},
            new ProgressCircleIndeterminateModel(){ Size = ProgressCircleSize.SmallTitle,Orientation = ProgressCircleIndeterminateOrientation.Horizontal, Scale = 0.67, GridSize = 16},

        };
        #endregion

        #region Depedency Properties
        public new Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly new DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(ProgressCircleIndeterminate), new PropertyMetadata(new SolidColorBrush(Colors.Transparent), OnForegroundPropertyChanged));

        public Brush PointForeground
        {
            get => (Brush)GetValue(PointForegroundProperty);
            set => SetValue(PointForegroundProperty, value);
        }

        public static readonly DependencyProperty PointForegroundProperty =
            DependencyProperty.Register(nameof(PointForeground), typeof(Brush), typeof(ProgressCircleIndeterminate), new PropertyMetadata(new SolidColorBrush(Colors.Transparent), OnPointForegroundPropertyChanged));

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
            _ellipse02 = (Ellipse)GetTemplateChild(PART_ELLIPSE03);
            _ellipse03 = (Ellipse)GetTemplateChild(PART_ELLIPSE04);

            TrySetInitialForegroundColors();

            RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityPropertyChanged);

            UpdateProgressCircleLayout();
            UpdateCircleScale();

            _rotateAnimation = (Storyboard)GetTemplateChild(PART_STORYBOARD_NAME);
            BeginAnimation();
        }

        protected override void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateCircleScale();
            UpdateProgressCircleLayout();
        }

        #endregion

        #region Event Handlers
        private void ProgressCircleIndeterminate_Loaded(object sender, RoutedEventArgs e)
        {
            if (_themeSettings == null &&
                XamlRoot != null &&
                XamlRoot.ContentIslandEnvironment != null)
            {
                var myWindowId = XamlRoot.ContentIslandEnvironment.AppWindowId;
                _themeSettings = ThemeSettings.CreateForWindowId(myWindowId);
                if (_themeSettings != null)
                {
                    _themeSettings.Changed += ThemeSettings_Changed;
                }
            }

            BeginAnimation();

            _visibilityPropertyRegisterToken = RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityPropertyChanged);
        }


        private void ProgressCircleIndeterminate_Unloaded(object sender, RoutedEventArgs e)
        {
            UnregisterPropertyChangedCallback(VisibilityProperty, _visibilityPropertyRegisterToken);

            _rotateAnimation?.Stop();
        }

        private void ThemeSettings_Changed(ThemeSettings sender, object args)
        {
            UpdateEllipseBrush(Foreground, ELLIPSE_INDETERMINATE_KEY, UpdateEllipsePointColor);
            UpdateEllipseBrush(PointForeground, VARIANT_ELLIPSE_INDETERMINATE_KEY, UpdateVariantEllipsePointColor);
        }

        private static void OnForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircleIndeterminate self)
            {
                self._elipseIndeterminateBrushDefault = (Brush)e.NewValue;
                self.UpdateEllipsePointColor(self._elipseIndeterminateBrushDefault);
            }
        }

        private static void OnPointForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircleIndeterminate self)
            {
                self._variantElipseIndeterminateBrushDefault = (Brush)e.NewValue;
                self.UpdateVariantEllipsePointColor(self._variantElipseIndeterminateBrushDefault);
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
                    self._rotateAnimation?.Begin();
                }
            }
        }
        #endregion

        #region Private Methods
        private void DoVerticalTextAlignment()
        {
            VisualStateManager.GoToState(this, TEXT_VERTICAL_ALINGMENT_STATE, true);
        }

        private void DoHorizontalTextAlignment()
        {
            VisualStateManager.GoToState(this, TEXT_HORIZONTAL_ALINGMENT_STATE, true);
        }

        private void UpdateCircleScale()
        {
            var progressDefinition = _progressCircleIndeterminateModels.FirstOrDefault(x => x.Size == Size);

            if (progressDefinition == null)
            {
                return;
            }

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
            {
                return;
            }

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

        private void BeginAnimation()
        {
            _rotateAnimation?.Begin();
        }

        private void TrySetInitialForegroundColors()
        {
            if (!IsSameSolidColorBrush(_variantElipseIndeterminateBrushDefault, new SolidColorBrush(Colors.Transparent)))
            {
                UpdateVariantEllipsePointColor(_variantElipseIndeterminateBrushDefault);
            }
            if (!IsSameSolidColorBrush(_elipseIndeterminateBrushDefault, new SolidColorBrush(Colors.Transparent)))
            {
                UpdateEllipsePointColor(_elipseIndeterminateBrushDefault);
            }
        }

        private void UpdateEllipsePointColor(Brush colorBrush)
        {
            if (_ellipse01 != null && _ellipse02 != null && _ellipse03 != null)
                _ellipse01.Fill = _ellipse02.Fill = _ellipse03.Fill = colorBrush;
        }

        private void UpdateVariantEllipsePointColor(Brush colorBrush)
        {
            if (_ellipsePoint != null)
                _ellipsePoint.Fill = colorBrush;
        }

        private bool IsSameSolidColorBrush(Brush brush1, Brush brush2)
        {
            if (brush1 is SolidColorBrush solidColorBrush1 && brush2 is SolidColorBrush solidColorBrush2)
            {
                return solidColorBrush1.Color == solidColorBrush2.Color;
            }
            return false;
        }

        private void UpdateEllipseBrush(Brush brush, string resourceKey, Action<Brush> updateAction)
        {
            Color color = ((SolidColorBrush)brush).Color;
            if (Colors.Transparent.Equals(color))
            {
                Brush defaultBrush = ColorsHelpers.ConvertColorHex(resourceKey);
                updateAction(defaultBrush);
            }
        }
        #endregion
    }
}