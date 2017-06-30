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
using AvalonDock;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public class DockableContentInfo
    {
        private DockWindow dockWindow;
        private DockWindowStartupMode dockWindowStartupMode;

        public DockableContentInfo()
            : this(null)
        { }

        public DockableContentInfo(DockWindow dockWindow)
            : this(dockWindow, DockWindowStartupMode.Default)
        { }

        public DockableContentInfo(DockWindow dockableContent, DockWindowStartupMode dockWindowStartupMode)
        {
            this.DockWindow = dockableContent;
            this.StartupMode = dockWindowStartupMode;
        }

        public DockWindowStartupMode StartupMode
        {
            get { return dockWindowStartupMode; }
            set { dockWindowStartupMode = value; }
        }

        public DockWindow DockWindow
        {
            get { return dockWindow; }
            set { dockWindow = value; }
        }

        public static DockableContent ToDockableContent(DockWindow dockWindow)
        {
            DockableContent dockableContent = null;

            if (dockWindow != null && dockWindow.Content != null && dockWindow.Content.Parent is DockableContent)
            {
                dockableContent = dockWindow.Content.Parent as DockableContent;
            }
            else
            {
                dockableContent = new DockableContent();
            }
            dockableContent.Name = dockWindow.Name;
            SetDockableContentTitle(dockableContent, dockWindow.Title);
            dockableContent.Content = dockWindow.Content;
            dockableContent.DockPosition = ConvertToDockPosition(dockWindow.Position);
            return dockableContent;
        }

        private static void SetDockableContentTitle(DockableContent item, string titleKey)
        {
            string title = GisEditor.LanguageManager.GetStringResource(titleKey);
            if (!string.IsNullOrEmpty(title))
            {
                item.SetResourceReference(DockableContent.TitleProperty, titleKey);
            }
            else
            {
                item.Title = titleKey;
            }
        }

        private static DockPosition ConvertToDockPosition(DockWindowPosition dockWindowPosition)
        {
            switch (dockWindowPosition)
            {
                case DockWindowPosition.Left:
                    return DockPosition.Left;
                case DockWindowPosition.Bottom:
                    return DockPosition.Bottom;
                case DockWindowPosition.Default:
                case DockWindowPosition.Right:
                case DockWindowPosition.Floating:
                default:
                    return DockPosition.Right;
            }
        }
    }
}