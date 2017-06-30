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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BatchTaskToImageSourceConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                BatchTaskState state = (BatchTaskState)value;
                BitmapImage imageSource = null;
                switch (state)
                {
                    case BatchTaskState.Running:
                        //return "Running...";
                        imageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/circle_double.png", UriKind.RelativeOrAbsolute));
                        break;
                    case BatchTaskState.Finished:
                        //return "Finished";
                        //imageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/right.png", UriKind.RelativeOrAbsolute));
                        return "Done";
                    case BatchTaskState.Pending:
                    default:
                        //return "Remove";
                        imageSource = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/delete.png", UriKind.RelativeOrAbsolute));
                        break;
                }

                return new Image { Source = imageSource, Stretch = Stretch.None };
            }
            catch(Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                return Binding.DoNothing;
            }
        }
    }
}