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
using System.Globalization;
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DashPatterToStringConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Collection<Single> numbers = (Collection<Single>)value;
            string result = string.Empty;
            foreach (var number in numbers)
            {
                result += number;
                result += ",";
            }
            return new String(result.Take(result.Length - 1).ToArray());
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = (string)value;
            string[] numbers = text.Split(',');
            return new Collection<Single>(numbers.Where(number =>
            {
                Single single = 0;
                return Single.TryParse(number, out single);
            }).Select(number => Single.Parse(number)).ToArray());
        }
    }
}