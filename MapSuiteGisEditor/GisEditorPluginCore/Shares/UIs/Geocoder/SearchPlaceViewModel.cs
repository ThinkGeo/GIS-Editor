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
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using ThinkGeo.MapSuite.GeocodeServerSdk;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SearchPlaceViewModel : ViewModelBase
    {
        private static readonly string loadingText = "Loading...";

        public static int SearchPlaceMaxResultCount = 10;
        public static readonly string geocoderServerUrl = "http://geocoderserver.thinkgeo.com/GeocoderServer.axd";
        public static readonly string InternalProjectionKey = "INTERNAL_PROJECTION";

        private bool isBusy;
        private string address;
        private DataTable currentDataTable;
        private LayerDefinition layerDefinition;
        private Visibility specifiedTableVisibility;
        private Visibility geocodeTableVisibility;
        private ObservableCollection<SearchedResultViewModel> searchResults;
        private Collection<SearchEntriesModel> searchEntriesModels;
        private ObservedCommand findCommand;
        private ObservedCommand clearPlottedPlacesCommand;

        public SearchPlaceViewModel()
        {
            searchResults = new ObservableCollection<SearchedResultViewModel>();
            geocodeTableVisibility = Visibility.Visible;
            specifiedTableVisibility = Visibility.Collapsed;
            InitializeSearchEntries();
        }

        public LayerDefinition LayerDefinition
        {
            get { return layerDefinition; }
            set { layerDefinition = value; }
        }

        public Visibility SpecifiedTableVisibility
        {
            get { return specifiedTableVisibility; }
            set
            {
                specifiedTableVisibility = value;
                RaisePropertyChanged(() => SpecifiedTableVisibility);
            }
        }

        public Visibility GeocodeTableVisibility
        {
            get { return geocodeTableVisibility; }
            set
            {
                geocodeTableVisibility = value;
                RaisePropertyChanged(() => GeocodeTableVisibility);
            }
        }

        public ObservableCollection<SearchedResultViewModel> SearchResults
        {
            get
            {
                return searchResults;
            }
        }

        public ObservedCommand ClearPlottedPlacesCommand
        {
            get
            {
                if (clearPlottedPlacesCommand == null)
                {
                    clearPlottedPlacesCommand = new ObservedCommand(() =>
                    {
                        Address = string.Empty;
                        SearchResults.Clear();
                        CurrentDataTable.Clear();

                        if (GisEditor.ActiveMap.Overlays.Contains("PopupOverlay"))
                        {
                            var popupOverlay = CurrentOverlays.PopupOverlay;
                            if (popupOverlay != null)
                            {
                                popupOverlay.Popups.Clear();
                                popupOverlay.Refresh();
                            }
                        }
                    },
                    () => !string.IsNullOrEmpty(Address) || SearchResults.Count > 0);
                }

                return clearPlottedPlacesCommand;
            }
        }

        public ObservedCommand FindCommand
        {
            get
            {
                return findCommand ?? (findCommand = new ObservedCommand(() =>
                {
                    Collection<LayerPlugin> plugins = new Collection<LayerPlugin>();

                    SearchEntriesModels.Where(m => m.IsChecked).ForEach(s => plugins.Add(s.LayerPlugin));
                    SearchPlace(plugins);
                }, () => !string.IsNullOrEmpty(Address) && !IsBusy && GisEditor.ActiveMap != null
                    ));
            }
        }

        public DataTable CurrentDataTable
        {
            get { return currentDataTable; }
            set
            {
                currentDataTable = value;
                RaisePropertyChanged(() => CurrentDataTable);
            }
        }

        public string Address
        {
            get { return address; }
            set
            {
                address = value;
                RaisePropertyChanged(() => Address);
            }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                RaisePropertyChanged(() => IsBusy);
            }
        }

        public Collection<SearchEntriesModel> SearchEntriesModels
        {
            get { return searchEntriesModels; }
        }

        public void SearchPlace(Collection<LayerPlugin> pluginNames)
        {
            SearchResults.Clear();
            CurrentDataTable = new DataTable();
            IsBusy = true;

            if (pluginNames.Count > 1 || (pluginNames.Count == 1 && pluginNames[0] is UsingOnlineServiceLayerPlugin))
            {
                SpecifiedTableVisibility = Visibility.Collapsed;
                GeocodeTableVisibility = Visibility.Visible;
            }
            else
            {
                SpecifiedTableVisibility = Visibility.Visible;
                GeocodeTableVisibility = Visibility.Collapsed;
            }

            Task.Run(() =>
            {
                Collection<GeocodeMatch> resultMatches = new Collection<GeocodeMatch>();
                Collection<string> errorMessages = new Collection<string>();

                if (pluginNames.Any(p => p is UsingOnlineServiceLayerPlugin))
                {
                    FillMatchesInGeocoder(Address, resultMatches, errorMessages);
                }

                if (pluginNames.Any(p => p is ShapeFileFeatureLayerPlugin) && pluginNames.Count == 1)
                {
                    Collection<Feature> features = new Collection<Feature>();
                    var allLayers = GisEditor.ActiveMap.GetFeatureLayers().OfType<ShapeFileFeatureLayer>().ToArray();
                    bool onlyOneLayer = allLayers.Length == 1;
                    FillMatchesByShapefiles(Address, resultMatches, features, errorMessages);
                    if (onlyOneLayer && features.Count > 0)
                    {
                        DataTable dataTable = new DataTable();
                        var columns = features[0].ColumnValues.Keys.Select(r => new DataColumn(r)).ToList();
                        //columns.Add(new DataColumn("BoundingBoxWkt"));

                        dataTable.Columns.AddRange(columns.ToArray());
                        foreach (var feature in features)
                        {
                            DataRow row = dataTable.NewRow();
                            row.RowError = feature.GetBoundingBox().GetWellKnownText();
                            foreach (var result in feature.ColumnValues)
                            {
                                row[result.Key] = result.Value;
                            }
                            dataTable.Rows.Add(row);
                        }

                        foreach (DataColumn column in dataTable.Columns)
                        {
                            string alias = allLayers[0].FeatureSource.GetColumnAlias(column.ColumnName);
                            column.ColumnName = alias;
                        }

                        CurrentDataTable = dataTable;
                    }
                }
                else
                {
                    FillMatchesByLocalLayers(Address, resultMatches, errorMessages, pluginNames.Select(p => p.Name));
                }

                Application.Current.Dispatcher.BeginInvoke(new Action(() => SearchPlaceMatches(resultMatches.Where(r => r.MatchResults.Count > 0), errorMessages)));
            });
        }

        private void SearchPlaceMatches(IEnumerable<GeocodeMatch> resultMatches, Collection<string> errorMessages)
        {
            IsBusy = false;
            lock (SearchResults)
            {
                foreach (var match in resultMatches.Take(SearchPlaceMaxResultCount))
                {
                    SearchedResultViewModel searchedPlace = new SearchedResultViewModel();
                    searchedPlace.City = loadingText;
                    searchedPlace.Load(match);
                    if (!searchedPlace.IsEmpty)
                    {
                        SearchResults.Add(searchedPlace);
                    }
                }
            }

            if (SearchResults.Count == 0 && errorMessages.Count == 0 && (currentDataTable == null || currentDataTable.Rows.Count == 0))
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("SearchPlaceVieModelNoResultsText"));
            }
            else if (errorMessages.Count > 0)
            {
                string errorMessage = string.Join("\r\n", errorMessages);
                string errorTitle = GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle");
                Application.Current.Dispatcher.BeginInvoke(() => System.Windows.Forms.MessageBox.Show(errorMessage, errorTitle));
            }
        }

        private void FillMatchesByShapefiles(string searchAddress, Collection<GeocodeMatch> resultMatches, Collection<Feature> features, Collection<string> errorMessages)
        {
            List<ShapeFileFeatureLayer> layers = new List<ShapeFileFeatureLayer>();

            int resultLayerCount = 0;
            try
            {
                layers = GisEditor.ActiveMap.GetFeatureLayers().OfType<ShapeFileFeatureLayer>().ToList();
                LayerPlugin layerPlugin = GisEditor.LayerManager.GetLayerPlugins<ShapeFileFeatureLayer>().FirstOrDefault();
                if (layerPlugin != null && layerPlugin.SearchPlaceTool.CanSearchPlace())
                {
                    foreach (ShapeFileFeatureLayer layer in layers)
                    {
                        List<Feature> resultFeatures = layerPlugin.SearchPlaceTool.SearchPlaces(searchAddress, layer).Take(SearchPlaceMaxResultCount).ToList();
                        if (resultFeatures.Count > 0)
                        {
                            resultLayerCount++;
                            foreach (var item in resultFeatures)
                            {
                                features.Add(item);
                            }
                            IEnumerable<GeocodeMatch> resultMatchs = GetMatchedResult(resultFeatures);

                            foreach (GeocodeMatch item in resultMatchs)
                            {
                                resultMatches.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                errorMessages.Add(ex.Message);
            }
        }

        private void FillMatchesByLocalLayers(string searchAddress, Collection<GeocodeMatch> resultMatches, Collection<string> errorMessages, IEnumerable<string> pluginNames)
        {
            try
            {
                List<Layer> layers = GetAvailableLayers(pluginNames).ToList();
                foreach (Layer layer in layers)
                {
                    LayerPlugin layerPlugin = GisEditor.LayerManager.GetLayerPlugins(layer.GetType()).FirstOrDefault();
                    if (layerPlugin != null && layerPlugin.SearchPlaceTool.CanSearchPlace(layer))
                    {
                        List<Feature> resultFeatures = layerPlugin.SearchPlaceTool.SearchPlaces(searchAddress, layer).Take(SearchPlaceMaxResultCount).ToList();
                        IEnumerable<GeocodeMatch> resultMatchs = GetMatchedResult(resultFeatures);

                        foreach (GeocodeMatch item in resultMatchs)
                        {
                            resultMatches.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                errorMessages.Add(ex.Message);
            }
        }

        private static void FillMatchesInGeocoder(PointShape pointShape, SearchedResultViewModel searchedResultViewModel)
        {
            try
            {
                Collection<GeocodeMatch> matches = SearchPlaceHelper.GetGeocodeMatches(pointShape.X, pointShape.Y);
                Application.Current.Dispatcher.BeginInvoke(obj =>
                {
                    Collection<GeocodeMatch> tempMatches = (Collection<GeocodeMatch>)obj;
                    if (searchedResultViewModel.City.Equals(loadingText))
                    {
                        searchedResultViewModel.City = string.Empty;
                    }
                    foreach (var match in tempMatches)
                    {
                        if (match.MatchResults.ContainsKey("City"))
                            searchedResultViewModel.City = match.MatchResults["City"];
                        else if (searchedResultViewModel.City.Equals(loadingText))
                        {
                            searchedResultViewModel.City = "";
                        }
                        if (match.MatchResults.ContainsKey("State"))
                            searchedResultViewModel.State = match.MatchResults["State"];
                        if (match.MatchResults.ContainsKey("County"))
                            searchedResultViewModel.County = match.MatchResults["County"];
                        if (match.MatchResults.ContainsKey("Zip"))
                            searchedResultViewModel.Zipcode = match.MatchResults["Zip"];
                    }
                }, matches);
            }
            catch (Exception ex)
            {
                if (searchedResultViewModel.City.Equals(loadingText))
                {
                    searchedResultViewModel.City = "";
                }
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
        }

        private static void FillMatchesInGeocoder(string searchAddress, Collection<GeocodeMatch> resultMatches, Collection<string> errorMessages)
        {
            try
            {
                var matches = SearchPlaceHelper.GetGeocodeMatches(searchAddress);
                matches.ForEach(m => resultMatches.Add(m));
            }
            catch (WebException webException)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, webException.Message, new ExceptionInfo(webException));
                errorMessages.Add(GisEditor.LanguageManager.GetStringResource("SearchPlaceViewModelOnlineSearchFailedText"));
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                errorMessages.Add(ex.Message);
            }
        }

        private static IEnumerable<GeocodeMatch> GetMatchedResult(IEnumerable<Feature> matchedFeatures)
        {
            Collection<GeocodeMatch> resultMatches = new Collection<GeocodeMatch>();
            foreach (Feature feature in matchedFeatures)
            {
                GeocodeMatch geocodeMatch = GetGeocodeMatch(feature);
                resultMatches.Add(geocodeMatch);
            }
            return resultMatches;
        }

        private IEnumerable<Layer> GetAvailableLayers(IEnumerable<string> pluginNames)
        {
            Collection<Layer> layers = new Collection<Layer>();
            foreach (LayerOverlay layerOverlay in GisEditor.ActiveMap.Overlays.OfType<LayerOverlay>())
            {
                layerOverlay.Layers.Where(l => pluginNames.Contains(GisEditor.LayerManager.GetLayerPlugins(l.GetType()).FirstOrDefault().Name)).ForEach(layers.Add);
            }

            return layers;
        }

        private void ReplaceResultMatch()
        {
            foreach (var item in SearchResults)
            {
                if (string.IsNullOrEmpty(item.County)
                    || string.IsNullOrEmpty(item.State)
                    || string.IsNullOrEmpty(item.Zipcode))
                {
                    if (item.CentroidPoint != null)
                    {
                        FillMatchesInGeocoder(item.CentroidPoint, item);
                    }
                    else if (item.City.Equals(loadingText)) item.City = "";
                }
                else if (item.City.Equals(loadingText))
                {
                    item.City = "";
                }
            }
        }

        private void InitializeSearchEntries()
        {
            searchEntriesModels = new Collection<SearchEntriesModel>();
            UsingOnlineServiceLayerPlugin geocodeplugin = new UsingOnlineServiceLayerPlugin();
            SearchEntriesModel searchModel = new SearchEntriesModel(geocodeplugin.Name, false, geocodeplugin);

            LayerPlugin shapeFilePlugin = GisEditor.LayerManager.GetLayerPlugins<ShapeFileFeatureLayer>().FirstOrDefault();
            SearchEntriesModel shapeFilePluginModel = new SearchEntriesModel("On Local Shapefiles", true, shapeFilePlugin);

            searchEntriesModels.Add(searchModel);
            searchEntriesModels.Add(shapeFilePluginModel);
        }

        private static GeocodeMatch GetGeocodeMatch(Feature feature)
        {
            GeocodeMatch geocodeMatch = new GeocodeMatch();
            geocodeMatch.MatchResults["BoundingBox"] = feature.GetBoundingBox().GetWellKnownText();
            geocodeMatch.MatchResults["CentroidPoint"] = feature.GetBoundingBox().GetCenterPoint().GetWellKnownText();
            if (feature.ColumnValues.ContainsKey(InternalProjectionKey))
            {
                geocodeMatch.MatchResults[InternalProjectionKey] = feature.ColumnValues[InternalProjectionKey];
            }

            foreach (var item in feature.ColumnValues)
            {
                if (item.Key.Equals("City"))
                {
                    geocodeMatch.MatchResults["City"] = item.Value;
                }
                if (item.Key.Equals("State"))
                {
                    geocodeMatch.MatchResults["State"] = item.Value;
                }
                if (item.Key.Equals("County"))
                {
                    geocodeMatch.MatchResults["County"] = item.Value;
                }
                if (item.Key.Equals("Zip"))
                {
                    geocodeMatch.MatchResults["Zip"] = item.Value;
                }
                if (item.Key.Equals("Street"))
                {
                    geocodeMatch.MatchResults["Street"] = item.Value;
                }
                if (item.Key.Equals("SearchSegment"))
                {
                    geocodeMatch.MatchResults["SearchSegment"] = item.Value;
                }
            }
            geocodeMatch.MatchType = MatchType.None;
            return geocodeMatch;
        }
    }
}