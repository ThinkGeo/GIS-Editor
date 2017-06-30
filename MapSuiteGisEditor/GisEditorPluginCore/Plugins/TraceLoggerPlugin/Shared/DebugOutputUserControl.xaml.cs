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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for DebugOutputUserControl.xaml
    /// </summary>
    public partial class DebugOutputUserControl : UserControl
    {
        private static LinearGradientBrush highlightBrush;
        private static SolidColorBrush transparentBrush;
        private static SolidColorBrush borderBrush;

        private DebugOutputViewModel viewModel;
        private bool isDesending;
        private FindWindow findWindow;
        private bool findWindowOpened;

        static DebugOutputUserControl()
        {
            transparentBrush = new SolidColorBrush(Colors.Transparent);
            borderBrush = new SolidColorBrush(Color.FromRgb(255, 183, 0));

            highlightBrush = new LinearGradientBrush();
            highlightBrush.StartPoint = new Point(0, 0);
            highlightBrush.EndPoint = new Point(0, 1);
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(254, 251, 244), 0));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(253, 231, 206), 0.19));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(253, 222, 184), 0.39));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 206, 107), 0.39));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 222, 154), 0.79));
            highlightBrush.GradientStops.Add(new GradientStop(Color.FromRgb(255, 235, 170), 1));
        }

        public DebugOutputUserControl()
        {
            InitializeComponent();

            viewModel = new DebugOutputViewModel();
            viewModel.SearchFinished = () => LoggerList.ScrollIntoView(viewModel.DisplayLoggerMessages.LastOrDefault());
            DataContext = viewModel;
        }

        [Obfuscation]
        private void GridViewColumnHeader_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            GridViewColumnHeader gridViewColumnHeader = e.OriginalSource as GridViewColumnHeader;
            if (gridViewColumnHeader != null && gridViewColumnHeader.Column != null)
            {
                string columnName = gridViewColumnHeader.Column.Header as string;
                if (columnName != null)
                {
                    LoggerMessageViewModel[] messages = null;
                    switch (columnName)
                    {
                        case "Message":
                            messages = !isDesending ? viewModel.DisplayLoggerMessages.OrderBy(m => m.LoggerMessage.Message).ToArray() : viewModel.DisplayLoggerMessages.OrderByDescending(m => m.LoggerMessage.Message).ToArray();
                            break;

                        case "Date":
                            messages = !isDesending ? viewModel.DisplayLoggerMessages.OrderBy(m => m.LoggerMessage.DateTime).ToArray() : viewModel.DisplayLoggerMessages.OrderByDescending(m => m.LoggerMessage.DateTime).ToArray();
                            break;

                        case "Level":
                            messages = !isDesending ? viewModel.DisplayLoggerMessages.OrderBy(m => m.LoggerMessage.LoggerLevel).ToArray() : viewModel.DisplayLoggerMessages.OrderByDescending(m => m.LoggerMessage.LoggerLevel).ToArray();
                            break;

                        case "Category":
                            messages = !isDesending ? viewModel.DisplayLoggerMessages.OrderBy(m => m.LoggerMessage.Category).ToArray() : viewModel.DisplayLoggerMessages.OrderByDescending(m => m.LoggerMessage.Category).ToArray();
                            break;
                    }
                    isDesending = !isDesending;
                    viewModel.DisplayLoggerMessages.Clear();
                    if (messages != null && messages.Length > 0)
                    {
                        foreach (var item in messages)
                        {
                            viewModel.DisplayLoggerMessages.Add(item);
                        }
                    }
                }
            }
        }

        [Obfuscation]
        private void FindClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!findWindowOpened)
            {
                Action<LoggerMessageViewModel> action = new Action<LoggerMessageViewModel>((loggerMessage) =>
                {
                    LoggerList.ScrollIntoView(loggerMessage);
                    viewModel.SelectedLoggerMessage = loggerMessage;
                });
                findWindow = new FindWindow(viewModel.DisplayLoggerMessages, action);
                findWindow.Closed += FindWindow_Closed;
                findWindow.Show();
                findWindowOpened = true;
            }
        }

        private void FindWindow_Closed(object sender, EventArgs e)
        {
            findWindowOpened = false;
        }

        [Obfuscation]
        private void ExportClick(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = string.Format(CultureInfo.InvariantCulture, "MapSuiteGisEditor_{0:yyyy_MM_dd}.log", DateTime.Now);
            saveFileDialog.Filter = "Log Files (*.log)|*.log";
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                viewModel.ExportToFile(saveFileDialog.FileName);
            }
        }

        [Obfuscation]
        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Border)sender).BorderBrush = borderBrush;
            ((Border)sender).Background = highlightBrush;
        }

        [Obfuscation]
        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Border)sender).BorderBrush = transparentBrush;
            ((Border)sender).Background = transparentBrush;
        }
    }
}