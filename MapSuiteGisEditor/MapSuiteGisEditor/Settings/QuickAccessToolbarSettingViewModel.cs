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
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using Microsoft.Windows.Controls.Ribbon;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    public class QuickAccessToolbarSettingViewModel : ViewModelBase
    {
        private QuickAccessToolbarManager option;

        private ObservedCommand upCommand;
        private ObservedCommand downCommand;
        private ObservedCommand addCommand;
        private ObservedCommand removeCommand;
        private ObservedCommand resetCommand;

        private bool isUpEnabled;
        private bool isDownEnabled;

        private object selectedItem;
        private object ribbonSelectedItem;

        internal GisEditorUserControl GisEditorUserControl { get; set; }

        public QuickAccessToolbarSettingViewModel()
            : this(null)
        { }

        public QuickAccessToolbarSettingViewModel(QuickAccessToolbarManager option)
        {
            this.option = option;
            this.isUpEnabled = false;
            this.isDownEnabled = false;

            GisEditorUserControl = GetLogicalChild(Application.Current.MainWindow);
        }

        public object RibbonSelectedItem
        {
            get { return ribbonSelectedItem; }
            set
            {
                ribbonSelectedItem = value;

                RaisePropertyChanged(()=>RibbonSelectedItem);
            }
        }

        public object SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;

                isUpEnabled = true;
                isDownEnabled = true;

                var index = ListItemSource.IndexOf(selectedItem);
                if (index < 1)
                {
                    isUpEnabled = false;
                }
                else if (index > ListItemSource.Count - 2)
                {
                    isDownEnabled = false;
                }

                if (selectedItem == null)
                {
                    isUpEnabled = false;
                    isDownEnabled = false;
                }

                RaisePropertyChanged(()=>SelectedItem);
            }
        }

        public ObservedCommand UpCommand
        {
            get
            {
                if (upCommand == null)
                {
                    upCommand = new ObservedCommand(() =>
                    {
                    }, () => { return isUpEnabled; });
                }

                return upCommand;
            }
        }

        public ObservedCommand DownCommand
        {
            get
            {
                if (downCommand == null)
                {
                    downCommand = new ObservedCommand(() =>
                    {
                    }, () => { return isDownEnabled; });
                }

                return downCommand;
            }
        }

        public ObservedCommand AddCommand
        {
            get
            {
                if (addCommand == null)
                {
                    addCommand = new ObservedCommand(() =>
                    {
                        if (ribbonSelectedItem != null)
                        {
                        }
                    }, () => { return ribbonSelectedItem != null; });
                }

                return addCommand;
            }
        }

        public ObservedCommand RemoveCommand
        {
            get
            {
                if (removeCommand == null)
                {
                    removeCommand = new ObservedCommand(() =>
                    {
                    }, () => { return selectedItem != null; });
                }

                return removeCommand;
            }
        }

        public ObservedCommand ResetCommand
        {
            get
            {
                if (resetCommand == null)
                {
                    resetCommand = new ObservedCommand(() =>
                    {
                        var quickAccessToolBarManager = GisEditorHelper.GetManagers().OfType<QuickAccessToolbarManager>().FirstOrDefault();
                        if (quickAccessToolBarManager != null)
                        {
                            quickAccessToolBarManager.ApplySettings(new StorableSettings());
                        }

                        RaisePropertyChanged(()=>ListItemSource);
                    }, () => { return true; });
                }

                return resetCommand;
            }
        }

        public ItemCollection ListItemSource
        {
            get
            {
                if (GisEditorUserControl != null)
                {
                    return GisEditorUserControl.ribbonContainer.QuickAccessToolBar.Items;
                }

                return null;
            }
        }

        public bool CheckCanAddToQuickAccessBar(object item)
        {
            bool result = false; ;

            var ribbonButton = item as RibbonButton;
            var ribbonMenuButton = item as RibbonMenuButton;
            var ribbonRadioButton = item as RibbonRadioButton;
            var ribbonToggleButton = item as RibbonToggleButton;

            if (ribbonButton != null && ribbonButton.QuickAccessToolBarId != null)
            {
                result = ribbonButton.CanAddToQuickAccessToolBarDirectly &&
                    !ListItemSource.OfType<object>().Any(l => l.GetType().Name == "RibbonButton" && ((RibbonButton)l).QuickAccessToolBarId.Equals(ribbonButton.QuickAccessToolBarId));
            }
            else if (ribbonMenuButton != null && ribbonMenuButton.QuickAccessToolBarId != null)
            {
                result = ribbonMenuButton.CanAddToQuickAccessToolBarDirectly &&
                    !ListItemSource.OfType<object>().Any(l => (l.GetType().Name == "RibbonMenuButton" || l.GetType().Name == "RibbonSplitButton") && ((RibbonMenuButton)l).QuickAccessToolBarId.Equals(ribbonMenuButton.QuickAccessToolBarId));
            }
            else if (ribbonRadioButton != null && ribbonRadioButton.QuickAccessToolBarId != null)
            {
                result = ribbonRadioButton.CanAddToQuickAccessToolBarDirectly &&
                    !ListItemSource.OfType<object>().Any(l => l.GetType().Name == "RibbonRadioButton" && ((RibbonRadioButton)l).QuickAccessToolBarId.Equals(ribbonRadioButton.QuickAccessToolBarId));
            }
            else if (ribbonToggleButton != null && ribbonToggleButton.QuickAccessToolBarId != null)
            {
                result = ribbonToggleButton.CanAddToQuickAccessToolBarDirectly &&
                 !ListItemSource.OfType<object>().Any(l => l.GetType().Name == "RibbonToggleButton" && (((RibbonToggleButton)l).QuickAccessToolBarId.Equals(ribbonToggleButton.QuickAccessToolBarId)));
            }

            return result;
        }

        private GisEditorUserControl GetLogicalChild(DependencyObject parent)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<UIElement>())
            {
                var userControl = child as GisEditorUserControl;

                if (userControl != null)
                {
                    return userControl;
                }
                else
                {
                    return GetLogicalChild(child);
                }
            }

            return null;
        }
    }
}