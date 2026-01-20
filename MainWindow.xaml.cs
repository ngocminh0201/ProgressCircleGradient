using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ProgressCircleGradient
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // Default page
            NavView.SelectedItem = NavItem_AngularBorder;
            ShowPage("AngularBorder");
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
            {
                ShowPage(tag);
            }
        }

        private void ShowPage(string tag)
        {
            Page_AngularBorder.Visibility = tag == "AngularBorder" ? Visibility.Visible : Visibility.Collapsed;
            Page_AngularImage.Visibility = tag == "AngularImage" ? Visibility.Visible : Visibility.Collapsed;

            Page_ProgressBarDeterminate.Visibility = tag == "ProgressBarDeterminate" ? Visibility.Visible : Visibility.Collapsed;
            Page_ProgressBarIndeterminate.Visibility = tag == "ProgressBarIndeterminate" ? Visibility.Visible : Visibility.Collapsed;

            Page_ProgressCircleDeterminate.Visibility = tag == "ProgressCircleDeterminate" ? Visibility.Visible : Visibility.Collapsed;
            Page_ProgressCircleIndeterminate.Visibility = tag == "ProgressCircleIndeterminate" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TbPbDeterminateWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TryParsePositiveDouble(TbPbDeterminateWidth.Text, out double width))
            {
                pbDeterminateLinear.Width = width;
            }
        }

        private void TbPbIndeterminateWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TryParsePositiveDouble(TbPbIndeterminateWidth.Text, out double width))
            {
                pbIndeterminateLinear.Width = width;
            }
        }

        private void CbPcDeterminateSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbPcDeterminateSize.SelectedItem is ComboBoxItem item && item.Content is string sizeName)
            {
                SetSize(pcDeterminateAngular, sizeName);
            }
        }

        private void CbPcIndeterminateSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbPcIndeterminateSize.SelectedItem is ComboBoxItem item && item.Content is string sizeName)
            {
                SetSize(pcIndeterminateAngular, sizeName);
            }
        }

        private static bool TryParsePositiveDouble(string text, out double value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Allow both "," and "." as decimal separators.
            string normalized = text.Trim().Replace(',', '.');

            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && v > 0)
            {
                value = v;
                return true;
            }

            if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out v) && v > 0)
            {
                value = v;
                return true;
            }

            return false;
        }

        // Reflection-based: 
        private static void SetSize(object control, string sizeName)
        {
            if (control is null || string.IsNullOrWhiteSpace(sizeName))
                return;

            var prop = control.GetType().GetProperty("Size");
            if (prop is null || !prop.CanWrite)
                return;

            var targetType = prop.PropertyType;
            object value = sizeName;

            if (targetType.IsEnum)
            {
                try
                {
                    value = Enum.Parse(targetType, sizeName);
                }
                catch
                {
                    return;
                }
            }

            prop.SetValue(control, value);
        }
    }
}
