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


using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class AdornmentRibbonGroupViewModel : ViewModelBase
    {
        private ObservedCommand showGraticules;
        private ObservedCommand showManageLegends;
        private ObservedCommand showTitle;
        private ObservedCommand showNorthArrow;
        private ObservedCommand showLogo;
        private ObservedCommand showScaleBars;
        private bool isGraticulesVisible;

        public AdornmentRibbonGroupViewModel()
        {
        }

        public ObservedCommand ShowGraticules
        {
            get
            {
                if (showGraticules == null)
                {
                    showGraticules = new ObservedCommand(() =>
                    {
                        MessengerInstance.Send("Graticules", this);
                    }, () => CheckActiveMapIsNotNull());
                }
                return showGraticules;
            }
        }

        public ObservedCommand ShowScaleBars
        {
            get
            {
                if (showScaleBars == null)
                {
                    showScaleBars = new ObservedCommand(() =>
                    {
                        MessengerInstance.Send("Scale", this);
                    }, () => CheckActiveMapIsNotNull());
                }
                return showScaleBars;
            }
        }

        public ObservedCommand ShowLogo
        {
            get
            {
                if (showLogo == null)
                {
                    showLogo = new ObservedCommand(() =>
                    {
                        MessengerInstance.Send("Logo", this);

                    }, () => CheckActiveMapIsNotNull());
                }
                return showLogo;
            }
        }

        public ObservedCommand ShowNorthArrow
        {
            get
            {
                if (showNorthArrow == null)
                {
                    showNorthArrow = new ObservedCommand(() =>
                    {
                        MessengerInstance.Send("NorthArrow", this);

                    }, () => CheckActiveMapIsNotNull());
                }
                return showNorthArrow;
            }
        }

        public ObservedCommand ShowTitle
        {
            get
            {
                if (showTitle == null)
                {
                    showTitle = new ObservedCommand(() =>
                    {
                        MessengerInstance.Send("Title", this);

                    }, () => CheckActiveMapIsNotNull());
                }
                return showTitle;
            }
        }

        public ObservedCommand ShowManageLegends
        {
            get
            {
                if (showManageLegends == null)
                {
                    showManageLegends = new ObservedCommand(() =>
                    {
                        MessengerInstance.Send("ManageLegends", this);

                    }, () => CheckActiveMapIsNotNull());
                }
                return showManageLegends;
            }
        }

        public bool IsGraticulesVisible
        {
            get { return isGraticulesVisible; }
            set
            {
                isGraticulesVisible = value;
                RaisePropertyChanged(()=>IsGraticulesVisible);
            }
        }

        private bool CheckActiveMapIsNotNull()
        {
            return GisEditor.ActiveMap != null;
        }
    }
}
