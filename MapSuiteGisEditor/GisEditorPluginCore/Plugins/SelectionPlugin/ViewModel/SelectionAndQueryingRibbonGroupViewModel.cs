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
using Microsoft.Windows.Controls.Ribbon;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SelectionAndQueryingRibbonGroupViewModel : ViewModelBase
    {
        private static string reg = @"(\S)([A-Z])";

        private bool isPointChecked;
        private bool isRectangleChecked;
        private bool isPolygonChecked;
        private bool isCircleChecked;
        private bool isLineChecked;
        private Collection<CheckableItemViewModel<SpatialQueryMode>> spatialQueryModeEntities;
        private ObservableCollection<CheckableItemViewModel<FeatureLayer>> layers;
        private ObservedCommand cleanSelectionHistoryCommand;
        private ObservedCommand openDbfViewerWindowCommand;
        private ObservedCommand<RibbonRadioButton> applySelectionModeCommand;
        private ObservedCommand changeSelectionStyleCommand;
        private ObservedCommand copyToEditLayerCommand;
        private ObservedCommand copyToExistingLayerCommand;
        private CompositeStyle selectionCompositeStyle;

        [NonSerialized]
        private BitmapImage selectionStylePreview;
        [NonSerialized]
        private BitmapImage outlineColorPreview;
        [NonSerialized]
        private RelayCommand<CheckableItemViewModel<SpatialQueryMode>> applySpatialQueryModeCommand;
        [NonSerialized]
        private RelayCommand<CheckableItemViewModel<FeatureLayer>> selectFeatureLayerCommand;
        [NonSerialized]
        private ObservedCommand copyToNewLayerCommand;

        static SelectionAndQueryingRibbonGroupViewModel()
        { }

        public SelectionAndQueryingRibbonGroupViewModel()
        {
            spatialQueryModeEntities = InitSpatialQueryModeEntities();
            layers = new ObservableCollection<CheckableItemViewModel<FeatureLayer>>();

            AreaStyle selectionAreaStyle = new AreaStyle(new GeoPen(GeoColor.StandardColors.Yellow, 3));
            selectionAreaStyle.Name = "Selected Area Style";
            LineStyle selectionLineStyle = new LineStyle(new GeoPen(GeoColor.StandardColors.Yellow, 5));
            selectionLineStyle.Name = "Selected Line Style";
            PointStyle selectionPointStyle = new PointStyle();
            selectionPointStyle.Name = "Selected Point Style";
            selectionPointStyle.SymbolPen = new GeoPen(GeoColor.StandardColors.Yellow, 3);
            IconTextStyle selectionTextStyle = new IconTextStyle();
            selectionTextStyle.TextColumnName = SelectionUIPlugin.FeatureIdColumnName;
            selectionTextStyle.TextSolidBrush = new GeoSolidBrush(GeoColor.StandardColors.Yellow);
            selectionTextStyle.Font = new GeoFont("Arial", 7, DrawingFontStyles.Regular);
            selectionTextStyle.CustomTextStyles.Add(new TextStyle(SelectionUIPlugin.FeatureIdColumnName, new GeoFont("Arial", 7, DrawingFontStyles.Regular), new GeoSolidBrush(GeoColor.StandardColors.Yellow)));
            selectionTextStyle.Name = "Selected Text Style";

            selectionCompositeStyle = new CompositeStyle();
            selectionCompositeStyle.Styles.Add(selectionAreaStyle);
            selectionCompositeStyle.Styles.Add(selectionLineStyle);
            selectionCompositeStyle.Styles.Add(selectionPointStyle);
            selectionCompositeStyle.Styles.Add(selectionTextStyle);

            UpdateStylePreview();
        }

        public static string DefaultTitle
        {
            get { return GisEditor.LanguageManager.GetStringResource("SelectFeaturesPluginNoSelection"); }
        }

        public bool IsPointChecked
        {
            get { return isPointChecked; }
            set
            {
                isPointChecked = value;
                RaisePropertyChanged(() => IsPointChecked);
                if (value)
                    ShowClearSelectedFeaturesHint();
            }
        }

        public bool IsRectangleChecked
        {
            get { return isRectangleChecked; }
            set
            {
                isRectangleChecked = value;
                RaisePropertyChanged(() => IsRectangleChecked);
                if (value)
                    ShowClearSelectedFeaturesHint();
            }
        }

        public bool IsPolygonChecked
        {
            get { return isPolygonChecked; }
            set
            {
                isPolygonChecked = value;
                RaisePropertyChanged(() => IsPolygonChecked);
                if (value)
                    ShowClearSelectedFeaturesHint();
            }
        }

        public bool IsCircleChecked
        {
            get { return isCircleChecked; }
            set
            {
                isCircleChecked = value;
                RaisePropertyChanged(() => IsCircleChecked);
                if (value)
                    ShowClearSelectedFeaturesHint();
            }
        }

        public bool IsLineChecked
        {
            get { return isLineChecked; }
            set
            {
                isLineChecked = value;
                RaisePropertyChanged(() => IsLineChecked);
                if (value)
                    ShowClearSelectedFeaturesHint();
            }
        }

        public SelectionTrackInteractiveOverlay SelectionOverlay
        {
            get { return GisEditor.SelectionManager.GetSelectionOverlay(); }
        }

        public Collection<CheckableItemViewModel<SpatialQueryMode>> SpatialQueryModeEntities
        {
            get { return spatialQueryModeEntities; }
        }

        public BitmapImage SelectionStylePreview
        {
            get { return selectionStylePreview; }
            set
            {
                selectionStylePreview = value;
                RaisePropertyChanged(() => SelectionStylePreview);
            }
        }

        public string DisplayText
        {
            get
            {
                string displayText = DefaultTitle;
                int selectedCount = layers.Count(l => l.IsChecked);
                if (selectedCount == 1)
                {
                    displayText = layers.Where(l => l.IsChecked).ElementAt(0).Name;
                }
                else if (selectedCount > 0)
                {
                    displayText = GisEditor.LanguageManager.GetStringResource("SelectionAndQueryViewModelMultipleLayersText");
                }

                return displayText;
            }
        }

        public ObservedCommand ChangeSelectionStyleCommand
        {
            get
            {
                if (changeSelectionStyleCommand == null)
                {
                    changeSelectionStyleCommand = new ObservedCommand(() =>
                    {
                        if (SelectionOverlay != null)
                        {
                            CompositeStyle clonedAreaStyle = (CompositeStyle)SelectionCompositeStyle.CloneDeep();

                            StyleBuilderArguments styleArguments = new StyleBuilderArguments();
                            styleArguments.IsSubStyleReadonly = true;
                            styleArguments.AvailableUIElements = StyleBuilderUIElements.StyleList;
                            styleArguments.AvailableStyleCategories = StyleCategories.Area | StyleCategories.Line | StyleCategories.Point;
                            styleArguments.StyleToEdit = clonedAreaStyle;
                            styleArguments.FeatureLayer = SelectionOverlay.TrackShapeLayer;
                            styleArguments.AppliedCallback = (result) =>
                            {
                                SelectionCompositeStyle = result.CompositeStyle;
                                this.SelectionOverlay.Refresh();
                            };

                            var resultStyle = GisEditor.StyleManager.EditStyle(styleArguments);
                            if (resultStyle != null && resultStyle.CompositeStyle != null)
                            {
                                SelectionCompositeStyle = resultStyle.CompositeStyle;
                                this.SelectionOverlay.Refresh();
                            }
                        }
                    }, () => GisEditor.ActiveMap != null);
                }
                return changeSelectionStyleCommand;
            }
        }

        public RelayCommand<CheckableItemViewModel<FeatureLayer>> SelectFeatureLayerCommand
        {
            get
            {
                if (selectFeatureLayerCommand == null)
                {
                    selectFeatureLayerCommand = new RelayCommand<CheckableItemViewModel<FeatureLayer>>(selectedFeatureLayerViewModel =>
                    {
                        selectedFeatureLayerViewModel.IsChecked = !selectedFeatureLayerViewModel.IsChecked;

                        var selectionTrackOverlay = SelectionOverlay;
                        if (selectionTrackOverlay != null)
                        {
                            selectionTrackOverlay.FilteredLayers.Clear();
                            Layers.Where(l => l.IsChecked).Select(l => l.Value).ForEach(l => selectionTrackOverlay.FilteredLayers.Add(l));
                            RaiseDisplayTextPropertyChanged();
                            GisEditor.UIManager.BeginRefreshPlugins();
                        }
                    });
                }
                return selectFeatureLayerCommand;
            }
        }

        public CompositeStyle SelectionCompositeStyle
        {
            get { return selectionCompositeStyle; }
            set
            {
                selectionCompositeStyle = value;
                UpdateStylePreview();
            }
        }

        public BitmapImage OutlineColorPreview
        {
            get { return outlineColorPreview; }
            set
            {
                outlineColorPreview = value;
                RaisePropertyChanged(() => OutlineColorPreview);
            }
        }

        public ObservableCollection<CheckableItemViewModel<FeatureLayer>> Layers
        {
            get { return layers; }
        }

        public RelayCommand<CheckableItemViewModel<SpatialQueryMode>> ApplySpatialQueryModeCommand
        {
            get
            {
                if (applySpatialQueryModeCommand == null)
                {
                    applySpatialQueryModeCommand = new RelayCommand<CheckableItemViewModel<SpatialQueryMode>>(checkableItem =>
                    {
                        foreach (var spatialEntity in SpatialQueryModeEntities)
                        {
                            spatialEntity.IsChecked = spatialEntity.Value == checkableItem.Value;
                        }
                        if (SelectionOverlay != null)
                        {
                            var oldSpatialQueryMode = SelectionOverlay.SpatialQueryMode;
                            SelectionOverlay.SpatialQueryMode = checkableItem.Value;
                            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.ApplySpatialQueryModeCommandDescription));
                        }
                    });
                }
                return applySpatialQueryModeCommand;
            }
        }

        public ObservedCommand<RibbonRadioButton> ApplySelectionModeCommand
        {
            get
            {
                if (applySelectionModeCommand == null)
                {
                    applySelectionModeCommand = new ObservedCommand<RibbonRadioButton>(sender =>
                    {
                        InitializeSelectionOverlay();
                        var selectionOverlay = SelectionOverlay;
                        if (SelectionOverlay.FilteredLayers.Count == 0)
                        {
                            UncheckAllSelectionButton();
                            System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("SelectionPluginNoLayerWarningMessage"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"));
                            return;
                        }

                        if (selectionOverlay != null)
                        {
                            var currentMap = GisEditor.ActiveMap;
                            currentMap.DisableInteractiveOverlaysExclude(selectionOverlay);
                            var oldTrackMode = selectionOverlay.TrackMode;
                            if (sender.IsChecked.Value)
                            {
                                switch (sender.Name)
                                {
                                    case "pointSelection":
                                        selectionOverlay.TrackMode = TrackMode.Point;
                                        currentMap.Cursor = GisEditorCursors.DrawPoint;
                                        break;
                                    case "rectangleSelection":
                                        selectionOverlay.TrackMode = TrackMode.Rectangle;
                                        currentMap.Cursor = GisEditorCursors.DrawRectangle;
                                        break;
                                    case "squareSelection":
                                        selectionOverlay.TrackMode = TrackMode.Square;
                                        currentMap.Cursor = GisEditorCursors.DrawSqure;
                                        break;
                                    case "ellipseSelection":
                                        selectionOverlay.TrackMode = TrackMode.Ellipse;
                                        currentMap.Cursor = GisEditorCursors.DrawEllipse;
                                        break;
                                    case "circleSelection":
                                        selectionOverlay.TrackMode = TrackMode.Circle;
                                        currentMap.Cursor = GisEditorCursors.DrawCircle;
                                        break;
                                    case "polygonSelection":
                                        selectionOverlay.TrackMode = TrackMode.Polygon;
                                        currentMap.Cursor = GisEditorCursors.DrawPolygon;
                                        break;
                                    case "lineSelection":
                                        selectionOverlay.TrackMode = TrackMode.Line;
                                        currentMap.Cursor = GisEditorCursors.DrawLine;
                                        break;
                                }
                            }
                            else
                            {
                                selectionOverlay.TrackMode = TrackMode.None;
                            }

                            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.ApplySelectionModeCommandDescription));
                        }
                    }, (sender) => GisEditor.ActiveMap != null);
                }
                return applySelectionModeCommand;
            }
        }

        public ObservedCommand OpenDbfViewerWindowCommand
        {
            get
            {
                if (openDbfViewerWindowCommand == null)
                {
                    openDbfViewerWindowCommand = new ObservedCommand(() =>
                    {
                        if (SelectionOverlay != null)
                        {
                            var content = new DataViewerUserControl(null, SelectionOverlay.GetSelectedFeaturesGroup().Keys);
                            content.IsHighlightFeatureOnly = true;
                            content.ShowDock();
                        }
                    }, CheckHasFeatureSelected);
                }
                return openDbfViewerWindowCommand;
            }
        }

        public ObservedCommand CleanSelectionHistoryCommand
        {
            get
            {
                if (cleanSelectionHistoryCommand == null)
                {
                    cleanSelectionHistoryCommand = new ObservedCommand(() =>
                    {
                        GisEditor.SelectionManager.ClearSelectedFeatures();
                        GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.CleanSelectionHistoryCommandDescription));
                    }, CheckHasFeatureSelected);
                }
                return cleanSelectionHistoryCommand;
            }
        }

        public ObservedCommand CopyToNewLayerCommand
        {
            get
            {
                if (copyToNewLayerCommand == null)
                {
                    copyToNewLayerCommand = new ObservedCommand(() =>
                    {
                        Collection<FeatureLayer> layers = GisEditor.SelectionManager.GetSelectionOverlay().TargetFeatureLayers;
                        Collection<FeatureLayer> selectedFeaturesLayers = new Collection<FeatureLayer>();
                        foreach (FeatureLayer layer in layers)
                        {
                            Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures(layer);
                            if (features.Count > 0) selectedFeaturesLayers.Add(layer);
                        }
                        HighlightedFeaturesHelper.CopyToNewLayer(selectedFeaturesLayers);
                    }, () =>
                    {
                        var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
                        return selectionOverlay != null && selectionOverlay.HighlightFeatureLayer.InternalFeatures.Count > 0;
                    });
                }

                return copyToNewLayerCommand;
            }
        }

        public ObservedCommand CopyToEditLayerCommand
        {
            get
            {
                if (copyToEditLayerCommand == null)
                {
                    copyToEditLayerCommand = new ObservedCommand(() =>
                    {
                        Collection<FeatureLayer> selectedFeatureLayers = new Collection<FeatureLayer>();
                        foreach (FeatureLayer layer in GisEditor.ActiveMap.GetFeatureLayers())
                        {
                            Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures(layer);
                            if (features.Count > 0) selectedFeatureLayers.Add(layer);
                        }
                        HighlightedFeaturesHelper.CopyToEditLayer(selectedFeatureLayers);
                    }, () =>
                    {
                        if (GisEditor.ActiveMap != null)
                        {
                            Collection<FeatureLayer> selectedFeaturesLayers = new Collection<FeatureLayer>();
                            foreach (FeatureLayer layer in GisEditor.ActiveMap.GetFeatureLayers())
                            {
                                Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures(layer);
                                if (features.Count > 0) selectedFeaturesLayers.Add(layer);
                            }
                            return HighlightedFeaturesHelper.CheckCopyToEditLayerIsAvailable(selectedFeaturesLayers);
                        }
                        else return false;
                    });
                }
                return copyToEditLayerCommand;
            }
        }

        public ObservedCommand CopyToExistingLayerCommand
        {
            get
            {
                if (copyToExistingLayerCommand == null)
                {
                    copyToExistingLayerCommand = new ObservedCommand(() =>
                    {
                        Collection<FeatureLayer> selectedFeatureLayers = new Collection<FeatureLayer>();
                        foreach (FeatureLayer layer in GisEditor.ActiveMap.GetFeatureLayers())
                        {
                            Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures(layer);
                            if (features.Count > 0) selectedFeatureLayers.Add(layer);
                        }
                        HighlightedFeaturesHelper.CopyToEditLayer(selectedFeatureLayers);
                    }, () =>
                    {
                        if (GisEditor.ActiveMap != null)
                        {
                            Collection<FeatureLayer> selectedFeaturesLayers = new Collection<FeatureLayer>();
                            foreach (FeatureLayer layer in GisEditor.ActiveMap.GetFeatureLayers())
                            {
                                Collection<Feature> features = GisEditor.SelectionManager.GetSelectedFeatures(layer);
                                if (features.Count > 0) selectedFeaturesLayers.Add(layer);
                            }
                            return HighlightedFeaturesHelper.CheckCopyToEditLayerIsAvailable(selectedFeaturesLayers);
                        }
                        else return false;
                    });
                }
                return copyToExistingLayerCommand;
            }
        }

        internal void UncheckAllSelectionButton()
        {
            IsPointChecked = false;
            IsRectangleChecked = false;
            IsPolygonChecked = false;
            IsCircleChecked = false;
            IsLineChecked = false;
        }

        public void RaiseDisplayTextPropertyChanged()
        {
            RaisePropertyChanged(() => DisplayText);
        }

        private void UpdateStylePreview()
        {
            if (SelectionOverlay != null)
            {
                SelectionOverlay.HighlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = null;
                SelectionOverlay.HighlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = null;
                SelectionOverlay.HighlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = null;
                SelectionOverlay.HighlightFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultTextStyle = null;
                SelectionOverlay.HighlightFeatureLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Clear();
                foreach (var tempStyle in SelectionCompositeStyle.Styles)
                {
                    SelectionOverlay.HighlightFeatureLayer.ZoomLevelSet.ZoomLevel01.CustomStyles.Add(tempStyle);
                }
            }
            OutlineColorPreview = SelectionCompositeStyle.GetPreviewImage(32, 32);
        }

        private static Collection<CheckableItemViewModel<SpatialQueryMode>> InitSpatialQueryModeEntities()
        {
            Collection<CheckableItemViewModel<SpatialQueryMode>> spatialQueryModeEntities = new Collection<CheckableItemViewModel<SpatialQueryMode>>();
            foreach (SpatialQueryMode spatialQueryMode in Enum.GetValues(typeof(SpatialQueryMode)))
            {
                CheckableItemViewModel<SpatialQueryMode> spatialQueryModeEntity = new CheckableItemViewModel<SpatialQueryMode>(spatialQueryMode, false, (mode) => Regex.Replace(mode.ToString(), reg, "$1 $2"));
                spatialQueryModeEntities.Add(spatialQueryModeEntity);
            }
            spatialQueryModeEntities.FirstOrDefault(tmpMode => tmpMode.Value == SpatialQueryMode.Touching).IsChecked = true;
            return spatialQueryModeEntities;
        }

        private void UpdateStyleSelectionPreview(Styles.Style style)
        {
            Task.Factory.StartNew(obj =>
            {
                var targetStyle = (Styles.Style)obj;
                var imageBuffer = StyleHelper.GetImageBufferFromStyle(targetStyle);
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var imageSource = ThinkGeo.MapSuite.GisEditor.Plugins.StyleHelper.ConvertToImageSource(imageBuffer);
                        SelectionStylePreview = imageSource;
                    }), DispatcherPriority.Background, null);
                }
            }, style);
        }

        private void InitializeSelectionOverlay()
        {
            var overlay = GisEditor.SelectionManager.GetSelectionOverlay();
            overlay.TargetFeatureLayers.Clear();
            foreach (var featureLayer in GisEditor.ActiveMap.GetFeatureLayers(true))
            {
                overlay.TargetFeatureLayers.Add(featureLayer);
            }
            overlay.TrackStarting -= SelectionTrackOverlay_TrackStarting;
            overlay.TrackStarting += SelectionTrackOverlay_TrackStarting;
            overlay.TrackEnded -= SelectionTrackOverlay_TrackEnded;
            overlay.TrackEnded += SelectionTrackOverlay_TrackEnded;
        }

        private void SelectionTrackOverlay_TrackEnded(object sender, TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            if (SelectionOverlay.TrackMode == TrackMode.Point
                || SelectionOverlay.TrackMode == TrackMode.Rectangle
                || SelectionOverlay.TrackMode == TrackMode.Polygon
                || SelectionOverlay.TrackMode == TrackMode.Circle
                || SelectionOverlay.TrackMode == TrackMode.Line)
            {
                // Force execute all the commands's CanExecute()
                CommandManager.InvalidateRequerySuggested();
            }
            if (SelectionOverlay.FilteredLayers.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("SelectionPluginNoLayerWarningMessage"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"));
            }
        }

        private void SelectionTrackOverlay_TrackStarting(object sender, TrackStartingTrackInteractiveOverlayEventArgs e)
        {
            var overlay = (SelectionTrackInteractiveOverlay)sender;
            overlay.TargetFeatureLayers.Clear();
            foreach (var featureLayer in GisEditor.ActiveMap.GetFeatureLayers(true))
            { overlay.TargetFeatureLayers.Add(featureLayer); }
        }

        private bool CheckHasFeatureSelected()
        {
            return SelectionOverlay != null && SelectionOverlay.HighlightFeatureLayer.InternalFeatures.Count > 0;
        }

        private void ShowClearSelectedFeaturesHint()
        {
            if (MapHelper.ShowHintWindow("ShowClearFeaturesHint"))
            {
                var title = GisEditor.LanguageManager.GetStringResource("SelectionAndQueryViewModelClearingSelectedFeaturesTitle");
                var description = GisEditor.LanguageManager.GetStringResource("SelectionAndQueryViewModelClearingSelectedFeaturesDescription");
                var steps = new Collection<String>()
                {
                        GisEditor.LanguageManager.GetStringResource("SelectionAndQueryViewModelRightClickMapText"),
                        GisEditor.LanguageManager.GetStringResource("SelectionAndQueryViewModelChooseClearText")
                };

                GisEditorHintWindow gisEditorHintWindow = new GisEditorHintWindow(title, description, steps, "/GisEditorPluginCore;component/Images/Hints/Clear Selecting by Context Menu.gif");
                gisEditorHintWindow.ShowDialog();
                var result = (gisEditorHintWindow.DataContext as GisEditorHintViewModel).DonotShowAgain;
                MapHelper.SetShowHint("ShowClearFeaturesHint", !result);
            }
        }
    }
}
