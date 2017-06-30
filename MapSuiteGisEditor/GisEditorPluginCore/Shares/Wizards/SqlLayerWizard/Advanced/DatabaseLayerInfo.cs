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
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public abstract class DatabaseLayerInfo<T> where T : FeatureLayer
    {
        private string serverName;
        private string userName;
        private string password;
        private string databaseName;
        private string tableName;
        private string featureIDColumnName;
        private string description;
        private bool useTrustAuthority;

        public DatabaseLayerInfo()
        {
            UseTrustAuthority = true;
            Description = GisEditor.LanguageManager.GetStringResource("DatabaseLayerInfoDescription");
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        public string ServerName
        {
            get { return serverName; }
            set { serverName = value; }
        }

        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }

        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        public string TableName
        {
            get { return tableName; }
            set { tableName = value; }
        }

        public string DatabaseName
        {
            get { return databaseName; }
            set { databaseName = value; }
        }

        public string FeatureIDColumnName
        {
            get { return featureIDColumnName; }
            set { featureIDColumnName = value; }
        }

        public bool UseTrustAuthority
        {
            get { return useTrustAuthority; }
            set { useTrustAuthority = value; }
        }

        public SimpleShapeType GetSimpleShapeType(T layer)
        {
            return GetSimpleShapeTypeCore(layer);
        }

        protected abstract SimpleShapeType GetSimpleShapeTypeCore(T layer);

        public T CreateLayer()
        {
            var layer = CreateLayerCore();
            layer.Name = TableName;
            layer.AddBlankZoomLevels();
            layer.DrawingMarginInPixel = 200;
            layer.Open();
            var simpleShapeType = GetSimpleShapeType(layer);
            Styles.Style shapeStyle = null;

            switch (simpleShapeType)
            {
                case SimpleShapeType.Point:
                    shapeStyle = GetDefaultStyle<PointStyle>(StyleCategories.Point);
                    if (shapeStyle == null)
                    {
                        shapeStyle = PointStyles.CreateSimpleCircleStyle(RandomColor(), 6);
                        shapeStyle.Name = "Simple Style 1";
                    }
                    break;
                case SimpleShapeType.Line:
                    shapeStyle = GetDefaultStyle<LineStyle>(StyleCategories.Line);
                    if (shapeStyle == null)
                    {
                        shapeStyle = new LineStyle(new GeoPen(RandomColor(RandomColorType.Bright)));
                        shapeStyle.Name = "Simple Style 1";
                    }
                    break;
                case SimpleShapeType.Area:
                    shapeStyle = GetDefaultStyle<AreaStyle>(StyleCategories.Area);
                    if (shapeStyle == null)
                    {
                        shapeStyle = AreaStyles.CreateSimpleAreaStyle(RandomColor(), GeoColor.SimpleColors.Black);
                        shapeStyle.Name = "Simple Style 1";
                    }
                    break;
            }

            if (shapeStyle != null)
            {
                ZoomLevelSet zoomLevelSet = GisEditor.ActiveMap.ZoomLevelSet;
                ZoomLevel zoomLevel = new ZoomLevel(zoomLevelSet.ZoomLevel01.Scale);
                zoomLevel.CustomStyles.Add(shapeStyle);
                zoomLevel.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;

                layer.ZoomLevelSet.CustomZoomLevels[0] = zoomLevel;
                layer.DrawingQuality = DrawingQuality.CanvasSettings;
            }
            layer.Close();

            return layer;
        }

        protected abstract T CreateLayerCore();

        public Collection<string> CollectTablesFromDatabase()
        {
            return CollectTablesFromDatabaseCore();
        }

        protected abstract Collection<string> CollectTablesFromDatabaseCore();

        public Collection<string> CollectColumnsFromTable()
        {
            return CollectColumnsFromTableCore();
        }

        protected abstract Collection<string> CollectColumnsFromTableCore();

        public Collection<string> CollectDatabaseFromServer()
        {
            return CollectDatabaseFromServerCore();
        }

        protected abstract Collection<string> CollectDatabaseFromServerCore();

        private CoreStyle GetDefaultStyle<CoreStyle>(StyleCategories styleProviderTypes) where CoreStyle : Styles.Style
        {
            var provider = GisEditor.StyleManager.GetDefaultStylePlugin(styleProviderTypes);
            if (provider != null)
            {
                var styleProviderOptionUI = provider.GetSettingsUI();

                if (styleProviderOptionUI != null)
                {
                    bool useRandomColors = GetUseRandomColorsOption<CoreStyle>(provider.UseRandomColor, styleProviderTypes);
                    if (useRandomColors)
                    {
                        return null;
                    }
                }

                CoreStyle tmpStyle = provider.GetDefaultStyle() as CoreStyle;
                if (tmpStyle != null)
                {
                    return (CoreStyle)tmpStyle.CloneDeep();
                }
            }

            return null;
        }

        private bool GetUseRandomColorsOption<CoreStyle>(bool useRandomColor, StyleCategories styleProviderTypes) where CoreStyle : Styles.Style
        {
            if (styleProviderTypes == StyleCategories.Area
                || styleProviderTypes == StyleCategories.Line
                || styleProviderTypes == StyleCategories.Point)
            {
                return useRandomColor;
            }

            return false;
        }

        private static GeoColor RandomColor(RandomColorType randomColorType = RandomColorType.Pastel)
        {
            return GeoColor.GetRandomGeoColor(randomColorType);
        }
    }
}
