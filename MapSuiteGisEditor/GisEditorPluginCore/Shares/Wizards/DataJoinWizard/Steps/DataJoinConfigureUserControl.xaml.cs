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


using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for DataJoinConfigureUserControl.xaml
    /// </summary>
    public partial class DataJoinConfigureUserControl : UserControl
    {
        public DataJoinConfigureUserControl()
        {
            InitializeComponent();
        }

        [Obfuscation]
        private void Left2Right_Click(object sender, RoutedEventArgs e)
        {
            var entity = this.DataContext as DataJoinWizardShareObject;
            if (entity != null)
            {
                var selectedItems = LeftList.SelectedItems.Cast<FeatureSourceColumn>().ToArray();
                foreach (var item in selectedItems)
                {
                    if (!entity.IncludedColumnsList.Contains(item))
                    {
                        entity.SourceColumnsList.Remove(item);
                        entity.IncludedColumnsList.Add(item);
                    }
                }
            }
        }

        [Obfuscation]
        private void Right2Left_Click(object sender, RoutedEventArgs e)
        {
            var entity = this.DataContext as DataJoinWizardShareObject;
            if (entity != null)
            {
                var selectedItems = RightList.SelectedItems.Cast<FeatureSourceColumn>().ToArray();
                foreach (var item in selectedItems)
                {
                    if (!entity.SourceColumnsList.Contains(item))
                    {
                        entity.IncludedColumnsList.Remove(item);
                        entity.SourceColumnsList.Add(item);
                    }
                }
            }
        }

        [Obfuscation]
        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            var entity = this.DataContext as DataJoinWizardShareObject;
            if (entity.SelectedIncludeColumnItem != null && entity.SelectedIncludeColumnItem != entity.IncludedColumnsList.FirstOrDefault())
            {
                int index = entity.IncludedColumnsList.IndexOf(entity.SelectedIncludeColumnItem) - 1;
                var tmpItem = entity.SelectedIncludeColumnItem;
                entity.IncludedColumnsList.Remove(entity.SelectedIncludeColumnItem);
                entity.IncludedColumnsList.Insert(index, tmpItem);
                RightList.SelectedIndex = index;
            }
        }

        [Obfuscation]
        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            var entity = this.DataContext as DataJoinWizardShareObject;
            if (entity.SelectedIncludeColumnItem != null && entity.SelectedIncludeColumnItem != entity.IncludedColumnsList.LastOrDefault())
            {
                int index = entity.IncludedColumnsList.IndexOf(entity.SelectedIncludeColumnItem) + 1;
                var tmpItem = entity.SelectedIncludeColumnItem;
                entity.IncludedColumnsList.Remove(entity.SelectedIncludeColumnItem);
                entity.IncludedColumnsList.Insert(index, tmpItem);
                RightList.SelectedIndex = index;
            }
        }
    }
}
