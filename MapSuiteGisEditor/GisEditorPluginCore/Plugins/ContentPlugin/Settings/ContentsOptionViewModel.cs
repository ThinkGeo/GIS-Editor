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
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ContentsOptionViewModel : ViewModelBase
    {
        private int[] tileSizes;
        private int[] limitations;
        private DefaultBaseMap[] defaultBaseMapOptions;
        private AltitudeMode[] altitudeModes;

        private ContentSetting contentOption;

        public ContentsOptionViewModel(ContentSetting contentOption)
        {
            this.contentOption = contentOption;
            limitations = new[] { 1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 12000, 20000 };
            tileSizes = new[] { 128, 256, 512, 1024 };
            defaultBaseMapOptions = new[] { DefaultBaseMap.None, DefaultBaseMap.WorldMapKit, DefaultBaseMap.OpenStreetMaps, DefaultBaseMap.BingMaps };
            altitudeModes = new[] { AltitudeMode.Absolute, AltitudeMode.ClampToGround, AltitudeMode.RelativeToGround };
        }

        public int[] Limitations
        {
            get { return limitations; }
        }

        public int MaxRecordsToDraw
        {
            get { return contentOption.MaxRecordsToDraw; }
            set
            {
                contentOption.MaxRecordsToDraw = value;
                RaisePropertyChanged(() => MaxRecordsToDraw);
            }
        }

        public DefaultBaseMap[] DefaultBaseMapOptions
        {
            get { return defaultBaseMapOptions; }
        }


        public AltitudeMode[] AltitudeModes
        {
            get { return altitudeModes; }
        }

        public DefaultBaseMap DefaultBaseMapOption
        {
            get { return contentOption.DefaultBaseMapOption; }
            set
            {
                contentOption.DefaultBaseMapOption = value;
                RaisePropertyChanged(() => DefaultBaseMapOption);
            }
        }

        public decimal PlaceSearchMaxValue
        {
            get { return contentOption.PlaceSearchMaxValue; }
            set
            {
                contentOption.PlaceSearchMaxValue = value;
                RaisePropertyChanged(() => PlaceSearchMaxValue);
            }
        }

        public int OverlayRefreshDelayInterval
        {
            get { return contentOption.OverlayRefreshDelayInterval; }
            set
            {
                contentOption.OverlayRefreshDelayInterval = value;
                RaisePropertyChanged(() => OverlayRefreshDelayInterval);
            }
        }

        public bool DisableGlobeButton
        {
            get { return contentOption.DisableGlobeButton; }
            set
            {
                contentOption.DisableGlobeButton = value;
                RaisePropertyChanged(() => DisableGlobeButton);
            }
        }

        public bool IsZoomSnapDirectionWithLowerScale
        {
            get { return contentOption.ZoomSnapDirection == ZoomSnapDirection.LowerScale; }
            set
            {
                contentOption.ZoomSnapDirection = value ? ZoomSnapDirection.LowerScale : ZoomSnapDirection.UpperScale;
                RaisePropertyChanged(() => IsZoomSnapDirectionWithLowerScale);
            }
        }

        public AltitudeMode AltitudeMode
        {
            get { return contentOption.AltitudeMode; }
            set
            {
                contentOption.AltitudeMode = value;
                RaisePropertyChanged(() => AltitudeMode);
            }
        }

        public int Height
        {
            get { return contentOption.Height; }
            set
            {
                contentOption.Height = value;
                RaisePropertyChanged(() => Height);
            }
        }

        public int[] TileSizes
        {
            get { return tileSizes; }
        }

        public int TileSize
        {
            get { return contentOption.TileSize; }
            set
            {
                contentOption.TileSize = value;
                RaisePropertyChanged(() => TileSize);
            }
        }

        public bool HighQuality
        {
            get { return contentOption.HighQuality; }
            set { contentOption.HighQuality = value; }
        }

        public bool IsZoomToExtentOfNewLayer
        {
            get { return contentOption.IsZoomToExtentOfNewLayer; }
            set
            {
                contentOption.IsZoomToExtentOfNewLayer = value;
                RaisePropertyChanged(() => IsZoomToExtentOfNewLayer);
                if (!value)
                {
                    IsZoomToExtentOfOnlyFirstLayer = false;
                }
            }
        }

        public bool IsZoomToExtentOfOnlyFirstLayer
        {
            get { return contentOption.IsZoomToExtentOfOnlyFirstLayer; }
            set
            {
                contentOption.IsZoomToExtentOfOnlyFirstLayer = value;
                RaisePropertyChanged(() => IsZoomToExtentOfOnlyFirstLayer);
            }
        }

        public bool IsLimitDrawgingFeaturesCount
        {
            get { return contentOption.IsLimitDrawgingFeaturesCount; }
            set
            {
                contentOption.IsLimitDrawgingFeaturesCount = value;
                RaisePropertyChanged(() => IsLimitDrawgingFeaturesCount);
            }
        }

        public bool IsAlwaysShowStyleWizardWhenLayerIsAdded
        {
            get
            {
                return contentOption.IsAlwaysShowStyleWizardWhenLayerIsAdded;
            }
            set
            {
                contentOption.IsAlwaysShowStyleWizardWhenLayerIsAdded = value;
                RaisePropertyChanged(() => IsAlwaysShowStyleWizardWhenLayerIsAdded);
            }
        }

        public bool IsShowPanZoomBar
        {
            get { return contentOption.IsShowPanZoomBar; }
            set
            {
                contentOption.IsShowPanZoomBar = value;
                RaisePropertyChanged(() => IsShowPanZoomBar);
            }
        }

        public bool UseCache
        {
            get { return contentOption.UseCache; }
            set
            {
                contentOption.UseCache = value;
                RaisePropertyChanged(() => UseCache);
            }
        }

        public bool IsShowAddDataRepositoryDialog
        {
            get { return contentOption.IsShowAddDataRepositoryDialog; }
            set
            {
                contentOption.IsShowAddDataRepositoryDialog = value;
                RaisePropertyChanged(() => IsShowAddDataRepositoryDialog);
            }
        }
    }
}