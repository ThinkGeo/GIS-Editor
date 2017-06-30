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
using System.IO;
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ExportTaskPlugin : GeoTaskPlugin
    {
        private bool hasLinkSource;
        private Collection<FeatureSourceColumn> featureSourceColumns;
        private Collection<Feature> featuresForExporting;
        private Collection<string> featureIdsForExporting;
        private FeatureLayer featureLayer;
        private string outputPathFileName;
        private string projectionWkt;
        private string internalPrj4Projection;
        private Dictionary<string, string> costomizedColumnNames;

        public ExportTaskPlugin()
        {
            featureSourceColumns = new Collection<FeatureSourceColumn>();
            featuresForExporting = new Collection<Feature>();
            featureIdsForExporting = new Collection<string>();
            costomizedColumnNames = new Dictionary<string, string>();
        }

        public bool NeedConvertMemoToCharacter { get; set; }

        public string InternalPrj4Projection
        {
            get { return internalPrj4Projection; }
            set { internalPrj4Projection = value; }
        }

        public string OutputPathFileName
        {
            get { return outputPathFileName; }
            set { outputPathFileName = value; }
        }

        public string ProjectionWkt
        {
            get { return projectionWkt; }
            set { projectionWkt = value; }
        }

        public FeatureLayer FeatureLayer
        {
            get { return featureLayer; }
            set { featureLayer = value; }
        }

        public Collection<Feature> FeaturesForExporting
        {
            get { return featuresForExporting; }
        }

        public Dictionary<string, string> CostomizedColumnNames
        {
            get { return costomizedColumnNames; }
        }

        public Collection<string> FeatureIdsForExporting
        {
            get { return featureIdsForExporting; }
        }

        public Collection<FeatureSourceColumn> FeatureSourceColumns
        {
            get { return featureSourceColumns; }
        }

        protected override void LoadCore()
        {
            Name = GisEditor.LanguageManager.GetStringResource("ExportTaskPluginOperationText");
            Description = Name + " " + GisEditor.LanguageManager.GetStringResource("TaskPluginCreatingText") + " " + Path.GetFileName(OutputPathFileName);
        }

        protected override void RunCore()
        {
            //if (FeatureLayer != null && FeatureLayer.FeatureSource.LinkSources.Count > 0)
            //{
            //    hasLinkSource = true;
            //}

            if (FeaturesForExporting.Count == 0 && FeatureIdsForExporting.Count > 0 && FeatureLayer != null)
            {
                FeatureLayer.CloseAll();
                FeatureLayer.SafeProcess(() =>
                {
                    //if (FeatureLayer.FeatureSource.LinkSources.Count > 0)
                    //{
                    //    featuresForExporting = FeatureLayer.FeatureSource.GetFeaturesByIds(FeatureIdsForExporting, FeatureLayer.GetDistinctColumnNames());
                    //    hasLinkSource = true;
                    //}
                    //else
                    //{
                    //    featuresForExporting = FeatureLayer.QueryTools.GetFeaturesByIds(FeatureIdsForExporting, FeatureLayer.GetDistinctColumnNames());
                    //    hasLinkSource = false;
                    //}

                    featuresForExporting = FeatureLayer.QueryTools.GetFeaturesByIds(FeatureIdsForExporting, FeatureLayer.GetDistinctColumnNames());
                    hasLinkSource = false;
                });
            }

            Proj4Projection proj4 = new Proj4Projection();
            proj4.InternalProjectionParametersString = InternalPrj4Projection;
            proj4.ExternalProjectionParametersString = Proj4Projection.ConvertPrjToProj4(ProjectionWkt);
            proj4.SyncProjectionParametersString();
            proj4.Open();

            Collection<Feature> exportFeatures = FeaturesForExporting;
            if (proj4.CanProject())
            {
                exportFeatures = new Collection<Feature>();
                foreach (var item in FeaturesForExporting)
                {
                    Feature newFeature = item;
                    newFeature = proj4.ConvertToExternalProjection(item);
                    exportFeatures.Add(newFeature);
                }
            }
            proj4.Close();

            #region This is a trick to fix that fox pro columns cannot write to dbf, convert all fox pro columns to character column type.

            if (hasLinkSource)
            {
                Collection<string> dateTimeColumns = new Collection<string>();
                Collection<string> dateColumns = new Collection<string>();

                foreach (var item in FeatureSourceColumns)
                {
                    string doubleInBinary = DbfColumnType.DoubleInBinary.ToString();
                    string integerInBinary = DbfColumnType.IntegerInBinary.ToString();
                    string dateTime = DbfColumnType.DateTime.ToString();
                    string date = DbfColumnType.Date.ToString();
                    string logical = DbfColumnType.Logical.ToString();

                    if (item.TypeName.Equals(doubleInBinary, StringComparison.OrdinalIgnoreCase)
                        || item.TypeName.Equals(integerInBinary, StringComparison.OrdinalIgnoreCase)
                        || item.TypeName.Equals(dateTime, StringComparison.OrdinalIgnoreCase)
                        || item.TypeName.Equals(date, StringComparison.OrdinalIgnoreCase)
                        || item.TypeName.Equals(logical, StringComparison.OrdinalIgnoreCase))
                    {
                        item.TypeName = DbfColumnType.Character.ToString();
                    }
                }

                Dictionary<string, int> longLengthColumnNames = new Dictionary<string, int>();

                //foreach (var feature in exportFeatures)
                //{
                //    // Fill link column values into column values.
                //    foreach (var item in feature.LinkColumnValues)
                //    {
                //        string key = item.Key;
                //        string value = string.Join("\r\n", item.Value.Select(v => v.Value));
                //        feature.ColumnValues[key] = value;
                //        longLengthColumnNames[key] = value.Length;
                //    }
                //}

                // fix too long column value
                foreach (var key in longLengthColumnNames.Keys)
                {
                    FeatureSourceColumn column = FeatureSourceColumns.FirstOrDefault(f => f.ColumnName.Equals(key, StringComparison.OrdinalIgnoreCase));
                    if (column != null)
                    {
                        int length = longLengthColumnNames[key];
                        if (length > column.MaxLength)
                        {
                            column.MaxLength = length;
                        }
                    }
                }
            }

            #endregion This is a trick to fix that fox pro columns cannot write to dbf.

            if (NeedConvertMemoToCharacter)
            {
                foreach (var feature in exportFeatures)
                {
                    foreach (var item in FeatureSourceColumns)
                    {
                        if (item.TypeName.Equals(DbfColumnType.Memo.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            item.TypeName = DbfColumnType.Character.ToString();
                            item.MaxLength = 254;
                        }
                        else if (item.TypeName.Equals(DbfColumnType.Character.ToString(), StringComparison.OrdinalIgnoreCase)
                            && item.MaxLength > 254)
                        {
                            item.MaxLength = 254;
                        }
                        //if (feature.ColumnValues.ContainsKey(item.ColumnName) && feature.ColumnValues[item.ColumnName].Length > 254)
                        //{
                        //    feature.ColumnValues[item.ColumnName] = feature.ColumnValues[item.ColumnName].Substring(0, 254);
                        //}
                    }
                }
            }

            var info = new FileExportInfo(exportFeatures, FeatureSourceColumns, OutputPathFileName, ProjectionWkt);
            foreach (var item in CostomizedColumnNames)
            {
                info.CostomizedColumnNames.Add(item.Key, item.Value);
            }
            Export(info);
        }
    }
}