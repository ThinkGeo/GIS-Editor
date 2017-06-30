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
using System.IO;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    internal class BasEntityParser
    {
        public static BasEntity Parse(BinaryReader br)
        {
            BasEntity entity = new BasEntity();
            entity.Offset = br.BaseStream.Position;
            entity.HeaderResult = ReadHeader(br);

            if (br.BaseStream.Length - br.BaseStream.Position > 2)
            {
                br.BaseStream.Seek(2, SeekOrigin.Current); // Skip "\r\n"
            }

            do
            {
                BasRecordType.DataType type = BasHelper.GetType(br);

                if (type == BasRecordType.DataType.Annotation)
                {
                    entity.AnnotationsResult.Add(ReadAnnotation(br));
                }
                else if (type == BasRecordType.DataType.Coordinate)
                {
                    entity.CoordinatesResult.Add(ReadCoordinate(br));
                }
                else
                {
                    break;
                }

                // Skip "\r\n"
                if (br.BaseStream.Length - br.BaseStream.Position > 2)
                {
                    br.BaseStream.Seek(2, SeekOrigin.Current);
                }
            }
            while (true);

            return entity;
        }

        private static RecordHeader ReadHeader(BinaryReader br)
        {
            RecordHeader header = new RecordHeader();
            header.Read(br);
            return header;
        }

        private static RecordAnnotation ReadAnnotation(BinaryReader br)
        {
            RecordAnnotation anno = new RecordAnnotation();
            anno.Read(br);
            return anno;
        }

        private static RecordCoordinate ReadCoordinate(BinaryReader br)
        {
            RecordCoordinate coordinate = new RecordCoordinate();
            coordinate.Read(br);
            return coordinate;
        }
    }
}