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
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class GoogleEarthUIPlugin : UIPlugin
    {
        private RibbonEntry ribbonEntry;
        private GoogleEarthRibbonGroup googleEarthRibbonGroup;

        public GoogleEarthUIPlugin()
        {
            Index = UIPluginOrder.GoogleEarthUIPlugin;
            Name = "Google earth plugin";
            Description = "Google earth plugin";
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/reprojection_32x32.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/reprojection_32x32.png", UriKind.RelativeOrAbsolute));

            googleEarthRibbonGroup = new GoogleEarthRibbonGroup();
            ribbonEntry = new RibbonEntry(googleEarthRibbonGroup, RibbonTabOrder.Tools, "ToolsRibbonTabHeader");
        }

        protected override void LoadCore()
        {
            base.LoadCore();

            if (!RibbonEntries.Contains(ribbonEntry))
            {
                RibbonEntries.Add(ribbonEntry);
            }
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            RibbonEntries.Clear();
        }

        protected override void RefreshCore(WpfDesktop.Extension.GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            base.RefreshCore(currentMap, refreshArgs);

            GoogleEarthRibbonGroupViewModel viewModel = googleEarthRibbonGroup.DataContext as GoogleEarthRibbonGroupViewModel;
            if (viewModel != null)
            {
                viewModel.RaisePropertyChanged();
            }
        }

        protected override Collection<MenuItem> GetMapContextMenuItemsCore(GetMapContextMenuParameters parameters)
        {
            return new Collection<MenuItem> { GoogleEarthHelper.GetSaveAsKMLMenuItem() };
        }
    }
}