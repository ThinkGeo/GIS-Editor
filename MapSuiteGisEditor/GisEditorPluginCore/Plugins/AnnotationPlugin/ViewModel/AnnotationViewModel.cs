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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class AnnotationViewModel : ViewModelBase
    {
        private readonly string linkFileName = "Link File";
        /// <summary>
        /// the first item is undo list while the second is redo list.
        /// </summary>

        private string displayHideText;
        private Dictionary<GisEditorWpfMap, StateController<AnnotationSnapshot>> mapSnapshots;
        private CancellationTokenSource cancellationTokenSource;
        private ImageSource annotationPreview;
        private ImageSource displayHidePreview;
        private Collection<TrackModeViewModel> trackModes;
        private TrackModeViewModel selectedMode;

        private ObservedCommand undoCommand;
        private ObservedCommand redoCommand;
        private ObservedCommand deleteCommand;
        private ObservedCommand clearCommand;
        private ObservedCommand displayHideCommand;
        private ObservedCommand convertToAnnotationCommand;
        private ObservedCommand<bool> selectMoveCommand;
        private ObservedCommand changeAnnotationStyleCommand;

        public AnnotationViewModel()
        {
            mapSnapshots = new Dictionary<GisEditorWpfMap, StateController<AnnotationSnapshot>>();
            trackModes = new Collection<TrackModeViewModel>();
            trackModes.Add(new TrackModeViewModel("Point", TrackMode.Point, "/GisEditorPluginCore;component/Images/draw_points.png"));
            trackModes.Add(new TrackModeViewModel("Line", TrackMode.Line, "/GisEditorPluginCore;component/Images/draw_line.png"));
            trackModes.Add(new TrackModeViewModel("Polygon", TrackMode.Polygon, "/GisEditorPluginCore;component/Images/Draw_Polygon.png"));
            trackModes.Add(new TrackModeViewModel("Circle", TrackMode.Circle, "/GisEditorPluginCore;component/Images/Draw_Circle.png"));
            trackModes.Add(new TrackModeViewModel("Square", TrackMode.Square, "/GisEditorPluginCore;component/Images/Draw_Square.png"));
            trackModes.Add(new TrackModeViewModel("Rectangle", TrackMode.Rectangle, "/GisEditorPluginCore;component/Images/Draw_Rectangle.png"));
            trackModes.Add(new TrackModeViewModel("Ellipse", TrackMode.Ellipse, "/GisEditorPluginCore;component/Images/Draw_Ellipse.png"));
            trackModes.Add(new TrackModeViewModel("Label", TrackMode.Custom, "/GisEditorPluginCore;component/Images/draw_text.png"));
            trackModes.Add(new TrackModeViewModel(linkFileName, TrackMode.Point, "/GisEditorPluginCore;component/Images/linkFile.png"));
            displayHideText = "Hide Annotations";
            displayHidePreview = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/hideEye.png", UriKind.RelativeOrAbsolute));
        }

        public Collection<TrackModeViewModel> TrackModes
        {
            get { return trackModes; }
        }

        public TrackModeViewModel SelectedMode
        {
            get { return selectedMode; }
            set
            {
                if (GisEditor.ActiveMap != null)
                {
                    selectedMode = value;
                    RaisePropertyChanged(() => SelectedMode);
                    if (value != null)
                    {
                        CurrentAnnotationOverlayTrackMode = selectedMode.Mode;
                        if (value.Name == linkFileName)
                        {
                            CurrentAnnotationOverlay.FileLinkable = true;
                        }
                        else
                        {
                            CurrentAnnotationOverlay.FileLinkable = false;
                        }
                        MarkerHelper.AddMarkerOverlayIfNotExisting(GisEditor.ActiveMap);
                        DisableSwitcher();
                        SyncUIState();
                        GisEditor.ActiveMap.DisableInteractiveOverlaysExclude(CurrentAnnotationOverlay);
                    }
                    else
                    {
                        CurrentAnnotationOverlayTrackMode = TrackMode.None;
                    }
                }
            }
        }

        public ObservedCommand ChangeAnnotationStyleCommand
        {
            get
            {
                if (changeAnnotationStyleCommand == null)
                {
                    changeAnnotationStyleCommand = new ObservedCommand(() =>
                    {
                        AnnotationHelper.EditAnnotationStyle<TextStyle>(AnnotaionStyleType.LayerStyle);
                    }, () => GisEditor.ActiveMap != null);
                }
                return changeAnnotationStyleCommand;
            }
        }

        internal AnnotationTrackInteractiveOverlay CurrentAnnotationOverlay
        {
            get
            {
                if (GisEditor.ActiveMap != null && GisEditor.ActiveMap.EditOverlay != null)
                {
                    var annotationOverlay = GisEditor.ActiveMap.InteractiveOverlays
                                                                      .OfType<AnnotationTrackInteractiveOverlay>()
                                                                      .FirstOrDefault();
                    if (annotationOverlay == null)
                    {
                        annotationOverlay = new AnnotationTrackInteractiveOverlay();
                        annotationOverlay.TrackEnded += AnnotationOverlay_TrackEnded;
                        annotationOverlay.SelectionFinished += new EventHandler<TrackEndedTrackInteractiveOverlayEventArgs>(AnnotationOverlay_SelectionFinished);
                        GisEditor.ActiveMap.EditOverlay.FeatureEdited -= new EventHandler<FeatureEditedEditInteractiveOverlayEventArgs>(EditOverlay_FeatureEdited);
                        GisEditor.ActiveMap.EditOverlay.FeatureEdited += new EventHandler<FeatureEditedEditInteractiveOverlayEventArgs>(EditOverlay_FeatureEdited);
                        GisEditor.ActiveMap.InteractiveOverlays.Insert(0, "annotationOverlay", annotationOverlay);
                        InitializeStyleIcons();
                    }
                    if (!mapSnapshots.ContainsKey(GisEditor.ActiveMap))
                    {
                        mapSnapshots.Add(GisEditor.ActiveMap, new StateController<AnnotationSnapshot>());
                        TakeSnapshot();
                    }
                    return annotationOverlay;
                }

                return null;
            }
        }

        internal EditInteractiveOverlay CurrentEditOverlay
        {
            get
            {
                if (GisEditor.ActiveMap != null)
                {
                    return GisEditor.ActiveMap.EditOverlay;
                }
                return null;
            }
        }

        internal TrackMode CurrentAnnotationOverlayTrackMode
        {
            get
            {
                if (CurrentAnnotationOverlay != null)
                {
                    return CurrentAnnotationOverlay.TrackMode;
                }
                else return TrackMode.None;
            }
            set
            {
                MarkerHelper.CommitTextAnnotations();
                AnnotationHelper.CommitEdit();

                var annotationOverlay = CurrentAnnotationOverlay;
                if (annotationOverlay != null)
                {
                    if (value != TrackMode.None)
                    {
                        GisEditor.ActiveMap.DisableInteractiveOverlaysExclude(annotationOverlay);
                    }

                    bool isChanged = false;
                    if (annotationOverlay.IsInModifyMode)
                    {
                        annotationOverlay.IsInModifyMode = false;
                        isChanged = true;
                    }

                    if (annotationOverlay.TrackMode != value)
                    {
                        annotationOverlay.TrackMode = value;
                        isChanged = true;
                    }

                    if (isChanged)
                    {
                        GisEditor.UIManager.RefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.CurrentAnnotationOverlayTrackModeDescription));
                    }
                }

                if (value != TrackMode.None)
                {
                    DisableSwitcher();
                }

                SyncUIState();
            }
        }

        public bool IsInModifyMode
        {
            get
            {
                if (CurrentAnnotationOverlay != null)
                {
                    return CurrentAnnotationOverlay.IsInModifyMode;
                }
                else return false;
            }
            set
            {
                CurrentAnnotationOverlay.IsInModifyMode = value;
            }
        }

        public bool CanUndo
        {
            get
            {
                return CurrentAnnotationOverlay != null && mapSnapshots[GisEditor.ActiveMap].CanRollBack;
            }
        }

        public bool CanRedo
        {
            get
            {
                return CurrentAnnotationOverlay != null && mapSnapshots[GisEditor.ActiveMap].CanForward;
            }
        }

        public bool AnnotationsExist
        {
            get
            {
                if (CurrentAnnotationOverlay != null)
                {
                    return CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.Count > 0;
                }
                else
                {
                    return false;
                }
            }
        }

        public ImageSource AnnotationPreview
        {
            get { return annotationPreview; }
            set
            {
                annotationPreview = value;
                RaisePropertyChanged(() => AnnotationPreview);
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

        public bool AnyAnnotationSelected
        {
            get
            {
                if (CurrentEditOverlay != null && CurrentEditOverlay.EditShapesLayer.InternalFeatures.Count > 0)
                {
                    return true;
                }
                else if (MarkerHelper.CurrentMarkerOverlay != null && MarkerHelper.CurrentMarkerOverlay.Markers.Count > 0)
                {
                    return true;
                }
                else return false;
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

        public ObservedCommand DisplayHideCommand
        {
            get
            {
                if (displayHideCommand == null)
                {
                    displayHideCommand = new ObservedCommand(() =>
                    {
                        CurrentAnnotationOverlay.IsVisible = !CurrentAnnotationOverlay.IsVisible;
                        DisplayHideText = CurrentAnnotationOverlay.IsVisible ? "Hide Annotations" : "Display Annotations";
                        DisplayHidePreview = CurrentAnnotationOverlay.IsVisible ? new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/hideEye.png", UriKind.RelativeOrAbsolute)) : new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/displayEye.png", UriKind.RelativeOrAbsolute));
                    }, () => CurrentAnnotationOverlay != null);
                }
                return displayHideCommand;
            }
        }

        public ObservedCommand<bool> SelectMoveCommand
        {
            get
            {
                if (selectMoveCommand == null)
                {
                    selectMoveCommand = new ObservedCommand<bool>(isChecked =>
                    {
                        SelectedMode = null;
                        if (isChecked)
                        {
                            CurrentAnnotationOverlayTrackMode = TrackMode.None;
                            GisEditor.ActiveMap.Cursor = Cursors.Select;
                        }
                        CurrentAnnotationOverlay.IsInModifyMode = isChecked;
                        DisableSwitcher();
                        SyncUIState();
                        GisEditor.ActiveMap.DisableInteractiveOverlaysExclude(CurrentAnnotationOverlay);
                    },
                    isChecked => AnnotationsExist);
                }
                return selectMoveCommand;
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
                        MessageBoxResult result = MessageBox.Show("Are you sure you wish to remove all annotations?", "Info", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (result == MessageBoxResult.Yes)
                        {
                            ClearAllAnnotations();
                            if (mapSnapshots[GisEditor.ActiveMap].CanForward || mapSnapshots[GisEditor.ActiveMap].CanRollBack)
                            {
                                mapSnapshots[GisEditor.ActiveMap].Clear();
                            }
                            IsInModifyMode = false;
                        }
                    },
                    () => AnnotationsExist);
                }
                return clearCommand;
            }
        }

        public ObservedCommand DeleteCommand
        {
            get
            {
                if (deleteCommand == null)
                {
                    deleteCommand = new ObservedCommand(() =>
                    {
                        DeleteSelectedAnnotations();
                    },
                    () => AnyAnnotationSelected);
                }
                return deleteCommand;
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
                        Undo();
                    }, () => CanUndo);
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
                        Redo();
                    },
                    () => CanRedo
                    );
                }
                return redoCommand;
            }
        }

        public ObservedCommand ConvertToAnnotationCommand
        {
            get
            {
                if (convertToAnnotationCommand == null)
                {
                    convertToAnnotationCommand = new ObservedCommand(() =>
                    {
                        Collection<FeatureLayer> featureLayers = GisEditor.ActiveMap.GetFeatureLayers(true);
                        foreach (var featureLayer in featureLayers)
                        {
                            featureLayer.Open();
                            ZoomLevel zoomLevel = featureLayer.ZoomLevelSet.GetZoomLevelForDrawing(GisEditor.ActiveMap.CurrentExtent, GisEditor.ActiveMap.ActualWidth, GisEditor.ActiveMap.MapUnit);
                            if (zoomLevel != null)
                            {
                                Collection<string> columns = zoomLevel.GetRequiredColumnNames();

                                Collection<TextStyle> textStyles = GetTextStyles(zoomLevel);
                                if (textStyles.Count > 0)
                                {
                                    Collection<Feature> features = featureLayer.FeatureSource.GetFeaturesForDrawing(featureLayer.GetBoundingBox(), GisEditor.ActiveMap.ActualWidth, GisEditor.ActiveMap.ActualHeight, columns);
                                    //if (features.Any(f => f.LinkColumnValues.Count > 0))
                                    //{
                                    //    features = IconTextStyle.ReplaceColumnValues(features, columns);
                                    //}
                                    foreach (var textStyle in textStyles)
                                    {
                                        CurrentAnnotationOverlay.ChangeAppliedStyle(textStyle, AnnotaionStyleType.LayerStyle);
                                        ValueItem lastValueItem = CurrentAnnotationOverlay.TrackLayerStyle.ValueItems.Last(valueItem => valueItem.DefaultTextStyle != null && valueItem.Value != AnnotationTrackInteractiveOverlay.LinkFileStyleColumnName);
                                        if (lastValueItem != null)
                                        {
                                            foreach (var feature in features)
                                            {
                                                if (feature.ColumnValues.ContainsKey(textStyle.TextColumnName))
                                                {
                                                    string annotationText = feature.ColumnValues[textStyle.TextColumnName];
                                                    if (!string.IsNullOrEmpty(annotationText))
                                                    {
                                                        ConvertTextToAnnotation(lastValueItem.Value, textStyle, feature, annotationText);
                                                    }
                                                }
                                                //else if (feature.LinkColumnValues.ContainsKey(textStyle.TextColumnName))
                                                //{
                                                //    string annotationText = string.Join(Environment.NewLine, feature.LinkColumnValues[textStyle.TextColumnName]);
                                                //    if (!string.IsNullOrEmpty(annotationText))
                                                //    {
                                                //        ConvertTextToAnnotation(lastValueItem.Value, textStyle, feature, annotationText);
                                                //    }
                                                //}
                                            }
                                            textStyle.TextColumnName = AnnotationTrackInteractiveOverlay.AnnotationTextColumnName;
                                        }
                                    }
                                }
                            }
                        }

                        CurrentAnnotationOverlay.Refresh();
                        SyncUIState();

                    }, () => CurrentAnnotationOverlay != null);
                }
                return convertToAnnotationCommand;
            }
        }

        private void ConvertTextToAnnotation(string valueStyleMatchColumnName, TextStyle textStyle, Feature feature, string annotationText)
        {
            PlatformGeoCanvas canvas = new PlatformGeoCanvas
            {
                CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed,
                DrawingQuality = DrawingQuality.HighSpeed,
                SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed
            };

            double width = GisEditor.ActiveMap.ActualWidth;
            double height = GisEditor.ActiveMap.ActualHeight;
            Bitmap nativeImage = new Bitmap((int)width, (int)height);
            canvas.BeginDrawing(nativeImage, GisEditor.ActiveMap.CurrentExtent, GisEditor.ActiveMap.MapUnit);
            DrawingRectangleF rectangle = canvas.MeasureText(annotationText, textStyle.Font);

            Type type = textStyle.GetType();
            MethodInfo method = type.GetMethod("GetLabelingCandidates", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null)
            {
                Collection<LabelingCandidate> candidates = method.Invoke(textStyle, new object[] { feature, canvas }) as Collection<LabelingCandidate>;
                if (candidates != null)
                {
                    foreach (var candidate in candidates)
                    {
                        foreach (var labelInfo in candidate.LabelInformation)
                        {
                            ScreenPointF point = new ScreenPointF((float)labelInfo.PositionInScreenCoordinates.X + rectangle.Width / 2 + 3, (float)labelInfo.PositionInScreenCoordinates.Y - rectangle.Height / 2);

                            PointShape pointShape = ExtentHelper.ToWorldCoordinate(GisEditor.ActiveMap.CurrentExtent, point, (float)width, (float)height);
                            Feature pointFeature = new Feature(pointShape);
                            pointFeature.Id = pointShape.Id;
                            pointFeature.ColumnValues[AnnotationTrackInteractiveOverlay.valueStyleMatchColumnName] = valueStyleMatchColumnName;
                            pointFeature.ColumnValues[AnnotationTrackInteractiveOverlay.AnnotationTextColumnName] = annotationText;
                            CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures[pointShape.Id] = pointFeature;
                        }
                    }
                }
            }
        }

        private Collection<TextStyle> GetTextStyles(ZoomLevel zoomLevel)
        {
            Collection<TextStyle> textStyles = new Collection<TextStyle>();

            if (zoomLevel.CustomStyles.Count == 0)
            {
                TextStyle newStyle = (TextStyle)zoomLevel.DefaultTextStyle.CloneDeep();
                textStyles.Add(newStyle);
            }
            else
            {
                foreach (var item in zoomLevel.CustomStyles.OfType<TextStyle>())
                {
                    TextStyle newStyle = (TextStyle)item.CloneDeep();
                    textStyles.Add(newStyle);
                }
                foreach (var compositeStyle in zoomLevel.CustomStyles.OfType<CompositeStyle>())
                {
                    foreach (var item in compositeStyle.Styles.OfType<TextStyle>())
                    {
                        TextStyle newStyle = (TextStyle)item.CloneDeep();
                        textStyles.Add(newStyle);
                    }
                }
            }

            return textStyles;
        }

        private void ClearAllAnnotations()
        {
            CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.Clear();
            CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Clear();
            CurrentAnnotationOverlay.Refresh();

            CurrentEditOverlay.EditShapesLayer.InternalFeatures.Clear();
            CurrentEditOverlay.CalculateAllControlPoints();
            GisEditor.ActiveMap.Refresh(CurrentEditOverlay);

            SyncUIState();
        }

        private void DisableSwitcher()
        {
            if (GisEditor.ActiveMap != null)
            {
                var switcher = GisEditor.ActiveMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
                if (switcher != null)
                {
                    switcher.SwitcherMode = SwitcherMode.None;
                }
            }
        }

        private void AnnotationOverlay_TrackStarting(object sender, TrackStartingTrackInteractiveOverlayEventArgs e)
        {
            TakeSnapshot();
        }

        private void AnnotationOverlay_TrackEnded(object sender, TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            SyncUIState();
            TakeSnapshot();
        }

        private void AnnotationOverlay_SelectionFinished(object sender, TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            bool isShiftKeyDown = Keyboard.Modifiers == ModifierKeys.Shift;
            if (!isShiftKeyDown)
            {
                AnnotationHelper.CommitEdit();
            }

            var selectionArea = e.TrackShape.GetBoundingBox();
            CurrentAnnotationOverlay.TrackShapeLayer.SafeProcess(() =>
            {
                var tmpFeatureIdsToExclude = new Collection<string>();
                foreach (var id in CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude)
                {
                    tmpFeatureIdsToExclude.Add(id);
                }
                CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Clear();

                var selectedFeatures = CurrentAnnotationOverlay.TrackShapeLayer.QueryTools.GetFeaturesInsideBoundingBox(selectionArea, CurrentAnnotationOverlay.TrackShapeLayer.GetDistinctColumnNames());

                foreach (var id in tmpFeatureIdsToExclude)
                {
                    CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Add(id);
                }

                bool needMarkerOverlayRefreshed = false;
                foreach (var selectedFeature in selectedFeatures)
                {
                    bool isEditing = true;
                    if (!CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Contains(selectedFeature.Id))
                    {
                        CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Add(selectedFeature.Id);
                    }
                    else
                    {
                        isEditing = false;
                        if (isShiftKeyDown)
                        {
                            CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Remove(selectedFeature.Id);
                            if (CurrentEditOverlay.EditShapesLayer.InternalFeatures.Contains(selectedFeature.Id))
                            {
                                CurrentEditOverlay.EditShapesLayer.InternalFeatures.Remove(selectedFeature.Id);
                            }
                            else if (MarkerHelper.CurrentMarkerOverlay.Markers.Contains(selectedFeature.Id))
                            {
                                MarkerHelper.CurrentMarkerOverlay.Markers.Remove(selectedFeature.Id);
                                needMarkerOverlayRefreshed = true;
                            }
                        }
                    }

                    if (needMarkerOverlayRefreshed)
                    {
                        MarkerHelper.CurrentMarkerOverlay.Refresh();
                    }

                    if (isEditing)
                    {
                        AnnotationHelper.SetAnnotationToEditMode(selectedFeature);
                    }
                }
            });

            CurrentEditOverlay.CalculateAllControlPoints();
            CurrentEditOverlay.Refresh();
            CurrentAnnotationOverlay.Refresh();
            SyncUIState();

            TakeSnapshot();
        }

        private void InitializeStyleIcons()
        {
            var styles = CurrentAnnotationOverlay.TrackShapeLayer.ZoomLevelSet
             .ZoomLevel01.CustomStyles.OfType<ValueStyle>().First()
             .ValueItems.Take(1).SelectMany(valueItem =>
             {
                 return new Styles.Style[] 
                { 
                    valueItem.DefaultAreaStyle, 
                    valueItem.DefaultLineStyle, 
                    valueItem.DefaultPointStyle, 
                    valueItem.DefaultTextStyle 
                };
             });

            CompositeStyle style = new CompositeStyle();
            foreach (var item in styles)
            {
                style.Styles.Add(item);
            }
            AnnotationPreview = style.GetPreviewImage(32, 32);
        }

        internal void SyncStylePreview()
        {
            if (CurrentAnnotationOverlay != null)
            {
                if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                }

                cancellationTokenSource = new CancellationTokenSource();
            }
        }

        internal void SyncUIState()
        {
            RaisePropertyChanged(() => IsInModifyMode);
            RaisePropertyChanged(() => AnnotationsExist);
            RaisePropertyChanged(() => AnyAnnotationSelected);
        }

        internal void Refresh()
        {
            if (CurrentAnnotationOverlay != null)
            {
                selectedMode = TrackModes.FirstOrDefault(t => t.Mode == CurrentAnnotationOverlay.TrackMode);
                RaisePropertyChanged(() => SelectedMode);
                switch (CurrentAnnotationOverlay.TrackMode)
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
                        GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawText;
                        break;
                }
            }

            DisplayHideText = CurrentAnnotationOverlay.IsVisible ? "Hide Annotations" : "Display Annotations";
            DisplayHidePreview = CurrentAnnotationOverlay.IsVisible ? new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/hideEye.png", UriKind.RelativeOrAbsolute)) : new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/displayEye.png", UriKind.RelativeOrAbsolute));
        }

        private void DeleteSelectedAnnotations()
        {
            var idsToDelete = CurrentEditOverlay.EditShapesLayer.InternalFeatures.Select(f => f.Id).ToList();
            var markerIdsToDelete = new List<string>();
            if (MarkerHelper.CurrentMarkerOverlay != null && MarkerHelper.CurrentMarkerOverlay.Markers.Count > 0)
            {
                markerIdsToDelete = MarkerHelper.CurrentMarkerOverlay.Markers.Select(tmpMarker => tmpMarker.Tag as string).ToList();
                MarkerHelper.CurrentMarkerOverlay.Markers.Clear();
            }

            CurrentEditOverlay.EditShapesLayer.InternalFeatures.Clear();

            bool needMarkerOverlayRefreshed = false;
            for (int i = CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.Count - 1; i >= 0; i--)
            {
                var featureIdToDelete = CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures[i].Id;
                if (idsToDelete.Contains(featureIdToDelete))
                {
                    CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.RemoveAt(i);
                }
                else if (markerIdsToDelete.Contains(featureIdToDelete))
                {
                    needMarkerOverlayRefreshed = true;
                    CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.RemoveAt(i);
                }
            }

            if (needMarkerOverlayRefreshed)
            {
                MarkerHelper.CurrentMarkerOverlay.Refresh();
            }

            CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Clear();
            CurrentAnnotationOverlay.Refresh();
            CurrentEditOverlay.CalculateAllControlPoints();
            CurrentEditOverlay.Refresh();

            SyncUIState();
        }

        private void EditOverlay_FeatureEdited(object sender, FeatureEditedEditInteractiveOverlayEventArgs e)
        {
            TakeSnapshot();
        }

        public void TakeSnapshot()
        {
            mapSnapshots[GisEditor.ActiveMap].Add(GetCurrentSnapshot());
            RaisePropertyChanged(() => CanUndo);
            RaisePropertyChanged(() => CanRedo);
        }

        private void Undo()
        {
            if (mapSnapshots[GisEditor.ActiveMap].CanRollBack)
            {
                RestoreSnapshot(mapSnapshots[GisEditor.ActiveMap].RollBack());
            }
            RaisePropertyChanged(() => CanUndo);
            RaisePropertyChanged(() => CanRedo);
        }

        private void Redo()
        {
            if (mapSnapshots[GisEditor.ActiveMap].CanForward)
            {
                var snippet = mapSnapshots[GisEditor.ActiveMap].Forward();
                RestoreSnapshot(snippet);
            }
            RaisePropertyChanged(() => CanUndo);
            RaisePropertyChanged(() => CanRedo);
        }

        private void RestoreSnapshot(AnnotationSnapshot snapshot)
        {
            CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.Clear();
            CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Clear();
            CurrentEditOverlay.EditShapesLayer.InternalFeatures.Clear();

            if (MarkerHelper.CurrentMarkerOverlay != null) MarkerHelper.CurrentMarkerOverlay.Markers.Clear();

            foreach (var feature in snapshot.AnnotationFeatures)
            {
                CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures.Add(feature.Id, feature.CloneDeep(ReturningColumnsType.AllColumns));
            }

            foreach (var featureId in snapshot.FeatureIdsToExclude)
            {
                CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude.Add(featureId);
            }

            foreach (var feature in snapshot.EditingVectorFeatures)
            {
                CurrentEditOverlay.EditShapesLayer.InternalFeatures.Add(feature);
            }

            foreach (var markerFeature in snapshot.EditingMarkerFeatures)
            {
                MarkerHelper.AddMarker((PointShape)markerFeature.GetShape()
                    , markerFeature.ColumnValues[AnnotationTrackInteractiveOverlay.AnnotationTextColumnName]
                    , markerFeature.Id
                    , markerFeature.ColumnValues[AnnotationTrackInteractiveOverlay.ValueStyleMatchColumnName]);
            }

            if (CurrentAnnotationOverlay.MapArguments != null)
            {
                CurrentAnnotationOverlay.Refresh();
            }

            CurrentEditOverlay.CalculateAllControlPoints();

            if (CurrentEditOverlay.MapArguments != null)
            {
                CurrentEditOverlay.Refresh();
            }

            if (MarkerHelper.CurrentMarkerOverlay != null)
            {
                MarkerHelper.CurrentMarkerOverlay.Refresh();
            }

            RaisePropertyChanged(() => CanUndo);
            RaisePropertyChanged(() => CanRedo);
        }

        private AnnotationSnapshot GetCurrentSnapshot()
        {
            AnnotationSnapshot snapshot = new AnnotationSnapshot();
            foreach (var feature in CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures)
            {
                snapshot.AnnotationFeatures.Add(feature.CloneDeep(ReturningColumnsType.AllColumns));
            }

            foreach (var featureId in CurrentAnnotationOverlay.TrackShapeLayer.FeatureIdsToExclude)
            {
                snapshot.FeatureIdsToExclude.Add(featureId);
            }

            foreach (var feature in CurrentEditOverlay.EditShapesLayer.InternalFeatures)
            {
                snapshot.EditingVectorFeatures.Add(feature.CloneDeep(ReturningColumnsType.AllColumns));
            }

            if (MarkerHelper.CurrentMarkerOverlay != null)
            {
                foreach (var marker in MarkerHelper.CurrentMarkerOverlay.Markers)
                {
                    string featureId = marker.Tag as string;
                    string content = String.Empty;
                    if (marker.Content != null && marker.Content is TextBox)
                    {
                        content = ((TextBox)marker.Content).Text;
                    }

                    PointShape position = new PointShape(marker.Position.X, marker.Position.Y);

                    var vectorFeature = CurrentAnnotationOverlay.TrackShapeLayer.InternalFeatures
                        .FirstOrDefault(tmpFeature => tmpFeature.Id.Equals(featureId, StringComparison.Ordinal));

                    if (vectorFeature != default(Feature))
                    {
                        Dictionary<string, string> columnValues = new Dictionary<string, string>();
                        columnValues.Add(AnnotationTrackInteractiveOverlay.ValueStyleMatchColumnName
                            , vectorFeature.ColumnValues[AnnotationTrackInteractiveOverlay.ValueStyleMatchColumnName]);
                        columnValues.Add(AnnotationTrackInteractiveOverlay.AnnotationTextColumnName, content);
                        Feature markerFeature = new Feature(position.GetWellKnownBinary(), featureId, columnValues);
                        snapshot.EditingMarkerFeatures.Add(markerFeature);
                    }
                }
            }

            return snapshot;
        }

        private ValueItem CloneValueItem(ValueItem valueItem)
        {
            BinaryFormatter serializer = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, valueItem);
                stream.Seek(0, SeekOrigin.Begin);
                return serializer.Deserialize(stream) as ValueItem;
            }
        }

        private class AnnotationSnapshot
        {
            private Collection<Feature> annotationFeatures;
            private Collection<string> featureIdsToExclude;
            private Collection<ValueItem> valueItems;
            private Collection<Feature> editingVectorFeatures;
            private Collection<Feature> editingMarkerFeatures;

            public AnnotationSnapshot()
            {
                valueItems = new Collection<ValueItem>();
                annotationFeatures = new Collection<Feature>();
                featureIdsToExclude = new Collection<string>();
                editingVectorFeatures = new Collection<Feature>();
                editingMarkerFeatures = new Collection<Feature>();
            }

            public Collection<Feature> AnnotationFeatures
            {
                get { return annotationFeatures; }
            }

            public Collection<string> FeatureIdsToExclude
            {
                get { return featureIdsToExclude; }
            }

            public Collection<ValueItem> ValueItems
            {
                get { return valueItems; }
            }

            public Collection<Feature> EditingVectorFeatures
            {
                get { return editingVectorFeatures; }
            }

            public Collection<Feature> EditingMarkerFeatures
            {
                get { return editingMarkerFeatures; }
            }
        }
    }
}