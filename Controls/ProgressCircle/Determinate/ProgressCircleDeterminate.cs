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
        private const string PART_INNER_PATH = "PART_InnerPath"; // legacy fallback
        private const string PART_OUTER_PATH_FIGURE = "PART_OuterPathFigure";
        private const string PART_INNER_PATH_FIGURE = "PART_InnerPathFigure"; // legacy fallback
        private const string PART_OUTER_ARC_SEGMENT = "PART_OuterArcSegment";
        private const string PART_INNER_ARC_SEGMENT = "PART_InnerArcSegment"; // legacy fallback
        private const string PART_START_ELLIPSE = "PART_startEllipse"; // legacy fallback
        private const string PART_END_ELLIPSE = "PART_endEllipse";     // legacy fallback

        // NEW: progress path filled by ConicGradientBrush (masking via geometry)
        private const string PART_PROGRESS_PATH = "PART_ProgressPath";

        private const string RESOURCE_COLOR_ARC_OUTER = "#17171A"; // 10%
        private const string RESOURCE_COLOR_ARC_INNER = "#387AFF";
        private const double CIRCLE_CENTER_TO_BORDER_CORRECTION_FACTOR = 0.98;

        private const int GRIDSIZE_XL = 45;
        private const int GRIDSIZE_LG = 30;
        private const int GRIDSIZE_MD = 24;
        private const int GRIDSIZE_SM = 12;
        private const int GRIDSIZE_ST = 8;
        #endregion

        #region Variables
        private Canvas _canvas;

        // Track
        private Path _outerPath;
        private PathFigure _outerPathFigure;
        private ArcSegment _outerArc;

        // Progress (filled)
        private Path _progressPath;

        // Legacy fallback (stroke arc)
        private Path _innerPath;
        private PathFigure _innerPathFigure;
        private ArcSegment _innerArc;
        private EllipseGeometry _startEllipse;
        private EllipseGeometry _endEllipse;

        private TextBlock _text;

        // Reusable geometries for progress segment
        private PathGeometry _progressGeom;
        private PathFigure _progressFig;
        private ArcSegment _segOuter;
        private ArcSegment _segEndCap;
        private ArcSegment _segInner;
        private ArcSegment _segStartCap;

        // Full ring (100%)
        private GeometryGroup _fullRingGeom;
        private EllipseGeometry _fullOuterEllipse;
        private EllipseGeometry _fullInnerEllipse;

        private readonly List<ProgressCircleDeterminateModel> _progressCircleDeterminateModels = new List<ProgressCircleDeterminateModel> {
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.XLarge, Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 30, Thickness = 10, Margin = new Thickness(7) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.Large,  Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 21, Thickness = 8,  Margin = new Thickness(5) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.Medium, Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 17, Thickness = 6,  Margin = new Thickness(4) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.Small,  Orientation = ProgressCircleIndeterminateOrientation.Horizontal, RadiusSize = 8.5, Thickness = 3, Margin = new Thickness(2) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate1, Size = ProgressCircleSize.SmallTitle, Orientation = ProgressCircleIndeterminateOrientation.Horizontal, RadiusSize = 7, Thickness = 2, Margin = new Thickness(1.6) },
            new ProgressCircleDeterminateModel(){ Type = ProgressCircleDeterminateType.Determinate2, Size = ProgressCircleSize.SmallTitle, Orientation = ProgressCircleIndeterminateOrientation.Vertical, RadiusSize = 31, Thickness = 6, Margin = new Thickness(7) },
        };
        #endregion

        #region Dependency Properties
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(0.0, OnPercentValuePropertyChanged));

        public new Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public static readonly new DependencyProperty ForegroundProperty =
            DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(ColorsHelpers.ConvertHexToColor(RESOURCE_COLOR_ARC_INNER), OnForegroundPropertyChanged));

        public new Brush Background
        {
            get => (Brush)GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public static readonly new DependencyProperty BackgroundProperty =
            DependencyProperty.Register(nameof(Background), typeof(Brush), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(ColorsHelpers.ConvertHexToColor(RESOURCE_COLOR_ARC_OUTER, 10), OnBackgroundPropertyChanged));

        public ProgressCircleDeterminateType Type
        {
            get => (ProgressCircleDeterminateType)GetValue(TypeProperty);
            set => SetValue(TypeProperty, value);
        }

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(nameof(Type), typeof(ProgressCircleDeterminateType), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(ProgressCircleDeterminateType.Determinate1, OnTypePropertyChanged));

        internal double Radius
        {
            get => (double)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }

        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register(nameof(Radius), typeof(double), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(50.0, OnRadiusOrThicknessPropertyChanged));

        internal double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(ProgressCircleDeterminate),
                new PropertyMetadata(2.0, OnRadiusOrThicknessPropertyChanged));
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

            // Track
            _outerPath = GetTemplateChild(PART_OUTER_PATH) as Path;
            _outerPathFigure = GetTemplateChild(PART_OUTER_PATH_FIGURE) as PathFigure;
            _outerArc = GetTemplateChild(PART_OUTER_ARC_SEGMENT) as ArcSegment;

            // Progress filled path (new)
            _progressPath = GetTemplateChild(PART_PROGRESS_PATH) as Path;

            // Legacy fallback parts (template cũ)
            _innerPath = GetTemplateChild(PART_INNER_PATH) as Path;
            _innerPathFigure = GetTemplateChild(PART_INNER_PATH_FIGURE) as PathFigure;
            _innerArc = GetTemplateChild(PART_INNER_ARC_SEGMENT) as ArcSegment;
            _startEllipse = GetTemplateChild(PART_START_ELLIPSE) as EllipseGeometry;
            _endEllipse = GetTemplateChild(PART_END_ELLIPSE) as EllipseGeometry;

            EnsureProgressGeometries();

            SetControlSize();
            Draw();

            // Safety: nếu template không TemplateBinding
            if (_outerPath != null) _outerPath.Stroke = Background;
            if (_progressPath != null) _progressPath.Fill = Foreground;
            if (_innerPath != null) _innerPath.Stroke = Foreground;
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
            if (d is not ProgressCircleDeterminate control) return;
            control.SetControlSize();
            control.Draw();
        }

        private static void OnForegroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ProgressCircleDeterminate pc) return;

            if (pc._progressPath != null)
                pc._progressPath.Fill = (Brush)e.NewValue;

            // legacy fallback
            if (pc._innerPath != null)
                pc._innerPath.Stroke = (Brush)e.NewValue;
        }

        private static void OnBackgroundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircleDeterminate pc && pc._outerPath != null)
                pc._outerPath.Stroke = (Brush)e.NewValue;
        }
        #endregion

        #region Private Methods
        private void Draw()
        {
            if (_canvas == null)
                return;

            var center = GetCenterPoint();
            var angle = GetAngle();

            // Track always
            UpdateTrackGeometry(center, Radius);

            // New masking path
            if (_progressPath != null)
            {
                UpdateProgressPathGeometry(center, angle);
                // Hide legacy if exists
                if (_innerPath != null) _innerPath.Visibility = Visibility.Collapsed;
                return;
            }

            // Legacy fallback (old template)
            if (_innerPath == null || _innerPathFigure == null || _innerArc == null)
                return;

            _innerPath.Visibility = (Value <= 0) ? Visibility.Collapsed : Visibility.Visible;
            UpdateLegacyInnerArc(center, Radius, angle);
        }

        private void SetControlSize()
        {
            if (_canvas == null)
                return;

            _canvas.Width = Radius * 2 + Thickness;
            _canvas.Height = Radius * 2 + Thickness;

            if (_progressPath != null)
            {
                _progressPath.Width = _canvas.Width;
                _progressPath.Height = _canvas.Height;
            }
        }

        private Point GetCenterPoint()
        {
            return new Point(Radius + Thickness / 2, Radius + Thickness / 2);
        }

        //private double GetAngle()
        //{
        //    double v = Value;
        //    if (v < 0) v = 0;
        //    if (v > 100) v = 100;

        //    if (v <= 0) return 0;

        //    // Ép full vòng khi gần 100% (tránh float/animation làm không đúng 100)
        //    if (v >= 99.95) return 360.0;

        //    // Không dùng correction nữa cho determinate fill (mask)
        //    double angle = v / 100.0 * 360.0;

        //    // đảm bảo không chạm đúng 360 để không gây edge-case ở arc math (nhưng vẫn < 360)
        //    return Math.Min(angle, 359.999);
        //}


        private double GetAngle()
        {
            double v = Value;
            if (v < 0) v = 0;
            if (v > 100) v = 100;

            if (v <= 0) return 0;

            // 100%: vẽ full ring để không hở
            if (v >= 100) return 360.0;

            // <100%: dùng đúng logic cũ (correction factor) để tránh “đuôi đè lên đầu”
            double angle = v * CIRCLE_CENTER_TO_BORDER_CORRECTION_FACTOR / 100.0 * 360.0;

            // tránh đúng 360 trong nhánh segment
            return Math.Min(angle, 359.999);
        }


        private void UpdateTrackGeometry(Point centerPoint, double radius)
        {
            if (_outerPath == null || _outerPathFigure == null || _outerArc == null)
                return;

            var circleStart = new Point(centerPoint.X, centerPoint.Y - radius);

            _outerPath.Width = Radius * 2 + Thickness;
            _outerPath.Height = Radius * 2 + Thickness;
            _outerPath.StrokeThickness = Thickness;

            _outerPathFigure.StartPoint = circleStart;
            _outerPathFigure.IsClosed = false;

            _outerArc.IsLargeArc = true;
            _outerArc.Point = ScaleUnitCirclePoint(centerPoint, 359.999, radius);
            _outerArc.Size = new Size(radius, radius);
            _outerArc.SweepDirection = SweepDirection.Clockwise;
        }

        private void EnsureProgressGeometries()
        {
            if (_progressGeom == null)
            {
                _segOuter = new ArcSegment();
                _segEndCap = new ArcSegment();
                _segInner = new ArcSegment();
                _segStartCap = new ArcSegment();

                _progressFig = new PathFigure
                {
                    IsClosed = true,
                    IsFilled = true,
                    Segments = new PathSegmentCollection
                    {
                        _segOuter,
                        _segEndCap,
                        _segInner,
                        _segStartCap
                    }
                };

                _progressGeom = new PathGeometry
                {
                    FillRule = FillRule.Nonzero,
                    Figures = new PathFigureCollection { _progressFig }
                };
            }

            if (_fullRingGeom == null)
            {
                _fullOuterEllipse = new EllipseGeometry();
                _fullInnerEllipse = new EllipseGeometry();

                _fullRingGeom = new GeometryGroup { FillRule = FillRule.EvenOdd };
                _fullRingGeom.Children.Add(_fullOuterEllipse);
                _fullRingGeom.Children.Add(_fullInnerEllipse);
            }
        }

        private void UpdateProgressPathGeometry(Point centerPoint, double angle)
        {
            EnsureProgressGeometries();
            if (_progressPath == null)
                return;

            if (Value <= 0 || angle <= 0.001)
            {
                _progressPath.Visibility = Visibility.Collapsed;
                _progressPath.Data = null;
                return;
            }

            _progressPath.Visibility = Visibility.Visible;

            double capR = Thickness / 2.0;
            double rOuter = Radius + capR;
            double rInner = Math.Max(0.0, Radius - capR);

            // Full ring (near 100%)
            if (angle >= 359.99)
            {
                _fullOuterEllipse.Center = centerPoint;
                _fullOuterEllipse.RadiusX = rOuter;
                _fullOuterEllipse.RadiusY = rOuter;

                _fullInnerEllipse.Center = centerPoint;
                _fullInnerEllipse.RadiusX = rInner;
                _fullInnerEllipse.RadiusY = rInner;

                _progressPath.Data = _fullRingGeom;
                return;
            }

            // Segment with round caps
            var outerStart = ScaleUnitCirclePoint(centerPoint, 0, rOuter);
            var outerEnd = ScaleUnitCirclePoint(centerPoint, angle, rOuter);
            var innerEnd = ScaleUnitCirclePoint(centerPoint, angle, rInner);
            var innerStart = ScaleUnitCirclePoint(centerPoint, 0, rInner);

            _progressFig.StartPoint = outerStart;

            _segOuter.Point = outerEnd;
            _segOuter.Size = new Size(rOuter, rOuter);
            _segOuter.IsLargeArc = angle > 180.0;
            _segOuter.SweepDirection = SweepDirection.Clockwise;

            _segEndCap.Point = innerEnd;
            _segEndCap.Size = new Size(capR, capR);
            _segEndCap.IsLargeArc = false;
            _segEndCap.SweepDirection = SweepDirection.Clockwise;

            _segInner.Point = innerStart;
            _segInner.Size = new Size(rInner, rInner);
            _segInner.IsLargeArc = angle > 180.0;
            _segInner.SweepDirection = SweepDirection.Counterclockwise;

            _segStartCap.Point = outerStart;
            _segStartCap.Size = new Size(capR, capR);
            _segStartCap.IsLargeArc = false;
            _segStartCap.SweepDirection = SweepDirection.Clockwise;

            _progressPath.Data = _progressGeom;
        }

        // Legacy fallback (old stroke-based arc)
        private void UpdateLegacyInnerArc(Point centerPoint, double radius, double angle)
        {
            var circleStart = new Point(centerPoint.X, centerPoint.Y - radius);

            _innerPath.Width = Radius * 2 + Thickness;
            _innerPath.Height = Radius * 2 + Thickness;
            _innerPath.StrokeThickness = Thickness;

            _innerPathFigure.StartPoint = circleStart;
            _innerPathFigure.IsClosed = false;

            _innerArc.IsLargeArc = angle > 180.0;
            _innerArc.Point = ScaleUnitCirclePoint(centerPoint, angle, radius);
            _innerArc.Size = new Size(radius, radius);
            _innerArc.SweepDirection = SweepDirection.Clockwise;

            // keep tiny ellipses if template relies on it
            if (_startEllipse != null)
            {
                _startEllipse.Center = circleStart;
                _startEllipse.RadiusX = 0.25;
                _startEllipse.RadiusY = 0.25;
            }

            if (_endEllipse != null)
            {
                double xEnd = _innerArc.Point.X + Math.Cos(RADIANS * angle);
                double yEnd = _innerArc.Point.Y + Math.Sin(RADIANS * angle);

                _endEllipse.Center = new Point(xEnd, yEnd);
                _endEllipse.RadiusX = 0.25;
                _endEllipse.RadiusY = 0.25;
            }
        }

        private static Point ScaleUnitCirclePoint(Point origin, double angle, double radius)
        {
            return new Point(
                origin.X + Math.Sin(RADIANS * angle) * radius,
                origin.Y - Math.Cos(RADIANS * angle) * radius);
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
                return;

            Radius = progress.RadiusSize;
            Thickness = progress.Thickness;

            SetCanvasMargin(progress.Margin);
            SetTextAlignment(progress.Orientation);
            UpdateFontSizeMessage(progress);
        }

        private void UpdateFontSizeMessage(ProgressCircleDeterminateModel progressDefinition)
        {
            if (_text == null)
                return;

            if (progressDefinition.Type == ProgressCircleDeterminateType.Determinate2)
            {
                _text.FontSize = 13;
                return;
            }

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
