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
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.Command;
using Microsoft.Windows.Controls.Ribbon;
using System.Windows.Input;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Obfuscation]
    public static class HelpButtonHelper
    {
        private static readonly string helpIcon = "pack://application:,,,/GisEditorToolkits;component/Images/help.png";

        public static FrameworkElement GetHelpButton(string helpUri, HelpButtonMode helpButtonMode, ICommand clickCommand = null)
        {
            return GetHelpButton(helpUri, helpButtonMode, new Uri(helpIcon, UriKind.Absolute), clickCommand);
        }

        public static FrameworkElement GetHelpButton(string helpUri, HelpButtonMode helpButtonMode, Uri iconSource, ICommand clickCommand)
        {
            FrameworkElement frameworkElement = null;
            string helpContentKey = "HelpHeader";
            switch (helpButtonMode)
            {
                case HelpButtonMode.RibbonButton:
                    RibbonButton ribbonButton = new RibbonButton();
                    ribbonButton.LargeImageSource = new BitmapImage(iconSource);
                    ribbonButton.SmallImageSource = new BitmapImage(iconSource);
                    ribbonButton.SetResourceReference(RibbonButton.LabelProperty, helpContentKey);
                    ribbonButton.Click += NavigateToHelpUri_Click;
                    frameworkElement = ribbonButton;
                    break;

                case HelpButtonMode.IconWithLabel:
                    StackPanel stackPanel = GetButtonContainer(helpUri, helpContentKey, iconSource);
                    stackPanel.MouseLeftButtonUp += NavigateToHelpUri_Click;
                    frameworkElement = stackPanel;
                    break;

                case HelpButtonMode.IconOnly:
                    frameworkElement = GetImageButton(iconSource);
                    frameworkElement.MouseLeftButtonUp += NavigateToHelpUri_Click;
                    break;

                case HelpButtonMode.Default:
                case HelpButtonMode.NormalButton:
                default:
                    Button button = new Button { Content = GetButtonContainer(helpUri, helpContentKey, iconSource) };
                    button.Click += NavigateToHelpUri_Click;
                    frameworkElement = button;
                    break;
            }

            frameworkElement.SetResourceReference(FrameworkElement.ToolTipProperty, helpContentKey);
            frameworkElement.Tag = string.IsNullOrEmpty(helpUri) ? (object)clickCommand : helpUri;
            return frameworkElement;
        }

        private static StackPanel GetButtonContainer(string helpUri, string helpContentKey, Uri iconSource)
        {
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;
            stackPanel.Children.Add(GetImageButton(iconSource));
            TextBlock textBlock = new TextBlock() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(3, -1, 0, 0) };
            textBlock.SetResourceReference(TextBlock.TextProperty, helpContentKey);
            stackPanel.Children.Add(textBlock);
            return stackPanel;
        }

        private static Image GetImageButton(Uri iconSource)
        {
            return new Image { Source = new BitmapImage(iconSource), Width = 16, Height = 16 };
        }

        private static void NavigateToHelpUri_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement button = sender as FrameworkElement;
            if (button != null)
            {
                if (button.Tag is string)
                {
                    string uri = (string)button.Tag;
                    if (!string.IsNullOrEmpty(uri)) Process.Start(uri);
                }
                else if (button.Tag is RelayCommand)
                {
                    ((RelayCommand)(button.Tag)).Execute(null);
                }
            }
        }
    }
}