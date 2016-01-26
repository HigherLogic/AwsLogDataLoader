using System;
using System.Windows.Input;

namespace AwsLogDataLoader
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _action;
        public bool CanExecuteValue { get; set; } = false;

        public RelayCommand(Action<object> action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return CanExecuteValue;
        }

        public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public event EventHandler CanExecuteChanged;
    }
}
