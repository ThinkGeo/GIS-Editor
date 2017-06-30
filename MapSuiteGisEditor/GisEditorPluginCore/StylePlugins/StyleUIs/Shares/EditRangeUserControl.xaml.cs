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


using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for EditZoomLevelApplyRangeWindow.xaml
    /// </summary>
    public partial class EditRangeUserControl : UserControl
    {
        public EditRangeUserControl()
        {
            InitializeComponent();
            fromComboBox.ItemsSource = GetZoomLevelEntities();
            toComboBox.ItemsSource = GetZoomLevelEntities();
        }

        public int From
        {
            get { return fromComboBox.SelectedIndex + 1; }
            set { fromComboBox.SelectedIndex = value - 1; }
        }

        public int To
        {
            get { return toComboBox.SelectedIndex + 1; }
            set { toComboBox.SelectedIndex = value - 1; }
        }

        private Collection<string> GetZoomLevelEntities()
        {
            Collection<string> items = new Collection<string>();
            if (GisEditor.ActiveMap != null)
            {
                ZoomLevelSet zoomLevelSet = GisEditor.ActiveMap.ZoomLevelSet;
                for (int i = 0; i < zoomLevelSet.CustomZoomLevels.Count; i++)
                {
                    string zoomLevelTitle = string.Format(CultureInfo.InvariantCulture, "Level {0:D2}", i + 1);
                    string zoomLevelScale = string.Format(CultureInfo.InvariantCulture, "Scale 1:{0:N0}", (int)zoomLevelSet.CustomZoomLevels[i].Scale);
                    items.Add(zoomLevelTitle + " - " + zoomLevelScale);
                }
            }
            return items;
        }
    }
}