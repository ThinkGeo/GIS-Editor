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
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Windows.Controls.Ribbon;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    internal class StyleBuilderViewModel : ViewModelBase
    {
        private bool isRootNodeSelected;
        private bool isZoomLevelPickerEnabled;
        private ContextMenu mainContextMenu;
        private StyleBuilderArguments styleArguments;
        private CompositeStyle componentStyle;
        private Visibility styleListVisibility;
        private Visibility previewImageVisibility;
        private SolidColorBrush rootNodeBackground;
        private StyleItemViewModel rootStyleItem;
        private StyleItemViewModel selectedStyleItem;
        private Collection<ZoomLevelModel> fromZoomLevelModels;
        private Collection<ZoomLevelModel> toZoomLevelModels;

        private RelayCommand renameCommand;
        private RelayCommand duplicateCommand;
        private RelayCommand deleteStyleCommand;
        private RelayCommand clearStylesCommand;
        private RelayCommand saveToLibraryCommand;
        private RelayCommand loadFromLibraryCommand;
        private ZoomLevelModel selectedToZoomLevelModel;
        private ZoomLevelModel selectedFromZoomLevelModel;

        public StyleBuilderViewModel(CompositeStyle editingStyle, StyleBuilderArguments styleArguments)
        {
            this.componentStyle = editingStyle;
            this.styleArguments = styleArguments;
            this.fromZoomLevelModels = new Collection<ZoomLevelModel>();
            this.InitializeZoomLevels(fromZoomLevelModels);
            this.toZoomLevelModels = new Collection<ZoomLevelModel>();
            this.InitializeZoomLevels(toZoomLevelModels);

            this.InitializeStyleItems(styleArguments.SelectedConcreteObject);
            this.InitializeUIItems(styleArguments.AvailableUIElements);
            this.InitializeCommands();
            this.mainContextMenu = RootStyleItem.ContextMenu;
        }

        public string Name
        {
            get { return componentStyle.Name; }
            set
            {
                componentStyle.Name = value;
                RaisePropertyChanged(() => Name);
            }
        }

        public SolidColorBrush RootNodeBackground
        {
            get { return rootNodeBackground; }
            set
            {
                rootNodeBackground = value;
                RaisePropertyChanged(() => RootNodeBackground);
            }
        }

        public StyleItemViewModel RootStyleItem
        {
            get { return rootStyleItem; }
            set { rootStyleItem = value; }
        }

        public int FromZoomLevelIndex
        {
            get { return FromZoomLevelModels.IndexOf(SelectedFromZoomLevelModel) + 1; }
            set { SelectedFromZoomLevelModel = FromZoomLevelModels[value - 1]; }
        }

        public int ToZoomLevelIndex
        {
            get { return ToZoomLevelModels.IndexOf(SelectedToZoomLevelModel) + 1; }
            set { SelectedToZoomLevelModel = ToZoomLevelModels[value - 1]; }
        }

        public ZoomLevelModel SelectedToZoomLevelModel
        {
            get { return selectedToZoomLevelModel; }
            set
            {
                selectedToZoomLevelModel = value;
                RaisePropertyChanged(() => SelectedToZoomLevelModel);
            }
        }

        public ZoomLevelModel SelectedFromZoomLevelModel
        {
            get { return selectedFromZoomLevelModel; }
            set
            {
                selectedFromZoomLevelModel = value;
                RaisePropertyChanged(() => SelectedFromZoomLevelModel);
            }
        }

        public Collection<ZoomLevelModel> ToZoomLevelModels
        {
            get { return toZoomLevelModels; }
        }

        public Collection<ZoomLevelModel> FromZoomLevelModels
        {
            get { return fromZoomLevelModels; }
        }

        public bool IsRootNodeSelected
        {
            get { return isRootNodeSelected; }
            set
            {
                isRootNodeSelected = value;
                if (!isRootNodeSelected)
                {
                    RootStyleItem.CommitRenaming();
                }
                else
                {
                    RaisePropertyChanged(() => CanAddStyle);
                    RaisePropertyChanged(() => StyleToolMenuItems);
                    RaisePropertyChanged(() => AddDefaultStyleCommand);
                }
            }
        }

        public CompositeStyle ResultStyle
        {
            get
            {
                componentStyle.Styles.Clear();
                foreach (var innerStyle in StyleItems.Select(item => item.StyleItem.ConcreteObject).OfType<Styles.Style>().Reverse())
                {
                    componentStyle.Styles.Add(innerStyle);
                }
                return componentStyle;
            }
        }

        public StyleBuilderArguments StyleArguments
        {
            get { return styleArguments; }
            set { styleArguments = value; }
        }

        public RelayCommand SaveToLibraryCommand
        {
            get { return saveToLibraryCommand; }
        }

        public RelayCommand LoadFromLibraryCommand
        {
            get { return loadFromLibraryCommand; }
        }

        public RelayCommand ClearStylesCommand
        {
            get { return clearStylesCommand; }
        }

        public RelayCommand DuplicateCommand
        {
            get { return duplicateCommand; }
        }

        public RelayCommand DeleteStyleCommand
        {
            get { return deleteStyleCommand; }
        }

        public RelayCommand RenameCommand
        {
            get { return renameCommand; }
        }

        public ContextMenu MainContextMenu
        {
            get { return mainContextMenu; }
        }

        public bool CanAddStyle
        {
            get { return !styleArguments.IsSubStyleReadonly && (IsRootNodeSelected || (SelectedStyleItem != null && SelectedStyleItem.CanAddStyle)); }
        }

        public RelayCommand AddDefaultStyleCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var firstMenuItem = StyleToolMenuItems.OfType<RibbonMenuItem>().FirstOrDefault();
                    if (firstMenuItem != null)
                    {
                        if (firstMenuItem.CommandParameter != null)
                        {
                            firstMenuItem.Command.Execute(firstMenuItem.CommandParameter);
                        }
                        else if (firstMenuItem.Items.OfType<RibbonMenuItem>().Count() > 0)
                        {
                            firstMenuItem = firstMenuItem.Items.OfType<RibbonMenuItem>().FirstOrDefault();
                            firstMenuItem.Command.Execute(firstMenuItem.CommandParameter);
                        }
                    }
                });
            }
        }

        public Collection<object> StyleToolMenuItems
        {
            get
            {
                var menuItems = new Collection<object>();

                var itemCollection = (MainContextMenu.Items.OfType<MenuItem>().First()).Items;
                if (!IsRootNodeSelected && SelectedStyleItem != null)
                {
                    itemCollection = (SelectedStyleItem.ContextMenu.Items.OfType<MenuItem>().First()).Items;
                    if (itemCollection.Count == 0)
                    {
                        itemCollection = ((MenuItem)SelectedStyleItem.ParentViewModel.ContextMenu.Items[0]).Items;
                    }
                }

                ImageToImageSourceConverter converter = new ImageToImageSourceConverter();
                foreach (var item in itemCollection)
                {
                    if (item is MenuItem)
                    {
                        var menuItem = (MenuItem)item;

                        RibbonMenuItem ribbonMenuItem = GetRibbonMenuItem(converter, menuItem);

                        menuItems.Add(ribbonMenuItem);
                    }
                    else if (item is Separator) menuItems.Add(new RibbonSeparator());
                }

                return menuItems;
            }
        }

        private static RibbonMenuItem GetRibbonMenuItem(ImageToImageSourceConverter converter, MenuItem menuItem)
        {
            RibbonMenuItem ribbonMenuItem = new RibbonMenuItem();
            ribbonMenuItem.Header = menuItem.Header;
            ribbonMenuItem.Command = menuItem.Command;
            ribbonMenuItem.CommandParameter = menuItem.CommandParameter;
            if (menuItem.Icon != null)
            {
                ribbonMenuItem.ImageSource = (ImageSource)converter.Convert(menuItem.Icon, null, null, null);
            }
            if (menuItem.Items.Count > 0)
            {
                foreach (var item in menuItem.Items.OfType<MenuItem>())
                {
                    ribbonMenuItem.Items.Add(GetRibbonMenuItem(converter, item));
                }
            }
            return ribbonMenuItem;
        }

        public Visibility PreviewImageVisibility
        {
            get { return previewImageVisibility; }
        }

        public bool IsZoomLevelPickerEnabled
        {
            get { return isZoomLevelPickerEnabled; }
        }

        public Visibility ApplyButtonVisibility
        {
            get
            {
                bool result = styleArguments.AppliedCallback != null;
                return result ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility StyleListVisibility
        {
            get { return styleListVisibility; }
        }

        public bool IsStyleLoadable
        {
            get { return styleArguments.AvailableStyleCategories != StyleCategories.None; }
        }

        public StyleItemViewModel SelectedStyleItem
        {
            get { return selectedStyleItem; }
            set
            {
                if (selectedStyleItem != value)
                {
                    if (selectedStyleItem != null)
                    {
                        selectedStyleItem.CommitRenaming();
                    }
                    selectedStyleItem = value;
                    IsRootNodeSelected = false;
                    RaisePropertyChanged(() => SelectedStyleItem);
                    RaisePropertyChanged(() => SelectedStyleItemUI);

                    RaisePropertyChanged(() => CanAddStyle);
                    RaisePropertyChanged(() => StyleToolMenuItems);
                    RaisePropertyChanged(() => AddDefaultStyleCommand);
                    RaiseCanExecuteChanged(duplicateCommand);
                    RaiseCanExecuteChanged(deleteStyleCommand);
                    RaiseCanExecuteChanged(renameCommand);
                }
            }
        }

        public StyleUserControl SelectedStyleItemUI
        {
            get
            {
                if (selectedStyleItem != null)
                {
                    return selectedStyleItem.StyleItem.GetUI(styleArguments);
                }
                else return null;
            }
        }

        public ObservableCollection<StyleItemViewModel> StyleItems
        {
            get { return rootStyleItem.StyleItemViewModels; }
        }

        public ReadOnlyCollection<Styles.Style> Styles
        {
            get
            {
                var styles = StyleItems.Select(s => s.StyleItem.ConcreteObject).OfType<Styles.Style>();
                return new ReadOnlyCollection<Styles.Style>(styles.ToList());
            }
        }

        public BitmapSource PreviewSource
        {
            get
            {
                return componentStyle.GetPreviewImage(102, 31);
            }
        }

        public BitmapSource SmallPreviewSource
        {
            get
            {
                return componentStyle.GetPreviewImage(16, 16);
            }
        }

        public void ApplyStyles(StyleBuilderResult styleEditResult)
        {
            if (styleArguments.AppliedCallback != null)
            {
                styleArguments.AppliedCallback(styleEditResult);
            }
        }

        private void InitializeStyleItems(object selectedObject)
        {
            if (selectedObject == null) selectedObject = componentStyle.Styles.FirstOrDefault();
            var actualRootStyleItem = new ComponentStyleItem(componentStyle);
            rootStyleItem = new StyleItemViewModel(actualRootStyleItem, styleArguments);
            rootStyleItem.StyleBuilder = this;
            rootStyleItem.StyleItem.ConcreteObjectUpdated += UpdatePreviewSource;
            ResetSelectedStyleItem(selectedObject);
        }

        private void ResetSelectedStyleItem(object selectedObject)
        {
            if (selectedObject == null)
            {
                SelectedStyleItem = rootStyleItem.StyleItemViewModels.FirstOrDefault();
            }
            else
            {
                SelectedStyleItem = FindStyleItemViewModel(rootStyleItem.StyleItemViewModels, selectedObject);
            }

            if (SelectedStyleItem != null)
            {
                SelectedStyleItem.IsSelected = true;
                var styleUI = SelectedStyleItem.StyleItem.GetUI(StyleArguments);
                if (styleUI != null)
                {
                    SelectedStyleItem.StyleItem.UpdateUI(styleUI);
                }
            }
        }

        private StyleItemViewModel FindStyleItemViewModel(ObservableCollection<StyleItemViewModel> observableCollection, object selectedObject)
        {
            StyleItemViewModel result = null;
            foreach (var styleItemViewModel in observableCollection)
            {
                if (styleItemViewModel.StyleItem.ConcreteObject == selectedObject)
                {
                    result = styleItemViewModel;
                    break;
                }
                else
                {
                    result = FindStyleItemViewModel(styleItemViewModel.StyleItemViewModels, selectedObject);
                    if (result != null)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public void UpdatePreviewSource()
        {
            UpdatePreviewSource(this, new EventArgs());
        }

        private void UpdatePreviewSource(object sender, EventArgs e)
        {
            RaisePropertyChanged(() => PreviewSource);
            RaisePropertyChanged(() => SmallPreviewSource);
        }

        private void InitializeUIItems(StyleBuilderUIElements uiItems)
        {
            previewImageVisibility = Visibility.Visible;
            isZoomLevelPickerEnabled = uiItems != StyleBuilderUIElements.None && uiItems.HasFlag(StyleBuilderUIElements.ZoomLevelPicker);
            styleListVisibility = GetStyleUIItemsVisibility(uiItems, StyleBuilderUIElements.StyleList);
        }

        private static Visibility GetStyleUIItemsVisibility(StyleBuilderUIElements checkingUiItems, StyleBuilderUIElements uiItem)
        {
            return checkingUiItems != StyleBuilderUIElements.None && checkingUiItems.HasFlag(uiItem) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InitializeCommands()
        {
            #region load from lib

            loadFromLibraryCommand = new RelayCommand(() =>
            {
                StyleLibraryWindow libraryWindow = new StyleLibraryWindow();
                if (libraryWindow.ShowDialog().Value)
                {
                    var componentStyle = libraryWindow.Result.CompositeStyle;
                    if (componentStyle != null)
                    {
                        FromZoomLevelIndex = libraryWindow.Result.FromZoomLevelIndex;
                        ToZoomLevelIndex = libraryWindow.Result.ToZoomLevelIndex;
                        RootStyleItem.StyleItem.Children.Clear();
                        componentStyle.Styles.Reverse().ForEach(s => LoadStyle(s));
                        RootStyleItem.StyleItem.Name = componentStyle.Name;
                        RootStyleItem.StyleItem.UpdateConcreteObject();
                        UpdatePreviewSource();
                        SelectedStyleItem = RootStyleItem.StyleItemViewModels.FirstOrDefault();
                        if (SelectedStyleItem != null) SelectedStyleItem.IsSelected = true;
                    }
                }
            }, () => !styleArguments.IsSubStyleReadonly);

            #endregion load from lib

            #region save to lib

            saveToLibraryCommand = new RelayCommand(() =>
            {
                var innerStyles = StyleItems.Select(s => s.StyleItem.ConcreteObject).Reverse().OfType<Styles.Style>();
                CompositeStyle componentStyle = new CompositeStyle(innerStyles) { Name = Name };
                GisEditor.StyleManager.SaveStyleToLibrary(componentStyle, SelectedToZoomLevelModel.Scale, SelectedFromZoomLevelModel.Scale);
            });

            #endregion save to lib

            #region clear styles

            clearStylesCommand = new RelayCommand(() =>
            {
                System.Windows.Forms.DialogResult result = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("StyleBuilderViewModelEraseAllStylesText")
                    , GisEditor.LanguageManager.GetStringResource("StyleBuilderViewModelClearStylesCaption")
                    , System.Windows.Forms.MessageBoxButtons.YesNo
                    , System.Windows.Forms.MessageBoxIcon.Warning);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    RootStyleItem.StyleItem.Children.Clear();
                    RootStyleItem.StyleItem.UpdateConcreteObject();
                    UpdatePreviewSource();
                    SelectRootNodeIfEmpty();
                }
            }, () => !styleArguments.IsSubStyleReadonly);

            #endregion clear styles

            #region duplicate style

            duplicateCommand = new RelayCommand(() =>
            {
                StyleLayerListItem duplicateStyleItem = SelectedStyleItem.StyleItem.CloneDeep();
                if (duplicateStyleItem != null)
                {
                    var parent = SelectedStyleItem.StyleItem.Parent as StyleLayerListItem;
                    if (parent != null)
                    {
                        parent.Children.Insert(0, duplicateStyleItem);
                        parent.UpdateConcreteObject();
                        if (StyleItems.Count > 0) StyleItems.FirstOrDefault().IsSelected = true;
                        UpdatePreviewSource();
                    }
                }
            }, () => !styleArguments.IsSubStyleReadonly && SelectedStyleItem != null && SelectedStyleItem.StyleItem.Parent != null);

            #endregion duplicate style

            #region delete style

            deleteStyleCommand = new RelayCommand(() =>
            {
                var parentItem = SelectedStyleItem.StyleItem.Parent as StyleLayerListItem;
                if (parentItem != null && parentItem.Children.Contains(SelectedStyleItem.StyleItem))
                {
                    parentItem.Children.Remove(SelectedStyleItem.StyleItem);
                    parentItem.UpdateConcreteObject();
                    UpdatePreviewSource();
                    SelectRootNodeIfEmpty();
                }
            }, () => !styleArguments.IsSubStyleReadonly && SelectedStyleItem != null && SelectedStyleItem.StyleItem.Parent != null && SelectedStyleItem.StyleItem.ConcreteObject is Styles.Style);

            #endregion delete style

            #region rename

            renameCommand = new RelayCommand(() =>
            {
                if (IsRootNodeSelected)
                {
                    RootStyleItem.IsRenaming = true;
                }
                else
                {
                    SelectedStyleItem.IsRenaming = true;
                }
            }, () => !styleArguments.IsSubStyleReadonly && (IsRootNodeSelected || (SelectedStyleItem != null && SelectedStyleItem.StyleItem.CanRename)));

            #endregion rename
        }

        private void SelectRootNodeIfEmpty()
        {
            if (RootStyleItem.StyleItem.Children.Count == 0)
            {
                IsRootNodeSelected = true;
            }
        }

        private void LoadStyle(Styles.Style s)
        {
            //s.Name = GisEditor.StyleManager.GetStylePluginByStyle(s).Name;
            var styleItem = GisEditor.StyleManager.GetStyleLayerListItem(s);
            if (styleItem != null)
            {
                RootStyleItem.StyleItem.Children.Add(styleItem);
            }
        }

        private void InitializeZoomLevels(Collection<ZoomLevelModel> zoomLevelModels)
        {
            if (GisEditor.ActiveMap != null)
            {
                var allZoomLevels = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Where(z => z.GetType() == typeof(ZoomLevel)).ToList();
                for (int i = 0; i < allZoomLevels.Count; i++)
                {
                    zoomLevelModels.Add(new ZoomLevelModel(i + 1, allZoomLevels[i].Scale));
                }
            }
        }

        private void RaiseCanExecuteChanged(RelayCommand command)
        {
            if (command != null) command.RaiseCanExecuteChanged();
        }
    }
}