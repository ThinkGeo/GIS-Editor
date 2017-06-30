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
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class IconHelper
    {
        private readonly static string iconsFolderName = "Images/CustomIcons";
        private readonly static string iconsRootPattern = "Images/pointstyle";

        private static IconCategory[] cachedData = null;

        public static IconCategory[] GetIconCategories()
        {
            if (cachedData == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourcesName = assembly.GetName().Name + ".g";
                var manager = new ResourceManager(resourcesName, assembly);
                var resourceSet = manager.GetResourceSet(CultureInfo.InvariantCulture, true, true)
                                  .OfType<DictionaryEntry>()
                                  .Where(entry =>
                                  {
                                      string key = entry.Key.ToString();
                                      return key.StartsWith(iconsRootPattern, StringComparison.InvariantCultureIgnoreCase) && key.EndsWith("png") && key.Split('/').Length > 3;
                                  });

                var iconsFolderPath = Path.Combine(FolderHelper.GetGisEditorFolder(), iconsFolderName);

                if (Directory.Exists(iconsFolderPath))
                {
                    var customSet = new Collection<DictionaryEntry>();

                    foreach (var categroy in Directory.GetDirectories(iconsFolderPath, "*", SearchOption.TopDirectoryOnly))
                    {
                        foreach (var file in Directory.GetFiles(categroy, "*.png", SearchOption.TopDirectoryOnly))
                        {
                            var fileName = file.Replace(iconsFolderPath, string.Empty).Replace("\\", "/");
                            fileName = iconsRootPattern + fileName;
                            customSet.Add(new DictionaryEntry(fileName, new MemoryStream(File.ReadAllBytes(file))));
                        }
                    }

                    foreach (var file in Directory.GetFiles(iconsFolderPath, "*.png", SearchOption.TopDirectoryOnly))
                    {
                        var fileName = file.Replace(iconsFolderPath, string.Empty).Replace("\\", "/");
                        fileName = iconsRootPattern + "/unsorted" + fileName;
                        customSet.Add(new DictionaryEntry(fileName, new MemoryStream(File.ReadAllBytes(file))));
                    }

                    resourceSet = resourceSet.Union(customSet);
                }

                var groupsByCatName = resourceSet
                                      .GroupBy(entry => GetSubCatName((string)entry.Key));

                var categories = groupsByCatName.Select(group => new IconCategory(group));

                cachedData = categories.OrderBy(cat => cat.CategoryName).ToArray();
            }

            return cachedData;
        }

        private static string GetCategoryName(string wholeKey)
        {
            var parts = wholeKey.Split('/');
            return parts[1];
        }

        private static string GetSubCatName(string wholeKey)
        {
            return wholeKey.Split('/')[2];
        }
    }
}