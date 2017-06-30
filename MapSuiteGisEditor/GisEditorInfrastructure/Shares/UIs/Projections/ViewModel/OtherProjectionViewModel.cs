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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ThinkGeo.MapSuite.GisEditor.Properties;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    internal class OtherProjectionViewModel : INotifyPropertyChanged, IProjectionViewModel
    {
        private string searchKeyWord;
        private Proj4Model selectedProj4Model;
        private ObservableCollection<Proj4Model> searchedResult;
        private SearchProjectionType selectedProjectionType;

        public event PropertyChangedEventHandler PropertyChanged;

        private static object lockObject = new object();
        private bool searchIsEnabled;
        private bool loadButtonIsEnabled;

        public OtherProjectionViewModel()
        {
            SearchIsEnabled = true;
            searchedResult = new ObservableCollection<Proj4Model>();
            Search();
            OnPropertyChanged("SearchedResult");
        }

        public bool IsReadOnly
        {
            get
            {
                return SelectedProjectionType != SearchProjectionType.Custom;
            }
        }

        public SearchProjectionType SelectedProjectionType
        {
            get { return selectedProjectionType; }
            set
            {
                selectedProjectionType = value;
                if (selectedProjectionType != SearchProjectionType.Custom)
                {
                    SearchIsEnabled = true;
                    Search();
                }
                else
                {
                    SearchIsEnabled = false;
                    SearchKeyWord = string.Empty;
                    SelectedProj4Model = new Proj4Model();
                }
                OnPropertyChanged("SelectedProjectionType");
                OnPropertyChanged("SearchedResult");
                OnPropertyChanged("IsReadOnly");
            }
        }

        public string SearchKeyWord
        {
            get { return searchKeyWord; }
            set
            {
                searchKeyWord = value;
                Search();
                OnPropertyChanged("SearchKeyWord");
                OnPropertyChanged("SearchedResult");
            }
        }

        public Proj4Model SelectedProj4Model
        {
            get { return selectedProj4Model; }
            set
            {
                selectedProj4Model = value;
                OnPropertyChanged("SelectedProj4Model");
                OnPropertyChanged("SelectedProj4ProjectionParameters");
            }
        }

        public ObservableCollection<Proj4Model> SearchedResult
        {
            get { return searchedResult; }
        }

        public string SelectedProj4ProjectionParameters
        {
            get
            {
                if (SelectedProj4Model == null) return String.Empty;
                else return SelectedProj4Model.Proj4Parameter;
            }
            set
            {
                if (SelectedProj4Model != null) SelectedProj4Model.Proj4Parameter = value;
                OnPropertyChanged("SelectedProj4ProjectionParameters");
            }
        }

        public bool SearchIsEnabled
        {
            get { return searchIsEnabled; }
            set
            {
                searchIsEnabled = value;
                OnPropertyChanged("SearchIsEnabled");
                LoadButtonIsEnabled = !value;
            }
        }

        public bool LoadButtonIsEnabled
        {
            get { return loadButtonIsEnabled; }
            set
            {
                loadButtonIsEnabled = value;
                OnPropertyChanged("LoadButtonIsEnabled");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Search()
        {
            SearchedResult.Clear();
            using (var stream = new MemoryStream(Resources.projOther))
            {
                XElement proj4s = XElement.Load(stream);
                var result = from proj4 in proj4s.Descendants("Proj4")
                             where SearchHandler(SearchKeyWord, proj4)
                             select new Proj4Model
                             {
                                 Name = proj4.Attribute("Name").Value,
                                 Proj4Parameter = proj4.Attribute("Parameter").Value,
                                 ProjectionType = SelectedProjectionType,
                                 SRS = Int32.Parse(proj4.Attribute("SRS").Value)
                             };

                foreach (var proj4 in result)
                {
                    if (!SearchedResult.Any(p => p.SRS.Equals(proj4.SRS) || p.Proj4Parameter.Equals(proj4.Proj4Parameter, StringComparison.OrdinalIgnoreCase)))
                        SearchedResult.Add(proj4);
                }

                SelectedProj4Model = SearchedResult.FirstOrDefault(r => SelectedProj4Model != null && r.SRS == SelectedProj4Model.SRS);
                if (SelectedProj4Model == null) { SelectedProj4Model = SearchedResult.FirstOrDefault(); }
            }
        }

        private bool SearchHandler(string keyword, XElement element)
        {
            bool isMatched = false;
            string upperCasedKeyword = String.Empty;
            if (!String.IsNullOrEmpty(keyword)) upperCasedKeyword = keyword.ToUpperInvariant();
            if (String.IsNullOrEmpty(keyword) && element.Attribute("Type").Value == SelectedProjectionType.ToString())
            {
                isMatched = true;
            }
            else if ((element.Attribute("SRS").Value.ToUpperInvariant().Contains(upperCasedKeyword)
                || element.Attribute("Name").Value.ToUpperInvariant().Contains(upperCasedKeyword))
                && element.Attribute("Type").Value == SelectedProjectionType.ToString())
            {
                isMatched = true;
            }

            return isMatched;
        }
    }
}