using System.Windows.Input;

namespace PostOffice.Infrastructure;

// Команда модели представления: хранит выполняемое действие и условие
// его доступности, поэтому сама не знает, что именно выполняется.
// Конструктор ожидает два параметра:
// 1) выполняемое действие                                  - Action<object>
// 2) признак, можно ли выполнять команду (необязательный)  - Func<object, bool>
public class RelayCommand(Action<object> execute, Func<object, bool> canExecute = null!)
    : ICommand
{

    // о смене доступности команд оповещает сам WPF при перепросмотре
    public event EventHandler? CanExecuteChanged {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    } // CanExecuteChanged


    // можно ли выполнять команду; без заданного условия команда доступна всегда
    public bool CanExecute(object? parameter) =>
        canExecute == null! || canExecute(parameter!);


    // выполнить действие команды
    public void Execute(object? parameter) => execute(parameter!);

} // class RelayCommand
