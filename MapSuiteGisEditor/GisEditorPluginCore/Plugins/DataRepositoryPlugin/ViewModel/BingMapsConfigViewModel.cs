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
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.Core;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BingMapsConfigViewModel : ViewModelBase
    {
        private BingMapDataRepositoryItem bingMapDataRepositoryItem;
        private string bingMapsKey;
        private string clientSecret;
        private ThinkGeoCloudRasterMapsMapType mapType;

        [NonSerialized]
        private RelayCommand applyCommand;
        [NonSerialized]
        private RelayCommand cancelCommand;
        private bool showMapTypeOptions;

        public BingMapsConfigViewModel()
        {
            bingMapsKey = string.Empty;
            clientSecret = BaseMapsHelper.ThinkGeoCloudClientSecret;
            mapType = ThinkGeoCloudRasterMapsMapType.Light_V2_X1;
            showMapTypeOptions = true;
            var baseMapDataPlugin = GisEditor.DataRepositoryManager.GetPlugins().OfType<BaseMapDataRepositoryPlugin>().FirstOrDefault();
            if (baseMapDataPlugin != null &&
                (bingMapDataRepositoryItem = baseMapDataPlugin.RootDataRepositoryItem.Children.OfType<BingMapDataRepositoryItem>().FirstOrDefault()) != null)
            {
                bingMapsKey = bingMapDataRepositoryItem.BingMapsKey;
                clientSecret = bingMapDataRepositoryItem.ThinkGeoCloudClientSecret;
                mapType = bingMapDataRepositoryItem.ThinkGeoCloudMapType;
            }

            if (String.IsNullOrWhiteSpace(bingMapsKey))
            {
                bingMapsKey = BaseMapsHelper.ThinkGeoCloudClientId;
            }

            if (String.IsNullOrWhiteSpace(clientSecret))
            {
                clientSecret = BaseMapsHelper.ThinkGeoCloudClientSecret;
            }
        }

        public string BingMapsKey
        {
            get { return bingMapsKey; }
            set
            {
                bingMapsKey = value;
                RaisePropertyChanged(() => BingMapsKey);
            }
        }

        public string ClientSecret
        {
            get { return clientSecret; }
            set
            {
                clientSecret = value;
                RaisePropertyChanged(() => ClientSecret);
            }
        }

        public ThinkGeoCloudRasterMapsMapType MapType
        {
            get { return mapType; }
            set
            {
                mapType = value;
                RaisePropertyChanged(() => MapType);
            }
        }

        public RelayCommand ApplyCommand
        {
            get
            {
                if (applyCommand == null)
                {
                    applyCommand = new RelayCommand(() =>
                    {
                        if (Validate())
                        {
                            SaveBingMapsKey();
                            Messenger.Default.Send(true, this);
                        }
                        else
                        {
                            Messenger.Default.Send(new DialogMessage(GisEditor.LanguageManager.GetStringResource("DataRepositoryBingIDInvalidWarningLabel"), null) { Caption = GisEditor.LanguageManager.GetStringResource("WarningLabel"), Button = System.Windows.MessageBoxButton.OK, Icon = System.Windows.MessageBoxImage.Information });
                        }
                    });
                }
                return applyCommand;
            }
        }

        public RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(() =>
                    {
                        Messenger.Default.Send(false, this);
                    });
                }
                return cancelCommand;
            }
        }

        public bool ShowMapTypeOptions
        {
            get { return showMapTypeOptions; }
            set
            {
                showMapTypeOptions = value;
                RaisePropertyChanged(nameof(ShowMapTypeOptions));
            }
        }

        public bool Validate()
        {
            return !string.IsNullOrWhiteSpace(BingMapsKey) && !string.IsNullOrWhiteSpace(ClientSecret);
        }

        public void SaveBingMapsKey()
        {
            if (bingMapDataRepositoryItem != null)
            {
                bingMapDataRepositoryItem.BingMapsKey = BingMapsKey;
                bingMapDataRepositoryItem.ThinkGeoCloudClientSecret = ClientSecret;
                bingMapDataRepositoryItem.ThinkGeoCloudMapType = MapType;
            }
        }
    }
}
