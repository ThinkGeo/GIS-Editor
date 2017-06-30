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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    internal class CoreInternalFunctions
    {
        public static RectangleShape ConvertToInternalProjectionCall(RectangleShape rectangle, FeatureSource featureSource)
        {
            RectangleShape newRectangle = rectangle;

            if (featureSource.Projection != null)
            {
                newRectangle = featureSource.Projection.ConvertToInternalProjection(newRectangle);
            }

            return newRectangle;
        }

        public static void RemoveEmptyAndExcludeFeatures(Collection<Feature> sourceFeatures, FeatureSource featureSource)
        {
            Collection<int> features = new Collection<int>();
            for (int i = 0; i < sourceFeatures.Count; i++)
            {
                if (sourceFeatures[i] == null || sourceFeatures[i].GetWellKnownBinary() == null || sourceFeatures[i].GetWellKnownBinary().Length == 0 || featureSource.FeatureIdsToExclude.Contains(sourceFeatures[i].Id))
                {
                    features.Add(i);
                }
            }

            for (int i = features.Count - 1; i >= 0; i--)
            {
                sourceFeatures.RemoveAt(features[i]);
            }
        }

        public static void ProcessTransaction(RectangleShape boundingBox, Collection<Feature> returnFeatures, FeatureSource featureSource, bool needUpdateProjection = false)
        {
            if (featureSource.IsInTransaction && featureSource.IsTransactionLive)
            {
                foreach (Feature feature in featureSource.TransactionBuffer.AddBuffer.Values)
                {
                    if (boundingBox.Intersects(feature.GetBoundingBox()))
                    {
                        var newFeature = needUpdateProjection ? ConvertToExternalProjection(feature, featureSource) : feature;
                        returnFeatures.Add(newFeature);
                    }
                }

                Dictionary<string, Feature> recordsInDictionary = new Dictionary<string, Feature>();
                if (featureSource.TransactionBuffer.EditBuffer.Count > 0 || featureSource.TransactionBuffer.DeleteBuffer.Count > 0)
                {
                    recordsInDictionary = GetFeaturesDictionaryFromCollecion(returnFeatures);
                }

                foreach (Feature feature in featureSource.TransactionBuffer.EditBuffer.Values)
                {
                    bool isContained = boundingBox.Intersects(feature.GetBoundingBox());
                    if (isContained)
                    {
                        if (recordsInDictionary.ContainsKey(feature.Id))
                        {
                            Feature oringalFeature = recordsInDictionary[feature.Id];
                            returnFeatures.Remove(oringalFeature);
                        }

                        var newFeature = needUpdateProjection ? ConvertToExternalProjection(feature, featureSource) : feature;
                        returnFeatures.Add(newFeature);
                    }
                    else if (!isContained && recordsInDictionary.ContainsKey(feature.Id))
                    {
                        Feature oringalFeature = recordsInDictionary[feature.Id];
                        returnFeatures.Remove(oringalFeature);
                    }
                }

                foreach (string id in featureSource.TransactionBuffer.DeleteBuffer)
                {
                    if (recordsInDictionary.ContainsKey(id))
                    {
                        if (boundingBox.Intersects(recordsInDictionary[id].GetBoundingBox()) && recordsInDictionary.ContainsKey(id))
                        {
                            Feature feature = recordsInDictionary[id];
                            returnFeatures.Remove(feature);
                        }
                    }
                }
            }
        }

        private static Dictionary<string, Feature> GetFeaturesDictionaryFromCollecion(Collection<Feature> features)
        {
            Dictionary<string, Feature> returnDictionary = new Dictionary<string, Feature>(features.Count);
            foreach (Feature featue in features)
            {
                returnDictionary[featue.Id] = featue;
            }
            return returnDictionary;
        }

        private static Collection<Feature> ConvertToExternalProjection(IEnumerable<Feature> features, FeatureSource featureSource)
        {
            //Validators.CheckParameterIsNotNull(features, "features");

            Collection<Feature> returnFeatures = new Collection<Feature>();

            foreach (Feature feature in features)
            {
                if (feature.GetWellKnownBinary().Length != 0)
                {
                    returnFeatures.Add(ConvertToExternalProjection(feature, featureSource));
                }
                else
                {
                    returnFeatures.Add(feature);
                }
            }

            return returnFeatures;
        }

        private static Feature ConvertToExternalProjection(Feature feature, FeatureSource featureSource)
        {
            //Validators.CheckParameterIsNotNull(feature, "feature");

            Feature newFeature = feature;

            if (featureSource.Projection != null)
            {
                try
                {
                    newFeature = featureSource.Projection.ConvertToExternalProjection(feature);
                }
                catch
                {
                    if (!feature.IsValid() && feature.CanMakeValid)
                    {
                        feature = feature.MakeValid();
                        newFeature = featureSource.Projection.ConvertToExternalProjection(feature);
                    }
                }
            }

            return newFeature;
        }
    }
}