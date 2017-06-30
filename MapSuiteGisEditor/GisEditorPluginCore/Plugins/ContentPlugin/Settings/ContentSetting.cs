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
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// ContentsOptionViewModel
    /// ContentsOptionUserControl
    /// </summary>
    [Serializable]
    public class ContentSetting : Setting
    {
        private int height;
        private decimal placeSearchMaxValue;
        private bool isZoomToExtentOfNewLayer;
        private bool isZoomToExtentOfOnlyFirstLayer;
        private int maxRecordsToDraw;
        private bool useCache;
        private bool isLimitDrawgingFeaturesCount;
        private int tileSize;
        private bool isShowAddDataRepositoryDialog;
        private bool showPanZoomBar;
        private bool highQuality;
        private DefaultBaseMap defaultBaseMapOption;
        private AltitudeMode altitudeMode;
        private ZoomSnapDirection zoomSnapDirection;
        private bool disableGlobeButton;

        public ContentSetting()
        {
            TileSize = 256;
            UseCache = true;
            MaxRecordsToDraw = 12000;
            placeSearchMaxValue = 10;
            IsLimitDrawgingFeaturesCount = false;
            IsShowAddDataRepositoryDialog = true;
            IsShowPanZoomBar = true;
            HighQuality = true;
            DefaultBaseMapOption = DefaultBaseMap.WorldMapKit;
            IsZoomToExtentOfNewLayer = true;
            OverlayRefreshDelayInterval = 200;

            AltitudeMode = AltitudeMode.ClampToGround;
            Height = 0;
            ZoomSnapDirection = ZoomSnapDirection.UpperScale;
        }

        [DataMember]
        public decimal PlaceSearchMaxValue
        {
            get { return placeSearchMaxValue; }
            set
            {
                placeSearchMaxValue = value;
                SearchPlaceViewModel.SearchPlaceMaxResultCount = (int)placeSearchMaxValue;
            }
        }

        [DataMember]
        public bool IsZoomToExtentOfNewLayer
        {
            get { return isZoomToExtentOfNewLayer; }
            set { isZoomToExtentOfNewLayer = value; }
        }

        [DataMember]
        public bool IsZoomToExtentOfOnlyFirstLayer
        {
            get { return isZoomToExtentOfOnlyFirstLayer; }
            set { isZoomToExtentOfOnlyFirstLayer = value; }
        }

        [DataMember]
        public int OverlayRefreshDelayInterval
        {
            get { return TileOverlayExtension.RefreshBufferTimeInMillisecond; }
            set { TileOverlayExtension.RefreshBufferTimeInMillisecond = value; }
        }

        [DataMember]
        public int MaxRecordsToDraw
        {
            get { return maxRecordsToDraw; }
            set
            {
                if (maxRecordsToDraw != value)
                {
                    maxRecordsToDraw = value;
                    GetTileOverlays().OfType<LayerOverlay>().SelectMany(o => o.Layers).OfType<ShapeFileFeatureLayer>().ForEach(l =>
                    {
                        if (l.MaxRecordsToDraw != 0) l.MaxRecordsToDraw = value;
                    });
                }
            }
        }

        [DataMember]
        public bool UseCache
        {
            get { return useCache; }
            set
            {
                if (useCache != value)
                {
                    useCache = value;
                    GetTileOverlays().ForEach(o =>
                    {
                        if (value) o.RefreshCache(RefreshCacheMode.ApplyNewCache);
                        else o.RefreshCache(RefreshCacheMode.DisableCache);
                    });
                }
            }
        }

        public bool HighQuality
        {
            get { return highQuality; }
            set
            {
                highQuality = value;
                GetTileOverlays().OfType<LayerOverlay>().ForEach(o =>
                {
                    if (value) o.DrawingQuality = DrawingQuality.HighQuality;
                    else o.DrawingQuality = DrawingQuality.HighSpeed;

                    if (useCache) o.RefreshCache(RefreshCacheMode.ApplyNewCache);
                });
            }
        }

        [DataMember]
        public bool IsLimitDrawgingFeaturesCount
        {
            get { return isLimitDrawgingFeaturesCount; }
            set
            {
                if (isLimitDrawgingFeaturesCount != value)
                {
                    isLimitDrawgingFeaturesCount = value;
                    GetTileOverlays().OfType<LayerOverlay>().SelectMany(o => o.Layers).OfType<ShapeFileFeatureLayer>().ForEach(l =>
                    {
                        if (value) l.MaxRecordsToDraw = MaxRecordsToDraw;
                        else l.MaxRecordsToDraw = 0;
                    });
                }
            }
        }

        [DataMember]
        public int TileSize
        {
            get { return tileSize; }
            set
            {
                if (tileSize != value)
                {
                    tileSize = value;
                    GetTileOverlays().ForEach(o =>
                    {
                        o.TileWidth = value;
                        o.TileHeight = value;
                        if (o.TileCache != null) o.RefreshCache(RefreshCacheMode.ApplyNewCache);
                    });
                }
            }
        }

        [DataMember]
        public bool IsAlwaysShowStyleWizardWhenLayerIsAdded
        {
            get { return GisEditor.StyleManager.UseWizard; }
            set { GisEditor.StyleManager.UseWizard = value; }
        }

        [DataMember]
        public bool IsShowAddDataRepositoryDialog
        {
            get { return isShowAddDataRepositoryDialog; }
            set { isShowAddDataRepositoryDialog = value; }
        }

        [DataMember]
        public DefaultBaseMap DefaultBaseMapOption
        {
            get { return defaultBaseMapOption; }
            set { defaultBaseMapOption = value; }
        }

        [DataMember]
        public bool IsShowPanZoomBar
        {
            get { return showPanZoomBar; }
            set
            {
                showPanZoomBar = value;
                foreach (var map in GisEditor.GetMaps())
                {
                    SetPanZoomBarVisiable(map);
                }

                if (Application.Current != null
                    && Application.Current.MainWindow != null
                    && Application.Current.MainWindow.Tag is Dictionary<string, string>)
                {
                    ((Dictionary<string, string>)Application.Current.MainWindow.Tag)["IsShowPanZoomBar"] = value.ToString();
                }
            }
        }

        [DataMember]
        public AltitudeMode AltitudeMode
        {
            get { return altitudeMode; }
            set
            {
                altitudeMode = value;
                KmlGeoCanvas.Mode = altitudeMode;
            }
        }

        [DataMember]
        public int Height
        {
            get { return height; }
            set
            {
                height = value;
                KmlGeoCanvas.ZHeight = height;
            }
        }

        public bool DisableGlobeButton
        {
            get { return disableGlobeButton; }
            set
            {
                disableGlobeButton = value;
                foreach (var map in GisEditor.GetMaps())
                {
                    foreach (var mapTool in map.MapTools.OfType<SwitcherPanZoomBarMapTool>())
                    {
                        mapTool.IsGlobeButtonEnabled = !value;
                    }
                }
            }
        }

        [DataMember]
        public ZoomSnapDirection ZoomSnapDirection
        {
            get { return zoomSnapDirection; }
            set
            {
                zoomSnapDirection = value;
                foreach (var map in GisEditor.GetMaps())
                {
                    foreach (ExtentInteractiveOverlay overlay in map.InteractiveOverlays.OfType<ExtentInteractiveOverlay>())
                    {
                        overlay.ZoomSnapDirection = value;
                    }
                }
            }
        }

        public void SetPanZoomBarVisiable(GisEditorWpfMap map)
        {
            var panZoomBar = map.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
            if (panZoomBar != null) panZoomBar.IsEnabled = showPanZoomBar;
        }

        public override Dictionary<string, string> SaveState()
        {
            Dictionary<string, string> resultState = base.SaveState();
            resultState["IsZoomToExtentOfNewLayer"] = IsZoomToExtentOfNewLayer.ToString();
            resultState["IsZoomToExtentOfOnlyFirstLayer"] = IsZoomToExtentOfOnlyFirstLayer.ToString();
            resultState["MaxRecordsToDraw"] = MaxRecordsToDraw.ToString(CultureInfo.InvariantCulture);
            resultState["UseCache"] = UseCache.ToString();
            resultState["IsLimitDrawgingFeaturesCount"] = IsLimitDrawgingFeaturesCount.ToString();
            resultState["TileSize"] = TileSize.ToString(CultureInfo.InvariantCulture);
            resultState["IsShowAddDataRepositoryDialog"] = IsShowAddDataRepositoryDialog.ToString();
            resultState["IsShowPanZoomBar"] = IsShowPanZoomBar.ToString();
            resultState["HighQuality"] = HighQuality.ToString();
            resultState["DefaultBaseMapOption"] = ((int)DefaultBaseMapOption).ToString(CultureInfo.InvariantCulture);
            resultState["PlaceSearchMaxResultCount"] = PlaceSearchMaxValue.ToString(CultureInfo.InvariantCulture);
            resultState["AltitudeMode"] = ((int)AltitudeMode).ToString(CultureInfo.InvariantCulture);
            resultState["Height"] = Height.ToString(CultureInfo.InvariantCulture);
            resultState["ZoomSnapDirection"] = ((int)ZoomSnapDirection).ToString(CultureInfo.InvariantCulture);
            resultState["DisableGlobeButton"] = DisableGlobeButton.ToString();
            resultState["OverlayRefreshDelayInterval"] = OverlayRefreshDelayInterval.ToString();

            return resultState;
        }

        public override void LoadState(Dictionary<string, string> state)
        {
            base.LoadState(state);

            PluginHelper.RestoreBoolean(state, "IsZoomToExtentOfNewLayer", v => IsZoomToExtentOfNewLayer = v);
            PluginHelper.RestoreBoolean(state, "IsZoomToExtentOfOnlyFirstLayer", v => IsZoomToExtentOfOnlyFirstLayer = v);
            PluginHelper.RestoreInteger(state, "MaxRecordsToDraw", v => MaxRecordsToDraw = v);
            PluginHelper.RestoreBoolean(state, "UseCache", v => UseCache = v);
            PluginHelper.RestoreBoolean(state, "IsLimitDrawgingFeaturesCount", v => IsLimitDrawgingFeaturesCount = v);
            PluginHelper.RestoreInteger(state, "TileSize", v => TileSize = v);
            PluginHelper.RestoreBoolean(state, "IsShowAddDataRepositoryDialog", v => IsShowAddDataRepositoryDialog = v);
            PluginHelper.RestoreBoolean(state, "IsShowPanZoomBar", v => IsShowPanZoomBar = v);
            PluginHelper.RestoreBoolean(state, "HighQuality", v => HighQuality = v);
            PluginHelper.RestoreInteger(state, "DefaultBaseMapOption", v => DefaultBaseMapOption = (DefaultBaseMap)v);
            PluginHelper.RestoreInteger(state, "PlaceSearchMaxResultCount", v => PlaceSearchMaxValue = (decimal)v);
            PluginHelper.RestoreInteger(state, "AltitudeMode", v => AltitudeMode = (AltitudeMode)v);
            PluginHelper.RestoreInteger(state, "Height", v => Height = v);
            PluginHelper.RestoreInteger(state, "ZoomSnapDirection", v => ZoomSnapDirection = (ZoomSnapDirection)v);
            PluginHelper.RestoreBoolean(state, "DisableGlobeButton", v => DisableGlobeButton = v);
            PluginHelper.RestoreInteger(state, "OverlayRefreshDelayInterval", v => OverlayRefreshDelayInterval = v);
        }

        private IEnumerable<TileOverlay> GetTileOverlays()
        {
            return GisEditor.GetMaps().SelectMany(tmpMap => tmpMap.Overlays.OfType<TileOverlay>());
        }
    }
}