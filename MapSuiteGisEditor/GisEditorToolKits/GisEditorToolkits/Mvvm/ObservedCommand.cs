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
    public class ObservedCommand : ICommand
    {
        private Action execute;
        private Func<bool> canExecute;

        private event EventHandler canExecuteChanged;

        public ObservedCommand()
            : this(null, null)
        { }

        /// <summary>
        /// Create a new command that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public ObservedCommand(Action execute)
            : this(execute, null)
        { }

        public ObservedCommand(Action executeAction, Func<bool> canExecuteFunc)
        {
            if (executeAction != null) execute += executeAction;
            if (canExecuteFunc != null) canExecute += canExecuteFunc;
        }

        public event Action Executed
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

        public event Func<bool> CanExecuting
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

        public void Execute(object e)
        {
            ExecuteCore();
        }

        protected virtual void ExecuteCore()
        {
            Action handler = execute;
            if (handler != null)
            {
                handler();
            }
        }

        /// <summary>
        /// Tests whether the current data context allows this command to be run
        /// </summary>
        /// <param name="parameter">Parameter passed to the object's CanExecute delegate</param>
        /// <returns>True if the command is currently enabled; false if not enabled.</returns>
        public bool CanExecute(object e)
        {
            return CanExecuteCore();
        }

        protected virtual bool CanExecuteCore()
        {
            Func<bool> handler = canExecute;
            if (handler != null)
            {
                return handler();
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