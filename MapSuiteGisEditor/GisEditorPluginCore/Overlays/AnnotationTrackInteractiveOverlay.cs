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


using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class AnnotationTrackInteractiveOverlay : TrackInteractiveOverlay
    {
        /// <summary>
        /// "NoteText"
        /// </summary>
        /// <remarks>
        /// this should be less than 10 char, else it can not be exported
        /// </remarks>
        private const string annotationTextColumnName = "NoteText";

        /// <summary>
        /// "ValueStyleKey"
        /// </summary>
        internal const string valueStyleMatchColumnName = "ValueStyleKey";

        internal static readonly string LinkFileStyleColumnName = "LinkFileName";

        /// <summary>
        /// "DefaultPointStyle", "DefaultLineStyle", "DefaultAreaStyle", "DefaultTextStyle"
        /// </summary>
        private static readonly string[] defaultStyleKeys = { "DefaultPointStyle", "DefaultLineStyle", "DefaultAreaStyle", "DefaultTextStyle" };

        public event EventHandler<TrackEndedTrackInteractiveOverlayEventArgs> SelectionFinished;

        [NonSerialized]
        private bool fileLinkable;

        [Obfuscation(Exclude = true)]
        private TextFirstValueStyle layerStyle;

        [NonSerialized]
        private InMemoryFeatureLayer selectionLayer;

        [NonSerialized]
        private PointShape trackStartPointShape;

        [Obfuscation(Exclude = true)]
        private bool isInModifyMode;

        [NonSerialized]
        private BaseShape tempBaseShape;

        [Obfuscation(Exclude = true)]
        private TextFirstValueStyle fileLinkStyle;

        public AnnotationTrackInteractiveOverlay()
            : base()
        {
            RenderMode = RenderMode.GdiPlus;
            InitValueStyle();
            InitSelectStyle();
        }

        public InMemoryFeatureLayer SelectionLayer { get { return selectionLayer; } }

        public ValueStyle TrackLayerStyle { get { return layerStyle; } }

        public bool FileLinkable
        {
            get { return fileLinkable; }
            set { fileLinkable = value; }
        }

        public Styles.Style LastPointStyle
        {
            get
            {
                var lastValueItem = layerStyle.ValueItems.LastOrDefault(v => v.Value != LinkFileStyleColumnName);
                if (lastValueItem != null)
                {
                    return lastValueItem.DefaultTextStyle;
                }

                return null;
            }
        }

        public static string AnnotationTextColumnName
        {
            get
            {
                return annotationTextColumnName;
            }
        }

        public static string ValueStyleMatchColumnName
        {
            get
            {
                return valueStyleMatchColumnName;
            }
        }

        public bool IsInModifyMode
        {
            get { return isInModifyMode; }
            set
            {
                isInModifyMode = value;
                if (value) TrackMode = TrackMode.None;
            }
        }

        public void ChangeAppliedStyle(Styles.Style style, AnnotaionStyleType annotationStyleType)
        {
            AreaStyle areaStyle = style as AreaStyle;
            LineStyle lineStyle = style as LineStyle;
            TextStyle textStyle = style as TextStyle;
            PointStyle pointStyle = style as PointStyle;

            if (areaStyle != null)
            {
                ApplyNewStyle(defaultStyleKeys[2], areaStyle);
            }
            else if (lineStyle != null)
            {
                ApplyNewStyle(defaultStyleKeys[1], lineStyle);
            }
            else if (textStyle != null)
            {
                ApplyNewStyle(defaultStyleKeys[3], textStyle, annotationStyleType);
            }
            else if (pointStyle != null)
            {
                ApplyNewStyle(defaultStyleKeys[0], pointStyle, annotationStyleType);
            }
        }

        public void StyleLastTextAnnotation(Feature editingFeature, string text, string styleValue = null)
        {
            if (editingFeature != null)
            {
                ValueItem lastValueItem = null;
                if (String.IsNullOrEmpty(styleValue))
                {
                    lastValueItem = TrackLayerStyle.ValueItems.Last(valueItem => valueItem.DefaultTextStyle != null && valueItem.Value != LinkFileStyleColumnName);
                }
                else
                {
                    lastValueItem = TrackLayerStyle.ValueItems.Last(tmpItem => tmpItem.DefaultTextStyle != null
                        && tmpItem.Value.Equals(styleValue));
                }

                if (lastValueItem != null)
                {
                    editingFeature.ColumnValues[valueStyleMatchColumnName] = lastValueItem.Value;
                    editingFeature.ColumnValues[annotationTextColumnName] = text;
                }
            }
        }

        protected override void OnTrackEnded(TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            if (FileLinkable && (tempBaseShape == null || tempBaseShape.GetWellKnownText() != e.TrackShape.GetWellKnownText()))
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = false;
                Feature tempFeature = TrackShapeLayer.InternalFeatures.FirstOrDefault(f => f.GetWellKnownText().Equals(e.TrackShape.GetWellKnownText()));
                if (openFileDialog.ShowDialog().GetValueOrDefault())
                {
                    tempBaseShape = e.TrackShape;
                    SetLinkFileName(tempFeature, openFileDialog.FileName);
                }
                else
                {
                    TrackShapeLayer.InternalFeatures.Remove(tempFeature);
                }
                SetDefaultColumnValue(AnnotaionStyleType.FileLinkStyle);
            }
            else
            {
                SetDefaultColumnValue();
            }

            var lastFeature = TrackShapeLayer.InternalFeatures.LastOrDefault();
            if (lastFeature != null) lastFeature.Id = NewFeatureName();
            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.OnTrackEndedDescription));
            base.OnTrackEnded(e);
        }

        public void SetLinkFileName(Feature tempFeature, string fileName)
        {
            if (tempFeature.ColumnValues.ContainsKey("LinkFileName"))
            {
                string value = tempFeature.ColumnValues["LinkFileName"];
                if (tempFeature.ColumnValues["LinkFileName"].Contains("||"))
                {
                    string temp = value.Substring(0, 6);
                    tempFeature.ColumnValues["LinkFileName"] = temp + fileName;
                }
                else if (string.IsNullOrEmpty(tempFeature.ColumnValues["LinkFileName"]))
                {
                    var availableValueItem = fileLinkStyle.ValueItems.LastOrDefault();
                    string tempValue = availableValueItem.Value + "||" + fileName;
                    tempFeature.ColumnValues["LinkFileName"] = tempValue;
                }
            }
            else
            {
                var availableValueItem = fileLinkStyle.ValueItems.LastOrDefault();
                string value = availableValueItem.Value + "||" + fileName;
                tempFeature.ColumnValues["LinkFileName"] = value;
            }
        }

        public string NewFeatureName()
        {
            int index = 1;
            const string nameTemplate = "Annotation {0}";
            string currentName = String.Format(CultureInfo.InvariantCulture, nameTemplate, index++);
            while (TrackShapeLayer.InternalFeatures.Any(f => f.Id.Equals(currentName, StringComparison.Ordinal)))
            {
                currentName = String.Format(CultureInfo.InvariantCulture, nameTemplate, index++);
            }

            return currentName;
        }

        protected override InteractiveResult MouseDownCore(InteractionArguments interactionArguments)
        {
            InteractiveResult result = base.MouseDownCore(interactionArguments);
            if (IsInModifyMode && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                trackStartPointShape = new PointShape(interactionArguments.WorldX, interactionArguments.WorldY);
            }
            return result;
        }

        protected override InteractiveResult MouseMoveCore(InteractionArguments interactionArguments)
        {
            InteractiveResult result = base.MouseMoveCore(interactionArguments);
            SetDefaultColumnValue();
            if (trackStartPointShape != null)
            {
                result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                double left = trackStartPointShape.X < interactionArguments.WorldX ? trackStartPointShape.X : interactionArguments.WorldX;
                double right = trackStartPointShape.X > interactionArguments.WorldX ? trackStartPointShape.X : interactionArguments.WorldX;
                double top = trackStartPointShape.Y > interactionArguments.WorldY ? trackStartPointShape.Y : interactionArguments.WorldY;
                double bottom = trackStartPointShape.Y < interactionArguments.WorldY ? trackStartPointShape.Y : interactionArguments.WorldY;
                RectangleShape selectionArea = new RectangleShape(left, top, right, bottom);
                selectionLayer.InternalFeatures.Clear();
                selectionLayer.InternalFeatures.Add(new Feature(selectionArea));
            }
            return result;
        }

        protected override InteractiveResult MouseUpCore(InteractionArguments interactionArguments)
        {
            InteractiveResult result = base.MouseUpCore(interactionArguments);
            if (selectionLayer.InternalFeatures.Count > 0)
            {
                result.ProcessOtherOverlaysMode = ProcessOtherOverlaysMode.DoNotProcessOtherOverlays;
                result.DrawThisOverlay = InteractiveOverlayDrawType.Draw;
                OnSelectionFinished(selectionLayer.InternalFeatures.First().GetBoundingBox());
                selectionLayer.InternalFeatures.Clear();
                trackStartPointShape = null;
            }

            return result;
        }

        protected virtual void OnSelectionFinished(RectangleShape selectionArea)
        {
            EventHandler<TrackEndedTrackInteractiveOverlayEventArgs> handler = SelectionFinished;
            if (handler != null)
            {
                handler(this, new TrackEndedTrackInteractiveOverlayEventArgs(selectionArea));
            }
        }

        //public T GetLatestStyle<T>(bool returnRealStyle = true) where T : Style
        //{
        //    return GetLatestStyle<T>(String.Empty, returnRealStyle);
        //}

        public T GetLatestStyle<T>(AnnotaionStyleType annotaionStyleType, bool returnRealStyle = true) where T : Styles.Style
        {
            ValueItem lastValueItem = null;

            switch (annotaionStyleType)
            {
                case AnnotaionStyleType.LayerStyle:
                    lastValueItem = layerStyle.ValueItems.LastOrDefault();
                    break;
                case AnnotaionStyleType.FileLinkStyle:
                    lastValueItem = fileLinkStyle.ValueItems.LastOrDefault();
                    break;
            }

            if (lastValueItem != null)
            {
                var styles = new Collection<Styles.Style>();

                if (returnRealStyle)
                {
                    styles.Add(lastValueItem.DefaultAreaStyle.CustomAreaStyles.Count > 0 ? lastValueItem.DefaultAreaStyle.CustomAreaStyles[0] : lastValueItem.DefaultAreaStyle);
                    styles.Add(lastValueItem.DefaultLineStyle.CustomLineStyles.Count > 0 ? lastValueItem.DefaultLineStyle.CustomLineStyles[0] : lastValueItem.DefaultLineStyle);
                    styles.Add(lastValueItem.DefaultPointStyle.CustomPointStyles.Count > 0 ? lastValueItem.DefaultPointStyle.CustomPointStyles[0] : lastValueItem.DefaultPointStyle);
                    styles.Add(lastValueItem.DefaultTextStyle.CustomTextStyles.Count > 0 ? lastValueItem.DefaultTextStyle.CustomTextStyles[0] : lastValueItem.DefaultTextStyle);
                }
                else
                {
                    styles.Add(lastValueItem.DefaultAreaStyle);
                    styles.Add(lastValueItem.DefaultLineStyle);
                    styles.Add(lastValueItem.DefaultPointStyle);
                    styles.Add(lastValueItem.DefaultTextStyle);
                }

                return styles.OfType<T>().FirstOrDefault();
            }
            return null;
        }

        public TextStyle GetSpecificTextStyle(string styleValue, bool returnRealTextStyle = true)
        {
            var foundValueItem = layerStyle.ValueItems
                .FirstOrDefault(tmpItem => tmpItem.DefaultTextStyle != null && tmpItem.Value.Equals(styleValue, StringComparison.Ordinal));

            if (foundValueItem != null)
            {
                if (returnRealTextStyle)
                {
                    return foundValueItem.DefaultTextStyle.CustomTextStyles.Count > 0 ? foundValueItem.DefaultTextStyle.CustomTextStyles[0] : foundValueItem.DefaultTextStyle;
                }
                return foundValueItem.DefaultTextStyle;
            }
            return null;
        }

        protected override void DrawTileCore(GeoCanvas geoCanvas)
        {
            LayerTile layerTile = OverlayCanvas.Children.OfType<LayerTile>().FirstOrDefault(tmpTile
                => tmpTile.GetValue(FrameworkElement.NameProperty).Equals("DefaultLayerTile"));

            if (layerTile != null && !layerTile.DrawingLayers.Contains(selectionLayer))
            {
                layerTile.DrawingLayers.Add(selectionLayer);
            }

            base.DrawTileCore(geoCanvas);
        }

        protected override void OnDrawing(DrawingOverlayEventArgs e)
        {
            SetDefaultColumnValue();
            base.OnDrawing(e);
        }

        private static string RandomString()
        {
            return Guid.NewGuid().ToString().Substring(0, 4);
        }

        private void InitValueStyle()
        {
            ValueItem initValueItem = new ValueItem
            {
                Value = RandomString(),
                DefaultAreaStyle = GetDefaultAreaStyle(),
                DefaultLineStyle = GetDefaultLineStyle(),
                DefaultPointStyle = GetDefaultPointStyle(),
                DefaultTextStyle = GetDefaultTextStyle(annotationTextColumnName, PointPlacement.LowerRight)
            };

            ValueItem fileLinkValueItem = new ValueItem
            {
                //Value = LinkFileStyleColumnName,
                Value = RandomString(),
                DefaultAreaStyle = GetDefaultAreaStyle(),
                DefaultLineStyle = GetDefaultLineStyle(),
                DefaultPointStyle = GetDefaultPointStyle(),
                DefaultTextStyle = GetDefaultTextStyle(LinkFileStyleColumnName, PointPlacement.LowerCenter),
            };
            fileLinkValueItem.DefaultPointStyle.Name = "FileLinkStyle";
            fileLinkValueItem.DefaultTextStyle.Name = "FileLinkStyle";

            fileLinkStyle = new TextFirstValueStyle();
            fileLinkStyle.ColumnName = LinkFileStyleColumnName;
            fileLinkStyle.ValueItems.Add(fileLinkValueItem);

            layerStyle = new TextFirstValueStyle { ColumnName = valueStyleMatchColumnName };
            layerStyle.ValueItems.Add(initValueItem);

            TrackShapeLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(layerStyle);
            TrackShapeLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(fileLinkStyle);
            TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = null;
            TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = null;
            TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = null;
            TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultTextStyle = null;

            foreach (var targetLayer in new[] { TrackShapeLayer, TrackShapesInProcessLayer })
            {
                targetLayer.SafeProcess(() =>
                {
                    targetLayer.Columns.Add(new FeatureSourceColumn(LinkFileStyleColumnName));
                    targetLayer.Columns.Add(new FeatureSourceColumn(valueStyleMatchColumnName));
                    targetLayer.Columns.Add(new FeatureSourceColumn(AnnotationTextColumnName));
                });
            }
        }

        private void InitSelectStyle()
        {
            selectionLayer = new InMemoryFeatureLayer();
            selectionLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle
                = new AreaStyle(new GeoPen(GeoColor.SimpleColors.Black, 1f), new GeoSolidBrush(GeoColor.FromArgb(100, GeoColor.StandardColors.White)));
            selectionLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle.OutlinePen.DashPattern.Add(4);
            selectionLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle.OutlinePen.DashPattern.Add(4);
            selectionLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle.OutlinePen.DashStyle = LineDashStyle.Dash;
            selectionLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
        }

        private void ApplyNewStyle<T>(string propertyName, T newStyle, AnnotaionStyleType annotationStyleType = AnnotaionStyleType.LayerStyle) where T : Styles.Style
        {
            ValueItem valueItem = layerStyle.ValueItems.LastOrDefault().CloneDeep();
            if (annotationStyleType == AnnotaionStyleType.FileLinkStyle)
            {
                valueItem = fileLinkStyle.ValueItems.LastOrDefault().CloneDeep();
                newStyle.Name = "FileLinkStyle";
            }
            valueItem.Value = RandomString();
            valueItem.GetType().GetProperty(propertyName).GetSetMethod().Invoke(valueItem, new object[] { newStyle });
            TextStyle textStyle = newStyle as TextStyle;
            if (textStyle != null)
            {
                switch (annotationStyleType)
                {
                    case AnnotaionStyleType.LayerStyle:
                    default:
                        valueItem.DefaultTextStyle.PointPlacement = PointPlacement.LowerRight;
                        break;
                    case AnnotaionStyleType.FileLinkStyle:
                        valueItem.DefaultTextStyle.PointPlacement = PointPlacement.LowerCenter;
                        break;
                }
            }

            switch (annotationStyleType)
            {
                case AnnotaionStyleType.FileLinkStyle:
                    fileLinkStyle.ValueItems.Add(valueItem);
                    break;
                case AnnotaionStyleType.LayerStyle:
                default:
                    layerStyle.ValueItems.Add(valueItem);
                    break;
            }
        }

        private void SetDefaultColumnValue(AnnotaionStyleType annotationStyleType = AnnotaionStyleType.LayerStyle)
        {
            var lastFeature = TrackShapeLayer.InternalFeatures.LastOrDefault();
            switch (annotationStyleType)
            {
                case AnnotaionStyleType.LayerStyle:
                    if (lastFeature != null && !lastFeature.ColumnValues.ContainsKey(valueStyleMatchColumnName))
                    {
                        WellKnownType wktype = lastFeature.GetWellKnownType();
                        var availableValueItem = layerStyle.ValueItems.Last(tmpValueItem =>
                        {
                            switch (wktype)
                            {
                                case WellKnownType.Point:
                                case WellKnownType.Multipoint:
                                    return tmpValueItem.DefaultPointStyle != null;
                                case WellKnownType.Polygon:
                                case WellKnownType.Multipolygon:
                                    return tmpValueItem.DefaultAreaStyle != null;
                                case WellKnownType.Line:
                                case WellKnownType.Multiline:
                                    return tmpValueItem.DefaultLineStyle != null;
                                default: return false;
                            }
                        });

                        lastFeature.ColumnValues.Add(valueStyleMatchColumnName, availableValueItem.Value);
                    }
                    break;
                case AnnotaionStyleType.FileLinkStyle:
                    if (lastFeature != null && lastFeature.GetShape() is PointShape && lastFeature.ColumnValues.ContainsKey(LinkFileStyleColumnName) && !string.IsNullOrEmpty(lastFeature.ColumnValues[LinkFileStyleColumnName]))
                    {
                        var availableValueItem = fileLinkStyle.ValueItems.LastOrDefault();

                        if (!lastFeature.ColumnValues[LinkFileStyleColumnName].Contains("||"))
                        {
                            string value = availableValueItem.Value + "||" + lastFeature.ColumnValues[LinkFileStyleColumnName];
                            lastFeature.ColumnValues[LinkFileStyleColumnName] = value;
                        }
                    }
                    break;
            }
        }

        #region Default styles

        private static IconTextStyle GetDefaultTextStyle(string columnName, PointPlacement pointPlacement)
        {
            return new IconTextStyle
            {
                Font = new GeoFont("Arial", 11),
                TextSolidBrush = new GeoSolidBrush(GeoColor.SimpleColors.Black),
                OverlappingRule = LabelOverlappingRule.AllowOverlapping,
                ForceHorizontalLabelForLine = false,
                SplineType = SplineType.Default,
                DuplicateRule = LabelDuplicateRule.UnlimitedDuplicateLabels,
                SuppressPartialLabels = false,
                LabelAllLineParts = false,
                TextLineSegmentRatio = 0.9,
                IconImageScale = 1,
                Name = GisEditor.LanguageManager.GetStringResource("AnnotationTrackOverlayerLabelStyle1"),
                PointPlacement = pointPlacement,
                TextColumnName = columnName
            };
        }

        private static PointStyle GetDefaultPointStyle()
        {
            return new PointStyle
            {
                SymbolType = PointSymbolType.Circle,
                SymbolSize = 10,
                SymbolSolidBrush = new GeoSolidBrush(GeoColor.SimpleColors.Red),
                SymbolPen = new GeoPen(GeoColor.StandardColors.Transparent, 1),
                Name = GisEditor.LanguageManager.GetStringResource("AnnotationTrackOverlayerPointStyle1")
            };
        }

        private static LineStyle GetDefaultLineStyle()
        {
            return new LineStyle
            {
                OuterPen = new GeoPen(GeoColor.SimpleColors.Black, 2),
                Name = GisEditor.LanguageManager.GetStringResource("AnnotationTrackOverlayerLineStyle1")
            };
        }

        private static AreaStyle GetDefaultAreaStyle()
        {
            return new AreaStyle
            {
                FillSolidBrush = new GeoSolidBrush(GeoColor.StandardColors.White),
                OutlinePen = new GeoPen(GeoColor.SimpleColors.Black, 2),
                Name = GisEditor.LanguageManager.GetStringResource("AnnotationTrackOverlayerAreaStyle1")
            };
        }

        #endregion Default styles
    }
}