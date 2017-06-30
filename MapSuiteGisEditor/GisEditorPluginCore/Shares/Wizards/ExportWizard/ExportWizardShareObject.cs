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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ExportWizardShareObject : WizardShareObject
    {
        private bool addToMap;
        private bool needConvertMemoToCharacter;
        private string projectionParameter;
        private ObservableCollection<FeatureLayer> featureLayers;
        private ExportMode exportMode;
        private FeatureLayer selectedFeatureLayer;
        private string selectedMeasurementType;
        private IEnumerable<string> measurementTypes;
        private TrackInteractiveOverlay trackOverlay;
        private RelayCommand chooseProjectionCommand;
        private Collection<ColumnEntity> columnEntities;

        public ExportWizardShareObject(ExportMode mode, TrackInteractiveOverlay trackOverlay)
        {
            this.exportMode = mode;
            this.AddToMap = true;
            this.trackOverlay = trackOverlay;
            this.InitSelectableFeatureLayers();
            this.InitSelectableMeasurementTypes();
            this.InitColumns();
            this.projectionParameter = OutputWindow.SavedProj4ProjectionParametersString;
            if (string.IsNullOrEmpty(projectionParameter))
            {
                this.projectionParameter = GisEditor.ActiveMap.DisplayProjectionParameters;
            }
        }

        public string WizardName
        {
            get { return "Export"; }
        }

        public bool AddToMap
        {
            get { return addToMap; }
            set { addToMap = value; }
        }

        public bool NeedConvertMemoToCharacter
        {
            get { return needConvertMemoToCharacter; }
            set
            {
                needConvertMemoToCharacter = value;
                RaisePropertyChanged("NeedConvertMemoToCharacter");
            }
        }

        public ExportMode ExportMode
        {
            get { return exportMode; }
            set { exportMode = value; }
        }

        public ObservableCollection<FeatureLayer> FeatureLayers
        {
            get { return featureLayers; }
        }

        public FeatureLayer SelectedFeatureLayer
        {
            get { return selectedFeatureLayer; }
            set { selectedFeatureLayer = value; }
        }

        public Collection<ColumnEntity> ColumnEntities
        {
            get { return columnEntities; }
        }

        public string SelectedMeasurementType
        {
            get { return selectedMeasurementType; }
            set { selectedMeasurementType = value; }
        }

        public IEnumerable<string> MeasurementTypes
        {
            get { return measurementTypes; }
            set { measurementTypes = value; }
        }

        public string ProjectionParameter
        {
            get { return projectionParameter; }
            set
            {
                projectionParameter = value;
                RaisePropertyChanged("ProjectionParameter");
            }
        }

        public RelayCommand ChooseProjectionCommand
        {
            get
            {
                if (chooseProjectionCommand == null)
                {
                    chooseProjectionCommand = new RelayCommand(() =>
                    {
                        ProjectionWindow projectionWindow = new ProjectionWindow(ProjectionParameter, "Choose the projection you want to save", "");
                        if (projectionWindow.ShowDialog().GetValueOrDefault())
                        {
                            string selectedProj4Parameter = projectionWindow.Proj4ProjectionParameters;
                            if (!string.IsNullOrEmpty(selectedProj4Parameter))
                            {
                                ProjectionParameter = selectedProj4Parameter;
                                OutputWindow.SavedProj4ProjectionParametersString = selectedProj4Parameter;
                            }
                        }
                    });
                }
                return chooseProjectionCommand;
            }
        }

        protected override TaskPlugin GetTaskPluginCore()
        {
            ExportTaskPlugin exportTaskPlugin = new ExportTaskPlugin();
            exportTaskPlugin.NeedConvertMemoToCharacter = NeedConvertMemoToCharacter;
            if (OutputMode == OutputMode.ToFile)
            {
                exportTaskPlugin.OutputPathFileName = OutputPathFileName;
            }
            else
            {
                string tempPathFileName = Path.Combine(FolderHelper.GetCurrentProjectTaskResultFolder(), TempFileName) + ".shp";
                exportTaskPlugin.OutputPathFileName = tempPathFileName;
                OutputPathFileName = tempPathFileName;
            }
            exportTaskPlugin.ProjectionWkt = Proj4Projection.ConvertProj4ToPrj(ProjectionParameter);
            exportTaskPlugin.InternalPrj4Projection = GisEditor.ActiveMap.DisplayProjectionParameters;
            switch (ExportMode)
            {
                case ExportMode.ExportMeasuredFeatures:
                    FillMeasuredFeatures(exportTaskPlugin.FeaturesForExporting, exportTaskPlugin.FeatureSourceColumns);
                    break;
                case ExportMode.ExportSelectedFeatures:
                default:
                    foreach (var entry in this.ColumnEntities)
                    {
                        if (entry.IsChecked)
                        {
                            exportTaskPlugin.FeatureSourceColumns.Add(new FeatureSourceColumn(entry.ColumnName, entry.ColumnType, entry.MaxLength));
                            exportTaskPlugin.CostomizedColumnNames[entry.ColumnName] = entry.EditedColumnName;
                        }
                    }
                    FillSelectedFeatures(exportTaskPlugin.FeatureIdsForExporting);
                    exportTaskPlugin.FeatureLayer = SelectedFeatureLayer;
                    break;
            }

            return exportTaskPlugin;
        }

        protected override void LoadToMapCore()
        {
            var getLayersParameters = new GetLayersParameters();
            getLayersParameters.LayerUris.Add(new Uri(OutputPathFileName));
            var layers = GisEditor.LayerManager.GetLayers<ShapeFileFeatureLayer>(getLayersParameters);
            if (layers != null)
            {
                GisEditor.ActiveMap.AddLayersBySettings(layers);
                GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(this, RefreshArgsDescription.LoadToMapCoreDescription));
            }
        }

        private void FillMeasuredFeatures(Collection<Feature> features, Collection<FeatureSourceColumn> columns)
        {
            var measureOverlay = trackOverlay as MeasureTrackInteractiveOverlay;
            if (measureOverlay != null)
            {
                Type shapeType = null;
                MeasurementType measurementType = (MeasurementType)Enum.Parse(typeof(MeasurementType), SelectedMeasurementType);
                switch (measurementType)
                {
                    case MeasurementType.Area:
                        shapeType = typeof(AreaBaseShape);
                        break;
                    case MeasurementType.Line:
                        shapeType = typeof(LineBaseShape);
                        break;
                    default:
                        break;
                }
                var allFeatures = measureOverlay.ShapeLayer.MapShapes.Values.Select(m => m.Feature);
                var featuresToExport = allFeatures.Where(f =>
                {
                    var t = f.GetShape().GetType();
                    return t == shapeType || t.IsSubclassOf(shapeType);
                });

                foreach (var feature in featuresToExport)
                {
                    features.Add(feature);
                }

                if (features.Count > 0)
                {
                    if (measurementType == MeasurementType.Area)
                    {
                        columns.Add(new FeatureSourceColumn(MeasureTrackInteractiveOverlay.AreaColumnName, "Double", 20));
                        columns.Add(new FeatureSourceColumn(MeasureTrackInteractiveOverlay.UnitColumnName, "String", 20));
                    }
                    else if (measurementType == MeasurementType.Line)
                    {
                        columns.Add(new FeatureSourceColumn(MeasureTrackInteractiveOverlay.DistanceColumnName, "Double", 20));
                        columns.Add(new FeatureSourceColumn(MeasureTrackInteractiveOverlay.UnitColumnName, "String", 20));
                    }
                }
            }
        }

        private void FillSelectedFeatures(Collection<Feature> features, Collection<FeatureSourceColumn> columns)
        {
            var featuresToExport = GisEditor.SelectionManager.GetSelectedFeatures().Where(f => f.Tag == SelectedFeatureLayer);

            foreach (var feature in featuresToExport)
            {
                features.Add(feature);
            }

            var firstFeature = featuresToExport.FirstOrDefault();
            if (firstFeature != null && firstFeature != default(Feature))
            {
                FeatureLayer layer = (FeatureLayer)firstFeature.Tag;
                if (layer != null)
                {
                    layer.SafeProcess(() =>
                    {
                        foreach (var column in layer.FeatureSource.GetColumns())
                        {
                            columns.Add(column);
                        }
                    });
                }
            }
        }

        private void FillSelectedFeatures(Collection<string> featureIds)
        {
            var featuresToExport = GisEditor.SelectionManager.GetSelectedFeatures().Where(f => f.Tag == SelectedFeatureLayer);

            foreach (var feature in featuresToExport)
            {
                string originalFeatureId = GisEditor.SelectionManager.GetSelectionOverlay().GetOriginalFeatureId(feature);
                featureIds.Add(originalFeatureId);
            }
        }

        private void InitSelectableMeasurementTypes()
        {
            if (GisEditor.ActiveMap != null)
            {
                var measureOverlay = trackOverlay as MeasureTrackInteractiveOverlay;
                if (measureOverlay != null)
                {
                    var measureTypes = new Collection<string>();
                    var allFeatures = measureOverlay.ShapeLayer.MapShapes.Values.Select(m => m.Feature).ToList();
                    if (allFeatures.Any(f => f.GetShape().GetType().IsSubclassOf(typeof(AreaBaseShape))))
                    {
                        measureTypes.Add(MeasurementType.Area.ToString());
                    }
                    if (allFeatures.Any(f => f.GetShape().GetType().IsSubclassOf(typeof(LineBaseShape))))
                    {
                        measureTypes.Add(MeasurementType.Line.ToString());
                    }
                    MeasurementTypes = measureTypes;
                }
            }
        }

        internal void InitColumns()
        {
            columnEntities = new Collection<ColumnEntity>();
            if (SelectedFeatureLayer != null)
            {
                SelectedFeatureLayer.SafeProcess(() =>
                {
                    var columns = SelectedFeatureLayer.FeatureSource.GetColumns();
                    foreach (var column in columns)
                    {
                        string editedName = column.ColumnName;
                        if (column.ColumnName.Contains("."))
                        {
                            int index = column.ColumnName.IndexOf(".") + 1;
                            editedName = column.ColumnName.Substring(index, column.ColumnName.Length - index);
                        }
                        ColumnEntity entity = new ColumnEntity();
                        entity.MaxLength = column.MaxLength;
                        entity.ColumnName = column.ColumnName;
                        entity.EditedColumnName = editedName;
                        entity.ColumnType = column.TypeName;
                        entity.IsChecked = true;
                        columnEntities.Add(entity);
                    }
                });
            }
        }

        private void InitSelectableFeatureLayers()
        {
            featureLayers = new ObservableCollection<FeatureLayer>();

            if (GisEditor.ActiveMap != null)
            {
                var selectionOverlay = GisEditor.SelectionManager.GetSelectionOverlay();
                var allFeatureLayers = from l in GisEditor.ActiveMap.GetFeatureLayers(true)
                                       where selectionOverlay == null ? false : selectionOverlay.HighlightFeatureLayer.InternalFeatures.Any(f => f.Tag == l)
                                       select l;

                foreach (var featureLayer in allFeatureLayers)
                {
                    FeatureLayers.Add(featureLayer);
                }
                SelectedFeatureLayer = FeatureLayers.FirstOrDefault();
            }
        }
    }
}