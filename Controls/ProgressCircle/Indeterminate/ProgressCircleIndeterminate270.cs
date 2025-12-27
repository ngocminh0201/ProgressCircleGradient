using System;
using Microsoft.UI.Xaml;

namespace ProgressCircleGradient.Controls.ProgressCircle
{
    public class ProgressCircleIndeterminate270 : ProgressCircleIndeterminate
    {
        public ProgressCircleIndeterminate270() : base()
        {
            DefaultStyleKey = typeof(ProgressCircleIndeterminate);
        }

        protected override double InitialRotationDegrees => 270;
    }
}
