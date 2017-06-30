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
using GalaSoft.MvvmLight;
using System.Collections.Generic;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class StyleWizardSharedObject : ViewModelBase
    {
        private ObservableCollection<StyleCheckableItemModel> styleCategories;
        private StyleCategories targetFeatureLayer;
        private StyleCheckableItemModel selectedStyleCategory;
        private bool isAlwaysShow;
        private string styleFileFullName;
        private Dictionary<string, Collection<StyleCheckableItemModel>> allStyleSources;
        private Style targetStyle;
        private double fromScale;
        private double toScale;
        private StylePlugin resultStyleProvider;
        private Collection<StyleCheckableItemModel> styleSources;

        public StyleWizardSharedObject()
        {
            IsAlwaysShow = true;
            styleCategories = new ObservableCollection<StyleCheckableItemModel>();
            allStyleSources = new Dictionary<string, Collection<StyleCheckableItemModel>>();
        }

        public StyleCategories TargetStyleCategories
        {
            get { return targetFeatureLayer; }
            set
            {
                targetFeatureLayer = value;
            }
        }

        public Style TargetStyle
        {
            get { return targetStyle; }
            set { targetStyle = value; }
        }

        public double FromScale
        {
            get { return fromScale; }
            set { fromScale = value; }
        }

        public double ToScale
        {
            get { return toScale; }
            set { toScale = value; }
        }

        public StylePlugin ResultStylePlugin
        {
            get { return resultStyleProvider; }
            set { resultStyleProvider = value; }
        }

        public bool IsAlwaysShow
        {
            get
            {
                return isAlwaysShow;
            }
            set
            {
                isAlwaysShow = value;
                RaisePropertyChanged(()=>IsAlwaysShow);
            }
        }

        public string StyleFileFullName
        {
            get { return styleFileFullName; }
            set
            {
                styleFileFullName = value;
                RaisePropertyChanged(()=>StyleFileFullName);
            }
        }

        public ObservableCollection<StyleCheckableItemModel> StyleCategories
        {
            get { return styleCategories; }
        }

        public StyleCheckableItemModel SelectedStyleCategory
        {
            get { return selectedStyleCategory; }
            set
            {
                selectedStyleCategory = value;
                RaisePropertyChanged(()=>SelectedStyleCategory);
            }
        }

        public Dictionary<string, Collection<StyleCheckableItemModel>> AllStyleSources
        {
            get { return allStyleSources; }
            set
            {
                allStyleSources = value;
                RaisePropertyChanged(()=>StyleSources);
            }
        }

        public Collection<StyleCheckableItemModel> StyleSources
        {
            get { return styleSources; }
            set
            {
                styleSources = value;
                RaisePropertyChanged(()=>StyleSources);
            }
        }
    }
}
