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
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    [Serializable]
    internal class DataViewerViewModel : ViewModelBase
    {
        private bool isBusy;
        private bool allowEdit;
        private bool clearEnable;
        private bool showSelectedFeatures;
        private bool enableColumnVirtualization;
        private int rowCount;
        private int deletedCount;
        private int selectedCount;
        private string busyContent;
        private GisEditorWpfMap map;
        private DataTable currentDataTable;
        private Collection<FeatureLayer> changedLayers;
        private Collection<QueryConditionViewModel> queryConditions;
        private ObservableCollection<EditDataChange> editDataChanges;
        private ObservableCollection<FeatureLayerAdapter> allLayerAdapters;

        [NonSerialized]
        private FeatureLayerAdapter selectedLayerAdapter;

        [NonSerialized]
        private Brush hightLightRowColor;

        [NonSerialized]
        private RelayCommand exportCommand;

        [NonSerialized]
        private RelayCommand clearCommand;

        [NonSerialized]
        private ObservedCommand filterCommand;

        [NonSerialized]
        private ObservedCommand deleteSelectedDataCommand;

        [NonSerialized]
        private RelayCommand<DataRowView> deleteOneRowCommand;

        [NonSerialized]
        private RelayCommand<DataRowView> zoomToOneFeatureCommand;

        private ObservedCommand zoomToSelectedFeatureCommand;

        public DataViewerViewModel(GisEditorWpfMap map, IEnumerable<FeatureLayer> availableLayers, FeatureLayer selectedLayer, bool showSelectedFeatures, bool allowEdit, IDictionary<FeatureLayer, Collection<string>> linkColumnNames)
        {
            enableColumnVirtualization = true;
            this.map = map;
            CurrentDataTable = new DataTable();
            queryConditions = new Collection<QueryConditionViewModel>();
            editDataChanges = new ObservableCollection<EditDataChange>();
            HightLightRowColor = DataViewerHelper.GetHightlightLayerColor();
            this.showSelectedFeatures = showSelectedFeatures;
            changedLayers = new Collection<FeatureLayer>();
            AllowEdit = allowEdit;
            if (availableLayers != null && availableLayers.Count() > 0)
            {
                allLayerAdapters = new ObservableCollection<FeatureLayerAdapter>();

                foreach (var item in availableLayers)
                {
                    Collection<string> tempUriColumnNames = new Collection<string>();
                    if (linkColumnNames.ContainsKey(item))
                    {
                        tempUriColumnNames = linkColumnNames[item];
                    }
                    FeatureLayerPlugin featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(item.GetType()).OfType<FeatureLayerPlugin>().FirstOrDefault();
                    if (featureLayerPlugin != null)
                    {
                        bool isEfficient = featureLayerPlugin.CanPageFeaturesEfficiently;
                        if (isEfficient)
                        {
                            PagedFeatureLayerAdapter shapeFileFeatureLayerDataAdapter = new PagedFeatureLayerAdapter(item, tempUriColumnNames);
                            shapeFileFeatureLayerDataAdapter.IsLinkDataSourceEnabled = !allowEdit;
                            shapeFileFeatureLayerDataAdapter.LoadingData += new EventHandler<ProgressChangedEventArgs>(shapeFileFeatureLayerDataAdapter_LoadingData);
                            allLayerAdapters.Add(shapeFileFeatureLayerDataAdapter);
                        }
                        else
                        {
                            FeatureLayerAdapter featureLayerAdapter = new FeatureLayerAdapter(item, tempUriColumnNames);
                            allLayerAdapters.Add(featureLayerAdapter);
                        }
                    }
                }
                if (selectedLayer != null && availableLayers.Contains(selectedLayer))
                {
                    SelectedLayerAdapter = allLayerAdapters.Where(adapter => adapter.FeatureLayer == selectedLayer).FirstOrDefault();
                }
                else if (availableLayers.Contains(map.ActiveLayer))
                {
                    SelectedLayerAdapter = allLayerAdapters.Where(adapter => adapter.FeatureLayer == map.ActiveLayer).FirstOrDefault();
                }
                else
                {
                    SelectedLayerAdapter = allLayerAdapters.FirstOrDefault();
                }
            }
        }

        public bool EnableColumnVirtualization
        {
            get { return enableColumnVirtualization; }
            set
            {
                enableColumnVirtualization = value;
                RaisePropertyChanged("EnableColumnVirtualization");
            }
        }

        public ObservableCollection<FeatureLayerAdapter> AllLayerAdapters
        {
            get { return allLayerAdapters; }
        }

        public int RowCount
        {
            get { return rowCount; }
            internal set
            {
                rowCount = value;
                RaisePropertyChanged(() => RowCount);
            }
        }

        public FeatureLayerAdapter SelectedLayerAdapter
        {
            get { return selectedLayerAdapter; }
            set
            {
                selectedLayerAdapter = value;
                RaisePropertyChanged(() => SelectedLayerAdapter);
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (!ShowSelectedFeatures)
                    {
                        CurrentDataTable = selectedLayerAdapter.GetDataTable();
                    }
                    else
                    {
                        CurrentDataTable = CollectSelectedFeaturesData();
                    }
                    EnableColumnVirtualization = selectedLayerAdapter.EnableColumnVirtualization;
                });

                SelectedCount = selectedLayerAdapter.SelectedFeatures.Count;

                RowCount = selectedLayerAdapter.GetCount();
                if (!selectedLayerAdapter.FeatureLayer.FeatureSource.CanGetCountQuickly())
                {
                    selectedLayerAdapter.GetCountAsync(count => RowCount = count);
                }
            }
        }

        public FeatureLayer SelectedLayer
        {
            get
            {
                if (selectedLayerAdapter != null)
                    return selectedLayerAdapter.FeatureLayer;
                return null;
            }
        }

        public DataTable CurrentDataTable
        {
            get { return currentDataTable; }
            set
            {
                currentDataTable = value;
                RaisePropertyChanged(() => CurrentDataTable);
            }
        }

        public bool ShowSelectedFeatures
        {
            get { return showSelectedFeatures; }
            set
            {
                showSelectedFeatures = value;
                RaisePropertyChanged(() => ShowSelectedFeatures);
                if (SelectedLayerAdapter != null)
                {
                    ClearEnable = false;
                    CurrentDataTable.Rows.Clear();
                    if (showSelectedFeatures)
                    {
                        CurrentDataTable = CollectSelectedFeaturesData();
                        SelectedCount = selectedLayerAdapter.SelectedFeatures.Count;
                    }
                    else
                    {
                        SelectedLayerAdapter = selectedLayerAdapter;
                    }
                }
            }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                RaisePropertyChanged(() => IsBusy);
            }
        }

        public string BusyContent
        {
            get { return busyContent; }
            set
            {
                busyContent = value;
                RaisePropertyChanged(() => BusyContent);
            }
        }

        public int SelectedCount
        {
            get { return selectedCount; }
            set
            {
                selectedCount = value;
                RaisePropertyChanged(() => SelectedCount);
            }
        }

        public Brush HightLightRowColor
        {
            get { return hightLightRowColor; }
            set
            {
                hightLightRowColor = value;
                RaisePropertyChanged(() => HightLightRowColor);
            }
        }

        public bool AllowEdit
        {
            get { return allowEdit; }
            set
            {
                allowEdit = value;
                RaisePropertyChanged(() => IsReadOnly);
                RaisePropertyChanged(() => InvisbleForEditMode);
            }
        }

        public bool IsReadOnly
        {
            get { return !AllowEdit; }
        }

        public Visibility EditVisible
        {
            get
            {
                return AllowEdit ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility InvisbleForEditMode
        {
            get
            {
                return AllowEdit ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public Collection<FeatureLayer> ChangedLayers { get { return changedLayers; } }

        public bool ClearEnable
        {
            get { return clearEnable; }
            set
            {
                clearEnable = value;
                RaisePropertyChanged(() => ClearEnable);
            }
        }

        public Collection<QueryConditionViewModel> QueryConditions
        {
            get { return queryConditions; }
        }

        public ObservableCollection<EditDataChange> EditDataChanges
        {
            get { return editDataChanges; }
        }

        public RelayCommand ClearCommand
        {
            get
            {
                if (clearCommand == null)
                {
                    clearCommand = new RelayCommand(() =>
                    {
                        SelectedLayerAdapter = selectedLayerAdapter;
                        queryConditions.Clear();
                        ClearEnable = false;
                    });
                }
                return clearCommand;
            }
        }

        public RelayCommand ExportCommand
        {
            get
            {
                if (exportCommand == null)
                {
                    exportCommand = new RelayCommand(() =>
                    {
                        ExportCSVData(currentDataTable);
                    });
                }

                return exportCommand;
            }
        }

        public ObservedCommand FilterCommand
        {
            get
            {
                if (filterCommand == null)
                {
                    filterCommand = new ObservedCommand(() =>
                    {
                        AdvancedQueryUserControl userControl = new AdvancedQueryUserControl(SelectedLayer);
                        userControl.ViewModel.CloseWhenQueryFinished = true;

                        Window filterDataWindow = new Window
                        {
                            Style = Application.Current.FindResource("WindowStyle") as System.Windows.Style,
                            Title = GisEditor.LanguageManager.GetStringResource("DataViewerViewModelFilterDataListTitle"),
                            Width = 415,
                            Height = 348,
                            Content = userControl
                        };

                        foreach (var item in QueryConditions)
                        {
                            if (!userControl.ViewModel.Conditions.Contains(item))
                                userControl.ViewModel.Conditions.Add(item);
                        }
                        if (filterDataWindow.ShowDialog().GetValueOrDefault())
                        {
                            CurrentDataTable.Rows.Clear();
                            var featureIds = userControl.ResultFeatures.Select(f => f.Id).ToArray();
                            CurrentDataTable = SelectedLayerAdapter.GetDataTable(featureIds);
                            RowCount = featureIds.Length;
                            CloseFeatureLayer();
                            foreach (var item in userControl.ViewModel.Conditions)
                            {
                                if (!QueryConditions.Contains(item))
                                    QueryConditions.Add(item);
                            }
                            ClearEnable = true;
                        }
                    }, () => SelectedLayerAdapter != null);
                }
                return filterCommand;
            }
        }

        public ObservedCommand DeleteSelectedDataCommand
        {
            get
            {
                if (deleteSelectedDataCommand == null)
                {
                    deleteSelectedDataCommand = new ObservedCommand(() =>
                    {
                        var deleteMessenger = new DialogMessage("Do you want to delete the selected rows?", null);
                        deleteMessenger.Caption = "Delete Rows";
                        deleteMessenger.Button = MessageBoxButton.YesNo;

                        if (ShowMessageBox(deleteMessenger) == MessageBoxResult.Yes)
                        {
                            var selectedFeatureIds = SelectedLayerAdapter.SelectedFeatures.Keys.ToArray();
                            if (DeleteFeatures(null, selectedFeatureIds))
                            {
                                RowCount = RowCount - selectedFeatureIds.Length;
                                SelectedCount = SelectedCount - selectedFeatureIds.Length;
                                deletedCount += selectedFeatureIds.Length;
                            }
                        }
                    }, () => SelectedLayerAdapter != null && SelectedLayerAdapter.SelectedFeatures.Count > 0);
                }
                return deleteSelectedDataCommand;
            }
        }

        public RelayCommand<DataRowView> DeleteOneRowCommand
        {
            get
            {
                if (deleteOneRowCommand == null)
                {
                    deleteOneRowCommand = new RelayCommand<DataRowView>((dataRowView) =>
                    {
                        var deleteMessenger = new DialogMessage("Do you want to delete the selected row?", null);
                        deleteMessenger.Caption = "Delete Row";
                        deleteMessenger.Button = MessageBoxButton.YesNo;

                        if (ShowMessageBox(deleteMessenger) == MessageBoxResult.Yes)
                        {
                            string featureId = dataRowView[FeatureLayerAdapter.FeatureIdColumnName].ToString();
                            if (DeleteFeatures(dataRowView, new string[] { featureId }))
                            {
                                RowCount--;
                                SelectedCount--;
                                deletedCount++;
                            }
                        }
                    });
                }
                return deleteOneRowCommand;
            }
        }

        public RelayCommand<DataRowView> ZoomToOneFeatureCommand
        {
            get
            {
                if (zoomToOneFeatureCommand == null)
                {
                    zoomToOneFeatureCommand = new RelayCommand<DataRowView>((dataRowView) =>
                    {
                        string featureID = dataRowView[FeatureLayerAdapter.FeatureIdColumnName].ToString();

                        ZoomTo(featureID);
                    });
                }
                return zoomToOneFeatureCommand;
            }
        }

        public ObservedCommand ZoomToSelectedFeatureCommand
        {
            get
            {
                if (zoomToSelectedFeatureCommand == null)
                {
                    zoomToSelectedFeatureCommand = new ObservedCommand(() =>
                    {
                        ZoomTo(string.Empty);
                    }, () => SelectedLayerAdapter != null);
                }
                return zoomToSelectedFeatureCommand;
            }
        }

        public static Collection<LayerOverlay> FindLayerOverlayContaining(GisEditorWpfMap map, FeatureLayer featureLayer)
        {
            Collection<LayerOverlay> results = new Collection<LayerOverlay>();
            foreach (LayerOverlay overlay in map.Overlays.OfType<LayerOverlay>())
            {
                if (overlay.Layers.OfType<FeatureLayer>().Any<FeatureLayer>(
                    layer => layer == featureLayer))
                {
                    results.Add(overlay);
                    continue;
                }
            }
            return results;
        }

        public void ChangeCurrentLayerReadWriteMode(GeoFileReadWriteMode mode)
        {
            if (SelectedLayer is ShapeFileFeatureLayer)
            {
                ShapeFileFeatureLayer layer = (ShapeFileFeatureLayer)SelectedLayer;
                lock (layer)
                {
                    layer.Close();
                    layer.ReadWriteMode = mode;
                }
            }
        }

        public void OpenFeatureLayer()
        {
            if (SelectedLayer != null && !SelectedLayer.IsOpen)
            {
                SelectedLayer.Open();
            }
        }

        public void CloseFeatureLayer()
        {
            if (SelectedLayer != null && SelectedLayer.IsOpen)
            {
                lock (SelectedLayer) SelectedLayer.Close();
            }
        }

        private bool DeleteFeatures(DataRowView dataRowView, IEnumerable<string> featureIds)
        {
            try
            {
                DialogMessage deleteMessenger = null;
                TransactionResult result = null;
                Collection<Feature> features = new Collection<Feature>();
                Collection<LayerOverlay> overlays = new Collection<LayerOverlay>();
                lock (SelectedLayerAdapter)
                {
                    overlays = FindLayerOverlayContaining(map, SelectedLayerAdapter.FeatureLayer);
                    foreach (var overlay in overlays)
                    {
                        overlay.Close();
                    }
                    ChangeCurrentLayerReadWriteMode(GeoFileReadWriteMode.ReadWrite);
                    OpenFeatureLayer();
                    foreach (var featureId in featureIds)
                    {
                        var feature = SelectedLayerAdapter.FeatureLayer.QueryTools.GetFeatureById(featureId, SelectedLayerAdapter.FeatureLayer.GetDistinctColumnNames());
                        features.Add(feature);
                    }
                    SelectedLayerAdapter.FeatureLayer.EditTools.BeginTransaction();
                    foreach (var featureId in featureIds)
                    {
                        SelectedLayerAdapter.FeatureLayer.EditTools.Delete(featureId);
                    }
                    result = SelectedLayerAdapter.FeatureLayer.EditTools.CommitTransaction();
                }
                if (result.FailureReasons.Count != 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var pair in result.FailureReasons)
                    {
                        sb.AppendLine(pair.Value);
                    }
                    deleteMessenger = new DialogMessage(sb.ToString(), null);
                    deleteMessenger.Caption = "Delete Failed";
                    deleteMessenger.Button = MessageBoxButton.OK;

                    ShowMessageBox(deleteMessenger);
                }
                else
                {
                    if (dataRowView != null)
                    {
                        CurrentDataTable.Rows.Remove(dataRowView.Row);
                    }
                    else
                    {
                        Collection<DataRow> dataRowToDelete = new Collection<DataRow>();
                        foreach (DataRow dataRow in CurrentDataTable.Rows)
                        {
                            if (featureIds.Contains(dataRow[FeatureLayerAdapter.FeatureIdColumnName].ToString()))
                            {
                                dataRowToDelete.Add(dataRow);
                            }
                        }
                        foreach (var item in dataRowToDelete)
                        {
                            CurrentDataTable.Rows.Remove(item);
                        }
                    }
                    foreach (var featureId in featureIds)
                    {
                        SelectedLayerAdapter.SelectedFeatures.Remove(featureId);
                    }
                    var selectionTrackOverlay = map.SelectionOverlay;
                    var dictionary = selectionTrackOverlay.GetSelectedFeaturesGroup(SelectedLayerAdapter.FeatureLayer);
                    if (dictionary.Count > 0)
                    {
                        foreach (var feature in features)
                        {
                            if (dictionary[SelectedLayerAdapter.FeatureLayer].Count(tmpFeature => tmpFeature.Id.Equals(feature.Id)) != 0)
                            {
                                string targetFeatureId = selectionTrackOverlay.CreateHighlightFeatureId(feature, selectedLayerAdapter.FeatureLayer);
                                Feature targetFeature = selectionTrackOverlay.HighlightFeatureLayer.InternalFeatures
                                    .FirstOrDefault(f => f.Tag == selectedLayerAdapter.FeatureLayer && f.Id.Equals(targetFeatureId));

                                if (targetFeature != null)
                                {
                                    selectionTrackOverlay.HighlightFeatureLayer.InternalFeatures.Remove(targetFeature);
                                    selectionTrackOverlay.HighlightFeatureLayer.BuildIndex();
                                }
                            }
                        }
                        map.Refresh(selectionTrackOverlay);
                    }
                }
                if (!changedLayers.Contains(SelectedLayer))
                {
                    changedLayers.Add(SelectedLayer);
                }
                return true;
            }
            catch (UnauthorizedAccessException accessEx)
            {
                System.Windows.Forms.MessageBox.Show(accessEx.Message, "Access Denied");
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, accessEx.Message, new ExceptionInfo(accessEx));
                return false;
            }
            finally
            {
                CloseFeatureLayer();
                ChangeCurrentLayerReadWriteMode(GeoFileReadWriteMode.Read);
            }
        }

        private DataTable CollectSelectedFeaturesData()
        {
            return selectedLayerAdapter.GetDataTable(selectedLayerAdapter.SelectedFeatures.Keys);
        }

        private void shapeFileFeatureLayerDataAdapter_LoadingData(object sender, ProgressChangedEventArgs e)
        {
            string processingFormat = GisEditor.LanguageManager.GetStringResource("DataViewerViewModelProcessingText");
            if (e.ProgressPercentage % 20 == 0)
            {
                BusyContent = string.Format(processingFormat, e.ProgressPercentage, rowCount);
            }
            if (e.ProgressPercentage >= rowCount)
            {
                BusyContent = string.Format(processingFormat, rowCount, rowCount);
            }
        }

        private MessageBoxResult ShowMessageBox(DialogMessage msg)
        {
            return MessageBox.Show(msg.Content, msg.Caption, msg.Button, msg.Icon);
        }

        private void ZoomTo(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                OpenFeatureLayer();
                Feature feature = SelectedLayer.QueryTools.GetAllFeatures(ReturningColumnsType.AllColumns).FirstOrDefault(f => f.Id.Equals(msg, StringComparison.InvariantCultureIgnoreCase));
                CloseFeatureLayer();

                if (feature != null && feature.GetWellKnownBinary() != null)
                {
                    ZoomToFeatures(new Feature[] { feature }, SelectedLayer);
                }
            }
            else if (SelectedLayerAdapter != null)
            {
                var selectionTrackOverlay = map.SelectionOverlay;
                var dic = selectionTrackOverlay.GetSelectedFeaturesGroup(SelectedLayer);
                if (dic.Count > 0)
                {
                    ZoomToFeatures(dic[SelectedLayer], SelectedLayer);
                }
            }
        }

        private void ZoomToFeatures(IEnumerable<Feature> features, FeatureLayer featureLayer)
        {
            RectangleShape extent = features.Count() == 1 ? GetBoundingBox(features.FirstOrDefault()) : ExtentHelper.GetBoundingBoxOfItems(features);
            RectangleShape drawingExtent = ExtentHelper.GetDrawingExtent(extent, (float)GisEditor.ActiveMap.ActualWidth, (float)GisEditor.ActiveMap.ActualHeight);
            var scale = ExtentHelper.GetScale(drawingExtent, (float)map.ActualWidth, map.MapUnit);
            map.ZoomTo(extent.GetCenterPoint(), scale);
            //GisEditor.UIManager.RefreshPlugins(new RefreshArgs(extent.GetCenterPoint(), "Identify"));
            GisEditor.UIManager.RefreshPlugins(new RefreshArgs(new Tuple<IEnumerable<Feature>, FeatureLayer>(features, featureLayer), "Identify"));
        }

        private RectangleShape GetBoundingBox(Feature tmpFeature)
        {
            if (tmpFeature.GetWellKnownType() == WellKnownType.Point)
            {
                PointShape point = (PointShape)tmpFeature.GetShape();
                return point.Buffer(2, map.MapUnit, DistanceUnit.Kilometer).GetBoundingBox();
            }
            else return tmpFeature.GetBoundingBox();
        }

        internal void ExportCSVData(DataTable csvTable)
        {
            StringBuilder csvBuilder = new StringBuilder();

            foreach (var column in csvTable.Columns.Cast<DataColumn>().Skip(2))
            {
                csvBuilder.Append("\"");
                csvBuilder.Append(column.ColumnName.Replace("\"", "\"\""));
                csvBuilder.Append("\"");
                csvBuilder.Append(',');
            }

            csvBuilder.Append(Environment.NewLine);

            foreach (DataRow row in csvTable.Rows)
            {
                foreach (var item in row.ItemArray.Skip(2))
                {
                    csvBuilder.Append("\"");
                    csvBuilder.Append(item.ToString().Replace("\"", "\"\""));
                    csvBuilder.Append("\"");
                    csvBuilder.Append(',');
                }

                csvBuilder.Append(Environment.NewLine);
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "CSV File (*.csv)|*.csv";

            if (saveFileDialog.ShowDialog().Value)
            {
                var resultFileName = System.IO.Path.ChangeExtension(saveFileDialog.FileName, "csv");
                System.IO.File.WriteAllText(resultFileName, csvBuilder.ToString());
            }
        }
    }
}