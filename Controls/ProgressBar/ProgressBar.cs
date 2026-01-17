using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System;
using Application = Microsoft.UI.Xaml.Application;

namespace ProgressCircleGradient.Controls.ProgressBar
{
    public sealed partial class ProgressBar : Microsoft.UI.Xaml.Controls.ProgressBar
    {
        #region Constants
        private const string PART_ANIMATION = "IndeterminateAnimation";
        private const string PART_PROGRESSTEXT = "OneUIProgressTextBlock";
        private const string STATE_NORMAL = "Normal";
        private const string STATE_INDETERMINATE = "Indeterminate";
        private const string PROGRESS_BAR_INDICATOR = "ProgressBarIndicator";
        private const string DETERMINATE_STYLE = "OneUIProgressBarDeterminateStyle";
        private const string INDETERMINATE_STYLE = "OneUIProgressBarIndeterminateStyle";
        #endregion

        #region Variable
        private Storyboard _indeterminateAnimation;
        private TextBlock _progressText;
        private Rectangle _progressBarIndicator;
        private LinearGradientBrush? _fixedGradientBrush;
        private MatrixTransform? _fixedGradientTransform;

        #endregion

        #region DependencyProperty
        public Brush MaskBrush
        {
            get => (Brush)GetValue(MaskBrushProperty);
            set => SetValue(MaskBrushProperty, value);
        }

        public static readonly DependencyProperty MaskBrushProperty =
            DependencyProperty.Register(
                nameof(MaskBrush),
                typeof(Brush),
                typeof(ProgressBar),
                new PropertyMetadata(null, OnMaskBrushChanged));

        private static void OnMaskBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressBar self)
            {
                self.ApplyMaskBrush();
            }
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ProgressBar), new PropertyMetadata(null, OnTextPropertyChanged));
        #endregion

        #region Methods
        #region Public Methods
        public ProgressBar()
        {
            Style = (Style)(IsIndeterminate ? Application.Current.Resources[INDETERMINATE_STYLE] : Application.Current.Resources[DETERMINATE_STYLE]);
            RegisterPropertyChangedCallback(IsIndeterminateProperty, OnIndeterminatePropertyChanged);
            RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityPropertyChanged);

            AssignInternalEvents();
        }

        private Grid? _indeterminateClipHost;
        private TranslateTransform? _indeterminateTranslateTransform;
        private Rectangle? _indeterminateActiveRect;

        private void SetAnimation()
        {
            _indeterminateAnimation = GetTemplateChild(PART_ANIMATION) as Storyboard;

            // Indeterminate template parts (WinUI 3 - no OpacityMask/ClipToBounds)
            _indeterminateClipHost = GetTemplateChild("IndeterminateClipHost") as Grid;
            _indeterminateTranslateTransform = GetTemplateChild("IndeterminateTranslateTransform") as TranslateTransform;
            _indeterminateActiveRect = GetTemplateChild("IndeterminateActiveRect") as Rectangle;

            if (_indeterminateClipHost != null)
            {
                _indeterminateClipHost.SizeChanged -= IndeterminateClipHost_SizeChanged;
                _indeterminateClipHost.SizeChanged += IndeterminateClipHost_SizeChanged;
            }

            UpdateIndeterminateAnimation();
        }

        private void IndeterminateClipHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateIndeterminateAnimation();
        }

        private void UpdateIndeterminateAnimation()
        {
            if (!IsIndeterminate)
            {
                _indeterminateAnimation?.Stop();
                return;
            }

            if (_indeterminateAnimation == null ||
                _indeterminateClipHost == null ||
                _indeterminateTranslateTransform == null ||
                _indeterminateActiveRect == null)
            {
                return;
            }

            double w = ActualWidth;
            double h = _indeterminateClipHost.ActualHeight;

            if (w <= 0 || h <= 0)
            {
                return;
            }

            // Active rect width equals track width (w). It moves from -w -> +w.
            _indeterminateActiveRect.Width = w;

            // Clip to bounds (replacement for ClipToBounds)
            _indeterminateClipHost.Clip = new RectangleGeometry()
            {
                Rect = new Windows.Foundation.Rect(0, 0, w, h)
            };

            // Reset start position
            _indeterminateTranslateTransform.X = -w;

            // IMPORTANT: stop storyboard BEFORE mutating keyframes (WinUI throws if active)
            _indeterminateAnimation.Stop();

            if (_indeterminateAnimation.Children.Count > 0 &&
                _indeterminateAnimation.Children[0] is DoubleAnimationUsingKeyFrames translateAnim)
            {
                // Prefer updating existing keyframes instead of clearing (safer & less churn)
                if (translateAnim.KeyFrames.Count >= 2 &&
                    translateAnim.KeyFrames[0] is SplineDoubleKeyFrame k0 &&
                    translateAnim.KeyFrames[1] is SplineDoubleKeyFrame k1)
                {
                    k0.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0));
                    k0.Value = -w;
                    k0.KeySpline = new KeySpline()
                    {
                        ControlPoint1 = new Windows.Foundation.Point(0.54, 0),
                        ControlPoint2 = new Windows.Foundation.Point(0.38, 1)
                    };

                    k1.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.2));
                    k1.Value = +w;
                    k1.KeySpline = new KeySpline()
                    {
                        ControlPoint1 = new Windows.Foundation.Point(0.54, 0),
                        ControlPoint2 = new Windows.Foundation.Point(0.38, 1)
                    };
                }
                else
                {
                    translateAnim.KeyFrames.Clear();
                    translateAnim.KeyFrames.Add(CreateSplineDoubleKeyFrame(0.0, -w));
                    translateAnim.KeyFrames.Add(CreateSplineDoubleKeyFrame(1.2, +w));
                }
            }

            _indeterminateAnimation.Begin();
        }

        private SplineDoubleKeyFrame CreateSplineDoubleKeyFrame(double timeSeconds, double value)
        {
            var keyFrame = new SplineDoubleKeyFrame();
            keyFrame.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(timeSeconds));
            keyFrame.Value = value;
            keyFrame.KeySpline = new KeySpline()
            {
                ControlPoint1 = new Windows.Foundation.Point(0.54, 0),
                ControlPoint2 = new Windows.Foundation.Point(0.38, 1)
            };
            return keyFrame;
        }

        private static void OnVisibilityPropertyChanged(DependencyObject d, DependencyProperty dp)
        {
            if (d is ProgressBar self)
            {
                if (!self.IsIndeterminate)
                {
                    return;
                }

                if (self.Visibility == Visibility.Collapsed)
                {
                    self.StopAnimation();
                }
                else
                {
                    self.SetAnimation();
                }
            }
        }

        #endregion

        #region Internal Events

        private void ProgressBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ForceUpdateProgressIndicator();
            SetAnimation();
        }

        private void ProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            ValueChanged += ProgressBar_ValueChanged;
        }

        private void ProgressBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ForceUpdateProgressIndicator();
        }

        private void ProgressBar_Unloaded(object sender, RoutedEventArgs e)
        {
            StopAnimation();
        }

        private void StopAnimation()
        {
            if (_indeterminateAnimation != null)
            {
                _indeterminateAnimation.Completed -= IndeterminateAnimation_Completed;
                _indeterminateAnimation?.Stop();
            }
        }

        #endregion

        #region PrivateMethods
        private static LinearGradientBrush CloneLinearGradientBrush(LinearGradientBrush src)
        {
            var clone = new LinearGradientBrush
            {
                StartPoint = src.StartPoint,
                EndPoint = src.EndPoint,
                Opacity = src.Opacity,
                SpreadMethod = src.SpreadMethod
            };

            foreach (var gs in src.GradientStops)
            {
                clone.GradientStops.Add(new GradientStop
                {
                    Color = gs.Color,
                    Offset = gs.Offset
                });
            }

            return clone;
        }

        private void FixDeterminateGradientMapping()
        {
            if (IsIndeterminate)
                return;

            _progressBarIndicator ??= GetTemplateChild(PROGRESS_BAR_INDICATOR) as Rectangle;
            if (_progressBarIndicator == null)
                return;

            if (_progressBarIndicator.Fill is not LinearGradientBrush current)
            {
                _fixedGradientBrush = null;
                _fixedGradientTransform = null;
                return;
            }

            if (!ReferenceEquals(current, _fixedGradientBrush))
            {
                _fixedGradientBrush = CloneLinearGradientBrush(current);
                _fixedGradientTransform = new MatrixTransform();
                _fixedGradientBrush.RelativeTransform = _fixedGradientTransform;
                _progressBarIndicator.Fill = _fixedGradientBrush;
            }

            if (Math.Abs(_fixedGradientBrush.StartPoint.Y - _fixedGradientBrush.EndPoint.Y) < 0.000001)
            {
                double ratio = (Maximum > 0) ? (Value / Maximum) : 0.0;
                ratio = Math.Clamp(ratio, 0.00001, 1.0);

                _fixedGradientTransform.Matrix = new Matrix
                {
                    M11 = 1.0 / ratio,
                    M12 = 0,
                    M21 = 0,
                    M22 = 1,
                    OffsetX = 0,
                    OffsetY = 0
                };
            }
            else
            {
                _fixedGradientBrush.RelativeTransform = null;
            }
        }

        private void ApplyMaskBrush()
        {
            if (IsIndeterminate)
                return;

            _progressBarIndicator ??= GetTemplateChild(PROGRESS_BAR_INDICATOR) as Rectangle;
            if (_progressBarIndicator == null)
                return;

            if (MaskBrush != null)
            {
                _progressBarIndicator.Fill = MaskBrush;
            }
        }

        private void AssignInternalEvents()
        {
            Loaded += ProgressBar_Loaded;
            SizeChanged += ProgressBar_SizeChanged;
            Unloaded += ProgressBar_Unloaded;
        }

        private void ForceUpdateProgressIndicator()
        {
            UpdateIndicatorElement();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            InitiateIndeterminateAnimation();
            UpdateStyle();
            ApplyMaskBrush();
            FixDeterminateGradientMapping();
            UpdateLayoutProgressText();
        }

        private void InitiateIndeterminateAnimation()
        {
            SetAnimation();
        }

        private void UpdateLayoutProgressText()
        {
            if (_progressText == null)
                _progressText = GetTemplateChild(PART_PROGRESSTEXT) as TextBlock;

            if (_progressText != null && !string.IsNullOrEmpty(Text))
            {
                _progressText.Text = Text;
                _progressText.Visibility = Visibility.Visible;
            }
        }

        private void OnIndeterminatePropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (sender is ProgressBar self)
            {
                self.UpdateStyle();
            }
        }

        private void UpdateStyle()
        {
            Style = (Style)(IsIndeterminate ? Application.Current.Resources[INDETERMINATE_STYLE] : Application.Current.Resources[DETERMINATE_STYLE]);
            ApplyMaskBrush();
            UpdateIndicatorElement();
        }

        private void UpdateIndicatorElement()
        {
            if (!IsIndeterminate)
            {
                _progressBarIndicator = GetTemplateChild(PROGRESS_BAR_INDICATOR) as Rectangle;
                ApplyMaskBrush();
                if (_progressBarIndicator != null && Maximum > 0)
                {
                    _progressBarIndicator.Width = ActualWidth * (Value / Maximum);
                    FixDeterminateGradientMapping();
                }
            }
        }

        private void IndeterminateAnimation_Completed(object sender, object e)
        {
            RestartAnimation();
        }

        private void RestartAnimation()
        {
            VisualStateManager.GoToState(this, STATE_NORMAL, false);
            VisualStateManager.GoToState(this, STATE_INDETERMINATE, false);
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressBar self)
            {
                if (self._progressText == null)
                    return;

                self._progressText.Visibility = string.IsNullOrEmpty(self.Text) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        #endregion
        #endregion
    }
}