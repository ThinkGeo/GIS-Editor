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


using GalaSoft.MvvmLight.Command;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class EditingToolsViewModel : INotifyPropertyChanged
    {
        public event EventHandler<CancelEventArgs> EditingLayerChanging;

        private static readonly double searchTolerance = 4.2;
        private static readonly string editOverlayForEditingToolsRibbonGroup = "editOverlayForEditingToolsRibbonGroup";
        private static readonly string cannotSplitMessage = "The split task could not be completed.\r\n\r\nA shape split operation could not classify all parts of the polygon or line as left or right of the cutting line.";
        private static readonly string cannotAddInnerRingMessage = "The inner ring polygon must meet the following conditions:\r\n\r\n1. It must be completely within the parent polygon.\r\n2. It must not overlap the parent polygon's other inner rings, if any.";
        private static readonly SolidColorBrush transparentBrush = new SolidColorBrush(Colors.Transparent);
        private static readonly SolidColorBrush drawingToolBorderBrush = new SolidColorBrush(Color.FromArgb(255, 195, 166, 44));
        private static readonly SolidColorBrush drawingToolBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, 255, 201, 102));
        private static EditingToolsViewModel instance;
        private static bool showSelectedFeaturesMode;

        private Dictionary<TrackMode, DrawingToolItemModel> drawingTools;
        private EditShapesLayerStyleViewModel styleViewModel;
        private GisEditorEditInteractiveOverlay editOverlay;
        private Feature[] tmpEditShapeFeatures;
        private string[] tmpFeatureIdsToExclude;
        private bool isAllSelectedFeaturesModified = true;
        private object selectedDrawingTool;
        private ObservableCollection<CheckableItemViewModel<FeatureLayer>> availableLayers;
        private bool drawingToolsButtonIsEnabled;
        private bool selectIsChecked;
        private bool transformIsChecked;
        private bool moveIsChecked;
        private bool editIsChecked;
        private Dictionary<TrackMode, Cursor> navigateTypeDic;
        private Collection<LayerPlugin> supportedCreateLayerProviders;

        [NonSerialized]
        private SolidColorBrush drawingToolsButtonBackground;

        [NonSerialized]
        private SolidColorBrush drawingToolsButtonBorderBrush;

        private ObservedCommand selectToolCommand;
        private ObservedCommand transformToolCommand;
        private ObservedCommand moveToolCommand;
        private ObservedCommand editToolCommand;
        private ObservedCommand drawingToolsCommand;
        private ObservedCommand cancelCommand;
        private ObservedCommand rollBackCommand;
        private ObservedCommand forwardCommand;
        private ObservedCommand editItemCommand;
        private ObservedCommand editDataCommand;
        private ObservedCommand editColumnCommand;
        private ObservedCommand saveEditingCommand;
        private ObservedCommand removeFeaturesCommand;
        private ObservedCommand changeEditStyleCommand;
        private RelayCommand<CheckableItemViewModel<FeatureLayer>> editingLayerChangedCommand;
        private bool isInRefresh;

        public EditingToolsViewModel()
        {
            navigateTypeDic = new Dictionary<TrackMode, Cursor>();
            navigateTypeDic.Add(TrackMode.None, GisEditorCursors.Pan);
            navigateTypeDic.Add(TrackMode.Circle, GisEditorCursors.DrawCircle);
            navigateTypeDic.Add(TrackMode.Ellipse, GisEditorCursors.DrawEllipse);
            navigateTypeDic.Add(TrackMode.Line, GisEditorCursors.DrawLine);
            navigateTypeDic.Add(TrackMode.StraightLine, GisEditorCursors.DrawLine);
            navigateTypeDic.Add(TrackMode.Point, GisEditorCursors.DrawPoint);
            navigateTypeDic.Add(TrackMode.Multipoint, GisEditorCursors.DrawPoint);
            navigateTypeDic.Add(TrackMode.Polygon, GisEditorCursors.DrawPolygon);
            navigateTypeDic.Add(TrackMode.Rectangle, GisEditorCursors.DrawRectangle);
            navigateTypeDic.Add(TrackMode.Square, GisEditorCursors.DrawSqure);

            DrawingToolsButtonIsEnabled = true;
            InitializeDrawingTools();

            styleViewModel = new EditShapesLayerStyleViewModel();
            availableLayers = new ObservableCollection<CheckableItemViewModel<FeatureLayer>>();
            UpdateTargetLayers();
            showSelectedFeaturesMode = false;
        }

        public static EditingToolsViewModel Instance
        {
            get
            {
                if (instance == null) instance = new EditingToolsViewModel();
                return instance;
            }
        }

        public ObservableCollection<CheckableItemViewModel<FeatureLayer>> AvailableLayers
        {
            get { return availableLayers; }
        }

        public bool SelectIsChecked
        {
            get { return selectIsChecked; }
            set
            {
                selectIsChecked = value;
                RaisePropertyChanged("SelectIsChecked");
            }
        }

        public bool TransformIsChecked
        {
            get { return transformIsChecked; }
            set
            {
                transformIsChecked = value;
                RaisePropertyChanged("TransformIsChecked");
            }
        }

        public bool MoveIsChecked
        {
            get { return moveIsChecked; }
            set
            {
                moveIsChecked = value;
                RaisePropertyChanged("MoveIsChecked");
            }
        }

        public bool EditIsChecked
        {
            get { return editIsChecked; }
            set
            {
                editIsChecked = value;
                RaisePropertyChanged("EditIsChecked");
            }
        }

        public bool DrawingToolsButtonIsEnabled
        {
            get { return drawingToolsButtonIsEnabled; }
            set
            {
                drawingToolsButtonIsEnabled = value;
                RaisePropertyChanged("DrawingToolsButtonIsEnabled");
            }
        }

        public CheckableItemViewModel<FeatureLayer> SelectedLayer
        {
            get { return availableLayers.FirstOrDefault(t => t.IsChecked); }
        }

        public SolidColorBrush DrawingToolsButtonBackground
        {
            get { return drawingToolsButtonBackground; }
            set
            {
                drawingToolsButtonBackground = value;
                RaisePropertyChanged("DrawingToolsButtonBackground");
            }
        }

        public SolidColorBrush DrawingToolsButtonBorderBrush
        {
            get { return drawingToolsButtonBorderBrush; }
            set
            {
                drawingToolsButtonBorderBrush = value;
                RaisePropertyChanged("DrawingToolsButtonBorderBrush");
            }
        }

        public GisEditorEditInteractiveOverlay EditOverlay
        {
            get { return editOverlay; }
        }

        public EditShapesLayerStyleViewModel StyleViewModel
        {
            get { return styleViewModel; }
        }

        public Dictionary<TrackMode, DrawingToolItemModel> DrawingTools
        {
            get { return drawingTools; }
        }

        public object SelectedDrawingTool
        {
            get
            {
                return selectedDrawingTool;
            }
            set
            {
                if (selectedDrawingTool != value)
                {
                    selectedDrawingTool = value;
                    RaisePropertyChanged("SelectedDrawingTool");

                    if (selectedDrawingTool != null)
                    {
                        TrackMode trackMode = ((KeyValuePair<TrackMode, DrawingToolItemModel>)selectedDrawingTool).Key;
                        GisEditor.ActiveMap.DisableInteractiveOverlaysExclude(GisEditor.ActiveMap.TrackOverlay);
                        GisEditor.ActiveMap.TrackOverlay.SetTrackMode(trackMode);
                        SetCursor(trackMode);
                    }
                    else
                    {
                        GisEditor.ActiveMap.TrackOverlay.Disable();
                    }
                    if (!isInRefresh)
                    {
                        GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.SetSelectedDrawingToolDescription));
                    }
                }
            }
        }

        public Collection<LayerPlugin> SupportedCreateLayerProviders
        {
            get
            {
                if (supportedCreateLayerProviders == null) supportedCreateLayerProviders = new ObservableCollection<LayerPlugin>();
                supportedCreateLayerProviders.Clear();

                if (GisEditor.LayerManager != null)
                {
                    foreach (var plugin in GisEditor.LayerManager.GetActiveLayerPlugins<FeatureLayerPlugin>()
                        .Where(p => p.CanCreateFeatureLayer))
                    {
                        supportedCreateLayerProviders.Add(plugin);
                    }
                }

                return supportedCreateLayerProviders;
            }
        }

        public ObservedCommand ChangeEditStyleCommand
        {
            get
            {
                if (changeEditStyleCommand == null)
                {
                    changeEditStyleCommand = new ObservedCommand(() =>
                    {
                        if (EditOverlay != null)
                        {
                            StyleBuilderArguments styleArguments = new StyleBuilderArguments();
                            styleArguments.AvailableUIElements = StyleBuilderUIElements.StyleList;
                            styleArguments.AvailableStyleCategories = StyleCategories.Area | StyleCategories.Line | StyleCategories.Point;
                            styleArguments.StyleToEdit = StyleViewModel.EditCompositeStyle;
                            styleArguments.FeatureLayer = EditOverlay.EditShapesLayer;
                            styleArguments.AppliedCallback = (result) =>
                            {
                                StyleViewModel.EditCompositeStyle = result.CompositeStyle;
                            };

                            var resultStyle = GisEditor.StyleManager.EditStyle(styleArguments);
                            if (resultStyle != null && resultStyle.CompositeStyle != null)
                            {
                                StyleViewModel.EditCompositeStyle = resultStyle.CompositeStyle;
                            }
                        }
                    }, () => GisEditor.ActiveMap != null);
                }
                return changeEditStyleCommand;
            }
        }

        public ObservedCommand SelectToolCommand
        {
            get
            {
                if (selectToolCommand == null)
                {
                    selectToolCommand = new ObservedCommand(() =>
                    {
                        SetEditOverlayMode(false, false, false, false);
                        if (MapHelper.ShowHintWindow("ShowEditingFeaturesHint"))
                        {
                            var title = GisEditor.LanguageManager.GetStringResource("EditingToolsViewModelHintWindowTitle");
                            var description = GisEditor.LanguageManager.GetStringResource("EditingToolsViewModelHintWindowDescription");
                            var steps = new Collection<String>()
                            {
                                GisEditor.LanguageManager.GetStringResource("EditingToolsViewModelHintWindowStep1Choose"),
                                GisEditor.LanguageManager.GetStringResource("EditingToolsViewModelHintWindowStep2Hold"),
                                GisEditor.LanguageManager.GetStringResource("EditingToolsViewModelHintWindowStep3If")
                            };
                            GisEditorHintWindow gisEditorHintWindow = new GisEditorHintWindow(title, description, steps, "/GisEditorPluginCore;component/Images/Hints/Editing Multiples.gif");
                            gisEditorHintWindow.ShowDialog();

                            var result = (gisEditorHintWindow.DataContext as GisEditorHintViewModel).DonotShowAgain;
                            MapHelper.SetShowHint("ShowEditingFeaturesHint", !result);
                        }
                    }, () =>
                    {
                        bool canEdit = CanEdit();
                        if (!canEdit)
                        {
                            SelectIsChecked = false;
                        }
                        return canEdit;
                    });
                }
                return selectToolCommand;
            }
        }

        public RelayCommand<CheckableItemViewModel<FeatureLayer>> EditingLayerChangedCommand
        {
            get
            {
                if (editingLayerChangedCommand == null)
                {
                    editingLayerChangedCommand = new RelayCommand<CheckableItemViewModel<FeatureLayer>>(clickingItem =>
                    {
                        if (clickingItem != SelectedLayer)
                        {
                            var e = new CancelEventArgs();
                            OnEditingLayerChanging(e);
                            if (!e.Cancel)
                            {
                                foreach (var layerEntity in AvailableLayers)
                                {
                                    layerEntity.IsChecked = layerEntity.Value == clickingItem.Value;
                                }
                                if (GisEditor.ActiveMap != null)
                                {
                                    TargetLayerChanged();
                                    if (clickingItem.Value == null && AvailableLayers.Count >= 1)
                                    {
                                        //GisEditor.ActiveMap.TrackOverlay.Disable();
                                        //editOverlay.Disable();

                                        GisEditor.ActiveMap.DisableInteractiveOverlays();
                                        SwitchToPanMode();

                                        //if (activeFeatureLayer != null)
                                        //{
                                        //    var overlay = GisEditor.ActiveMap.GetOverlaysContaining(activeFeatureLayer).FirstOrDefault();
                                        //    if (overlay != null)
                                        //    {
                                        //        overlay.RefreshCache(RefreshCacheMode.ApplyNewCache);
                                        //        overlay.Refresh();
                                        //    }
                                        //}
                                    }
                                }
                                GisEditor.UIManager.RefreshPlugins(new RefreshArgs(null, RefreshArgsDescription.EditLayerChangedDescription));
                            }
                        }
                    });
                }
                return editingLayerChangedCommand;
            }
        }

        public ObservedCommand TransformToolCommand
        {
            get
            {
                if (transformToolCommand == null)
                {
                    transformToolCommand = new ObservedCommand(() =>
                    {
                        SetEditOverlayMode(false, true, true, true);
                    }, () =>
                    {
                        bool canEdit = CanEdit();
                        if (!canEdit)
                        {
                            TransformIsChecked = false;
                        }
                        return canEdit;
                    });
                }
                return transformToolCommand;
            }
        }

        public ObservedCommand MoveToolCommand
        {
            get
            {
                if (moveToolCommand == null)
                {
                    moveToolCommand = new ObservedCommand(() =>
                    {
                        SetEditOverlayMode(false, true, false, false);
                    }, () =>
                    {
                        bool canEdit = CanEdit();
                        if (!canEdit)
                        {
                            MoveIsChecked = false;
                        }
                        return canEdit;
                    });
                }
                return moveToolCommand;
            }
        }

        public ObservedCommand EditToolCommand
        {
            get
            {
                if (editToolCommand == null)
                {
                    editToolCommand = new ObservedCommand(() =>
                    {
                        SetEditOverlayMode(true, false, false, false);
                    }, () =>
                    {
                        bool canEdit = CanEdit();
                        if (!canEdit)
                        {
                            EditIsChecked = false;
                        }
                        return canEdit;
                    });
                }
                return editToolCommand;
            }
        }

        public ObservedCommand DrawingToolsCommand
        {
            get
            {
                if (drawingToolsCommand == null)
                {
                    drawingToolsCommand = new ObservedCommand(() =>
                    {
                        SelectedDrawingTool = DrawingTools.FirstOrDefault(s => s.Value.IsEnabled);
                    }, () => CanEdit());
                }
                return drawingToolsCommand;
            }
        }

        public ObservedCommand RemoveFeaturesCommand
        {
            get
            {
                if (removeFeaturesCommand == null)
                {
                    removeFeaturesCommand = new ObservedCommand(() =>
                    {
                        editOverlay.RemoveFeatures();
                    }, () => editOverlay != null && editOverlay.EditShapesLayer.InternalFeatures.Count > 0);
                }
                return removeFeaturesCommand;
            }
        }

        public ObservedCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new ObservedCommand(() =>
                    {
                        MessageBoxResult result = MessageBox.Show(GisEditor.LanguageManager.GetStringResource("EditingToolsViewModelCancelUnsavedChanges"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (result == MessageBoxResult.Yes)
                        {
                            CancelEidt();
                        }
                    },
                    () => editOverlay != null && editOverlay.CanCancel && SelectedLayer.Value != null
                    );
                }
                return cancelCommand;
            }
        }

        public void CancelEidt()
        {
            editOverlay.Cancel();
            RefreshActiveFeatureLayer();
        }

        public ObservedCommand RollBackCommand
        {
            get
            {
                if (rollBackCommand == null)
                {
                    rollBackCommand = new ObservedCommand(() =>
                    {
                        RollBack();
                    },
                    () => editOverlay != null ? (editOverlay.CanRollback && SelectedLayer.Value != null) : false
                    );
                }
                return rollBackCommand;
            }
        }

        public ObservedCommand ForwardCommand
        {
            get
            {
                if (forwardCommand == null)
                {
                    forwardCommand = new ObservedCommand(() =>
                    {
                        Forward();
                    },
                    () => editOverlay != null ? editOverlay.CanFoward : false
                    );
                }
                return forwardCommand;
            }
        }

        public ObservedCommand EditItemCommand
        {
            get
            {
                if (editItemCommand == null)
                {
                    editItemCommand = new ObservedCommand(() =>
                    {
                        if (editOverlay != null && editOverlay.EditShapesLayer.InternalFeatures.Count > 0)
                        {
                            Dictionary<FeatureLayer, Collection<Feature>> features = new Dictionary<FeatureLayer, Collection<Feature>>();
                            features[SelectedLayer.Value] = new Collection<Feature>();
                            foreach (var feature in editOverlay.EditShapesLayer.InternalFeatures)
                            {
                                features[SelectedLayer.Value].Add(feature);
                            }

                            //Collection<HighlightedFeatureModel> entities = new Collection<HighlightedFeatureModel>();
                            //foreach (var item in editOverlay.EditShapesLayer.InternalFeatures)
                            //{
                            //    entities.Add(new HighlightedFeatureModel(item, SelectedLayer.Value));
                            //}
                            if (ShowFeatureAttributeWindow(features))
                            {
                                SaveEditing();

                                //RefreshEditOverlay();
                            }
                        }
                    },
                    () => editOverlay != null && editOverlay.EditShapesLayer.InternalFeatures.Count == 1
                    );
                }
                return editItemCommand;
            }
        }

        public ObservedCommand EditDataCommand
        {
            get
            {
                if (editDataCommand == null)
                {
                    editDataCommand = new ObservedCommand(() =>
                    {
                        DataViewerUserControl content = new DataViewerUserControl(editOverlay.EditTargetLayer, new FeatureLayer[] { editOverlay.EditTargetLayer });
                        content.IsEditable = true;
                        content.IsHighlightFeatureOnly = showSelectedFeaturesMode;
                        content.ShowDialog();
                        showSelectedFeaturesMode = content.IsHighlightFeatureOnly;

                    }, () => editOverlay != null && editOverlay.EditTargetLayer != null);
                }
                return editDataCommand;
            }
        }

        public ObservedCommand EditColumnCommand
        {
            get
            {
                if (editColumnCommand == null)
                {
                    editColumnCommand = new ObservedCommand(() =>
                    {
                        FeatureLayerPlugin plugin = GisEditor.LayerManager.GetLayerPlugins(SelectedLayer.Value.GetType()).OfType<FeatureLayerPlugin>().Where(p => p.IsActive).FirstOrDefault();
                        ConfigureFeatureLayerParameters parameters = plugin.GetConfigureFeatureLayerParameters(SelectedLayer.Value);
                        if (parameters != null)
                        {
                            FeatureLayer featureLayer = SelectedLayer.Value;
                            foreach (var item in parameters.CustomData)
                            {
                                string alias = item.Value.ToString();
                                featureLayer.FeatureSource.SetColumnAlias(item.Key, alias);
                            }

                            UpdateColumns(featureLayer, parameters);

                            var updateColumns = new Collection<FeatureSourceColumn>();
                            foreach (var column in parameters.AddedColumns)
                            {
                                updateColumns.Add(column);
                            }
                            foreach (var column in parameters.UpdatedColumns.Values)
                            {
                                updateColumns.Add(column);
                            }

                            Collection<FeatureSourceColumn> newUpdatedColumns = new Collection<FeatureSourceColumn>(updateColumns.ToList());
                            EditorUIPlugin editorPlugin = GisEditor.UIManager.GetActiveUIPlugins<EditorUIPlugin>().FirstOrDefault();
                            if (editorPlugin != null)
                            {
                                string id = featureLayer.FeatureSource.Id;
                                if (editorPlugin.CalculatedColumns.ContainsKey(id))
                                {
                                    foreach (var column in updateColumns)
                                    {
                                        var calculateColumn = editorPlugin.CalculatedColumns[id].FirstOrDefault(c => c.ColumnName == column.ColumnName);
                                        if (calculateColumn != null)
                                        {
                                            if (!(column is CalculatedDbfColumn))
                                            {
                                                editorPlugin.CalculatedColumns[id].Remove(calculateColumn);
                                                newUpdatedColumns.Remove(column);
                                            }
                                        }
                                    }
                                }
                            }

                            EditorUIPlugin.UpdateCalculatedRecords(featureLayer, newUpdatedColumns, true);

                            UpdateEditLayerColumns();
                        }
                    },
                    () => editOverlay != null && SelectedLayer.Value != null && SelectedLayer.Value.FeatureSource.CanModifyColumnStructure);
                }
                return editColumnCommand;
            }
        }

        public void OnEditingLayerChanging(CancelEventArgs e)
        {
            EventHandler<CancelEventArgs> handler = EditingLayerChanging;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private static void UpdateColumns(FeatureLayer featureLayer, ConfigureFeatureLayerParameters parameters)
        {
            Collection<FeatureSourceColumn> newColumns = parameters.AddedColumns;
            Collection<FeatureSourceColumn> deletedColumns = parameters.DeletedColumns;
            Dictionary<string, FeatureSourceColumn> updateColumns = parameters.UpdatedColumns;

            featureLayer.SafeProcess(() =>
            {
                try
                {
                    featureLayer.CloseInOverlay();
                    featureLayer.SetLayerAccess(LayerAccessMode.ReadWrite);
                    featureLayer.Open();

                    // update features first, to avoid the exception caused by the column length get smaller
                    featureLayer.EditTools.BeginTransaction();
                    foreach (var updatedFeature in parameters.UpdatedFeatures)
                    {
                        featureLayer.EditTools.Update(updatedFeature.Value);
                    }
                    featureLayer.EditTools.CommitTransaction();
                    featureLayer.FeatureSource.Close();

                    featureLayer.Open();
                    featureLayer.EditTools.BeginTransaction();

                    foreach (var column in deletedColumns)
                    {
                        featureLayer.FeatureSource.DeleteColumn(column.ColumnName);
                    }

                    foreach (var column in newColumns)
                    {
                        featureLayer.FeatureSource.AddColumn(column);
                    }

                    foreach (var column in updateColumns)
                    {
                        featureLayer.FeatureSource.UpdateColumn(column.Key, column.Value);
                    }
                    featureLayer.EditTools.CommitTransaction();
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
                finally
                {
                    featureLayer.FeatureSource.RefreshColumns();
                    featureLayer.FeatureSource.Close();
                    featureLayer.SetLayerAccess(LayerAccessMode.Read);
                }
            });
        }

        public ObservedCommand SaveEditingCommand
        {
            get
            {
                if (saveEditingCommand == null)
                {
                    saveEditingCommand = new ObservedCommand(() =>
                    {
                        CheckWhetherSaveEditing();
                    }, () => editOverlay != null && editOverlay.IsEdited);
                }
                //editOverlay != null ? (editOverlay.EditShapesLayer.InternalFeatures.Count > 0 || (SelectedLayer.Value != null && SelectedLayer.Value.FeatureIdsToExclude.Count != 0)) : false
                return saveEditingCommand;
            }
        }

        public void Refresh(GisEditorWpfMap map)
        {
            isInRefresh = true;
            editOverlay = map.FeatureLayerEditOverlay;
            if (editOverlay == null)
            {
                editOverlay = new GisEditorEditInteractiveOverlay();
                editOverlay.ParentMap = map as GisEditorWpfMap;
                editOverlay.Name = editOverlayForEditingToolsRibbonGroup;
                editOverlay.FeatureClicked += new EventHandler<FeatureClickedGisEditorEditInteractiveOverlayEventArgs>(EditOverlay_ModifierClick);
                editOverlay.FeatureTrackEnded += new EventHandler<FeatureTrackEndedGisEditorEditInteractiveOverlayEventArgs>(EditOverlay_FeatureTrackEnded);
                map.InteractiveOverlays.Insert(0, editOverlay);
            }

            editOverlay.FeatureClicked -= EditOverlay_ModifierClick;
            editOverlay.FeatureClicked += EditOverlay_ModifierClick;
            editOverlay.FeatureTrackEnded -= EditOverlay_FeatureTrackEnded;
            editOverlay.FeatureTrackEnded += EditOverlay_FeatureTrackEnded;

            //map.TrackOverlay.TrackStarted -= TrackOverlay_TrackStarted;
            //map.TrackOverlay.TrackStarted += TrackOverlay_TrackStarted;
            map.TrackOverlay.TrackEnded -= TrackOverlay_TrackEnded;
            map.TrackOverlay.TrackEnded += TrackOverlay_TrackEnded;

            //map.TrackOverlay.MouseMoved -= TrackOverlay_MouseMoved;
            //map.TrackOverlay.MouseMoved += TrackOverlay_MouseMoved;
            //map.TrackOverlay.VertexAdding -= TrackOverlay_VertexAdding;
            //map.TrackOverlay.VertexAdding += TrackOverlay_VertexAdding;
            map.TrackOverlay.Drawn -= TrackOverlay_Drawn;
            map.TrackOverlay.Drawn += TrackOverlay_Drawn;

            RefreshTargetLayerList();

            if (SelectedLayer.Value == null && AvailableLayers.Count >= 1 && editOverlay != null && editOverlay.EditTargetLayer != null)
            {
                GisEditor.ActiveMap.DisableInteractiveOverlays();
                SwitchToPanMode();
            }

            if (editOverlay != null && editOverlay.EditTargetLayer == null && editOverlay.IsEnabled())
            {
                if (editOverlay.EditShapesLayer.InternalFeatures.Count > 0)
                {
                    editOverlay.EditShapesLayer.InternalFeatures.Clear();
                    editOverlay.EditShapesLayer.BuildIndex();
                }
                if (editOverlay.EditCandidatesLayer.InternalFeatures.Count > 0)
                {
                    editOverlay.EditCandidatesLayer.InternalFeatures.Clear();
                    editOverlay.EditCandidatesLayer.BuildIndex();
                }
                editOverlay.ClearVertexControlPoints();
                editOverlay.ClearSnapshots();
                map.Refresh(editOverlay);
                editOverlay.Disable();
            }
            RefreshToolButtons();
            RefreshCursor();
            ChangeSketchToolButtonStyle(GisEditor.ActiveMap.TrackOverlay.TrackMode != TrackMode.None);
            StyleViewModel.UpdateEditStylePreviewASync();
            isInRefresh = false;
        }

        public void ExcuteEditingLayerChangedCommand(CheckableItemViewModel<FeatureLayer> selectionItem)
        {
            EditingLayerChangedCommand.Execute(selectionItem);
        }

        private void ChangeSketchToolButtonStyle(bool toggled)
        {
            DrawingToolsButtonBackground = toggled ? drawingToolBackgroundBrush : transparentBrush;
            DrawingToolsButtonBorderBrush = toggled ? drawingToolBorderBrush : transparentBrush;
        }

        private void TargetLayerChanged()
        {
            if (SelectedLayer.Value != null)
            {
                if (CheckWhetherSaveEditing() == System.Windows.Forms.DialogResult.Cancel)
                {
                    return;
                }

                if (SelectedLayer.Value is FeatureLayer)
                {
                    ActivateSkecthToolsAccordingToLayerType();
                }

                //else if (SelectedLayer.Value is CsvFeatureLayer)
                //{
                //    drawingTools[TrackMode.Point].IsEnabled = true;
                //}

                UpdateEditLayerColumns();
                DrawingToolsButtonIsEnabled = true;
            }
            else if (SelectedLayer.Value == null && AvailableLayers.Count >= 1)
            {
                if (editOverlay.EditTargetLayer != null)
                    CheckWhetherSaveEditing();

                foreach (var tool in drawingTools)
                {
                    tool.Value.IsEnabled = false;
                    tool.Value.IsSelected = false;
                }

                SetCursor(TrackMode.None);
                selectedDrawingTool = null;
                ChangeSketchToolButtonStyle(false);
            }

            foreach (var model in AvailableLayers)
            {
                model.IsChecked = model == SelectedLayer;
                if (model.IsChecked) editOverlay.EditTargetLayer = model.Value;
            }
        }

        private static void SwitchToPanMode()
        {
            if (GisEditor.ActiveMap != null && GisEditor.ActiveMap.InteractiveOverlays.All(o => !o.IsEnabled()))
            {
                var switcherPanZoomBar = GisEditor.ActiveMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
                if (switcherPanZoomBar != null && switcherPanZoomBar.SwitcherMode == SwitcherMode.None)
                {
                    switcherPanZoomBar.SwitcherMode = SwitcherMode.Pan;
                    GisEditor.ActiveMap.ExtentOverlay.PanMode = MapPanMode.Default;
                    GisEditor.ActiveMap.ExtentOverlay.LeftClickDragKey = System.Windows.Forms.Keys.ShiftKey;
                    GisEditor.ActiveMap.ExtentOverlay.OverlayCanvas.IsEnabled = true;
                }
            }
        }

        private void UpdateEditLayerColumns()
        {
            SelectedLayer.Value.SafeProcess(() =>
            {
                editOverlay.EditShapesLayer.Open();
                editOverlay.EditShapesLayer.Columns.Clear();
                foreach (var column in SelectedLayer.Value.FeatureSource.GetColumns())
                {
                    editOverlay.EditShapesLayer.Columns.Add(column);
                }
            });
        }

        private void ActivateSkecthToolsAccordingToLayerType()
        {
            var shpLayer = SelectedLayer.Value as ShapeFileFeatureLayer;

            TrackMode trackMode = GisEditor.ActiveMap.TrackOverlay.TrackMode;

            if (shpLayer != null)
            {
                ShapeFileType fileType = ShapeFileType.Null;

                shpLayer.SafeProcess(() =>
                {
                    fileType = shpLayer.GetShapeFileType();
                });

                DisableAllDrawingTools();
                switch (fileType)
                {
                    case ShapeFileType.Point:
                        drawingTools[TrackMode.Point].IsEnabled = true;
                        if (trackMode != TrackMode.None)
                        {
                            GisEditor.ActiveMap.TrackOverlay.SetTrackMode(TrackMode.Point);
                        }
                        break;

                    case ShapeFileType.Multipoint:
                        drawingTools[TrackMode.Multipoint].IsEnabled = true;
                        drawingTools[TrackMode.Point].IsEnabled = true;
                        break;

                    case ShapeFileType.Polygon:
                        drawingTools[TrackMode.Polygon].IsEnabled = true;
                        drawingTools[TrackMode.Rectangle].IsEnabled = true;
                        drawingTools[TrackMode.Square].IsEnabled = true;
                        drawingTools[TrackMode.Ellipse].IsEnabled = true;
                        drawingTools[TrackMode.Circle].IsEnabled = true;

                        if (trackMode != TrackMode.None &&
                              !(trackMode == TrackMode.Polygon
                              || trackMode == TrackMode.Rectangle
                              || trackMode == TrackMode.Square
                              || trackMode == TrackMode.Ellipse
                              || trackMode == TrackMode.Circle))
                        {
                            GisEditor.ActiveMap.TrackOverlay.SetTrackMode(TrackMode.Polygon);
                        }
                        break;

                    case ShapeFileType.Polyline:
                        drawingTools[TrackMode.Line].IsEnabled = true;
                        if (trackMode != TrackMode.None &&
                            !(trackMode == TrackMode.Line ||
                            trackMode == TrackMode.StraightLine))
                        {
                            GisEditor.ActiveMap.TrackOverlay.SetTrackMode(TrackMode.Line);
                        }
                        break;

                    default:
                        break;
                }
            }
            else
            {
                var layer = SelectedLayer.Value as FeatureLayer;

                if (layer != null)
                {
                    var shapeType = GisEditor.LayerManager.GetFeatureSimpleShapeType(layer);

                    DisableAllDrawingTools();

                    switch (shapeType)
                    {
                        case SimpleShapeType.Point:
                            drawingTools[TrackMode.Point].IsEnabled = true;
                            if (trackMode != TrackMode.None)
                            {
                                GisEditor.ActiveMap.TrackOverlay.SetTrackMode(TrackMode.Point);
                            }
                            break;

                        case SimpleShapeType.Area:
                            drawingTools[TrackMode.Polygon].IsEnabled = true;
                            drawingTools[TrackMode.Rectangle].IsEnabled = true;
                            drawingTools[TrackMode.Square].IsEnabled = true;
                            drawingTools[TrackMode.Ellipse].IsEnabled = true;
                            drawingTools[TrackMode.Circle].IsEnabled = true;

                            if (trackMode != TrackMode.None &&
                                  !(trackMode == TrackMode.Polygon
                                  || trackMode == TrackMode.Rectangle
                                  || trackMode == TrackMode.Square
                                  || trackMode == TrackMode.Ellipse
                                  || trackMode == TrackMode.Circle))
                            {
                                GisEditor.ActiveMap.TrackOverlay.SetTrackMode(TrackMode.Polygon);
                            }
                            break;

                        case SimpleShapeType.Line:
                            drawingTools[TrackMode.Line].IsEnabled = true;
                            if (trackMode != TrackMode.None &&
                                !(trackMode == TrackMode.Line ||
                                trackMode == TrackMode.StraightLine))
                            {
                                GisEditor.ActiveMap.TrackOverlay.SetTrackMode(TrackMode.Line);
                            }
                            break;

                        case SimpleShapeType.Unknown:
                        default:
                            drawingTools.ForEach(d => d.Value.IsEnabled = true);
                            if (trackMode != TrackMode.None &&
                                  !(trackMode == TrackMode.Polygon
                                  || trackMode == TrackMode.Rectangle
                                  || trackMode == TrackMode.Square
                                  || trackMode == TrackMode.Ellipse
                                  || trackMode == TrackMode.Circle))
                            {
                                GisEditor.ActiveMap.TrackOverlay.SetTrackMode(TrackMode.Polygon);
                            }
                            break;
                    }
                }
            }

            if (trackMode != TrackMode.None)
            {
                RefreshToolButtons();
            }
        }

        private void RefreshToolButtons()
        {
            //Force reconsider all of command objects' CanExecute states
            CommandManager.InvalidateRequerySuggested();
            if (!selectToolCommand.CanExecute(null))
                SelectIsChecked = false;
            if (!transformToolCommand.CanExecute(null))
                TransformIsChecked = false;
            if (!editToolCommand.CanExecute(null))
                EditIsChecked = false;
            if (!moveToolCommand.CanExecute(null))
                MoveIsChecked = false;

            if (!editOverlay.IsEnabled())
            {
                SelectIsChecked = false;
                EditIsChecked = false;
                TransformIsChecked = false;
                MoveIsChecked = false;
                if (editOverlay.MapArguments != null
                    && (editOverlay.AssociateControlPointsLayer.InternalFeatures.Count > 0 ||
                    editOverlay.ReshapeControlPointsLayer.InternalFeatures.Count > 0))
                {
                    editOverlay.ClearVertexControlPoints();
                    editOverlay.Refresh();
                }
            }
            else
            {
                if (editOverlay.CanDrag && editOverlay.CanResize && editOverlay.CanRotate)
                    TransformIsChecked = true;
                else if (editOverlay.CanReshape)
                    EditIsChecked = true;
                else if (editOverlay.CanDrag)
                    MoveIsChecked = true;
                else
                    SelectIsChecked = true;
            }

            foreach (var item in drawingTools)
            {
                item.Value.IsSelected = false;
            }
            if (SelectedLayer == null || SelectedLayer.Name.Equals("None"))
            {
                TargetLayerChanged();
            }
            if (drawingTools.ContainsKey(GisEditor.ActiveMap.TrackOverlay.TrackMode))
            {
                if (drawingTools[GisEditor.ActiveMap.TrackOverlay.TrackMode].IsSelected)
                {
                    drawingTools[GisEditor.ActiveMap.TrackOverlay.TrackMode].IsSelected = true;
                    selectedDrawingTool = drawingTools.FirstOrDefault(s => s.Key == GisEditor.ActiveMap.TrackOverlay.TrackMode);
                }
            }
            if (!drawingTools.Any(item => item.Value.IsSelected))
            {
                selectedDrawingTool = null;
            }
            RaisePropertyChanged("SelectedDrawingTool");
        }

        private void RefreshCursor()
        {
            if (editOverlay.IsEnabled())
            {
                GisEditor.ActiveMap.Cursor = GisEditorCursors.Normal;
            }
            else if (GisEditor.ActiveMap.TrackOverlay.TrackMode != TrackMode.None)
            {
                SetCursor(GisEditor.ActiveMap.TrackOverlay.TrackMode);
            }
        }

        #region Events

        //private void TrackOverlay_VertexAdding(object sender, VertexAddingTrackInteractiveOverlayEventArgs e)
        //{
        //PointShape snappedPoint = GetSnappingPoint(e.AddingVertex, e.AffectedFeature, false);
        //if (snappedPoint != null) e.AddingVertex = new Vertex(snappedPoint);
        //}

        private void TrackOverlay_TrackEnded(object sender, TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            var trackOverlay = sender as TrackInteractiveOverlay;
            if (e.TrackShape != null && trackOverlay.TrackShapeLayer.InternalFeatures.Count > 0)
            {
                BaseShape trackShape = e.TrackShape;
                trackOverlay.TrackShapeLayer.InternalFeatures.Clear();
                trackOverlay.Refresh();
                if (EditOverlay != null)
                {
                    var trackResultProcessMode = EditOverlay.TrackResultProcessMode;
                    switch (trackResultProcessMode)
                    {
                        case TrackResultProcessMode.Split:
                            TrackEndedWithSplitProcess(trackShape, trackOverlay);
                            break;

                        case TrackResultProcessMode.InnerRing:
                            TrackEndedWithInnerRingProcess(trackShape, trackOverlay);
                            break;

                        case TrackResultProcessMode.None:
                        default:
                            TrackEndedWithNormalProcess(trackShape);
                            break;
                    }
                }
                else
                {
                    TrackEndedWithNormalProcess(trackShape);
                }
            }
            SaveEditingCommand.RaiseExecuteChanged();
        }

        private void TrackEndedWithInnerRingProcess(BaseShape trackShape, TrackInteractiveOverlay trackOverlay)
        {
            if (trackShape is PolygonShape)
            {
                bool isAdded = false;
                PolygonShape innerRing = (PolygonShape)trackShape;
                Feature[] featuresToAddingInnerRing = EditOverlay.EditShapesLayer.InternalFeatures
                    .Where(f => (f.GetShape() is AreaBaseShape) && innerRing.IsWithin(f)).ToArray();

                foreach (var targetFeature in featuresToAddingInnerRing)
                {
                    BaseShape targetShape = targetFeature.GetShape();
                    if (targetShape.GetWellKnownType() == WellKnownType.Polygon)
                    {
                        PolygonShape targetPolygon = (PolygonShape)targetShape;
                        bool isSuccess = AddInnerRing(innerRing, targetPolygon);
                        if (isSuccess)
                        {
                            isAdded = true;
                            Feature innerRingFeature = new Feature(innerRing, targetFeature.ColumnValues);
                            EditOverlay.EditShapesLayer.InternalFeatures.Add(innerRingFeature.Id, innerRingFeature);
                            editOverlay.NewFeatureIds.Add(innerRingFeature.Id);
                        }
                    }
                    else if (targetShape.GetWellKnownType() == WellKnownType.Multipolygon)
                    {
                        MultipolygonShape targetMultipolygon = (MultipolygonShape)targetShape;
                        foreach (var targetPolygon in targetMultipolygon.Polygons)
                        {
                            bool isSuccessed = AddInnerRing(innerRing, targetPolygon);
                            if (isSuccessed)
                            {
                                isAdded = true;
                            }
                        }

                        if (isAdded)
                        {
                            Feature innerRingFeature = new Feature(innerRing, targetFeature.ColumnValues);
                            innerRingFeature.Id = Guid.NewGuid().ToString();
                            EditOverlay.EditShapesLayer.InternalFeatures.Add(innerRingFeature.Id, innerRingFeature);
                            editOverlay.NewFeatureIds.Add(innerRingFeature.Id);
                        }
                    }
                    int index = EditOverlay.EditShapesLayer.InternalFeatures.IndexOf(targetFeature);
                    Feature newFeature = new Feature(targetShape.GetWellKnownBinary(), targetFeature.Id, targetFeature.ColumnValues);
                    EditOverlay.EditShapesLayer.InternalFeatures.RemoveAt(index);
                    EditOverlay.EditShapesLayer.InternalFeatures.Insert(index, newFeature);
                }

                if (!isAdded)
                {
                    System.Windows.Forms.MessageBox.Show(cannotAddInnerRingMessage, "Add Inner Ring Operation Failed", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }

                EditOverlay.TakeSnapshot();
                EditOverlay.EditShapesLayer.BuildIndex();
                EditOverlay.Refresh();
            }

            trackOverlay.TrackMode = TrackMode.None;
            EditOverlay.TrackResultProcessMode = TrackResultProcessMode.None;
            EditOverlay.IsEnabled = true;
            RefreshCursor();
        }

        private static bool AddInnerRing(PolygonShape innerRing, PolygonShape targetPolygon)
        {
            bool isAdded = false;
            if (targetPolygon.InnerRings.Count == 0)
            {
                isAdded = true;
                if (targetPolygon.OuterRing.Contains(innerRing))
                {
                    targetPolygon.InnerRings.Add(innerRing.OuterRing);
                }
            }
            else
            {
                if (targetPolygon.InnerRings.All(r => r.IsDisjointed(innerRing)))
                {
                    isAdded = true;
                    targetPolygon.InnerRings.Add(innerRing.OuterRing);
                }
            }

            return isAdded;
        }

        private void TrackEndedWithSplitProcess(BaseShape trackShape, TrackInteractiveOverlay trackOverlay)
        {
            if (trackShape is LineShape)
            {
                LineShape splittingLine = (LineShape)trackShape;
                MultipolygonShape splittingArea = splittingLine.Buffer(0.2, EditOverlay.MapArguments.MapUnit, DistanceUnit.Meter);
                Feature[] featuresToSplit = EditOverlay.EditShapesLayer.InternalFeatures.Where(f =>
                {
                    WellKnownType shapeType = f.GetWellKnownType();
                    return ShapeOperationsViewModel.CanSplitShapeTypes.Contains(shapeType);
                }).ToArray();

                Collection<Feature> splitingResult = new Collection<Feature>();
                bool isSplitted = false;
                foreach (var featureToSplit in featuresToSplit)
                {
                    if (!featureToSplit.MakeValidUsingSqlTypes().IsDisjointed(new Feature(splittingArea).MakeValidUsingSqlTypes()))
                    {
                        WellKnownType currentFeatureType = featureToSplit.GetWellKnownType();
                        if (currentFeatureType == WellKnownType.Polygon || currentFeatureType == WellKnownType.Multipolygon)
                        {
                            AreaBaseShape sourceAreaShape = (AreaBaseShape)featureToSplit.MakeValidUsingSqlTypes().GetShape();
                            MultipolygonShape result = sourceAreaShape.GetDifference(splittingArea);
                            BaseShapeExtension.CloseSplittedPolygons(sourceAreaShape, result, splittingLine, EditOverlay.MapArguments.MapUnit, DistanceUnit.Meter, .5);
                            if (result.Polygons.Count > 1)
                            {
                                isSplitted = true;
                                EditOverlay.EditShapesLayer.InternalFeatures.Remove(featureToSplit);
                                foreach (var polygon in result.Polygons)
                                {
                                    Feature newFeature = new Feature(polygon, featureToSplit.ColumnValues);
                                    EditOverlay.EditShapesLayer.InternalFeatures.Add(newFeature.Id, newFeature);
                                    editOverlay.NewFeatureIds.Add(newFeature.Id);
                                }
                            }
                        }
                        else if (currentFeatureType == WellKnownType.Line || currentFeatureType == WellKnownType.Multiline)
                        {
                            EditOverlay.EditShapesLayer.InternalFeatures.Remove(featureToSplit);
                            if (currentFeatureType == WellKnownType.Line)
                            {
                                LineShape lineToSplit = (LineShape)featureToSplit.GetShape();
                                var successed = SplitLineShape(splittingLine, featureToSplit, lineToSplit);
                                if (successed) isSplitted = true;
                            }
                            else
                            {
                                Collection<LineShape> linesToSplit = ((MultilineShape)featureToSplit.GetShape()).Lines;
                                foreach (var lineToSplit in linesToSplit)
                                {
                                    var successed = SplitLineShape(splittingLine, featureToSplit, lineToSplit);
                                    if (successed) isSplitted = true;
                                }
                            }
                        }

                        EditOverlay.TakeSnapshot();
                        EditOverlay.EditShapesLayer.BuildIndex();
                        EditOverlay.Refresh();
                    }
                }

                if (!isSplitted)
                {
                    System.Windows.Forms.MessageBox.Show(cannotSplitMessage, "Split Operation Failed", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }

            trackOverlay.TrackMode = TrackMode.None;
            EditOverlay.TrackResultProcessMode = TrackResultProcessMode.None;
            EditOverlay.IsEnabled = true;
            RefreshCursor();
        }

        private bool SplitLineShape(LineShape splittingLine, Feature featureToSplit, LineShape lineToSplit)
        {
            bool isSplited = true;
            try
            {
                MultipointShape crossingPoints = lineToSplit.GetCrossing(splittingLine);
                if (crossingPoints.Points.Count > 0)
                {
                    Collection<Vertex> lineVertices = new Collection<Vertex>();
                    lineVertices.Add(lineToSplit.Vertices.First());

                    for (int i = 1; i < lineToSplit.Vertices.Count; i++)
                    {
                        lineVertices.Add(lineToSplit.Vertices[i]);
                        LineShape tempLine = new LineShape(lineVertices);
                        var lineCrossingPoints = crossingPoints.Points
                            .Where(p => p.Buffer(0.01, 4, GeographyUnit.Meter, DistanceUnit.Meter)
                                .Intersects(tempLine));

                        if (lineCrossingPoints.Count() == 1
                            && (lineVertices.First().X == lineCrossingPoints.First().X && lineVertices.First().Y == lineCrossingPoints.First().Y))
                            continue;

                        if (lineCrossingPoints.Count() > 0)
                        {
                            var tempCrossingPoints = new Collection<PointShape>();

                            foreach (var point in lineCrossingPoints)
                            {
                                if (!lineVertices.Contains(new Vertex(point))) tempCrossingPoints.Add(point.CloneDeep() as PointShape);
                            }

                            foreach (var point in tempCrossingPoints)
                            {
                                tempLine.Vertices.RemoveAt(tempLine.Vertices.Count - 1);
                                tempLine.Vertices.Add(new Vertex(point.X, point.Y));
                                Feature newFeature = new Feature(tempLine, featureToSplit.ColumnValues);
                                EditOverlay.EditShapesLayer.InternalFeatures.Add(newFeature.Id, newFeature);
                                editOverlay.NewFeatureIds.Add(newFeature.Id);

                                lineVertices.Clear();
                                lineVertices.Add(new Vertex(point.X, point.Y));
                                lineVertices.Add(lineToSplit.Vertices[i]);
                                tempLine = new LineShape(lineVertices);
                            }
                        }
                    }

                    if (lineVertices.Count > 1)
                    {
                        LineShape tempLine = new LineShape(lineVertices);
                        Feature newFeature = new Feature(tempLine, featureToSplit.ColumnValues);
                        EditOverlay.EditShapesLayer.InternalFeatures.Add(newFeature.Id, newFeature);
                        editOverlay.NewFeatureIds.Add(newFeature.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                isSplited = false;
            }

            return isSplited;
        }

        private void TrackEndedWithNormalProcess(BaseShape trackShape)
        {
            FeatureLayer editingLayer = SelectedLayer.Value;
            editingLayer.SafeProcess(() =>
            {
                if (trackShape is PointShape && editingLayer != null && (editingLayer is ShapeFileFeatureLayer && ((ShapeFileFeatureLayer)editingLayer).GetShapeFileType() == ShapeFileType.Multipoint))
                {
                    trackShape = new MultipointShape(new PointShape[] { (PointShape)trackShape });
                }
            });

            Feature feature = new Feature(trackShape);
            if (!editOverlay.EditShapesLayer.IsOpen)
            {
                editOverlay.EditShapesLayer.Open();
            }
            foreach (FeatureSourceColumn column in editOverlay.EditShapesLayer.QueryTools.GetColumns())
            {
                if (column.TypeName == "Double" || column.TypeName == "Integer")
                {
                    feature.ColumnValues[column.ColumnName] = 0.ToString();
                }
                else
                {
                    feature.ColumnValues[column.ColumnName] = string.Empty;
                }

                EditorUIPlugin editorPlugin = GisEditor.UIManager.GetActiveUIPlugins<EditorUIPlugin>().FirstOrDefault();
                if (editorPlugin != null)
                {
                    string id = SelectedLayer.Value.FeatureSource.Id;
                    if (editorPlugin.CalculatedColumns.ContainsKey(id))
                    {
                        var calculateColumn = editorPlugin.CalculatedColumns[id].FirstOrDefault(c => c.ColumnName == column.ColumnName);
                        if (calculateColumn != null)
                        {
                            var calculatedValue = string.Empty;
                            switch (calculateColumn.CalculationType)
                            {
                                case CalculatedDbfColumnType.Perimeter:
                                    var perimeterShape = trackShape as AreaBaseShape;
                                    if (perimeterShape != null)
                                    {
                                        calculatedValue = perimeterShape.GetPerimeter(GeographyUnit.DecimalDegree, calculateColumn.LengthUnit).ToString();
                                    }
                                    break;

                                case CalculatedDbfColumnType.Area:
                                    var areaShape = trackShape as AreaBaseShape;
                                    if (areaShape != null)
                                    {
                                        calculatedValue = areaShape.GetArea(GeographyUnit.DecimalDegree, calculateColumn.AreaUnit).ToString();
                                    }
                                    break;

                                case CalculatedDbfColumnType.Length:
                                    var lineShape = trackShape as LineBaseShape;
                                    if (lineShape != null)
                                    {
                                        calculatedValue = lineShape.GetLength(GeographyUnit.DecimalDegree, calculateColumn.LengthUnit).ToString();
                                    }
                                    break;

                                default:
                                    break;
                            }

                            if (calculatedValue.Length > calculateColumn.Length)
                            {
                                calculatedValue = calculatedValue.Substring(0, calculateColumn.Length);
                            }
                            feature.ColumnValues[calculateColumn.ColumnName] = calculatedValue;
                        }
                    }
                }
            }
            editOverlay.NewFeatureIds.Add(feature.Id);

            // Add new features to edit overlay
            editOverlay.EditShapesLayer.InternalFeatures.Add(feature.Id, feature);
            editOverlay.EditShapesLayer.BuildIndex();
            editOverlay.Refresh();
            editOverlay.TakeSnapshot();

            GisEditor.ActiveMap.TrackOverlay.TrackShapeLayer.InternalFeatures.Clear();
            GisEditor.ActiveMap.TrackOverlay.Refresh();

            // Edit new feature's attribute
            Dictionary<FeatureLayer, Collection<Feature>> features = new Dictionary<FeatureLayer, Collection<Feature>>();
            features[SelectedLayer.Value] = new Collection<Feature>();
            features[SelectedLayer.Value].Add(feature);

            if (Singleton<EditorSetting>.Instance.IsAttributePrompted)
            {
                ShowFeatureAttributeWindow(features);
            }

            //SaveEditing();
            //RefreshEditOverlay();
            SaveEditingCommand.RaiseExecuteChanged();

            lock (GisEditor.ActiveMap.ToolsGrid.Children)
            {
                var snappingCircle = GisEditor.ActiveMap.ToolsGrid.Children.OfType<System.Windows.Shapes.Ellipse>().FirstOrDefault();
                if (snappingCircle != null)
                {
                    GisEditor.ActiveMap.ToolsGrid.Children.Remove(snappingCircle);
                }
            }
        }

        private void TrackOverlay_Drawn(object sender, DrawnOverlayEventArgs e)
        {
            var trackOverlay = sender as TrackInteractiveOverlay;
            var snappingCircle = trackOverlay.OverlayCanvas.Children.OfType<System.Windows.Shapes.Ellipse>().FirstOrDefault();
            if (snappingCircle != null)
            {
                var screenPosition = Mouse.GetPosition(GisEditor.ActiveMap);
                var worldPosition = GisEditor.ActiveMap.ToWorldCoordinate(screenPosition);

                //TrackOverlay_MouseMoved(trackOverlay, new MouseMovedTrackInteractiveOverlayEventArgs(new Vertex(worldPosition.X, worldPosition.Y), lastAffectedFeature));
            }
        }

        //private void TrackOverlay_MouseMoved(object sender, MouseMovedTrackInteractiveOverlayEventArgs e)
        //{
        //lastAffectedFeature = e.AffectedFeature;
        //var trackOverlay = sender as TrackInteractiveOverlay;
        //if ((trackOverlay.TrackMode == TrackMode.Polygon || trackOverlay.TrackMode == TrackMode.Line) && editOverlay.SnappingLayers.Count > 0)
        //{
        //    PointShape snappedPoint = GetSnappingPoint(e.MovedVertex, e.AffectedFeature);
        //    if (snappedPoint != null)
        //    {
        //        e.MovedVertex = new Vertex(snappedPoint);
        //        lock (trackOverlay.OverlayCanvas.Children)
        //        {
        //            var snappingCircle = trackOverlay.OverlayCanvas.Children.OfType<System.Windows.Shapes.Ellipse>().FirstOrDefault();
        //            if (snappingCircle == null)
        //            {
        //                snappingCircle = new System.Windows.Shapes.Ellipse();
        //                snappingCircle.IsHitTestVisible = false;
        //                snappingCircle.HorizontalAlignment = HorizontalAlignment.Left;
        //                snappingCircle.VerticalAlignment = VerticalAlignment.Top;
        //                snappingCircle.Stroke = new SolidColorBrush(Colors.Black);
        //                snappingCircle.StrokeThickness = 1;
        //                trackOverlay.OverlayCanvas.Children.Add(snappingCircle);
        //            }

        //            var snappingDistance = editOverlay.SnappingDistance;
        //            var snappingDistanceUnit = editOverlay.SnappingDistanceUnit;
        //            var snappingScreenPoint = GisEditor.ActiveMap.ToScreenCoordinate(snappedPoint);
        //            var snappingArea = snappedPoint.Buffer(snappingDistance, editOverlay.MapArguments.MapUnit, snappingDistanceUnit).GetBoundingBox();
        //            var snappingScreenSize = Math.Max(snappingArea.Width, snappingArea.Height) / GisEditor.ActiveMap.CurrentResolution;
        //            snappingCircle.Width = snappingScreenSize;
        //            snappingCircle.Height = snappingScreenSize;
        //            snappingCircle.Margin = new Thickness(snappingScreenPoint.X - snappingScreenSize * .5, snappingScreenPoint.Y - snappingScreenSize * .5, 0, 0);
        //        }
        //    }
        //}
        //}

        //private void TrackOverlay_TrackStarted(object sender, TrackStartedTrackInteractiveOverlayEventArgs e)
        //{
        //PointShape snappedPoint = GetSnappingPoint(e.StartedVertex, new Feature());
        //if (snappedPoint != null) e.StartedVertex = new Vertex(snappedPoint);
        //}

        private void EditOverlay_FeatureTrackEnded(object sender, FeatureTrackEndedGisEditorEditInteractiveOverlayEventArgs e)
        {
            foreach (var item in e.SelectedFeatures)
            {
                if (!editOverlay.EditShapesLayer.InternalFeatures.Contains(item.Id))
                {
                    editOverlay.EditShapesLayer.InternalFeatures.Add(item.Id, item);
                }
            }

            Collection<Feature> allFeatures = new Collection<Feature>();
            foreach (var item in editOverlay.EditShapesLayer.InternalFeatures)
            {
                allFeatures.Add(item);
            }

            foreach (var item in editOverlay.EditCandidatesLayer.InternalFeatures)
            {
                if (!allFeatures.Any(f => f.GetWellKnownText() == item.GetWellKnownText()))
                {
                    allFeatures.Add(item);
                }
            }

            foreach (var item in e.SelectedFeatures)
            {
                if (!editOverlay.EditShapesLayer.InternalFeatures.Contains(item.Id))
                {
                    editOverlay.EditShapesLayer.InternalFeatures.Add(item.Id, item);
                }
            }

            editOverlay.EditShapesLayer.InternalFeatures.Clear();
            editOverlay.EditCandidatesLayer.InternalFeatures.Clear();
            SelectedLayer.Value.FeatureIdsToExclude.Clear();

            foreach (var item in allFeatures)
            {
                if (e.SelectedFeatures.Any(f => f.Id == item.Id))
                {
                    editOverlay.EditShapesLayer.InternalFeatures.Add(item.Id, item);
                    SelectedLayer.Value.FeatureIdsToExclude.Add(item.Id);
                }
                else
                {
                    var selectedFeature = SelectedLayer.Value.FeatureSource.GetFeatureById(item.Id, ReturningColumnsType.NoColumns);
                    if (selectedFeature.GetWellKnownText() != item.GetWellKnownText())
                    {
                        editOverlay.EditCandidatesLayer.InternalFeatures.Add(item.Id, item);
                        SelectedLayer.Value.FeatureIdsToExclude.Add(item.Id);
                    }
                }
            }

            editOverlay.EditShapesLayer.BuildIndex();
            editOverlay.EditCandidatesLayer.BuildIndex();

            RefreshOverlayContainsActiveFeatureLayer();

            editOverlay.TakeSnapshot();
        }

        private void EditOverlay_ModifierClick(object sender, FeatureClickedGisEditorEditInteractiveOverlayEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.None)
            {
                double searchWorldTolerance = GisEditor.ActiveMap.CurrentResolution * searchTolerance;
                RectangleShape searchingArea = new RectangleShape(e.Arguments.WorldX - searchWorldTolerance,
                    e.Arguments.WorldY + searchWorldTolerance,
                    e.Arguments.WorldX + searchWorldTolerance,
                    e.Arguments.WorldY - searchWorldTolerance);

                FeatureLayer targetFeatureLayer = SelectedLayer.Value;

                lock (targetFeatureLayer)
                {
                    if (!targetFeatureLayer.IsOpen) targetFeatureLayer.Open();

                    PointShape point = new PointShape(e.Arguments.WorldX, e.Arguments.WorldY);

                    Collection<Feature> selectedFeaturesInShapeLayer = new Collection<Feature>();
                    Collection<Feature> selectedFeaturesInTargetLayer = new Collection<Feature>();

                    try
                    {
                        selectedFeaturesInTargetLayer = targetFeatureLayer.QueryTools.GetFeaturesIntersecting(point, targetFeatureLayer.GetDistinctColumnNames(), true);
                        FindOutNearestFeatures(ref selectedFeaturesInTargetLayer, targetFeatureLayer, point);
                        editOverlay.EditShapesLayer.BuildIndex();

                        selectedFeaturesInShapeLayer = editOverlay.EditShapesLayer.QueryTools.GetFeaturesIntersecting(point, editOverlay.EditShapesLayer.GetDistinctColumnNames(), true);

                        if (selectedFeaturesInShapeLayer.Count == 0)
                        {
                            selectedFeaturesInShapeLayer = editOverlay.EditCandidatesLayer.QueryTools.GetFeaturesIntersecting(point, editOverlay.EditCandidatesLayer.GetDistinctColumnNames(), true);
                        }
                    }
                    catch (Exception ex)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                        System.Windows.Forms.MessageBox.Show(ex.Message, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    }

                    FindOutNearestFeatures(ref selectedFeaturesInShapeLayer, editOverlay.EditShapesLayer, point);
                    if (selectedFeaturesInTargetLayer.Count > 0 || selectedFeaturesInShapeLayer.Count > 0)
                    {
                        Collection<Feature> selectedFeatures = new Collection<Feature>();
                        if (selectedFeaturesInTargetLayer.Count == 1)
                        {
                            selectedFeatures.Add(selectedFeaturesInTargetLayer.First());

                            //Syce ChooseMultipleEditFeaturesWindow
                            OpenOrRefreshChooseMultipleEditFeaturesWindow(selectedFeaturesInTargetLayer, searchingArea, selectedFeaturesInShapeLayer);
                        }
                        else if (selectedFeaturesInTargetLayer.Count > 1)
                        {
                            //If ChooseMultipleEditFeaturesWindow not open, open it.
                            OpenOrRefreshChooseMultipleEditFeaturesWindow(selectedFeaturesInTargetLayer, searchingArea, selectedFeaturesInShapeLayer, true);
                            return;
                        }

                        foreach (var feature in selectedFeaturesInShapeLayer)
                            selectedFeatures.Add(feature);

                        SelectOrUnselectAFeature(targetFeatureLayer, selectedFeatures);
                        if (selectedFeatures.Count > 0)
                        {
                            e.ClickedFeature = selectedFeatures.Last();
                        }

                        if (editOverlay.EditShapesLayer.InternalFeatures.Count > 0)
                        {
                            if (editOverlay.CanReshape)
                            {
                                if (tmpEditShapeFeatures.Length == 0 || (editOverlay.ReshapeControlPointsLayer.InternalFeatures.Count == 0
                                    && !isAllSelectedFeaturesModified
                                    && System.Windows.Forms.DialogResult.Yes == System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("EditingToolsViewModelWarningText"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.YesNo)))
                                {
                                    editOverlay.ReshapeControlPointsLayer.InternalFeatures.Clear();
                                    editOverlay.CalculateReshapeControlPoints(e.ClickedFeature);
                                }
                                else if (editOverlay.ReshapeControlPointsLayer.InternalFeatures.Count > 0 || isAllSelectedFeaturesModified)
                                {
                                    editOverlay.ReshapeControlPointsLayer.InternalFeatures.Clear();
                                    editOverlay.CalculateReshapeControlPoints(e.ClickedFeature);
                                }
                                else
                                {
                                    targetFeatureLayer.FeatureIdsToExclude.Clear();
                                    RestoreFeatureIdsToExclude(targetFeatureLayer);
                                    editOverlay.EditShapesLayer.InternalFeatures.Clear();
                                    RestoreEditShapeFeatures();
                                    editOverlay.EditShapesLayer.BuildIndex();
                                }
                            }
                            else if (editOverlay.CanResize || editOverlay.CanRotate)
                            {
                                editOverlay.ClearVertexControlPoints();
                                editOverlay.CalculateAssociateControlPoints(e.ClickedFeature);
                            }
                        }
                        else
                        {
                            editOverlay.ClearVertexControlPoints();
                        }

                        //to make sure the highlight overlay refresh after the target overlay
                        var targetOverlay = GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>().Where(overlay => overlay.Layers.Any(layer => editOverlay.EditTargetLayer != null && layer.Name == editOverlay.EditTargetLayer.Name)).FirstOrDefault();

                        if (targetOverlay != null)
                        {
                            targetOverlay.Drawn -= new EventHandler<DrawnOverlayEventArgs>(TargetOverlay_Drawn);
                            targetOverlay.Drawn += new EventHandler<DrawnOverlayEventArgs>(TargetOverlay_Drawn);

                            RefreshOverlayContainsActiveFeatureLayer();
                        }
                    }
                }
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void OpenOrRefreshChooseMultipleEditFeaturesWindow(Collection<Feature> selectedFeaturesInTargetLayer, RectangleShape searchingArea, Collection<Feature> selectedFeaturesInShapeLayer, bool needOpen = false)
        {
            var clickedFeatures = selectedFeaturesInTargetLayer.Where(tmpFeature => searchingArea.Intersects(tmpFeature.GetShape()));
            var featureItems = new Collection<FeatureIdItem>();
            foreach (var item in clickedFeatures)
            {
                FeatureIdItem newItem = new FeatureIdItem(item);
                if (selectedFeaturesInShapeLayer.Any(f => f.Id == item.Id))
                {
                    newItem.IsChecked = true;
                }
                newItem.Name = "Feature Id:" + item.Id;
                featureItems.Add(newItem);
            }
            bool isOpen = false;

            ChooseMultipleEditFeaturesWindow currentWindow = null;
            foreach (var tempWindow in Application.Current.Windows.OfType<Window>())
            {
                if (tempWindow.Name == ChooseMultipleEditFeaturesWindow.ChooseMultipleEditFeaturesWindowName)
                {
                    isOpen = true;
                    currentWindow = tempWindow as ChooseMultipleEditFeaturesWindow;
                    break;
                }
            }
            if (!isOpen && needOpen)
            {
                ChooseMultipleEditFeaturesWindow window = new ChooseMultipleEditFeaturesWindow(featureItems);
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Owner = Application.Current.MainWindow;
                window.SelectedFeaturesChanged += Window_SelectedFeaturesChanged;
                window.Show();
            }
            else if (currentWindow != null)
            {
                currentWindow.RefreshData(featureItems);
            }
        }

        private void Window_SelectedFeaturesChanged(object sender, SelectedFeaturesChangedEventArgs e)
        {
            FeatureLayer targetFeatureLayer = SelectedLayer.Value;

            SelectOrUnselectAFeature(targetFeatureLayer, e.SelectedFeatures);

            //to make sure the highlight overlay refresh after the target overlay
            var targetOverlay = GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>().Where(overlay => overlay.Layers.Any(layer => editOverlay.EditTargetLayer != null && layer.Name == editOverlay.EditTargetLayer.Name)).FirstOrDefault();

            if (targetOverlay != null)
            {
                targetOverlay.Drawn -= new EventHandler<DrawnOverlayEventArgs>(TargetOverlay_Drawn);
                targetOverlay.Drawn += new EventHandler<DrawnOverlayEventArgs>(TargetOverlay_Drawn);

                RefreshOverlayContainsActiveFeatureLayer();
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private void TargetOverlay_Drawn(object sender, DrawnOverlayEventArgs e)
        {
            var targetOverlay = sender as Overlay;
            if (targetOverlay != null)
            {
                editOverlay.Refresh();
                targetOverlay.Drawn -= TargetOverlay_Drawn;
            }
        }

        private void DrawingToolItem_Selected(object sender, EventArgs e)
        {
            if (GisEditor.ActiveMap != null && (SelectedDrawingTool == null || ((KeyValuePair<TrackMode, DrawingToolItemModel>)SelectedDrawingTool).Value != sender))
            {
                SelectedDrawingTool = DrawingTools.FirstOrDefault(s => s.Value == sender);
                TrackMode trackMode = ((KeyValuePair<TrackMode, DrawingToolItemModel>)selectedDrawingTool).Key;
                switch (trackMode)
                {
                    case TrackMode.Line:
                        ShowDrawingLinesHint();
                        break;

                    case TrackMode.Polygon:
                        ShowDrawingPolygonsHint();
                        break;
                }
            }
        }

        #endregion Events

        private void ShowDrawingLinesHint()
        {
            if (MapHelper.ShowHintWindow("ShowDrawingLinesHint"))
            {
                var title = GisEditor.LanguageManager.GetStringResource("EditingToolsViewModelDrawingLinesTitle");
                var description = GisEditor.LanguageManager.GetStringResource("EditingToolsViewModelHowEasyDescription");
                var steps = new Collection<String>()
                {
                        GisEditor.LanguageManager.GetStringResource("EditingToolsViewModel1SelectLineText"),
                        GisEditor.LanguageManager.GetStringResource("EditingToolsViewModel2ClickMapText"),
                        GisEditor.LanguageManager.GetStringResource("EditingToolsViewModel3MoveMouseText"),
                        GisEditor.LanguageManager.GetStringResource("EditingToolsViewModel4ClickAgainText"),
                        GisEditor.LanguageManager.GetStringResource("EditingToolsViewModel5DoubleClickFinishText"),
                };
                GisEditorHintWindow gisEditorHintWindow = new GisEditorHintWindow(title, description, steps, "/GisEditorPluginCore;component/Images/Hints/Line Drawing.gif");
                gisEditorHintWindow.ShowDialog();
                var result = (gisEditorHintWindow.DataContext as GisEditorHintViewModel).DonotShowAgain;
                MapHelper.SetShowHint("ShowDrawingLinesHint", !result);
            }
        }

        private void ShowDrawingPolygonsHint()
        {
            if (MapHelper.ShowHintWindow("ShowDrawingPolygonsHint"))
            {
                var title = GisEditor.LanguageManager.GetStringResource("EditingToolsViewModelDrawingPolygonsTitle");
                var description = GisEditor.LanguageManager.GetStringResource("EditingToolsViewModelHowEasyDrawPolygonsDescription");
                var steps = new Collection<String>()
                {
                        GisEditor.LanguageManager.GetStringResource("EditingToolsViewModel1SelectPolygonText"),
                        GisEditor.LanguageManager.GetStringResource("EditingToolsViewModel2ClickMapText"),
                        GisEditor.LanguageManager.GetStringResource("EditingToolsViewModel3MoveMousePolygonText"),
                        GisEditor.LanguageManager.GetStringResource("EditingToolsViewModel4AddVertexContinueText"),
                        GisEditor.LanguageManager.GetStringResource("EditingToolsViewModel5DoubleClickFinishPolygonText")
                };
                GisEditorHintWindow gisEditorHintWindow = new GisEditorHintWindow(title, description, steps, "/GisEditorPluginCore;component/Images/Hints/Polygon Drawing.gif");
                gisEditorHintWindow.ShowDialog();
                var result = (gisEditorHintWindow.DataContext as GisEditorHintViewModel).DonotShowAgain;
                MapHelper.SetShowHint("ShowDrawingPolygonsHint", !result);
            }
        }

        private void SelectOrUnselectAFeature(FeatureLayer targetFeatureLayer, Collection<Feature> selectedFeatures, bool isSelectedFeatureJustCreated = false)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift && !editOverlay.CanReshape)
            {
                foreach (var selectedFeature in selectedFeatures)
                {
                    if (targetFeatureLayer.FeatureIdsToExclude.Contains(selectedFeature.Id))
                        targetFeatureLayer.FeatureIdsToExclude.Remove(selectedFeature.Id);
                    else if (!isSelectedFeatureJustCreated)
                        targetFeatureLayer.FeatureIdsToExclude.Add(selectedFeature.Id);

                    if (editOverlay.EditShapesLayer.InternalFeatures.Contains(selectedFeature.Id))
                        editOverlay.EditShapesLayer.InternalFeatures.Remove(selectedFeature.Id);
                    else
                        editOverlay.EditShapesLayer.InternalFeatures.Add(selectedFeature.Id, selectedFeature);
                }
            }
            else
            {
                BackupEditShapeFeatures();
                BackupFeatureIdsToExclude(targetFeatureLayer);
                targetFeatureLayer.FeatureIdsToExclude.Clear();

                Collection<Feature> featuresUserNotEdit = new Collection<Feature>();
                targetFeatureLayer.SafeProcess(() =>
                {
                    var featureIdsToExclude = tmpFeatureIdsToExclude.ToList();
                    int result = 0;
                    foreach (var feature in editOverlay.EditShapesLayer.InternalFeatures)
                    {
                        if (int.TryParse(feature.Id, out result))
                        {
                            var originalFeature = targetFeatureLayer.QueryTools.GetFeatureById(feature.Id, feature.ColumnValues.Keys);
                            string wkt = originalFeature.GetWellKnownText();
                            if (wkt.Equals(feature.GetWellKnownText()) && selectedFeatures.All(f => f.Id != feature.Id))
                            {
                                featuresUserNotEdit.Add(feature);
                                isAllSelectedFeaturesModified = false;
                            }
                        }
                    }
                });

                RestoreFeatureIdsToExclude(targetFeatureLayer);
                foreach (var item in featuresUserNotEdit)
                {
                    targetFeatureLayer.FeatureIdsToExclude.Remove(item.Id);
                    editOverlay.EditShapesLayer.InternalFeatures.Remove(item);
                }

                bool needTakeSnapshot = false;
                foreach (var selectedFeature in selectedFeatures)
                {
                    if (!targetFeatureLayer.FeatureIdsToExclude.Contains(selectedFeature.Id))
                    {
                        if (!isSelectedFeatureJustCreated)
                            targetFeatureLayer.FeatureIdsToExclude.Add(selectedFeature.Id);
                        if (!editOverlay.EditShapesLayer.InternalFeatures.Any(i => i.Id.Equals(selectedFeature.Id, StringComparison.OrdinalIgnoreCase)))
                            editOverlay.EditShapesLayer.InternalFeatures.Add(selectedFeature.Id, selectedFeature);
                        needTakeSnapshot = true;
                    }
                }
                if (needTakeSnapshot) editOverlay.TakeSnapshot();

                Collection<Feature> allFeatures = new Collection<Feature>();
                foreach (var item in editOverlay.EditShapesLayer.InternalFeatures)
                {
                    allFeatures.Add(item);
                }
                foreach (var item in editOverlay.EditCandidatesLayer.InternalFeatures)
                {
                    if (!allFeatures.Any(f => f.GetWellKnownText() == item.GetWellKnownText()))
                    {
                        allFeatures.Add(item);
                    }
                }

                editOverlay.EditShapesLayer.InternalFeatures.Clear();
                editOverlay.EditCandidatesLayer.InternalFeatures.Clear();
                foreach (var item in allFeatures)
                {
                    string wkt = item.GetWellKnownText();
                    if (selectedFeatures.Any(s => s.GetWellKnownText() == wkt))
                    {
                        editOverlay.EditShapesLayer.InternalFeatures.Add(item.Id, item);
                    }
                    else
                    {
                        editOverlay.EditCandidatesLayer.InternalFeatures.Add(item.Id, item);
                    }
                }
            }
            editOverlay.EditShapesLayer.BuildIndex();
            editOverlay.EditCandidatesLayer.BuildIndex();
        }

        private void RefreshTargetLayerList()
        {
            UpdateTargetLayers();

            var newSelectedItem = AvailableLayers.FirstOrDefault(m => m.Value == editOverlay.EditTargetLayer);
            if (newSelectedItem != null)
            {
                foreach (var item in AvailableLayers)
                {
                    item.IsChecked = false;
                }
                newSelectedItem.IsChecked = true;
            }
            else
            {
                SelectIsChecked = false;
                DisableAllDrawingTools();
                editOverlay.Cancel();
                if (AvailableLayers.Count > 0)
                {
                    newSelectedItem = AvailableLayers.FirstOrDefault();
                }
            }
            RaisePropertyChanged("SelectedLayer");
        }

        private void UpdateTargetLayers()
        {
            AvailableLayers.Clear();
            var nonSelectionItem = new CheckableItemViewModel<FeatureLayer>(null, true, f => GisEditor.LanguageManager.GetStringResource("EditingToolsEditLayerText"));
            nonSelectionItem.PropertyChanged += SelectionItem_PropertyChanged;
            AvailableLayers.Add(nonSelectionItem);
            if (GisEditor.ActiveMap != null)
            {
                foreach (FeatureLayer layer in GisEditor.ActiveMap.GetFeatureLayers(true).Where(f => f.FeatureSource.IsEditable))
                {
                    var item = new CheckableItemViewModel<FeatureLayer>(layer, false, f => f.Name);
                    item.PropertyChanged += SelectionItem_PropertyChanged;
                    AvailableLayers.Add(item);
                }
            }
        }

        private void SelectionItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsChecked") && (sender as CheckableItemViewModel<FeatureLayer>).IsChecked)
            {
                RaisePropertyChanged("SelectedLayer");
            }
        }

        private System.Windows.Forms.DialogResult CheckWhetherSaveEditing()
        {
            var result = System.Windows.Forms.DialogResult.None;

            if (editOverlay.IsEdited)
            {
                result = System.Windows.Forms.MessageBox.Show(String.Format("Do you want to save your edits to layer: {0}?", editOverlay.EditTargetLayer.Name), "Save", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Asterisk);

                switch (result)
                {
                    case System.Windows.Forms.DialogResult.Yes:
                        SaveEditing();
                        RefreshEditOverlay();
                        break;

                    case System.Windows.Forms.DialogResult.No:
                        RefreshEditOverlay();
                        break;

                    case System.Windows.Forms.DialogResult.Cancel:
                        break;
                }
            }
            else
            {
                RefreshEditOverlay();
            }

            return result;
        }

        private void RefreshEditOverlay()
        {
            foreach (var layerOverlay in GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>())
            {
                foreach (var layer in layerOverlay.Layers.OfType<FeatureLayer>())
                {
                    layer.FeatureIdsToExclude.Clear();
                }
            }

            editOverlay.AssociateControlPointsLayer.InternalFeatures.Clear();
            editOverlay.EditShapesLayer.InternalFeatures.Clear();
            editOverlay.EditShapesLayer.BuildIndex();
            editOverlay.EditCandidatesLayer.InternalFeatures.Clear();
            editOverlay.EditCandidatesLayer.BuildIndex();
            editOverlay.ClearVertexControlPoints();
            editOverlay.ClearSnapshots();

            Application.Current.Dispatcher.BeginInvoke(obj =>
            {
                RefreshOverlayContainsActiveFeatureLayer((FeatureLayer)obj);
                editOverlay.Refresh();
            }, editOverlay.EditTargetLayer, DispatcherPriority.Background);
        }

        private void InitializeDrawingTools()
        {
            drawingTools = new Dictionary<TrackMode, DrawingToolItemModel>();
            drawingTools[TrackMode.Point] = new DrawingToolItemModel
            {
                ImageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/images/point.png", UriKind.RelativeOrAbsolute)),
                Text = GisEditor.LanguageManager.GetStringResource("NavigatePluginPointLabel")
            };
            drawingTools[TrackMode.Point].Selected += new EventHandler(DrawingToolItem_Selected);

            drawingTools[TrackMode.Multipoint] = new DrawingToolItemModel
            {
                ImageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/images/Multipoint.png", UriKind.RelativeOrAbsolute)),
                Text = GisEditor.LanguageManager.GetStringResource("GeneralShapePultipointName")
            };
            drawingTools[TrackMode.Multipoint].Selected += new EventHandler(DrawingToolItem_Selected);

            drawingTools[TrackMode.Line] = new DrawingToolItemModel
            {
                ImageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/images/polyline.png", UriKind.RelativeOrAbsolute)),
                Text = GisEditor.LanguageManager.GetStringResource("NavigatePluginLineLabel")
            };
            drawingTools[TrackMode.Line].Selected += new EventHandler(DrawingToolItem_Selected);

            drawingTools[TrackMode.Polygon] = new DrawingToolItemModel
            {
                ImageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/images/polygon.png", UriKind.RelativeOrAbsolute)),
                Text = GisEditor.LanguageManager.GetStringResource("MeasureRibbonGroupPolygonLabel")
            };
            drawingTools[TrackMode.Polygon].Selected += new EventHandler(DrawingToolItem_Selected);

            drawingTools[TrackMode.Rectangle] = new DrawingToolItemModel
            {
                ImageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/images/rectangle.png", UriKind.RelativeOrAbsolute)),
                Text = GisEditor.LanguageManager.GetStringResource("MeasureRibbonGroupRectangleLabel")
            };
            drawingTools[TrackMode.Rectangle].Selected += new EventHandler(DrawingToolItem_Selected);

            drawingTools[TrackMode.Square] = new DrawingToolItemModel
            {
                ImageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/images/square.png", UriKind.RelativeOrAbsolute)),
                Text = GisEditor.LanguageManager.GetStringResource("MeasureRibbonGroupSquareLabel")
            };
            drawingTools[TrackMode.Square].Selected += new EventHandler(DrawingToolItem_Selected);

            drawingTools[TrackMode.Ellipse] = new DrawingToolItemModel
            {
                ImageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/images/ellipse.png", UriKind.RelativeOrAbsolute)),
                Text = GisEditor.LanguageManager.GetStringResource("MeasureRibbonGroupEllipseLabel")
            };
            drawingTools[TrackMode.Ellipse].Selected += new EventHandler(DrawingToolItem_Selected);

            drawingTools[TrackMode.Circle] = new DrawingToolItemModel
            {
                ImageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/images/Circle.png", UriKind.RelativeOrAbsolute)),
                Text = GisEditor.LanguageManager.GetStringResource("MeasureRibbonGroupCircleLabel")
            };
            drawingTools[TrackMode.Circle].Selected += new EventHandler(DrawingToolItem_Selected);
        }

        private bool CanEdit()
        {
            return SelectedLayer.Value != null;
        }

        private void DisableAllDrawingTools()
        {
            foreach (var item in drawingTools)
            {
                item.Value.IsEnabled = false;
            }
        }

        private void SetEditOverlayMode(bool canReshape, bool canDrag, bool canResize, bool canRotate)
        {
            var actualMap = GisEditor.ActiveMap;
            actualMap.DisableInteractiveOverlaysExclude(editOverlay);
            actualMap.Cursor = GisEditorCursors.Edit;
            editOverlay.IsEnabled = true;
            editOverlay.CanReshape = canReshape;
            editOverlay.CanDrag = canDrag;
            editOverlay.CanResize = canResize;
            editOverlay.CanRotate = canRotate;
            editOverlay.ClearVertexControlPoints();
            if (!EditIsChecked && !SelectIsChecked && !TransformIsChecked && !MoveIsChecked)
            {
                actualMap.Cursor = GisEditorCursors.Normal;
                editOverlay.Disable();
            }

            if (canDrag && canResize && canRotate)
            {
                editOverlay.CalculateAssociateControlPoints();
            }

            editOverlay.Refresh();
            SelectedDrawingTool = null;
            GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.SetEditOverlayModeDescription));
        }

        private void RollBack()
        {
            editOverlay.Rollback();
            RefreshActiveFeatureLayer();
        }

        private void Forward()
        {
            editOverlay.Forward();
            RefreshActiveFeatureLayer();
        }

        private void RefreshActiveFeatureLayer()
        {
            if (SelectedLayer.Value != null)
            {
                SelectedLayer.Value.FeatureIdsToExclude.Clear();
                foreach (var feature in editOverlay.EditShapesLayer.InternalFeatures)
                {
                    if (!editOverlay.NewFeatureIds.Contains(feature.Id))
                    {
                        SelectedLayer.Value.FeatureIdsToExclude.Add(feature.Id);
                    }
                }
                RefreshOverlayContainsActiveFeatureLayer();
            }
        }

        private void RefreshOverlayContainsActiveFeatureLayer()
        {
            RefreshOverlayContainsActiveFeatureLayer(SelectedLayer.Value);
        }

        private void RefreshOverlayContainsActiveFeatureLayer(FeatureLayer featureLayer)
        {
            if (featureLayer != null)
            {
                foreach (var overlay in GisEditor.ActiveMap.FindLayerOverlayContaining(featureLayer))
                {
                    overlay.Invalidate();
                }
            }
        }

        private bool ShowFeatureAttributeWindow(Dictionary<FeatureLayer, Collection<Feature>> entities)
        {
            FeatureAttributeWindow window = new FeatureAttributeWindow(entities, Singleton<EditorSetting>.Instance.IsAttributePrompted);
            return window.ShowDialog().GetValueOrDefault();
        }

        private void SetCursor(TrackMode trackMode)
        {
            var actualMap = GisEditor.ActiveMap;
            if (navigateTypeDic.ContainsKey(trackMode) && trackMode != TrackMode.None)
            {
                actualMap.Cursor = navigateTypeDic[trackMode];
            }
        }

        private void BackupFeatureIdsToExclude(FeatureLayer featureLayer)
        {
            tmpFeatureIdsToExclude = new string[featureLayer.FeatureIdsToExclude.Count];
            featureLayer.FeatureIdsToExclude.CopyTo(tmpFeatureIdsToExclude, 0);
        }

        private void RestoreFeatureIdsToExclude(FeatureLayer featureLayer)
        {
            foreach (string excludeId in tmpFeatureIdsToExclude)
            {
                featureLayer.FeatureIdsToExclude.Add(excludeId);
            }
        }

        private void BackupEditShapeFeatures()
        {
            tmpEditShapeFeatures = new Feature[editOverlay.EditShapesLayer.InternalFeatures.Count];
            editOverlay.EditShapesLayer.InternalFeatures.CopyTo(tmpEditShapeFeatures, 0);
        }

        private void RestoreEditShapeFeatures()
        {
            if (tmpEditShapeFeatures != null)
            {
                editOverlay.EditCandidatesLayer.InternalFeatures.Clear();
                foreach (var item in tmpEditShapeFeatures)
                {
                    //editOverlay.EditShapesLayer.InternalFeatures.Add(item.Id, item);
                    editOverlay.EditCandidatesLayer.InternalFeatures.Add(item.Id, item);
                }
            }
        }

        private void FindOutNearestFeatures(ref Collection<Feature> results, FeatureLayer featureLayer, PointShape point)
        {
            try
            {
                if (results.Count == 0)
                {
                    results = featureLayer.QueryTools.GetFeaturesNearestTo(point, GisEditor.ActiveMap.MapUnit, 1, featureLayer.GetDistinctColumnNames());
                    double screenToleranceDistance = 0.002;
                    if (results.Count > 0
                        && results[0].GetShape().GetDistanceTo(point, GisEditor.ActiveMap.MapUnit, DistanceUnit.Meter) > screenToleranceDistance * GisEditor.ActiveMap.CurrentScale)
                    {
                        results.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                results.Clear();
            }
        }

        private void SaveEditing()
        {
            FeatureLayer targetFeatureLayer = editOverlay.EditTargetLayer;
            try
            {
                Proj4Projection proj4Projection = new Proj4Projection();
                proj4Projection.InternalProjectionParametersString = Proj4Projection.GetDecimalDegreesParametersString();
                proj4Projection.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
                proj4Projection.Open();

                Monitor.Enter(targetFeatureLayer);
                targetFeatureLayer.CloseInOverlay();

                if (targetFeatureLayer is ShapeFileFeatureLayer)
                {
                    ((ShapeFileFeatureSource)((ShapeFileFeatureLayer)targetFeatureLayer).FeatureSource).ReadWriteMode = GeoFileReadWriteMode.ReadWrite;
                }

                if (!targetFeatureLayer.IsOpen) targetFeatureLayer.Open();
                if (!targetFeatureLayer.EditTools.IsInTransaction) targetFeatureLayer.EditTools.BeginTransaction();
                foreach (var feature in editOverlay.EditShapesLayer.InternalFeatures)
                {
                    if (!editOverlay.NewFeatureIds.Contains(feature.Id))
                    {
                        targetFeatureLayer.EditTools.Update(feature.GetShape());
                        if (targetFeatureLayer.FeatureIdsToExclude.Contains(feature.Id))
                            targetFeatureLayer.FeatureIdsToExclude.Remove(feature.Id);
                    }
                }

                foreach (var feature in editOverlay.EditCandidatesLayer.InternalFeatures)
                {
                    if (!editOverlay.NewFeatureIds.Contains(feature.Id))
                    {
                        targetFeatureLayer.EditTools.Update(feature.GetShape());
                        if (targetFeatureLayer.FeatureIdsToExclude.Contains(feature.Id))
                            targetFeatureLayer.FeatureIdsToExclude.Remove(feature.Id);
                    }
                }

                TransactionResult transactionResult1 = targetFeatureLayer.EditTools.CommitTransaction();

                if (!targetFeatureLayer.IsOpen) targetFeatureLayer.Open();
                targetFeatureLayer.EditTools.BeginTransaction();
                foreach (var feature in editOverlay.EditShapesLayer.InternalFeatures)
                {
                    if (editOverlay.NewFeatureIds.Contains(feature.Id))
                    {
                        var newFeature = feature;//.MakeValidUsingSqlTypes();
                        ShapeFileFeatureLayer shapeLayer = targetFeatureLayer as ShapeFileFeatureLayer;
                        if (shapeLayer != null)
                        {
                            bool result = CheckShapeTypeIsEqual(shapeLayer.GetShapeFileType(), newFeature.GetWellKnownType());
                            if (result) targetFeatureLayer.EditTools.Add(newFeature);
                        }
                        else
                            targetFeatureLayer.EditTools.Add(newFeature);
                    }
                }

                foreach (var feature in editOverlay.EditCandidatesLayer.InternalFeatures)
                {
                    if (editOverlay.NewFeatureIds.Contains(feature.Id))
                    {
                        var newFeature = feature;//.MakeValidUsingSqlTypes();
                        ShapeFileFeatureLayer shapeLayer = targetFeatureLayer as ShapeFileFeatureLayer;
                        if (shapeLayer != null)
                        {
                            bool result = CheckShapeTypeIsEqual(shapeLayer.GetShapeFileType(), newFeature.GetWellKnownType());
                            if (result) targetFeatureLayer.EditTools.Add(newFeature);
                        }
                        else
                            targetFeatureLayer.EditTools.Add(newFeature);
                    }
                }

                IEnumerable<string> columnNames = targetFeatureLayer.FeatureSource.GetColumns().Select(c => c.ColumnName);
                editOverlay.EditShapesLayer.Open();
                foreach (var column in editOverlay.EditShapesLayer.Columns)
                {
                    if (!columnNames.Any(c => c == column.ColumnName))
                    {
                        targetFeatureLayer.FeatureSource.AddColumn(column);
                    }
                }
                editOverlay.EditShapesLayer.Close();

                editOverlay.EditCandidatesLayer.Open();
                foreach (var column in editOverlay.EditCandidatesLayer.Columns)
                {
                    if (!columnNames.Any(c => c == column.ColumnName) && column.ColumnName != "state")
                    {
                        targetFeatureLayer.FeatureSource.AddColumn(column);
                    }
                }
                editOverlay.EditCandidatesLayer.Close();

                editOverlay.NewFeatureIds.Clear();

                if (targetFeatureLayer.FeatureIdsToExclude.Count != 0)
                {
                    foreach (var featureId in targetFeatureLayer.FeatureIdsToExclude)
                    {
                        targetFeatureLayer.EditTools.Delete(featureId);
                    }
                    targetFeatureLayer.FeatureIdsToExclude.Clear();
                }

                TransactionResult transactionResult2 = targetFeatureLayer.EditTools.CommitTransaction();

                string[] failureReasons = transactionResult1.FailureReasons.Values.Concat(transactionResult2.FailureReasons.Values).ToArray();
                if (failureReasons.Length > 0)
                {
                    TransactionErrorWindow errorWindow = new TransactionErrorWindow(failureReasons);
                    errorWindow.ShowDialog();
                }
            }
            catch (UnauthorizedAccessException accessEx)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, accessEx.Message, new ExceptionInfo(accessEx));
                System.Windows.Forms.MessageBox.Show(accessEx.Message, "Access Denied");
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                GisEditorMessageBox messageBox = new GisEditorMessageBox(MessageBoxButton.OK);
                messageBox.Title = "Warning";
                messageBox.Message = ex.Message;
                messageBox.ErrorMessage = ex.StackTrace;
                if (ex.InnerException != null)
                {
                    messageBox.Message = ex.InnerException.Message;
                    messageBox.ErrorMessage = ex.InnerException.StackTrace;
                }
                messageBox.ShowDialog();
            }
            finally
            {
                if (targetFeatureLayer is ShapeFileFeatureLayer)
                {
                    if (targetFeatureLayer.IsOpen)
                    {
                        lock (targetFeatureLayer)
                        {
                            targetFeatureLayer.Close();
                        }
                    }
                    ((ShapeFileFeatureSource)((ShapeFileFeatureLayer)targetFeatureLayer).FeatureSource).ReadWriteMode = GeoFileReadWriteMode.Read;
                }
                targetFeatureLayer.ReOpen();

                if (targetFeatureLayer != null)
                {
                    var overlay = GisEditor.ActiveMap.GetOverlaysContaining(targetFeatureLayer).FirstOrDefault();
                    if (overlay != null)
                    {
                        overlay.RefreshCache(RefreshCacheMode.ApplyNewCache);
                        overlay.Refresh();
                    }
                }

                Monitor.Exit(targetFeatureLayer);

                //To trigger the EditTargetLayer get method, to set editingFeature as default
                var tmpLayer = editOverlay.EditTargetLayer;
                editOverlay.EditTargetLayer = null;
                editOverlay.EditTargetLayer = tmpLayer;
            }
        }

        private bool CheckShapeTypeIsEqual(ShapeFileType shapeFileType, WellKnownType wellKnownType)
        {
            bool result = false;
            switch (shapeFileType)
            {
                case ShapeFileType.Point:
                case ShapeFileType.PointZ:
                case ShapeFileType.PointM:
                case ShapeFileType.Multipoint:
                case ShapeFileType.MultipointZ:
                case ShapeFileType.MultipointM:
                    if (wellKnownType == WellKnownType.Point || wellKnownType == WellKnownType.Multipoint)
                    {
                        result = true;
                    }
                    break;
                case ShapeFileType.Polyline:
                case ShapeFileType.PolylineZ:
                case ShapeFileType.PolylineM:
                    if (wellKnownType == WellKnownType.Line || wellKnownType == WellKnownType.Multiline)
                    {
                        result = true;
                    }
                    break;
                case ShapeFileType.Polygon:
                case ShapeFileType.PolygonZ:
                case ShapeFileType.PolygonM:
                    if (wellKnownType == WellKnownType.Polygon || wellKnownType == WellKnownType.Multipolygon)
                    {
                        result = true;
                    }
                    break;
                case ShapeFileType.Multipatch:
                case ShapeFileType.Null:
                default:
                    break;
            }

            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
