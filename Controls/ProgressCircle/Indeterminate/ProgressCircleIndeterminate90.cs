using System;
using Microsoft.UI.Xaml;

namespace ProgressCircleGradient.Controls.ProgressCircle
{
    public class ProgressCircleIndeterminate90 : ProgressCircleIndeterminate
    {
        public ProgressCircleIndeterminate90() : base()
        {
            // Use the same template/style as the base control.
            DefaultStyleKey = typeof(ProgressCircleIndeterminate);
        }

        protected override double InitialRotationDegrees => 90;
    }
}
