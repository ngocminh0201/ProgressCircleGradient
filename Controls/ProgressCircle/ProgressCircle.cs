using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ProgressCircleGradient.Controls.ProgressCircle
{
    public abstract class ProgressCircle : Control
    {
        #region Protected Variables
        protected Grid colorGrid;
        protected readonly long tokenOnFlowDirectionPropertyChanged;
        internal IDictionary<ProgressCircleIndeterminateOrientation, string> _progressCircleTextAlingmentDictionary = new Dictionary<ProgressCircleIndeterminateOrientation, string>()
        {
            { ProgressCircleIndeterminateOrientation.Vertical, "TextVerticalAlignment"},
            { ProgressCircleIndeterminateOrientation.Horizontal, "TextHorizontalAlignment"}
        };
        #endregion

        #region Size Property
        public ProgressCircleSize Size
        {
            get => (ProgressCircleSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(ProgressCircleSize), typeof(ProgressCircle), new PropertyMetadata(ProgressCircleSize.Small, OnSizePropertyChangedInternal));


        private static void OnSizePropertyChangedInternal(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressCircle progressCircle)
            {
                progressCircle.OnSizePropertyChanged(d, e);
            }
        }

        protected abstract void OnSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e);
        #endregion

        #region Text Property
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(ProgressCircle), new PropertyMetadata(null));
        #endregion

        protected internal void ForceLeftToRightFlowDirection()
        {
            if (FlowDirection != FlowDirection.LeftToRight)
            {
                FlowDirection = FlowDirection.LeftToRight;
            }
        }

        protected ProgressCircle()
        {
            FlowDirection = FlowDirection.LeftToRight;
            Unloaded += ProgressCircle_Unloaded;
            tokenOnFlowDirectionPropertyChanged = RegisterPropertyChangedCallback(FlowDirectionProperty, OnFlowDirectionPropertyChanged);
        }

        private void ProgressCircle_Unloaded(object sender, RoutedEventArgs e)
        {
            UnregisterPropertyChangedCallback(FlowDirectionProperty, tokenOnFlowDirectionPropertyChanged);
        }

        private void OnFlowDirectionPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            ForceLeftToRightFlowDirection();
        }
    }
}
