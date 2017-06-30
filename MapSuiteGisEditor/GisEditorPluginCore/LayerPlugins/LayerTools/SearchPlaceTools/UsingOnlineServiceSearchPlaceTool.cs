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
using System.Linq;
using System.Net;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class UsingOnlineServiceSearchPlaceTool : SearchPlaceTool
    {
        protected override bool CanSearchPlaceCore(Layer layer)
        {
            return true;
        }

        protected override Collection<Feature> SearchPlacesCore(string inputAddress, Layer layerToSearch)
        {
            Collection<Feature> results = new Collection<Feature>();
            try
            {
                SearchPlaceHelper.GetGeocodeMatches(inputAddress).ForEach(m =>
                {
                    Feature feature = new Feature();
                    foreach (var item in m.MatchResults) feature.ColumnValues[item.Key] = item.Value;
                    results.Add(feature);
                });
            }
            catch (WebException webException)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, webException.Message, new ExceptionInfo(webException));
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }

            return results;
        }
    }
}
