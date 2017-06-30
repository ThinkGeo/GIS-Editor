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
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Linq;
using System;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for PostgreConfigureWindow.xaml
    /// </summary>
    public partial class FeatureIdColumnWindow : Window
    {
        private FeatureColumnIdViewModel viewModel;
        private string featureIdColumn;

        public FeatureIdColumnWindow(IEnumerable<string> columnNames)
            : base()
        {
            InitializeComponent();

            viewModel = new FeatureColumnIdViewModel(columnNames);
            DataContext = viewModel;

            Messenger.Default.Register<string>(this, viewModel, msg =>
            {
                if (msg.Equals("OK", StringComparison.Ordinal))
                {
                    featureIdColumn = viewModel.SelectedColumn;
                    DialogResult = true;
                    viewModel.Cleanup();
                }
            });
        }

        public string FeatureIdColumn
        {
            get { return featureIdColumn; }
            set { featureIdColumn = value; }
        }
    }

    public class FeatureColumnIdViewModel : ViewModelBase
    {
        private Collection<string> columns;
        private string selectedColumn;
        private RelayCommand confirmCommand;

        public FeatureColumnIdViewModel(IEnumerable<string> columns)
        {
            this.columns = new Collection<string>();
            foreach (var item in columns)
            {
                this.columns.Add(item);
            }

            confirmCommand = new RelayCommand(() => MessengerInstance.Send("OK", this), () => !string.IsNullOrEmpty(SelectedColumn));

            if (this.columns.Contains("oid", StringComparer.OrdinalIgnoreCase))
            {
                SelectedColumn = this.columns.FirstOrDefault(c => c.Equals("oid", StringComparison.OrdinalIgnoreCase));
            }
            else if (this.columns.Contains("id", StringComparer.OrdinalIgnoreCase))
            {
                SelectedColumn = this.columns.FirstOrDefault(c => c.Equals("id", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                SelectedColumn = this.columns.FirstOrDefault();
            }
        }

        public Collection<string> Columns { get { return columns; } }

        public string SelectedColumn
        {
            get { return selectedColumn; }
            set
            {
                selectedColumn = value;
                RaisePropertyChanged(() => SelectedColumn);
                confirmCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand ConfirmCommand
        {
            get { return confirmCommand; }
        }
    }
}