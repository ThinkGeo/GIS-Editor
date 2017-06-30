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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DissolveOperatorPairToStringConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is IEnumerable<OperatorPair>) return ConvertTo((IEnumerable<OperatorPair>)value);
            else if (value is string) return ConvertFrom((string)value);
            else return Binding.DoNothing;
        }

        public static string ConvertTo(IEnumerable<OperatorPair> operatorPairs)
        {
            StringBuilder result = new StringBuilder();
            foreach (var operatorPair in operatorPairs)
            {
                result.Append(String.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}||", operatorPair.Operator, operatorPair.ColumnName, operatorPair.ColumnType));
            }

            return result.ToString().TrimEnd('|');
        }

        public static List<OperatorPair> ConvertFrom(string operatorPairsString)
        {
            return operatorPairsString.Split(new string[] { "||" }, StringSplitOptions.None).Select(line =>
            {
                var items = line.Split('|');
                try { return new OperatorPair(items[1], items[2], (DissolveOperatorMode)Enum.Parse(typeof(DissolveOperatorMode), items[0])); }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    return null;
                }
            }).Where(op => op != null).ToList();
        }
    }
}