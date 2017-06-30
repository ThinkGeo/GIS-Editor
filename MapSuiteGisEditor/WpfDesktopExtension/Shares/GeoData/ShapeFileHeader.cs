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
using System.Reflection;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    internal class ShapeFileHeader
    {
        private const int fileVersionNumber = 1000;
        private const long fileLengthInfoPosition = 24;
        private const int boundingBoxStartPostion = 36;
        private const int dobleLength = 8;
        private const int intLength = 4;
        [Obfuscation(Exclude = true)]
        private int fileCode;
        [Obfuscation(Exclude = true)]
        private int fileLength;
        [Obfuscation(Exclude = true)]
        private int version;
        [Obfuscation(Exclude = true)]
        private ShapeFileBoundingBox boundingBox;
        [Obfuscation(Exclude = true)]
        private ShapeFileType shapeFileType;

        public ShapeFileHeader()
            : this(ShapeFileType.Null)
        {
        }

        public ShapeFileHeader(ShapeFileType shapeFileType)
        {
            fileCode = 9994;
            version = fileVersionNumber;
            this.shapeFileType = shapeFileType;
            boundingBox = new ShapeFileBoundingBox();
        }

        /// <summary>
        /// This variable is in 16-bit decimalDegreesValue.
        /// </summary>
        public int FileLength
        {
            get { return fileLength; }
            set { fileLength = value; }
        }

        public ShapeFileType ShapeFileType
        {
            get { return shapeFileType; }
            set { shapeFileType = value; }
        }

        public ShapeFileBoundingBox BoundingBox
        {
            get { return boundingBox; }
            set { boundingBox = value; }
        }

        public static ShapeFileHeader GetHeaderFromStream(Stream stream)
        {
            ShapeFileHeader returnHeader = new ShapeFileHeader();

            stream.Seek(0, SeekOrigin.Begin);

            returnHeader.fileCode = IOUtil.ReadIntFromStream(stream, WkbByteOrder.BigEndian);

            stream.Seek(fileLengthInfoPosition, SeekOrigin.Begin);
            returnHeader.fileLength = IOUtil.ReadIntFromStream(stream, WkbByteOrder.BigEndian);


            returnHeader.version = IOUtil.ReadIntFromStream(stream, WkbByteOrder.LittleEndian);

            returnHeader.shapeFileType = ConvertIntToShapeFileType(IOUtil.ReadIntFromStream(stream, WkbByteOrder.LittleEndian));

            returnHeader.boundingBox = ShapeFileBoundingBox.GetHeaderBoundingBox(stream);

            return returnHeader;
        }

        public void WriteToStream(Stream targetFileStream)
        {
            targetFileStream.Seek(0, SeekOrigin.Begin);
            IOUtil.WriteIntToStream(fileCode, targetFileStream, WkbByteOrder.BigEndian);

            targetFileStream.Seek(fileLengthInfoPosition, SeekOrigin.Begin);
            int tempFileLength = (int)targetFileStream.Length / 2;
            IOUtil.WriteIntToStream(tempFileLength, targetFileStream, WkbByteOrder.BigEndian);

            IOUtil.WriteIntToStream(version, targetFileStream, WkbByteOrder.LittleEndian);

            int typeNumber = GetShapeFileTypeNumber(shapeFileType);
            IOUtil.WriteIntToStream(typeNumber, targetFileStream, WkbByteOrder.LittleEndian);

            boundingBox.WriteBoundingBox(targetFileStream, true);
        }

        public static ShapeFileType ConvertIntToShapeFileType(int value)
        {
            switch (value)
            {
                case 0: return ShapeFileType.Null;
                case 1: return ShapeFileType.Point;
                case 3: return ShapeFileType.Polyline;
                case 5: return ShapeFileType.Polygon;
                case 8: return ShapeFileType.Multipoint;
                case 11: return ShapeFileType.PointZ;
                case 13: return ShapeFileType.PolylineZ;
                case 15: return ShapeFileType.PolygonZ;
                case 18: return ShapeFileType.MultipointZ;
                case 21: return ShapeFileType.PointM;
                case 23: return ShapeFileType.PolylineM;
                case 25: return ShapeFileType.PolygonM;
                case 28: return ShapeFileType.MultipointM;
                case 31: return ShapeFileType.Multipatch;
                default: return ShapeFileType.Null;
            }
        }

        private static int GetShapeFileTypeNumber(ShapeFileType shapeFileType)
        {
            switch (shapeFileType)
            {
                case ShapeFileType.Null: return 0;
                case ShapeFileType.Point: return 1;
                case ShapeFileType.Polyline: return 3;
                case ShapeFileType.Polygon: return 5;
                case ShapeFileType.Multipoint: return 8;
                case ShapeFileType.PointZ: return 11;
                case ShapeFileType.PolylineZ: return 13;
                case ShapeFileType.PolygonZ: return 15;
                case ShapeFileType.MultipointZ: return 18;
                case ShapeFileType.PointM: return 21;
                case ShapeFileType.PolylineM: return 23;
                case ShapeFileType.PolygonM: return 25;
                case ShapeFileType.MultipointM: return 28;
                case ShapeFileType.Multipatch: return 31;
                default: return 0;
            }
        }
    }
}
