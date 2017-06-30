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
using ICSharpCode.AvalonEdit;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TextEditorExtension : DependencyObject
    {
        public static readonly DependencyProperty ScriptTextProperty =
            DependencyProperty.RegisterAttached("ScriptText", typeof(string), typeof(TextEditorExtension), new UIPropertyMetadata(String.Empty, OnScriptTextPropertyChanged));

        public static void SetScriptText(DependencyObject sender, string scriptText)
        {
            sender.SetValue(ScriptTextProperty, scriptText);
        }

        public static string GetScriptText(DependencyObject sender)
        {
            return (string)sender.GetValue(ScriptTextProperty);
        }

        public static void OnScriptTextPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor textEditor = sender as TextEditor;
            if (textEditor != null && e.NewValue is String)
            {
                var newValue = (string)e.NewValue;
                if (textEditor.Text != newValue)
                {
                    textEditor.Text = newValue;
                }
            }
        }
    }
}