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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public static class FeatureLayerExtension
    {
        public static string[] GetDistinctColumnNames(this FeatureLayer featureLayer)
        {
            Collection<string> columnNames = new Collection<string>();
            foreach (var item in featureLayer.FeatureSource.GetColumns(GettingColumnsType.All))
            {
                columnNames.Add(item.ColumnName);
            }

            return columnNames.Distinct(new ColumnNamesComparer()).ToArray();
        }

        public static void SetFeatureLayerAccess(this FeatureLayer featureLayer, LayerAccessMode layerAccessMode)
        {
            ShapeFileFeatureLayer shapeFileFeatureLayer = featureLayer as ShapeFileFeatureLayer;
            if (shapeFileFeatureLayer != null)
            {
                GeoFileReadWriteMode readWriteMode = GeoFileReadWriteMode.Read;
                switch (layerAccessMode)
                {
                    case LayerAccessMode.Write:
                    case LayerAccessMode.ReadWrite:
                        readWriteMode = GeoFileReadWriteMode.ReadWrite;
                        break;

                    case LayerAccessMode.Read:
                    default:
                        readWriteMode = GeoFileReadWriteMode.Read;
                        break;
                }
                shapeFileFeatureLayer.ReadWriteMode = readWriteMode;
            }
        }

        public static string[] GetDistinctColumnNames(this FeatureSource featureSource)
        {
            Collection<string> columnNames = new Collection<string>();
            foreach (var item in featureSource.GetColumns())
            {
                columnNames.Add(item.ColumnName);
            }

            return columnNames.Distinct(new ColumnNamesComparer()).ToArray();
        }

        public static void LoadFrom<T>(this GeoCollection<T> sourceCollection, GeoCollection<T> targetCollection)
        {
            sourceCollection.Clear();
            foreach (var key in targetCollection.GetKeys())
            {
                sourceCollection.Add(key, targetCollection[key]);
            }
        }

        public static void SafeProcess(this FeatureLayer featureLayer, Action process)
        {
            bool isClosed = false;
            if (!featureLayer.IsOpen)
            {
                featureLayer.Open();
                isClosed = true;
            }

            if (process != null) process();

            if (isClosed)
            {
                featureLayer.Close();
                if (featureLayer.FeatureSource.Projection != null)
                {
                    featureLayer.FeatureSource.Projection.Close();
                }
            }
        }

        public static void SafeProcess(this FeatureSource featureSource, Action process)
        {
            bool isClosed = false;
            if (!featureSource.IsOpen)
            {
                featureSource.Open();
                isClosed = true;
            }

            if (process != null) process();

            if (isClosed)
            {
                featureSource.Close();
                if (featureSource.Projection != null)
                {
                    featureSource.Projection.Close();
                }
            }
        }

        public static Proj4ProjectionInfo GetProj4ProjectionInfo(this Layer layer)
        {
            Proj4ProjectionInfo proj4ProjectionInfo = null;
            FeatureLayer featureLayer = layer as FeatureLayer;
            RasterLayer rasterLayer = layer as RasterLayer;
            if (featureLayer != null)
            {
                Proj4Projection proj4Projection = featureLayer.FeatureSource.Projection as Proj4Projection;
                Proj4Projection managedProj4Projection = featureLayer.FeatureSource.Projection as Proj4Projection;

                if (proj4Projection != null)
                {
                    proj4ProjectionInfo = new UnManagedProj4ProjectionInfo(proj4Projection);
                }
                else if (managedProj4Projection != null)
                {
                    proj4ProjectionInfo = new ManagedProj4ProjectionInfo(managedProj4Projection);
                }
            }
            else if (rasterLayer != null)
            {
                Proj4Projection proj4Projection = rasterLayer.ImageSource.Projection as Proj4Projection;
                Proj4Projection managedProj4Projection = rasterLayer.ImageSource.Projection as Proj4Projection;

                if (proj4Projection != null)
                {
                    proj4ProjectionInfo = new UnManagedProj4ProjectionInfo(proj4Projection);
                }
                else if (managedProj4Projection != null)
                {
                    proj4ProjectionInfo = new ManagedProj4ProjectionInfo(managedProj4Projection);
                }
            }

            return proj4ProjectionInfo;
        }

        public static Collection<FeatureSourceColumn> GetColumns(this FeatureSource featureSource, GettingColumnsType returningFeatureSourceColumnsType)
        {
            //LINK: removed link logic.
            Collection<FeatureSourceColumn> columns = featureSource.GetColumns();
            return columns;
        }

        [Obsolete("This method is obsolete, please use AliasExtension.GetColumnAlias(this FeatureSource featureSource, String columnName) instead. This API is obsolete and may be removed in or after version 9.0.")]
        public static String GetAliasName(this FeatureSource featureSource, String columnName)
        {
            return AliasExtension.GetColumnAlias(featureSource, columnName);
        }

        [Obsolete("This method is obsolete, please use AliasExtension.GetColumnAlias(this FeatureSource featureSource, String columnName) instead. This API is obsolete and may be removed in or after version 9.0.")]
        public static String GetColumnName(FeatureSource featureSource, String aliasName)
        {
            return AliasExtension.GetColumnAlias(featureSource, aliasName);
        }

        [Obsolete("This method is obsolete, please use GetProj4ProjectionInfo(this Layer layer) instead. This API is obsolete and may be removed in or after version 9.0.")]
        public static Proj4ProjectionInfo GetProj4ProjectionInfo(this FeatureLayer featureLayer)
        {
            return ((Layer)featureLayer).GetProj4ProjectionInfo();
        }
    }
}