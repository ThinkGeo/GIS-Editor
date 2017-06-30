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
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class MeasureRibbonGroupViewModel : ViewModelBase
    {
        private PolygonTrackMode selectedPolygonTrackMode;
        private Collection<PolygonTrackMode> polygonTrackModes;
        private ObservedCommand clearCommand;
        private ObservedCommand undoCommand;
        private ObservedCommand redoCommand;
        private ObservedCommand changeMeasurementStyleCommand;
        private ObservedCommand moveMeasureCommand;
        private ObservedCommand displayHideCommand;
        private string displayHideText;

        [NonSerialized]
        private BitmapImage measureLineStylePreview;
        [NonSerialized]
        private BitmapImage measurementStylePreview;
        [NonSerialized]
        private ImageSource displayHidePreview;
        [NonSerialized]
        private RelayCommand<string> changeMeasureModeCommand;

        private Collection<MeasuringInMode> measuringModes;

        public MeasureRibbonGroupViewModel()
        {
            polygonTrackModes = new Collection<PolygonTrackMode>();
            polygonTrackModes.Add(PolygonTrackMode.LineWithFill);
            polygonTrackModes.Add(PolygonTrackMode.LineOnly);
            selectedPolygonTrackMode = polygonTrackModes.First();
            measuringModes = new Collection<MeasuringInMode>();
            foreach (MeasuringInMode measuringMode in Enum.GetValues(typeof(MeasuringInMode)))
            {
                measuringModes.Add(measuringMode);
            }
            UpdateStylePreview();
            displayHideText = "Hide Measurements";
            displayHidePreview = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/hideEye.png", UriKind.RelativeOrAbsolute));
        }

        public MeasuringInMode SelectedMeasuringMode
        {
            get { return MeasureOverlay != null ? MeasureOverlay.MeasuringMode : MeasuringInMode.DecimalDegree; }
            set
            {
                MeasureOverlay.MeasuringMode = value;
                RaisePropertyChanged(() => SelectedMeasuringMode);
            }
        }

        public Collection<MeasuringInMode> MeasuringModes
        {
            get { return measuringModes; }
        }

        public PolygonTrackMode SelectedPolygonTrackMode
        {
            get { return selectedPolygonTrackMode; }
            set
            {
                if (selectedPolygonTrackMode != value)
                {
                    selectedPolygonTrackMode = value;
                    if (MeasureOverlay != null)
                    {
                        MeasureOverlay.PolygonTrackMode = value;
                    }
                    RaisePropertyChanged(() => SelectedPolygonTrackMode);
                }
            }
        }

        public Collection<PolygonTrackMode> PolygonTrackModes
        {
            get { return polygonTrackModes; }
        }

        public MeasureTrackInteractiveOverlay MeasureOverlay
        {
            get
            {
                MeasureTrackInteractiveOverlay measurementOverlay = null;
                if (GisEditor.ActiveMap != null && !GisEditor.ActiveMap.InteractiveOverlays.Contains("MeasurementOverlay"))
                {
                    measurementOverlay = new MeasureTrackInteractiveOverlay();
                    measurementOverlay.RenderMode = MeasureSetting.Instance.UseGdiPlusInsteadOfDrawingVisual ? RenderMode.GdiPlus : RenderMode.DrawingVisual;
                    measurementOverlay.MeasuringMode = MeasureUIPlugin.MeasuringMode;
                    measurementOverlay.ParentMap = GisEditor.ActiveMap;
                    GisEditor.ActiveMap.InteractiveOverlays.Insert(0, "MeasurementOverlay", measurementOverlay);
                }
                else if (GisEditor.ActiveMap != null)
                {
                    measurementOverlay = GisEditor.ActiveMap.InteractiveOverlays["MeasurementOverlay"] as MeasureTrackInteractiveOverlay;
                }

                return measurementOverlay;
            }
        }

        public BitmapImage MeasurementStylePreview
        {
            get { return measurementStylePreview; }
            set
            {
                measurementStylePreview = value;
                RaisePropertyChanged(() => MeasurementStylePreview);
            }
        }

        public string DisplayHideText
        {
            get { return displayHideText; }
            set
            {
                displayHideText = value;
                RaisePropertyChanged(() => DisplayHideText);
            }
        }

        public ImageSource DisplayHidePreview
        {
            get { return displayHidePreview; }
            set
            {
                displayHidePreview = value;
                RaisePropertyChanged(() => DisplayHidePreview);
            }
        }

        public CompositeStyle MeasurementStyle
        {
            get
            {
                return MeasureOverlay != null ? MeasureOverlay.MeasurementStyle : MeasureTrackInteractiveOverlay.GetInitialCompositeStyle();
            }
            set
            {
                MeasureOverlay.MeasurementStyle = value;
                UpdateStylePreview();
            }
        }

        public DistanceUnit SelectedDistanceUnit
        {
            get { return MeasureSetting.Instance.SelectedDistanceUnit; }
            set
            {
                if (MeasureOverlay != null)
                {
                    MeasureSetting.Instance.SelectedDistanceUnit = value;
                    RaisePropertyChanged(() => SelectedDistanceUnit);
                }
            }
        }

        public AreaUnit SelectedAreaUnit
        {
            get { return MeasureSetting.Instance.SelectedAreaUnit; }
            set
            {
                if (MeasureOverlay != null)
                {
                    MeasureSetting.Instance.SelectedAreaUnit = value;
                    RaisePropertyChanged(() => SelectedAreaUnit);
                }
            }
        }

        public ObservedCommand DisplayHideCommand
        {
            get
            {
                if (displayHideCommand == null)
                {
                    displayHideCommand = new ObservedCommand(() =>
                    {
                        MeasureOverlay.IsVisible = !MeasureOverlay.IsVisible;
                        DisplayHideText = MeasureOverlay.IsVisible ? "Hide Measurements" : "Display Measurements";
                        DisplayHidePreview = MeasureOverlay.IsVisible ? new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/hideEye.png", UriKind.RelativeOrAbsolute)) : new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/displayEye.png", UriKind.RelativeOrAbsolute));
                    }, () => MeasureOverlay != null);
                }
                return displayHideCommand;
            }
        }

        public ObservedCommand MoveMeasureCommand
        {
            get
            {
                if (moveMeasureCommand == null)
                {
                    moveMeasureCommand = new ObservedCommand(() =>
                    {
                        if (MeasureOverlay != null)
                        {
                            var extendedMap = GisEditor.ActiveMap;
                            extendedMap.DisableInteractiveOverlaysExclude(MeasureOverlay);
                            if (!MeasureOverlay.IsVisible)
                            {
                                MeasureOverlay.IsVisible = true;
                            }

                            MeasureOverlay.TrackMode = TrackMode.Custom;
                            MeasureOverlay.MeasureCustomeMode = MeasureCustomeMode.Move;

                            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.MoveMeasureCommandDescription));
                        }
                    }, () =>
                    {
                        var measureOverlay = MeasureOverlay;
                        return measureOverlay != null && measureOverlay.ShapeLayer.MapShapes.Count > 0;
                    });
                }

                return moveMeasureCommand;
            }
        }

        public ObservedCommand ClearCommand
        {
            get
            {
                if (clearCommand == null)
                {
                    clearCommand = new ObservedCommand(() =>
                    {
                        var measureOverlay = MeasureOverlay;
                        if (measureOverlay != null)
                        {
                            measureOverlay.MeasureCustomeMode = MeasureCustomeMode.Select;
                            if (!measureOverlay.IsVisible) measureOverlay.IsVisible = true;
                            measureOverlay.ShapeLayer.MapShapes.Clear();
                            measureOverlay.History.Clear();
                            measureOverlay.Refresh();
                            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.ClearCommandDescription));
                        }
                    }, () =>
                    {
                        var measureOverlay = MeasureOverlay;
                        return measureOverlay != null && measureOverlay.ShapeLayer.MapShapes.Count > 0;
                    });
                }
                return clearCommand;
            }
        }

        public ObservedCommand UndoCommand
        {
            get
            {
                if (undoCommand == null)
                {
                    undoCommand = new ObservedCommand(() =>
                    {
                        var measureOverlay = MeasureOverlay;
                        if (measureOverlay != null)
                        {
                            measureOverlay.MeasureCustomeMode = MeasureCustomeMode.Select;
                            measureOverlay.Rollback();
                            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.UndoCommandDescription));
                        }
                    }, () =>
                    {
                        var measureOverlay = MeasureOverlay;
                        return (measureOverlay != null) ? measureOverlay.CanRollback : false;
                    });
                }
                return undoCommand;
            }
        }

        public ObservedCommand RedoCommand
        {
            get
            {
                if (redoCommand == null)
                {
                    redoCommand = new ObservedCommand(() =>
                    {
                        var measureOverlay = MeasureOverlay;
                        if (measureOverlay != null)
                        {
                            measureOverlay.MeasureCustomeMode = MeasureCustomeMode.Select;
                            measureOverlay.Forward();
                            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.RedoCommandDescription));
                        }
                    }, () =>
                    {
                        var measureOverlay = MeasureOverlay;
                        return (measureOverlay != null) ? measureOverlay.CanForward : false;
                    });
                }
                return redoCommand;
            }
        }

        public RelayCommand<string> ChangeMeasureModeCommand
        {
            get
            {
                if (changeMeasureModeCommand == null)
                {
                    changeMeasureModeCommand = new RelayCommand<string>(name =>
                    {
                        if (MeasureOverlay != null)
                        {
                            var extendedMap = GisEditor.ActiveMap;
                            extendedMap.DisableInteractiveOverlaysExclude(MeasureOverlay);
                            if (!MeasureOverlay.IsVisible)
                            {
                                MeasureOverlay.IsVisible = true;
                            }

                            var oldTrackMode = MeasureOverlay.TrackMode;
                            switch (name)
                            {
                                case "pointMeasure":
                                    MeasureOverlay.TrackMode = TrackMode.Point;
                                    break;

                                case "rectangleMeasure":
                                    MeasureOverlay.TrackMode = TrackMode.Rectangle;
                                    break;

                                case "squareMeasure":
                                    MeasureOverlay.TrackMode = TrackMode.Square;
                                    break;

                                case "ellipseMeasure":
                                    MeasureOverlay.TrackMode = TrackMode.Ellipse;
                                    break;

                                case "circleMeasure":
                                    MeasureOverlay.TrackMode = TrackMode.Circle;
                                    break;

                                case "polygonMeasure":
                                    MeasureOverlay.TrackMode = TrackMode.Polygon;
                                    break;

                                case "lineMeasure":
                                    MeasureOverlay.TrackMode = TrackMode.Line;
                                    break;

                                case "selectMeasure":
                                    MeasureOverlay.TrackMode = TrackMode.Custom;
                                    break;
                            }

                            MeasureOverlay.MeasureCustomeMode = MeasureCustomeMode.Select;

                            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.ChangeMeasureModeCommandDescription));
                        }
                    });
                }
                return changeMeasureModeCommand;
            }
        }

        public ObservedCommand ChangeMeasurementStyleCommand
        {
            get
            {
                if (changeMeasurementStyleCommand == null)
                {
                    changeMeasurementStyleCommand = new ObservedCommand(() =>
                    {
                        if (MeasureOverlay != null && MeasureOverlay.MeasurementStyle != null)
                        {
                            CompositeStyle clonedAreaStyle = (CompositeStyle)MeasureOverlay.MeasurementStyle.CloneDeep();

                            StyleBuilderArguments styleArguments = new StyleBuilderArguments();
                            styleArguments.IsSubStyleReadonly = true;
                            styleArguments.AvailableUIElements = StyleBuilderUIElements.StyleList;
                            styleArguments.AvailableStyleCategories = StyleCategories.Area;
                            styleArguments.FeatureLayer = MeasureOverlay.TrackShapeLayer;
                            styleArguments.ColumnNames.Add("Result");
                            styleArguments.StyleToEdit = clonedAreaStyle;
                            styleArguments.AppliedCallback = (result) =>
                            {
                                MeasurementStyle = result.CompositeStyle;
                            };

                            var resultStyle = GisEditor.StyleManager.EditStyle(styleArguments);
                            if (resultStyle != null && resultStyle.CompositeStyle != null)
                            {
                                MeasurementStyle = resultStyle.CompositeStyle;
                            }
                        }
                    }, () => GisEditor.ActiveMap != null);
                }
                return changeMeasurementStyleCommand;
            }
        }

        public void UpdateStylePreview()
        {
            if (MeasureOverlay != null)
            {
                MeasureOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = null;
                MeasureOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = null;
                MeasureOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = null;
                MeasureOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultTextStyle = null;

                MeasureOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Clear();
                foreach (var item in MeasureOverlay.MeasurementStyle.Styles)
                {
                    MeasureOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(item);
                }
            }
            MeasurementStylePreview = MeasurementStyle.GetPreviewImage(32, 32);
        }
    }
}