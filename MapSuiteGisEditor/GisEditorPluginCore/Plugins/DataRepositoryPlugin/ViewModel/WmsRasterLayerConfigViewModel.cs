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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Serialize;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    [NonSerializableBaseType]
    public class WmsRasterLayerConfigViewModel : ViewModelBase
    {
        private WmsRasterLayer wmsRasterLayer;
        private WmsLayerViewModel selectedLayer;
        [NonSerialized]
        private BitmapImage previewSource;
        private bool isBusy;
        [NonSerialized]
        private string name;
        [NonSerialized]
        private string userName;
        [NonSerialized]
        private string password;
        [NonSerialized]
        private string parameters;
        [NonSerialized]
        private ObservableCollection<string> formats;
        [NonSerialized]
        private ObservableCollection<string> styles;
        [NonSerialized]
        private string selectedFormat;
        [NonSerialized]
        private string selectedStyle;
        [NonSerialized]
        private string wmsServerUrl;
        [NonSerialized]
        private bool doesAddToDataRepository;
        [NonSerialized]
        private Visibility addToDataRepositoryVisibility;
        [NonSerialized]
        private ObservableCollection<WmsLayerViewModel> availableLayers;
        private ObservableCollection<WmsRasterLayerConfigViewModel> wmsDataRepository;
        private WmsRasterLayerConfigViewModel selectedWms;
        [NonSerialized]
        private RelayCommand connectCommand;
        private ObservedCommand addLayerCommand;
        [NonSerialized]
        private RelayCommand viewCompabilityCommand;

        public WmsRasterLayerConfigViewModel(bool getWmsSourceFromDataRepsitory = false)
        {
            wmsDataRepository = new ObservableCollection<WmsRasterLayerConfigViewModel>();
            selectedWms = this;
            formats = new ObservableCollection<string>();
            styles = new ObservableCollection<string>();
            wmsServerUrl = string.Empty;
            addToDataRepositoryVisibility = Visibility.Collapsed;
            availableLayers = new ObservableCollection<WmsLayerViewModel>();
            previewSource = GetDefaultPreview();
        }

        public WmsLayerViewModel SelectedLayer
        {
            get { return selectedLayer; }
            set
            {
                IsBusy = true;
                selectedLayer = value;
                RaisePropertyChanged(()=>SelectedLayer);
                if (selectedLayer != null)
                {
                    Task.Factory.StartNew(() =>
                    {
                        DrawPreview();
                    });
                }
                else
                {
                    PreviewSource = GetDefaultPreview();
                    IsBusy = false;
                }
            }
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged(()=>Name);
            }
        }

        public string UserName
        {
            get { return userName; }
            set
            {
                userName = value;
                RaisePropertyChanged(()=>UserName);
            }
        }

        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                RaisePropertyChanged(()=>Password);
            }
        }

        public string Parameters
        {
            get { return parameters; }
            set
            {
                parameters = value;
                RaisePropertyChanged(()=>Parameters);
            }
        }

        public string SelectedFormat
        {
            get { return selectedFormat; }
            set
            {
                selectedFormat = value;
                RaisePropertyChanged(()=>SelectedFormat);
            }
        }

        public string SelectedStyle
        {
            get { return selectedStyle; }
            set
            {
                selectedStyle = value;
                RaisePropertyChanged(()=>SelectedStyle);
            }
        }

        public Visibility AddToDataRepositoryVisibility
        {
            get { return addToDataRepositoryVisibility; }
            set
            {
                addToDataRepositoryVisibility = value;
                RaisePropertyChanged(()=>AddToDataRepositoryVisibility);
            }
        }

        public ObservableCollection<string> Styles
        {
            get { return styles; }
        }

        public ObservableCollection<string> Formats
        {
            get { return formats; }
        }


        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                RaisePropertyChanged(()=>IsBusy);
            }
        }

        public BitmapImage PreviewSource
        {
            get { return previewSource; }
            set
            {
                previewSource = value;
                RaisePropertyChanged(()=>PreviewSource);
            }
        }

        public string WmsServerUrl
        {
            get { return wmsServerUrl; }
            set
            {
                wmsServerUrl = value;
                RaisePropertyChanged(()=>WmsServerUrl);
                if (!string.IsNullOrEmpty(wmsServerUrl) && string.IsNullOrEmpty(Name) && Uri.IsWellFormedUriString(wmsServerUrl, UriKind.RelativeOrAbsolute))
                {
                    Uri uri = new Uri(wmsServerUrl);
                    Name = uri.Host;
                }
            }
        }

        public ObservableCollection<WmsLayerViewModel> AvailableLayers { get { return availableLayers; } }

        public bool DoesAddToDataRepository
        {
            get { return doesAddToDataRepository; }
            set
            {
                doesAddToDataRepository = value;
                RaisePropertyChanged(()=>DoesAddToDataRepository);
            }
        }

        public WmsRasterLayer WmsRasterLayer
        {
            get { return wmsRasterLayer; }
        }

        public ObservableCollection<WmsRasterLayerConfigViewModel> WmsDataRepository
        {
            get { return wmsDataRepository; }
        }

        public WmsRasterLayerConfigViewModel SelectedWms
        {
            get { return selectedWms; }
            set
            {
                selectedWms = value ?? this;
                RaisePropertyChanged(()=>SelectedWms);
            }
        }
        public RelayCommand ConnectCommand
        {
            get
            {
                if (connectCommand == null)
                {
                    connectCommand = new RelayCommand(Connect);
                }
                return connectCommand;
            }
        }

        public ObservedCommand AddLayerCommand
        {
            get
            {
                if (addLayerCommand == null)
                {
                    addLayerCommand = new ObservedCommand(AddServerLayer, () => WmsRasterLayer != null && SelectedLayer != null);
                }
                return addLayerCommand;
            }
        }

        public RelayCommand ViewCompabilityCommand
        {
            get
            {
                if (viewCompabilityCommand == null)
                {
                    viewCompabilityCommand = new RelayCommand(ViewCompability);
                }
                return viewCompabilityCommand;
            }
        }

        private void Connect()
        {
            if (Uri.IsWellFormedUriString(WmsServerUrl, UriKind.Absolute))
            {
                if (WmsRasterLayer == null)
                {
                    InitializeWmsRasterLayer();
                }

                IsBusy = true;
                Task.Factory.StartNew(new Action(() =>
                {
                    Collection<string> styleNames = new Collection<string>();
                    Collection<string> outputFormats = new Collection<string>();
                    Collection<string> serverLayerNames = new Collection<string>();

                    try
                    {
                        if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password))
                        {
                            WmsRasterLayer.Credentials = new NetworkCredential(UserName, Password);
                        }
                        if (!WmsRasterLayer.IsOpen) WmsRasterLayer.Open();
                        styleNames = WmsRasterLayer.GetServerStyleNames();
                        outputFormats = WmsRasterLayer.GetServerOutputFormats();
                        serverLayerNames = WmsRasterLayer.GetServerLayerNames();
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SendMessageBox(ex.Message, "Warning");
                        }));
                    }
                    finally
                    {
                        var action = new Action(() =>
                        {
                            foreach (var item in styleNames)
                            {
                                styles.Add(item);
                            }
                            foreach (var item in outputFormats)
                            {
                                formats.Add(item);
                            }
                            if (serverLayerNames.Count() > 0)
                            {
                                AvailableLayers.Clear();
                                foreach (var serverLayer in serverLayerNames)
                                {
                                    if (!string.IsNullOrEmpty(serverLayer))
                                    {
                                        AvailableLayers.Add(new WmsLayerViewModel(serverLayer));
                                    }
                                }
                            }
                            IsBusy = false;
                        });
                        if (Application.Current != null)
                        {
                            Application.Current.Dispatcher.BeginInvoke(action);
                        }
                        else
                            action();
                    }
                }));
            }
            else
            {
                SendMessageBox("The server address is not valid.", "Info");
            }
        }

        private void InitializeWmsRasterLayer()
        {
            wmsRasterLayer = new WmsRasterLayer(new Uri(wmsServerUrl, UriKind.Absolute));
            wmsRasterLayer.UpperThreshold = double.MaxValue;
            wmsRasterLayer.LowerThreshold = double.MinValue;
        }

        private void AddServerLayer()
        {
            if (WmsRasterLayer != null && SelectedLayer != null)
            {
                if (!string.IsNullOrEmpty(parameters))
                {
                    string[] paramArray = parameters.Split('&');
                    foreach (var item in paramArray)
                    {
                        int index = item.IndexOf("=");
                        if (index > 0)
                        {
                            string paraKey = item.Substring(0, index);
                            string paraValue = item.Remove(0, index + 1);
                            if (!WmsRasterLayer.Parameters.ContainsKey(paraKey))
                            {
                                WmsRasterLayer.Parameters.Add(paraKey, paraValue);
                            }
                        }
                        else
                            continue;
                    }
                }
                WmsRasterLayer.ActiveLayerNames.Add(SelectedLayer.Name);
                WmsRasterLayer.Exceptions = "application/vnd.ogc.se_xmld";
                WmsRasterLayer.Name = Name;
                if (!string.IsNullOrEmpty(SelectedFormat))
                {
                    WmsRasterLayer.OutputFormat = SelectedFormat;
                }
                if (!string.IsNullOrEmpty(SelectedStyle) && !WmsRasterLayer.ActiveStyleNames.Contains(SelectedStyle))
                {
                    WmsRasterLayer.ActiveStyleNames.Add(SelectedStyle);
                }

                Messenger.Default.Send(true, this);
            }
            else
            {
                SendMessageBox("Please add a server and choose a layer.", "Info");
            }
        }

        private void ViewCompability()
        {
            string url = WmsServerUrl + "?REQUEST=GetCapabilities&SERVICE=WMS";
            if (!string.IsNullOrEmpty(WmsServerUrl) && WmsServerUrl.StartsWith("http://"))
            {
                Process.Start(url);
            }
            else
            {
                SendMessageBox("The server address is not valid.", "Info");
            }
        }

        private BitmapImage GetDefaultPreview()
        {
            var streamInfo = Application.GetResourceStream(new Uri("/GisEditorPluginCore;component/Images/Preview.png", UriKind.Relative));
            if (streamInfo != null)
            {
                return ToImageSource(streamInfo.Stream);
            }
            else return null;
        }

        private void DrawPreview()
        {
            if (WmsRasterLayer == null)
            {
                InitializeWmsRasterLayer();
            }
            Bitmap previewBitmap = null;
            MemoryStream bitmapMemory = null;
            try
            {
                WmsRasterLayer.Open();
                WmsRasterLayer.TimeoutInSecond = 30;
                WmsRasterLayer.ActiveLayerNames.Clear();
                WmsRasterLayer.ActiveLayerNames.Add(SelectedLayer.Name);

                MapEngine mapEngine = new MapEngine();
                mapEngine.StaticLayers.Add(WmsRasterLayer);
                mapEngine.CurrentExtent = WmsRasterLayer.GetBoundingBox();

                previewBitmap = new Bitmap(125, 125);
                mapEngine.DrawStaticLayers(previewBitmap, GeographyUnit.DecimalDegree);

                bitmapMemory = new MemoryStream();
                previewBitmap.Save(bitmapMemory, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SendMessageBox(ex.Message, "Warning");
                }));
            }
            finally
            {
                if (previewBitmap != null)
                {
                    previewBitmap.Dispose();
                }
                var action = new Action(() =>
                {
                    if (bitmapMemory == null || bitmapMemory.Length == 0)
                    {
                        PreviewSource = GetDefaultPreview();
                    }
                    else
                    {
                        PreviewSource = ToImageSource(bitmapMemory);
                    }
                    IsBusy = false;
                });
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(action);
                }
                else
                    action();
            }
        }

        private static BitmapImage ToImageSource(Stream stream)
        {
            BitmapImage blankImage = new BitmapImage();
            blankImage.BeginInit();
            blankImage.StreamSource = stream;
            blankImage.EndInit();
            blankImage.Freeze();
            return blankImage;
        }

        private void SendMessageBox(string content, string caption)
        {
            DialogMessage dialogMessage = new DialogMessage(content, null);
            dialogMessage.Caption = caption;
            Messenger.Default.Send(dialogMessage, this);
        }
    }
}
