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
using System.IO;

namespace ThinkGeo.MapSuite.GisEditor
{
    public abstract class ZipFileAdapter :IDisposable
    {
        protected ZipFileAdapter() { }

        public abstract IEnumerable<string> GetEntryNames();
        public abstract Stream GetEntryStreamByName(string name);

        public abstract void AddEntity(string key, MemoryStream value);
        public abstract void AddFileToZipFile(string fileName);
        public abstract void AddFileToZipFile(string fileName, string directoryPathInArchive);
        public abstract void AddDirectoryToZipFile(string directoryName);
        public abstract void AddDirectoryToZipFile(string directoryName, string directoryInArchive);
        public abstract void SetFileName(string entryName,string fileName  );

        public abstract void ExtractEntryToStream(string fileName, Stream stream);
        public abstract void ExtractEntryByName(string fileName, string path);
        public abstract void ExtractAll(string path);

        public abstract void RemoveEntry(string entryName);
        public abstract void RemoveEntries(string[] entryNames);
        public abstract void Save(string path);
        public abstract void Save(Stream stream);
        public abstract void Dispose();
    }
}
