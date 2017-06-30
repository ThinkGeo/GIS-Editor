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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class NonTopMostPopup : Popup
    {
        [DllImport("user32", EntryPoint = "SetWindowPos")]
        private static extern int SetWindowPos(IntPtr hwnd, int hwndInsertAfter, int x, int y, int cx, int cy, int wFlags);

        [NonSerialized]
        private FrameworkElement rootElement;
        internal static bool IsOpenedByDialog;
        private int offsetX;
        private int offsetY;

        public int OffsetX
        {
            get { return offsetX; }
            set { offsetX = value; }
        }

        public int OffsetY
        {
            get { return offsetY; }
            set { offsetY = value; }
        }

        protected override void OnOpened(EventArgs e)
        {
            rootElement = FindRoot(this);
            if (rootElement != null)
            {
                if (rootElement is TabItem)
                {
                    rootElement = (FrameworkElement)((TabItem)rootElement).Content;
                }

                rootElement.MouseUp += new System.Windows.Input.MouseButtonEventHandler(rootElement_MouseUp);
                if (rootElement is Window)
                {
                    Window rootWindow = (Window)rootElement;
                    rootWindow.LocationChanged -= new EventHandler(NonTopMostPopup_LocationChanged);
                    rootWindow.LocationChanged += new EventHandler(NonTopMostPopup_LocationChanged);
                }

                UIElement placementTarget = PlacementTarget;
                Point newPoint = placementTarget.PointToScreen(new Point(0, 0));
                IntPtr hwnd = ((HwndSource)PresentationSource.FromVisual(this.Child)).Handle;
                SetWindowPos(hwnd, -2, (int)newPoint.X - 10 + OffsetX, (int)newPoint.Y + 11 + OffsetY, (int)this.Width, (int)this.Height, 0);
            }
        }

        private void NonTopMostPopup_LocationChanged(object sender, EventArgs e)
        {
            IntPtr hwnd = ((HwndSource)PresentationSource.FromVisual(this.Child)).Handle;
            UIElement placementTarget = PlacementTarget;
            Point newPoint = placementTarget.PointToScreen(new Point(0, 0));
            SetWindowPos(hwnd, -2, (int)newPoint.X - 10 + OffsetX, (int)newPoint.Y + 11 + OffsetY, (int)this.Width, (int)this.Height, 0);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (rootElement != null)
            {
                rootElement.MouseUp -= new System.Windows.Input.MouseButtonEventHandler(rootElement_MouseUp);
                if (rootElement is Window)
                {
                    ((Window)rootElement).LocationChanged -= new EventHandler(NonTopMostPopup_LocationChanged);
                }
            }
        }

        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            e.Handled = true;
        }

        private void rootElement_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsOpenedByDialog)
            {
                IsOpen = false;
                e.Handled = true;
            }

            IsOpenedByDialog = false;
        }

        private static FrameworkElement FindRoot(FrameworkElement currentElement)
        {
            return Window.GetWindow(currentElement);
        }
    }
}