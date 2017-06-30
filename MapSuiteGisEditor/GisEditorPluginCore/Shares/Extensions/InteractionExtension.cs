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


using System.Windows;
using System.Windows.Interactivity;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public static class InteractionExtension
    {
        public static readonly DependencyProperty TriggersProperty =
            DependencyProperty.RegisterAttached("Triggers", typeof(GisEditorTriggers), typeof(InteractionExtension), new UIPropertyMetadata(null, OnPropertyTriggersChanged));

        public static GisEditorTriggers GetTriggers(DependencyObject obj)
        {
            return (GisEditorTriggers)obj.GetValue(TriggersProperty);
        }

        public static void SetTriggers(DependencyObject obj, GisEditorTriggers value)
        {
            obj.SetValue(TriggersProperty, value);
        }

        private static void OnPropertyTriggersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var triggers = Interaction.GetTriggers(d);
            foreach (var trigger in e.NewValue as GisEditorTriggers)
            {
                triggers.Add(trigger);
            }
        }
    }
}