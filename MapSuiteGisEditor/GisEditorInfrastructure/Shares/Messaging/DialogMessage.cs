using System;
using System.Windows;

namespace GalaSoft.MvvmLight.Messaging
{
    // Replacement for the deprecated MvvmLight DialogMessage type.
    public class DialogMessage
    {
        public DialogMessage(string content, Action<MessageBoxResult> callback)
        {
            Content = content;
            Callback = callback;
            Button = MessageBoxButton.OK;
            Icon = MessageBoxImage.None;
        }

        public DialogMessage(object sender, string content, Action<MessageBoxResult> callback)
        {
            Sender = sender;
            Content = content;
            Callback = callback;
            Button = MessageBoxButton.OK;
            Icon = MessageBoxImage.None;
        }

        public object Sender { get; }

        public string Content { get; }

        public string Caption { get; set; }

        public MessageBoxButton Button { get; set; }

        public MessageBoxImage Icon { get; set; }

        public Action<MessageBoxResult> Callback { get; }
    }
}
