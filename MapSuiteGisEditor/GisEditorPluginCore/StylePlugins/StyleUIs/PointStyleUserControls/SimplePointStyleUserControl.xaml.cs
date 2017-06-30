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
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for PointStyleUserControl.xaml
    /// </summary>
    public partial class SimplePointStyleUserControl : StyleUserControl
    {
        public static readonly string SimplePointStyleTypeName = "Simple Point Style";
        public static readonly string WellPointStyleTypeName = "Well Point Style";
        public static readonly string CustomPointStyleTypeName = "Custom Point Style";

        public SimplePointStyleUserControl(PointStyle style)
        {
            InitializeComponent();

            PointStyleViewModel pointStyleViewModel = new PointStyleViewModel(style);
            DataContext = pointStyleViewModel;

            string helpUri = GisEditor.LanguageManager.GetStringResource("PointStyleHelp");
            if (!string.IsNullOrEmpty(helpUri))
            {
                HelpUri = new Uri(helpUri);
            }
        }
    }
}