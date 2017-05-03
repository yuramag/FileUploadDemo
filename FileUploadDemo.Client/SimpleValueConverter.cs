using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileUploadDemoClient
{
    public abstract class SimpleValueConverter<TSource, TTarget> : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TSource)
            {
                try
                {
                    return ConvertBase((TSource)value);
                }
                catch (NotSupportedException) { }
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TTarget)
            {
                try
                {
                    return ConvertBackBase((TTarget)value);
                }
                catch (NotSupportedException) { }
            }
            return DependencyProperty.UnsetValue;
        }

        protected abstract TTarget ConvertBase(TSource input);

        protected virtual TSource ConvertBackBase(TTarget input)
        {
            throw new NotSupportedException();
        }
    }
}