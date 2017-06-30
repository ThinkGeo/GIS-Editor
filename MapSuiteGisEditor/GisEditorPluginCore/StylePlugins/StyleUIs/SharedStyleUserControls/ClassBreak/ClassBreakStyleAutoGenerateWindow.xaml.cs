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
using System.Reflection;
using System.Windows;
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for SimpleAreaStyleWindow.xaml
    /// </summary>
    public partial class ClassBreakStyleAutoGenerateWindow : Window
    {
        public ClassBreakStyleAutoGenerateWindow()
        {
            InitializeComponent();
        }

        public Collection<GeoSolidBrush> Brushes
        {
            get { return classBreakGenerator.ViewModel.CollectBrushes(); }
        }

        public double HighValue
        {
            get { return classBreakGenerator.ViewModel.HighValue; }
            set { classBreakGenerator.ViewModel.HighValue = value; }
        }

        public double LowValue
        {
            get { return classBreakGenerator.ViewModel.LowValue; }
            set { classBreakGenerator.ViewModel.LowValue = value; }
        }

        public ClassBreakViewModel ViewModel
        {
            get { return classBreakGenerator.ViewModel; }
        }

        [Obfuscation]
        private void GenerateClassesClick(object sender, RoutedEventArgs e)
        {
            if (classBreakGenerator.ViewModel.ClassesCount > 1)
            {
                string validationError = GetValidationResult();
                if (String.IsNullOrEmpty(validationError))
                {
                    var hasError = MvvmHelper.GetHasError(classBreakGenerator);
                    if (hasError)
                    {
                        System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ClassBreakStyleAutoGenerateWindowvalueinvalidMessage"));
                    }
                    else
                    {
                        DialogResult = true;
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(validationError, "Alert", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ClassBreakStyleAutoGenerateWindowlagerthanzeroMessage"), "Alert");
            }
        }

        private string GetValidationResult()
        {
            string error = string.Empty;
            if (double.IsNaN(LowValue) || double.IsNaN(HighValue))
            {
                error = GisEditor.LanguageManager.GetStringResource("ClassBreakStyleAutoGenerateWindownotNumberMessage");
            }
            else if (LowValue > HighValue || LowValue == HighValue)
            {
                error = GisEditor.LanguageManager.GetStringResource("ClassBreakStyleAutoGenerateWindowlargerMessage");
            }

            return error;
        }
    }
}