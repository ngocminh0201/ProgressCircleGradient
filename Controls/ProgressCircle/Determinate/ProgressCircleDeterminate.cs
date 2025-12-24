using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using ProgressCircleGradient.Controls.ProgressCircle;
using ProgressCircleGradient.Controls.ProgressCircle.Determinate;
using ProgressCircleGradient.Helpers;
using Windows.Foundation;

namespace ProgressCircleGradient.Controls.ProgressCircle
{
    public partial class ProgressCircleDeterminate : ProgressCircle
    {
        #region Constants       
        private const double RADIANS = Math.PI / 180;

        private const string PART_COLOR_GRID_NAME = "PART_ColorGrid";
        private const string PART_TEXT_NAME = "PART_text";
        private const string PART_CANVAS = "PART_canvas";

        private const string PART_OUTER_PATH = "PART_OuterPath";
        private const string PART_INNER_PATH = "PART_InnerPath";
        private const string PART_OUTER_PATH_FIGURE = "PART_OuterPathFigure";
        private const string PART_INNER_PATH_FIGURE = "PART_InnerPathFigure";
        private const string PART_OUTER_ARC_SEGMENT = "PART_OuterArcSegment";
        private const string PART_INNER_ARC_SEGMENT = "PART_InnerArcSegment";
        private const string PART_START_ELLIPSE = "PART_startEllipse";
        private const string PART_END_ELLIPSE = "PART_endEllipse";
        private const string RESOURCE_COLOR_ARC_OUTER = "#17171A"; // 10%
        private const string RESOURCE_COLOR_ARC_INNER = "#387AFF";
        private const double CIRCLE_CENTER_TO_BORDER_CORRECTION_FACTOR = 0.98;
        private const string PART_TEXT_FONTSIZE_MS = "OneUISizeMS";
        private const string PART_TEXT_FONTSIZE_SM = "OneUISizeSM";
        private const string PART_TEXT_FONTSIZE_XS = "OneUISizeXS";
        private const int GRIDSIZE_XL = 45;
        private const int GRIDSIZE_LG = 30;
        private const int GRIDSIZE_MD = 24;
        private const int GRIDSIZE_SM = 12;
        private const int GRIDSIZE_ST = 8;
        #endregion

        #region Variables    
        private Canvas _canvas;
        private Path _outerPath;
        private Path _innerPath;
        private PathFigure _outerPathFigure;
        private PathFigure _innerPathFigure;
        private ArcSegment _outerArc;
        private ArcSegment _innerArc;
        private EllipseGeometry _startEllipse;
        private EllipseGeometry _endEllipse;
        private TextBlock _text;

        private readonly List<ProgressCircleDeterminateModel> _progressCircleDeterminateModels = new List<ProgressCircleDeterminateModel> {

            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.XLarge, Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 30, Thickness = 10, Margin = new Thickness(7) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.Large,Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 21, Thickness = 8 , Margin = new Thickness(5)},
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.Medium,Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 17, Thickness = 6, Margin = new Thickness(4) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.Small,Orientation = ProgressCircleIndeterminateOrientation.Horizontal, RadiusSize = 8.5, Thickness = 3, Margin = new Thickness(2) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.SmallTitle,Orientation = ProgressCircleIndeterminateOrientation.Horizontal, RadiusSize = 7, Thickness = 2, Margin = new Thickness(1.6)},
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate2, Size = ProgressCircleSize.SmallTitle,Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 31, Thickness = 6, Margin = new Thickness(7) },

        };
        #endregion

        #region Dependency Properties
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(ProgressCircleDeterminate), new PropertyMetadata(0.0, OnPercentValuePropertyChanged));

        public new Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly new DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(ProgressCircleDeterminate), new PropertyMetadata(ColorsHelpers.ConvertHexToColor(RESOURCE_COLOR_ARC_INNER), OnForegroundPropertyChanged));

        public new Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly new DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(ProgressCircleDeterminate), new PropertyMetadata(ColorsHelpers.ConvertHexToColor(RESOURCE_COLOR_ARC_OUTER, 10), OnBackgroundPropertyChanged));

        public ProgressCircleDeterminateType Type
        {
            get => (ProgressCircleDeterminateType)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(nameof(Type), typeof(ProgressCircleDeterminateType), typeof(ProgressCircleDeterminate), new PropertyMetadata(ProgressCircleDeterminateType.Determinate1, OnTypePropertyChanged));

        internal double Radius
        {
            get => (double)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register(nameof(Radius), typeof(double), typeof(ProgressCircleDeterminate), new PropertyMetadata(50.0, OnRadiusOrThicknessPropertyChanged));

        internal double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(ProgressCircleDeterminate), new PropertyMetadata(2.0, OnRadiusOrThicknessPropertyChanged));

        #endregion

        #region Constructors
        public ProgressCircleDeterminate() : base()
        {
            DefaultStyleKey = typeof(ProgressCircleDeterminate);
        }
        #endregion

        #region Override Methods
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            colorGrid = (Grid)GetTemplateChild(PART_COLOR_GRID_NAME);
            _text = (TextBlock)GetTemplateChild(PART_TEXT_NAME);
            _canvas = (Canvas)GetTemplateChild(PART_CANVAS);

            _outerPath = GetTemplateChild(PART_OUTER_PATH) as Path;
            _innerPath = GetTemplateChild(PART_INNER_PATH) as Path;
            _outerPathFigure = GetTemplateChild(PART_OUTER_PATH_FIGURE) as PathFigure;
            _innerPathFigure = GetTemplateChild(PART_INNER_PATH_FIGURE) as PathFigure;
            _outerArc = GetTemplateChild(PART_OUTER_ARC_SEGMENT) as ArcSegment;
            _innerArc = GetTemplateChild(PART_INNER_ARC_SEGMENT) as ArcSegment;
            _startEllipse = GetTemplateChild(PART_START_ELLIPSE) as EllipseGeometry;
            _endEllipse = GetTemplateChild(PART_END_ELLIPSE) as EllipseGeometry;

            SetControlSize();
            Draw();

            _innerPath.Stroke = Foreground;
            _outerPath.Stroke = Background;
        }

        protected override void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateSize();
        }
        #endregion

        #region Event Handlers
        private static void OnRadiusOrThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ConfigureProgress(d);
        }

        private static void OnPercentValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ConfigureProgress(d);
        }

        private static void ConfigureProgress(DependencyObject d)
        {
            var control = d as ProgressCircleDeterminate;
            control.SetControlSize();
            control.Draw();
        }

        private static void OnForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircleDeterminate progressCircle && progressCircle._innerPath != null)
            {
                progressCircle._innerPath.Stroke = (Brush)e.NewValue;
            }
        }

        private static void OnBackgroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircleDeterminate progressCircle && progressCircle._outerPath != null)
            {
                progressCircle._outerPath.Stroke = (Brush)e.NewValue;
            }
        }
        #endregion

        #region Private Methods
        private void Draw()
        {
            if (_canvas == null)
                return;

            if (Value == 0)
            {
                _innerPath.Visibility = Visibility.Collapsed;
            }
            else
            {
                _innerPath.Visibility = Visibility.Visible;
            }
            GetCircleSegment(GetCenterPoint(), Radius, GetAngle());
        }

        private void SetControlSize()
        {
            if (_canvas == null)
                return;

            _canvas.Width = Radius * 2 + Thickness;
            _canvas.Height = Radius * 2 + Thickness;
        }

        private Point GetCenterPoint()
        {
            return new Point(Radius + Thickness / 2, Radius + Thickness / 2);
        }

        private double GetAngle()
        {
            double angle = Value * CIRCLE_CENTER_TO_BORDER_CORRECTION_FACTOR / 100 * 360;
            if (angle >= 360)
            {
                angle = 359.999;
            }
            return angle;
        }

        private void GetCircleSegment(Point centerPoint, double radius, double angle)
        {
            var circleStart = new Point(centerPoint.X, centerPoint.Y - radius);

            _innerPath.Width = Radius * 2 + Thickness;
            _innerPath.Height = Radius * 2 + Thickness;
            _innerPath.StrokeThickness = Thickness;

            _outerPath.Width = Radius * 2 + Thickness;
            _outerPath.Height = Radius * 2 + Thickness;
            _outerPath.StrokeThickness = Thickness;

            _innerPathFigure.StartPoint = circleStart;
            _innerPathFigure.IsClosed = false;

            _outerPathFigure.StartPoint = circleStart;
            _outerPathFigure.IsClosed = false;

            _innerArc.IsLargeArc = angle > 180.0;
            _innerArc.Point = ScaleUnitCirclePoint(centerPoint, angle, radius);
            _innerArc.Size = new Size(radius, radius);
            _innerArc.SweepDirection = SweepDirection.Clockwise;

            _outerArc.IsLargeArc = true;
            _outerArc.Point = ScaleUnitCirclePoint(centerPoint, 359.999, radius);
            _outerArc.Size = new Size(radius, radius);
            _outerArc.SweepDirection = SweepDirection.Clockwise;

            double xEnd = _innerArc.Point.X + Math.Cos(RADIANS * angle);
            double yEnd = _innerArc.Point.Y + Math.Sin(RADIANS * angle);

            _startEllipse.Center = circleStart;
            _startEllipse.RadiusX = 0.25;
            _startEllipse.RadiusY = 0.25;
            _endEllipse.Center = new Point(xEnd, yEnd);
            _endEllipse.RadiusX = 0.25;
            _endEllipse.RadiusY = 0.25;
        }

        private static Point ScaleUnitCirclePoint(Point origin, double angle, double radius)
        {
            return new Point(origin.X + Math.Sin(RADIANS * angle) * radius, origin.Y - Math.Cos(RADIANS * angle) * radius);
        }

        private static void OnTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircleDeterminate progressCircleDeterminate)
            {
                progressCircleDeterminate.UpdateSize();
            }
        }

        private void UpdateSize()
        {
            ProgressCircleDeterminateModel progress;

            if (Type == ProgressCircleDeterminateType.Determinate2)
                progress = _progressCircleDeterminateModels.FirstOrDefault(x => x.Type == Type);
            else
                progress = _progressCircleDeterminateModels.FirstOrDefault(x => x.Type == Type && x.Size == Size);

            if (progress == null)
            {
                return;
            }

            Radius = progress.RadiusSize;
            Thickness = progress.Thickness;

            SetCanvasMargin(progress.Margin);
            SetTextAlignment(progress.Orientation);
            UpdateFontSizeMessage(progress);
        }
        private void UpdateFontSizeMessage(ProgressCircleDeterminateModel progressDefinition)
        {
            if (progressDefinition.Type == ProgressCircleDeterminateType.Determinate2)
            {
                _text.FontSize = 13;
                return;
            }
            if (_text == null)
                return;

            var fontSizeToken = progressDefinition.RadiusSize switch
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

        private void SetCanvasMargin(Thickness margin)
        {
            if (_canvas != null)
                _canvas.Margin = margin;
        }

        private void SetTextAlignment(ProgressCircleIndeterminateOrientation orientation)
        {
            if (_progressCircleTextAlingmentDictionary.TryGetValue(orientation, out string alignment))
            {
                VisualStateManager.GoToState(this, alignment, true);
            }
        }

        #endregion
    }
}