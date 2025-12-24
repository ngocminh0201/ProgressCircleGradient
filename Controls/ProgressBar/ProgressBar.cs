using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System;
using Application = Microsoft.UI.Xaml.Application;

namespace ProgressCircleGradient.Controls.ProgressBar
{
    public sealed class ProgressBar : Microsoft.UI.Xaml.Controls.ProgressBar
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
        #endregion

        #region DependencyProperty
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
                doubleAnimationB1.KeyFrames.Clear();
                var B1InitialkeyFrame = CreateSplineDoubleKeyFrame(0, -1.448);
                var B1FinalKeyFrame = CreateSplineDoubleKeyFrame(1.280, 1.096);
                doubleAnimationB1.KeyFrames.Add(B1InitialkeyFrame);
                doubleAnimationB1.KeyFrames.Add(B1FinalKeyFrame);

                var doubleAnimationB2 = _indeterminateAnimation.Children[1] as DoubleAnimationUsingKeyFrames;
                doubleAnimationB2.KeyFrames.Clear();
                var B2InitialkeyFrame = CreateSplineDoubleKeyFrame(0.350, -0.537);
                var B2FinalKeyFrame = CreateSplineDoubleKeyFrame(1.550, 1.048);
                doubleAnimationB2.KeyFrames.Add(B2InitialkeyFrame);
                doubleAnimationB2.KeyFrames.Add(B2FinalKeyFrame);

                var doubleAnimationB3 = _indeterminateAnimation.Children[2] as DoubleAnimationUsingKeyFrames;
                doubleAnimationB3.KeyFrames.Clear();
                var B3InitialkeyFrame = CreateSplineDoubleKeyFrame(0.500, -0.281);
                var B3FinalKeyFrame = CreateSplineDoubleKeyFrame(1.750, 1.015);
                doubleAnimationB3.KeyFrames.Add(B3InitialkeyFrame);
                doubleAnimationB3.KeyFrames.Add(B3FinalKeyFrame);

                var doubleAnimationB4 = _indeterminateAnimation.Children[3] as DoubleAnimationUsingKeyFrames;
                doubleAnimationB4.KeyFrames.Clear();
                var B4InitialkeyFrame = CreateSplineDoubleKeyFrame(0.666, -0.015);
                var B4FinalKeyFrame = CreateSplineDoubleKeyFrame(1.916, 1.015);
                doubleAnimationB4.KeyFrames.Add(B4InitialkeyFrame);
                doubleAnimationB4.KeyFrames.Add(B4FinalKeyFrame);

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

        /// <summary>
        /// This event is necessary for WinUI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgressBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ForceUpdateProgressIndicator();
            SetAnimation();
        }

        /// <summary>
        /// This event is necessary for WinUI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            ValueChanged += ProgressBar_ValueChanged;
        }

        /// <summary>
        /// This event is necessary for WinUI.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// For WinUI, this events are necessary to assign.
        /// </summary>
        private void AssignInternalEvents()
        {
            Loaded += ProgressBar_Loaded;
            SizeChanged += ProgressBar_SizeChanged;
            Unloaded += ProgressBar_Unloaded;
        }

        /// <summary>
        /// For WinUI, it's necessary force update progress indicator.
        /// </summary>
        private void ForceUpdateProgressIndicator()
        {
            UpdateIndicatorElement();
        }

        /* 
         * The animation runs once, if the animation is run in forever mode, strange behavior happens, the bar starts to miscalculate
        This way we forced runs once ever after completed
        */
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            InitiateIndeterminateAnimation();
            UpdateStyle();
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

            UpdateIndicatorElement();
        }

        private void UpdateIndicatorElement()
        {
            if (!IsIndeterminate)
            {
                _progressBarIndicator = GetTemplateChild(PROGRESS_BAR_INDICATOR) as Rectangle;
                if (_progressBarIndicator != null && Maximum > 0)
                {
                    _progressBarIndicator.Width = ActualWidth * (Value / Maximum);
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