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
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BuildLargeIndexSyncWindowViewModel : ViewModelBase
    {
        [NonSerialized]
        private RelayCommand<int> confirmBuildIndexModeCommand;

        private string fileName;

        private BuildLargeIndexMode buildLargeIndexMode;

        public BuildLargeIndexMode BuildLargeIndexMode
        {
            get { return buildLargeIndexMode; }
            set { buildLargeIndexMode = value; }
        }

        public string FileName
        {
            get { return fileName; }
            set
            {
                fileName = value;
                RaisePropertyChanged(()=>FileName);
            }
        }

        public RelayCommand<int> ConfirmBuildIndexModeCommand
        {
            get
            {
                if (confirmBuildIndexModeCommand == null)
                {
                    confirmBuildIndexModeCommand = new RelayCommand<int>(tag =>
                    {
                        switch (tag)
                        {
                            case 1:
                                BuildLargeIndexMode = BuildLargeIndexMode.BackgroundBuild;
                                break;

                            case 2:
                                BuildLargeIndexMode = BuildLargeIndexMode.NormalBuild;
                                break;

                            case 3:
                                BuildLargeIndexMode = BuildLargeIndexMode.DoNotBuild;
                                break;

                            case 4:
                                BuildLargeIndexMode = BuildLargeIndexMode.DoNotAdd;
                                break;
                        }

                        MessengerInstance.Send(true, this);
                    });
                }
                return confirmBuildIndexModeCommand;
            }
        }
    }
}