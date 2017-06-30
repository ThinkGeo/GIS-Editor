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


using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor
{
    public class RenameMapViewModel : ViewModelBase
    {
        private string name;

        public RenameMapViewModel(string originalName)
        {
            name = originalName;
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged(()=>Name);
            }
        }
    }

    public class MapNameValidation : ValidationRule
    {
        private static readonly string mapNamePattern = @"^[a-zA-Z_]\w*$";

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            string newName = value.ToString();
            string warningText = string.Empty;
            if (!Regex.IsMatch(newName, mapNamePattern))
            {
                warningText = GisEditor.LanguageManager.GetStringResource("RenameWindowDscMapnamescanDscText");
            }
            else if (newName[0] >= '0' && newName[0] <= '9')
            {
                warningText = GisEditor.LanguageManager.GetStringResource("MapNameStartNumberWarning");
            }
            else if (newName.Contains("__"))
            {
                warningText = GisEditor.LanguageManager.GetStringResource("MapNameContainUnderscoreText");
            }
            else if (GisEditor.GetMaps().Select(m => m.Name).Except(RenameMapWindow.OriginalName).ToList().Contains(newName))
            {
                warningText = GisEditor.LanguageManager.GetStringResource("MapNameExistChosseDifferentNameText");
            }
            if (string.IsNullOrEmpty(warningText))
            {
                return ValidationResult.ValidResult;
            }
            return new ValidationResult(false, warningText);
        }
    }
}
