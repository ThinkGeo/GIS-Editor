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
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class AppMenuUIPlugin : UIPlugin
    {
        private static bool preserveScale;
        private static bool autoSelectPageSize;
        private static string selectedSignatureName;
        private static string allSignatures;

        private bool hotKeyRegistered;
        private string printedLayoutXml;
        private KeyBinding printHotKeyBinding;
        private WpfMap lastMap;

        public AppMenuUIPlugin()
        {
            Name = GisEditor.LanguageManager.GetStringResource("PrintUIPluginName");
            Description = GisEditor.LanguageManager.GetStringResource("AppMenuUIPluginDescription");
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/mapexport.png", UriKind.RelativeOrAbsolute));
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/mapexport.png", UriKind.RelativeOrAbsolute));
        }

        public string PrintedLayoutXml
        {
            get { return printedLayoutXml; }
            set { printedLayoutXml = value; }
        }

        public static bool AutoSelectPageSize
        {
            get { return autoSelectPageSize; }
            set { autoSelectPageSize = value; }
        }

        public static bool PreserveScale
        {
            get { return preserveScale; }
            set { preserveScale = value; }
        }

        public static string SelectedSignatureName
        {
            get { return selectedSignatureName; }
            set { selectedSignatureName = value; }
        }

        public static string AllSignatures
        {
            get { return allSignatures; }
            set { allSignatures = value; }
        }

        protected override void LoadCore()
        {
            base.LoadCore();
            ApplicationMenuItems.Add(new ExportMenuItem());
            ApplicationMenuItems.Add(new PrintMenuItem());

            printHotKeyBinding = GetPrintHotKeyBinding();
            if (Application.Current != null && Application.Current.MainWindow != null && !hotKeyRegistered)
            {
                Application.Current.MainWindow.InputBindings.Add(printHotKeyBinding);
                hotKeyRegistered = true;
            }
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            ApplicationMenuItems.Clear();
            if (Application.Current != null
                && Application.Current.MainWindow != null
                && hotKeyRegistered && printHotKeyBinding != null
                && Application.Current.MainWindow.InputBindings.Contains(printHotKeyBinding))
            {
                Application.Current.MainWindow.InputBindings.Remove(printHotKeyBinding);
                hotKeyRegistered = false;
                printHotKeyBinding = null;
            }
        }

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            if (lastMap != null && lastMap != currentMap)
            {
                var boundingBoxSelectTool = lastMap.MapTools.OfType<BoundingBoxSelectorMapTool>().FirstOrDefault();
                if (boundingBoxSelectTool != null)
                {
                    lastMap.MapTools.Remove(boundingBoxSelectTool);
                    currentMap.MapTools.Add(boundingBoxSelectTool);
                }
            }

            lastMap = currentMap;
        }

        protected override void DetachMapCore(GisEditorWpfMap wpfMap)
        {
            base.DetachMapCore(wpfMap);
            lastMap = null;
            PrintMapViewModel.CanUsePrint = true;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.ProjectSettings.ContainsKey("PrintedLayoutXml"))
                printedLayoutXml = settings.ProjectSettings["PrintedLayoutXml"];
            else printedLayoutXml = null;
            if (settings.GlobalSettings.ContainsKey("PreserveScale"))
            {
                PreserveScale = bool.Parse(settings.GlobalSettings["PreserveScale"]);
            }
            if (settings.GlobalSettings.ContainsKey("AutoSelectPageSize"))
            {
                AutoSelectPageSize = bool.Parse(settings.GlobalSettings["AutoSelectPageSize"]);
            }
            if (settings.GlobalSettings.ContainsKey("AllSignatures"))
            {
                allSignatures = settings.GlobalSettings["AllSignatures"];
            }
            if (settings.GlobalSettings.ContainsKey("SelectedSignatureName"))
            {
                selectedSignatureName = settings.GlobalSettings["SelectedSignatureName"];
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            var storableSettings = base.GetSettingsCore();
            storableSettings.ProjectSettings["PrintedLayoutXml"] = printedLayoutXml;
            storableSettings.GlobalSettings["PreserveScale"] = PreserveScale.ToString();
            storableSettings.GlobalSettings["AutoSelectPageSize"] = AutoSelectPageSize.ToString();
            storableSettings.GlobalSettings["AllSignatures"] = allSignatures;
            storableSettings.GlobalSettings["SelectedSignatureName"] = selectedSignatureName;
            return storableSettings;
        }

        private KeyBinding GetPrintHotKeyBinding()
        {
            if (printHotKeyBinding == null)
            {
                printHotKeyBinding = new KeyBinding(new ObservedCommand(() =>
                {
                    PrintMapWindow printMapWindow = new PrintMapWindow();
                    printMapWindow.ShowDialog();
                }, () => true), new KeyGesture(Key.P, ModifierKeys.Control));
            }

            return printHotKeyBinding;
        }
    }
}