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
using System.Windows.Threading;
using Microsoft.Windows.Controls.Ribbon;
using System.Collections.Generic;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    public class QuickAccessToolbarManager : Manager
    {
        private SettingUserControl quickAccessToolbarSettingUI;
        private QuickAccessToolbarSettingViewModel quickAccessToolbarSettingViewModel;

        public QuickAccessToolbarManager()
        {
            quickAccessToolbarSettingViewModel = new QuickAccessToolbarSettingViewModel(this);
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (quickAccessToolbarSettingUI == null)
            {
                quickAccessToolbarSettingUI = new QuickAccessToolbarSettingUserControl();
                quickAccessToolbarSettingUI.DataContext = quickAccessToolbarSettingViewModel;
            }

            return quickAccessToolbarSettingUI;
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();

            if (quickAccessToolbarSettingViewModel != null)
            {
                string setting = string.Empty;

                foreach (var item in quickAccessToolbarSettingViewModel.ListItemSource)
                {
                    var ribbonButton = item as RibbonButton;
                    var menuButton = item as RibbonMenuButton;
                    var rationButton = item as RibbonRadioButton;
                    if (ribbonButton != null)
                    {
                        setting = setting + ribbonButton.QuickAccessToolBarId.ToString() + ",";
                    }
                    else if (menuButton != null)
                    {
                        setting = setting + menuButton.QuickAccessToolBarId.ToString() + ",";
                    }
                    else if (rationButton != null)
                    {
                        setting = setting + rationButton.QuickAccessToolBarId.ToString() + ",";
                    }
                }

                settings.GlobalSettings["QuickAccessToolbarOrder"] = setting.TrimEnd(',');
            }

            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);

            var gisEidtorControl = quickAccessToolbarSettingViewModel.GisEditorUserControl;
            if (gisEidtorControl != null)
            {
                if (settings.GlobalSettings.ContainsKey("QuickAccessToolbarOrder"))
                {
                    var orderString = settings.GlobalSettings["QuickAccessToolbarOrder"];

                    string[] orders = orderString.Split(',');

                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            quickAccessToolbarSettingViewModel.ListItemSource.Clear();

                            foreach (var order in orders)
                            {
                                var ribbonControl = gisEidtorControl.ribbonContainer.Items.OfType<RibbonTab>().SelectMany(g => g.Items.OfType<RibbonGroup>())
                                      .SelectMany(g => g.Items.OfType<IInputElement>())
                                      .FirstOrDefault(i => CheckCanAddToQuickAccessBar(i, order));

                                if (ribbonControl != null)
                                {
                                    RibbonCommands.AddToQuickAccessToolBarCommand.Execute(null, ribbonControl);
                                }
                                else
                                {
                                    var ribbonMenu = gisEidtorControl.ribbonContainer.ApplicationMenu.Items.OfType<IInputElement>().FirstOrDefault(i => CheckCanAddToQuickAccessBar(i, order));

                                    if (ribbonMenu != null)
                                    {
                                        RibbonCommands.AddToQuickAccessToolBarCommand.Execute(null, ribbonMenu);
                                    }
                                    else
                                    {
                                        gisEidtorControl.ApplicationMenu_DropDownOpened(null, null);
                                        var ribbonTheme = GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>()
                                            .SelectMany(p => p.ApplicationMenuItems)
                                            .SelectMany(i => i.Items.OfType<IInputElement>())
                                            .FirstOrDefault(i => CheckCanAddToQuickAccessBar(i, order));

                                        if (ribbonTheme != null)
                                        {
                                            RibbonCommands.AddToQuickAccessToolBarCommand.Execute(null, ribbonTheme);
                                        }
                                    }
                                }
                            }
                        }, DispatcherPriority.Background);
                    }
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        InitializeQuickAccessToolBarItems(gisEidtorControl);
                    }, DispatcherPriority.Background);
                }
            }
        }

        private void InitializeQuickAccessToolBarItems(GisEditorUserControl gisEidtorControl)
        {
            quickAccessToolbarSettingViewModel.ListItemSource.Clear();

            RibbonTab homeTab = null;
            foreach (var item in gisEidtorControl.ribbonContainer.Items)
            {
                var ribbonTab = item as RibbonTab;
                if (ribbonTab != null)
                {
                    if (ribbonTab.Header.ToString().Equals(GisEditor.LanguageManager.GetStringResource("HomeRibbonTabHeader")))
                    {
                        homeTab = ribbonTab;
                        break;
                    }
                }
            }

            gisEidtorControl.ApplicationMenu_DropDownOpened(null, null);
            var printControl = GisEditor.UIManager.GetActiveUIPlugins<UIPlugin>()
                .SelectMany(p => p.ApplicationMenuItems)
                .FirstOrDefault(p => p.Name == "Print");

            RibbonGroup projectGroup = (RibbonGroup)homeTab.Items[0];
            for (int i = 0; i < projectGroup.Items.Count; i++)
            {
                Control control = projectGroup.Items[i] as Control;

                if (i == projectGroup.Items.Count - 2 && printControl != null)
                {
                    RibbonCommands.AddToQuickAccessToolBarCommand.Execute(null, printControl);
                }

                RibbonButton tempRibbonButton = control as RibbonButton;
                if (tempRibbonButton != null
                    && tempRibbonButton.QuickAccessToolBarId.ToString().Equals("PluginManager", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (control != null && RibbonCommands.AddToQuickAccessToolBarCommand.CanExecute(null, control))
                {
                    RibbonCommands.AddToQuickAccessToolBarCommand.Execute(null, control);
                }
            }

            if (homeTab.Items.Count > 1)
            {
                string[] necessaryQuickAccessIds = new string[]
                {
                    "ViewData", "PanMode", "ZoomToSelectionMode", "IdentifyMode"
                };
                List<RibbonButton> necessaryQuickAccessItems = homeTab.Items.OfType<RibbonGroup>()
                    .SelectMany(tempGroup => tempGroup.Items.OfType<RibbonButton>())
                    .Where(tempButton =>
                    {
                        object quickAccessBarId = tempButton.QuickAccessToolBarId;
                        return quickAccessBarId != null
                            && necessaryQuickAccessIds.Any(tempId => quickAccessBarId.ToString().Equals(tempId, StringComparison.Ordinal));
                    }).ToList();

                necessaryQuickAccessItems.ForEach(tempItem => 
                {
                    RibbonCommands.AddToQuickAccessToolBarCommand.Execute(null, tempItem);
                });
            }
        }

        private bool CheckCanAddToQuickAccessBar(IInputElement i, string order)
        {
            bool result = false;

            var ribbonButton = i as RibbonButton;
            var ribbonMenuButton = i as RibbonMenuButton;
            var ribbonRadioButton = i as RibbonRadioButton;
            var ribbonToggleButton = i as RibbonToggleButton;
            var ribbonMenuItem = i as RibbonMenuItem;

            if (ribbonButton != null)
            {
                result = ribbonButton.QuickAccessToolBarId != null && ribbonButton.QuickAccessToolBarId.ToString().Equals(order);
            }
            else if (ribbonMenuButton != null)
            {
                result = ribbonMenuButton.QuickAccessToolBarId != null && ribbonMenuButton.QuickAccessToolBarId.Equals(order);
            }
            else if (ribbonRadioButton != null)
            {
                result = ribbonRadioButton.QuickAccessToolBarId != null && ribbonRadioButton.QuickAccessToolBarId.Equals(order);
            }
            else if (ribbonToggleButton != null)
            {
                result = ribbonToggleButton.QuickAccessToolBarId != null && ribbonToggleButton.QuickAccessToolBarId.Equals(order);
            }
            else if (ribbonMenuItem != null)
            {
                result = ribbonMenuItem.QuickAccessToolBarId != null && ribbonMenuItem.QuickAccessToolBarId.Equals(order);
            }

            return result;
        }
    }
}
