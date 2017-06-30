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


using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for DbfViewerUserControl.xaml
    /// </summary>
    public partial class DataViewerUserControl : UserControl
    {
        private static Tuple<double, double> viewerSize;

        private bool isClosing;
        private bool isRetrived;
        private bool isEditable;
        private bool isHighlightFeatureOnly;
        private bool isHighlightFeatureEnabled;
        private int minRowIndex = 0;
        private int maxRowIndex = 0;
        private int selectedIndex = 0;
        private string cellValue = string.Empty;
        private Thread thread;
        private GisEditorWpfMap map;
        private ScrollBar scrollBar;
        private DataRowView dataRowView;
        private DataGridCell lastDataGridCell;
        private DataRowView lastDataRow = null;
        private DataViewerViewModel viewModel;
        private FeatureLayer selectedFeatureLayer;
        private Collection<FeatureLayer> featureLayers;
        private Dictionary<FeatureLayer, Collection<string>> linkColumnNames;

        public static event EventHandler<LoadingUriColumnsDataViewerUserControlEventArgs> LoadingUriColumns;

        protected virtual void OnLoadingUriColumns(LoadingUriColumnsDataViewerUserControlEventArgs e)
        {
            EventHandler<LoadingUriColumnsDataViewerUserControlEventArgs> handler = LoadingUriColumns;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<RoutedEventArgs> HyperlinkClick;
        private string title;

        protected virtual void OnHyperlinkClick(RoutedEventArgs e)
        {
            EventHandler<RoutedEventArgs> handler = HyperlinkClick;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public DataViewerUserControl()
            : this(GisEditor.ActiveMap.ActiveLayer as FeatureLayer)
        { }

        public DataViewerUserControl(FeatureLayer selectedFeatureLayer)
            : this(selectedFeatureLayer, GisEditor.ActiveMap.GetFeatureLayers(true))
        { }

        public DataViewerUserControl(FeatureLayer selectedFeatureLayer, IEnumerable<FeatureLayer> featureLayers)
            : this(selectedFeatureLayer, featureLayers, new Dictionary<FeatureLayer, Collection<string>>())
        { }

        public DataViewerUserControl(FeatureLayer selectedFeatureLayer, IEnumerable<FeatureLayer> featureLayers, IDictionary<FeatureLayer, Collection<string>> linkColumnNames)
        {
            this.InitializeComponent();
            this.Tag = GisEditor.ActiveMap;
            this.map = GisEditor.ActiveMap;
            this.isHighlightFeatureEnabled = true;
            this.selectedFeatureLayer = selectedFeatureLayer;
            this.featureLayers = new Collection<FeatureLayer>();

            this.linkColumnNames = new Dictionary<FeatureLayer, Collection<string>>();
            foreach (var item in featureLayers)
            {
                this.featureLayers.Add(item);
                this.linkColumnNames.Add(item, new Collection<string>());
            }
            foreach (var item in linkColumnNames)
            {
                this.linkColumnNames[item.Key] = item.Value;
            }
        }

        [Obsolete("This property is obsolete. This API is obsolete and may be removed in or after version 9.0.")]
        public Boolean IsInViewportFeatureOnly { get; set; }

        public bool IsHighlightFeatureOnly
        {
            get { return isHighlightFeatureOnly; }
            set { isHighlightFeatureOnly = value; }
        }

        public bool IsEditable
        {
            get { return isEditable; }
            set { isEditable = value; }
        }

        public bool IsHighlightFeatureEnabled
        {
            get { return isHighlightFeatureEnabled; }
            set { isHighlightFeatureEnabled = value; }
        }

        public void ShowDialog()
        {
            ShowDialogCore();
        }

        protected virtual void ShowDialogCore()
        {
            string title = isEditable ? GisEditor.LanguageManager.GetStringResource("ViewDataEditDataTitle") : GisEditor.LanguageManager.GetStringResource("ViewDataViewDataTitle");
            Window dbfViewer = new Window { Style = Application.Current.FindResource("WindowStyle") as System.Windows.Style, Title = title };
            dbfViewer.Content = this;
            if (viewerSize != null)
            {
                dbfViewer.Width = viewerSize.Item1;
                dbfViewer.Height = viewerSize.Item2;
            }
            dbfViewer.Closing += (s, e) =>
            {
                this.IsClosing = true;
                viewerSize = new Tuple<double, double>(dbfViewer.ActualWidth, dbfViewer.ActualHeight);
            };
            dbfViewer.ShowDialog();
            GisEditor.DockWindowManager.DockWindows.Select(d => d.Content).OfType<DataViewerUserControl>().ForEach(d =>
            {
                DataViewerViewModel viewModel = d.DataContext as DataViewerViewModel;
                if (viewModel != null)
                {
                    viewModel.SelectedLayerAdapter = viewModel.SelectedLayerAdapter;
                }
            });

            this.isHighlightFeatureOnly = (this.DataContext as DataViewerViewModel).ShowSelectedFeatures;
        }

        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        public void ShowDock()
        {
            ShowDockCore();
        }

        protected virtual void ShowDockCore()
        {
            string newTitle = isEditable ? GisEditor.LanguageManager.GetStringResource("ViewDataEditDataTitle") : GisEditor.LanguageManager.GetStringResource("ViewDataViewDataTitle");

            if (!string.IsNullOrEmpty(title))
            {
                title = title.Replace("+", "_");
                title = title.Replace("/", "_");
                title = title.Replace("!", "_");
                title = title.Replace("@", "_");
                title = title.Replace("#", "_");
                title = title.Replace("$", "_");
                title = title.Replace("%", "_");
                title = title.Replace("^", "_");
                title = title.Replace("&", "_");
                title = title.Replace("*", "_");
                title = title.Replace("(", "_");
                title = title.Replace(")", "_");
                title = title.Replace("-", "_");
                title = title.Replace("=", "_");
                title = title.Replace("[", "_");
                title = title.Replace("{", "_");
                title = title.Replace("]", "_");
                title = title.Replace("}", "_");
                title = title.Replace("\\", "_");
                title = title.Replace("|", "_");
                title = title.Replace(";", "_");
                title = title.Replace(":", "_");
                title = title.Replace("'", "_");
                title = title.Replace("\"", "_");
                title = title.Replace(",", "_");
                title = title.Replace("<", "_");
                title = title.Replace(".", "_");
                title = title.Replace(">", "_");
                title = title.Replace("/", "_");
                title = title.Replace("?", "_");
                title = title.Replace("`", "_");
                title = title.Replace("~", "_");
                title = title.Replace("\r", "_");
                title = title.Replace("\n", "_");
                title = title.Replace("\r\n", "_");

                newTitle += " " + title;
            }
            else if (GisEditor.ActiveMap != null)
            {
                newTitle += " " + GisEditor.ActiveMap.Name;
            }

            var floatingSize = new Size(800, 600);
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                double floatingWidth = Application.Current.MainWindow.ActualWidth - 100;
                double floatingHeight = Application.Current.MainWindow.ActualHeight - 100;
                if (floatingWidth < 800) floatingWidth = 800;
                if (floatingHeight < 600) floatingHeight = 600;

                floatingSize = new Size(floatingWidth, floatingHeight);
            }

            DockWindow newDockWindow = new DockWindow(this, DockWindowPosition.Bottom, newTitle);
            newDockWindow.FloatingSize = floatingSize;
            newDockWindow.Show(DockWindowPosition.Floating);
            this.isHighlightFeatureOnly = false;
        }

        internal bool IsClosing
        {
            get { return isClosing; }
            set { isClosing = value; }
        }

        private void RetrieveRestData()
        {
            viewModel.IsBusy = true;
            viewModel.BusyContent = "Querying Data...";
            thread = new Thread(new ThreadStart(() =>
            {
                int length = viewModel.RowCount - viewModel.CurrentDataTable.Rows.Count;
                viewModel.CurrentDataTable = viewModel.SelectedLayerAdapter.GetDataTable(viewModel.CurrentDataTable, length);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    viewModel.IsBusy = false;
                }));
            }));
            thread.Priority = ThreadPriority.Highest;
            thread.IsBackground = true;
            thread.Start();
            while (viewModel.IsBusy)
            {
                System.Windows.Forms.Application.DoEvents();
            }
        }

        [Obfuscation]
        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (thread != null && thread.IsAlive)
                thread.Abort();

            if (Tag != null)
            {
                var results = (from overlay in (Tag as GisEditorWpfMap).Overlays.OfType<LayerOverlay>()
                               from layer in overlay.Layers
                               where viewModel.ChangedLayers.Contains(layer)
                               select overlay).Distinct();

                foreach (var tileOverlay in results)
                {
                    TileOverlay overlay = tileOverlay as TileOverlay;
                    if (overlay != null)
                    {
                        tileOverlay.Invalidate();
                    }
                }
            }

            viewModel.CloseFeatureLayer();
            Messenger.Default.Unregister(this);
        }

        private static T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        [Obfuscation]
        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            featureLayers = new Collection<FeatureLayer>(featureLayers.Where(l =>
            {
                var plugin = GisEditor.LayerManager.GetLayerPlugins(l.GetType()).FirstOrDefault() as FeatureLayerPlugin;
                return plugin != null;
            }).ToList());
            if (isEditable)
            {
                string helpUri = GisEditor.LanguageManager.GetStringResource("EditDataHelp");
                HelpContainer.Content = HelpButtonHelper.GetHelpButton(helpUri, HelpButtonMode.IconOnly);
            }
            else
            {
                string helpUri = GisEditor.LanguageManager.GetStringResource("ViewDataHelp");
                HelpContainer.Content = HelpButtonHelper.GetHelpButton(helpUri, HelpButtonMode.IconOnly);
            }

            LoadingUriColumnsDataViewerUserControlEventArgs args = new LoadingUriColumnsDataViewerUserControlEventArgs();
            foreach (var item in linkColumnNames)
            {
                args.UriColumnNames.Add(item.Key, item.Value);
            }
            OnLoadingUriColumns(args);
            if (args.UriColumnNames.Count > 0)
            {
                foreach (var item in args.UriColumnNames)
                {
                    linkColumnNames[item.Key] = item.Value;
                }
            }

            viewModel = new DataViewerViewModel(map, featureLayers, selectedFeatureLayer, isHighlightFeatureOnly, isEditable, linkColumnNames);
            this.deleteColumn.Visibility = viewModel.EditVisible;
            this.DataContext = viewModel;
            viewModel.EditDataChanges.CollectionChanged += EditDataChanges_CollectionChanged;

            scrollBar = GetVisualChild<ScrollBar>(dg);
            scrollBar.LostMouseCapture += new MouseEventHandler(scrollBar_LostMouseCapture);
            Window parentWindow = Parent as Window;

            ContextMenu contextMenu = new ContextMenu();

            MenuItem exportItem = new MenuItem();
            exportItem.Header = GisEditor.LanguageManager.GetStringResource("ViewDataExportButton");

            exportItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/exporttoexcel.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
            exportItem.Click += new RoutedEventHandler(ExportItem_Click);

            MenuItem menuItem = new MenuItem() { Header = GisEditor.LanguageManager.GetStringResource("DataViewerUserControlExportSelectedData") };
            menuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/exporttoexcel.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
            menuItem.Click += new RoutedEventHandler(MenuItem_Click);

            MenuItem copyRowMenuItem = new MenuItem() { Header = GisEditor.LanguageManager.GetStringResource("DataViewerUserControlCopySelectedRowLabel") };
            copyRowMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/copytoexcel.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
            copyRowMenuItem.Click += new RoutedEventHandler(CopyRowMenuItem_Click);

            MenuItem copyCellMenuItem = new MenuItem() { Header = GisEditor.LanguageManager.GetStringResource("DataViewerUserControlCopySelectedCellLabel") };
            copyCellMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/copytoexcel.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
            copyCellMenuItem.Click += new RoutedEventHandler(CopyCellMenuItem_Click);

            contextMenu.Items.Add(exportItem);
            contextMenu.Items.Add(menuItem);
            contextMenu.Items.Add(copyRowMenuItem);
            contextMenu.Items.Add(copyCellMenuItem);
            dg.ContextMenu = contextMenu;

            if (parentWindow != null)
            {
                parentWindow.Closing += ParentWindow_Closing;
            }
        }

        [Obfuscation]
        private void ExportItem_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ExportCommand.Execute(null);
        }

        [Obfuscation]
        private void CopyRowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopyRowValueToClipboard();
        }

        [Obfuscation]
        private void CopyCellMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.Clear();
            Clipboard.SetText(cellValue);
        }

        [Obfuscation]
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectDataTable = new DataTable();

            var dataviewrViewMode = DataContext as DataViewerViewModel;

            foreach (var column in dataviewrViewMode.CurrentDataTable.Columns.Cast<DataColumn>())
            {
                selectDataTable.Columns.Add(column.ColumnName, column.DataType);
            }

            foreach (var rowView in dg.SelectedItems.Cast<DataRowView>())
            {
                DataRow dr = selectDataTable.NewRow();
                dr.ItemArray = rowView.Row.ItemArray;
                selectDataTable.Rows.Add(dr);
            }

            ((DataViewerViewModel)DataContext).ExportCSVData(selectDataTable);
        }

        private void ParentWindow_Closing(object sender, CancelEventArgs e)
        {
            if (viewModel.EditDataChanges.Count > 0)
            {
                System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("DataViewerUserControlSaveTheChangesLabel"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.YesNo);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    SaveChangesClick(null, null);
                }
            }
        }

        private void scrollBar_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (scrollBar.Value == scrollBar.Maximum && viewModel.CurrentDataTable.Rows.Count < viewModel.RowCount && !viewModel.ShowSelectedFeatures)
            {
                viewModel.CurrentDataTable = viewModel.SelectedLayerAdapter.GetDataTable(viewModel.CurrentDataTable, PagedFeatureLayerAdapter.IncreaseSize);
            }
        }

        [Obfuscation]
        private void dg_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (!viewModel.ShowSelectedFeatures && viewModel.CurrentDataTable.Rows.Count < viewModel.RowCount && !viewModel.ClearEnable)
            {
                RetrieveRestData();
                var targetColumn = (sender as DataGrid).Columns.FirstOrDefault(column => column.Header != null && column.Header.Equals(e.Column.Header));
                if (targetColumn != null) e = new DataGridSortingEventArgs(targetColumn);
                if (e.Column.SortDirection == null) e.Column.SortDirection = ListSortDirection.Ascending;
            }

            //UnhighlightAllRows();
            //lastDataRow = null;
            //minRowIndex = 0;
            //maxRowIndex = 0;
            if (e.Column.Header.ToString().Contains("."))
                e.Column.SortMemberPath = e.Column.SortMemberPath.Trim(new char[] { '[', ']' });
        }

        [Obfuscation]
        private void dg_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            string columnName = e.Column.Header.ToString();
            if (e.Column.Header is TextBlock)
            {
                columnName = ((TextBlock)e.Column.Header).Text;
            }
            string originColumnName = columnName;

            TextBox editingTextBox = e.EditingElement as TextBox;
            dataRowView = e.Row.Item as DataRowView;

            if (editingTextBox != null && dataRowView != null)
            {
                string oldValue = dataRowView[columnName].ToString();
                string newValue = editingTextBox.Text;
                if (!string.Equals(oldValue, newValue))
                {
                    try
                    {
                        string featureId = dataRowView[FeatureLayerAdapter.FeatureIdColumnName].ToString();
                        DataColumn dataColumn = dataRowView.DataView.Table.Columns[columnName];
                        if (dataColumn.DataType == typeof(Uri))
                        {
                            dataRowView.Row[columnName] = new Uri(newValue, UriKind.RelativeOrAbsolute);
                        }
                        else
                        {
                            dataRowView.Row[columnName] = newValue;
                        }
                        EditDataChange editDataChange = new EditDataChange(oldValue, newValue, originColumnName, featureId, editingTextBox.Parent as DataGridCell, dataRowView.Row);
                        viewModel.EditDataChanges.Add(editDataChange);
                    }
                    catch (Exception ex)
                    {
                        editingTextBox.Text = oldValue;
                        System.Windows.Forms.MessageBox.Show(ex.Message, "Warning");
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    }
                }
            }
        }

        private bool CheckColumnValuesAreValid(Feature feature, Collection<FeatureSourceColumn> columns)
        {
            var isValid = true;
            foreach (var column in columns)
            {
                if (!feature.ColumnValues.ContainsKey(column.ColumnName)) continue;

                string columnValue = feature.ColumnValues[column.ColumnName];
                if (string.IsNullOrEmpty(columnValue)) continue;
                if (column.TypeName.Equals(DbfColumnType.Numeric.ToString()))
                {
                    if (columnValue.Equals("NAN", StringComparison.OrdinalIgnoreCase) || columnValue.Contains('.'))
                    {
                        double result = 0;
                        if (!Double.TryParse(columnValue, out result))
                        {
                            isValid = false;
                            break;
                        }
                    }
                    else
                    {
                        int result = 0;
                        if (!Int32.TryParse(columnValue, out result))
                        {
                            isValid = false;
                            break;
                        }
                    }
                }
                else if (column.TypeName.Equals(DbfColumnType.Date.ToString()))
                {
                    DateTime result = DateTime.Now;
                    if (columnValue.Length >= 6)
                    {
                        columnValue = columnValue.Insert(6, "/");
                        columnValue = columnValue.Insert(4, "/");
                        if (!DateTime.TryParse(columnValue, out result))
                        {
                            isValid = false;
                            break;
                        }
                    }
                }
                else if (column.TypeName.Equals(DbfColumnType.Float.ToString()))
                {
                    double result = 0;
                    if (!Double.TryParse(columnValue, out result))
                    {
                        isValid = false;
                        break;
                    }
                }
                else if (column.TypeName.Equals(DbfColumnType.Logical.ToString()))
                {
                    bool result = false;
                    switch (columnValue)
                    {
                        case "Y":
                        case "Yes":
                        case "y":
                        case "yes":
                        case "T":
                        case "True":
                        case "t":
                        case "true":
                            columnValue = true.ToString();
                            break;

                        case "N":
                        case "No":
                        case "n":
                        case "no":
                        case "F":
                        case "False":
                        case "f":
                        case "false":
                            columnValue = false.ToString();
                            break;

                        case "?":
                        default:
                            break;
                    }
                    if (!Boolean.TryParse(columnValue, out result))
                    {
                        isValid = false;
                        break;
                    }
                }
            }

            return isValid;
        }

        [Obfuscation]
        private void DataGridRow_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            DataRowView currentDataRow = (sender as DataGridRow).Item as DataRowView;

            if (currentDataRow != null
                && e.LeftButton == MouseButtonState.Pressed
                && !Convert.ToBoolean(currentDataRow[FeatureLayerAdapter.IsSelectedColumnName]))
            {
                HighlightOrUnhighlightRecord(currentDataRow.Row);
            }
        }

        [Obfuscation]
        private void DataGridRow_PreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataRowView currentDataRow = (sender as DataGridRow).Item as DataRowView;
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                HighlightOrUnhighlightRecord(currentDataRow.Row);
                lastDataRow = currentDataRow;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                var currentIndex = dg.Items.IndexOf(currentDataRow);
                if (minRowIndex > 0 || maxRowIndex > 0)
                {
                    if (currentIndex < minRowIndex)
                    {
                        int tmpMaxRowIndex = minRowIndex - 1;
                        minRowIndex = currentIndex;
                        HightlightMultipleRows(minRowIndex, tmpMaxRowIndex);
                    }
                    else if (currentIndex > maxRowIndex)
                    {
                        int tmpMinRowIndex = maxRowIndex + 1;
                        maxRowIndex = currentIndex;
                        HightlightMultipleRows(tmpMinRowIndex, maxRowIndex);
                    }
                }
                else if (lastDataRow != null)
                {
                    var lastIndex = dg.Items.IndexOf(lastDataRow);
                    Collection<DataRow> dataRows = new Collection<DataRow>();
                    minRowIndex = lastIndex > currentIndex ? currentIndex : lastIndex;
                    maxRowIndex = lastIndex > currentIndex ? lastIndex : currentIndex;
                    if (minRowIndex == lastIndex)
                        HightlightMultipleRows(minRowIndex + 1, maxRowIndex);
                    else
                        HightlightMultipleRows(minRowIndex, maxRowIndex - 1);
                }
            }
            else
            {
                UnhighlightAllRows();
                HighlightOrUnhighlightRecord(currentDataRow.Row);
                lastDataRow = currentDataRow;
                minRowIndex = 0;
                maxRowIndex = 0;
            }
        }

        [Obfuscation]
        private void DataGridRow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            DataRowView currentDataRow = (sender as DataGridRow).Item as DataRowView;
            if (e.Key == Key.Delete && currentDataRow != null && !currentDataRow.IsEdit)
            {
                viewModel.DeleteOneRowCommand.Execute(currentDataRow);
                e.Handled = true;
            }
        }

        [Obfuscation]
        private void DataGridRow_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.C && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                CopyRowValueToClipboard();
            }
        }

        private void UnhighlightAllRows()
        {
            foreach (DataRow dataRow in viewModel.CurrentDataTable.Rows)
            {
                if (Convert.ToBoolean(dataRow[FeatureLayerAdapter.IsSelectedColumnName]))
                {
                    HighlightOrUnhighlightRecord(dataRow);
                }
            }
            dg.SelectedItem = null;
        }

        private void HightlightMultipleRows(int min, int max)
        {
            for (int i = min; i <= max; i++)
            {
                var rowView = dg.Items[i] as DataRowView;
                if (rowView != null && !Convert.ToBoolean(rowView[FeatureLayerAdapter.IsSelectedColumnName]))
                {
                    HighlightOrUnhighlightRecord(rowView.Row);
                }
            }
        }

        private void HighlightOrUnhighlightRecord(DataRow currentDataRow)
        {
            bool isSelected = !Convert.ToBoolean(currentDataRow[FeatureLayerAdapter.IsSelectedColumnName]);
            currentDataRow[FeatureLayerAdapter.IsSelectedColumnName] = isSelected;
            var featureId = currentDataRow[FeatureLayerAdapter.FeatureIdColumnName].ToString();
            Feature selectedFeature = null;
            if (!viewModel.SelectedLayer.FeatureIdsToExclude.Contains(featureId))
            {
                viewModel.OpenFeatureLayer();
                var columns = viewModel.SelectedLayer.FeatureSource.GetColumns(GettingColumnsType.FeatureSourceOnly);
                selectedFeature = viewModel.SelectedLayer.FeatureSource.GetFeatureById(featureId, columns.Select(c => c.ColumnName).ToArray());
                viewModel.CloseFeatureLayer();
            }
            else
            {
                var editorOverlay = GisEditor.ActiveMap.FeatureLayerEditOverlay;
                if (editorOverlay != null)
                {
                    if (editorOverlay.EditShapesLayer.InternalFeatures.Contains(featureId))
                    {
                        selectedFeature = editorOverlay.EditShapesLayer.InternalFeatures[featureId];
                    }
                }
            }

            if (selectedFeature != null && selectedFeature.GetWellKnownBinary() != null)
            {
                Feature copyFeature = GisEditor.SelectionManager.GetSelectionOverlay().CreateHighlightFeature(selectedFeature, viewModel.SelectedLayer);
                copyFeature.Tag = viewModel.SelectedLayer;
                if (isSelected)
                {
                    if (IsHighlightFeatureEnabled)
                    {
                        DataViewerHelper.HightlightSelectedFeature(copyFeature);
                    }
                    viewModel.SelectedLayerAdapter.SelectedFeatures.Add(selectedFeature.Id, selectedFeature);
                }
                else
                {
                    DataViewerHelper.RemoveHightlightFeature(copyFeature);
                    viewModel.SelectedLayerAdapter.SelectedFeatures.Remove(selectedFeature.Id);
                }
                viewModel.SelectedCount = viewModel.SelectedLayerAdapter.SelectedFeatures.Count;
            }
        }

        [Obfuscation]
        private void dg_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName.Equals(FeatureLayerAdapter.FeatureIdColumnName, StringComparison.Ordinal)
                || e.PropertyName.Equals(FeatureLayerAdapter.IsSelectedColumnName, StringComparison.Ordinal)
                || e.PropertyName.Equals(FeatureLayerAdapter.WkbColumnName, StringComparison.Ordinal))
            {
                e.Cancel = true;
            }
            else if (e.PropertyName.Contains(".") && e.Column is DataGridBoundColumn)
            {
                ((DataGridBoundColumn)e.Column).Binding = new Binding(string.Format("[{0}]", e.PropertyName));
            }
        }

        [Obfuscation]
        private void FindClick(object sender, RoutedEventArgs e)
        {
            btnFind.IsEnabled = false;

            UnhighlightAllRows();
            QuickFind();

            btnFind.IsEnabled = true;
        }

        [Obfuscation]
        private void TxtFind_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnFind.IsEnabled = !string.IsNullOrEmpty(txtFind.Text.Trim());
        }

        [Obfuscation]
        private void TxtFind_KeyDown(object sender, KeyEventArgs e)
        {
            if (btnFind.IsEnabled && e.Key == Key.Enter)
            {
                if (!isRetrived)
                {
                    FindClick(sender, e);
                }
                else if (viewModel.CurrentDataTable.Rows.Count == viewModel.RowCount
                    || viewModel.ShowSelectedFeatures)
                {
                    FindClick(sender, e);
                }
            }
        }

        [Obfuscation]
        private void dg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedIndex = dg.SelectedIndex;
            if (selectedIndex == -1) selectedIndex = 0;
        }

        private void QuickFind()
        {
            bool isFound = false;
            string key = txtFind.Text.ToUpperInvariant();
            for (int i = selectedIndex; i < dg.Items.Count; i++)
            {
                DataRowView dataRow = dg.Items[i] as DataRowView;
                string text = string.Join(",", dataRow.Row.ItemArray.OfType<string>()).ToUpperInvariant();
                if (text.Contains(key))
                {
                    isFound = true;

                    //dg.SelectedItem = dg.Items[i];
                    dg.ScrollIntoView(dg.Items[i]);
                    HighlightOrUnhighlightRecord(dataRow.Row);
                    lastDataRow = dataRow;

                    if (i + 1 < dg.Items.Count) selectedIndex = i + 1;
                    else selectedIndex = 0;
                    break;
                }
            }
            if (!isFound && selectedIndex == 0)
            {
                if (viewModel.CurrentDataTable.Rows.Count < viewModel.RowCount
                    && !viewModel.ShowSelectedFeatures)
                {
                    System.Windows.Forms.DialogResult result =
                        System.Windows.Forms.MessageBox.Show(string.Format(GisEditor.LanguageManager.GetStringResource("DataViewerUserControlLoadDataDescription"), txtFind.Text), GisEditor.LanguageManager.GetStringResource("DataViewerUserControlCannotFindLabel"), System.Windows.Forms.MessageBoxButtons.YesNo);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        RetrieveRestData();
                        isRetrived = true;
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(string.Format(GisEditor.LanguageManager.GetStringResource("DataViewerUserControlCannotFindTextLabel"), txtFind.Text), GisEditor.LanguageManager.GetStringResource("DataViewerUserControlCannotFindLabel"), System.Windows.Forms.MessageBoxButtons.OK);
                }
            }

            if (!isFound)
            {
                selectedIndex = 0;
            }
        }

        [Obfuscation]
        private void DataGridCell_Loaded(object sender, RoutedEventArgs e)
        {
            DataGridCell dataGridCell = sender as DataGridCell;
            if (dataGridCell != null)
            {
                if (dataGridCell.ActualWidth > 260)
                {
                    if (dataGridCell.Column.Width != 260)
                    {
                        dataGridCell.Column.Width = 260;
                    }
                }
                dataGridCell.ToolTip = "";
                dataGridCell.ToolTipOpening -= new ToolTipEventHandler(DataGridCell_ToolTipOpening);
                dataGridCell.ToolTipOpening += new ToolTipEventHandler(DataGridCell_ToolTipOpening);
                if (dataGridCell.Column.Header != null)
                {
                    string columnName = dataGridCell.Column.Header.ToString();
                    string currentId = ((DataRowView)dataGridCell.DataContext).Row.ItemArray[0].ToString();
                    if (viewModel.EditDataChanges.Count > 0)
                    {
                        foreach (var item in viewModel.EditDataChanges.Where(edc => edc.FeatureID.Equals(currentId) && edc.ColumnName.Equals(columnName)).ToArray())
                        {
                            item.GridCell = dataGridCell;
                        }
                    }
                }
            }
        }

        private void DataGridCell_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            DataGridCell dataGridCell = sender as DataGridCell;
            if (dataGridCell != null)
            {
                TextBlock textBlock = (dataGridCell.Content as TextBlock);
                if (textBlock != null)
                {
                    string value = textBlock.Text;
                    if (textBlock.ActualWidth > dataGridCell.ActualWidth)
                    {
                        if (dataGridCell.Column.Width != 260)
                        {
                            dataGridCell.Column.Width = 260;
                        }
                        TextBlock toolTipTextBlock = new TextBlock();
                        toolTipTextBlock.Text = value;
                        toolTipTextBlock.Width = 250;
                        toolTipTextBlock.TextWrapping = TextWrapping.Wrap;
                        dataGridCell.ToolTip = toolTipTextBlock;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(value.Trim()))
                        {
                            dataGridCell.ToolTip = value;
                        }
                        else
                        {
                            dataGridCell.ToolTip = null;
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        [Obfuscation]
        private void DataGridCell_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            DataGridCell dataGridCell = sender as DataGridCell;
            if (dataGridCell != null && dataGridCell.Column.Header != null)
            {
                string columnName = dataGridCell.Column.Header.ToString();
                cellValue = ((DataRowView)dataGridCell.DataContext).Row[columnName].ToString();
            }
        }

        [Obfuscation]
        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell dataGridCell = sender as DataGridCell;
            if (dataGridCell != null)
            {
                if (lastDataGridCell == dataGridCell)
                {
                    dataGridCell.Focus();
                    dg.BeginEdit();
                }
                lastDataGridCell = dataGridCell;
            }
        }

        [Obfuscation]
        private void SaveChangesClick(object sender, RoutedEventArgs e)
        {
            if (viewModel.EditDataChanges.Count > 0)
            {
                string[] ids = viewModel.EditDataChanges.Select(edc => edc.FeatureID).Distinct().ToArray();
                try
                {
                    TransactionResult result = null;
                    lock (viewModel.SelectedLayerAdapter)
                    {
                        var overlays = DataViewerViewModel.FindLayerOverlayContaining((Tag as GisEditorWpfMap), viewModel.SelectedLayer);
                        foreach (var overlay in overlays)
                        {
                            overlay.Close();
                        }
                        viewModel.ChangeCurrentLayerReadWriteMode(GeoFileReadWriteMode.ReadWrite);
                        viewModel.OpenFeatureLayer();
                        Collection<Feature> features = viewModel.SelectedLayer.QueryTools.GetFeaturesByIds(ids, viewModel.SelectedLayer.GetDistinctColumnNames());
                        var columns = viewModel.SelectedLayer.QueryTools.GetColumns();
                        foreach (var group in viewModel.EditDataChanges.GroupBy(edc => edc.FeatureID))
                        {
                            Feature resultFeature = features.FirstOrDefault(f => f.Id.Equals(group.Key));
                            if (resultFeature != null)
                            {
                                foreach (var item in group)
                                {
                                    resultFeature.ColumnValues[item.ColumnName] = item.NewValue;
                                }
                                var isColumnValueValid = CheckColumnValuesAreValid(resultFeature, columns);
                                if (!isColumnValueValid)
                                {
                                    var failureReasons = new Dictionary<string, string>();
                                    failureReasons.Add("InvalidValue", "Invalid value, please input a valid value.");
                                    result = new TransactionResult(0, 1, failureReasons, TransactionResultStatus.Failure);
                                    features.Remove(resultFeature);
                                    foreach (var item in group)
                                    {
                                        item.Undo();
                                    }
                                }
                            }
                        }

                        if (features.Count > 0)
                        {
                            viewModel.SelectedLayer.EditTools.BeginTransaction();
                            foreach (var feature in features)
                            {
                                viewModel.SelectedLayer.EditTools.Update(feature);
                            }
                            result = viewModel.SelectedLayer.EditTools.CommitTransaction();
                        }
                    }

                    if (result != null && result.TotalFailureCount != 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var keyValue in result.FailureReasons)
                        {
                            sb.AppendLine(keyValue.Value);
                            foreach (var item in viewModel.EditDataChanges.Where(edc => edc.FeatureID.Equals(keyValue.Key)))
                            {
                                item.Undo();
                            }
                        }

                        if (!isClosing)
                        {
                            if (result.TotalFailureCount > 1)
                            {
                                string message = "Many errors occur.";
                                GisEditorMessageBox messageBox = new GisEditorMessageBox(MessageBoxButton.OK);
                                messageBox.Message = message;
                                messageBox.ErrorMessage = sb.ToString();
                                messageBox.Title = GisEditor.LanguageManager.GetStringResource("FeatureAttibuteWindowUpdateFailedCaption");
                                messageBox.ShowDialog();
                            }
                            else
                            {
                                string message = sb.ToString();
                                if (message.Contains("The Value you input is too long"))
                                {
                                    message = "Value entered exceeds field length.";
                                }
                                GisEditorMessageBox messageBox = new GisEditorMessageBox(MessageBoxButton.OK);
                                messageBox.Message = message;
                                messageBox.ErrorMessage = sb.ToString();
                                messageBox.Title = GisEditor.LanguageManager.GetStringResource("FeatureAttibuteWindowUpdateFailedCaption");
                                messageBox.ShowDialog();
                            }
                        }
                    }
                    else
                    {
                        if (!viewModel.ChangedLayers.Contains(viewModel.SelectedLayer))
                        {
                            viewModel.ChangedLayers.Add(viewModel.SelectedLayer);
                        }
                    }
                }
                catch (UnauthorizedAccessException accessEx)
                {
                    System.Windows.Forms.MessageBox.Show(accessEx.Message, "Access Denied");
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, accessEx.Message, new ExceptionInfo(accessEx));
                }
                finally
                {
                    viewModel.CloseFeatureLayer();
                    viewModel.ChangeCurrentLayerReadWriteMode(GeoFileReadWriteMode.Read);
                }
                viewModel.EditDataChanges.Clear();
            }
        }

        [Obfuscation]
        private void CancelChangesClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in viewModel.EditDataChanges)
            {
                item.Undo();
            }
            viewModel.EditDataChanges.Clear();
        }

        [Obfuscation]
        private void EditDataChanges_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            bool isEnable = viewModel.EditDataChanges.Count > 0;
            btnCancelChagnes.IsEnabled = isEnable;
            btnSaveChanges.IsEnabled = isEnable;
        }

        [Obfuscation]
        private void dbfViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Z)
                {
                    EditDataChange resultEditDataChange = viewModel.EditDataChanges.LastOrDefault(edc => edc.ShortcutKeyMode == EditDataShortcutKeyMode.Undo);
                    if (resultEditDataChange != null)
                    {
                        resultEditDataChange.Undo();
                    }
                }
                else if (e.Key == Key.Y)
                {
                    EditDataChange resultEditDataChange = viewModel.EditDataChanges.FirstOrDefault(edc => edc.ShortcutKeyMode == EditDataShortcutKeyMode.Redo);
                    if (resultEditDataChange != null)
                    {
                        resultEditDataChange.Redo();
                    }
                }
            }
        }

        [Obfuscation]
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            OnHyperlinkClick(e);
            if (!e.Handled)
            {
                try
                {
                    Hyperlink link = (Hyperlink)sender;
                    bool isFile = false;
                    try
                    {
                        isFile = link.NavigateUri.IsFile;
                    }
                    catch
                    {
                    }

                    if (isFile)
                    {
                        Process.Start(link.NavigateUri.LocalPath);
                    }
                    else
                    {
                        Process.Start(link.NavigateUri.OriginalString);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Invalid Uri", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void CopyRowValueToClipboard()
        {
            StringBuilder copyText = new StringBuilder();

            foreach (var rowView in dg.SelectedItems.Cast<DataRowView>())
            {
                foreach (var item in rowView.Row.ItemArray.Skip(2))
                {
                    copyText.Append(item);
                    copyText.Append('\t');
                }
                copyText.Append(Environment.NewLine);
            }

            Clipboard.Clear();
            Clipboard.SetText(copyText.ToString());
        }
    }
}