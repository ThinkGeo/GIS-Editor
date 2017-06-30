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
using System.Globalization;
using System.IO;
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    public abstract partial class FeatureLayerPlugin : LayerPlugin
    {
        protected string GetInternalProj4ProjectionParameters(FeatureLayer featureLayer)
        {
            return GetInternalProj4ProjectionParametersCore(featureLayer);
        }

        protected virtual string GetInternalProj4ProjectionParametersCore(FeatureLayer featureLayer)
        {
            Uri uri = GetUri(featureLayer);
            string result = string.Empty;
            string pathFileName = string.Empty;
            if (uri != null && uri.Scheme.Equals("file") && File.Exists(uri.LocalPath))
            {
                pathFileName = uri.LocalPath;
            }

            if (File.Exists(pathFileName))
            {
                string prjPathFileName = Path.ChangeExtension(pathFileName, ".prj");
                if (File.Exists(prjPathFileName))
                {
                    string prjWkt = File.ReadAllText(prjPathFileName);
                    try
                    {
                        result = Proj4Projection.ConvertPrjToProj4(prjWkt);
                    }
                    catch (Exception e)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                    }
                }
            }

            return result;
        }

        protected void SaveInternalProj4ProjectionParameters(FeatureLayer featureLayer, string proj4ProjectionParameters)
        {
            Proj4ProjectionInfo projectionInfo = featureLayer.GetProj4ProjectionInfo();
            if (projectionInfo != null)
            {
                projectionInfo.InternalProjectionParametersString = proj4ProjectionParameters;
            }

            SaveInternalProj4ProjectionParametersCore(featureLayer, proj4ProjectionParameters);
        }

        protected virtual void SaveInternalProj4ProjectionParametersCore(FeatureLayer featureLayer, string proj4ProjectionParameters)
        {
            if (!string.IsNullOrEmpty(ExtensionFilter))
            {
                var uri = GetUri(featureLayer);
                if (uri != null && uri.Scheme.Equals("file") && File.Exists(uri.LocalPath))
                {
                    string proj4PathFileName = uri.LocalPath;
                    proj4PathFileName = Path.ChangeExtension(proj4PathFileName, ".prj");
                    string internalProj = Proj4Projection.ConvertProj4ToPrj(proj4ProjectionParameters);
                    File.WriteAllText(proj4PathFileName, internalProj);
                }
            }
        }

        private static bool IsDecimalDegree(Layer layer)
        {
            RectangleShape boundingBox = null;
            if (layer.HasBoundingBox)
            {
                lock (layer)
                {
                    layer.SafeProcess(() =>
                    {
                        boundingBox = layer.GetBoundingBox();
                    });
                }

                return boundingBox.LowerLeftPoint.X >= -185
                    && boundingBox.LowerLeftPoint.Y >= -95
                    && boundingBox.UpperRightPoint.X <= 185
                    && boundingBox.UpperRightPoint.Y <= 95;
            }
            else return false;
        }

        private void SetInternalProjections(IEnumerable<FeatureLayer> featureLayers)
        {
            Collection<FeatureLayer> undefinedProjectionLayers = new Collection<FeatureLayer>();
            foreach (var featureLayer in featureLayers)
            {
                Proj4Projection proj4 = featureLayer.FeatureSource.Projection as Proj4Projection;
                if (proj4 == null
                    || string.IsNullOrEmpty(proj4.InternalProjectionParametersString)
                    || string.IsNullOrEmpty(proj4.ExternalProjectionParametersString))
                {
                    proj4 = new Proj4Projection();
                    string internalProj4 = GetInternalProj4ProjectionParameters(featureLayer);
                    if (string.IsNullOrEmpty(internalProj4))
                    {
                        if (IsDecimalDegree(featureLayer))
                        {
                            proj4.InternalProjectionParametersString = Proj4Projection.GetEpsgParametersString(4326);
                        }
                        else
                        {
                            undefinedProjectionLayers.Add(featureLayer);
                        }
                    }
                    else
                    {
                        proj4.InternalProjectionParametersString = internalProj4;
                    }

                    proj4.SyncProjectionParametersString();
                    featureLayer.FeatureSource.Projection = proj4;
                }
            }

            if (undefinedProjectionLayers.Count > 0)
            {
                bool canceled = false;
                bool applyForAll = false;
                string internalProj4 = string.Empty;
                foreach (var featureLayer in undefinedProjectionLayers)
                {
                    if (applyForAll && !string.IsNullOrEmpty(internalProj4))
                    {
                        SaveInternalProj4ProjectionParameters(featureLayer, internalProj4);
                    }
                    else
                    {
                        string description = String.Format(CultureInfo.InvariantCulture, "Please select internal projection of layer {0}.", GetUri(featureLayer));
                        ProjectionWindow projSelectWindow = new ProjectionWindow(string.Empty, description, "Apply For All");
                        if (projSelectWindow.ShowDialog().GetValueOrDefault())
                        {
                            internalProj4 = projSelectWindow.Proj4ProjectionParameters;
                            applyForAll = projSelectWindow.SyncProj4ProjectionForAll;
                            SaveInternalProj4ProjectionParameters(featureLayer, internalProj4);
                        }
                        else
                        {
                            featureLayer.IsVisible = false;
                            applyForAll = projSelectWindow.SyncProj4ProjectionForAll;
                            canceled = true;
                        }
                    }

                    if (canceled && applyForAll) break;
                }
            }
        }

        private void SetExternalProjections(IEnumerable<FeatureLayer> featureLayers)
        {
            if (featureLayers.Count() != 0)
            {
                GisEditorWpfMap wpfMap = GisEditor.ActiveMap;
                foreach (FeatureLayer layer in featureLayers)
                {
                    Proj4Projection proj4 = (Proj4Projection)layer.FeatureSource.Projection;
                    if (wpfMap != null)
                    {
                        if (String.IsNullOrEmpty(wpfMap.DisplayProjectionParameters))
                        {
                            wpfMap.DisplayProjectionParameters = proj4.InternalProjectionParametersString;
                            proj4.ExternalProjectionParametersString = proj4.InternalProjectionParametersString;
                        }
                        else
                        {
                            proj4.ExternalProjectionParametersString = wpfMap.DisplayProjectionParameters;
                            proj4.SyncProjectionParametersString();
                        }
                    }
                }
            }
        }
    }
}