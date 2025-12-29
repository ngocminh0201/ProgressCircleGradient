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
            SizeChanged += ProgressBar_SizeChanged;
        }

        private void SetAnimation()
        {
            _indeterminateAnimation = GetTemplateChild(PART_ANIMATION) as Storyboard;
            if (IsIndeterminate && _indeterminateAnimation != null)
            {
                _indeterminateAnimation.Stop();

                var doubleAnimationB1 = _indeterminateAnimation.Children[0] as DoubleAnimationUsingKeyFrames;
                doubleAnimationB1?.KeyFrames.Clear();
                var B1InitialkeyFrame = CreateSplineDoubleKeyFrame(0, -1.448);
                var B1FinalKeyFrame = CreateSplineDoubleKeyFrame(1.280, 1.096);
                doubleAnimationB1?.KeyFrames.Add(B1InitialkeyFrame);
                doubleAnimationB1?.KeyFrames.Add(B1FinalKeyFrame);

                var doubleAnimationB2 = _indeterminateAnimation.Children[1] as DoubleAnimationUsingKeyFrames;
                doubleAnimationB2?.KeyFrames.Clear();
                var B2InitialkeyFrame = CreateSplineDoubleKeyFrame(0.350, -0.537);
                var B2FinalKeyFrame = CreateSplineDoubleKeyFrame(1.550, 1.048);
                doubleAnimationB2?.KeyFrames.Add(B2InitialkeyFrame);
                doubleAnimationB2?.KeyFrames.Add(B2FinalKeyFrame);

                var doubleAnimationB3 = _indeterminateAnimation.Children[2] as DoubleAnimationUsingKeyFrames;
                doubleAnimationB3?.KeyFrames.Clear();
                var B3InitialkeyFrame = CreateSplineDoubleKeyFrame(0.500, -0.281);
                var B3FinalKeyFrame = CreateSplineDoubleKeyFrame(1.750, 1.015);
                doubleAnimationB3?.KeyFrames.Add(B3InitialkeyFrame);
                doubleAnimationB3?.KeyFrames.Add(B3FinalKeyFrame);

                var doubleAnimationB4 = _indeterminateAnimation.Children[3] as DoubleAnimationUsingKeyFrames;
                doubleAnimationB4?.KeyFrames.Clear();
                var B4InitialkeyFrame = CreateSplineDoubleKeyFrame(0.666, -0.015);
                var B4FinalKeyFrame = CreateSplineDoubleKeyFrame(1.916, 1.015);
                doubleAnimationB4?.KeyFrames.Add(B4InitialkeyFrame);
                doubleAnimationB4?.KeyFrames.Add(B4FinalKeyFrame);

                _indeterminateAnimation.Begin();
                _indeterminateAnimation.Completed += IndeterminateAnimation_Completed;
                _indeterminateAnimation.RepeatBehavior = new RepeatBehavior(1);
            }
        }

        private SplineDoubleKeyFrame CreateSplineDoubleKeyFrame(double initialTime, double value)
        {
            var keyFrame = new SplineDoubleKeyFrame();
            keyFrame.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(initialTime));
            keyFrame.Value = ActualWidth * value;
            keyFrame.KeySpline = new KeySpline()
            {
                ControlPoint1 = new Windows.Foundation.Point(0.33, 0),
                ControlPoint2 = new Windows.Foundation.Point(0.2, 1)
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