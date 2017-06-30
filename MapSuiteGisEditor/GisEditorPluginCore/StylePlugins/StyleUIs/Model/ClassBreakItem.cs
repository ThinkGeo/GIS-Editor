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
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ClassBreakItem : ViewModelBase
    {
        private bool isEditing;
        [NonSerialized]
        private Image image;
        private string startingValue;
        private string id;
        private ClassBreak classBreak;

        public ClassBreakItem()
        {
            Id = Guid.NewGuid().ToString();
        }

        public bool IsEditing
        {
            get { return isEditing; }
            set
            {
                isEditing = value;
                RaisePropertyChanged(()=>IsEditing);
            }
        }

        public Image Image
        {
            get { return image; }
            set
            {
                image = value;
                RaisePropertyChanged(()=>Image);
            }
        }

        public string StartingValue
        {
            get { return startingValue; }
            set
            {
                startingValue = value;
                RaisePropertyChanged(()=>StartingValue);
            }
        }

        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        public ClassBreak ClassBreak
        {
            get { return classBreak; }
            set { classBreak = value; }
        }
    }
}