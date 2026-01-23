using System;
using System.Collections.Generic;

namespace GalaSoft.MvvmLight.Messaging
{
    // Minimal replacement for MvvmLight Messenger and messages.
    public sealed class Messenger
    {
        private readonly object syncRoot = new object();
        private readonly List<Registration> registrations = new List<Registration>();

        public static Messenger Default { get; } = new Messenger();

        private Messenger() { }

        public void Register<T>(object recipient, Action<T> action)
        {
            Register(recipient, null, action);
        }

        public void Register<T>(object recipient, object token, Action<T> action)
        {
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            if (action == null) throw new ArgumentNullException(nameof(action));

            lock (syncRoot)
            {
                registrations.Add(new Registration(recipient, token, typeof(T), message => action((T)message)));
            }
        }

        public void Send<T>(T message)
        {
            Send(message, null);
        }

        public void Send<T>(T message, object token)
        {
            List<Registration> snapshot;
            lock (syncRoot)
            {
                snapshot = new List<Registration>(registrations);
            }

            foreach (var registration in snapshot)
            {
                if (registration.MessageType != typeof(T)) continue;
                if (!TokenMatches(registration.Token, token)) continue;

                if (!registration.TryGetRecipient(out _))
                {
                    Remove(registration);
                    continue;
                }

                registration.Action(message);
            }
        }

        public void Unregister(object recipient)
        {
            if (recipient == null) return;

            lock (syncRoot)
            {
                registrations.RemoveAll(r => r.IsRecipient(recipient));
            }
        }

        private void Remove(Registration registration)
        {
            lock (syncRoot)
            {
                registrations.Remove(registration);
            }
        }

        private static bool TokenMatches(object registeredToken, object messageToken)
        {
            if (registeredToken == null && messageToken == null) return true;
            if (registeredToken == null || messageToken == null) return false;
            return Equals(registeredToken, messageToken);
        }

        private sealed class Registration
        {
            private readonly WeakReference recipientRef;

            public Registration(object recipient, object token, Type messageType, Action<object> action)
            {
                recipientRef = new WeakReference(recipient);
                Token = token;
                MessageType = messageType;
                Action = action;
            }

            public object Token { get; }

            public Type MessageType { get; }

            public Action<object> Action { get; }

            public bool TryGetRecipient(out object recipient)
            {
                recipient = recipientRef.Target;
                return recipient != null;
            }

            public bool IsRecipient(object recipient)
            {
                return recipientRef.Target == recipient;
            }
        }
    }

    public class NotificationMessage
    {
        public NotificationMessage(string notification)
        {
            Notification = notification;
        }

        public string Notification { get; }
    }

    public class NotificationMessage<T> : NotificationMessage
    {
        public NotificationMessage(T content, string notification)
            : base(notification)
        {
            Content = content;
        }

        public T Content { get; }
    }

    public class NotificationMessageAction<T> : NotificationMessage
    {
        public NotificationMessageAction(string notification, Action<T> callback)
            : base(notification)
        {
            Callback = callback;
        }

        public Action<T> Callback { get; }

        public void Execute(T parameter)
        {
            Callback?.Invoke(parameter);
        }
    }
}
