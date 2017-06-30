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


//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using SharpCompress.Archive;
//using SharpCompress.Compressor;
//using SharpCompress.Archive.Zip;
//using SharpCompress.Writer.Zip;

//namespace ThinkGeo.MapSuite.GisEditor
//{
//    public class SharpCompressZipFileAdapter : ZipFileAdapter
//    {
//        private ZipArchive zipArchive;

//        public SharpCompressZipFileAdapter()
//        {
//            zipArchive = ZipArchive.Create();
//        }

//        public SharpCompressZipFileAdapter(string fileName)
//        {
//            zipArchive = ZipArchive.Open(fileName);
//        }

//        public SharpCompressZipFileAdapter(Stream fileStream)
//        {
//            zipArchive = ZipArchive.Open(fileStream);
//        }

//        public override Stream GetEntryStreamByName(string name)
//        {
//            MemoryStream result = new MemoryStream();

//            var entry = zipArchive.Entries.Where(z=>z.FilePath.EndsWith(name)).FirstOrDefault();

//            if(entry!=null)
//            {
//                entry.WriteTo(result);
//            }

//            result.Seek(0, SeekOrigin.Begin);

//            return result;
//        }

//        public override IEnumerable<string> GetEntryNames()
//        {
//            string[] entryNames = new string[zipArchive.Entries.Count];

//            var index = 0;
//            foreach (var entry in zipArchive.Entries)
//            {
//                entryNames[index] = entry.FilePath;
//                index++;
//            }

//            return entryNames;
//        }

//        public static bool IsZipFile(string fileName)
//        {
//            return ZipArchive.IsZipFile(fileName);
//        }

//        public override void AddEntity(string key, MemoryStream value)
//        {
//            zipArchive.AddEntry(key, value);
//        }

//        public override void AddFileToZipFile(string fileName)
//        {
//            zipArchive.AddEntry(Path.GetFileName(fileName), fileName);
//        }

//        public override void AddFileToZipFile(string fileName, string directoryPathInArchive)
//        {
//            if (string.IsNullOrEmpty(directoryPathInArchive)) directoryPathInArchive = Path.GetFileName(fileName);

//            zipArchive.AddEntry(directoryPathInArchive, fileName);
//        }

//        public override void AddDirectoryToZipFile(string directoryName)
//        {
//            zipArchive.AddAllFromDirectory(directoryName);
//        }

//        public override void AddDirectoryToZipFile(string directoryName, string directoryPathInArchive)
//        {
//            var path = Path.Combine(directoryName, directoryPathInArchive);
            
//            if (!Directory.Exists(path))
//            {
//                Directory.CreateDirectory(path);
//            }

//            string[] files = Directory.GetFiles(directoryName);

//            foreach (var file in files)
//            {
//                File.Move(file, Path.Combine(path,Path.GetFileName(file)));
//                File.SetAttributes(path, FileAttributes.Normal);
//            }

//            var entry = zipArchive.Entries.FirstOrDefault();

//            if (entry != null)
//            {
//                entry.WriteToDirectory(directoryName);
//            }

//            zipArchive.Entries.Clear();

//            zipArchive.AddAllFromDirectory(directoryName);
//        }

//        public override void SetFileName(string entryName, string fileName)
//        {
//            var entry = zipArchive.Entries.Where(z => z.FilePath.EndsWith(entryName)).FirstOrDefault();

//            if (entry != null)
//            {
//                var stream = entry.OpenEntryStream();

//                var writer = new ZipWriter(stream, SharpCompress.Common.CompressionType.BZip2, string.Empty);
//                writer.Write(fileName, stream, System.DateTime.Now);
//            }
//        }

//        public override void ExtractEntryToStream(string fileName, Stream stream)
//        {
//            var entry = zipArchive.Entries.Where(z => z.FilePath.EndsWith(fileName)).FirstOrDefault();

//            if (entry != null)
//            {
//                entry.WriteTo(stream);
//            }
//        }

//        public override void ExtractEntryByName(string fileName, string path)
//        {
//            var entry = zipArchive.Entries.Where(z => z.FilePath.EndsWith(fileName)).FirstOrDefault();

//            if (entry != null)
//            {
//                if (!Directory.Exists(path))
//                {
//                    Directory.CreateDirectory(path);
//                }

//                entry.WriteToDirectory(path);
//            }
//        }

//        public override void ExtractAll(string path)
//        {
//            foreach (var entry in zipArchive.Entries)
//            {
//                if (!Directory.Exists(path))
//                {
//                    Directory.CreateDirectory(path);
//                }

//                entry.WriteToDirectory(path);
//            }
//        }

//        public override void RemoveEntry(string entryName)
//        {
//            var entry = zipArchive.Entries.Where(z => z.FilePath.EndsWith(entryName)).FirstOrDefault();

//            if (entry != null) zipArchive.RemoveEntry(entry);
//        }

//        public override void RemoveEntries(string[] entryNames)
//        {
//            foreach (var entryName in entryNames)
//            {
//                var entry = zipArchive.Entries.Where(z => z.FilePath.EndsWith(entryName)).FirstOrDefault();
//                if (entry != null) zipArchive.RemoveEntry(entry);
//            }
//        }

//        public override void Save(string path)
//        {
//            zipArchive.SaveTo(path, SharpCompress.Common.CompressionType.BZip2);
//        }

//        public override void Save(Stream stream)
//        {
//            zipArchive.SaveTo(stream, SharpCompress.Common.CompressionType.BZip2);
//        }

//        public override void Dispose()
//        {
//            if (zipArchive != null)
//            {
//                foreach (var entry in zipArchive.Entries)
//                {
//                    entry.OpenEntryStream().Dispose();
//                }

//                zipArchive.Dispose();
//            }
//        }
//    }
//}
