using Microsoft.UI.Xaml;
using ProgressCircleGradient.Controls.ProgressCircle;

namespace ProgressCircleGradient.Controls.ProgressCircle.Determinate
{
    internal class ProgressCircleDeterminateModel
    {
        public ProgressCircleDeterminateType Type
        {
            get; set;
        }
        public ProgressCircleIndeterminateOrientation Orientation
        {
            get; set;
        }
        public ProgressCircleSize Size
        {
            get; set;
        }
        public double RadiusSize
        {
            get; set;
        }
        public double Thickness
        {
            get; set;
        }
        public Thickness Margin
        {
            get; set;
        }
    }
}
