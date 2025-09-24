using System.Windows.Input;

namespace CloudZCrypt.WPF.Commands;

/// <summary>
/// Represents a versatile implementation of <see cref="ICommand"/> supporting both synchronous and asynchronous delegates.
/// </summary>
/// <remarks>
/// This command can be constructed with either a synchronous <see cref="Action"/> or an asynchronous <see cref="Func{Task}"/>.
/// Only one execution delegate is stored (sync or async). The optional canExecute delegate determines the current
/// availability of the command. The <see cref="CanExecute(object?)"/> method ignores the <paramref name="parameter"/> and
/// delegates the decision exclusively to the supplied predicate (if any). <see cref="Execute(object?)"/> will await the
/// asynchronous delegate when provided.
/// </remarks>
public sealed class RelayCommand : ICommand
{
    private readonly Func<Task>? asyncExecute;
    private readonly Action? execute;
    private readonly Func<bool>? canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class with a synchronous execute delegate.
    /// </summary>
    /// <param name="execute">The synchronous action to invoke when the command is executed. Cannot be null.</param>
    /// <param name="canExecute">Optional predicate that returns true when the command is allowed to execute; if null, the command is always executable.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="execute"/> is null.</exception>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class with an asynchronous execute delegate.
    /// </summary>
    /// <param name="execute">The asynchronous function to invoke when the command is executed. Cannot be null.</param>
    /// <param name="canExecute">Optional predicate that returns true when the command is allowed to execute; if null, the command is always executable.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="execute"/> is null.</exception>
    public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        asyncExecute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    /// <summary>
    /// Occurs when changes occur that affect whether the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">An optional parameter ignored by this implementation.</param>
    /// <returns>true if the command can execute; otherwise, false.</returns>
    public bool CanExecute(object? parameter)
    {
        return canExecute?.Invoke() ?? true;
    }

    /// <summary>
    /// Executes the command's delegate.
    /// </summary>
    /// <param name="parameter">An optional parameter ignored by this implementation.</param>
    /// <remarks>
    /// If an asynchronous delegate was supplied, it is awaited. Because WPF's <see cref="ICommand.Execute(object)"/>
    /// signature is void, this method is declared async void; unhandled exceptions will propagate to the
    /// synchronization context. Consider handling exceptions within the provided delegate.
    /// </remarks>
    public async void Execute(object? parameter)
    {
        if (asyncExecute is not null)
        {
            await asyncExecute();
            return;
        }
        execute?.Invoke();
    }

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event to signal that the executability of the command may have changed.
    /// </summary>
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
