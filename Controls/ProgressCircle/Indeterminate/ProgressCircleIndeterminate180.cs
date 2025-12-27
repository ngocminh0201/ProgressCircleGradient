using System;
using Microsoft.UI.Xaml;

namespace ProgressCircleGradient.Controls.ProgressCircle
{
    public class ProgressCircleIndeterminate180 : ProgressCircleIndeterminate
    {
        public ProgressCircleIndeterminate180() : base()
        {
            DefaultStyleKey = typeof(ProgressCircleIndeterminate);
        }

        protected override double InitialRotationDegrees => 180;
    }
}
