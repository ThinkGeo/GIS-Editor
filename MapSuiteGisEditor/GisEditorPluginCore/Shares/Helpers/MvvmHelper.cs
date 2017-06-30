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
using System.Windows;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class MvvmHelper
    {
        public static T GetDataContext<T>(this object sender) where T : class
        {
            var element = sender as FrameworkElement;
            if (element != null)
            {
                return element.DataContext as T;
            }
            return null;
        }

        public static bool GetHasError(UIElement element)
        {
            var result = false;
            foreach (UIElement child in LogicalTreeHelper.GetChildren(element).OfType<UIElement>())
            {
                if (child is TextBox)
                {
                    if ((result = Validation.GetHasError(child))) break;
                }
                else
                {
                    result = GetHasError(child);
                    if (result) break;
                }
            }

            return result;
        }
    }
}