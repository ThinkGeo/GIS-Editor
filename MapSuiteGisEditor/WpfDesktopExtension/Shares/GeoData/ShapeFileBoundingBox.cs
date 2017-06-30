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
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    internal class ShapeFileBoundingBox
    {
        private const int boundingBoxStartPostion = 36;
        private const int dobleLength = 8;
        [Obfuscation(Exclude = true)]
        private double minX;
        [Obfuscation(Exclude = true)]
        private double maxX;
        [Obfuscation(Exclude = true)]
        private double minY;
        [Obfuscation(Exclude = true)]
        private double maxY;

        /// <summary>
        /// Constructor
        /// </summary>
        public ShapeFileBoundingBox()
        {
        }

        public double MinX
        {
            get { return minX; }
            set { minX = value; }
        }

        public double MaxX
        {
            get { return maxX; }
            set { maxX = value; }
        }

        public double MinY
        {
            get { return minY; }
            set { minY = value; }
        }

        public double MaxY
        {
            get { return maxY; }
            set { maxY = value; }
        }

        public static ShapeFileBoundingBox GetHeaderBoundingBox(Stream stream)
        {
            stream.Seek(boundingBoxStartPostion, SeekOrigin.Begin);

            ShapeFileBoundingBox returnBoundingBox = new ShapeFileBoundingBox();

            BinaryReader br = new BinaryReader(stream);
            returnBoundingBox.minX = br.ReadDouble();
            returnBoundingBox.minY = br.ReadDouble();
            returnBoundingBox.maxX = br.ReadDouble();
            returnBoundingBox.maxY = br.ReadDouble();

            return returnBoundingBox;
        }

        /// <summary>
        /// merge the input Bounding Box, store the result
        /// </summary>
        /// <param name="targetBox">the input BoundingBox to be merged</param>
        public void MergeBoundingBox(ShapeFileBoundingBox targetBox)
        {
            if (minX == 0 && minY == 0 && maxX == 0 && maxY == 0)
            {
                minX = targetBox.minX;
                maxX = targetBox.maxX;
                minY = targetBox.minY;
                maxY = targetBox.maxY;
            }
            else
            {
                minX = targetBox.MinX < minX ? targetBox.MinX : minX;
                maxX = targetBox.MaxX > maxX ? targetBox.MaxX : maxX;
                minY = targetBox.MinY < minY ? targetBox.MinY : minY;
                maxY = targetBox.MaxY > maxY ? targetBox.MaxY : maxY;
            }
        }

        public void WriteBoundingBox(Stream stream, bool isHeaderBoundingBox)
        {
            byte[] doubleByte = GetByteArrayFromDouble(minX, (byte)1);
            stream.Write(doubleByte, 0, dobleLength);

            doubleByte = GetByteArrayFromDouble(minY, (byte)1);
            stream.Write(doubleByte, 0, dobleLength);

            doubleByte = GetByteArrayFromDouble(maxX, (byte)1);
            stream.Write(doubleByte, 0, dobleLength);

            doubleByte = GetByteArrayFromDouble(maxY, (byte)1);
            stream.Write(doubleByte, 0, dobleLength);

            if (isHeaderBoundingBox)
            {
                doubleByte = GetByteArrayFromDouble(0, (byte)1);
                stream.Write(doubleByte, 0, dobleLength);

                doubleByte = GetByteArrayFromDouble(0, (byte)1);
                stream.Write(doubleByte, 0, dobleLength);

                doubleByte = GetByteArrayFromDouble(0, (byte)1);
                stream.Write(doubleByte, 0, dobleLength);

                doubleByte = GetByteArrayFromDouble(0, (byte)1);
                stream.Write(doubleByte, 0, dobleLength);
            }
        }

        private static byte[] GetByteArrayFromDouble(double doubleValue, byte byteOrder)
        {
            byte[] bytes = BitConverter.GetBytes(doubleValue);

            if (byteOrder == 0) 
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        internal RectangleShape GetRectangleShape()
        {
            return new RectangleShape(this.MinX, this.MaxY, this.MaxX, this.MinY);
        }
    }
}
