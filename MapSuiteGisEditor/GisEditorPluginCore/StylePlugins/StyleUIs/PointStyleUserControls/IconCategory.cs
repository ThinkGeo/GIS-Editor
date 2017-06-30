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
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class IconCategory
    {
        private string categoryName;
        private ObservableCollection<IconCategory> subCategories;
        private bool hasSubCategories;
        private ObservableCollection<IconEntity> icons;

        public IconCategory(IGrouping<string, DictionaryEntry> group)
        {
            this.categoryName = group.Key;

            bool hasSub = HasSub(group);

            //this category has no sub categories
            if (!hasSub)
            {
                icons = new ObservableCollection<IconEntity>(group
                                .Select(entry => GetIconFromStream(entry))
                                .OrderBy(entity => entity.IconName));
            }
            //this category has some sub categories
            else
            {
                var subGroups = group.GroupBy(entry => GetSubCatName(entry.Key.ToString()));
                subCategories = new ObservableCollection<IconCategory>(subGroups
                                .Select(subGroup => new IconCategory(subGroup))
                                .OrderBy(subCat => subCat.CategoryName));
            }
            hasSubCategories = hasSub;
        }

        public string CategoryName
        {
            get { return categoryName; }
        }

        public ObservableCollection<IconCategory> SubCategories
        {
            get { return subCategories; }
        }

        public bool HasSubCategories
        {
            get { return hasSubCategories; }
        }

        public ObservableCollection<IconEntity> Icons
        {
            get { return icons; }
        }

        private string GetSubCatName(string wholeKey)
        {
            return wholeKey.Split('/')[2];
        }

        private bool HasSub(IGrouping<string, DictionaryEntry> group)
        {
            var entries = group.ToArray();
            bool hasFour = entries.Any(entry => entry.Key.ToString().Split('/').Length == 4);
            string thirdName = entries.First().Key.ToString().Split('/')[2];
            bool allThirdSame = entries.All(entry => entry.Key.ToString().Split('/')[2] == thirdName);

            return hasFour && !allThirdSame;
        }

        private IconEntity GetIconFromStream(DictionaryEntry entry)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = (Stream)entry.Value;
            bitmap.EndInit();

            return new IconEntity { Icon = bitmap, IconName = Path.GetFileName(entry.Key.ToString()) };
        }
    }
}