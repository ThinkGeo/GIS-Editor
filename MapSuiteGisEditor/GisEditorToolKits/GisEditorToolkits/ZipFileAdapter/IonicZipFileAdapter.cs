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
using System.IO;
using Ionic.Zip;

namespace ThinkGeo.MapSuite.GisEditor
{
    public class IonicZipFileAdapter : ZipFileAdapter
    {
        private ZipFile zipFile;

        public IonicZipFileAdapter()
        {
            zipFile = new ZipFile();
        }

        public IonicZipFileAdapter(string fileName)
        {
            zipFile = new ZipFile(fileName);
        }

        public IonicZipFileAdapter(Stream fileStream)
        {
            zipFile = ZipFile.Read(fileStream);
        }

        public override Stream GetEntryStreamByName(string name)
        {
            Stream result = null;

            if (zipFile.ContainsEntry(name))
            {
                result = zipFile[name].OpenReader();
            }

            return result;
        }

        public override IEnumerable<string> GetEntryNames()
        {
            return zipFile.EntryFileNames;
        }

        public static bool IsZipFile(string fileName)
        {
            return ZipFile.IsZipFile(fileName);
        }

        public override void AddEntity(string key, MemoryStream value)
        {
            zipFile.AddEntry(key, value);
        }

        public override void AddFileToZipFile(string fileName)
        {
            zipFile.AddFile(fileName);
        }

        public override void AddFileToZipFile(string fileName, string directoryPathInArchive)
        {
            zipFile.AddFile(fileName, directoryPathInArchive);
        }

        public override void AddDirectoryToZipFile(string directoryName)
        {
            zipFile.AddDirectory(directoryName);
        }

        public override void AddDirectoryToZipFile(string directoryName, string directoryPathInArchive)
        {
            zipFile.AddDirectory(directoryName, directoryPathInArchive);
        }

        public override void SetFileName(string entryName, string fileName)
        {
            zipFile[entryName].FileName = fileName;
        }

        public override void ExtractEntryToStream(string fileName, Stream stream)
        {
            zipFile[fileName].Extract(stream);
        }

        public override void ExtractEntryByName(string fileName, string path)
        {
            zipFile[fileName].Extract(path, ExtractExistingFileAction.OverwriteSilently);
        }

        public override void ExtractAll(string path)
        {
            zipFile.ExtractAll(path, ExtractExistingFileAction.OverwriteSilently);
        }

        public override void RemoveEntry(string entryName)
        {
            zipFile.RemoveEntry(entryName);
        }

        public override void RemoveEntries(string []entryNames)
        {
            zipFile.RemoveEntries(entryNames);
        }

        public override void Save(string path)
        {
            string directoryName = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

            zipFile.Save(path);
        }

        public override void Save(Stream stream)
        {
            zipFile.Save(stream);
        }

        public override void Dispose()
        {
            if (zipFile != null) zipFile.Dispose();
        }
    }
}
