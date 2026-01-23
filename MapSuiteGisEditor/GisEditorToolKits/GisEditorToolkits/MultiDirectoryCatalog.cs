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
                    if (IsMismatchedArchitecture(file)) continue;
                    if (!IsManagedAssembly(file)) continue;

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
                    catch (ReflectionTypeLoadException)
                    {
                        // Skip catalogs that fail to load due to missing dependencies or bitness.
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
            try
            {
                AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsMismatchedArchitecture(string path)
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

            try
            {
                if (!File.Exists(path))
                    return false;

                var name = AssemblyName.GetAssemblyName(path);
                if (Environment.Is64BitProcess && name.ProcessorArchitecture == ProcessorArchitecture.X86)
                {
                    return true;
                }
                if (!Environment.Is64BitProcess && name.ProcessorArchitecture == ProcessorArchitecture.Amd64)
                {
                    return true;
                }
            }
            catch
            {
                // Ignore non-managed assemblies; IsManagedAssembly handles them.
            }

            return false;
        }
    }
}
