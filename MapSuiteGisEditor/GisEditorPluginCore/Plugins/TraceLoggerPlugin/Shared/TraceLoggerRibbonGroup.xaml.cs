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


using System.Reflection;
using System.Windows;
using Microsoft.Windows.Controls.Ribbon;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class TraceLoggerRibbonGroup : RibbonGroup
    {
        private DebugOutputUserControl debugOutputUserControl;

        public TraceLoggerRibbonGroup()
        {
            InitializeComponent();
            debugOutputUserControl = new DebugOutputUserControl();
        }

        [Obfuscation]
        private void ShowOutput_Click(object sender, RoutedEventArgs e)
        {
            DockWindow outputWindow = new DockWindow();
            outputWindow.Title = GisEditor.LanguageManager.GetStringResource("TraceLoggerRibbonGroupOutputTitle");
            outputWindow.Name = GisEditor.LanguageManager.GetStringResource("TraceLoggerRibbonGroupOutputName");
            outputWindow.Content = debugOutputUserControl;
            outputWindow.Position = DockWindowPosition.Bottom;
            outputWindow.StartupMode = DockWindowStartupMode.Hide;
            outputWindow.Show(DockWindowPosition.Bottom);
        }
    }
}