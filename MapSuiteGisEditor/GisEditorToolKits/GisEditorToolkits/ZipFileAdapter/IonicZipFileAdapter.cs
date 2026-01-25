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
using System.IO.Compression;
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor
{
    public class IonicZipFileAdapter : ZipFileAdapter
    {
        private ZipArchive zipArchive;
        private Stream backingStream;
        private readonly bool ownsStream;
        private readonly string backingFilePath;

        public IonicZipFileAdapter()
        {
            backingStream = new MemoryStream();
            zipArchive = new ZipArchive(backingStream, ZipArchiveMode.Update, true);
            ownsStream = true;
        }

        public IonicZipFileAdapter(string fileName)
        {
            backingFilePath = fileName;
            backingStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
            zipArchive = new ZipArchive(backingStream, ZipArchiveMode.Update, true);
            ownsStream = true;
        }

        public IonicZipFileAdapter(Stream fileStream)
        {
            backingStream = fileStream;
            zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Update, true);
            ownsStream = false;
        }

        public override Stream GetEntryStreamByName(string name)
        {
            EnsureArchiveOpen();
            var entry = zipArchive.GetEntry(name);
            return entry?.Open();
        }

        public override IEnumerable<string> GetEntryNames()
        {
            EnsureArchiveOpen();
            return zipArchive.Entries.Select(entry => entry.FullName).ToList();
        }

        public static bool IsZipFile(string fileName)
        {
            try
            {
                using (ZipFile.OpenRead(fileName)) { }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void AddEntity(string key, MemoryStream value)
        {
            EnsureArchiveOpen();
            RemoveEntryIfExists(key);

            value.Position = 0;
            var entry = zipArchive.CreateEntry(NormalizeEntryPath(key), CompressionLevel.Optimal);
            using (var entryStream = entry.Open())
            {
                value.CopyTo(entryStream);
            }
        }

        public override void AddFileToZipFile(string fileName)
        {
            AddFileToZipFile(fileName, string.Empty);
        }

        public override void AddFileToZipFile(string fileName, string directoryPathInArchive)
        {
            EnsureArchiveOpen();
            var entryName = Path.GetFileName(fileName);
            if (!string.IsNullOrEmpty(directoryPathInArchive))
            {
                entryName = Path.Combine(directoryPathInArchive, entryName);
            }
            entryName = NormalizeEntryPath(entryName);
            RemoveEntryIfExists(entryName);

            var entry = zipArchive.CreateEntry(entryName, CompressionLevel.Optimal);
            using (var entryStream = entry.Open())
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileStream.CopyTo(entryStream);
            }
        }

        public override void AddDirectoryToZipFile(string directoryName)
        {
            AddDirectoryToZipFile(directoryName, string.Empty);
        }

        public override void AddDirectoryToZipFile(string directoryName, string directoryPathInArchive)
        {
            EnsureArchiveOpen();
            if (!Directory.Exists(directoryName)) return;

            var basePath = Path.GetFullPath(directoryName);
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                basePath += Path.DirectorySeparatorChar;
            }

            foreach (var filePath in Directory.EnumerateFiles(directoryName, "*", SearchOption.AllDirectories))
            {
                var fullPath = Path.GetFullPath(filePath);
                var relativePath = fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)
                    ? fullPath.Substring(basePath.Length)
                    : Path.GetFileName(fullPath);
                var entryName = string.IsNullOrEmpty(directoryPathInArchive)
                    ? relativePath
                    : Path.Combine(directoryPathInArchive, relativePath);
                AddFileToZipFile(filePath, Path.GetDirectoryName(entryName) ?? string.Empty);
            }
        }

        public override void SetFileName(string entryName, string fileName)
        {
            EnsureArchiveOpen();
            var normalizedEntryName = NormalizeEntryPath(entryName);
            var entry = zipArchive.GetEntry(normalizedEntryName);
            if (entry == null) return;

            var newEntryName = NormalizeEntryPath(fileName);
            if (string.Equals(entry.FullName, newEntryName, StringComparison.OrdinalIgnoreCase)) return;

            RemoveEntryIfExists(newEntryName);

            var newEntry = zipArchive.CreateEntry(newEntryName, CompressionLevel.Optimal);
            newEntry.LastWriteTime = entry.LastWriteTime;
            using (var sourceStream = entry.Open())
            using (var targetStream = newEntry.Open())
            {
                sourceStream.CopyTo(targetStream);
            }
            entry.Delete();
        }

        public override void ExtractEntryToStream(string fileName, Stream stream)
        {
            EnsureArchiveOpen();
            var entry = zipArchive.GetEntry(fileName);
            if (entry == null) return;

            using (var entryStream = entry.Open())
            {
                entryStream.CopyTo(stream);
            }
        }

        public override void ExtractEntryByName(string fileName, string path)
        {
            EnsureArchiveOpen();
            var entry = zipArchive.GetEntry(fileName);
            if (entry == null) return;

            var targetPath = Directory.Exists(path) ? Path.Combine(path, entry.FullName) : path;
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            entry.ExtractToFile(targetPath, true);
        }

        public override void ExtractAll(string path)
        {
            EnsureArchiveOpen();
            Directory.CreateDirectory(path);

            foreach (var entry in zipArchive.Entries)
            {
                var targetPath = Path.Combine(path, entry.FullName);
                if (entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
                    entry.FullName.EndsWith("\\", StringComparison.Ordinal))
                {
                    Directory.CreateDirectory(targetPath);
                    continue;
                }

                var directory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                entry.ExtractToFile(targetPath, true);
            }
        }

        public override void RemoveEntry(string entryName)
        {
            EnsureArchiveOpen();
            var entry = zipArchive.GetEntry(entryName);
            if (entry != null) entry.Delete();
        }

        public override void RemoveEntries(string []entryNames)
        {
            EnsureArchiveOpen();
            foreach (var entryName in entryNames)
            {
                RemoveEntry(entryName);
            }
        }

        public override void Save(string path)
        {
            EnsureArchiveOpen();
            var directoryName = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            FinalizeArchive();

            if (backingStream is MemoryStream memoryStream)
            {
                memoryStream.Position = 0;
                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    memoryStream.CopyTo(fileStream);
                }
                return;
            }

            if (backingStream is FileStream && !string.IsNullOrEmpty(backingFilePath) && !string.Equals(backingFilePath, path, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(backingFilePath, path, true);
            }
        }

        public override void Save(Stream stream)
        {
            EnsureArchiveOpen();
            FinalizeArchive();

            if (backingStream is MemoryStream memoryStream)
            {
                memoryStream.Position = 0;
                memoryStream.CopyTo(stream);
                return;
            }

            if (!string.IsNullOrEmpty(backingFilePath))
            {
                using (var fileStream = new FileStream(backingFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    fileStream.CopyTo(stream);
                }
            }
        }

        public override void Dispose()
        {
            if (zipArchive != null)
            {
                zipArchive.Dispose();
                zipArchive = null;
            }

            if (ownsStream && backingStream != null)
            {
                backingStream.Dispose();
                backingStream = null;
            }
        }

        private void EnsureArchiveOpen()
        {
            if (zipArchive == null) throw new ObjectDisposedException(nameof(IonicZipFileAdapter));
        }

        private void FinalizeArchive()
        {
            if (zipArchive != null)
            {
                zipArchive.Dispose();
                zipArchive = null;
            }
        }

        private void RemoveEntryIfExists(string entryName)
        {
            var entry = zipArchive.GetEntry(NormalizeEntryPath(entryName));
            if (entry != null) entry.Delete();
        }

        private static string NormalizeEntryPath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
