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


using System.Globalization;
using System.Windows.Controls;
using System;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class RowColumnValidation : ValidationRule
    {
        private int minValue;

        public RowColumnValidation()
        {
            minValue = int.MinValue;
        }

        public int MinValue
        {
            get { return minValue; }
            set { minValue = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int currentValue = Convert.ToInt32(value);
            if (currentValue < minValue)
            {
                return new ValidationResult(false, string.Format("The value must be larger than or equal {0}", minValue));
            }
            return new ValidationResult(true, string.Empty);
        }
    }
}
