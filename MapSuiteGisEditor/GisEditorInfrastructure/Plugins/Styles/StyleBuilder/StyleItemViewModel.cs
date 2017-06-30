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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class StyleItemViewModel : ViewModelBase
    {
        private bool isRenaming;
        private bool isSelected;
        private bool isExpanded;
        private StyleLayerListItem styleItem;
        private StyleBuilderViewModel styleBuilder;
        private ContextMenu contextMenu;
        private StyleBuilderArguments styleArguments;
        private StyleItemViewModel parentViewModel;
        private ObservableCollection<StyleItemViewModel> styleItemViewModels;
        private static StyleCategories tmpStyleCategories;

        public StyleItemViewModel(StyleLayerListItem styleItem, StyleBuilderArguments styleArguments)
        {
            this.styleItem = styleItem;
            this.styleArguments = styleArguments;
            this.InitializeSubViewModels();
            this.InitializeContextMenuItems();
        }

        public Visibility CheckBoxVisibility
        {
            get { return styleItem.ConcreteObject is Styles.Style ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool IsActive
        {
            get
            {
                Styles.Style style = styleItem.ConcreteObject as Styles.Style;
                bool isActive = true;
                if (style != null)
                {
                    isActive = style.IsActive;
                }
                return isActive;
            }
            set
            {
                Styles.Style style = styleItem.ConcreteObject as Styles.Style;
                if (style != null)
                {
                    style.IsActive = value;
                }
                RaisePropertyChanged(() => IsActive);
            }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                RaisePropertyChanged(() => IsSelected);
            }
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { isExpanded = value; }
        }

        public StyleItemViewModel ParentViewModel
        {
            get { return parentViewModel; }
            set { parentViewModel = value; }
        }

        public ObservableCollection<StyleItemViewModel> StyleItemViewModels
        {
            get { return styleItemViewModels; }
        }

        public StyleLayerListItem StyleItem
        {
            get { return styleItem; }
        }

        public string Name
        {
            get
            {
                return styleItem.Name;
            }
            set { styleItem.Name = value; }
        }

        internal StyleBuilderViewModel StyleBuilder
        {
            get { return styleBuilder; }
            set { styleBuilder = value; }
        }

        public ContextMenu ContextMenu
        {
            get { return contextMenu; }
        }

        public BitmapSource PreviewSource
        {
            get { return styleItem.GetPreviewSource(31, 31); }
        }

        public bool IsRenaming
        {
            get { return isRenaming; }
            set
            {
                isRenaming = value;
                RaisePropertyChanged(() => HeaderLabelVisibility);
                RaisePropertyChanged(() => RenameInputVisibility);
            }
        }

        public bool CanAddStyle
        {
            get
            {
                if (StyleItem.Parent is StyleLayerListItem)
                {
                    return StyleItem.CanAddInnerStyle || ((StyleLayerListItem)StyleItem.Parent).CanAddInnerStyle;
                }
                else return false;
            }
        }

        public Visibility HeaderLabelVisibility
        {
            get { return IsRenaming ? Visibility.Collapsed : Visibility.Visible; }
        }

        public Visibility RenameInputVisibility
        {
            get { return IsRenaming ? Visibility.Visible : Visibility.Collapsed; }
        }

        public void CommitRenaming()
        {
            if (IsRenaming)
            {
                IsRenaming = false;
                RaisePropertyChanged(() => Name);
                var styleItemUI = StyleItem.GetUI(styleArguments);
                if (styleItemUI != null) StyleItem.UpdateUI(styleItemUI);
            }
        }

        public static StyleItemViewModel FindStyleItemViewModel(Collection<StyleItemViewModel> itemViewModels, StyleLayerListItem itemToFind)
        {
            foreach (var vm in itemViewModels)
            {
                if (vm.StyleItem == itemToFind) return vm;
                else if (vm.StyleItemViewModels.Count > 0)
                {
                    var subVM = FindStyleItemViewModel(vm.StyleItemViewModels, itemToFind);
                    if (subVM != null) return subVM;
                }
            }

            return null;
        }

        private string GetImageUri(string imageFileName)
        {
            return "pack://application:,,,/GisEditorInfrastructure;component/Images/" + imageFileName;
        }

        private void InitializeContextMenuItems()
        {
            if (styleArguments.AvailableStyleCategories != StyleCategories.None
                && styleItem.Parent != null)
            {
                this.contextMenu = new ContextMenu();

                if (StyleItem.ConcreteObject is FilterStyle)
                {
                    this.contextMenu.Items.Add(GetMenuItem("View filtered data", ViewFilteredData, "/GisEditorPluginCore;component/Images/styles_filterarealinepoint.png"));
                }

                if (StyleItem.CanAddInnerStyle)
                {
                    var menuItem = GetAddStyleMenuItem(styleArguments, this);
                    this.contextMenu.Items.Add(menuItem);
                }

                this.contextMenu.Items.Add(GetMenuItem("Move up", MoveUpClick, GetImageUri("moveUp.png")));
                this.contextMenu.Items.Add(GetMenuItem("Move down", MoveDownClick, GetImageUri("moveDown.png")));
                this.contextMenu.Items.Add(GetMenuItem("Move to top", MoveToTopClick, GetImageUri("toTop.png")));
                this.contextMenu.Items.Add(GetMenuItem("Move to bottom", MoveToBottomClick, GetImageUri("toBottom.png")));
                if (styleItem.ConcreteObject is Styles.Style)
                {
                    this.contextMenu.Items.Add(new Separator());
                    this.contextMenu.Items.Add(GetMenuItem("Insert from Library...", InsertFromLibrary, GetImageUri("insert_from_library.png"), !styleArguments.IsSubStyleReadonly));
                    this.contextMenu.Items.Add(GetMenuItem("Replace from Library...", ReplaceFromLibrary, GetImageUri("replace_from_library.png"), !styleArguments.IsSubStyleReadonly));
                    this.contextMenu.Items.Add(new Separator());
                }
                if (StyleItem.ConcreteObject is Styles.Style)
                {
                    this.contextMenu.Items.Add(GetMenuItem("Duplicate", DuplicateClick, "/GisEditorInfrastructure;component/Images/duplicate.png", !styleArguments.IsSubStyleReadonly));
                }
                if (StyleItem.CanRename)
                {
                    this.contextMenu.Items.Add(GetMenuItem("Rename", RenameClick, GetImageUri("rename.png"), !styleArguments.IsSubStyleReadonly));
                }

                this.contextMenu.Items.Add(GetMenuItem("Remove", RemoveItemClick, GetImageUri("unload.png"), !styleArguments.IsSubStyleReadonly));
            }
            else if (styleItem is ComponentStyleItem)
            {
                this.contextMenu = new ContextMenu();

                MenuItem addStyleMenuItem = StyleItemViewModel.GetAddStyleMenuItem(styleArguments, this);
                if (addStyleMenuItem.Items.Count > 0)
                {
                    addStyleMenuItem.IsEnabled = !styleArguments.IsSubStyleReadonly;
                    this.contextMenu.Items.Add(addStyleMenuItem);
                }

                this.contextMenu.Items.Add(new Separator());
                this.contextMenu.Items.Add(GetMenuItem("Insert from Library...", InsertFromLibrary, GetImageUri("insert_from_library.png"), !styleArguments.IsSubStyleReadonly));
                this.contextMenu.Items.Add(GetMenuItem("Replace from Library...", ReplaceFromLibrary, GetImageUri("replace_from_library.png")));
                this.contextMenu.Items.Add(new Separator());

                var renameMenuItem = new MenuItem();
                renameMenuItem.Header = GisEditor.LanguageManager.GetStringResource("SytleBuilderWindowRenameLabel");
                renameMenuItem.Click += RenameClick;
                renameMenuItem.Icon = new Image
                {
                    Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/rename.png", UriKind.Relative)),
                    Width = 16,
                    Height = 16
                };

                this.contextMenu.Items.Add(renameMenuItem);
                this.contextMenu.Items.Add(GetMenuItem("Save Style", SaveStyleClick, GetImageUri("Export.png")));
            }
        }

        internal static MenuItem GetAddStyleMenuItem(StyleBuilderArguments styleArguments, StyleItemViewModel currentStyleItemViewModel)
        {
            var menuItem = new MenuItem();
            menuItem.Header = GisEditor.LanguageManager.GetStringResource("StyleBuilderWindowAddStyleLabel");
            menuItem.Icon = new Image
            {
                Source = new BitmapImage(new Uri("/GisEditorInfrastructure;component/Images/addStyle.png", UriKind.Relative)),
                Width = 16,
                Height = 16
            };

            var styleCategoryPairs = Enum.GetValues(typeof(StyleCategories)).Cast<StyleCategories>()
                .Where(c => c != StyleCategories.None)
                .Select(c => new { StyleCategory = c, Name = c + " Style" });

            var currentStyleCategory = currentStyleItemViewModel.StyleItem.GetRestrictStyleCategories();
            if (currentStyleCategory == StyleCategories.None) currentStyleCategory = styleArguments.AvailableStyleCategories;

            var hasCompositedStyle = currentStyleCategory.HasFlag(StyleCategories.Composite);
            var styleCategoriesToAdd = styleCategoryPairs.Where(s => s.StyleCategory != StyleCategories.Composite)
                .Where(item => currentStyleCategory.HasFlag(item.StyleCategory)).ToArray();
            if (styleCategoriesToAdd.Length > 2)
            {
                foreach (var item in styleCategoriesToAdd)
                {
                    if (item.StyleCategory == StyleCategories.Point || item.StyleCategory == StyleCategories.Line || item.StyleCategory == StyleCategories.Area)
                    {
                        MenuItem groupMenuItem = new MenuItem();
                        groupMenuItem.Header = item.StyleCategory.ToString();
                        AddSubMenuItemsForStyle(styleArguments, item.StyleCategory, item.Name, groupMenuItem, currentStyleItemViewModel, hasCompositedStyle);
                        menuItem.Items.Add(groupMenuItem);
                    }
                    else
                    {
                        AddSubMenuItemsForStyle(styleArguments, item.StyleCategory, item.Name, menuItem, currentStyleItemViewModel, hasCompositedStyle);
                    }
                    menuItem.Items.Add(new Separator());
                }

                var styleCatagories = styleCategoriesToAdd.Select(s => s.StyleCategory).ToList();
                if (styleCatagories.Contains(StyleCategories.Point) && styleCatagories.Contains(StyleCategories.Line) && styleCatagories.Contains(StyleCategories.Area))
                {
                    string shareStyleText = "Share Styles";
                    MenuItem groupMenuItem = new MenuItem();
                    groupMenuItem.Header = shareStyleText;
                    AddSubMenuItemsForStyle(styleArguments, StyleCategories.Composite, shareStyleText, groupMenuItem, currentStyleItemViewModel, true);
                    menuItem.Items.Add(groupMenuItem);
                    menuItem.Items.Add(new Separator());
                }
            }
            else
            {
                foreach (var item in styleCategoriesToAdd)
                {
                    AddSubMenuItemsForStyle(styleArguments, item.StyleCategory, item.Name, menuItem, currentStyleItemViewModel, hasCompositedStyle);
                    menuItem.Items.Add(new Separator());
                }
            }
            if (menuItem.Items.Count > 0) menuItem.Items.RemoveAt(menuItem.Items.Count - 1);

            return menuItem;
        }

        private static void AddSubMenuItemsForStyle(StyleBuilderArguments styleArguments, StyleCategories styleCategories
            , string categoryName
            , MenuItem rootMenuItem
            , StyleItemViewModel currentStyleItemViewModel
            , bool hasCompositedStyle)
        {
            if (hasCompositedStyle
                   && styleCategories != StyleCategories.None
                   && !styleCategories.HasFlag(StyleCategories.Label))
            {
                styleCategories = styleCategories | StyleCategories.Composite;
            }

            var plugins = GisEditor.StyleManager.GetStylePlugins(styleCategories);
            if (plugins.Count > 0)
            {
                var menuItems = plugins.Select(plugin =>
                {
                    if (plugin.RequireColumnNames && styleArguments.ColumnNames.Count == 0)
                        return null;

                    MenuItem subMenuItem = new MenuItem();
                    subMenuItem.Header = plugin.Name;
                    subMenuItem.Icon = new Image { Source = plugin.SmallIcon, Width = 16, Height = 16 };
                    subMenuItem.CommandParameter = new Tuple<StylePlugin, StyleCategories>(plugin, styleCategories);
                    subMenuItem.Command = new RelayCommand<Tuple<StylePlugin, StyleCategories>>(commandParameter =>
                    {
                        StylePlugin tmpStylePlugin = commandParameter.Item1;
                        Styles.Style style = tmpStylePlugin.GetDefaultStyle();
                        style.Name = tmpStylePlugin.Name;

                        StyleLayerListItem styleItem = GisEditor.StyleManager.GetStyleLayerListItem(style);
                        if (styleItem != null)
                        {
                            currentStyleItemViewModel.StyleItem.Children.Insert(0, styleItem);
                            currentStyleItemViewModel.StyleItem.UpdateConcreteObject();
                            var styleItemUI = currentStyleItemViewModel.StyleItem.GetUI(GetDuplicateStyleArguments(styleCategories, styleArguments));
                            if (styleItemUI != null) currentStyleItemViewModel.StyleItem.UpdateUI(styleItemUI);

                            var addedStyleItemViewModel = currentStyleItemViewModel.StyleItemViewModels.FirstOrDefault(vm => vm.StyleItem == styleItem);
                            if (addedStyleItemViewModel != null)
                            {
                                if (commandParameter.Item2 == StyleCategories.Label)
                                {
                                    addedStyleItemViewModel.IsSelected = true;
                                }
                                else
                                {
                                    StyleItemViewModel rootStyleItemViewModel = GetRootViewModel(currentStyleItemViewModel);

                                    tmpStyleCategories = commandParameter.Item2;
                                    var tmpStyleArguments = rootStyleItemViewModel.StyleBuilder.StyleArguments;
                                    rootStyleItemViewModel.StyleBuilder.PropertyChanged -= StyleBuilder_PropertyChanged;
                                    rootStyleItemViewModel.StyleBuilder.PropertyChanged += StyleBuilder_PropertyChanged;

                                    addedStyleItemViewModel.IsSelected = true;

                                    rootStyleItemViewModel.StyleBuilder.PropertyChanged -= StyleBuilder_PropertyChanged;
                                    rootStyleItemViewModel.StyleBuilder.StyleArguments = tmpStyleArguments;
                                }
                            }
                        }
                    });
                    return subMenuItem;
                }).Where(i => i != null).ToArray();

                foreach (var item in menuItems)
                {
                    rootMenuItem.Items.Add(item);
                }
            }
        }

        private static StyleItemViewModel GetRootViewModel(StyleItemViewModel currentStyleItemViewModel)
        {
            if (currentStyleItemViewModel.StyleBuilder != null)
            {
                return currentStyleItemViewModel;
            }
            else
            {
                return GetRootViewModel(currentStyleItemViewModel.ParentViewModel);
            }
        }

        private static void StyleBuilder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedStyleItem")
            {
                ((StyleBuilderViewModel)sender).StyleArguments = GetDuplicateStyleArguments(tmpStyleCategories, ((StyleBuilderViewModel)sender).StyleArguments);
            }
        }

        private static StyleBuilderArguments GetDuplicateStyleArguments(StyleCategories styleCategories, StyleBuilderArguments styleArguments)
        {
            StyleBuilderArguments newStyleBuilderArguments = new StyleBuilderArguments
            {
                AppliedCallback = styleArguments.AppliedCallback,
                AvailableStyleCategories = styleCategories | StyleCategories.Label | StyleCategories.Composite,
                AvailableUIElements = styleArguments.AvailableUIElements,
                FeatureLayer = styleArguments.FeatureLayer,
                FromZoomLevelIndex = styleArguments.FromZoomLevelIndex,
                SelectedConcreteObject = styleArguments.SelectedConcreteObject,
                StyleToEdit = styleArguments.StyleToEdit,
                ToZoomLevelIndex = styleArguments.ToZoomLevelIndex
            };
            newStyleBuilderArguments.FillRequiredColumnNames();

            return newStyleBuilderArguments;
        }

        private static MenuItem GetMenuItem(string header, RoutedEventHandler handler, string uri, bool isEnabled = true)
        {
            MenuItem menuItem = new MenuItem
            {
                IsEnabled = isEnabled,
                Header = header
            };
            Image image = new Image();
            menuItem.Icon = image;
            image.Source = new BitmapImage(new Uri(uri, UriKind.RelativeOrAbsolute));
            image.Width = 16;
            image.Height = 16;
            menuItem.Click += handler;
            return menuItem;
        }

        private void DuplicateClick(object sender, RoutedEventArgs e)
        {
            StyleLayerListItem duplicateStyleItem = StyleItem.CloneDeep();
            if (duplicateStyleItem == null) return;

            var parent = StyleItem.Parent as StyleLayerListItem;
            if (parent == null) return;

            parent.Children.Insert(0, duplicateStyleItem);
            parent.UpdateConcreteObject();
            if (ParentViewModel.StyleItemViewModels.Count > 0) ParentViewModel.StyleItemViewModels[0].IsSelected = true;
        }

        private void SaveStyleClick(object sender, RoutedEventArgs e)
        {
            var compositeStyle = styleItem.ConcreteObject as CompositeStyle;
            if (compositeStyle != null && StyleBuilder != null && GisEditor.ActiveMap != null)
            {
                var count = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count;
                if (count > styleBuilder.FromZoomLevelIndex - 1 && count > styleBuilder.ToZoomLevelIndex - 1)
                {
                    var upperScale = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels[styleBuilder.FromZoomLevelIndex - 1].Scale;
                    var lowerScale = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels[styleBuilder.ToZoomLevelIndex - 1].Scale;
                    GisEditor.StyleManager.SaveStyleToLibrary(compositeStyle, lowerScale, upperScale);
                }
            }
        }

        private void RemoveItemClick(object sender, RoutedEventArgs e)
        {
            var parent = styleItem.Parent as StyleLayerListItem;
            if (parent != null)
            {
                parent.Children.Remove(styleItem);
                parent.UpdateConcreteObject();
                RefreshUI(parent);
            }
        }

        private void RenameClick(object sender, RoutedEventArgs e)
        {
            IsRenaming = true;
        }

        private void ViewFilteredData(object sender, RoutedEventArgs e)
        {
            FilterStyle filterStyle = StyleItem.ConcreteObject as FilterStyle;
            if (filterStyle != null)
            {
                ShowFilteredData(styleArguments.FeatureLayer, filterStyle.Conditions);
            }
        }

        private static void ShowFilteredData(FeatureLayer featureLayer, Collection<FilterCondition> conditions)
        {
            Collection<FeatureLayer> layers = new Collection<FeatureLayer>();
            InMemoryFeatureLayer layer = new InMemoryFeatureLayer();
            layer.Name = featureLayer.Name;
            layer.Open();

            //foreach (var item in featureLayer.FeatureSource.LinkExpressions)
            //{
            //    layer.FeatureSource.LinkExpressions.Add(item);
            //}
            //foreach (var item in featureLayer.FeatureSource.LinkSources)
            //{
            //    layer.FeatureSource.LinkSources.Add(item);
            //}
            layer.Columns.Clear();

            featureLayer.SafeProcess(() =>
            {
                Collection<FeatureSourceColumn> columns = featureLayer.FeatureSource.GetColumns();
                foreach (var column in columns)
                {
                    layer.Columns.Add(column);
                }

                Collection<Feature> resultFeatures = featureLayer.QueryTools.GetAllFeatures(ReturningColumnsType.AllColumns);
                foreach (var condition in conditions)
                {
                    resultFeatures = condition.GetMatchingFeatures(resultFeatures);
                }

                foreach (Feature feature in resultFeatures)
                {
                    layer.InternalFeatures.Add(feature);
                }
            });

            layer.ZoomLevelSet = featureLayer.ZoomLevelSet;
            layers.Add(layer);

            DataViewerUserControl content = new DataViewerUserControl(layer, layers);
            content.IsHighlightFeatureEnabled = false;
            content.ShowDock();
        }

        private void MoveToTopClick(object sender, RoutedEventArgs e)
        {
            var subStyleItems = styleItem.Parent.Children;
            if (subStyleItems != null)
            {
                var index = subStyleItems.IndexOf(styleItem);
                if (index >= 0)
                {
                    subStyleItems.Move(index, 0);
                    RefreshUI(styleItem.Parent as StyleLayerListItem);
                }
            }
        }

        private void MoveToBottomClick(object sender, RoutedEventArgs e)
        {
            var subStyleItems = styleItem.Parent.Children;
            if (subStyleItems != null)
            {
                var index = subStyleItems.IndexOf(styleItem);
                if (index < subStyleItems.Count - 1)
                {
                    subStyleItems.Move(index, subStyleItems.Count - 1);
                    RefreshUI(styleItem.Parent as StyleLayerListItem);
                }
            }
        }

        private void MoveUpClick(object sender, RoutedEventArgs e)
        {
            var subStyleItems = styleItem.Parent.Children;
            if (subStyleItems != null)
            {
                var index = subStyleItems.IndexOf(styleItem);
                if (index - 1 >= 0)
                {
                    subStyleItems.Move(index, index - 1);
                    RefreshUI(styleItem.Parent as StyleLayerListItem);
                }
            }
        }

        private void MoveDownClick(object sender, RoutedEventArgs e)
        {
            var subStyleItems = styleItem.Parent.Children;
            if (subStyleItems != null)
            {
                var index = subStyleItems.IndexOf(styleItem);
                if (index + 1 <= subStyleItems.Count - 1)
                {
                    subStyleItems.Move(index, index + 1);
                    RefreshUI(styleItem.Parent as StyleLayerListItem);
                }
            }
        }

        private void ReplaceFromLibrary(object sender, RoutedEventArgs e)
        {
            var styleLibraryWindow = new StyleLibraryWindow();
            if (styleLibraryWindow.ShowDialog().GetValueOrDefault())
            {
                var compositeStyle = styleItem.ConcreteObject as CompositeStyle;

                //styleLibraryWindow.Result.CompositeStyle
                var compositeStyleItem = GisEditor.StyleManager.GetStyleLayerListItem(styleLibraryWindow.Result.CompositeStyle);
                if (compositeStyle != null)
                {
                    styleItem.Children.Clear();
                    styleItem.Name = compositeStyleItem.Name;
                    foreach (var item in compositeStyleItem.Children)
                    {
                        styleItem.Children.Add(item);
                    }
                    styleItem.UpdateConcreteObject();
                }
                else
                {
                    var parentStyleitem = styleItem.Parent as StyleLayerListItem;
                    if (parentStyleitem != null)
                    {
                        var index = parentStyleitem.Children.IndexOf(styleItem);
                        parentStyleitem.Children.RemoveAt(index);
                        foreach (var item in compositeStyleItem.Children)
                        {
                            parentStyleitem.Children.Insert(index, item);
                            index++;
                        }
                        parentStyleitem.UpdateConcreteObject();
                    }
                }
            }
        }

        private void InsertFromLibrary(object sender, RoutedEventArgs e)
        {
            var styleLibraryWindow = new StyleLibraryWindow();
            if (styleLibraryWindow.ShowDialog().GetValueOrDefault())
            {
                var compositeStyle = styleItem.ConcreteObject as CompositeStyle;
                var compositeStyleItem = GisEditor.StyleManager.GetStyleLayerListItem(styleLibraryWindow.Result.CompositeStyle);
                if (compositeStyle != null)
                {
                    foreach (var item in compositeStyleItem.Children.Reverse())
                    {
                        styleItem.Children.Insert(0, item);
                    }
                }
                else
                {
                    var index = styleItem.Parent.Children.IndexOf(styleItem);
                    foreach (var item in compositeStyleItem.Children)
                    {
                        index++;
                        styleItem.Parent.Children.Insert(index, item);
                    }
                }
            }
        }

        private void InitializeSubViewModels()
        {
            styleItem.ConcreteObjectUpdated -= RaisePropertiesChanged;
            styleItem.ConcreteObjectUpdated += RaisePropertiesChanged;
            styleItem.Children.CollectionChanged -= StyleItems_CollectionChanged;
            styleItem.Children.CollectionChanged += StyleItems_CollectionChanged;
            styleItemViewModels = new ObservableCollection<StyleItemViewModel>();
            foreach (var tmpStyleItem in styleItem.Children.OfType<StyleLayerListItem>())
            {
                AddNewStyleItemViewModel(tmpStyleItem);
            }
        }

        private void RefreshUI(StyleLayerListItem styleItem)
        {
            if (styleItem != null)
            {
                styleItem.UpdateConcreteObject();
                var styleItemUI = styleItem.GetUI(styleArguments);
                if (styleItemUI != null)
                {
                    styleItem.UpdateUI(styleItemUI);
                }
            }
        }

        private void AddNewStyleItemViewModel(MapSuite.GisEditor.StyleLayerListItem tmpStyleItem, int insertIndex = -1)
        {
            var styleItemViewModel = new StyleItemViewModel(tmpStyleItem, styleArguments);
            styleItemViewModel.ParentViewModel = this;

            if (insertIndex == -1)
            {
                styleItemViewModels.Add(styleItemViewModel);
            }
            else
            {
                styleItemViewModels.Insert(insertIndex, styleItemViewModel);
            }
        }

        private void StyleItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (var newItem in e.NewItems.OfType<StyleLayerListItem>())
                {
                    if (!StyleItemViewModels.Any(s => s.StyleItem == newItem))
                    {
                        AddNewStyleItemViewModel(newItem, e.NewStartingIndex);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems.OfType<StyleLayerListItem>())
                {
                    var deletingStyleItemViewModel = StyleItemViewModels.FirstOrDefault(s => s.StyleItem == oldItem);
                    if (deletingStyleItemViewModel != null)
                    {
                        StyleItemViewModels.Remove(deletingStyleItemViewModel);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                StyleItemViewModels.Clear();
            }
            else if (e.Action == NotifyCollectionChangedAction.Move)
            {
                StyleItemViewModels.Move(e.OldStartingIndex, e.NewStartingIndex);
            }

            RaisePropertiesChanged(this, new EventArgs());
        }

        private void RaisePropertiesChanged(object sender, EventArgs e)
        {
            RaisePropertyChanged(() => Name);
            RaisePropertyChanged(() => PreviewSource);

            var parentItem = StyleItem.Parent as StyleLayerListItem;
            if (parentItem != null)
            {
                var styleUI = parentItem.GetUI(styleArguments);
                if (styleUI != null) parentItem.UpdateUI(styleUI);
            }
        }
    }
}