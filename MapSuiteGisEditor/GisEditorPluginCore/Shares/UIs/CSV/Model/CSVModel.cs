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
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Resources;
using System.Xml.Linq;
using Kent.Boogaart.KBCsv;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class CSVModel : INotifyPropertyChanged
    {
        private Collection<string> wktValues = new Collection<string>();

        private string[] columns;

        private string configFilePath;

        [NonSerialized]
        private DataTable dataTable;

        private readonly string longitudeColumnName = "LongitudeColumnName";

        private readonly string latitudeColumnName = "LatitudeColumnName";

        private readonly string wktColumnName = "WktColumnName";

        public event PropertyChangedEventHandler PropertyChanged;

        private CsvFeatureLayer csvFeatureLayer;

        private bool canAutoMatch;

        public CSVModel(CsvFeatureLayer delimitedFeatureLayer)
        {
            this.csvFeatureLayer = delimitedFeatureLayer;
            this.configFilePath = CsvFeatureLayer.DelimitedPathFilename + ".config";
        }

        public bool CanAutoMatch
        {
            get { return canAutoMatch; }
            set { canAutoMatch = value; }
        }

        public string ConfigFilePath
        {
            get { return configFilePath; }
        }

        public CsvFeatureLayer CsvFeatureLayer
        {
            get { return csvFeatureLayer; }
            set { csvFeatureLayer = value; }
        }

        public string CSVShortName { get { return Path.GetFileName(CsvFeatureLayer.DelimitedPathFilename); } }

        public string Delimiter
        {
            get { return CsvFeatureLayer.Delimiter; }
            set
            {
                CsvFeatureLayer.Delimiter = value; OnPropertyChanged("Delimiter");
                AutoMatch();
            }
        }

        public DelimitedSpatialColumnsType MappingType
        {
            get { return CsvFeatureLayer.SpatialColumnType; }
            set
            {
                CsvFeatureLayer.SpatialColumnType = value; OnPropertyChanged("MappingType");
                AutoMatch();
            }
        }

        public string LongitudeColumnName
        {
            get { return CsvFeatureLayer.XColumnName; }
            set
            {
                CsvFeatureLayer.XColumnName = value;
                OnPropertyChanged("LongitudeColumnName");
                OnPropertyChanged("IsReady");
            }
        }

        public string LatitudeColumnName
        {
            get { return CsvFeatureLayer.YColumnName; }
            set
            {
                CsvFeatureLayer.YColumnName = value;
                OnPropertyChanged("LatitudeColumnName");
                OnPropertyChanged("IsReady");
            }
        }

        public string WktColumnName
        {
            get { return CsvFeatureLayer.WellKnownTextColumnName; }
            set
            {
                CsvFeatureLayer.WellKnownTextColumnName = value;
                OnPropertyChanged("WktColumnName");
                OnPropertyChanged("IsReady");
            }
        }

        public bool IsReady
        {
            get
            {
                return (!String.IsNullOrEmpty(LatitudeColumnName)
                       && !String.IsNullOrEmpty(LongitudeColumnName)
                       || !String.IsNullOrEmpty(WktColumnName));
            }
        }

        public string[] GetAllColumns()
        {
            using (var csvReader = InitReader())
            {
                var results = csvReader.HeaderRecord.Values.ToArray();
                csvReader.Close();
                return results;
            }
        }

        private CsvReader InitReader()
        {
            CsvReader csvReader = null;

            if (File.Exists(CsvFeatureLayer.DelimitedPathFilename))
            {
                if (CsvFeatureLayer.Encoding == null)
                {
                    csvReader = new CsvReader(CsvFeatureLayer.DelimitedPathFilename);
                }
                else
                {
                    csvReader = new CsvReader(CsvFeatureLayer.DelimitedPathFilename, CsvFeatureLayer.Encoding);
                }

                if (!string.IsNullOrEmpty(Delimiter) && Delimiter.Length == 1)
                {
                    csvReader.ValueSeparator = Delimiter[0];
                }

                csvReader.ReadHeaderRecord();
            }

            return csvReader;
        }

        public void LoadCsvConfig()
        {
            string configurationPathFileName = csvFeatureLayer.DelimitedPathFilename + ".config";
            using (StreamReader sr = new StreamReader(configurationPathFileName))
            {
                Delimiter = sr.ReadLine();
                DelimitedSpatialColumnsType mappingType = DelimitedSpatialColumnsType.XAndY;
                if (Enum.TryParse<DelimitedSpatialColumnsType>(sr.ReadLine(), out mappingType))
                {
                    MappingType = mappingType;
                }
                csvFeatureLayer.XColumnName = sr.ReadLine();
                csvFeatureLayer.YColumnName = sr.ReadLine();
                csvFeatureLayer.WellKnownTextColumnName = sr.ReadLine();
            }
        }

        public void SaveCsvConfig()
        {
            BuildDelimitedConfigurationFile(csvFeatureLayer);
        }

        public static void BuildDelimitedConfigurationFile(CsvFeatureLayer csvFeatureLayer)
        {
            csvFeatureLayer.SafeProcess(() =>
            {
                string configurationPathFileName = csvFeatureLayer.DelimitedPathFilename + ".config";
                using (StreamWriter sw = new StreamWriter(configurationPathFileName))
                {
                    sw.WriteLine(csvFeatureLayer.Delimiter);
                    sw.WriteLine(csvFeatureLayer.SpatialColumnType);
                    sw.WriteLine(csvFeatureLayer.XColumnName);
                    sw.WriteLine(csvFeatureLayer.YColumnName);
                    sw.WriteLine(csvFeatureLayer.WellKnownTextColumnName);
                    sw.WriteLine(csvFeatureLayer.GetBoundingBox().GetWellKnownText());
                    sw.WriteLine(new FileInfo(csvFeatureLayer.DelimitedPathFilename).LastWriteTime.ToString());
                }
            });
        }

        public void CopySettingsTo(CSVModel targetModel)
        {
            targetModel.CsvFeatureLayer.Delimiter = Delimiter;
            targetModel.CsvFeatureLayer.XColumnName = LongitudeColumnName;
            targetModel.CsvFeatureLayer.YColumnName = LatitudeColumnName;
            targetModel.CsvFeatureLayer.WellKnownTextColumnName = WktColumnName;
            targetModel.CsvFeatureLayer.SpatialColumnType = MappingType;
        }

        public void AutoMatch()
        {
            if (!CanAutoMatch) return;

            if (columns == null)
            {
                columns = GetAllColumns();
            }
            List<string> columnsUpper = new List<string>(columns.Select(column => column.ToUpperInvariant()));
            StreamResourceInfo streamResourceInfo = Application.GetResourceStream(
                new Uri("/GisEditorPluginCore;component/Shares/UIs/CSV/ColumnName.xml", UriKind.RelativeOrAbsolute));
            XElement xRoot = XElement.Load(streamResourceInfo.Stream);
            var relatedElement = xRoot.Element(MappingType.ToString());
            if (relatedElement != null)
            {
                if (MappingType == DelimitedSpatialColumnsType.XAndY)
                {
                    foreach (var item in relatedElement.Elements())
                    {
                        var lonAttribute = item.Attribute(longitudeColumnName);
                        var latAttribute = item.Attribute(latitudeColumnName);
                        if (lonAttribute == null || latAttribute == null) continue;
                        string lon = item.Attribute(longitudeColumnName).Value.ToUpperInvariant();
                        string lat = item.Attribute(latitudeColumnName).Value.ToUpperInvariant();
                        int lonIndex = columnsUpper.IndexOf(lon);
                        int latIndex = columnsUpper.IndexOf(lat);
                        LongitudeColumnName = lonIndex >= 0 ? columns[lonIndex] : null;
                        LatitudeColumnName = latIndex >= 0 ? columns[latIndex] : null;
                        if (!string.IsNullOrEmpty(LongitudeColumnName) || !string.IsNullOrEmpty(LatitudeColumnName))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var item in relatedElement.Elements())
                    {
                        var attribute = item.Attribute(wktColumnName);
                        if (attribute == null) continue;
                        string wkt = attribute.Value.ToUpperInvariant();
                        int wktIndex = columnsUpper.IndexOf(wkt);
                        WktColumnName = wktIndex >= 0 ? columns[wktIndex] : null;
                        if (!string.IsNullOrEmpty(WktColumnName))
                        {
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(WktColumnName))
                    {
                        XElement wktValue = xRoot.Element("WellKnownTextValue");
                        foreach (var item in wktValue.Elements())
                        {
                            var attribute = item.Attribute("Value");
                            if (attribute == null) continue;
                            wktValues.Add(attribute.Value);
                        }
                        string wktText = Path.Combine(GisEditor.InfrastructureManager.SettingsPath, "WktValues.txt");
                        if (File.Exists(wktText))
                        {
                            using (StreamReader sr = new StreamReader(wktText))
                            {
                                string value = sr.ReadLine();
                                if (!string.IsNullOrEmpty(value))
                                {
                                    wktValues.Add(value);
                                }
                            }
                        }

                        DataTable dt = GetDatas();
                        if (dt.Rows.Count > 0)
                        {
                            bool match = false;
                            for (int i = 0; i < dt.Columns.Count; i++)
                            {
                                string value = dt.Rows[0][i].ToString();
                                foreach (var item in wktValues)
                                {
                                    if (value.IndexOf(item, StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        WktColumnName = dt.Columns[i].ColumnName;
                                        match = true;
                                        break;
                                    }
                                }
                                if (match)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public DataTable GetDatas()
        {
            using (var csvReader = InitReader())
            {
                dataTable = new DataTable();

                foreach (var field in csvReader.HeaderRecord.Values)
                {
                    dataTable.Columns.Add(field);
                }

                foreach (var record in csvReader.DataRecords)
                {
                    if (record.Values.Count == csvReader.HeaderRecord.Values.Count)
                    {
                        DataRow row = dataTable.NewRow();
                        foreach (var field in csvReader.HeaderRecord.Values)
                        {
                            row[field] = record.Values[csvReader.HeaderRecord.IndexOf(field)];
                        }

                        dataTable.Rows.Add(row);
                    }
                }
                csvReader.Close();
            }

            return dataTable;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
