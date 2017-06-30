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
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class ConfigCsvFileViewModel : ViewModelBase
    {
        private bool isShapeTypeEnabled;
        private bool isLayerNameEnabled;
        private bool isDelimiterEnabled;
        private bool isOutputEnabled;

        private bool isCustomDelimiterEnabled;
        private string customDelimiter;

        private string delimiter;
        private KeyValuePair<string, string> selectedDelimiter;
        private GeneralShapeFileType selectedShapeType;
        private Collection<GeneralShapeFileType> shapeTypes;
        private string fileName;
        private ObservableCollection<AddNewCsvColumnViewModel> allCsvColumns;
        private string outputFolder;
        private DelimitedSpatialColumnsType mappingType;
        private FeatureLayer featureLayer;

        private RelayCommand addNewCommand;

        public ConfigCsvFileViewModel(FeatureLayer featureLayer)
            : base()
        {
            this.featureLayer = featureLayer;

            shapeTypes = new Collection<GeneralShapeFileType>();
            shapeTypes.Add(GeneralShapeFileType.Point);
            shapeTypes.Add(GeneralShapeFileType.Line);
            shapeTypes.Add(GeneralShapeFileType.Area);
            allCsvColumns = new ObservableCollection<AddNewCsvColumnViewModel>();
            allCsvColumns.CollectionChanged += new NotifyCollectionChangedEventHandler(AllCsvColumns_CollectionChanged);
            CsvFeatureLayer delimitedFeatureLayer = this.featureLayer as CsvFeatureLayer;

            if (delimitedFeatureLayer != null)
            {
                LoadFromLayer(delimitedFeatureLayer);
            }
            else
            {
                IsShapeTypeEnabled = true;
                IsLayerNameEnabled = true;
                IsDelimiterEnabled = true;
                IsOutputEnabled = true;
                SelectedShapeType = GeneralShapeFileType.Point;
                SelectedDelimiter = new KeyValuePair<string, string>("Comma", ",");
                outputFolder = ConfigShapeFileViewModel.GetDefaultOutputPath();
            }
        }

        private void LoadFromLayer(CsvFeatureLayer delimitedFeatureLayer)
        {
            bool iswkt = !string.IsNullOrEmpty(delimitedFeatureLayer.WellKnownTextColumnName);
            Collection<CsvColumnType> csvColumnTypes = new Collection<CsvColumnType>();
            csvColumnTypes.Add(CsvColumnType.String);
            if (iswkt)
            {
                csvColumnTypes.Add(CsvColumnType.WKT);
            }
            else
            {
                csvColumnTypes.Add(CsvColumnType.Longitude);
                csvColumnTypes.Add(CsvColumnType.Latitude);
            }


            FileName = delimitedFeatureLayer.Name;
            OutputFolder = Path.GetDirectoryName(delimitedFeatureLayer.DelimitedPathFilename);

            delimitedFeatureLayer.SafeProcess(() =>
            {
                foreach (FeatureSourceColumn item in delimitedFeatureLayer.QueryTools.GetColumns())
                {
                    AddNewCsvColumnViewModel addNewCsvColumnViewModel = new AddNewCsvColumnViewModel(csvColumnTypes);
                    addNewCsvColumnViewModel.ColumnName = item.ColumnName;
                    if (item.ColumnName == delimitedFeatureLayer.WellKnownTextColumnName)
                    {
                        addNewCsvColumnViewModel.SelectedCsvColumnType = CsvColumnType.WKT;
                    }
                    else if (item.ColumnName == delimitedFeatureLayer.XColumnName)
                    {
                        addNewCsvColumnViewModel.SelectedCsvColumnType = CsvColumnType.Longitude;
                    }
                    else if (item.ColumnName == delimitedFeatureLayer.YColumnName)
                    {
                        addNewCsvColumnViewModel.SelectedCsvColumnType = CsvColumnType.Latitude;
                    }
                    else
                    {
                        addNewCsvColumnViewModel.SelectedCsvColumnType = CsvColumnType.String;
                    }

                    allCsvColumns.Add(addNewCsvColumnViewModel);
                }

            });

            DelimiterDictionary delimiterDictionary = new DelimiterDictionary();
            if (delimiterDictionary.ContainsValue(delimitedFeatureLayer.Delimiter))
            {
                SelectedDelimiter = delimiterDictionary.FirstOrDefault(item => item.Value == delimitedFeatureLayer.Delimiter);
            }
            else
            {
                SelectedDelimiter = delimiterDictionary.FirstOrDefault(item => item.Key == "Custom");
                Delimiter = delimitedFeatureLayer.Delimiter;
                IsCustomDelimiterEnabled = false;
            }
            if (!iswkt)
            {
                SelectedShapeType = GeneralShapeFileType.Point;
            }
            else
            {
                delimitedFeatureLayer.SafeProcess(() =>
                {
                    Feature firstFeature = delimitedFeatureLayer.QueryTools.GetFeatureById("1", ReturningColumnsType.NoColumns);
                    if (firstFeature != null)
                    {
                        BaseShape baseShape = firstFeature.GetShape();
                        if (baseShape is AreaBaseShape)
                        {
                            SelectedShapeType = GeneralShapeFileType.Area;
                        }
                        else if (baseShape is LineBaseShape)
                        {
                            SelectedShapeType = GeneralShapeFileType.Line;
                        }
                        else if (baseShape is PointBaseShape)
                        {
                            SelectedShapeType = GeneralShapeFileType.Point;
                        }
                    }
                    else
                    {
                        SelectedShapeType = GeneralShapeFileType.Point;
                    }
                });
            }
        }

        private void AllCsvColumns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            GetAvailableCsvColumnType();
        }

        internal Collection<CsvColumnType> GetAvailableCsvColumnType()
        {
            var allRemindingCsvColumns = allCsvColumns.Where(c => c.ChangedStatus != FeatureSourceColumnChangedStatus.Deleted).ToList();
            Collection<CsvColumnType> csvColumnTypes = new Collection<CsvColumnType>();
            if (allRemindingCsvColumns.Any(c => c.SelectedCsvColumnType == CsvColumnType.WKT))
            {
                csvColumnTypes = new Collection<CsvColumnType> { CsvColumnType.String };
            }
            else if (allRemindingCsvColumns.Any(c => c.SelectedCsvColumnType == CsvColumnType.Latitude) && allCsvColumns.Any(c => c.SelectedCsvColumnType == CsvColumnType.Longitude))
            {
                csvColumnTypes = new Collection<CsvColumnType> { CsvColumnType.String };
            }
            else if (allRemindingCsvColumns.Any(c => c.SelectedCsvColumnType == CsvColumnType.Latitude))
            {
                csvColumnTypes = new Collection<CsvColumnType> { CsvColumnType.String, CsvColumnType.Longitude };
            }
            else if (allRemindingCsvColumns.Any(c => c.SelectedCsvColumnType == CsvColumnType.Longitude))
            {
                csvColumnTypes = new Collection<CsvColumnType> { CsvColumnType.String, CsvColumnType.Latitude };
            }
            else
            {
                csvColumnTypes = new Collection<CsvColumnType> { CsvColumnType.String, CsvColumnType.Longitude, CsvColumnType.Latitude, CsvColumnType.WKT };
            }
            return csvColumnTypes;
        }

        public bool IsShapeTypeEnabled
        {
            get { return isShapeTypeEnabled; }
            set
            {
                isShapeTypeEnabled = value;
                RaisePropertyChanged(()=>IsShapeTypeEnabled);
            }
        }

        public bool IsLayerNameEnabled
        {
            get { return isLayerNameEnabled; }
            set
            {
                isLayerNameEnabled = value;
                RaisePropertyChanged(()=>IsLayerNameEnabled);
            }
        }

        public bool IsDelimiterEnabled
        {
            get { return isDelimiterEnabled; }
            set
            {
                isDelimiterEnabled = value;
                RaisePropertyChanged(()=>IsDelimiterEnabled);
            }
        }

        public bool IsOutputEnabled
        {
            get { return isOutputEnabled; }
            set
            {
                isOutputEnabled = value;
                RaisePropertyChanged(()=>IsOutputEnabled);
            }
        }

        public string Delimiter
        {
            get { return delimiter; }
            set { delimiter = value; }
        }

        public KeyValuePair<string, string> SelectedDelimiter
        {
            get { return selectedDelimiter; }
            set
            {
                selectedDelimiter = value;
                IsCustomDelimiterEnabled = value.Key.Equals("Custom", StringComparison.InvariantCulture);
                if (IsCustomDelimiterEnabled && !string.IsNullOrEmpty(CustomDelimiter))
                {
                    Delimiter = CustomDelimiter;
                }
                else
                {
                    Delimiter = value.Value;
                }
                RaisePropertyChanged(()=>SelectedDelimiter);
                RaisePropertyChanged(()=>IsCustomDelimiterEnabled);
            }
        }

        public string CustomDelimiter
        {
            get { return customDelimiter; }
            set
            {
                customDelimiter = value;
                Delimiter = value;
                RaisePropertyChanged(()=>CustomDelimiter);
            }
        }

        public bool IsCustomDelimiterEnabled
        {
            get { return isCustomDelimiterEnabled; }
            set { isCustomDelimiterEnabled = value; }
        }

        public GeneralShapeFileType SelectedShapeType
        {
            get { return selectedShapeType; }
            set
            {
                selectedShapeType = value;
                RaisePropertyChanged(()=>SelectedShapeType);
            }
        }

        public Collection<GeneralShapeFileType> ShapeTypes
        {
            get { return shapeTypes; }
        }

        public string FileName
        {
            get { return fileName; }
            set
            {
                fileName = value;
                RaisePropertyChanged(()=>FileName);
            }
        }

        public ObservableCollection<AddNewCsvColumnViewModel> CsvColumns
        {
            get { return allCsvColumns; }
        }

        public string OutputFolder
        {
            get { return outputFolder; }
            set
            {
                outputFolder = value;
                RaisePropertyChanged(()=>OutputFolder);
            }
        }

        public DelimitedSpatialColumnsType MappingType
        {
            get
            {
                mappingType = DelimitedSpatialColumnsType.XAndY;
                if (allCsvColumns.Any(c => c.SelectedCsvColumnType == CsvColumnType.WKT))
                {
                    mappingType = DelimitedSpatialColumnsType.WellKnownText;
                }
                return mappingType;
            }
        }

        public RelayCommand AddNewCommand
        {
            get
            {
                if (addNewCommand == null)
                {
                    addNewCommand = new RelayCommand(() =>
                    {
                        AddNewCsvColumnWindow addNewWindow = new AddNewCsvColumnWindow(GetAvailableCsvColumnType());
                        //addNewWindow.ViewModel.MappingType = mappingType;
                        if (addNewWindow.ShowDialog().GetValueOrDefault())
                        {
                            addNewWindow.ViewModel.ChangedStatus = FeatureSourceColumnChangedStatus.Added;
                            allCsvColumns.Add(addNewWindow.ViewModel);
                        }
                    });
                }
                return addNewCommand;
            }
        }


    }
}
