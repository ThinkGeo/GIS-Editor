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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class NameToImageUriConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = string.Empty;
            switch (parameter.ToString())
            {
                case "forward":
                    path = value == null ? "pack://application:,,,/GisEditorPluginCore;component/Images/grayForward.png" : "pack://application:,,,/GisEditorPluginCore;component/Images/forward.png"; break;
                case "back":
                    path = value == null ? "pack://application:,,,/GisEditorPluginCore;component/Images/grayBack.png" : "pack://application:,,,/GisEditorPluginCore;component/Images/back.png"; break;
                case "up":
                    path = value == null ? "pack://application:,,,/GisEditorPluginCore;component/Images/grayUp.png" : "pack://application:,,,/GisEditorPluginCore;component/Images/arrow_up.png"; break;
                default:
                    path = value == null ? "pack://application:,,,/GisEditorPluginCore;component/Images/grayDown.png" : "pack://application:,,,/GisEditorPluginCore;component/Images/arrow_down.png"; break;
            }
            return path;
        }
    }
}