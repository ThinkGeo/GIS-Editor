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


using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.GeocodeServerSdk;
using System.Reflection;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for StepOfSelectingColumnsToRetain.xaml
    /// </summary>
    public partial class StepOfSelectingColumnsToRetain : UserControl
    {
        private Dictionary<DataRow, Collection<GeocodeMatch>> dictionary = new Dictionary<DataRow, Collection<GeocodeMatch>>();
        private GeocoderWizardSharedObject sharedObject;

        public StepOfSelectingColumnsToRetain(GeocoderWizardSharedObject parameter)
        {
            InitializeComponent();
            DataContext = parameter;
            sharedObject = parameter;
        }

        [Obfuscation]
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            cbFinal.IsChecked = false;
            cbGeocoder.IsChecked = false;
            cbOriginal.IsChecked = false;
        }

        [Obfuscation]
        private void OriginalColumnsToFinalClick(object sender, RoutedEventArgs e)
        {
            Collection<string> tmpCollection = new Collection<string>();
            foreach (string item in originalList.SelectedItems)
            {
                sharedObject.RetainedColumns.Add(item + " (I)");
                tmpCollection.Add(item);
            }
            foreach (string item in tmpCollection)
            {
                sharedObject.InputFileColumns.Remove(item);
            }
            sharedObject.CreateResultsPreview();
        }

        [Obfuscation]
        private void GeocodedColumnsToFinalClick(object sender, RoutedEventArgs e)
        {
            Collection<string> tmpCollection = new Collection<string>();
            foreach (string item in geocoderList.SelectedItems)
            {
                sharedObject.RetainedColumns.Add(item + " (O)");
                tmpCollection.Add(item);
            }
            foreach (string item in tmpCollection)
            {
                sharedObject.GeocoderColumns.Remove(item);
            }
            sharedObject.CreateResultsPreview();
        }

        [Obfuscation]
        private void FinalColumnsToBackClick(object sender, RoutedEventArgs e)
        {
            Collection<string> tmpCollection = new Collection<string>();
            foreach (string item in finalList.SelectedItems)
            {
                tmpCollection.Add(item);
                if (item.Contains("(O)"))
                {
                    sharedObject.GeocoderColumns.Add(item.Replace(" (O)", ""));
                }
                else
                {
                    sharedObject.InputFileColumns.Add(item.Replace(" (I)", ""));
                }
            }
            foreach (string item in tmpCollection)
            {
                sharedObject.RetainedColumns.Remove(item);
            }
            sharedObject.InputFileColumns.Sort();
            sharedObject.GeocoderColumns.Sort();
            sharedObject.CreateResultsPreview();
        }

        [Obfuscation]
        private void DownClick(object sender, RoutedEventArgs e)
        {
            Collection<string> tmpCollection = new Collection<string>();
            foreach (string item in finalList.SelectedItems)
            {
                tmpCollection.Add(item);
            }
            foreach (string item in tmpCollection)
            {
                int index = sharedObject.RetainedColumns.IndexOf(item);
                if (index + 1 == sharedObject.RetainedColumns.Count)
                {
                    break;
                }
                sharedObject.RetainedColumns.Move(index, index + 1);
            }
            sharedObject.CreateResultsPreview();
        }

        [Obfuscation]
        private void UpClick(object sender, RoutedEventArgs e)
        {
            Collection<string> tmpCollection = new Collection<string>();
            foreach (string item in finalList.SelectedItems)
            {
                tmpCollection.Add(item);
            }
            foreach (string item in tmpCollection)
            {
                int index = sharedObject.RetainedColumns.IndexOf(item);
                if (index == 0)
                {
                    break;
                }
                sharedObject.RetainedColumns.Move(index, index - 1);
            }
            sharedObject.CreateResultsPreview();
        }

        [Obfuscation]
        private void FinalColumnsSelectAllClick(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                if ((bool)checkBox.IsChecked)
                {
                    finalList.SelectAll();
                }
                else
                {
                    finalList.SelectedIndex = -1;
                }
            }
        }

        [Obfuscation]
        private void GeocodeColumnsSelectAllClick(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                if ((bool)checkBox.IsChecked)
                {
                    geocoderList.SelectAll();
                }
                else
                {
                    geocoderList.SelectedIndex = -1;
                }
            }
        }

        [Obfuscation]
        private void OriginalColumnsSelectAllClick(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                if ((bool)checkBox.IsChecked)
                {
                    originalList.SelectAll();
                }
                else
                {
                    originalList.SelectedIndex = -1;
                }
            }
        }

        [Obfuscation]
        private void originalList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbOriginal.IsChecked = originalList.SelectedItems.Count == 0 ? false : originalList.SelectedItems.Count == originalList.Items.Count;
        }

        [Obfuscation]
        private void geocoderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbGeocoder.IsChecked = geocoderList.SelectedItems.Count == 0 ? false : geocoderList.SelectedItems.Count == geocoderList.Items.Count;
        }

        [Obfuscation]
        private void finalList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cbFinal.IsChecked = finalList.SelectedItems.Count == 0 ? false : finalList.SelectedItems.Count == finalList.Items.Count;
        }
    }
}