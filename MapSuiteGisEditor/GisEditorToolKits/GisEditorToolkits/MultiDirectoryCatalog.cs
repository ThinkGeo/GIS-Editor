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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public class MultiDirectoryCatalog : ComposablePartCatalog
    {
        private string searchPattern;
        private Collection<string> directories;
        private Collection<DirectoryCatalog> directoryCatalogs;

        public MultiDirectoryCatalog(IEnumerable<string> directories, string searchPattern = "*.dll")
        {
            this.searchPattern = searchPattern;
            this.directories = new ObservableCollection<string>();
            directoryCatalogs = new Collection<DirectoryCatalog>();

            foreach (string directory in directories)
            {
                this.directories.Add(directory);
            }

            var directoriesToScan = directories.Concat(directories.Where(d => Directory.Exists(d))
                .SelectMany(d => Directory.GetDirectories(d, "*", SearchOption.AllDirectories)))
                .Distinct().ToArray();

            foreach (var directory in directoriesToScan)
            {
                if (Directory.Exists(directory) && Directory.GetFiles(directory, "*.dll").Length > 0)
                {
                    DirectoryCatalog catalog = new DirectoryCatalog(directory, searchPattern);
                    directoryCatalogs.Add(catalog);
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
                return directoryCatalogs.SelectMany(catalog => catalog.Parts).AsQueryable();
            }
        }

        public void Refresh()
        {
            foreach (DirectoryCatalog catalog in directoryCatalogs)
            {
                catalog.Refresh();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (DirectoryCatalog catalog in directoryCatalogs)
                {
                    catalog.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}