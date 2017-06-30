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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ThinkGeo.MapSuite.GisEditor
{
    [TemplateVisualState(Name = "Idle", GroupName = "BusyStatusStates"), TemplateVisualState(Name = "Busy", GroupName = "BusyStatusStates"), TemplateVisualState(Name = "Visible", GroupName = "VisibilityStates"), TemplateVisualState(Name = "Hidden", GroupName = "VisibilityStates"), StyleTypedProperty(Property = "OverlayStyle", StyleTargetType = typeof(Rectangle)), StyleTypedProperty(Property = "ProgressBarStyle", StyleTargetType = typeof(ProgressBar))]
    public class BusyIndicator : ContentControl, INotifyPropertyChanged
    {
        private DispatcherTimer _displayAfterTimer = new DispatcherTimer();
        public static readonly DependencyProperty IsBusyProperty;
        public static readonly DependencyProperty BusyContentProperty;
        public static readonly DependencyProperty BusyContentTemplateProperty;
        public static readonly DependencyProperty DisplayAfterProperty;
        public static readonly DependencyProperty OverlayStyleProperty;
        public static readonly DependencyProperty ProgressBarStyleProperty;
        public static readonly DependencyProperty CanCancelProperty;
        public event EventHandler Cancelled;
        private Button cancelButton;
        private bool canCancel;

        static BusyIndicator()
        {
            BusyIndicator.CanCancelProperty = DependencyProperty.Register("CanCancel", typeof(bool), typeof(BusyIndicator), new UIPropertyMetadata(false));
            BusyIndicator.IsBusyProperty = DependencyProperty.Register("IsBusy", typeof(bool), typeof(BusyIndicator), new PropertyMetadata(true, new PropertyChangedCallback(BusyIndicator.OnIsBusyChanged)));
            BusyIndicator.BusyContentProperty = DependencyProperty.Register("BusyContent", typeof(object), typeof(BusyIndicator), new PropertyMetadata(null));
            BusyIndicator.BusyContentTemplateProperty = DependencyProperty.Register("BusyContentTemplate", typeof(DataTemplate), typeof(BusyIndicator), new PropertyMetadata(null));
            BusyIndicator.DisplayAfterProperty = DependencyProperty.Register("DisplayAfter", typeof(TimeSpan), typeof(BusyIndicator), new PropertyMetadata(TimeSpan.FromSeconds(0.1)));
            BusyIndicator.OverlayStyleProperty = DependencyProperty.Register("OverlayStyle", typeof(Style), typeof(BusyIndicator), new PropertyMetadata(null));
            BusyIndicator.ProgressBarStyleProperty = DependencyProperty.Register("ProgressBarStyle", typeof(Style), typeof(BusyIndicator), new PropertyMetadata(null));
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyIndicator), new FrameworkPropertyMetadata(typeof(BusyIndicator)));
        }

        public BusyIndicator()
        {
            this._displayAfterTimer.Tick += new EventHandler(this.DisplayAfterTimerElapsed);
        }

        protected bool IsContentVisible
        {
            get;
            set;
        }

        public bool CanCancel
        {
            get { return canCancel; }
            set
            {
                canCancel = value;
                OnPropertyChanged("CanCancel");
            }
        }

        public bool IsBusy
        {
            get
            {
                return (bool)base.GetValue(BusyIndicator.IsBusyProperty);
            }
            set
            {
                base.SetValue(BusyIndicator.IsBusyProperty, value);
            }
        }

        public object BusyContent
        {
            get
            {
                return base.GetValue(BusyIndicator.BusyContentProperty);
            }
            set
            {
                base.SetValue(BusyIndicator.BusyContentProperty, value);
            }
        }

        public DataTemplate BusyContentTemplate
        {
            get
            {
                return (DataTemplate)base.GetValue(BusyIndicator.BusyContentTemplateProperty);
            }
            set
            {
                base.SetValue(BusyIndicator.BusyContentTemplateProperty, value);
            }
        }

        public TimeSpan DisplayAfter
        {
            get
            {
                return (TimeSpan)base.GetValue(BusyIndicator.DisplayAfterProperty);
            }
            set
            {
                base.SetValue(BusyIndicator.DisplayAfterProperty, value);
            }
        }

        public Style OverlayStyle
        {
            get
            {
                return (Style)base.GetValue(BusyIndicator.OverlayStyleProperty);
            }
            set
            {
                base.SetValue(BusyIndicator.OverlayStyleProperty, value);
            }
        }

        public Style ProgressBarStyle
        {
            get
            {
                return (Style)base.GetValue(BusyIndicator.ProgressBarStyleProperty);
            }
            set
            {
                base.SetValue(BusyIndicator.ProgressBarStyleProperty, value);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.ChangeVisualState(false);
            cancelButton = (Button)GetTemplateChild("CancelButton");
            cancelButton.Click -= new RoutedEventHandler(CancelButtonClick);
            cancelButton.Click += new RoutedEventHandler(CancelButtonClick);
        }

        protected virtual void OnIsBusyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (this.IsBusy)
            {
                if (this.DisplayAfter.Equals(TimeSpan.Zero))
                {
                    this.IsContentVisible = true;
                }
                else
                {
                    this._displayAfterTimer.Interval = this.DisplayAfter;
                    this._displayAfterTimer.Start();
                }
            }
            else
            {
                this._displayAfterTimer.Stop();
                this.IsContentVisible = false;
            }
            this.ChangeVisualState(true);
        }

        protected virtual void ChangeVisualState(bool useTransitions)
        {
            VisualStateManager.GoToState(this, this.IsBusy ? "Busy" : "Idle", useTransitions);
            VisualStateManager.GoToState(this, this.IsContentVisible ? "Visible" : "Hidden", useTransitions);
        }

        protected virtual void OnCancelled()
        {
            EventHandler handler = Cancelled;
            if (handler != null) handler(this, new EventArgs());
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            OnCancelled();
        }

        private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BusyIndicator)d).OnIsBusyChanged(e);
        }

        private void DisplayAfterTimerElapsed(object sender, EventArgs e)
        {
            this._displayAfterTimer.Stop();
            this.IsContentVisible = true;
            this.ChangeVisualState(true);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if(handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
