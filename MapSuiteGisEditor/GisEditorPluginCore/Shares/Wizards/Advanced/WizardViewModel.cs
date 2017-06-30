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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using GalaSoft.MvvmLight.Command;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class WizardViewModel<T> : Collection<WizardStep<T>>, IEnumerator<WizardStep<T>>, INotifyPropertyChanged where T : class
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<FinishedWizardEventArgs<T>> Finished;

        public event EventHandler<CancelledWizardEventArgs<T>> Cancelled;

        private int currentStepIndex = -1;
        private bool isInBatch;
        private bool isActionButtonEnabled;
        private bool isCancellationRequested;
        private string title;
        private string helpKey;
        private T targetObject;
        private ObservedCommand backCommand;
        private ObservedCommand nextCommand;
        private ObservedCommand finishCommand;
        [NonSerialized]
        private RelayCommand cancelCommand;
        private MoveDirection moveDirection;
        private Exception error;
        private BatchTaskState state;

        public WizardViewModel()
        {
            isActionButtonEnabled = true;
        }

        public string HelpKey
        {
            get { return helpKey; }
            set
            {
                helpKey = value;
                OnPropertyChanged("HelpKey");
                OnPropertyChanged("HelpButtonVisibility");
            }
        }

        public Visibility HelpButtonVisibility
        {
            get { return string.IsNullOrEmpty(HelpKey) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public ObservedCommand BackCommand
        {
            get
            {
                if (backCommand == null)
                {
                    backCommand = new ObservedCommand(() =>
                    {
                        MoveBack();
                    }, () => currentStepIndex > 0);
                }
                return backCommand;
            }
        }

        public ObservedCommand NextCommand
        {
            get
            {
                if (nextCommand == null)
                {
                    nextCommand = new ObservedCommand(() =>
                    {
                        MoveNext();
                    },
                    () =>
                    {
                        bool canExecute = (currentStepIndex < Count - 1);
                        if (Current != null)
                        {
                            canExecute &= Current.CanMoveToNext();
                        }
                        return canExecute;
                    });
                }
                return nextCommand;
            }
        }

        public ObservedCommand FinishCommand
        {
            get
            {
                if (finishCommand == null)
                {
                    finishCommand = new ObservedCommand(() =>
                    {
                        RaiseFinishCommand();
                    }, () =>
                    {
                        bool canExecute = currentStepIndex == (Count - 1);
                        if (canExecute && Current != null)
                        {
                            canExecute &= Current.CanMoveToNext();
                        }
                        return canExecute;
                    });
                }
                return finishCommand;
            }
        }

        public RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(() =>
                    {
                        IsCancellationRequested = true;
                        CancelledCore();
                        OnCancelled(new CancelledWizardEventArgs<T>(TargetObject));
                    });
                }
                return cancelCommand;
            }
        }

        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        public T TargetObject
        {
            get { return targetObject; }
            set { targetObject = value; }
        }

        public virtual string TaskName
        {
            get { return GetType().Name; }
        }

        public WizardStep<T> Current
        {
            get
            {
                if (currentStepIndex > -1 && currentStepIndex < Count)
                {
                    return this[currentStepIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        public bool IsBatchTask
        {
            get { return isInBatch; }
            set { isInBatch = value; }
        }

        public virtual string FinishButtonText { get { return "Finish"; } }

        public MoveDirection MoveDirection
        {
            get { return moveDirection; }
            set { moveDirection = value; }
        }

        public bool IsActionButtonEnabled
        {
            get { return isActionButtonEnabled; }
            set
            {
                isActionButtonEnabled = value;
                OnPropertyChanged("IsActionButtonEnabled");
            }
        }

        public bool IsCancellationRequested
        {
            get { return isCancellationRequested; }
            set
            {
                isCancellationRequested = true;
                OnPropertyChanged("IsCancellationRequested");
            }
        }

        public Exception Error
        {
            get { return error; }
            set { error = value; OnPropertyChanged("Error"); }
        }

        public BatchTaskState State
        {
            get { return state; }
            set { state = value; OnPropertyChanged("State"); }
        }

        protected override void InsertItem(int index, WizardStep<T> item)
        {
            base.InsertItem(index, item);
            item.Parent = this;
        }

        public void Execute()
        {
            ExecuteCore();
        }

        protected virtual void ExecuteCore() { }

        public void Cancel()
        {
            CancelCore();
        }

        protected virtual void CancelCore() { }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public bool IsPreviousButtonVisible { get { return currentStepIndex > 0; } }

        public bool IsNextButtonVisible { get { return currentStepIndex < Count - 1; } }

        public bool IsFinishButtonVisible { get { return currentStepIndex == Count - 1; } }

        public bool MoveNext()
        {
            MoveDirection = MoveDirection.Next;

            if (LeaveOldStep())
            {
                bool hasNext = ++currentStepIndex < Count;
                if (hasNext)
                {
                    EnterNewStep();
                }
                else
                {
                    currentStepIndex--;
                    hasNext = true;
                }
                OnPropertyChanged("Current");
                OnPropertyChanged("IsPreviousButtonVisible");
                OnPropertyChanged("IsNextButtonVisible");
                OnPropertyChanged("IsFinishButtonVisible");
                OnPropertyChanged("AddToBatchButtonVisible");
                return hasNext;
            }
            else if (currentStepIndex == Count - 1)
            {
                return false;
            }
            else
            {
                return currentStepIndex < Count;
            }
        }

        public bool MoveBack()
        {
            MoveDirection = MoveDirection.Back;
            LeaveOldStep();
            bool hasBack = --currentStepIndex >= 0;
            EnterNewStep();
            OnPropertyChanged("Current");
            OnPropertyChanged("IsPreviousButtonVisible");
            OnPropertyChanged("IsNextButtonVisible");
            OnPropertyChanged("IsFinishButtonVisible");
            OnPropertyChanged("AddToBatchButtonVisible");
            return hasBack;
        }

        public void Reset()
        {
            MoveDirection = MoveDirection.None;
            LeaveOldStep();
            currentStepIndex = -1;
            EnterNewStep();
            OnPropertyChanged("Current");
            OnPropertyChanged("IsPreviousButtonVisible");
            OnPropertyChanged("IsNextButtonVisible");
            OnPropertyChanged("IsFinishButtonVisible");
            OnPropertyChanged("AddToBatchButtonVisible");
        }

        protected void OnFinished(FinishedWizardEventArgs<T> e)
        {
            EventHandler<FinishedWizardEventArgs<T>> handler = Finished;
            if (handler != null) handler(this, e);
        }

        protected virtual void FinishedCore() { }

        protected void OnCancelled(CancelledWizardEventArgs<T> e)
        {
            EventHandler<CancelledWizardEventArgs<T>> handler = Cancelled;
            if (handler != null) handler(this, e);
        }

        protected virtual void CancelledCore() { }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool LeaveOldStep()
        {
            if (Current != null) return Current.Leave(TargetObject);
            else return true;
        }

        private void EnterNewStep()
        {
            foreach (var item in this)
            {
                item.IsCurrent = Current == item;
            }

            if (Current != null) Current.Enter(TargetObject);
        }

        #region commands

        private void RaiseFinishCommand()
        {
            if (MoveNext())
            {
                IsActionButtonEnabled = false;
                FinishedCore();
                OnFinished(new FinishedWizardEventArgs<T>(TargetObject));
            }
        }

        #endregion commands

        void IDisposable.Dispose()
        {
        }
    }
}