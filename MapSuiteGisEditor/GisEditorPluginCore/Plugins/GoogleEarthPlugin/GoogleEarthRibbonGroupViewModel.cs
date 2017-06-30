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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class GoogleEarthRibbonGroupViewModel : ViewModelBase
    {
        private string googleEarthInstalledPath, googleEarthProInstalledPath;
        private ObservedCommand<GoogleEarthModel> goolgeEarthCommand;
        private ObservableCollection<GoogleEarthModel> buttons;

        public GoogleEarthRibbonGroupViewModel()
        {
            InitializeButtons();
        }

        public ObservableCollection<GoogleEarthModel> Buttons
        {
            get { return buttons; }
        }

        public bool IsGoolgeEarthButtonEnabled
        {
            get { return GisEditor.ActiveMap != null && GisEditor.ActiveMap.Overlays.Count != 0; }
        }

        public ObservedCommand<GoogleEarthModel> GoolgeEarthCommand
        {
            get
            {
                if (goolgeEarthCommand == null)
                {
                    goolgeEarthCommand = new ObservedCommand<GoogleEarthModel>((g) =>
                    {
                        switch (g.Name)
                        {
                            case "Save as raster KML":
                                GisEditor.ActiveMap.SaveAsRasterKML();
                                break;

                            case "Save as vector KML":
                                GisEditor.ActiveMap.SaveAsVectorKML();
                                break;

                            case "Open in Google earth":
                            case "Open in Google earth Pro":
                                List<FeatureLayer> featureLayers = GisEditor.ActiveMap.GetFeatureLayers(true).Select(l =>
                                {
                                    FeatureLayer featureLayer = null;
                                    lock (l)
                                    {
                                        if (l.IsOpen) l.Close();
                                        featureLayer = (FeatureLayer)l.CloneDeep();
                                        l.Open();
                                    }
                                    return featureLayer;
                                }).ToList();

                                AnnotationTrackInteractiveOverlay annotationOverlay = GisEditor.ActiveMap.InteractiveOverlays.OfType<AnnotationTrackInteractiveOverlay>().FirstOrDefault();
                                if (annotationOverlay != null && annotationOverlay.TrackShapeLayer.InternalFeatures.Count > 0)
                                {
                                    featureLayers.Add(annotationOverlay.TrackShapeLayer);
                                }
                                MeasureTrackInteractiveOverlay measureOverlay = GisEditor.ActiveMap.InteractiveOverlays.OfType<MeasureTrackInteractiveOverlay>().FirstOrDefault();
                                if (measureOverlay != null && measureOverlay.ShapeLayer.MapShapes.Count > 0)
                                {
                                    measureOverlay.TrackShapeLayer.Close();
                                    InMemoryFeatureLayer featureLayer = (InMemoryFeatureLayer)measureOverlay.TrackShapeLayer.CloneDeep();
                                    measureOverlay.TrackShapeLayer.Open();
                                    foreach (var item in measureOverlay.ShapeLayer.MapShapes)
                                    {
                                        featureLayer.InternalFeatures.Add(item.Value.Feature);
                                    }
                                    featureLayers.Add(featureLayer);
                                }

                                GisEditor.ActiveMap.OpenInGoogleEarth(g.Name.Equals("Open in Google earth") ? googleEarthInstalledPath : googleEarthProInstalledPath
                                    , featureLayers.ToArray());
                                break;
                        }
                    }, (g) => IsGoolgeEarthButtonEnabled);
                }
                return goolgeEarthCommand;
            }
        }

        public void UpdateButtonStates()
        {
            GoogleEarthModel ogeButton = buttons.Where(b => b.Name.Equals("Open in Google earth", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            GoogleEarthModel ogeProButton = buttons.Where(b => b.Name.Equals("Open in Google earth Pro", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            googleEarthInstalledPath = GoogleEarthHelper.GetGoogleEarthInstalledPath();
            googleEarthProInstalledPath = GoogleEarthHelper.GetGoogleEarthProInstalledPath();

            ogeButton.IsEnabled = !string.IsNullOrEmpty(googleEarthInstalledPath);
            ogeProButton.IsEnabled = !string.IsNullOrEmpty(googleEarthProInstalledPath);
        }

        private void InitializeButtons()
        {
            buttons = new ObservableCollection<GoogleEarthModel>();

            GoogleEarthModel saveRasterKMLButton = new GoogleEarthModel();
            saveRasterKMLButton.Name = "Save as raster KML";
            saveRasterKMLButton.Icon = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/rasterkml.png"));

            GoogleEarthModel saveVectorKMLButton = new GoogleEarthModel();
            saveVectorKMLButton.Name = "Save as vector KML";
            saveVectorKMLButton.Icon = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/vectorkml.png"));

            GoogleEarthModel openGoogleEarthButton = new GoogleEarthModel();
            openGoogleEarthButton.Name = "Open in Google earth";
            openGoogleEarthButton.Icon = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/googleEarth.png"));

            GoogleEarthModel openGoogleEarthProButton = new GoogleEarthModel();
            openGoogleEarthProButton.Name = "Open in Google earth Pro";
            openGoogleEarthProButton.Icon = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/googleEarth.png"));

            buttons.Add(saveRasterKMLButton);
            buttons.Add(saveVectorKMLButton);
            buttons.Add(openGoogleEarthButton);
            buttons.Add(openGoogleEarthProButton);
        }

        internal void RaisePropertyChanged()
        {
            RaisePropertyChanged(() => IsGoolgeEarthButtonEnabled);
        }
    }
}