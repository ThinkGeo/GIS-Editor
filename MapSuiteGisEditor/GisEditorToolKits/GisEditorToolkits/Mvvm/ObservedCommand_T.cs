/*
* Licensed to the Apache Software Foundation (ASF) under one
* or more contributor license agreements.  See the NOTICE file
* distributed with this work for additional information
* regarding copyright ownership.  The ASF licenses this file
* to you under the Apache License, Version 2.0 (the
* "License"); you may not use this file except in compliance
* with the License.  You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/


using System;
using System.Windows.Input;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// A command object recognized by WPF. Implements the
    /// System.Windows.Input.ICommand interface
    /// </summary>
    [Serializable]
    public class ObservedCommand<T> : ICommand
    {
        private Action<T> execute;
        private Func<T, bool> canExecute;

        private event EventHandler canExecuteChanged;

        public ObservedCommand()
            : this(null, null)
        { }

        /// <summary>
        /// Create a new command that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public ObservedCommand(Action<T> execute)
            : this(execute, null)
        { }

        public ObservedCommand(Action<T> executeAction, Func<T, bool> canExecuteFunc)
        {
            if (executeAction != null) execute += executeAction;
            if (canExecuteFunc != null) canExecute += canExecuteFunc;
        }

        public event Action<T> Executed
        {
            add
            {
                execute -= value;
                execute += value;
            }
            remove
            {
                execute -= value;
            }
        }

        public event Func<T, bool> CanExecuting
        {
            add
            {
                canExecute -= value;
                canExecute += value;
            }
            remove
            {
                canExecute -= value;
            }
        }

        /// <summary>
        /// Event that is raised when the "CanExecute" status of this command changes
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                canExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                canExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        public void Execute()
        {
            Execute(default(T));
        }

        /// <summary>
        /// Performs the work of the "execute" delegate.
        /// </summary>
        /// <param name="parameter"></param>
        public void Execute(object parameter)
        {
            ExecuteCore(parameter);
        }

        protected virtual void ExecuteCore(object parameter)
        {
            Action<T> handler = execute;
            if (handler != null)
            {
                handler((T)parameter);
            }
        }

        /// <summary>
        /// Tests whether the current data context allows this command to be run
        /// </summary>
        /// <param name="parameter">Parameter passed to the object's CanExecute delegate</param>
        /// <returns>True if the command is currently enabled; false if not enabled.</returns>
        public bool CanExecute(object parameter)
        {
            return CanExecuteCore(parameter);
        }

        protected virtual bool CanExecuteCore(object parameter)
        {
            Func<T, bool> handler = canExecute;
            if (handler != null)
            {
                if (parameter != null)
                {
                    return handler((T)parameter);
                }
                else
                {
                    return handler(default(T));
                }
            }
            else return false;
        }

        /// <summary>
        /// Causes the CanExecuteChanged handler to tun
        /// </summary>
        /// <remarks>Should only be invoked by the view model</remarks>
        public void RaiseExecuteChanged()
        {
            RaiseExecuteChangedCore();
        }

        protected virtual void RaiseExecuteChangedCore()
        {
            EventHandler handler = canExecuteChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}