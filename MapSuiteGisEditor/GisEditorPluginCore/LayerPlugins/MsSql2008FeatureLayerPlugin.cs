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
using System.Linq;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class MsSql2008FeatureLayerPlugin : FeatureLayerPlugin
    {
        private static List<String> historyServerNames;

        static MsSql2008FeatureLayerPlugin()
        {
            historyServerNames = new List<string>();
        }

        public MsSql2008FeatureLayerPlugin()
        {
            Name = GisEditor.LanguageManager.GetStringResource("MsSql2008FeatureLayerPluginName");
            Description = GisEditor.LanguageManager.GetStringResource("MsSql2008FeatureLayerPluginDescription");
        }

        protected override bool CanPageFeaturesEfficientlyCore
        {
            get
            {
                return true;
            }
            set
            {
                base.CanPageFeaturesEfficientlyCore = value;
            }
        }

        public static List<String> HistoryServerNames
        {
            get { return historyServerNames; }
        }

        protected override Uri GetUriCore(Layer layer)
        {
            return new Uri("mssql://test");
        }

        protected override Type GetLayerTypeCore()
        {
            return typeof(MsSqlFeatureLayer);
        }

        protected override string GetInternalProj4ProjectionParametersCore(FeatureLayer featureLayer)
        {
            MsSqlFeatureLayer sqlLayer = featureLayer as MsSqlFeatureLayer;
            if (sqlLayer == null)
            {
                return base.GetInternalProj4ProjectionParametersCore(featureLayer);
            }
            return Proj4Projection.GetEpsgParametersString(sqlLayer.Srid);
        }

        protected override Collection<Layer> GetLayersCore(GetLayersParameters getLayersParameters)
        {
            Collection<Layer> resultLayers = base.GetLayersCore(getLayersParameters);
            Collection<MsSqlFeatureLayer> newFeatureLayers = new Collection<MsSqlFeatureLayer>();

            if (getLayersParameters.CustomData.ContainsKey("TableName") && getLayersParameters.CustomData.ContainsKey("DatabaseName") && getLayersParameters.CustomData.ContainsKey("IdColumn") && getLayersParameters.CustomData.ContainsKey("ServerName"))
            {
                MsSql2008FeatureLayerInfo layerInfo = new MsSql2008FeatureLayerInfo();
                layerInfo.TableName = getLayersParameters.CustomData["TableName"].ToString();
                layerInfo.SchemaName = getLayersParameters.CustomData["SchemaName"].ToString();
                layerInfo.DatabaseName = getLayersParameters.CustomData["DatabaseName"].ToString();
                layerInfo.FeatureIDColumnName = getLayersParameters.CustomData["IdColumn"].ToString();
                layerInfo.ServerName = getLayersParameters.CustomData["ServerName"].ToString();
                layerInfo.UserName = getLayersParameters.CustomData["UserName"].ToString();
                layerInfo.Password = getLayersParameters.CustomData["Password"].ToString();
                if (!string.IsNullOrEmpty(layerInfo.UserName) && !string.IsNullOrEmpty(layerInfo.Password))
                {
                    layerInfo.UseTrustAuthority = false;
                }
                newFeatureLayers = GetLayers(layerInfo);
            }
            else
            {
                newFeatureLayers = GetFeatureLayersCore();
            }

            foreach (var layer in newFeatureLayers)
            {
                resultLayers.Add(layer);
            }

            return resultLayers;
        }

        private Collection<MsSqlFeatureLayer> GetFeatureLayersCore()
        {
            Collection<MsSqlFeatureLayer> layers = new Collection<MsSqlFeatureLayer>();
            foreach (var layer in GetLayers(null))
            {
                layers.Add(layer);
            }
            return layers;
        }

        protected override UserControl GetPropertiesUICore(Layer layer)
        {
            UserControl propertiesUserControl = base.GetPropertiesUICore(layer);

            LayerPluginHelper.SaveFeatureIDColumns((FeatureLayer)layer, propertiesUserControl);

            return propertiesUserControl;
        }

        public Collection<MsSqlFeatureLayer> GetLayers(params MsSql2008FeatureLayerInfo[] configurations)
        {
            return GetLayers(configurations as IEnumerable<MsSql2008FeatureLayerInfo>);
        }

        public Collection<MsSqlFeatureLayer> GetLayers(IEnumerable<MsSql2008FeatureLayerInfo> configurations)
        {
            return GetLayersCore(configurations);
        }

        protected override SimpleShapeType GetFeatureSimpleShapeTypeCore(FeatureLayer featureLayer)
        {
            //var result = WellKnownType.Invalid;
            //var sqlFeatureSource = featureLayer.FeatureSource as MsSql2008FeatureSource;
            //if (sqlFeatureSource != null)
            //{
            //    sqlFeatureSource.SafeProcess(() =>
            //    {
            //        result = sqlFeatureSource.GetFirstGeometryType();
            //    });
            //}
            //return MapHelper.GetSimpleShapeType(result);
            return SimpleShapeType.Unknown;
        }

        protected virtual Collection<MsSqlFeatureLayer> GetLayersCore(IEnumerable<MsSql2008FeatureLayerInfo> configurations)
        {
            Collection<MsSqlFeatureLayer> resultLayers = new Collection<MsSqlFeatureLayer>();
            if (configurations == null)
            {
                var window = new DatabaseLayerInfoWindow();
                var model = new MsSql2008FeatureLayerInfo();

                window.SetSource(model);

                if (window.ShowDialog().GetValueOrDefault())
                {
                    resultLayers.Add(model.CreateLayer());
                }
            }
            else
            {
                foreach (var layer in configurations.Select(c => c.CreateLayer()))
                {
                    layer.CommandTimeout = Singleton<ServerFeatureLayerSettingsUserControl>.Instance.SQLTimeoutInSecond;
                    resultLayers.Add(layer);
                }
            }

            return resultLayers;
        }

        protected override StorableSettings GetSettingsCore()
        {
            StorableSettings settings = base.GetSettingsCore();

            settings.GlobalSettings["SQLServerHistoryServerName"] = string.Join("|", historyServerNames);
            settings.GlobalSettings["SQLServerTimeout"] = Singleton<ServerFeatureLayerSettingsUserControl>.Instance.SQLTimeoutInSecond.ToString(CultureInfo.InvariantCulture);

            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);

            if (settings.GlobalSettings.ContainsKey("SQLServerTimeout"))
            {
                string timeoutString = settings.GlobalSettings["SQLServerTimeout"];
                int timeout = 20;
                int tempTimeout;
                if (Int32.TryParse(timeoutString, out tempTimeout))
                {
                    timeout = tempTimeout;
                }

                Singleton<ServerFeatureLayerSettingsUserControl>.Instance.SQLTimeoutInSecond = timeout;
                GisEditor.GetMaps().SelectMany(m => m.Overlays.OfType<LayerOverlay>().SelectMany(lo => lo.Layers.OfType<MsSqlFeatureLayer>())).ForEach(l =>
                {
                    l.CommandTimeout = timeout;
                });
            }

            if (settings.GlobalSettings.ContainsKey("SQLServerHistoryServerName"))
            {
                string SQLServerHistoryServerNames = settings.GlobalSettings["SQLServerHistoryServerName"];
                historyServerNames.Clear();
                SQLServerHistoryServerNames.Split('|').ForEach(n => historyServerNames.Add(n));
            }
        }

        //protected override LayerListItem GetLayerListItemCore(Layer layer)
        //{
        //    var layerViewModel = base.GetLayerListItemCore(layer);
        //    var msSql2008Layer = (MsSql2008FeatureLayer)layer;

        //    foreach (var item in LayerListHelper.CollectComponentStyleLayerListItem(msSql2008Layer))
        //    {
        //        item.Parent = layerViewModel;
        //        layerViewModel.Children.Add(item);
        //    }
        //    return layerViewModel;
        //}
    }
}