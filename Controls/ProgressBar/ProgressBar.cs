using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // Determinate drifting (match AngularGradientBrush speed: 1 cycle/1.7s)
        private const double DeterminateDriftDurationSeconds = 3.6;
        private static readonly TimeSpan DeterminateDriftTickInterval = TimeSpan.FromMilliseconds(16);
        private const double DeterminateDriftDirection = 1.0; // -1: drift left, +1: drift right

        // Seamless repeat synthesis for determinate gradient (higher = smoother, fewer visible seams; keep modest for perf)
        private const int DeterminateSeamlessSampleCount = 32;
        private const int DeterminateSeamlessSmoothRadius = 3;
        #endregion
        #region Variable
        private Storyboard _indeterminateAnimation;
        private TextBlock _progressText;
        private Rectangle _progressBarIndicator;

        // Determinate brush instance we "own" (cloned from Foreground/MaskBrush)
        private LinearGradientBrush? _fixedGradientBrush;

        // Determinate transforms: Translate (drift) then Scale (mapping fix)
        private TransformGroup? _determinateTransformGroup;
        private TranslateTransform? _determinateGradientTranslate;
        private MatrixTransform? _determinateGradientScaleMatrix;
        private bool _determinateSeamlessPrepared;

        // Determinate drift timer state
        private DispatcherQueueTimer? _determinateDriftTimer;
        private DateTimeOffset _determinateLastTickUtc;
        private double _determinatePhase01;
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
                self.FixDeterminateGradientMapping();
                self.UpdateDeterminateGradientDriftState();
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
                if (self.Visibility == Visibility.Collapsed)
                {
                    self.StopAnimation();
                    return;
                }

                // Visible again
                if (self.IsIndeterminate)
                {
                    self.SetAnimation();
                }
                else
                {
                    self.FixDeterminateGradientMapping();
                    self.UpdateDeterminateGradientDriftState();
                }
            }
        }

        #endregion

        #region Internal Events

        private void ProgressBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ForceUpdateProgressIndicator();
            SetAnimation();

            FixDeterminateGradientMapping();
            UpdateDeterminateGradientDriftState();
        }

        private void ProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            ValueChanged += ProgressBar_ValueChanged;

            FixDeterminateGradientMapping();
            UpdateDeterminateGradientDriftState();
        }

        private void ProgressBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ForceUpdateProgressIndicator();

            FixDeterminateGradientMapping();
            UpdateDeterminateGradientDriftState();
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

            StopDeterminateGradientDrift();
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

        private double GetDeterminateRatio()
        {
            if (Maximum <= 0)
                return 0.0;

            double ratio = Value / Maximum;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
                ratio = 0.0;

            return Math.Clamp(ratio, 0.00001, 1.0);
        }

        private void EnsureDeterminateBrushTransforms(LinearGradientBrush brush)
        {
            if (_determinateTransformGroup != null &&
                _determinateGradientTranslate != null &&
                _determinateGradientScaleMatrix != null &&
                ReferenceEquals(brush.RelativeTransform, _determinateTransformGroup))
            {
                return;
            }

            _determinateGradientTranslate = new TranslateTransform();
            _determinateGradientScaleMatrix = new MatrixTransform
            {
                Matrix = new Matrix { M11 = 1, M22 = 1 }
            };

            _determinateTransformGroup = new TransformGroup();
            _determinateTransformGroup.Children.Add(_determinateGradientTranslate);
            _determinateTransformGroup.Children.Add(_determinateGradientScaleMatrix);

            brush.RelativeTransform = _determinateTransformGroup;
        }

        private static void MakeGradientSeamlessForRepeat(LinearGradientBrush brush)
        {
            if (brush.GradientStops == null || brush.GradientStops.Count < 2)
                return;

            // Sort original stops and make sure [0..1] endpoints exist
            var src = brush.GradientStops
                .Select(s => new GradientStop { Offset = s.Offset, Color = s.Color })
                .OrderBy(s => s.Offset)
                .ToList();

            if (src[0].Offset > 0)
                src.Insert(0, new GradientStop { Offset = 0.0, Color = src[0].Color });
            if (src[^1].Offset < 1)
                src.Add(new GradientStop { Offset = 1.0, Color = src[^1].Color });

            static Windows.UI.Color Lerp(Windows.UI.Color a, Windows.UI.Color b, double t)
            {
                t = Math.Clamp(t, 0.0, 1.0);
                byte lerp(byte x, byte y) => (byte)Math.Clamp((int)Math.Round(x + (y - x) * t), 0, 255);
                return Windows.UI.Color.FromArgb(
                    lerp(a.A, b.A),
                    lerp(a.R, b.R),
                    lerp(a.G, b.G),
                    lerp(a.B, b.B));
            }

            static Windows.UI.Color Eval(List<GradientStop> stops, double t)
            {
                t = Math.Clamp(t, 0.0, 1.0);
                // Fast paths
                if (t <= stops[0].Offset)
                    return stops[0].Color;
                if (t >= stops[^1].Offset)
                    return stops[^1].Color;

                // Find segment (linear scan is fine for small stop counts)
                for (int i = 0; i < stops.Count - 1; i++)
                {
                    var s0 = stops[i];
                    var s1 = stops[i + 1];
                    if (t >= s0.Offset && t <= s1.Offset)
                    {
                        var span = s1.Offset - s0.Offset;
                        if (span <= 1e-9)
                            return s1.Color;
                        var u = (t - s0.Offset) / span;
                        return Lerp(s0.Color, s1.Color, u);
                    }
                }

                return stops[^1].Color;
            }

            // 1) Sample the original gradient as a periodic signal (exclude 1.0, we'll close it later)
            int n = Math.Max(8, DeterminateSeamlessSampleCount);
            var samples = new Windows.UI.Color[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / n; // [0,1)
                samples[i] = Eval(src, t);
            }

            // 2) Circular smoothing to remove visible seams at the repeat boundary.
            //    This spreads any mismatch around 0/1 over a small neighborhood (looks like the phone video).
            int r = Math.Clamp(DeterminateSeamlessSmoothRadius, 1, 8);
            int[] w = new int[2 * r + 1];
            int wsum = 0;
            for (int k = -r; k <= r; k++)
            {
                // triangular weights: r+1-|k|
                int wk = (r + 1) - Math.Abs(k);
                w[k + r] = wk;
                wsum += wk;
            }

            var smooth = new Windows.UI.Color[n];
            for (int i = 0; i < n; i++)
            {
                double a = 0, rr = 0, gg = 0, bb = 0;
                for (int k = -r; k <= r; k++)
                {
                    int idx = (i + k) % n;
                    if (idx < 0) idx += n;
                    var c = samples[idx];
                    int wk = w[k + r];
                    a += c.A * wk;
                    rr += c.R * wk;
                    gg += c.G * wk;
                    bb += c.B * wk;
                }
                smooth[i] = Windows.UI.Color.FromArgb(
                    (byte)Math.Clamp((int)Math.Round(a / wsum), 0, 255),
                    (byte)Math.Clamp((int)Math.Round(rr / wsum), 0, 255),
                    (byte)Math.Clamp((int)Math.Round(gg / wsum), 0, 255),
                    (byte)Math.Clamp((int)Math.Round(bb / wsum), 0, 255));
            }

            // 3) Rebuild stops: ensure explicit closure so Repeat has NO visible seam.
            brush.GradientStops.Clear();
            for (int i = 0; i < n; i++)
            {
                brush.GradientStops.Add(new GradientStop
                {
                    Offset = (double)i / n,
                    Color = smooth[i]
                });
            }
            // Close at 1.0 with the same color as 0.0
            brush.GradientStops.Add(new GradientStop
            {
                Offset = 1.0,
                Color = smooth[0]
            });
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
                _determinateTransformGroup = null;
                _determinateGradientTranslate = null;
                _determinateGradientScaleMatrix = null;
                _determinateSeamlessPrepared = false;
                return;
            }

            // Clone once so we don't mutate ThemeResource brushes
            if (!ReferenceEquals(current, _fixedGradientBrush))
            {
                _fixedGradientBrush = CloneLinearGradientBrush(current);
                _determinateSeamlessPrepared = false;

                // We need Repeat for continuous drift, but only if the gradient is seamless
                _fixedGradientBrush.SpreadMethod = GradientSpreadMethod.Repeat;

                EnsureDeterminateBrushTransforms(_fixedGradientBrush);
                _progressBarIndicator.Fill = _fixedGradientBrush;
            }

            if (_fixedGradientBrush == null)
                return;

            // Prepare once: make it seamless under Repeat
            if (!_determinateSeamlessPrepared)
            {
                MakeGradientSeamlessForRepeat(_fixedGradientBrush);
                _determinateSeamlessPrepared = true;
            }

            // Ensure we still own the expected transform chain
            EnsureDeterminateBrushTransforms(_fixedGradientBrush);

            // Only apply mapping fix for horizontal gradients
            if (Math.Abs(_fixedGradientBrush.StartPoint.Y - _fixedGradientBrush.EndPoint.Y) < 0.000001)
            {
                double ratio = GetDeterminateRatio();

                if (_determinateGradientScaleMatrix != null)
                {
                    _determinateGradientScaleMatrix.Matrix = new Matrix
                    {
                        M11 = 1.0 / ratio,
                        M12 = 0,
                        M21 = 0,
                        M22 = 1,
                        OffsetX = 0,
                        OffsetY = 0
                    };
                }
            }
            else
            {
                // Non-horizontal: keep drift, but no special scaling
                if (_determinateGradientScaleMatrix != null)
                {
                    _determinateGradientScaleMatrix.Matrix = new Matrix
                    {
                        M11 = 1,
                        M12 = 0,
                        M21 = 0,
                        M22 = 1,
                        OffsetX = 0,
                        OffsetY = 0
                    };
                }
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
            UpdateDeterminateGradientDriftState();
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
            FixDeterminateGradientMapping();
            UpdateDeterminateGradientDriftState();
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
                    UpdateDeterminateGradientDriftState();
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

        // =========================
        // Determinate drift
        // =========================
        private void UpdateDeterminateGradientDriftState()
        {
            if (IsIndeterminate || Visibility == Visibility.Collapsed)
            {
                StopDeterminateGradientDrift();
                return;
            }

            _progressBarIndicator ??= GetTemplateChild(PROGRESS_BAR_INDICATOR) as Rectangle;
            if (_progressBarIndicator?.Fill is LinearGradientBrush && _determinateGradientTranslate != null)
            {
                StartDeterminateGradientDrift();
            }
            else
            {
                StopDeterminateGradientDrift();
            }
        }

        private void StartDeterminateGradientDrift()
        {
            if (_determinateDriftTimer != null)
                return;

            var dq = DispatcherQueue.GetForCurrentThread();
            _determinateDriftTimer = dq.CreateTimer();
            _determinateDriftTimer.Interval = DeterminateDriftTickInterval;
            _determinateDriftTimer.IsRepeating = true;
            _determinateDriftTimer.Tick += OnDeterminateDriftTick;

            _determinateLastTickUtc = DateTimeOffset.UtcNow;
            _determinateDriftTimer.Start();
        }

        private void StopDeterminateGradientDrift()
        {
            if (_determinateDriftTimer == null)
                return;

            _determinateDriftTimer.Tick -= OnDeterminateDriftTick;
            _determinateDriftTimer.Stop();
            _determinateDriftTimer = null;
        }

        private void OnDeterminateDriftTick(DispatcherQueueTimer sender, object args)
        {
            if (IsIndeterminate || Visibility == Visibility.Collapsed || _determinateGradientTranslate == null)
            {
                StopDeterminateGradientDrift();
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var dt = (now - _determinateLastTickUtc).TotalSeconds;
            _determinateLastTickUtc = now;

            // Phase in [0..1) in gradient space (t). This makes wrapping seamless.
            _determinatePhase01 = (_determinatePhase01 + (dt / DeterminateDriftDurationSeconds)) % 1.0;

            // IMPORTANT: because we also apply a Scale (M11 = 1/ratio),
            // the Repeat period in Translate-space becomes "ratio".
            // Setting Translate = phase * ratio ensures the wrap happens on an integer period after scaling.
            double ratio = GetDeterminateRatio();
            _determinateGradientTranslate.X = DeterminateDriftDirection * _determinatePhase01 * ratio;
        }

        #endregion
        #endregion
    }
}
