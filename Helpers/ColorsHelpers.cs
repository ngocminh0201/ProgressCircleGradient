using System;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ProgressCircleGradient.Helpers
{
    internal static class ColorsHelpers
    {
        public static SolidColorBrush ConvertColorHex(string colorString)
        {
            try
            {
                var converter = new System.Drawing.ColorConverter();
                var colorObj = converter.ConvertFromString(colorString);
                if (colorObj is System.Drawing.Color colorConverted)
                {
                    var colorARGB = Color.FromArgb(colorConverted.A, colorConverted.R, colorConverted.G, colorConverted.B);
                    return new SolidColorBrush(colorARGB);
                }
                else
                {
                    return new SolidColorBrush();
                }
            }
            catch (ArgumentException)
            {
                return new SolidColorBrush();
            }
        }

        public static Color ConvertHexToColor(string hex, double alpha = 100)
        {
            hex = hex.Replace("#", string.Empty);

            byte a = (byte)Math.Round(alpha / 100f * 255), r = 0, g = 0, b = 0;

            if (hex.Length == 8)
            {
                a = Convert.ToByte(hex.Substring(0, 2), 16);
                r = Convert.ToByte(hex.Substring(2, 2), 16);
                g = Convert.ToByte(hex.Substring(4, 2), 16);
                b = Convert.ToByte(hex.Substring(6, 2), 16);
            }
            else if (hex.Length == 6)
            {
                r = Convert.ToByte(hex.Substring(0, 2), 16);
                g = Convert.ToByte(hex.Substring(2, 2), 16);
                b = Convert.ToByte(hex.Substring(4, 2), 16);
            }
            else
            {
                throw new ArgumentException("Invalid color format. Use #RRGGBB or #AARRGGBB.");
            }

            return Color.FromArgb(a, r, g, b);
        }

        public static SolidColorBrush CreateFromChannels(int alpha, int red, int green, int blue)
        {
            byte a = (byte)Math.Clamp(alpha, 0, 255);
            byte r = (byte)Math.Clamp(red, 0, 255);
            byte g = (byte)Math.Clamp(green, 0, 255);
            byte b = (byte)Math.Clamp(blue, 0, 255);

            var colorARGB = Color.FromArgb(a, r, g, b);
            return new SolidColorBrush(colorARGB);
        }

        public static bool AreColorsEqual(SolidColorBrush firstSolidColorBrush, SolidColorBrush secondSolidColorBrush)
        {
            if (firstSolidColorBrush == null || secondSolidColorBrush == null)
            {
                return false;
            }
            return firstSolidColorBrush.Color.ToString().Equals(secondSolidColorBrush.Color.ToString());
        }
    }
}