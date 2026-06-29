using System;
using System.Windows.Input;

namespace Macad.Presentation;

public static class CommandEventHub
{
    public static event Action<ICommand, object> CommandExecuted;

    public static void FireCommandExecuted(ICommand command, object parameter)
    {
        CommandExecuted?.Invoke(command, parameter);
    }
}
