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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class Numeric : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached("Value", typeof(decimal), typeof(Numeric), new PropertyMetadata(new PropertyChangedCallback(ValuePropertyChanged)));
        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(Numeric));
        private RoutedCommand cmdUp;
        private RoutedCommand cmdDown;
        private DispatcherTimer updownTimer;
        private UpDownMode updownMode;
        private bool allowDecimal;
        private TextBox tbValue = null;

        private enum UpDownMode
        {
            None = 0,
            Up = 1,
            Down = 2
        }

        public event RoutedEventHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        public Numeric()
        {
            DefaultStyleKey = typeof(Numeric);

            cmdUp = new RoutedCommand();
            cmdDown = new RoutedCommand();
            Maximum = int.MaxValue;
            Minimum = int.MinValue;
            Increment = 1;
            updownTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(200) };
            updownTimer.Tick += new System.EventHandler(updownTimer_Tick);
        }

        public bool AllowDecimal
        {
            get { return allowDecimal; }
            set { allowDecimal = value; }
        }

        public decimal Increment { get; set; }

        public int Maximum { get; set; }

        public int Minimum { get; set; }

        public decimal Value
        {
            get { return (decimal)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            tbValue.Focus();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Button btnUp = (Button)GetTemplateChild("btnUp");
            Button btnDown = (Button)GetTemplateChild("btnDown");
            tbValue = (TextBox)GetTemplateChild("tbValue");

            btnUp.CommandBindings.Add(new CommandBinding(cmdUp, Executed, CanExecuted));
            btnUp.Command = cmdUp;
            btnDown.CommandBindings.Add(new CommandBinding(cmdDown, Executed, CanExecuted));
            btnDown.Command = cmdDown;

            btnUp.PreviewMouseDown -= updownButtonMouseDown;
            btnUp.PreviewMouseUp -= updownButtonMouseUp;
            btnDown.PreviewMouseDown -= updownButtonMouseDown;
            btnDown.PreviewMouseUp -= updownButtonMouseUp;
            tbValue.PreviewKeyDown -= tbValue_PreviewKeyDown;

            btnUp.PreviewMouseDown += updownButtonMouseDown;
            btnUp.PreviewMouseUp += updownButtonMouseUp;
            btnDown.PreviewMouseDown += updownButtonMouseDown;
            btnDown.PreviewMouseUp += updownButtonMouseUp;
            tbValue.PreviewKeyDown += tbValue_PreviewKeyDown;
        }

        private void updownTimer_Tick(object sender, EventArgs e)
        {
            this.Focus();
            GotoNextValue();
            OnValueChanged();
        }

        private void tbValue_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = !LimitedNumericInputService.IsValidDecimalKey(e.Key, AllowDecimal);
        }

        private void updownButtonMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (updownTimer.IsEnabled)
            {
                updownTimer.Stop();
            }
        }

        private void updownButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Focus();
            Button button = (Button)sender;
            switch (button.CommandParameter.ToString())
            {
                case "UP":
                    updownMode = UpDownMode.Up;
                    break;

                case "DW":
                    updownMode = UpDownMode.Down;
                    break;
            }

            if (!updownTimer.IsEnabled)
            {
                updownTimer.Start();
            }
        }

        private void CanExecuted(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter.Equals("UP"))
            {
                e.CanExecute = Value <= Maximum;
            }
            else if (e.Parameter.Equals("DW"))
            {
                e.CanExecute = Value >= Minimum;
            }
        }

        private void Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Focus();
            if (e.Parameter.Equals("UP"))
            {
                updownMode = UpDownMode.Up;
            }
            else if (e.Parameter.Equals("DW"))
            {
                updownMode = UpDownMode.Down;
            }

            GotoNextValue();
        }

        private static void ValuePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((Numeric)sender).OnValueChanged();
        }

        protected virtual void OnValueChanged()
        {
            RoutedEventArgs e = new RoutedEventArgs(Numeric.ValueChangedEvent);
            RaiseEvent(e);
        }

        private void GotoNextValue()
        {
            decimal currentValue = Value;
            if (updownMode == UpDownMode.Up)
            {
                //if (currentValue % 1 != 0 && Increment % 1 == 0) currentValue = Math.Ceiling(currentValue);
                currentValue += Increment;
            }
            else if (updownMode == UpDownMode.Down)
            {
                //if (currentValue % 1 != 0 && Increment % 1 == 0) currentValue = Math.Floor(currentValue);
                currentValue -= Increment;
            }

            if (currentValue < Minimum) Value = Minimum;
            else if (currentValue > Maximum) Value = Maximum;
            else Value = currentValue;
        }
    }
}