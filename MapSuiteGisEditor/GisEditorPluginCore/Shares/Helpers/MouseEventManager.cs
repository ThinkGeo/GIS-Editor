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
using System.Windows.Input;
using System.Windows.Threading;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class MouseEventManager
    {
        public event EventHandler<MouseButtonEventArgs> MouseDoubleClick;

        public event EventHandler<MouseButtonEventArgs> MouseClick;

        public event EventHandler<MouseButtonEventArgs> MouseButtonDown;

        public event EventHandler<MouseButtonEventArgs> MouseButtonUp;

        public event EventHandler<MouseEventArgs> MouseMove;

        public event EventHandler<MouseEventArgs> MouseLeave;

        public event EventHandler<MouseEventArgs> MouseEnter;

        public event EventHandler<MouseWheelEventArgs> MouseWheel;

        internal event EventHandler<MouseButtonEventArgs> ActualMouseButtonDown;

        private static int systemClickInterval = System.Windows.Forms.SystemInformation.DoubleClickTime;
        private object tempMouseDownSender;
        private int mouseDownCount;
        private int mouseUpCount;

        [NonSerialized]
        private UIElement element;

        [NonSerialized]
        private DispatcherTimer timerMouseDown;

        [NonSerialized]
        private MouseButtonEventArgs tempMouseDownEventArgs;

        public MouseEventManager(UIElement element)
        {
            this.element = element;
            this.element.MouseDown += new MouseButtonEventHandler(OnMouseButtonDown);
            this.element.MouseUp += new MouseButtonEventHandler(OnMouseButtonUp);
            this.element.MouseEnter += new MouseEventHandler(OnMouseEnter);
            this.element.MouseLeave += new MouseEventHandler(OnMouseLeave);
            this.element.MouseMove += new MouseEventHandler(OnMouseMove);
            this.element.MouseWheel += new MouseWheelEventHandler(OnMouseWheel);

            timerMouseDown = new DispatcherTimer();
            timerMouseDown.Interval = TimeSpan.FromMilliseconds(systemClickInterval);
            timerMouseDown.Tick += new EventHandler(TimerMouseDown_Tick);
        }

        public static int SystemClickInterval
        {
            get { return systemClickInterval; }
            set
            {
                systemClickInterval = value;
            }
        }

        protected void OnMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            OnActualMouseButtonDown(e);
            mouseDownCount++;
            tempMouseDownSender = sender;
            tempMouseDownEventArgs = e;
            timerMouseDown.Interval = TimeSpan.FromMilliseconds(SystemClickInterval);
            timerMouseDown.Start();
            e.Handled = true;
        }

        protected virtual void OnMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (timerMouseDown.IsEnabled)
            {
                mouseUpCount++;
            }
            else
            {
                mouseUpCount = 0;
                mouseDownCount = 0;

                EventHandler<MouseButtonEventArgs> handler = MouseButtonUp;
                if (handler != null) { handler(sender, e); }
                element.ReleaseMouseCapture();
            }
            //e.Handled = true;
        }

        protected virtual void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (timerMouseDown.IsEnabled)
            {
                RaiseSpecialMouseEvent(sender, tempMouseDownEventArgs);
            }

            EventHandler<MouseEventArgs> handler = MouseMove;
            if (handler != null) { handler(sender, e); }
            //e.Handled = true;
        }

        protected virtual void OnMouseLeave(object sender, MouseEventArgs e)
        {
            EventHandler<MouseEventArgs> handler = MouseLeave;
            if (handler != null) { handler(sender, e); }
            //e.Handled = true;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            EventHandler<MouseEventArgs> handler = MouseEnter;
            if (handler != null) { handler(sender, e); }
            //e.Handled = true;
        }

        protected virtual void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            EventHandler<MouseWheelEventArgs> handler = MouseWheel;
            if (handler != null) { handler(sender, e); }
            //e.Handled = true;
        }

        protected virtual void OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EventHandler<MouseButtonEventArgs> handler = MouseDoubleClick;
            if (handler != null) { handler(sender, e); }
            //e.Handled = true;
        }

        protected virtual void OnSingleClick(object sender, MouseButtonEventArgs e)
        {
            EventHandler<MouseButtonEventArgs> handler = MouseClick;
            if (handler != null) { handler(sender, e); }
            //e.Handled = true;
        }

        private void OnActualMouseButtonDown(MouseButtonEventArgs e)
        {
            EventHandler<MouseButtonEventArgs> handler = ActualMouseButtonDown;
            if (handler != null)
            {
                handler(this, e);
            }
            //e.Handled = true;
        }

        private void RaiseMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            EventHandler<MouseButtonEventArgs> handler = MouseButtonDown;
            if (handler != null)
            {
                handler(sender, e);
                element.CaptureMouse();
            }
        }

        private void RaiseSpecialMouseEvent(object sender, MouseButtonEventArgs args)
        {
            if (timerMouseDown.IsEnabled)
            {
                timerMouseDown.Stop();
                if (mouseDownCount == 1 && mouseUpCount == 0)
                {
                    RaiseMouseButtonDown(sender, args);
                }
                if (mouseDownCount == 1 && mouseUpCount == 1)
                {
                    OnSingleClick(sender, args);
                }
                else if (mouseDownCount > 1 && mouseUpCount > 1)
                {
                    OnDoubleClick(sender, args);
                }

                mouseUpCount = 0;
                mouseDownCount = 0;
            }
        }

        private void TimerMouseDown_Tick(object sender, EventArgs e)
        {
            RaiseSpecialMouseEvent(tempMouseDownSender, tempMouseDownEventArgs);
        }
    }
}