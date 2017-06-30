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


using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class ShapeFileFeatureLayerExtension
    {
        private static readonly int recordLength = 4;
        private static readonly int shxHeaderLength = 100;

        public static int GetRecordCount(this ShapeFileFeatureLayer featureLayer)
        {
            return GetRecordCountInternal(featureLayer);
        }

        public static Dictionary<int, int> GetRecordsContentLength(this ShapeFileFeatureLayer featureLayer, IEnumerable<int> ids)
        {
            return GetRecordContentLengthInternal(featureLayer, ids);
        }

        public static Collection<int> GetAvailableFeatureIds(this ShapeFileFeatureLayer featureLayer)
        {
            return GetAvailableFeatureIdsInternal(featureLayer);
        }

        public static ShapeFileType GetShapeFileType(string shapePathFileName)
        {
            return GetShapeFileTypeInternal(shapePathFileName);
        }

        public static void RemoveShapeFiles(string shapePathFileName)
        {
            IOUtil.RemoveShapeFiles(shapePathFileName);
        }

        private static ShapeFileType GetShapeFileTypeInternal(string shapePathFileName)
        {
            Stream stream = null;
            try
            {
                stream = File.OpenRead(shapePathFileName);
                stream.Seek(32, SeekOrigin.Begin);
                var shapeTypeFlag = IOUtil.ReadIntFromStream(stream, WkbByteOrder.LittleEndian);
                return ShapeFileHeader.ConvertIntToShapeFileType(shapeTypeFlag);
            }
            finally
            {
                if (stream != null) stream.Dispose();
            }
        }

        private static Collection<int> GetAvailableFeatureIdsInternal(ShapeFileFeatureLayer featureLayer)
        {
            Collection<int> results = new Collection<int>();

            string shxPathFileName = Path.ChangeExtension(featureLayer.ShapePathFilename, ".shx");
            ShapeFileIndex shx = null;
            try
            {
                shx = new ShapeFileIndex(shxPathFileName);
                shx.Open();

                var count = shx.GetRecordCount();
                for (int i = 1; i <= count; i++)
                {
                    var contentLength = shx.GetRecordContentLength(i);
                    if (contentLength != 0)
                    {
                        results.Add(i);
                    }
                }
            }
            finally
            {
                if (shx != null) shx.Close();
            }

            return results;
        }

        private static Dictionary<int, int> GetRecordContentLengthInternal(ShapeFileFeatureLayer featureLayer, IEnumerable<int> ids)
        {
            Dictionary<int, int> results = new Dictionary<int, int>();
            string shxPathFileName = Path.ChangeExtension(featureLayer.ShapePathFilename, ".shx");
            ShapeFileIndex shx = null;
            try
            {
                shx = new ShapeFileIndex(shxPathFileName);
                shx.Open();

                foreach (var id in ids)
                {
                    int contentLength = shx.GetRecordContentLength(id);
                    results.Add(id, contentLength);
                }
            }
            finally
            {
                if (shx != null) shx.Close();
            }

            return results;
        }

        private static int GetRecordCountInternal(ShapeFileFeatureLayer featureLayer)
        {
            if (featureLayer.RequireIndex && File.Exists(featureLayer.IndexPathFilename))
            {
                using (RtreeSpatialIndex index = new RtreeSpatialIndex(featureLayer.IndexPathFilename))
                {
                    index.Open();
                    int recordCount = index.GetFeatureCount();
                    index.Close();
                    return recordCount;
                }
            }
            else
            {
                int recordCount = featureLayer.QueryTools.GetCount();
                string shxFilePath = Path.ChangeExtension(featureLayer.ShapePathFilename, "shx");
                FileStream shxFileStream = File.OpenRead(shxFilePath);
                BinaryReader shxReader = new BinaryReader(shxFileStream);
                try
                {
                    shxReader.BaseStream.Seek(shxHeaderLength, SeekOrigin.Begin);
                    bool needBreak = false;
                    while (shxReader.BaseStream.Position != shxReader.BaseStream.Length)
                    {
                        shxReader.BaseStream.Seek(recordLength, SeekOrigin.Current);
                        int length = 0;
                        if (shxReader.BaseStream.Position != shxReader.BaseStream.Length)
                            length = shxReader.ReadInt32();
                        else
                            needBreak = true;
                        if (length == 0) recordCount--;
                        if (needBreak) break;
                    }
                }
                finally
                {
                    shxFileStream.Close();
                    shxReader.Close();
                }
                return recordCount;
            }
        }
    }
}