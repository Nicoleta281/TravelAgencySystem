using System;
using System.Windows.Input;

namespace TravelAgency.WPF.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            var value = parameter is T t ? t : default;
            return _canExecute?.Invoke(value) ?? true;
        }

        public void Execute(object? parameter)
        {
            var value = parameter is T t ? t : default;
            _execute(value);
        }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}