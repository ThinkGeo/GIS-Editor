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
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class StyleSettingViewModel : ViewModelBase
    {
        private StyleSetting option;
        private ObservedCommand addDefaultCommand;
        private StyleItem selectedStyleItem;
        private ObservableCollection<StyleItem> styleItems;

        [NonSerialized]
        private RelayCommand toTopCommand;

        [NonSerialized]
        private RelayCommand toBottomCommand;

        [NonSerialized]
        private RelayCommand moveUpCommand;

        [NonSerialized]
        private RelayCommand moveDownCommand;

        public StyleSettingViewModel(StyleSetting option)
        {
            this.option = option;
            styleItems = new ObservableCollection<StyleItem>();
            foreach (var item in option.DefaultStyles)
            {
                styleItems.Add(new StyleItem(item));
            }
            SelectedStyleItem = styleItems.FirstOrDefault();

            this.styleItems.CollectionChanged += (s, e) =>
            {
                option.DefaultStyles.Clear();

                foreach (var item in styleItems)
                {
                    option.DefaultStyles.Add(item.Style);
                }

                RaisePropertyChanged(() => CanMoveUp);
                RaisePropertyChanged(() => CanMoveDown);
            };
        }

        public StyleSetting Option
        {
            get { return option; }
        }

        public Style SelectedStyle
        {
            get
            {
                Style style = null;
                if (selectedStyleItem != null)
                {
                    style = selectedStyleItem.Style;
                }
                else if (styleItems.FirstOrDefault() != null)
                {
                    style = styleItems.FirstOrDefault().Style;
                }
                return style;
            }
        }

        public StyleItem SelectedStyleItem
        {
            get { return selectedStyleItem; }
            set
            {
                selectedStyleItem = value;
                foreach (var item in styleItems)
                {
                    item.Refresh();
                }
                RaisePropertyChanged(() => SelectedStyle);
                RaisePropertyChanged(() => SelectedStyleItem);
                RaisePropertyChanged(() => CanMoveUp);
                RaisePropertyChanged(() => CanMoveDown);
            }
        }

        public ObservableCollection<StyleItem> StyleItems
        {
            get
            {
                foreach (var item in styleItems)
                {
                    item.Refresh();
                }
                return styleItems;
            }
        }

        public ObservedCommand AddDefaultCommand
        {
            get
            {
                if (addDefaultCommand == null)
                {
                    addDefaultCommand = new ObservedCommand(() =>
                    {
                        lock (option.StylePlugin)
                        {
                            bool temp = option.StylePlugin.UseRandomColor;
                            option.StylePlugin.UseRandomColor = true;
                            Style style = option.StylePlugin.GetDefaultStyle().CloneDeep();
                            option.StylePlugin.UseRandomColor = temp;
                            StyleItem newItem = new StyleItem(style);
                            StyleItems.Add(newItem);
                            option.StylePlugin.StyleCandidates.Add(style);
                            SelectedStyleItem = newItem;
                        }
                    }, () =>
                    {
                        return !UseRandomColors;
                    });
                }
                return addDefaultCommand;
            }
        }

        public bool UseRandomColors
        {
            get { return option.UseRandomColors; }
            set
            {
                option.UseRandomColors = value;
                RaisePropertyChanged(() => UseRandomColors);
            }
        }

        public bool CanMoveUp
        {
            get
            {
                return SelectedStyleItem != null && StyleItems.Count > 0 && StyleItems.IndexOf(SelectedStyleItem) != 0;
            }
        }

        public bool CanMoveDown
        {
            get
            {
                return SelectedStyleItem != null && StyleItems.Count > 0 && StyleItems.IndexOf(SelectedStyleItem) != (StyleItems.Count - 1);
            }
        }

        public RelayCommand ToTopCommand
        {
            get
            {
                if (toTopCommand == null)
                {
                    toTopCommand = new RelayCommand(() =>
                    {
                        if (SelectedStyleItem != null && StyleItems.Contains(SelectedStyleItem))
                        {
                            var tmpSelectedItem = SelectedStyleItem;
                            StyleItems.Remove(tmpSelectedItem);
                            StyleItems.Insert(0, tmpSelectedItem);
                            SelectedStyleItem = tmpSelectedItem;
                        }
                    });
                }
                return toTopCommand;
            }
        }

        public RelayCommand ToBottomCommand
        {
            get
            {
                if (toBottomCommand == null)
                {
                    toBottomCommand = new RelayCommand(() =>
                    {
                        if (SelectedStyleItem != null && StyleItems.Contains(SelectedStyleItem))
                        {
                            var tmpSelectedItem = SelectedStyleItem;
                            StyleItems.Remove(tmpSelectedItem);
                            StyleItems.Add(tmpSelectedItem);
                            SelectedStyleItem = tmpSelectedItem;
                        }
                    });
                }
                return toBottomCommand;
            }
        }

        public RelayCommand MoveUpCommand
        {
            get
            {
                if (moveUpCommand == null)
                {
                    moveUpCommand = new RelayCommand(() =>
                    {
                        if (SelectedStyleItem != null && StyleItems.Contains(SelectedStyleItem))
                        {
                            int index = StyleItems.IndexOf(SelectedStyleItem);
                            var tmpSelectedItem = SelectedStyleItem;
                            StyleItems.Remove(tmpSelectedItem);
                            StyleItems.Insert(index - 1, tmpSelectedItem);
                            SelectedStyleItem = tmpSelectedItem;
                        }
                    });
                }
                return moveUpCommand;
            }
        }

        public RelayCommand MoveDownCommand
        {
            get
            {
                if (moveDownCommand == null)
                {
                    moveDownCommand = new RelayCommand(() =>
                    {
                        if (SelectedStyleItem != null && StyleItems.Contains(SelectedStyleItem))
                        {
                            int index = StyleItems.IndexOf(SelectedStyleItem);
                            var tmpSelectedItem = SelectedStyleItem;
                            StyleItems.Remove(tmpSelectedItem);
                            StyleItems.Insert(index + 1, tmpSelectedItem);
                            SelectedStyleItem = tmpSelectedItem;
                        }
                    });
                }
                return moveDownCommand;
            }
        }
    }

    [Serializable]
    public class StyleItem : ViewModelBase
    {
        private Style style;
        private string name;
        private BitmapImage bitmapImage;

        public StyleItem(Style style)
        {
            name = style.Name;
            this.style = style;
            BitmapImage = style.GetPreviewImage(20, 20);
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged(() => Name);
            }
        }

        public BitmapImage BitmapImage
        {
            get { return bitmapImage; }
            set
            {
                bitmapImage = value;
                RaisePropertyChanged(() => BitmapImage);
            }
        }

        public Style Style
        {
            get { return style; }
        }

        public void Refresh()
        {
            Name = style.Name;
            BitmapImage = style.GetPreviewImage(20, 20);
        }
    }
}