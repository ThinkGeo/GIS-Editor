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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Text;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BingMapsConfigViewModel : ViewModelBase
    {
        private static readonly string loginServiceTemplate = "http://dev.virtualearth.net/REST/v1/Imagery/Metadata/{0}?&incl=ImageryProviders&o=xml&key={1}";

        private BingMapDataRepositoryItem bingMapDataRepositoryItem;
        private string bingMapsKey;
        private BingMapsMapType mapType;

        [NonSerialized]
        private RelayCommand applyCommand;
        [NonSerialized]
        private RelayCommand cancelCommand;
        private bool showMapTypeOptions;

        public BingMapsConfigViewModel()
        {
            bingMapsKey = string.Empty;
            mapType = BingMapsMapType.Road;
            showMapTypeOptions = true;
            var baseMapDataPlugin = GisEditor.DataRepositoryManager.GetPlugins().OfType<BaseMapDataRepositoryPlugin>().FirstOrDefault();
            if (baseMapDataPlugin != null &&
                (bingMapDataRepositoryItem = baseMapDataPlugin.RootDataRepositoryItem.Children.OfType<BingMapDataRepositoryItem>().FirstOrDefault()) != null)
            {
                bingMapsKey = bingMapDataRepositoryItem.BingMapsKey;
                mapType = bingMapDataRepositoryItem.BingMapType;
                if (String.IsNullOrEmpty(bingMapsKey))
                {
                    byte[] keyBuffer = Convert.FromBase64String("QWk5SDVWVnQtZTI2VEdaRFgtakstVUVqOW5KeU9BdHF1OVAyalRURHdETXJTNS1CTUlzZVpxY01BVGpnSmtBeg==");
                    bingMapsKey = Encoding.UTF8.GetString(keyBuffer);
                }
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

        public BingMapsMapType MapType
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
            bool result = false;

            try
            {
                string loginServiceUri = String.Format(CultureInfo.InvariantCulture
                              , loginServiceTemplate, MapType, BingMapsKey);

                WebRequest request = HttpWebRequest.Create(loginServiceUri);
                WebResponse response = request.GetResponse();
                Stream stream = response.GetResponseStream();
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(stream);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xDoc.NameTable);
                nsmgr.AddNamespace("bing", "http://schemas.microsoft.com/search/local/ws/rest/v1");

                var root = xDoc.SelectSingleNode("bing:Response", nsmgr);
                var imageUrlElement = root.SelectSingleNode("bing:ResourceSets/bing:ResourceSet/bing:Resources/bing:ImageryMetadata/bing:ImageUrl", nsmgr);
                var subdomainsElement
                    = root.SelectNodes("bing:ResourceSets/bing:ResourceSet/bing:Resources/bing:ImageryMetadata/bing:ImageUrlSubdomains/bing:string", nsmgr);

                if (imageUrlElement != null && subdomainsElement != null)
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
            }

            return result;
        }

        public void SaveBingMapsKey()
        {
            if (bingMapDataRepositoryItem != null)
            {
                bingMapDataRepositoryItem.BingMapsKey = BingMapsKey;
                bingMapDataRepositoryItem.BingMapType = MapType;
            }
        }
    }
}