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
using System.Reflection;
using System.Text;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class CsvFeatureSource : FeatureSource, IDisposable
    {
        private static readonly int maxValueInCoordinate = 1000000000;
        public event EventHandler<BuildingIndexCsvFeatureSourceEventArgs> BuildingIndex;

        [NonSerialized]
        private RtreeSpatialIndex rTreeIndex;
        [NonSerialized]
        private string[] configurationInformation;
        [NonSerialized]
        private string configurationPathFileName;

        [Obfuscation(Exclude = true)]
        private bool requireIndex;
        [Obfuscation(Exclude = true)]
        private string csvPathFileName;
        [Obfuscation(Exclude = true)]
        private Encoding encoding;
        [Obfuscation(Exclude = true)]
        private string delimiter;
        [Obfuscation(Exclude = true)]
        private string longitudeColumnName;
        [Obfuscation(Exclude = true)]
        private string latitudeColumnName;
        [Obfuscation(Exclude = true)]
        private string wktColumnName;
        [Obfuscation(Exclude = true)]
        private CsvMappingType mappingType;

        public CsvFeatureSource()
            : this(string.Empty)
        { }

        public CsvFeatureSource(string csvFileName)
        {
            CsvPathFileName = csvFileName;
            Encoding = Encoding.Default;
            this.CanModifyColumnStructureCore = true;

            string idxPathFileName = Path.ChangeExtension(csvFileName, ".idx");
            string idsPathFileName = Path.ChangeExtension(csvFileName, ".ids");
            if (!File.Exists(idxPathFileName) || !File.Exists(idsPathFileName)) RequireIndex = false;
            else RequireIndex = true;
        }

        public static void CreateCsvFile(string csvPathFileName, IEnumerable<string> databaseColumns, IEnumerable<Feature> features, string delimiter)
        {
            CreateCsvFile(csvPathFileName, databaseColumns, features, delimiter, OverwriteMode.DoNotOverwrite, Encoding.Default);
        }

        public static void CreateCsvFile(string csvPathFileName, IEnumerable<string> databaseColumns, IEnumerable<Feature> features, string delimiter, OverwriteMode overWriteMode, Encoding encoding)
        {
            CreateFile(csvPathFileName, databaseColumns, features, delimiter, overWriteMode, encoding);
        }       

        public bool RequireIndex
        {
            get { return requireIndex; }
            set { requireIndex = value; }
        }

        public string CsvPathFileName
        {
            get { return csvPathFileName; }
            set { csvPathFileName = value; }
        }

        public Encoding Encoding
        {
            get { return encoding; }
            set { encoding = value; }
        }

        public string Delimiter
        {
            get { return delimiter; }
            set { delimiter = value; }
        }

        public string LongitudeColumnName
        {
            get { return longitudeColumnName; }
            set { longitudeColumnName = value; }
        }

        public string LatitudeColumnName
        {
            get { return latitudeColumnName; }
            set { latitudeColumnName = value; }
        }

        public string WktColumnName
        {
            get { return wktColumnName; }
            set { wktColumnName = value; }
        }

        public CsvMappingType MappingType
        {
            get { return mappingType; }
            set { mappingType = value; }
        }

        protected override void OpenCore()
        {
            base.OpenCore();
            string idxPathFileName = Path.ChangeExtension(CsvPathFileName, "idx");
            string idsPathFileName = Path.ChangeExtension(CsvPathFileName, "ids");

            if (RequireIndex)
            {
                Validators.CheckFileExists(CsvPathFileName, "Csv file not found.");
                Validators.CheckFileExists(idsPathFileName, "Index file not found.");
                Validators.CheckFileExists(idxPathFileName, "Index file not found.");
                OpenRtree(idxPathFileName, GeoFileReadWriteMode.Read);
            }

            configurationPathFileName = CsvPathFileName + ".config";
            if (File.Exists(configurationPathFileName))
            {
                configurationInformation = File.ReadAllLines(configurationPathFileName);
            }
            else configurationInformation = null;
        }

        public override bool IsEditable
        {
            get
            {
                return true;
            }
        }

        protected override bool CanGetCountQuicklyCore()
        {
            return true;
        }

        protected override void CloseCore()
        {
            if (rTreeIndex != null)
            {
                rTreeIndex.Close();
                rTreeIndex = null;
            }

            configurationInformation = null;
        }

        protected override int GetCountCore()
        {
            int index = -1;
            using (StreamReader sr = new StreamReader(CsvPathFileName, Encoding))
            {
                while (!sr.EndOfStream)
                {
                    sr.ReadLine();
                    index++;
                }
            }
            return index;
        }

        protected override RectangleShape GetBoundingBoxCore()
        {
            var wkt = string.Empty;
            if (configurationInformation != null && configurationInformation.Length == 6)
            {
                wkt = configurationInformation[5];
            }

            try
            {
                if (!string.IsNullOrEmpty(wkt))
                {
                    return new RectangleShape(wkt);
                }
            }
            catch
            { }

            return base.GetBoundingBoxCore();
        }

        protected override Collection<FeatureSourceColumn> GetColumnsCore()
        {
            Collection<FeatureSourceColumn> result = new Collection<FeatureSourceColumn>();
            using (var csvReader = CreateCsvReader())
            {
                foreach (var headerValue in csvReader.HeaderRecord.Values)
                {
                    result.Add(new FeatureSourceColumn(headerValue, DbfColumnType.Character.ToString(), 255));
                }
            }

            return result;
        }

        protected override Collection<Feature> GetFeaturesByIdsCore(IEnumerable<string> ids, IEnumerable<string> returningColumnNames)
        {
            Collection<Feature> result = new Collection<Feature>();
            using (var csvReader = CreateCsvReader())
            {
                foreach (var dataRecord in csvReader.DataRecords)
                {
                    string id = csvReader.RecordNumber.ToString();
                    if (ids.Contains(id))
                    {
                        int recordNumber = 0;
                        if (int.TryParse(id, out recordNumber))
                        {
                            var feature = GetFeature(id, dataRecord);
                            if (feature != null)
                            {
                                result.Add(feature);
                            }
                        }
                    }
                }
            }

            return result;

            //ConcurrentQueue<Feature> results = new ConcurrentQueue<Feature>();
            //using (var csvReader = CreateCsvReader())
            //{
            //    Parallel.ForEach(csvReader.DataRecords, d =>
            //    {
            //        string id = csvReader.RecordNumber.ToString();
            //        if (ids.Contains(id))
            //        {
            //            var feature = GetFeature(id, d);
            //            if (feature != null)
            //            {
            //                results.Enqueue(feature);
            //            }
            //        }
            //    });
            //}
            //return new Collection<Feature>(results.ToList());
        }

        protected override Collection<Feature> GetAllFeaturesCore(IEnumerable<string> returningColumnNames, int startIndex, int takeCount)
        {
            Collection<Feature> features = new Collection<Feature>();
            using (var csvReader = CreateCsvReader())
            {
                foreach (var record in csvReader.DataRecords.Skip(startIndex).Take(takeCount))
                {
                    int recordNumber = (int)csvReader.RecordNumber;
                    Feature feature = GetFeature(csvReader.RecordNumber.ToString(), record);
                    if (feature != null)
                    {
                        features.Add(feature);
                    }
                }
            }

            return features;
        }

        protected override Collection<Feature> GetAllFeaturesCore(IEnumerable<string> returningColumnNames)
        {
            Collection<Feature> features = new Collection<Feature>();
            using (var csvReader = CreateCsvReader())
            {
                foreach (var record in csvReader.DataRecords)
                {
                    int recordNumber = (int)csvReader.RecordNumber;
                    Feature feature = GetFeature(csvReader.RecordNumber.ToString(), record);
                    if (feature != null)
                    {
                        features.Add(feature);
                    }
                }
            }

            return features;
        }

        private void OpenRtree(string idxPathFileName, GeoFileReadWriteMode rTreeFileAccess)
        {
            if (RequireIndex)
            {
                if (rTreeIndex == null)
                {
                    rTreeIndex = new RtreeSpatialIndex(idxPathFileName, rTreeFileAccess);
                }

                rTreeIndex.Open();
            }
        }

        private Kent.Boogaart.KBCsv.CsvReader CreateCsvReader()
        {
            Kent.Boogaart.KBCsv.CsvReader csvReader = null;
            if (Encoding != null)
            {
                csvReader = new Kent.Boogaart.KBCsv.CsvReader(CsvPathFileName, Encoding);
            }
            else
            {
                csvReader = new Kent.Boogaart.KBCsv.CsvReader(CsvPathFileName);
            }

            if (!string.IsNullOrEmpty(Delimiter) && Delimiter.Length == 1)
            {
                csvReader.ValueSeparator = Delimiter[0];
            }

            csvReader.ReadHeaderRecord();
            return csvReader;
        }

        public static DateTime GetLastWriteTime(string csvPathFileName)
        {
            DateTime lastWriteTime = default(DateTime);
            string configurationPathFileName = csvPathFileName + ".config";
            string[] configurationItems = File.ReadAllLines(configurationPathFileName);
            if (configurationItems.Length == 7)
            {
                if (!DateTime.TryParse(configurationItems[6], out lastWriteTime))
                {
                    lastWriteTime = default(DateTime);
                }
            }

            return lastWriteTime;
        }

        [Obsolete("This method will not be used anymore.")]
        public void BuildConfigurationFile()
        {
            BuildConfigurationFileCore();
        }

        [Obsolete("This method will not be used anymore.")]
        protected virtual void BuildConfigurationFileCore()
        {
            string configurationPathFileName = CsvPathFileName + ".config";
            using (StreamWriter sw = new StreamWriter(configurationPathFileName))
            {
                sw.WriteLine(Delimiter);
                sw.WriteLine(MappingType);
                sw.WriteLine(LongitudeColumnName);
                sw.WriteLine(LatitudeColumnName);
                sw.WriteLine(WktColumnName);
                sw.WriteLine(GetBoundingBox().GetWellKnownText());
                sw.WriteLine(new FileInfo(CsvPathFileName).LastWriteTime.ToString());
            }
        }

        [Obsolete("This method will not be used anymore.")]
        public void LoadConfiguration()
        {
            LoadConfigurationCore();
        }

        [Obsolete("This method will not be used anymore.")]
        protected virtual void LoadConfigurationCore()
        {
            string configurationPathFileName = CsvPathFileName + ".config";
            using (StreamReader sr = new StreamReader(configurationPathFileName))
            {
                Delimiter = sr.ReadLine();
                CsvMappingType mappingType = CsvMappingType.LongitudeAndLatitude;
                if (Enum.TryParse<CsvMappingType>(sr.ReadLine(), out mappingType))
                {
                    MappingType = mappingType;
                }
                LongitudeColumnName = sr.ReadLine();
                LatitudeColumnName = sr.ReadLine();
                WktColumnName = sr.ReadLine();
            }
        }

        protected override Collection<Feature> GetFeaturesInsideBoundingBoxCore(RectangleShape boundingBox, IEnumerable<string> returningColumnNames)
        {
            if (!RequireIndex)
            {
                return base.GetFeaturesInsideBoundingBoxCore(boundingBox, returningColumnNames);
            }

            Collection<Feature> results = new Collection<Feature>();
            string[] ids = rTreeIndex.GetFeatureIdsIntersectingBoundingBox(boundingBox).ToArray();

            if (ids.Length > 0)
            {
                results = GetFeaturesByIdsCore(ids, returningColumnNames);
            }

            return results;
        }

        protected override TransactionResult CommitTransactionCore(TransactionBuffer transactions)
        {
            TransactionResult transactionResult = new TransactionResult();

            //Delete Feature
            Collection<string> deleteBuffer = transactions.DeleteBuffer;
            if (deleteBuffer.Count > 0)
            {
                DeleteRecord(deleteBuffer, transactionResult);
            }

            //Get Column Names
            string[] columnNames = null;
            using (StreamReader streamReader = new StreamReader(CsvPathFileName, Encoding))
            {
                string columnHeaders = streamReader.ReadLine();
                if (!string.IsNullOrEmpty(columnHeaders))
                {
                    columnNames = columnHeaders.Split(Delimiter.ToCharArray());
                }
                else
                {
                    return transactionResult;
                }
            }

            //Add Feature
            Dictionary<string, Feature> addBuffer = transactions.AddBuffer;
            if (addBuffer.Count > 0)
            {
                AddRecord(addBuffer, columnNames, transactionResult);
            }

            //Update Feature
            Dictionary<string, Feature> editBuffer = transactions.EditBuffer;
            if (editBuffer.Count > 0)
            {
                UpdateRecord(editBuffer, columnNames, transactionResult);
            }

            //Delete columns
            if (transactions.DeleteColumnBuffer.Count > 0)
            {
                DeleteCsvColumns(transactions.DeleteColumnBuffer);
            }

            //Add columns
            if (transactions.AddColumnBuffer.Count > 0)
            {
                AddCsvColumns(transactions.AddColumnBuffer.Select(f => f.ColumnName).ToList());
            }

            //Update columns
            if (transactions.UpdateColumnBuffer.Count > 0)
            {
                UpdateCsvColumns(transactions.UpdateColumnBuffer);
            }


            RebuildIndex();
            return transactionResult;
        }

        private void DeleteCsvColumns(Collection<string> deleteColumns)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (StreamReader streamReader = new StreamReader(CsvPathFileName, (Encoding == null ? Encoding.Default : Encoding)))
            {
                string currentLine = currentLine = streamReader.ReadLine();
                int lineNumber = 1;
                Collection<int> columnIndexs = new Collection<int>();
                var columns = currentLine.Split(Delimiter.ToCharArray()).ToList();
                foreach (var item in deleteColumns)
                {
                    columnIndexs.Add(columns.IndexOf(item));
                }
                foreach (var item in columnIndexs)
                {
                    columns.RemoveAt(item);
                }
                stringBuilder.AppendLine(string.Join(Delimiter, columns));

                while (!string.IsNullOrEmpty(currentLine = streamReader.ReadLine()))
                {
                    List<string> recordValues = currentLine.Split(Delimiter.ToCharArray()).ToList();
                    foreach (var item in columnIndexs)
                    {
                        recordValues.RemoveAt(item);
                    }
                    stringBuilder.AppendLine(string.Join(Delimiter, recordValues));
                    lineNumber++;
                }
            }
            File.WriteAllText(CsvPathFileName, stringBuilder.ToString(), Encoding);
        }

        private void AddCsvColumns(List<string> addedColumns)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (StreamReader streamReader = new StreamReader(CsvPathFileName, (Encoding == null ? Encoding.Default : Encoding)))
            {
                string currentLine = currentLine = streamReader.ReadLine();
                int lineNumber = 1;
                Collection<int> columnIndexs = new Collection<int>();
                var newColumns = currentLine.Split(Delimiter.ToCharArray()).Concat(addedColumns).ToList();

                stringBuilder.AppendLine(string.Join(Delimiter, newColumns));

                while (!string.IsNullOrEmpty(currentLine = streamReader.ReadLine()))
                {
                    for (int i = 0; i < addedColumns.Count; i++)
                    {
                        currentLine += Delimiter;
                    }
                    stringBuilder.AppendLine(currentLine);
                    lineNumber++;
                }
            }
            File.WriteAllText(CsvPathFileName, stringBuilder.ToString(), Encoding);
        }

        private void UpdateCsvColumns(Dictionary<string, FeatureSourceColumn> updateColumns)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (StreamReader streamReader = new StreamReader(CsvPathFileName, (Encoding == null ? Encoding.Default : Encoding)))
            {
                string currentLine = currentLine = streamReader.ReadLine();
                int lineNumber = 1;
                Collection<int> columnIndexs = new Collection<int>();
                string[] newColumns = currentLine.Split(Delimiter.ToCharArray());

                for (int i = 0; i < newColumns.Length; i++)
                {
                    if (updateColumns.ContainsKey(newColumns[i]))
                    {
                        newColumns[i] = updateColumns[newColumns[i]].ColumnName;
                    }
                }

                stringBuilder.AppendLine(string.Join(Delimiter, newColumns));

                while (!string.IsNullOrEmpty(currentLine = streamReader.ReadLine()))
                {
                    stringBuilder.AppendLine(currentLine);
                    lineNumber++;
                }
            }
            File.WriteAllText(CsvPathFileName, stringBuilder.ToString(), Encoding);
        }

        private void RebuildConfiguration()
        {
            if (configurationInformation != null && configurationInformation.Length == 6 && File.Exists(configurationPathFileName))
            {
                configurationInformation[5] = GetBoundingBox().GetWellKnownText();
                File.WriteAllLines(configurationPathFileName, configurationInformation);
            }
        }

        private void RebuildIndex()
        {
            if (RequireIndex)
            {
                rTreeIndex.Close();
                BuildIndexFile(true);
                rTreeIndex.Open();
            }
        }

        private void AddRecord(Dictionary<string, Feature> addBuffer, string[] columnNames, TransactionResult transactionResult)
        {
            using (StreamWriter streamWriter = new StreamWriter(CsvPathFileName, true, Encoding))
            {
                foreach (Feature feature in addBuffer.Values)
                {
                    try
                    {
                        streamWriter.WriteLine(GetRowData(feature, columnNames));
                        transactionResult.TotalSuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        transactionResult.TotalFailureCount++;
                        transactionResult.FailureReasons.Add(feature.Id, ex.ToString());
                    }
                }
            }
        }

        private void DeleteRecord(Collection<string> deleteBuffer, TransactionResult transactionResult)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (StreamReader streamReader = new StreamReader(CsvPathFileName, (Encoding == null ? Encoding.Default : Encoding)))
            {
                string currentLine = null;
                int lineNumber = 0;
                while (!string.IsNullOrEmpty(currentLine = streamReader.ReadLine()))
                {
                    if (deleteBuffer.Contains(lineNumber.ToString()))
                    {
                        try
                        {
                            deleteBuffer.Remove(lineNumber.ToString());
                            transactionResult.TotalSuccessCount++;
                            lineNumber++;

                            continue;
                        }
                        catch (Exception ex)
                        {
                            transactionResult.TotalFailureCount++;
                            transactionResult.FailureReasons.Add(lineNumber.ToString(), ex.ToString());
                        }
                    }
                    stringBuilder.AppendLine(currentLine);
                    lineNumber++;
                }
            }
            File.WriteAllText(CsvPathFileName, stringBuilder.ToString(), Encoding);
        }

        private void UpdateRecord(Dictionary<string, Feature> keyValue, string[] columnNames, TransactionResult transactionResult)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (StreamReader streamReader = new StreamReader(CsvPathFileName, Encoding))
            {
                int lineNumber = 0;
                string currentLine = null;
                while (!string.IsNullOrEmpty((currentLine = streamReader.ReadLine())))
                {
                    try
                    {
                        if (keyValue.ContainsKey(lineNumber.ToString()))
                        {
                            stringBuilder.AppendLine(GetRowData(keyValue[lineNumber.ToString()], columnNames));
                        }
                        else
                        {
                            stringBuilder.AppendLine(currentLine);
                        }
                        lineNumber++;
                        transactionResult.TotalSuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        transactionResult.TotalFailureCount++;
                        transactionResult.FailureReasons.Add(lineNumber.ToString(), ex.Message);
                    }
                }
            }
            File.WriteAllText(CsvPathFileName, stringBuilder.ToString(), Encoding);
        }

        private string GetRowData(Feature feature, string[] columnNames)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (MappingType == CsvMappingType.LongitudeAndLatitude)
            {
                foreach (string columnName in columnNames)
                {
                    var newColumnName = columnName.Trim();
                    if (newColumnName == LongitudeColumnName)
                    {
                        stringBuilder.Append(feature.GetShape().GetCenterPoint().X.ToString());
                    }
                    else if (newColumnName == LatitudeColumnName)
                    {
                        stringBuilder.Append(feature.GetShape().GetCenterPoint().Y.ToString());
                    }
                    else
                    {
                        if (feature.ColumnValues.ContainsKey(newColumnName))
                        {
                            string value = feature.ColumnValues[newColumnName];

                            value = value.Replace("\"", "\"\"");
                            if (value.Contains(",") || value.Contains("\""))
                            {
                                value = "\"" + value + "\"";
                            }

                            stringBuilder.Append(value);

                        }
                    }
                    stringBuilder.Append(Delimiter);
                }
            }
            else if (MappingType == CsvMappingType.WellKnownText)
            {
                foreach (string columnName in columnNames)
                {
                    var newColumnName = columnName.Trim();
                    if (newColumnName == WktColumnName)
                    {
                        stringBuilder.Append(feature.GetWellKnownText());
                    }
                    else
                    {
                        string value = feature.ColumnValues[newColumnName];

                        if (value.Contains(",") || value.Contains("\""))
                        {
                            value = "\"" + value + "\"";
                        }

                        stringBuilder.Append(value);
                    }
                    stringBuilder.Append(Delimiter);
                }
            }
            else
            {
                throw new ArgumentException("MappingType is incorrect.");
            }

            var result = stringBuilder.ToString();
            if (result.Length > 0)
                result = result.Remove(stringBuilder.Length - 1);

            return result;
        }

        private Feature GetFeature(string id, Kent.Boogaart.KBCsv.DataRecord csvDataRecord)
        {
            var mappingType = MappingType;
            var latColumn = LatitudeColumnName;
            var lonColumn = LongitudeColumnName;
            var wktColumn = WktColumnName;
            var lineNumber = id;

            if (csvDataRecord == null || csvDataRecord.HeaderRecord == null)
            {
                return null;
            }

            int i = -1;
            var list = csvDataRecord.Values.Select(value =>
            {
                i++;
                return csvDataRecord.HeaderRecord[i] + ':' + value;
            });

            switch (mappingType)
            {
                case CsvMappingType.LongitudeAndLatitude:
                    double x, y;
                    bool xParsed = double.TryParse(csvDataRecord.Values[csvDataRecord.HeaderRecord.IndexOf(lonColumn)], out x);
                    bool yParsed = double.TryParse(csvDataRecord.Values[csvDataRecord.HeaderRecord.IndexOf(latColumn)], out y);
                    if (xParsed && yParsed && Math.Abs(x) < maxValueInCoordinate && Math.Abs(y) < maxValueInCoordinate)
                    {
                        return new Feature(x, y, lineNumber.ToString(), list);
                    }
                    else
                    {
                        return null;
                    }
                case CsvMappingType.WellKnownText:
                    string wkt = csvDataRecord.Values[csvDataRecord.HeaderRecord.IndexOf(wktColumn)];
                    var feature = new Feature(wkt, lineNumber.ToString(), list);
                    return feature;
                default:
                    return null;
            }
        }

        public void BuildIndexFile(bool rebuild = false)
        {
            BuildIndexFileCore(rebuild);
        }

        protected virtual void BuildIndexFileCore(bool rebuild)
        {
            string idxPathFileName = Path.ChangeExtension(CsvPathFileName, ".idx");
            string idsPathFileName = Path.ChangeExtension(CsvPathFileName, ".ids");
            if (!File.Exists(idxPathFileName) && !File.Exists(idsPathFileName) || rebuild)
            {
                if (!string.IsNullOrEmpty(WktColumnName))
                {
                    RtreeSpatialIndex.CreateRectangleSpatialIndex(idxPathFileName, RtreeSpatialIndexPageSize.EightKilobytes, RtreeSpatialIndexDataFormat.Float);
                }
                else
                {
                    RtreeSpatialIndex.CreatePointSpatialIndex(idxPathFileName, RtreeSpatialIndexPageSize.EightKilobytes, RtreeSpatialIndexDataFormat.Float);
                }

                using (RtreeSpatialIndex tempRTree = new RtreeSpatialIndex(idxPathFileName, GeoFileReadWriteMode.ReadWrite))
                {
                    tempRTree.Open();
                    bool isCanceled = false;
                    DateTime startDate = DateTime.Now;
                    using (var csvReader = CreateCsvReader())
                    {
                        var allRecords = csvReader.DataRecords.ToArray();
                        int index = 0;
                        foreach (var currentDataRecord in allRecords)
                        {
                            index++;
                            var feature = GetFeature(index.ToString(), currentDataRecord);
                            if (feature != null)
                            {
                                tempRTree.Add(feature);
                                BuildingIndexCsvFeatureSourceEventArgs e = new BuildingIndexCsvFeatureSourceEventArgs(allRecords.Length, index, feature, startDate, false);
                                OnBuildingIndex(e);
                                if (e.Cancel)
                                {
                                    isCanceled = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!isCanceled)
                    {
                        tempRTree.Flush();
                    }
                    else
                    {
                        if (File.Exists(idxPathFileName)) File.Delete(idxPathFileName);
                        if (File.Exists(idsPathFileName)) File.Delete(idsPathFileName);
                    }
                }
            }
        }

        protected void OnBuildingIndex(BuildingIndexCsvFeatureSourceEventArgs e)
        {
            EventHandler<BuildingIndexCsvFeatureSourceEventArgs> handler = BuildingIndex;
            if (handler != null)
            {
                handler(null, e);
            }
        }

        ~CsvFeatureSource()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (rTreeIndex != null) rTreeIndex.Dispose();
            }
        }

        private static void CreateFile(string pathFilename, IEnumerable<string> columns, IEnumerable<Feature> features, string delimiter, OverwriteMode overwriteMode,
            Encoding encoding)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var column in columns)
            {
                sb.Append(column);
                sb.Append(delimiter);
            }
            sb.Remove(sb.Length - 1, 1);
            sb.AppendLine();

            //foreach (var feature in features)
            //{
            //    foreach (var value in feature.ColumnValues)
            //    {
            //        sb.Append("\"");
            //        sb.Append(value.Value);
            //        sb.Append("\"");
            //        sb.Append(delimiter);
            //    }
            //    sb.Remove(sb.Length - 1, 1);
            //    sb.AppendLine();
            //}

            File.WriteAllText(pathFilename, sb.ToString());
        }

        #region wrapper

        public DelimitedSpatialColumnsType SpatialColumnType
        {
            get { return (DelimitedSpatialColumnsType)MappingType; }
            set { MappingType = (CsvMappingType)value; }
        }

        public string WellKnownTextColumnName
        {
            get { return WktColumnName; }
            set { WktColumnName = value; }
        }

        public string DelimitedPathFilename
        {
            get { return CsvPathFileName; }
            set { CsvPathFileName = value; }
        }

        public string XColumnName
        {
            get { return LongitudeColumnName; }
            set { LongitudeColumnName = value; }
        }

        public string YColumnName
        {
            get { return LatitudeColumnName; }
            set { LatitudeColumnName = value; }
        }

        public void BuildIndexFile(string delimitedPathFilename, string wellKnownTextColumnName, string delimiter, BuildIndexMode buildIndexMode)
        {
            BuildIndexFile();
        }

        #endregion
    }
}