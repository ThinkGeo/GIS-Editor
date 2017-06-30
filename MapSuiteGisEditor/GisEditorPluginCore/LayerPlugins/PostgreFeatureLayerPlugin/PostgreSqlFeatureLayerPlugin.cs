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
using System.Globalization;
using System.Linq;
using System.Windows;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class OsmPostgreSqlLayerPlugin : FeatureLayerPlugin
    {
        public OsmPostgreSqlLayerPlugin()
            : base()
        {
            Name = "PostgreSql";
            Author = "ThinkGeo";
            Description = "PostgreSql";
            GettingLayers -= OsmPostgreSqlLayerPlugin_GettingLayers;
            GettingLayers += OsmPostgreSqlLayerPlugin_GettingLayers;
        }

        protected override bool CanPageFeaturesEfficientlyCore
        {
            get { return true; }
        }

        [Obsolete("This property is obsoleted, please call CanGetFeaturesByColumnValueEfficientlyCore instead.")]
        protected override bool CanQueryFeaturesEfficientlyCore
        {
            get { return false; }
        }

        protected override bool CanGetFeaturesByColumnValueEfficientlyCore
        {
            get { return false; }
        }

        protected override StorableSettings GetSettingsCore()
        {
            StorableSettings settings = base.GetSettingsCore();
            settings.GlobalSettings["Timeout"] = Singleton<ServerFeatureLayerSettingsUserControl>.Instance.PostgreTimeoutInSecond.ToString(CultureInfo.InvariantCulture);
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            if (settings.GlobalSettings.ContainsKey("Timeout"))
            {
                string timeoutString = settings.GlobalSettings["Timeout"];
                int timeout = 20;
                int tempTimeout;
                if (Int32.TryParse(timeoutString, out tempTimeout))
                {
                    timeout = tempTimeout;
                }

                Singleton<ServerFeatureLayerSettingsUserControl>.Instance.PostgreTimeoutInSecond = timeout;
                GisEditor.GetMaps().SelectMany(m => m.Overlays.OfType<LayerOverlay>().SelectMany(lo => lo.Layers.OfType<PostgreSqlFeatureLayer>())).ForEach(l =>
                {
                    l.CommandTimeout = timeout;
                });
            }
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            return Singleton<ServerFeatureLayerSettingsUserControl>.Instance;
        }

        protected override SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            return SimpleShapeType.Unknown;
        }

        private void OsmPostgreSqlLayerPlugin_GettingLayers(object sender, GettingLayersLayerPluginEventArgs e)
        {
            if (e.Parameters.LayerUris.Count == 0)
            {
                PostgreConfigureWindow window = new PostgreConfigureWindow();
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (Application.Current != null && Application.Current.MainWindow != null)
                {
                    window.Owner = Application.Current.MainWindow;
                }

                if (window.ShowDialog().GetValueOrDefault())
                {
                    Uri layerUri = window.GetResultLayerUri();
                    e.Parameters.LayerUris.Add(layerUri);
                }
            }
        }

        protected override void UnloadCore()
        {
            GettingLayers -= OsmPostgreSqlLayerPlugin_GettingLayers;
            base.UnloadCore();
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(PostgreSqlFeatureLayer);
        }

        protected override Uri GetUriCore(Layer layer)
        {
            PostgreSqlFeatureLayer postgreSqlFeatureLayer = (PostgreSqlFeatureLayer)layer;
            return new Uri("postgreSqlFeatureLayer:" + postgreSqlFeatureLayer.ConnectionString + postgreSqlFeatureLayer.TableName);
        }

        protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        {
            Collection<Layer> layers = new Collection<Layer>();
            foreach (Uri uri in getLayersParameters.LayerUris)
            {
                string tableName = uri.LocalPath.Split('|')[0];
                string schema = uri.LocalPath.Split('|')[1];
                string connectionString = uri.LocalPath.Split('|')[2];
                string featureIdColumn = uri.LocalPath.Split('|')[3];
                PostgreSqlFeatureLayer postgreSqlFeatureLayer = new PostgreSqlFeatureLayer(connectionString, tableName, featureIdColumn);
                postgreSqlFeatureLayer.SchemaName = schema;
                postgreSqlFeatureLayer.Name = postgreSqlFeatureLayer.TableName;
                postgreSqlFeatureLayer.CommandTimeout = Singleton<ServerFeatureLayerSettingsUserControl>.Instance.PostgreTimeoutInSecond;
                layers.Add(postgreSqlFeatureLayer);
            }

            return layers;
        }

        protected override string GetInternalProj4ProjectionParametersCore(FeatureLayer featureLayer)
        {
            return Proj4Projection.GetEpsgParametersString(3857);
        }
    }
}