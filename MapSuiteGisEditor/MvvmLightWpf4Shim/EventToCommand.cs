using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace GalaSoft.MvvmLight.Command
{
    // Minimal EventToCommand replacement to satisfy legacy XAML references.
    public class EventToCommand : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(EventToCommand), new PropertyMetadata(null));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(EventToCommand), new PropertyMetadata(null));

        public static readonly DependencyProperty PassEventArgsToCommandProperty =
            DependencyProperty.Register("PassEventArgsToCommand", typeof(bool), typeof(EventToCommand), new PropertyMetadata(false));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public bool PassEventArgsToCommand
        {
            get { return (bool)GetValue(PassEventArgsToCommandProperty); }
            set { SetValue(PassEventArgsToCommandProperty, value); }
        }

        protected override void Invoke(object parameter)
        {
            var command = Command;
            if (command == null) return;

            object param = CommandParameter;
            if (param == null && PassEventArgsToCommand)
            {
                param = parameter;
            }

            if (command.CanExecute(param))
            {
                command.Execute(param);
            }
        }
    }
}
