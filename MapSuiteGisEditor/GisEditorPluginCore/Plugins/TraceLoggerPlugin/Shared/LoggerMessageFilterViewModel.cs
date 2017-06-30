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
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class LoggerMessageFilterViewModel : ViewModelBase
    {
        private static string lastCategory = "All";
        private static string lastLevel = "All";
        private static string lastInclude = string.Empty;
        private static string lastExclude = string.Empty;

        private Collection<string> allLevels;
        private string category;
        private string level;
        private string include;
        private string exclude;
        private static Collection<string> categories;

        public LoggerMessageFilterViewModel()
        {
            category = lastCategory;
            level = lastLevel;
            include = lastInclude;
            exclude = lastExclude;
            allLevels = new Collection<string>();
            categories = new Collection<string>();
            foreach (string item in Enum.GetNames(typeof(LoggerLevel)))
            {
                allLevels.Add(item);
            }
            allLevels.Add("All");
        }

        public Collection<string> Categories
        {
            get { return categories; }
        }

        public Collection<string> AllLevels
        {
            get { return allLevels; }
        }

        public string Category
        {
            get { return category; }
            set
            {
                lastCategory = value;
                category = value;
                RaisePropertyChanged(()=>Category);
            }
        }

        public string Level
        {
            get { return level; }
            set
            {
                lastLevel = value;
                level = value;
                RaisePropertyChanged(()=>Level);
            }
        }

        public string Include
        {
            get { return include; }
            set
            {
                lastInclude = value;
                include = value;
                RaisePropertyChanged(()=>Include);
            }
        }

        public string Exclude
        {
            get { return exclude; }
            set
            {
                lastExclude = value;
                exclude = value;
                RaisePropertyChanged(()=>Exclude);
            }
        }

        public static void Load(Collection<LoggerMessageViewModel> loggerMessages)
        {
            foreach (var item in loggerMessages.Take(500))
            {
                if (!categories.Contains(item.LoggerMessage.Category))
                    categories.Add(item.LoggerMessage.Category);
            }
            if (!categories.Contains("All"))
            {
                categories.Add("All");
            }
        }
    }
}
