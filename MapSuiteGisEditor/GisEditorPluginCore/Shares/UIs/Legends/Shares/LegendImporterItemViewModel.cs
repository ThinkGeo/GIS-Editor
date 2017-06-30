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
using System.Windows.Media;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class LegendImporterItemViewModel : ViewModelBase
    {
        private string text;
        [NonSerialized]
        private ImageSource imageSource;
        private int level;
        private Visibility checkBoxVisibility = Visibility.Collapsed;
        private int leftPaddingLevel;
        private Styles.Style style;
        private bool allowToAdd;

        public LegendImporterItemViewModel()
        { }

        public string Text
        {
            get
            {
                return text;
            }

            set
            {
                if (text == value)
                {
                    return;
                }

                text = value.Substring(value.LastIndexOf('#') + 1);
                RaisePropertyChanged(()=>Text);
            }
        }

        public ImageSource IconSource
        {
            get
            {
                return imageSource;
            }

            set
            {
                if (imageSource == value)
                {
                    return;
                }

                imageSource = value;
                RaisePropertyChanged(()=>IconSource);
            }
        }

        public int Level
        {
            get
            {
                return level;
            }

            set
            {
                if (level == value)
                {
                    return;
                }

                level = value;
                RaisePropertyChanged(()=>Level);
            }
        }

        public Visibility CheckBoxVisibility
        {
            get
            {
                return checkBoxVisibility;
            }

            set
            {
                if (checkBoxVisibility == value)
                {
                    return;
                }

                checkBoxVisibility = value;
                RaisePropertyChanged(()=>CheckBoxVisibility);
            }
        }

        public int LeftPaddingLevel
        {
            get
            {
                return leftPaddingLevel;
            }

            set
            {
                if (leftPaddingLevel == value)
                {
                    return;
                }

                leftPaddingLevel = value;
                RaisePropertyChanged(()=>LeftPaddingLevel);
            }
        }

        public Styles.Style Style
        {
            get
            {
                return style;
            }

            set
            {
                if (style == value)
                {
                    return;
                }

                style = value;
                RaisePropertyChanged(()=>Style);
            }
        }

        public bool AllowToAdd
        {
            get
            {
                return allowToAdd;
            }

            set
            {
                if (allowToAdd == value)
                {
                    return;
                }

                allowToAdd = value;
                RaisePropertyChanged(()=>AllowToAdd);
            }
        }
    }
}
