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
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for SearchPlaceUserControl.xaml
    /// </summary>
    public partial class SearchPlaceUserControl : UserControl
    {
        public event EventHandler<PlotOnMapEventArgs> Plotted;

        public event EventHandler<EventArgs> SearchContextMenuOpening;

        public event CanExecuteRoutedEventHandler CanClearPlottedPlaces;

        public event ExecutedRoutedEventHandler ClearPlottedPlaces;

        public SearchPlaceUserControl()
        {
            InitializeComponent();
            HelpContainer.Content = HelpResourceHelper.GetHelpButton("PlaceSearchHelp", HelpButtonMode.IconOnly);
        }

        [Obfuscation]
        protected virtual void OnSearchContextMenuOpening(object sender, EventArgs e)
        {
            EventHandler<EventArgs> handler = SearchContextMenuOpening;
            if (handler != null) handler(sender, e);
        }

        [Obfuscation]
        protected virtual void OnPlotted(PlotOnMapEventArgs e)
        {
            EventHandler<PlotOnMapEventArgs> handler = Plotted;
            if (handler != null) handler(this, e);
        }

        [Obfuscation]
        protected virtual void OnCanClearPlottedPlaces(CanExecuteRoutedEventArgs e)
        {
            CanExecuteRoutedEventHandler handler = CanClearPlottedPlaces;
            if (handler != null) handler(this, e);
        }

        [Obfuscation]
        protected virtual void OnClearPlottedPlaces(ExecutedRoutedEventArgs e)
        {
            ExecutedRoutedEventHandler handler = ClearPlottedPlaces;
            if (handler != null) handler(this, e);
        }

        [Obfuscation]
        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SearchedResultViewModel tmpModel = (SearchedResultViewModel)e.Parameter;
            OnPlotted(new PlotOnMapEventArgs(tmpModel));
        }

        [Obfuscation]
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && FindButton.Command.CanExecute(null))
            {
                FindButton.Command.Execute(null);
            }
        }

        [Obfuscation]
        private void ClearPlottedPlacesCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            OnCanClearPlottedPlaces(e);
        }

        [Obfuscation]
        private void ClearPlottedPlacesCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OnClearPlottedPlaces(e);
        }

        [Obfuscation]
        private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = e.Source as DataGridRow;
            if (item != null)
            {
                DataRowView rowView = (DataRowView)item.Item;
                DataRow row = rowView.Row;
                if (!string.IsNullOrEmpty(row.RowError))
                {
                    RectangleShape boundingBox = new RectangleShape(row.RowError);
                    SearchedResultViewModel searchedPlace = new SearchedResultViewModel();
                    searchedPlace.BoundingBox = boundingBox;
                    searchedPlace.Address = string.Join(",", row.ItemArray.Take(row.ItemArray.Length - 1));
                    OnPlotted(new PlotOnMapEventArgs(searchedPlace));
                }
            }
        }

        [Obfuscation]
        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = e.Source as ListViewItem;
            if (item != null)
            {
                SearchedResultViewModel tmpModel = item.Content as SearchedResultViewModel;
                if (tmpModel != null)
                    OnPlotted(new PlotOnMapEventArgs(tmpModel));
            }
        }

        [Obfuscation]
        private void ListViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            OnSearchContextMenuOpening(sender, e);
        }
    }
}