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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public class MultiDirectoryCatalog : ComposablePartCatalog
    {
        private string searchPattern;
        private Collection<string> directories;
        private Collection<ComposablePartCatalog> catalogs;

        public MultiDirectoryCatalog(IEnumerable<string> directories, string searchPattern = "*.dll")
        {
            this.searchPattern = searchPattern;
            this.directories = new ObservableCollection<string>();
            catalogs = new Collection<ComposablePartCatalog>();

            foreach (string directory in directories)
            {
                this.directories.Add(directory);
            }

            var directoriesToScan = directories.Concat(directories.Where(d => Directory.Exists(d))
                .SelectMany(d => Directory.GetDirectories(d, "*", SearchOption.AllDirectories)))
                .Distinct().ToArray();

            foreach (var directory in directoriesToScan)
            {
                if (!Directory.Exists(directory)) continue;

                foreach (var file in Directory.GetFiles(directory, searchPattern))
                {
                    if (!IsManagedAssembly(file)) continue;
                    if (IsMismatchedArchitecture(file)) continue;
                    if (!IsLikelyPluginAssembly(file)) continue;

                    if (file.Contains("FileGDBAPI"))
                        continue;
                    try
                    {
                        catalogs.Add(new AssemblyCatalog(file));
                    }
                    catch (BadImageFormatException)
                    {
                        // Skip native or incompatible assemblies.
                    }
                    catch (FileLoadException)
                    {
                        // Skip assemblies that cannot be loaded in this process.
                    }
                }
            }
        }

        public Collection<string> Directories
        {
            get { return directories; }
        }

        public string SearchPattern
        {
            get { return searchPattern; }
        }

        public override IQueryable<ComposablePartDefinition> Parts
        {
            get
            {
                var parts = new List<ComposablePartDefinition>();
                foreach (var catalog in catalogs)
                {
                    try
                    {
                        parts.AddRange(catalog.Parts);
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        // Fall back to the types that did load so core plugins still compose.
                        var safeTypes = ex.Types == null ? Array.Empty<Type>() : ex.Types.Where(t => t != null).ToArray();
                        if (safeTypes.Length > 0)
                        {
                            try
                            {
                                parts.AddRange(new TypeCatalog(safeTypes).Parts);
                            }
                            catch (CompositionException)
                            {
                                // Skip catalogs that still cannot be composed.
                            }
                        }
                    }
                    catch (CompositionException)
                    {
                        // Skip catalogs that cannot be composed.
                    }
                }

                return parts.AsQueryable();
            }
        }

        public void Refresh()
        {
            foreach (var catalog in catalogs.OfType<DirectoryCatalog>())
            {
                catalog.Refresh();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var catalog in catalogs)
                {
                    catalog.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        private static bool IsManagedAssembly(string path)
        {
            return IsManagedPeFile(path);
        }

        private static bool IsLikelyPluginAssembly(string path)
        {
            var fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            // Limit MEF scanning to probable plugin assemblies to avoid reflecting over
            // large dependency graphs and native/3rd-party libraries.
            return fileName.IndexOf("Plugin", StringComparison.OrdinalIgnoreCase) >= 0
                || fileName.IndexOf("GisEditor", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMismatchedArchitecture(string path)
        {
            if (!TryGetAssemblyName(path, out var name))
            {
                if (Environment.Is64BitProcess)
                {
                    if (path.IndexOf("Windows-X86", StringComparison.OrdinalIgnoreCase) >= 0
                        || path.IndexOf("win-x86", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
                else
                {
                    if (path.IndexOf("Windows-X64", StringComparison.OrdinalIgnoreCase) >= 0
                        || path.IndexOf("win-x64", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            if (Environment.Is64BitProcess && name.ProcessorArchitecture == ProcessorArchitecture.X86)
            {
                return true;
            }
            if (!Environment.Is64BitProcess && name.ProcessorArchitecture == ProcessorArchitecture.Amd64)
            {
                return true;
            }

            return false;
        }

        private static bool TryGetAssemblyName(string path, out AssemblyName name)
        {
            name = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                if (!IsManagedPeFile(path))
                {
                    return false;
                }

                name = AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
            catch (FileLoadException)
            {
                return false;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        private static bool IsManagedPeFile(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new BinaryReader(stream))
                {
                    if (stream.Length < 0x40)
                    {
                        return false;
                    }

                    // DOS header "MZ"
                    if (reader.ReadUInt16() != 0x5A4D)
                    {
                        return false;
                    }

                    stream.Seek(0x3C, SeekOrigin.Begin);
                    int peOffset = reader.ReadInt32();
                    if (peOffset <= 0 || peOffset > stream.Length - 4)
                    {
                        return false;
                    }

                    stream.Seek(peOffset, SeekOrigin.Begin);
                    if (reader.ReadUInt32() != 0x00004550) // "PE\0\0"
                    {
                        return false;
                    }

                    // Skip COFF header (20 bytes)
                    stream.Seek(20, SeekOrigin.Current);
                    long optionalHeaderStart = stream.Position;
                    ushort magic = reader.ReadUInt16();

                    // PE32 = 0x10B, PE32+ = 0x20B
                    int dataDirectoryOffset;
                    if (magic == 0x10B)
                    {
                        dataDirectoryOffset = 96;
                    }
                    else if (magic == 0x20B)
                    {
                        dataDirectoryOffset = 112;
                    }
                    else
                    {
                        return false;
                    }

                    stream.Seek(optionalHeaderStart + dataDirectoryOffset, SeekOrigin.Begin);

                    // DataDirectory[14] = CLI header
                    stream.Seek(14 * 8, SeekOrigin.Current);
                    uint cliRva = reader.ReadUInt32();
                    uint cliSize = reader.ReadUInt32();

                    return cliRva != 0 && cliSize != 0;
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
