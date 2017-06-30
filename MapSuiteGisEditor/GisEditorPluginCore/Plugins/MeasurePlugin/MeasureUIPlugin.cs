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
using Microsoft.Windows.Controls.Ribbon;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class MeasureUIPlugin : UIPlugin
    {
        private readonly static string measureModeKeyName = "MeasureModeKey";

        private static MeasuringInMode measuringMode;

        [NonSerialized]
        private RibbonGroup helpRibbonGroup;
        [NonSerialized]
        private MeasureRibbonGroup measureGroup;
        [NonSerialized]
        private MeasureOptionUserControl optionUI;

        private RibbonEntry helpEntry;
        private RibbonEntry measureEntry;
        private bool measuringModeIsRestored;

        public MeasureUIPlugin()
        {
            Index = UIPluginOrder.MeasurePlugin;
            Description = GisEditor.LanguageManager.GetStringResource("MeasurePluginDescription");

            measureGroup = new MeasureRibbonGroup();
            measureEntry = new RibbonEntry(measureGroup, RibbonTabOrder.Measure, "MeasureRibbonTabHeader");

            helpRibbonGroup = new RibbonGroup();
            helpRibbonGroup.Items.Add(HelpResourceHelper.GetHelpButton("MeasurePluginHelp", HelpButtonMode.RibbonButton));
            helpRibbonGroup.GroupSizeDefinitions.Add(new RibbonGroupSizeDefinition() { IsCollapsed = false });
            helpRibbonGroup.SetResourceReference(RibbonGroup.HeaderProperty, "HelpHeader");
            helpEntry = new RibbonEntry(helpRibbonGroup, RibbonTabOrder.Measure, "MeasureRibbonTabHeader");
        }

        internal static MeasuringInMode MeasuringMode
        {
            get { return measuringMode; }
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (optionUI == null)
            {
                optionUI = new MeasureOptionUserControl();
                optionUI.DataContext = new MeasureOptionViewModel(Singleton<MeasureSetting>.Instance);
            }

            return optionUI;
        }

        protected override void LoadCore()
        {
            base.LoadCore();

            if (!RibbonEntries.Contains(measureEntry)) RibbonEntries.Add(measureEntry);
            if (!RibbonEntries.Contains(helpEntry)) RibbonEntries.Add(helpEntry);
        }

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            measureGroup.SynchronizeState(currentMap);
            MeasureTrackInteractiveOverlay measurementOverlay = null;
            if (GisEditor.ActiveMap != null && GisEditor.ActiveMap.InteractiveOverlays.Contains("MeasurementOverlay"))
            {
                measurementOverlay = GisEditor.ActiveMap.InteractiveOverlays["MeasurementOverlay"] as MeasureTrackInteractiveOverlay;
                if (measurementOverlay != null)
                {
                    RenderMode renderMode = MeasureSetting.Instance.UseGdiPlusInsteadOfDrawingVisual ? RenderMode.GdiPlus : RenderMode.DrawingVisual;
                    if (measurementOverlay.RenderMode != renderMode)
                    {
                        measurementOverlay.RenderMode = renderMode;
                    }
                    switch (measurementOverlay.TrackMode)
                    {
                        case TrackMode.Point:
                            GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawPoint;
                            break;
                        case TrackMode.Rectangle:
                            GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawRectangle;
                            break;
                        case TrackMode.Square:
                            GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawSqure;
                            break;
                        case TrackMode.Ellipse:
                            GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawEllipse;
                            break;
                        case TrackMode.Circle:
                            GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawCircle;
                            break;
                        case TrackMode.Polygon:
                            GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawPolygon;
                            break;
                        case TrackMode.Line:
                            GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawLine;
                            break;
                        case TrackMode.Custom:
                            GisEditor.ActiveMap.Cursor = GisEditorCursors.Normal;
                            break;
                    }

                    if (measurementOverlay.MeasureCustomeMode == MeasureCustomeMode.Move 
                        && measurementOverlay.TrackMode == TrackMode.Custom) 
                        GisEditor.ActiveMap.Cursor = GisEditorCursors.Cross;
                }
            }
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            foreach (var item in Singleton<MeasureSetting>.Instance.SaveState())
            {
                settings.GlobalSettings[item.Key] = item.Value;
            }
            if (GisEditor.ActiveMap != null && measureGroup != null)
            {
                settings.GlobalSettings[measureModeKeyName] = measureGroup.ViewModel.SelectedMeasuringMode.ToString();
                measureGroup.SynchronizeState(GisEditor.ActiveMap);
            }
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            Singleton<MeasureSetting>.Instance.LoadState(settings.GlobalSettings);
            if (settings.GlobalSettings.ContainsKey(measureModeKeyName) && !measuringModeIsRestored)
            {
                measuringModeIsRestored = true;
                measuringMode = MeasuringInMode.DecimalDegree;
                Enum.TryParse(settings.GlobalSettings[measureModeKeyName], out measuringMode);
            }
        }
    }
}