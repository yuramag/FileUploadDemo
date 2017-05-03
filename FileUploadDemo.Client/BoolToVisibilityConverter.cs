using System.Windows;
using System.Windows.Data;

namespace FileUploadDemoClient
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : SimpleValueConverter<bool, Visibility>
    {
        protected override Visibility ConvertBase(bool input)
        {
            return input ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override bool ConvertBackBase(Visibility input)
        {
            return input == Visibility.Visible;
        }
    }
}