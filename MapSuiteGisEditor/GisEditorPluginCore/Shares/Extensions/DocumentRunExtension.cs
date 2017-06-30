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
using System.Windows;
using System.Windows.Documents;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DocumentRunExtension : DependencyObject
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string), typeof(DocumentRunExtension), new UIPropertyMetadata("Unknown", OnTextPropertyChanged));

        public static void SetText(DependencyObject sender, string scriptText)
        {
            sender.SetValue(TextProperty, scriptText);
        }

        public static string GetText(DependencyObject sender)
        {
            return (string)sender.GetValue(TextProperty);
        }

        public static void OnTextPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Run run = sender as Run;
            if (run != null)
            {
                run.Text = (string)e.NewValue;
            }
        }
    }
}