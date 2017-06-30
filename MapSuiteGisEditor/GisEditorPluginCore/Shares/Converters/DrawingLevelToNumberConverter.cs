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
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DrawingLevelToNumberConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((DrawingLevel)value)
            {
                case DrawingLevel.LevelOne:
                    return 1;
                case DrawingLevel.LevelTwo:
                    return 2;
                case DrawingLevel.LevelThree:
                    return 3;
                case DrawingLevel.LevelFour:
                    return 4;
                default:
                case DrawingLevel.LabelLevel:
                    return "Label";
            }
        }
    }
}
