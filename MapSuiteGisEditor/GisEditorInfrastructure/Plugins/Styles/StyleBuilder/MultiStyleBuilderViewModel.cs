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


using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal class MultiStyleBuilderViewModel : ViewModelBase
    {
        private ObservableCollection<StyleBuilderViewModel> styleBuilderViewModels;
        private StyleBuilderViewModel selectedStyleBuilderViewModel;
        private Collection<StyleBuilderResult> styleBuilderResults;

        public MultiStyleBuilderViewModel()
            : this(null)
        { }

        public MultiStyleBuilderViewModel(IEnumerable<StyleBuilderViewModel> viewModels)
        {
            styleBuilderViewModels = new ObservableCollection<StyleBuilderViewModel>();
            styleBuilderResults = new Collection<StyleBuilderResult>();
            viewModels.ForEach(item => styleBuilderViewModels.Add(item));
        }

        public ObservableCollection<StyleBuilderViewModel> StyleBuilderViewModels
        {
            get { return styleBuilderViewModels; }
        }

        public StyleBuilderViewModel SelectedStyleBuilderViewModel
        {
            get { return selectedStyleBuilderViewModel; }
            set
            {
                selectedStyleBuilderViewModel = value;
                RaisePropertyChanged(()=>SelectedStyleBuilderViewModel);
            }
        }

        public Collection<StyleBuilderResult> StyleBuilderResults
        {
            get { return styleBuilderResults; }
        }

        public void CancelStyles()
        {
            StyleBuilderResults.ForEach(item => item.Canceled = true);
        }

        public void SyncStyleBuilderResults()
        {
            SyncStyleBuilderResults(null);
        }

        public void SyncStyleBuilderResults(Action<StyleBuilderResult, StyleBuilderViewModel> resultAdded)
        {
            StyleBuilderResults.Clear();
            foreach (var item in StyleBuilderViewModels)
            {
                StyleBuilderResult result = new StyleBuilderResult(item.ResultStyle, false, item.FromZoomLevelIndex, item.ToZoomLevelIndex);
                StyleBuilderResults.Add(result);
                if (resultAdded != null) resultAdded(result, item);
            }
        }
    }
}