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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class StylePreviewViewModel
    {
        private string name;
        private string styleFilePath;
        private BitmapSource preview;
        private StyleWrapper styleWrapper;

        public StylePreviewViewModel(string styleFilePath)
        {
            this.styleFilePath = styleFilePath;
            this.name = Path.GetFileNameWithoutExtension(styleFilePath);
            if (File.Exists(styleFilePath))
            {
                try
                {
                    XElement rootXml = XElement.Load(styleFilePath);
                    styleWrapper = new StyleWrapper(rootXml);
                    if (styleWrapper.Style != null)
                    {
                        preview = styleWrapper.Style.GetPreview(32, 32);
                    }
                    else
                    {
                        SetDefaultPreview();
                    }
                }
                catch (Exception e)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                    SetDefaultPreview();
                }
            }
            else
            {
                SetDefaultPreview();
            }
        }

        public StylePreviewViewModel(CompositeStyle style)
        {
            if (style != null)
            {
                styleWrapper = new StyleWrapper(0, 1000000000, style);
                preview = style.GetPreview(32, 32);
                name = style.Name;
            }
        }

        private void SetDefaultPreview()
        {
            preview = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/folder_large.png", UriKind.RelativeOrAbsolute));
        }

        public double LowerScale
        {
            get { return styleWrapper.LowerScale; }
        }

        public double UpperScale
        {
            get { return styleWrapper.UpperScale; }
        }

        public string StyleFilePath
        {
            get { return styleFilePath; }
        }

        public BitmapSource Preview
        {
            get { return preview; }
        }

        public string Name
        {
            get { return name; }
        }

        public string DisplayName
        {
            get
            {
                string displayName = name;
                if (GisEditor.ActiveMap != null && IsStyle)
                {
                    var fromZoomLevel = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.OrderBy(z => Math.Abs(z.Scale - UpperScale)).FirstOrDefault();
                    var toZoomLevel = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.OrderBy(z => Math.Abs(z.Scale - LowerScale)).FirstOrDefault();
                    var fromIndex = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.IndexOf(fromZoomLevel) + 1;
                    var toIndex = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.IndexOf(toZoomLevel) + 1;

                    if (fromIndex > 1 || toIndex < GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count)
                    {
                        displayName += string.Format(CultureInfo.InvariantCulture, "\r\n {0} - {1}", fromIndex, toIndex);
                    }
                }

                return displayName;
            }
        }

        public CompositeStyle Style
        {
            get { return styleWrapper.Style; }
        }

        public bool IsStyle { get { return File.Exists(StyleFilePath); } }
    }
}