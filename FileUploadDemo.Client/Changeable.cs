using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace FileUploadDemoClient
{
    public abstract class Changeable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, args);
        }

        protected void NotifyOfPropertyChange(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException("propertyName");
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public void NotifyOfPropertyChange<TProp>(Expression<Func<TProp>> property)
        {
            NotifyOfPropertyChange(GetName(property));
        }

        public string GetPropertyName<TProp>(Expression<Func<TProp>> property)
        {
            return GetName(property);
        }

        private static string GetName<TProp>(Expression<Func<TProp>> property)
        {
            var expr = property.Body as MemberExpression;
            if (expr == null)
                throw new InvalidOperationException("Invalid property type");
            return expr.Member.Name;
        }
    }
}