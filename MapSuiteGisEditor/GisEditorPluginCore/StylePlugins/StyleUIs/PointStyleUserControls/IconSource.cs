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
using System.Linq;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class IconSource : ViewModelBase
    {
        private IconEntity selectedIcon;
        private IconCategory selectedCategory;
        private IconCategory selectedSubCategory;
        private PointStyleViewModel viewModel;
        private bool constrainProportions;
        private bool enableEditSize;
        private IEnumerable<IconCategory> iconCategories;
        private ObservableCollection<IconEntity> currentIcons;

        public IconSource(PointStyleViewModel pointStyleViewModel)
        {
            viewModel = pointStyleViewModel;

            EnableEditSize = true;
            ConstrainProportions = true;

            iconCategories = IconHelper.GetIconCategories();
            currentIcons = new ObservableCollection<IconEntity>();
            SelectedCategory = IconCategories.First();
            if (SelectedCategory != null)
            {
                if (viewModel.ActualImage != null)
                {
                    var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = viewModel.ActualImage.GetImageStream();
                    bitmapImage.EndInit();

                    SelectedIcon = new IconEntity() { Icon = bitmapImage };
                }

                if (selectedCategory.HasSubCategories)
                {
                    SelectedSubCategory = SelectedCategory.SubCategories[0];
                    if (string.IsNullOrEmpty(viewModel.ImagePath) && SelectedIcon.Icon == null)
                        SelectedIcon = SelectedSubCategory.Icons[0];
                }
                else
                {
                    if (string.IsNullOrEmpty(viewModel.ImagePath) && (SelectedIcon == null || SelectedIcon.Icon == null))
                        SelectedIcon = SelectedCategory.Icons[0];
                }
            }
        }

        public bool ConstrainProportions
        {
            get { return constrainProportions; }
            set { constrainProportions = value; }
        }

        public bool EnableEditSize
        {
            get { return enableEditSize; }
            set { enableEditSize = value; }
        }

        public IEnumerable<IconCategory> IconCategories
        {
            get { return iconCategories; }
        }

        public ObservableCollection<IconEntity> CurrentIcons
        {
            get { return currentIcons; }
        }

        public IconCategory SelectedCategory
        {
            get { return selectedCategory; }
            set
            {
                selectedCategory = value;
                RaisePropertyChanged(()=>SelectedCategory);
                CurrentIcons.Clear();
                if (!value.HasSubCategories)
                {
                    foreach (var item in value.Icons)
                    {
                        CurrentIcons.Add(item);
                    }
                }
                else
                {
                    SelectedSubCategory = value.SubCategories[0];
                }
            }
        }

        public IconCategory SelectedSubCategory
        {
            get { return selectedSubCategory; }
            set
            {
                selectedSubCategory = value;
                RaisePropertyChanged(()=>SelectedSubCategory);
                CurrentIcons.Clear();
                if (value != null)
                {
                    foreach (var item in value.Icons)
                    {
                        CurrentIcons.Add(item);
                    }
                    SelectedIcon = value.Icons[0];
                }
            }
        }

        public IconEntity SelectedIcon
        {
            get { return selectedIcon; }
            set
            {
                selectedIcon = value;
                RaisePropertyChanged(()=>SelectedIcon);

                if (value != null)
                {
                    viewModel.ImageHeight = (int)value.Icon.Height;
                    viewModel.ImageWidth = (int)value.Icon.Width;
                    viewModel.ImagePath = null;
                }
            }
        }
    }
}