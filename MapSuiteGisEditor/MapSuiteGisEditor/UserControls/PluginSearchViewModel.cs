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
using System.Linq;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public class PluginSearchViewModel : ViewModelBase
    {
        private static readonly string tagFormat = "|~S~|{0}|~E~|";

        private string keyword;

        private ObservableCollection<PluginViewModel> results;

        private PluginViewModel selectedPlugin;

        public PluginSearchViewModel()
        {
            keyword = string.Empty;
            results = new ObservableCollection<PluginViewModel>();
            UpdatePlugins();
        }

        public ObservableCollection<PluginViewModel> Results
        {
            get { return results; }
        }

        public string Keyword
        {
            get { return keyword; }
            set
            {
                keyword = value;
                RaisePropertyChanged(()=>Keyword);
                if (string.IsNullOrEmpty(keyword))
                {
                    UpdatePlugins();
                }
                else
                {
                    Search(keyword);
                }
            }
        }

        public PluginViewModel SelectedPlugin
        {
            get { return selectedPlugin; }
            set
            {
                if (selectedPlugin != null)
                {
                    //GisEditor.LayoutManager.HighlightUIPlugins.Clear();
                    (selectedPlugin.Plugin as UIPlugin).IsHighlighted = false;
                }

                selectedPlugin = value;
                RaisePropertyChanged(()=>SelectedPlugin);

                if (selectedPlugin != null)
                {
                    //GisEditor.LayoutManager.HighlightUIPlugins.Add(selectedPlugin.Plugin as UIPlugin);
                    (selectedPlugin.Plugin as UIPlugin).IsHighlighted = true;
                }
            }
        }

        public void Unselect(UIPlugin plugin)
        {
            //GisEditor.LayoutManager.HighlightUIPlugins.Remove(plugin);
            plugin.IsHighlighted = false;
            SelectedPlugin = null;
        }

        private void UpdatePlugins()
        {
            results.Clear();
            foreach (var plugin in GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>())
            {
                var pluginModel = new PluginViewModel(plugin);
                pluginModel.Keywords = string.Empty;
                results.Add(pluginModel);
            }
        }

        private void Search(string word)
        {
            results.Clear();
            Collection<List<UIPlugin>> allPossibleResults = new Collection<List<UIPlugin>>();
            var keywords = word.Split(',');
            foreach (var tmpKeyword in keywords)
            {
                if (!string.IsNullOrEmpty(tmpKeyword))
                {
                    var pluginResults = GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>().Where(p => p.Name.Contains(tmpKeyword)
                        || p.Author.Contains(tmpKeyword)
                        || p.Description.Contains(tmpKeyword)).ToList();

                    allPossibleResults.Add(pluginResults);
                }
            }

            foreach (var plugin in GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>())
            {
                bool doesAllContains = false;
                foreach (var item in allPossibleResults)
                {
                    if (!(doesAllContains = item.Contains(plugin)))
                    {
                        break;
                    }
                }

                if (doesAllContains)
                {
                    if (plugin != null)
                    {
                        var pluginModel = new PluginViewModel(plugin);
                        pluginModel.Keywords = word;
                        foreach (var singleKeyword in keywords)
                        {
                            if (!string.IsNullOrEmpty(singleKeyword))
                            {
                                pluginModel.Keywords = pluginModel.Keywords.Replace(singleKeyword, string.Format(tagFormat, singleKeyword));
                            }
                        }

                        results.Add(pluginModel);
                    }
                }
            }
        }
    }
}