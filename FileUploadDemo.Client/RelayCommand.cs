using System;
using System.Diagnostics;
using System.Windows.Input;

namespace FileUploadDemoClient
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> m_execute;
        private readonly Predicate<T> m_canExecute;

        public RelayCommand(Action<T> execute)
            : this(execute, null) { }

        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            m_execute = execute;
            m_canExecute = canExecute ?? (param => true);
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return m_canExecute((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            var prm = (T)parameter;
            if (!CanExecute(prm))
                throw new InvalidOperationException("Command cannot be executed");
            m_execute(prm);
        }
    }

    public class RelayCommand : RelayCommand<object>
    {
        public RelayCommand(Action execute)
            : base(param => execute(), null) { }

        public RelayCommand(Action execute, Func<bool> canExecute)
            : base(param => execute(), param => canExecute()) { }
    }
}