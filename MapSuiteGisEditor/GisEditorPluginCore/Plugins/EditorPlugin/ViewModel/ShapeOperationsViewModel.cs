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
using System.Linq;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.WpfDesktop.Extension;
using System.Windows;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ShapeOperationsViewModel : ViewModelBase
    {
        private ObservedCommand combineFeaturesCommand;
        private ObservedCommand explodeFeaturesCommand;
        private ObservedCommand unionFeaturesCommand;
        private ObservedCommand subtractFeaturesCommand;
        private ObservedCommand intersetFeaturesCommand;
        private ObservedCommand splitFeaturesCommand;
        private ObservedCommand innerRingCommand;

        public ObservedCommand InnerRingCommand
        {
            get
            {
                if (innerRingCommand == null)
                {
                    innerRingCommand = new ObservedCommand(() =>
                    {
                        var editOverlay = SharedViewModel.Instance.EditOverlay;
                        var trackOverlay = SharedViewModel.Instance.TrackOverlay;
                        if (editOverlay != null && trackOverlay != null)
                        {
                            editOverlay.IsEnabled = true;
                            GisEditor.ActiveMap.DisableInteractiveOverlaysExclude(editOverlay);

                            trackOverlay.TrackMode = TrackMode.Polygon;
                            editOverlay.TrackResultProcessMode = TrackResultProcessMode.InnerRing;
                            GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawPolygon;
                        }
                    }, () =>
                    {
                        var editOverlay = SharedViewModel.Instance.EditOverlay;
                        if (editOverlay == null)
                        {
                            return false;
                        }
                        else
                        {
                            return editOverlay.EditShapesLayer.InternalFeatures.Any(f =>
                            {
                                var shapeType = f.GetWellKnownType();
                                return CanAddInnerRingTypes.Contains(shapeType);
                            });
                        }
                    });
                }
                return innerRingCommand;
            }
        }

        public ObservedCommand SplitFeaturesCommand
        {
            get
            {
                if (splitFeaturesCommand == null)
                {
                    splitFeaturesCommand = new ObservedCommand(() =>
                    {
                        var editOverlay = SharedViewModel.Instance.EditOverlay;
                        var trackOverlay = SharedViewModel.Instance.TrackOverlay;
                        if (editOverlay != null && trackOverlay != null)
                        {
                            editOverlay.IsEnabled = true;
                            GisEditor.ActiveMap.DisableInteractiveOverlaysExclude(editOverlay);

                            trackOverlay.TrackMode = TrackMode.Line;
                            editOverlay.TrackResultProcessMode = TrackResultProcessMode.Split;
                            GisEditor.ActiveMap.Cursor = GisEditorCursors.DrawLine;
                        }
                    }, () =>
                    {
                        var editOverlay = SharedViewModel.Instance.EditOverlay;
                        if (editOverlay == null)
                        {
                            return false;
                        }
                        else
                        {
                            return editOverlay.EditShapesLayer.InternalFeatures.Any(f =>
                            {
                                var shapeType = f.GetWellKnownType();
                                return CanSplitShapeTypes.Contains(shapeType);
                            });
                        }
                    });
                }
                return splitFeaturesCommand;
            }
        }

        public static IEnumerable<WellKnownType> CanSplitShapeTypes
        {
            get
            {
                yield return WellKnownType.Line;
                yield return WellKnownType.Multiline;
                yield return WellKnownType.Polygon;
                yield return WellKnownType.Multipolygon;
            }
        }

        public static IEnumerable<WellKnownType> CanAddInnerRingTypes
        {
            get
            {
                yield return WellKnownType.Polygon;
                yield return WellKnownType.Multipolygon;
            }
        }

        public ObservedCommand IntersetFeaturesCommand
        {
            get
            {
                if (intersetFeaturesCommand == null)
                {
                    intersetFeaturesCommand = new ObservedCommand(IntersectFeatures, CheckCanIntersectFeatures);
                }
                return intersetFeaturesCommand;
            }
        }

        public ObservedCommand SubtractFeaturesCommand
        {
            get
            {
                if (subtractFeaturesCommand == null)
                {
                    subtractFeaturesCommand = new ObservedCommand(SubtractFeatures, CheckCanSubtractFeatures);
                }
                return subtractFeaturesCommand;
            }
        }

        public ObservedCommand UnionFeaturesCommand
        {
            get
            {
                if (unionFeaturesCommand == null)
                {
                    unionFeaturesCommand = new ObservedCommand(UnionFeatures, CheckCanUnionFeatures);
                }
                return unionFeaturesCommand;
            }
        }

        public ObservedCommand CombineFeaturesCommand
        {
            get
            {
                if (combineFeaturesCommand == null)
                {
                    combineFeaturesCommand = new ObservedCommand(CombineFeatures, CheckCanCombineFeatures);
                }
                return combineFeaturesCommand;
            }
        }

        public ObservedCommand ExplodeFeaturesCommand
        {
            get
            {
                if (explodeFeaturesCommand == null)
                {
                    explodeFeaturesCommand = new ObservedCommand(ExplodeFeatures, CheckCanExplodeFeatures);
                }
                return explodeFeaturesCommand;
            }
        }

        private static bool CheckCanIntersectFeatures()
        {
            return CanExecuteBasedOnEditOverlay(typeof(AreaBaseShape));
        }

        private static bool CheckCanSubtractFeatures()
        {
            return CanExecuteBasedOnEditOverlay(typeof(AreaBaseShape));
        }

        private static bool CheckCanUnionFeatures()
        {
            return CanExecuteBasedOnEditOverlay(typeof(AreaBaseShape));
        }

        private static bool CheckCanCombineFeatures()
        {
            if (SharedViewModel.Instance.EditOverlay != null)
            {
                bool pointsSelected = SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Any(f => f.GetWellKnownType() == WellKnownType.Point);
                return CanExecuteBasedOnEditOverlay() && !pointsSelected;
            }
            else return false;
        }

        private static bool CheckCanExplodeFeatures()
        {
            if (SharedViewModel.Instance.EditOverlay != null)
            {
                var features = SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures;
                bool isMultiShape = false;
                if (features.Count == 1)
                {
                    var f = features[0];
                    isMultiShape = (f.GetWellKnownType() == WellKnownType.Multipoint) || (f.GetWellKnownType() == WellKnownType.Multiline) || (f.GetWellKnownType() == WellKnownType.Multipolygon);
                }
                return isMultiShape;
            }
            else return false;
        }

        private static void IntersectFeatures()
        {
            string id = GetId();

            var resultFeature = GeoProcessHelper.IntersectFeatures(SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures);
            if (resultFeature != null)
            {
                SetFeatrueAttribute(resultFeature);
                AddGeoProcessedFeatureToOverlay(resultFeature, id.ToString());
                SubtractFeatures();
                SharedViewModel.Instance.EditOverlay.TakeSnapshot();
            }
        }

        private static void SubtractFeatures()
        {
            var ids = SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Select(f => f.Id).ToArray();

            int temp = 0;
            bool allIdsAreNumbers = SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.All(f => int.TryParse(f.Id, out temp));
            IEnumerable<Feature> featuresWithNumberId = null;
            if (!allIdsAreNumbers)
            {
                int numberId = 0;
                featuresWithNumberId = SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Select(f => new Feature(f.GetWellKnownBinary(), (numberId++).ToString()));
            }
            else
            {
                featuresWithNumberId = SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures;
            }

            var subtractedAreas = GeoProcessHelper.SubtractAreas(featuresWithNumberId);

            SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Clear();

            if (ids.Length == subtractedAreas.Length)
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Add(new Feature(subtractedAreas[i].GetWellKnownBinary(), ids[i]));
                }
            }

            SharedViewModel.Instance.EditOverlay.EditShapesLayer.BuildIndex();
            GisEditor.ActiveMap.Refresh(SharedViewModel.Instance.EditOverlay);

            SharedViewModel.Instance.EditOverlay.TakeSnapshot();
        }

        private static void UnionFeatures()
        {
            string id = GetId();

            try
            {
                var resultFeature = GeoProcessHelper.UnionAreaFeatures(SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures);
                SetFeatrueAttribute(resultFeature);
                SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Clear();
                AddGeoProcessedFeatureToOverlay(resultFeature, id.ToString());

                SharedViewModel.Instance.EditOverlay.TakeSnapshot();
            }
            catch (Exception ex)
            {
                GisEditorMessageBox box = new GisEditorMessageBox(System.Windows.MessageBoxButton.OK);
                box.Owner = Application.Current.MainWindow;
                box.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                box.Title = "Error";
                box.Message = "An error has occurred while trying to union shapes.";
                box.ErrorMessage = ex.ToString();
                box.ShowDialog();
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, ex);
            }
        }

        private static void ExplodeFeatures()
        {
            var features = SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures;
            Collection<Feature> results = new Collection<Feature>();
            for (int i = 0; i < features.Count; i++)
            {
                var multipolygon = features[i].GetShape() as MultipolygonShape;
                var multiline = features[i].GetShape() as MultilineShape;
                var multipoint = features[i].GetShape() as MultipointShape;

                Collection<BaseShape> resultShapes = new Collection<BaseShape>();
                if (multipolygon != null)
                {
                    foreach (var item in multipolygon.Polygons)
                    {
                        resultShapes.Add(item);
                    }
                }
                else if (multiline != null)
                {
                    foreach (var item in multiline.Lines)
                    {
                        resultShapes.Add(item);
                    }
                }
                else if (multipoint != null)
                {
                    foreach (var item in multipoint.Points)
                    {
                        resultShapes.Add(item);
                    }
                }

                if (resultShapes.Count > 0)
                {
                    foreach (var item in resultShapes)
                    {
                        Feature newFeature = new Feature(item);
                        string id = GetId();
                        newFeature.Id = id;
                        SetFeatrueAttribute(newFeature, i);
                        results.Add(newFeature);
                    }
                }
            }

            if (results.Count > 0)
            {
                SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Clear();
                foreach (var item in results)
                {
                    var feature = new Feature(item.GetWellKnownBinary(), item.Id, item.ColumnValues);
                    SharedViewModel.Instance.EditOverlay.NewFeatureIds.Add(item.Id);
                    SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Add(feature.Id, feature);
                    SharedViewModel.Instance.EditOverlay.EditShapesLayer.BuildIndex();
                }
                SharedViewModel.Instance.EditOverlay.TakeSnapshot();
                GisEditor.ActiveMap.Refresh(SharedViewModel.Instance.EditOverlay);
            }
        }

        private static void CombineFeatures()
        {
            string id = GetId();

            var resultFeature = GeoProcessHelper.CombineFeatures(SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures);
            SetFeatrueAttribute(resultFeature);
            SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Clear();
            AddGeoProcessedFeatureToOverlay(resultFeature, id);

            SharedViewModel.Instance.EditOverlay.TakeSnapshot();
        }

        private static bool CanExecuteBasedOnEditOverlay(Type shapeType = null)
        {
            bool canExecute = false;

            if (SharedViewModel.Instance.EditOverlay != null && SharedViewModel.Instance.EditOverlay.EditTargetLayer != null)
            {
                bool moreThanTwoFeaturesAreSelected = SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Count >= 2;

                if (moreThanTwoFeaturesAreSelected)
                {
                    bool featuresMatchType = false;
                    var shapes = SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Select(f => f.GetShape()).ToArray();
                    if (shapeType == null)
                    {
                        featuresMatchType = shapes.All(s => s is AreaBaseShape);
                        if (!featuresMatchType)
                        {
                            featuresMatchType = shapes.All(s => s is LineBaseShape);
                        }

                        if (!featuresMatchType)
                        {
                            featuresMatchType = shapes.All(s => s is PointBaseShape);
                        }
                    }
                    else
                    {
                        featuresMatchType = shapes.All(s => s.GetType().IsSubclassOf(shapeType));
                    }

                    canExecute = moreThanTwoFeaturesAreSelected && featuresMatchType;
                }
                else canExecute = false;
            }

            return canExecute;
        }

        private static string GetId()
        {
            //int idNumber = 0;
            //string id = string.Empty;

            //SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Any(f => int.TryParse(f.Id, out idNumber));

            //if (idNumber == 0)
            //{
            //    id = SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.First().Id;
            //}
            //else
            //{
            //    id = idNumber.ToString();
            //}
            return Guid.NewGuid().ToString();
        }

        private static void SetFeatrueAttribute(Feature resultFeature)
        {
            SetFeatrueAttribute(resultFeature, 0);
        }

        private static void SetFeatrueAttribute(Feature resultFeature, int index)
        {

            var oldfeature = SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures[index];

            Dictionary<FeatureLayer, Collection<Feature>> features = new Dictionary<FeatureLayer, Collection<Feature>>();
            features[SharedViewModel.Instance.EditOverlay.EditTargetLayer] = new Collection<Feature>();
            features[SharedViewModel.Instance.EditOverlay.EditTargetLayer].Add(oldfeature);

            if (Singleton<EditorSetting>.Instance.IsAttributePrompted)
            {
                FeatureAttributeWindow window = new FeatureAttributeWindow(features, Singleton<EditorSetting>.Instance.IsAttributePrompted);
                window.ShowDialog().GetValueOrDefault();
            }

            SharedViewModel.Instance.EditOverlay.EditTargetLayer.SafeProcess(() =>
            {
                var columns = SharedViewModel.Instance.EditOverlay.EditTargetLayer.FeatureSource.GetColumns();
                foreach (var column in columns)
                {
                    if (resultFeature.ColumnValues.ContainsKey(column.ColumnName))
                    {
                        resultFeature.ColumnValues[column.ColumnName] = oldfeature.ColumnValues[column.ColumnName];
                    }
                    else
                    {
                        if (oldfeature.ColumnValues.ContainsKey(column.ColumnName))
                        {
                            resultFeature.ColumnValues.Add(column.ColumnName, oldfeature.ColumnValues[column.ColumnName]);
                        }
                        else
                        {
                            resultFeature.ColumnValues.Add(column.ColumnName, string.Empty);
                        }
                    }
                }
            });
        }

        private static void AddGeoProcessedFeatureToOverlay(Feature resultFeature, string id)
        {
            var feature = new Feature(resultFeature.GetWellKnownBinary(), id, resultFeature.ColumnValues);
            //SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Clear();
            SharedViewModel.Instance.EditOverlay.NewFeatureIds.Add(id);
            SharedViewModel.Instance.EditOverlay.EditShapesLayer.InternalFeatures.Add(feature.Id, feature);
            SharedViewModel.Instance.EditOverlay.EditShapesLayer.BuildIndex();
            GisEditor.ActiveMap.Refresh(SharedViewModel.Instance.EditOverlay);
        }
    }
}