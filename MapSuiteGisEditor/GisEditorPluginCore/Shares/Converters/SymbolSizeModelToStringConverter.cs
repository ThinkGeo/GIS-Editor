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
    public class SymbolSizeModelToStringConverter : ValueConverter
    {
        private const string matchLargestInfo = "Resize all symbols to match the largest one";
        private const string matchSmallestInfo = "Resize all symbols to match the smallest one";
        private const string fixedInfo = "Use fixed width and height for all icons";
        private const string noneInfo = "None (Display each symbol at its original size)";

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SymbolSizeMode)
            {
                switch ((SymbolSizeMode)value)
                {
                    case SymbolSizeMode.MatchLargest:
                        return matchLargestInfo;
                    case SymbolSizeMode.MatchSmallest:
                        return matchSmallestInfo;
                    case SymbolSizeMode.Fixed:
                        return fixedInfo;
                    case SymbolSizeMode.None:
                    default:
                        return noneInfo;
                }
            }
            else
            {
                switch (value.ToString())
                {
                    case matchLargestInfo:
                        return SymbolSizeMode.MatchLargest;
                    case matchSmallestInfo:
                        return SymbolSizeMode.MatchSmallest;
                    case fixedInfo:
                        return SymbolSizeMode.Fixed;
                    case noneInfo:
                    default:
                        return SymbolSizeMode.None;
                }
            }
        }
    }
}