namespace ThunderbirdViewerWPF
{
    using System;
    using System.Windows.Input;

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
                return true;

            // WPF ruft CanExecute oft mit null auf
            if (parameter == null)
                return typeof(T).IsClass || default(T) == null
                    ? _canExecute(default)
                    : false;

            // WPF kann falsche Typen liefern
            if (parameter is not T typedParameter)
                return false;

            return _canExecute(typedParameter);
        }

        public void Execute(object parameter)
        {
            if (parameter is T typedParameter)
            {
                _execute(typedParameter);
            }
            else if (parameter == null && (typeof(T).IsClass || default(T) == null))
            {
                _execute(default);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
            => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter)
            => _execute();

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
