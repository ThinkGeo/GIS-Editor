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
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ContentRibbonGroupViewModel : ViewModelBase
    {
        private ObservableCollection<LayerPlugin> supportedLayerProviders;
        private ObservedCommand<string> addLayerCommand;
        private ObservedCommand refreshSupportedLayerProvidersCommand;
        private ObservedCommand addAllKindOfLayersCommand;

        public ContentRibbonGroupViewModel()
        { }

        public ObservedCommand AddAllKindOfLayersCommand
        {
            get
            {
                if (addAllKindOfLayersCommand == null)
                {
                    addAllKindOfLayersCommand = new ObservedCommand(() =>
                    {
                        CommandHelper.AddNewLayersCommand.Execute(true);
                    }, CommandHelper.CheckMapIsNotNull);
                }
                return addAllKindOfLayersCommand;
            }
        }

        public ObservedCommand RefreshSupportedLayerProvidersCommand
        {
            get
            {
                if (refreshSupportedLayerProvidersCommand == null)
                {
                    refreshSupportedLayerProvidersCommand = new ObservedCommand(() =>
                    {
                        RaisePropertyChanged(() => SupportedLayerProviders);
                    }, CommandHelper.CheckMapIsNotNull);
                }
                return refreshSupportedLayerProvidersCommand;
            }
        }

        public ObservedCommand<string> AddLayerCommand
        {
            get
            {
                if (addLayerCommand == null)
                {
                    addLayerCommand = new ObservedCommand<string>(layerGeneratorName =>
                    {
                        LayerPlugin layerGenerator = SupportedLayerProviders.FirstOrDefault(l => l.Name == layerGeneratorName);
                        if (layerGenerator != null)
                        {
                            Collection<Layer> newLayers = new Collection<Layer>();
                            try
                            {
                                newLayers = layerGenerator.GetLayers();
                            }
                            catch (Exception ex)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                                if (ex.InnerException != null)
                                {
                                    SendExceptionMessage("Unable to Open File(s)", ex.InnerException.Message);
                                }
                                else
                                {
                                    SendExceptionMessage("Unable to Open File(s)", ex.Message);
                                }
                            }
                            finally
                            {
                                if (GisEditor.ActiveMap != null && newLayers.Count > 0)
                                {
                                    var featureLayer = newLayers[0] as FeatureLayer;
                                    if (featureLayer != null)
                                    {
                                        var uri = layerGenerator.GetUri(featureLayer);
                                        if (uri != null && uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
                                            CommandHelper.AddToDataRepository(uri.LocalPath);
                                    }

                                    GisEditor.ActiveMap.AddLayersBySettings(newLayers, true);
                                    GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.AddLayerCommandDescription));
                                }
                            }
                        }
                    }, CommandHelper.CheckMapIsNotNull);
                }
                return addLayerCommand;
            }
        }

        public ObservableCollection<LayerPlugin> SupportedLayerProviders
        {
            get
            {
                if (supportedLayerProviders == null)
                {
                    supportedLayerProviders = new ObservableCollection<LayerPlugin>();
                }
                supportedLayerProviders.Clear();

                var contentUIPlugin = GisEditor.UIManager.GetUIPlugins().OfType<ContentUIPlugin>().FirstOrDefault();

                if (contentUIPlugin != null)
                    contentUIPlugin.OnLayerPluginDropDownOpening(supportedLayerProviders);

                if (GisEditor.LayerManager != null)
                {
                    var allLayerPlugins = GisEditor.LayerManager.GetActiveLayerPlugins<LayerPlugin>();
                    var groupPlugins = allLayerPlugins.OfType<GroupLayerPlugin>().SelectMany(p => p.LayerPlugins).ToList();
                    foreach (var item in groupPlugins)
                    {
                        if (allLayerPlugins.Contains(item))
                        {
                            allLayerPlugins.Remove(item);
                        }
                    }
                    foreach (var provider in allLayerPlugins)
                    {
                        supportedLayerProviders.Add(provider);
                    }
                }

                if (contentUIPlugin != null)
                    contentUIPlugin.OnLayerPluginDropDownOpened(supportedLayerProviders);

                return supportedLayerProviders;
            }
        }

        private void SendExceptionMessage(string caption, string message)
        {
            DialogMessage dialogMessage = new DialogMessage(message, null);
            dialogMessage.Button = MessageBoxButton.OK;
            dialogMessage.Icon = MessageBoxImage.Warning;
            dialogMessage.Caption = caption;
            MessengerInstance.Send(dialogMessage, this);
        }
    }
}