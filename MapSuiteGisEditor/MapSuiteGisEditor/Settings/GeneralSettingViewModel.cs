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
using System.Globalization;
using System.Reflection;
using GalaSoft.MvvmLight;
using System.Windows;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    public class GeneralSettingViewModel : ViewModelBase
    {
        private GeneralManager generalOption;

        public GeneralSettingViewModel(GeneralManager generalOption)
        {
            this.generalOption = generalOption;
        }

        public MouseCoordinateType DisplayMouseCoordinate
        {
            get { return generalOption.MouseCoordinateType; }
            set
            {
                generalOption.MouseCoordinateType = value;
                RaisePropertyChanged(() => DisplayMouseCoordinate);
            }
        }

        public Theme Theme
        {
            get { return generalOption.Theme; }
            set
            {
                if (generalOption.Theme != value)
                {
                    generalOption.Theme = value;
                    RaisePropertyChanged(() => Theme);
                }
            }
        }

        public bool IsAutoSaveIntervalInMinutesEnabled
        {
            get { return AutoSave; }
        }

        public bool AutoSave
        {
            get { return generalOption.AutoSave; }
            set
            {
                if (generalOption.AutoSave != value)
                {
                    generalOption.AutoSave = value;

                    if (!generalOption.AutoSave) IsDisplayAutoSave = false;

                    RaisePropertyChanged(() => AutoSave);
                    RaisePropertyChanged(() => IsAutoSaveIntervalInMinutesEnabled);
                }
            }
        }

        public bool IsDisplayAutoSave
        {
            get { return generalOption.IsDisplayAutoSave; }
            set
            {
                if (generalOption.IsDisplayAutoSave != value)
                {
                    generalOption.IsDisplayAutoSave = value;
                    RaisePropertyChanged(() => IsDisplayAutoSave);
                }
            }
        }

        public int ThreadMinCount
        {
            get { return generalOption.ThreadMinCount; }
            set
            {
                int processorCount = Environment.ProcessorCount;
                if (value > processorCount)
                {
                    generalOption.ThreadMinCount = value;
                }
                else
                {
                    MessageBox.Show("You cannot set the number of threads to a number smaller than the number of processors in the computer.");
                }
                RaisePropertyChanged(() => ThreadMinCount);
            }
        }

        public int ThreadMaxCount
        {
            get { return generalOption.ThreadMaxCount; }
            set
            {
                if (value > ThreadMinCount)
                {
                    generalOption.ThreadMaxCount = value;
                }
                RaisePropertyChanged(() => ThreadMaxCount);
            }
        }

        public int AutoSaveIntervalInMinutes
        {
            get { return (int)generalOption.AutoSaveInterval.TotalMinutes; }
            set
            {
                if (generalOption.AutoSaveInterval.TotalMinutes != value)
                {
                    generalOption.AutoSaveInterval = TimeSpan.FromMinutes(value);
                    RaisePropertyChanged(() => AutoSaveIntervalInMinutes);
                }
            }
        }

        public Collection<CultureInfo> Languages
        {
            get { return GisEditor.LanguageManager.GetAvailableLanguages(); }
        }

        public CultureInfo DisplayLanguage
        {
            get { return generalOption.DisplayLanguage; }
            set
            {
                if (generalOption.DisplayLanguage != value)
                {
                    generalOption.DisplayLanguage = value;
                    RaisePropertyChanged(() => DisplayLanguage);
                }
            }
        }
    }
}