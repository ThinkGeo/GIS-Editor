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
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class FeatureInfoUserControl : UserControl
    {
        //private const string filterStyleNameFormat = "\"{0}\" equals to \"{1}\"";
        private const string filterStyleNameFormat = "\"{0}\" contains \"{1}\"";
        private const string numericEqualConditionFormat = @"^{0}(\\.0+|0+|$)";
        //private const string textEqualConditionFormat = @"^{0}$";
        private const string textContainsConditionFormat = @".*{0}.*";

        private bool isEditing;

        public FeatureInfoUserControl()
        {
            InitializeComponent();
        }

        public void Refresh(Dictionary<FeatureLayer, Collection<Feature>> features)
        {
            ViewModel.IsBusy = true;
            ViewModel.RefreshFeatureEntities(features);
            ViewModel.IsBusy = false;
        }

        [Obfuscation]
        private void featuresList_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            FeatureViewModel entity = featuresList.SelectedValue as FeatureViewModel;
            if (entity != null)
            {
                e.Handled = true;
                ViewModel.SelectedEntity = entity;
                var overlay = GisEditor.SelectionManager.GetSelectionOverlay();
                if (overlay != null)
                {
                    overlay.StandOutHighlightFeatureLayer.InternalFeatures.Clear();
                    if (ViewModel.FeatureEntities.SelectMany(l => l.FoundFeatures).Any(en => en == entity))
                    {
                        overlay.StandOutHighlightFeatureLayer.InternalFeatures.Add(entity.Feature);
                    }
                    GisEditor.ActiveMap.Refresh(overlay);
                }
            }
            else
            {
                FeatureLayerViewModel layerEntity = featuresList.SelectedValue as FeatureLayerViewModel;
                if (layerEntity != null)
                {
                    var featureEntity = layerEntity.FoundFeatures.FirstOrDefault();
                    if (featureEntity != null)
                    {
                        featureEntity.IsSelected = true;
                        ViewModel.SelectedEntity = featureEntity;
                    }
                }
            }
        }

        [Obfuscation]
        private void featureNode_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement clickedElement = sender as FrameworkElement;
            if (clickedElement != null)
            {
                FeatureViewModel entity = clickedElement.DataContext as FeatureViewModel;
                if (entity != null)
                {
                    GisEditor.ActiveMap.CurrentExtent = entity.Feature.GetBoundingBox();
                    GisEditor.ActiveMap.Refresh();
                }
            }
        }

        [Obfuscation]
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.ContextMenu = GetAttributeContextMenu(e.Row);
        }

        private void EditColumnValue(object sender, RoutedEventArgs e)
        {
            featureInforGrid.IsReadOnly = false;
            DataGridRow dataGridRow = ((MenuItem)sender).Tag as DataGridRow;
            if (dataGridRow != null)
            {
                DataGridCell cell = GetCell(dataGridRow, 1);
                if (cell != null)
                {
                    cell.Focus();
                    featureInforGrid.BeginEdit();
                }
            }
            //featureInforGrid.IsReadOnly = true;
        }

        private static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
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

        private void CopyTableClick(object sender, RoutedEventArgs e)
        {
            string result = string.Empty;
            foreach (var item in featureInforGrid.Items)
            {
                var rowView = item as DataRowView;
                if (rowView != null)
                {
                    foreach (var itemView in rowView.Row.ItemArray)
                    {
                        result += itemView + "\t";
                    }

                    result += Environment.NewLine;
                }
            }
            Clipboard.SetDataObject(result);
        }

        private void CopyFieldValueClick(object sender, RoutedEventArgs e)
        {
            var rowView = featureInforGrid.SelectedValue as DataRowView;
            if (rowView != null)
            {
                object result = rowView.Row.ItemArray.LastOrDefault();
                if (result != null)
                {
                    Clipboard.SetDataObject(result.ToString());
                }
            }
        }

        private void CopySelectionClick(object sender, RoutedEventArgs e)
        {
            var rowView = featureInforGrid.SelectedValue as DataRowView;
            if (rowView != null)
            {
                string result = string.Empty;

                foreach (var item in rowView.Row.ItemArray)
                {
                    result += item + "\t";
                }
                Clipboard.SetDataObject(result);
            }
        }

        [Obfuscation]
        private void FeatureInforGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (isEditing) return;
            try
            {
                isEditing = true;
                TextBox editingTextBox = e.EditingElement as TextBox;
                DataRowView dataRowView = e.Row.Item as DataRowView;

                if (editingTextBox != null && dataRowView != null)
                {
                    string columnName = dataRowView[0].ToString();
                    string oldValue = dataRowView[1].ToString();
                    string newValue = editingTextBox.Text;
                    if (!string.Equals(oldValue, newValue) && GisEditor.ActiveMap.FeatureLayerEditOverlay != null)
                    {
                        TransactionResult result = null;
                        FeatureLayer selectedFeatureLayer = GisEditor.ActiveMap.FeatureLayerEditOverlay.EditTargetLayer;
                        lock (selectedFeatureLayer)
                        {
                            var overlays = FeatureInfoViewModel.FindLayerOverlayContaining(GisEditor.ActiveMap, selectedFeatureLayer);
                            foreach (var overlay in overlays)
                            {
                                overlay.Close();
                            }
                            ViewModel.ChangeCurrentLayerReadWriteMode(GeoFileReadWriteMode.ReadWrite, selectedFeatureLayer);
                            selectedFeatureLayer.SafeProcess(() =>
                            {
                                List<string> excludedIds = new List<string>();
                                if (selectedFeatureLayer.FeatureIdsToExclude.Count > 0)
                                {
                                    excludedIds.AddRange(selectedFeatureLayer.FeatureIdsToExclude);
                                    selectedFeatureLayer.FeatureIdsToExclude.Clear();
                                }

                                Feature feature = selectedFeatureLayer.QueryTools
                                    .GetFeatureById(ViewModel.SelectedEntity.FeatureId, selectedFeatureLayer.FeatureSource.GetColumns(GettingColumnsType.FeatureSourceOnly).Select(c => c.ColumnName).Distinct());

                                if (excludedIds.Count > 0)
                                {
                                    excludedIds.ForEach(excludedId => selectedFeatureLayer.FeatureIdsToExclude.Add(excludedId));
                                }

                                feature.ColumnValues[columnName] = newValue;

                                Collection<FeatureSourceColumn> checkColumns = new Collection<FeatureSourceColumn>();
                                var column = selectedFeatureLayer.QueryTools.GetColumns().FirstOrDefault(c => c.ColumnName.Equals(columnName));
                                if (column != null)
                                {
                                    checkColumns.Add(column);
                                }
                                var isColumnValueValid = CheckColumnValuesAreValid(feature, checkColumns);
                                if (isColumnValueValid)
                                {
                                    try
                                    {
                                        selectedFeatureLayer.EditTools.BeginTransaction();
                                        selectedFeatureLayer.EditTools.Update(feature);
                                        result = selectedFeatureLayer.EditTools.CommitTransaction();
                                    }
                                    catch
                                    {
                                        selectedFeatureLayer.EditTools.RollbackTransaction();
                                    }
                                }
                                else
                                {
                                    var failureReasons = new Dictionary<string, string>();
                                    failureReasons.Add("InvalidValue", "Invalid value, please input a valid value.");
                                    result = new TransactionResult(0, 1, failureReasons, TransactionResultStatus.Failure);
                                }
                            });
                        }

                        if (result.TotalFailureCount != 0)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (var keyValue in result.FailureReasons)
                            {
                                sb.AppendLine(keyValue.Value);
                            }

                            editingTextBox.Text = oldValue;
                            System.Windows.Forms.MessageBox.Show(sb.ToString(), "Update Failed");
                        }
                        else
                        {
                            foreach (DataRow dataRow in ViewModel.SelectedDataView.Table.Rows)
                            {
                                if (string.Equals(dataRow[0].ToString(), columnName))
                                {
                                    dataRow["Value"] = new Uri(newValue, UriKind.RelativeOrAbsolute);
                                    break;
                                }
                            }
                            foreach (var highlightFeature in GisEditor.SelectionManager.GetSelectedFeatures(selectedFeatureLayer))
                            {
                                string id = highlightFeature.Id;
                                int i = id.IndexOf("[TG]", StringComparison.Ordinal);
                                if (i >= 0)
                                {
                                    id = id.Substring(0, i);
                                }
                                if (id.Equals(ViewModel.SelectedEntity.FeatureId))
                                {
                                    highlightFeature.ColumnValues[columnName] = newValue;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException accessEx)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, accessEx.Message, new ExceptionInfo(accessEx));
                System.Windows.Forms.MessageBox.Show(accessEx.Message, "Access Denied");
            }
            finally
            {
                if (GisEditor.ActiveMap.FeatureLayerEditOverlay != null)
                {
                    ViewModel.ChangeCurrentLayerReadWriteMode(GeoFileReadWriteMode.Read, GisEditor.ActiveMap.FeatureLayerEditOverlay.EditTargetLayer);
                }
                featureInforGrid.IsReadOnly = true;
                isEditing = false;
                if (e.EditingElement.IsMouseCaptured)
                {
                    e.EditingElement.ReleaseMouseCapture();
                }
                //GisEditor.UIManager.RefreshPlugins();
            }
        }

        private bool CheckColumnValuesAreValid(Feature feature, IEnumerable<FeatureSourceColumn> columns)
        {
            bool isValid = true;
            foreach (var column in columns)
            {
                if (!feature.ColumnValues.ContainsKey(column.ColumnName)) continue;

                string columnValue = feature.ColumnValues[column.ColumnName];
                if (string.IsNullOrEmpty(columnValue)) continue;
                if (column.TypeName.Equals(DbfColumnType.Numeric.ToString()))
                {
                    int result = 0;
                    if (!Int32.TryParse(columnValue, out result))
                    {
                        isValid = false;
                        break;
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
        private void FeatureInforGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (!e.EditingElement.IsMouseCaptured)
            {
                e.EditingElement.CaptureMouse();
            }
        }

        [Obfuscation]
        private void featureNode_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var featrueEntity = sender.GetDataContext<FeatureViewModel>();
            if (featrueEntity != null) featrueEntity.IsSelected = true;
            var treeViewItem = sender as TreeViewItem;
            if (treeViewItem != null) treeViewItem.ContextMenu.IsOpen = true;
        }

        [Obfuscation]
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            FeatureViewModel entity = featuresList.SelectedValue as FeatureViewModel;
            if (entity == null)
            {
                FeatureLayerViewModel layerEntity = featuresList.SelectedValue as FeatureLayerViewModel;
                if (layerEntity != null)
                    entity = layerEntity.FoundFeatures.FirstOrDefault();
            }

            FeatureWktWindow window = new FeatureWktWindow(entity)
            {
                WindowStyle = WindowStyle.ToolWindow
            };

            window.ShowDialog();
        }

        [Obfuscation]
        private void MenuItemDeselect_Click(object sender, RoutedEventArgs e)
        {
            FeatureViewModel entity = featuresList.SelectedValue as FeatureViewModel;
            if (entity != null)
            {
                var selectedEntityLayer = featuresList.Items.OfType<FeatureLayerViewModel>().FirstOrDefault(l => l.LayerName == entity.LayerName);
                selectedEntityLayer.FoundFeatures.Remove(entity);

                var overlay = GisEditor.ActiveMap.SelectionOverlay;
                if (overlay != null)
                {
                    e.Handled = true;
                    var selectedLayer = entity.OwnerFeatureLayer;
                    if (selectedLayer != null)
                    {
                        var key = entity.Feature.Id + SelectionTrackInteractiveOverlay.FeatureIdSeparator + selectedLayer.GetHashCode();
                        overlay.HighlightFeatureLayer.InternalFeatures.Remove(key);
                        overlay.StandOutHighlightFeatureLayer.Clear();
                        ViewModel.SelectedEntity = null;
                        overlay.HighlightFeatureLayer.BuildIndex();

                        GisEditor.ActiveMap.Refresh(overlay);

                        Dictionary<FeatureLayer, Collection<Feature>> featureGroup = new Dictionary<FeatureLayer, Collection<Feature>>();

                        IEnumerable<FeatureLayerViewModel> layers = featuresList.Items.OfType<FeatureLayerViewModel>().Where(l => l.LayerName != entity.LayerName);

                        foreach (var layer in layers)
                        {
                            FeatureViewModel featureViewModel = layer.FoundFeatures.FirstOrDefault();
                            if (featureViewModel != null)
                            {
                                FeatureLayer featureLayer = featureViewModel.OwnerFeatureLayer;
                                featureGroup[featureLayer] = new Collection<Feature>();
                                foreach (var item in layer.FoundFeatures)
                                {
                                    featureGroup[featureLayer].Add(item.Feature);
                                }
                            }
                        }

                        if (selectedEntityLayer.FoundFeatures.Count > 0)
                        {
                            featureGroup[selectedLayer] = new Collection<Feature>();

                            foreach (var featureViewModel in selectedEntityLayer.FoundFeatures)
                            {
                                featureGroup[selectedLayer].Add(featureViewModel.Feature);
                            }
                        }
                        entity.IsSelected = false;

                        Refresh(featureGroup);
                    }
                }
            }
        }

        private DataGridCell GetCell(DataGridRow row, int column)
        {
            DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);

            // try to get the cell but it may possibly be virtualized
            DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
            if (cell == null)
            {
                // now try to bring into view and retreive the cell
                cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
            }
            return cell;
        }

        private MenuItem GetMenuItem(string header, RoutedEventHandler handler, Object obj)
        {
            MenuItem menuItem = new MenuItem();
            menuItem.Tag = obj;
            menuItem.Header = header;
            menuItem.Click += handler;
            return menuItem;
        }

        [Obfuscation]
        private void Row_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            DataGridRow row = (DataGridRow)sender;
            MenuItem editMenuItem = row.ContextMenu.Items[row.ContextMenu.Items.Count - 1] as MenuItem;
            if (editMenuItem != null && GisEditor.ActiveMap.FeatureLayerEditOverlay != null)
            {
                FeatureLayer editingLayer = GisEditor.ActiveMap.FeatureLayerEditOverlay.EditTargetLayer;
                if (!(editingLayer != null && ViewModel.SelectedEntity.LayerName.Equals(editingLayer.Name)))
                {
                    editMenuItem.Visibility = Visibility.Collapsed;
                }
                else
                {
                    editMenuItem.Visibility = Visibility.Visible;
                }
            }
        }

        private void QuickFilterClick(object sender, RoutedEventArgs e)
        {
            var rowView = featureInforGrid.SelectedValue as DataRowView;
            if (rowView != null)
            {
                string columnName = rowView.Row.ItemArray[0].ToString();
                string columnValue = rowView.Row.ItemArray[1].ToString();

                if (rowView.Row.ItemArray.Length == 3)
                {
                    columnValue = rowView.Row.ItemArray[2].ToString();
                }

                AddQuickFilterStyle(columnName, columnValue);
            }
        }

        private ContextMenu GetAttributeContextMenu(DataGridRow dataGridRow)
        {
            var attributeContextMenu = new ContextMenu();
            attributeContextMenu.Items.Add(GetMenuItem("Copy Field Value", CopyFieldValueClick, dataGridRow));
            attributeContextMenu.Items.Add(GetMenuItem("Copy Selection", CopySelectionClick, dataGridRow));
            attributeContextMenu.Items.Add(GetMenuItem("Copy Table", CopyTableClick, dataGridRow));
            attributeContextMenu.Items.Add(GetMenuItem("Quick Filters", QuickFilterClick, dataGridRow));

            MenuItem editMenuItem = GetMenuItem("Edit", EditColumnValue, dataGridRow);
            attributeContextMenu.Items.Add(editMenuItem);
            return attributeContextMenu;
        }

        public void AddQuickFilterStyle(string columnName, string columnValue)
        {
            var styleProvider = GisEditor.StyleManager.GetActiveStylePlugins<FilterStylePlugin>().FirstOrDefault();
            var styleArguments = new StyleBuilderArguments();
            styleArguments.FeatureLayer = ViewModel.SelectedEntity.OwnerFeatureLayer;
            var featureLayerPlugin = GisEditor.LayerManager.GetLayerPlugins(styleArguments.FeatureLayer.GetType()).FirstOrDefault() as FeatureLayerPlugin;
            if (featureLayerPlugin != null)
            {
                styleArguments.AvailableStyleCategories = StylePluginHelper.GetStyleCategoriesByFeatureLayer(styleArguments.FeatureLayer);
                styleArguments.FromZoomLevelIndex = 1;
                styleArguments.ToZoomLevelIndex = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count(z => z.GetType() == typeof(ZoomLevel));
                styleArguments.FillRequiredColumnNames();
                styleArguments.AppliedCallback = styleResults =>
                {
                    if (styleResults.CompositeStyle != null)
                    {
                        ZoomLevelHelper.AddStyleToZoomLevels(styleResults.CompositeStyle, styleResults.FromZoomLevelIndex, styleResults.ToZoomLevelIndex, styleArguments.FeatureLayer.ZoomLevelSet.CustomZoomLevels);
                        TileOverlay tileOverlay = GisEditor.ActiveMap.GetOverlaysContaining(styleArguments.FeatureLayer).FirstOrDefault();
                        if (tileOverlay != null)
                        {
                            tileOverlay.Invalidate();
                        }
                        GisEditor.UIManager.BeginRefreshPlugins(new RefreshArgs(GisEditor.LayerListManager.SelectedLayerListItem, RefreshArgsDescription.ApplyStyleDescription));
                    }
                };
                var defaultFilterStyle = styleProvider.GetDefaultStyle() as FilterStyle;
                defaultFilterStyle.Name = string.Format(filterStyleNameFormat, columnName, columnValue);
                FilterCondition filterCondition = new FilterCondition();
                filterCondition.ColumnName = columnName;
                filterCondition.Name = defaultFilterStyle.Name;
                bool isNumericColumn = false;
                styleArguments.FeatureLayer.SafeProcess(() =>
                {
                    Collection<FeatureSourceColumn> columns = styleArguments.FeatureLayer.FeatureSource.GetColumns();
                    var resultColumn = columns.FirstOrDefault(c => c.ColumnName.Equals(columnName, StringComparison.InvariantCultureIgnoreCase));
                    if (resultColumn != null)
                    {
                        isNumericColumn = resultColumn.TypeName.Equals("DOUBLE", StringComparison.InvariantCultureIgnoreCase) || resultColumn.TypeName.Equals("INTEGER", StringComparison.InvariantCultureIgnoreCase) || resultColumn.TypeName.Equals("FLOAT", StringComparison.InvariantCultureIgnoreCase);
                    }
                });

                //filterCondition.Expression = string.Format(isNumericColumn ? numericEqualConditionFormat : textContainsConditionFormat, columnValue.ToLowerInvariant());

                filterCondition.RegexOptions = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                filterCondition.Expression = string.Format(isNumericColumn ? numericEqualConditionFormat : textContainsConditionFormat, columnValue);

                defaultFilterStyle.Conditions.Add(filterCondition);
                var componentStyle = new CompositeStyle(defaultFilterStyle) { Name = styleArguments.FeatureLayer.Name };
                styleArguments.StyleToEdit = componentStyle;
                var styleBuilder = GisEditor.StyleManager.GetStyleBuiderUI(styleArguments);
                if (styleBuilder.ShowDialog().GetValueOrDefault())
                    styleArguments.AppliedCallback(styleBuilder.StyleBuilderResult);
            }
        }

        [Obfuscation]
        private void DataGridScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        [Obfuscation]
        private void FeatureInforGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.Column.Header.Equals("RealValue"))
            {
                e.Column.Visibility = Visibility.Collapsed;
            }
            else if (e.Column.Header.Equals("Value"))
            {
                System.Windows.Style style = new System.Windows.Style();
                style.TargetType = typeof(DataGridCell);
                DataTrigger trigger = new DataTrigger();
                Binding binding = new Binding();
                binding.Path = new PropertyPath(".");
                binding.Converter = new UriToBooleanConverter();
                trigger.Binding = binding;
                trigger.Value = true;
                Setter foregroundSetter = new Setter(ForegroundProperty, new SolidColorBrush(Colors.Blue));
                Setter cursorSetter = new Setter(CursorProperty, System.Windows.Input.Cursors.Hand);
                Setter isHitSetter = new Setter(IsHitTestVisibleProperty, true);
                trigger.Setters.Add(foregroundSetter);
                trigger.Setters.Add(cursorSetter);
                trigger.Setters.Add(isHitSetter);
                style.Triggers.Add(trigger);
                e.Column.CellStyle = style;
            }
        }

        private void EventSetter_OnHandler(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DataRowView view = (DataRowView)featureInforGrid.SelectedItem;
                string columnName = featureInforGrid.CurrentColumn.Header.ToString();
                if (columnName == "Value")
                {
                    string uri = view.Row[columnName].ToString();
                    Process.Start(uri);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}