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
using System.Collections.ObjectModel;
using ThinkGeo.MapSuite.GeocodeServerSdk;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class ShapeFileSearchPlaceTool : SearchPlaceTool
    {
        protected override bool CanSearchPlaceCore(Layer layer)
        {
            return true;
        }

        protected override Collection<Feature> SearchPlacesCore(string inputAddress, Layer layerToSearch)
        {
            Collection<Feature> features = new Collection<Feature>();

            ShapeFileFeatureLayer layer = layerToSearch as ShapeFileFeatureLayer;

            if (layer != null)
            {
                Collection<Feature> layerFeatures = null;
                layer.SafeProcess(() =>
                {
                    layerFeatures = layer.QueryTools.GetAllFeatures(ReturningColumnsType.AllColumns);
                });

                if (layerFeatures != null)
                {
                    foreach (var feature in layerFeatures)
                    {
                        foreach (var columnValue in feature.ColumnValues)
                        {
                            if (columnValue.Value.ToUpperInvariant().Contains(inputAddress.ToUpperInvariant()))
                            {
                                FillMatchesInGeocoder(feature);
                                feature.ColumnValues["Street"] = columnValue.Value;
                                features.Add(feature);
                                break;
                            }
                        }
                    }
                }
            }

            return features;
        }

        private static void FillMatchesInGeocoder(Feature feature)
        {
            try
            {
                if (feature.GetWellKnownBinary() != null)
                {
                    PointShape searchWorldCenter = feature.GetBoundingBox().GetCenterPoint();
                    var matches = SearchPlaceHelper.GetGeocodeMatches(searchWorldCenter.X, searchWorldCenter.Y);
                    foreach (var match in matches)
                    {
                        if (match.MatchResults.ContainsKey("City"))
                            feature.ColumnValues["City"] = match.MatchResults["City"];
                        if (match.MatchResults.ContainsKey("State"))
                            feature.ColumnValues["State"] = match.MatchResults["State"];
                        if (match.MatchResults.ContainsKey("County"))
                            feature.ColumnValues["County"] = match.MatchResults["County"];
                        if (match.MatchResults.ContainsKey("Zip"))
                            feature.ColumnValues["Zip"] = match.MatchResults["Zip"];
                        if (match.MatchResults.ContainsKey("CentroidPoint"))
                            feature.ColumnValues["CentroidPoint"] = match.MatchResults["CentroidPoint"];
                        if (match.MatchResults.ContainsKey("BoundingBox"))
                            feature.ColumnValues["BoundingBox"] = match.MatchResults["BoundingBox"];
                    }
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
        }
    }
}
