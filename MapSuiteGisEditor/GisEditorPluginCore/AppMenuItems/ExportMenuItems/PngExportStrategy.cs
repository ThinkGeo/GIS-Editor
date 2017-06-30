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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class PngExportStrategy : ExportStrategy
    {
        public PngExportStrategy()
        { }

        protected override void ExportCore(GisEditorWpfMap map)
        {
            Bitmap bitmap = map.GetBitmap((int)map.ActualWidth, (int)map.ActualHeight, MapResizeMode.PreserveScaleAndCenter);

            SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "PNG files|*.png|PNG with PGW files|*.png" };
            if (saveFileDialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                bitmap.Save(saveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                if (saveFileDialog.FilterIndex == 2)
                {
                    string fileName = Path.ChangeExtension(saveFileDialog.FileName, ".pgw");
                    Collection<string> contents = new Collection<string>();
                    WorldFile worldFile = new WorldFile(map.CurrentExtent, (float)map.ActualWidth, (float)map.ActualHeight);
                    contents.Add(worldFile.HorizontalResolution.ToString(CultureInfo.InvariantCulture));
                    contents.Add(worldFile.RotationRow.ToString(CultureInfo.InvariantCulture));
                    contents.Add(worldFile.RotationColumn.ToString(CultureInfo.InvariantCulture));
                    contents.Add(worldFile.VerticalResolution.ToString(CultureInfo.InvariantCulture));
                    contents.Add(worldFile.UpperLeftX.ToString(CultureInfo.InvariantCulture));
                    contents.Add(worldFile.UpperLeftY.ToString(CultureInfo.InvariantCulture));
                    File.WriteAllLines(fileName, contents);
                }
            }
        }
    }
}