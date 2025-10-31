using System.Windows.Input;

namespace CloudZCrypt.WPF.Commands;

public sealed class RelayCommand : ICommand
{
    private readonly Func<Task>? asyncExecute;
    private readonly Action? execute;
    private readonly Func<bool>? canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        asyncExecute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return canExecute?.Invoke() ?? true;
    }

    public async void Execute(object? parameter)
    {
        if (asyncExecute is not null)
        {
            await asyncExecute();
            return;
        }
        execute?.Invoke();
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
