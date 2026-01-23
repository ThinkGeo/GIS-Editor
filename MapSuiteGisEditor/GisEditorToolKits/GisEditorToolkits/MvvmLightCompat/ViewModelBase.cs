using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace GalaSoft.MvvmLight
{
    // Minimal replacement for MvvmLight ViewModelBase.
    public class ViewModelBase : INotifyPropertyChanged
    {
        protected Messaging.Messenger MessengerInstance { get; set; } = Messaging.Messenger.Default;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            string propertyName = GetPropertyName(propertyExpression);
            if (!string.IsNullOrEmpty(propertyName))
            {
                RaisePropertyChanged(propertyName);
            }
        }

        protected virtual bool Set<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        public virtual void Cleanup()
        {
        }

        private static string GetPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null) return null;

            if (propertyExpression.Body is MemberExpression member)
            {
                return member.Member.Name;
            }

            if (propertyExpression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
            {
                return unaryMember.Member.Name;
            }

            return null;
        }
    }
}
