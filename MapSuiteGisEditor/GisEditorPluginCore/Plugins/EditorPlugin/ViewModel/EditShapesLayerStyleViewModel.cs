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
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ThinkGeo.MapSuite.Styles;
using Style = ThinkGeo.MapSuite.Styles.Style;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class EditShapesLayerStyleViewModel : ViewModelBase
    {
        [NonSerialized]
        private BitmapImage editStylePreview;

        public EditShapesLayerStyleViewModel()
        {
            UpdateEditStylePreviewASync();
        }

        public BitmapImage EditStylePreview
        {
            get { return editStylePreview; }
            set
            {
                editStylePreview = value;
                RaisePropertyChanged(() => EditStylePreview);
            }
        }

        public CompositeStyle EditCompositeStyle
        {
            get { return Singleton<EditorSetting>.Instance.EditCompositeStyle; }
            set
            {
                Singleton<EditorSetting>.Instance.EditCompositeStyle = value;
                var editOverlay = SharedViewModel.Instance.EditOverlay;
                if (editOverlay != null)
                {
                    if (editOverlay.EditShapesLayer.InternalFeatures.Count > 0)
                    {
                        editOverlay.Refresh();
                    }
                }
                UpdateEditStylePreviewASync();
            }
        }

        public void UpdateEditStylePreviewASync()
        {
            var editOverlay = SharedViewModel.Instance.EditOverlay;
            if (editOverlay != null)
            {
                editOverlay.EditShapesLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = null;
                editOverlay.EditShapesLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = null;
                editOverlay.EditShapesLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = null;
                editOverlay.EditShapesLayer.ZoomLevelSet.ZoomLevel01.DefaultTextStyle = null;
                if (EditCompositeStyle != null)
                {
                    foreach (var tempStyle in EditCompositeStyle?.Styles)
                    {
                        editOverlay.EditShapesLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(tempStyle);
                    }
                }
            }

            if (Application.Current != null)
            {
                Task.Factory.StartNew(obj =>
                {
                    var targetStyle = (Style)obj;
                    var imageBuffer = StyleHelper.GetImageBufferFromStyle(targetStyle);
                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var imageSource = StyleHelper.ConvertToImageSource(imageBuffer);
                            EditStylePreview = imageSource;
                        }), DispatcherPriority.Background, null);
                    }
                }, EditCompositeStyle);
            }
        }
    }
}