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
    internal class ShapeFileIndex
    {
        private const int eachRecordLength = 8;
        private const int fileHeaderLength = 100;
        private const int cacheSize = 512;
        [Obfuscation(Exclude = true)]
        private string shxPathFileName;
        [Obfuscation(Exclude = true)]
        private ShapeFileHeader fileHeader;
        [NonSerialized]
        private Stream shxStream;
        [Obfuscation(Exclude = true)]
        private long startIndex;
        [Obfuscation(Exclude = true)]
        private long endIndex;
        [Obfuscation(Exclude = true)]
        private byte[] cache;
        [Obfuscation(Exclude = true)]
        private bool isOpen;

        public ShapeFileIndex()
            : this(string.Empty)
        { }

        public ShapeFileIndex(string shxPathFileName)
        {
            this.shxPathFileName = shxPathFileName;
            fileHeader = new ShapeFileHeader();
        }

        public string ShxPathFileName
        {
            get { return shxPathFileName; }
            set { shxPathFileName = value; }
        }

        public ShapeFileHeader FileHeader
        {
            get { return fileHeader; }
            set { fileHeader = value; }
        }

        public void Open()
        {
            Open(FileAccess.Read);
        }

        /// <summary>
        /// Open Files
        /// </summary>
        /// <param name="fileAccess">the name of the Shx file</param>
        public void Open(FileAccess fileAccess)
        {
            shxStream = new FileStream(shxPathFileName, FileMode.Open, fileAccess, FileShare.ReadWrite);
            isOpen = true;
        }

        /// <summary>
        /// Close all the Files
        /// </summary>
        public void Close()
        {
            if (isOpen && shxStream != null)
            {
                shxStream.Close();
                shxStream = null;
            }
            cache = null;
            startIndex = 0;
            endIndex = 0;
            isOpen = false;
        }

        public int GetRecordCount()
        {
            return (int)((shxStream.Length - fileHeaderLength) / eachRecordLength);
        }

        public int GetRecordOffset(int recordIndex)
        {
            int recordPosition = (recordIndex - 1) * eachRecordLength + fileHeaderLength;
            if (recordPosition + 4 >= endIndex || recordPosition < startIndex)
            {
                startIndex = recordPosition;
                endIndex = startIndex + cacheSize;
                cache = new byte[cacheSize];
                shxStream.Seek(startIndex, SeekOrigin.Begin);
                shxStream.Read(cache, 0, cacheSize);
            }

            int position = (int)(recordPosition - startIndex);
            int offset = (((cache[position] << 0x18) | (cache[position + 1] << 0x10)) | (cache[position + 2] << 8)) | cache[position + 3];

            return offset * 2;
        }

        public int GetRecordContentLength(int recordIndex)
        {
            int recordPosition = (recordIndex - 1) * eachRecordLength + fileHeaderLength;
            int recordContentPosition = recordPosition + 4;

            if (recordContentPosition + 4 >= endIndex || recordContentPosition < startIndex)
            {
                startIndex = recordContentPosition;
                endIndex = startIndex + cacheSize;
                cache = new byte[cacheSize];
                shxStream.Seek(startIndex, SeekOrigin.Begin);
                shxStream.Read(cache, 0, cacheSize);
            }

            int position = (int)(recordContentPosition - startIndex);
            int offset = (((cache[position] << 0x18) | (cache[position + 1] << 0x10)) | (cache[position + 2] << 8)) | cache[position + 3];

            return offset * 2;
        }

        public void UpdateRecord(int recordIndex, int offset, int contentLength)
        {
            offset = offset / 2;
            WriteRecord(recordIndex, offset, contentLength);
        }

        public void AddRecord(int offset, int contentLength)
        {
            offset = offset / 2;

            WriteRecord(GetRecordCount() + 1, offset, contentLength);
        }

        private void WriteRecord(int recordIndex, int offset, int contentLength)
        {
            shxStream.Seek(fileHeaderLength + (recordIndex - 1) * 8, SeekOrigin.Begin);

            IOUtil.WriteIntToStream(offset, shxStream, WkbByteOrder.BigEndian);

            IOUtil.WriteIntToStream(contentLength, shxStream, WkbByteOrder.BigEndian);
        }

        public void DeleteRecord(int recordIndex)
        {
            int recordStartPosition = (recordIndex - 1) * eachRecordLength + fileHeaderLength;

            shxStream.Seek(recordStartPosition + 4, SeekOrigin.Begin);

            IOUtil.WriteIntToStream(0, shxStream, WkbByteOrder.BigEndian);

            startIndex = 0;
            endIndex = 0;
        }

        public void Flush()
        {
            fileHeader.WriteToStream(shxStream);
            shxStream.Flush();
        }
    }
}
