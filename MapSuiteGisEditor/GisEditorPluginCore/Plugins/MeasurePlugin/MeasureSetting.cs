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
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Serialize;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class MeasureSetting : Setting
    {
        private ObservableCollection<DistanceUnit> distanceUnits;
        private ObservableCollection<AreaUnit> areaUnits;
        private GeoBrush measureFillColor;
        private GeoBrush measureOutlineColor;
        private float measureOutlineThickness;
        private GeoBrush trackFillColor;
        private GeoBrush trackOutlineColor;
        private float trackOutlineThickness;
        private bool allowCollectFixedElements;
        private bool useGdiPlusInsteadOfDrawingVisual;
        private CompositeStyle measurementStyle;

        /// <summary>
        /// MeasureOptionViewModel
        /// MeasureOptionUserControl
        /// </summary>
        public MeasureSetting()
        {
            allowCollectFixedElements = true;

            MeasurementStyle = MeasureTrackInteractiveOverlay.GetInitialCompositeStyle();
        }

        public CompositeStyle MeasurementStyle
        {
            get { return measurementStyle; }
            set { measurementStyle = value; }
        }

        public static MeasureSetting Instance { get { return Singleton<MeasureSetting>.Instance; } }

        public DistanceUnit SelectedDistanceUnit
        {
            get { return MeasureTrackInteractiveOverlay.DistanceUnit; }
            set { MeasureTrackInteractiveOverlay.DistanceUnit = value; }
        }

        public AreaUnit SelectedAreaUnit
        {
            get { return MeasureTrackInteractiveOverlay.AreaUnit; }
            set { MeasureTrackInteractiveOverlay.AreaUnit = value; }
        }

        public bool AllowCollectFixedElements
        {
            get { return allowCollectFixedElements; }
            set { allowCollectFixedElements = value; }
        }

        public bool UseGdiPlusInsteadOfDrawingVisual
        {
            get { return useGdiPlusInsteadOfDrawingVisual; }
            set { useGdiPlusInsteadOfDrawingVisual = value; }
        }

        public override Dictionary<string, string> SaveState()
        {
            var resultState = base.SaveState();
            resultState["MeasureDistanceUnit"] = SelectedDistanceUnit.ToString();
            resultState["MeasureAreaUnit"] = SelectedAreaUnit.ToString();
            resultState["AllowCollectFixedElements"] = AllowCollectFixedElements.ToString();
            resultState["UseGdiPlusInsteadOfDrawingVisual"] = UseGdiPlusInsteadOfDrawingVisual.ToString();
            try
            {
                GeoSerializer serializer = new GeoSerializer();
                resultState.Add("MeasurementStyle", serializer.Serialize(MeasurementStyle));
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            return resultState;
        }

        public override void LoadState(System.Collections.Generic.Dictionary<string, string> state)
        {
            base.LoadState(state);
            PluginHelper.Restore(state, "MeasureDistanceUnit", tmpString =>
            {
                DistanceUnit currentDistanceUnit = DistanceUnit.Meter;
                if (Enum.TryParse(tmpString, out currentDistanceUnit))
                {
                    SelectedDistanceUnit = currentDistanceUnit;
                }
            });

            PluginHelper.Restore(state, "MeasureAreaUnit", tmpString =>
            {
                AreaUnit currentAreaUnit = AreaUnit.SquareMeters;
                if (Enum.TryParse(tmpString, out currentAreaUnit))
                {
                    SelectedAreaUnit = currentAreaUnit;
                }
            });

            PluginHelper.RestoreBoolean(state, "AllowCollectFixedElements", v => AllowCollectFixedElements = v);
            PluginHelper.RestoreBoolean(state, "UseGdiPlusInsteadOfDrawingVisual", v => UseGdiPlusInsteadOfDrawingVisual = v);
            PluginHelper.Restore(state, "MeasurementStyle", v =>
            {
                GeoSerializer serializer = new GeoSerializer();
                try
                {
                    MeasurementStyle = (CompositeStyle)serializer.Deserialize(v);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            });
        }
    }
}