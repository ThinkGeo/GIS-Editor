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


using System.Windows.Controls;
using ThinkGeo.MapSuite.Serialize;

namespace ThinkGeo.MapSuite.GisEditor
{
    //public class ExportAsCodeMenuItemViewModel : LayerListMenuItem
    internal partial class LayerListMenuItemHelper
    {
        public static MenuItem GetExportAsCodeMenuItem()
        {
            var command = new ObservedCommand(ExportAsCode, ExportAsCodeCanExecute);
            return GetMenuItem("Export as Code", "/GisEditorInfrastructure;component/Images/export_as_code.png", command);
        }

        private static void ExportAsCode()
        {
            if (GisEditor.LayerListManager.SelectedLayerListItem == null) return;
            GeoCodeGenerator codeGenerator = new GeoCodeGenerator();
            string code = codeGenerator.GenerateCode(GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject);

            ExportCodeWindow codeWindow = new ExportCodeWindow(code);
            codeWindow.ShowDialog();
        }

        private static bool ExportAsCodeCanExecute()
        {
            return !(GisEditor.LayerListManager.SelectedLayerListItems.Count > 0);
        }
    }
}