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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class LoadCustomSetViewModel : ViewModelBase
    {
        private ObservableCollection<string> allCustomSetNames;
        private string selectedSetName;
        private ViewUIPlugin viewPlugin;

        public LoadCustomSetViewModel()
        {
            allCustomSetNames = new ObservableCollection<string>();
            allCustomSetNames.CollectionChanged += AllCustomSetNames_CollectionChanged;
            viewPlugin = GisEditor.UIManager.GetActiveUIPlugins<ViewUIPlugin>().FirstOrDefault();
            if (viewPlugin != null)
            {
                foreach (var item in viewPlugin.CustomZoomLevelSets)
                {
                    allCustomSetNames.Add(item.Key);
                }
            }
        }

        private void AllCustomSetNames_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove && viewPlugin != null)
            {
                var deletedItem = e.OldItems.OfType<string>().FirstOrDefault();
                viewPlugin.CustomZoomLevelSets.Remove(deletedItem);
            }
        }

        public ObservableCollection<string> AllCustomSetNames
        {
            get { return allCustomSetNames; }
        }

        public List<double> SelectedScales
        {
            get
            {
                if (viewPlugin != null && viewPlugin.CustomZoomLevelSets.ContainsKey(selectedSetName))
                    return viewPlugin.CustomZoomLevelSets[selectedSetName];
                else return null;
            }
        }

        public string SelectedSetName
        {
            get { return selectedSetName; }
            set
            {
                selectedSetName = value;
                RaisePropertyChanged(()=>SelectedSetName);
            }
        }
    }
}
