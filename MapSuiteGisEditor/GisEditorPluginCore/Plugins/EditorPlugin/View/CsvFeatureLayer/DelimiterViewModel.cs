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
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class DelimiterViewModel : ViewModelBase
    {
        private bool isDelimiterEnabled;

        private bool isCustomDelimiterEnabled;
        private string customDelimiter;

        private string delimiter;
        private KeyValuePair<string, string> selectedDelimiter;

        public DelimiterViewModel(WellKnownType wellKnownType)
        {
            IsDelimiterEnabled = true;
            if (wellKnownType == WellKnownType.Multipoint || wellKnownType == WellKnownType.Point)
            {
                SelectedDelimiter = new KeyValuePair<string, string>("Comma", ",");
            }
            else
            {
                SelectedDelimiter = new KeyValuePair<string, string>("Tab", "\t");
            }
        }

        public bool IsDelimiterEnabled
        {
            get { return isDelimiterEnabled; }
            set
            {
                isDelimiterEnabled = value;
                RaisePropertyChanged(()=>IsDelimiterEnabled);
            }
        }

        public string Delimiter
        {
            get { return delimiter; }
            set { delimiter = value; }
        }

        public KeyValuePair<string, string> SelectedDelimiter
        {
            get { return selectedDelimiter; }
            set
            {
                selectedDelimiter = value;
                IsCustomDelimiterEnabled = value.Key.Equals("Custom", StringComparison.InvariantCulture);
                if (IsCustomDelimiterEnabled && !string.IsNullOrEmpty(CustomDelimiter))
                {
                    Delimiter = CustomDelimiter;
                }
                else
                {
                    Delimiter = value.Value;
                }
                RaisePropertyChanged(()=>SelectedDelimiter);
                RaisePropertyChanged(()=>IsCustomDelimiterEnabled);
            }
        }

        public string CustomDelimiter
        {
            get { return customDelimiter; }
            set
            {
                customDelimiter = value;
                Delimiter = value;
                RaisePropertyChanged(()=>CustomDelimiter);
            }
        }

        public bool IsCustomDelimiterEnabled
        {
            get { return isCustomDelimiterEnabled; }
            set { isCustomDelimiterEnabled = value; }
        }
    }
}
