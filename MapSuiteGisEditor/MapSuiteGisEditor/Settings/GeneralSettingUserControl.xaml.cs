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


using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using ThinkGeo.Core;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for GeneralOption.xaml
    /// </summary>
    [Obfuscation]
    public partial class GeneralSettingUserControl : UserControl
    {
        public GeneralSettingUserControl()
        {
            InitializeComponent();
            Loaded += new System.Windows.RoutedEventHandler(GeneralSettingUserControl_Loaded);
        }

        private void GeneralSettingUserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Application.Current != null)
            {
                var tmpSettings = Application.Current.MainWindow.Tag as Dictionary<string, string>;
                if (tmpSettings != null)
                    ResetButton.IsEnabled = tmpSettings["ShowHintSettings"].Contains("false") || tmpSettings["ShowHintSettings"].Contains("False");
            }
        }

        private void ResetTipClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Application.Current != null && Application.Current.MainWindow.Tag is Dictionary<string, string>)
            {
                var tmpSettings = Application.Current.MainWindow.Tag as Dictionary<string, string>;
                var showHintsXml = XDocument.Parse(tmpSettings["ShowHintSettings"]);
                foreach (var item in showHintsXml.Root.Elements())
                {
                    item.Value = "true";
                }
                tmpSettings["ShowHintSettings"] = showHintsXml.ToString();
                ResetButton.IsEnabled = false;
            }
        }

        private void UseCurrentExtentClick(object sender, RoutedEventArgs e)
        {
            if (Application.Current != null && Application.Current.MainWindow.Tag is Dictionary<string, string>)
            {
                var tmpSettings = Application.Current.MainWindow.Tag as Dictionary<string, string>;
                RectangleShape currentExtent = GisEditor.ActiveMap.CurrentExtent;
                ProjectionConverter projection = new ProjectionConverter(GisEditor.ActiveMap.DisplayProjectionParameters, Projection.GetWgs84ProjString());
                //projection.Open();
                //RectangleShape extent = projection.ConvertToExternalProjection(currentExtent);
                //projection.Close();

                RectangleShape extent = currentExtent;

                //projection.SyncProjectionParametersString();
                //if (projection.CanProject())
                {
                    try
                    {
                        projection.Open();
                        extent = projection.ConvertToExternalProjection(extent);
                    }
                    finally
                    {
                        projection.Close();
                    }
                }

                tmpSettings["CurrentExtent"] = extent.ToString();
            }
        }
    }
}
