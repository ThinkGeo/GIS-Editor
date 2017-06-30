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
using System.Windows.Media;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public class QuickAccessToolbarItem
    {
        private ImageSource icon;
        private string controlName;
        private object quickAccessToolBarId;

        public QuickAccessToolbarItem()
            : this(null, null, string.Empty)
        { }

        public QuickAccessToolbarItem(object quickAccessToolBarId, ImageSource icon, string controlName)
        {
            this.quickAccessToolBarId = quickAccessToolBarId;
            Icon = icon;
            ControlName = controlName;
        }

        public object QuickAccessToolBarId
        {
            get { return quickAccessToolBarId; }
            set { quickAccessToolBarId = value; }
        }

        public ImageSource Icon
        {
            get { return icon; }
            set { icon = value; }
        }

        public string ControlName
        {
            get { return controlName; }
            set { controlName = value; }
        }
    }
}
