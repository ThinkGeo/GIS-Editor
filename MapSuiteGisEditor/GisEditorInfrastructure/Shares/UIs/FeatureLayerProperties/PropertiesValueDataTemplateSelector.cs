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
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    internal class PropertiesValueDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            //dynamic pairedItem = item;

            var valuePropertyInfo = item.GetType().GetProperty("Value");
            if (valuePropertyInfo != null)
            {
                object itemValue = valuePropertyInfo.GetGetMethod().Invoke(item, null);
                if (itemValue is String)
                {
                    return (DataTemplate)((ContentPresenter)container).FindResource("StringValueTemplate");
                }
                else if (itemValue is IEnumerable<Encoding>)
                {
                    return (DataTemplate)((ContentPresenter)container).FindResource("EncodingValueTemplate");
                }
                else if (itemValue is IEnumerable<string>)
                {
                    return (DataTemplate)((ContentPresenter)container).FindResource("FeatureIDColumnTemplate");
                }
            }

            return null;
        }
    }
}