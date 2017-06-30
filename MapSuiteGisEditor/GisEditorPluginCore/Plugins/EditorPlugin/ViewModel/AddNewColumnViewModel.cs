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


using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Windows;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class AddNewColumnViewModel : ViewModelBase
    {
        private DbfColumnType columnType;
        private string columnName;
        private string aliasName;
        private int length;
        private int decimalLength;
        private bool decimalLengthIsEnable;
        private bool lengthIsEnable;
        private bool columnTypeEnable;
        private Visibility isLengthUnitVisbility;
        private Visibility isMeasurementUnitVisbility;
        private List<string> columnNames;
        private DbfColumnMode columnMode;
        private Visibility isCalculatedVisbility;
        private CalculatedDbfColumnType calculationType;
        private AreaUnit measurementUnit;
        private DistanceUnit lengthUnit;

        [NonSerialized]
        private RelayCommand okCommand;
        private bool isEmptyChecked;
        private bool isCalculatedChecked;

        public AddNewColumnViewModel()
        {
            columnType = DbfColumnType.Character;
            columnName = string.Empty;
            length = 20;
            decimalLength = 0;
            decimalLengthIsEnable = false;
            lengthIsEnable = true;
            columnTypeEnable = true;

            isCalculatedVisbility = Visibility.Collapsed;
            calculationType = CalculatedDbfColumnType.Area;
            measurementUnit = AreaUnit.SquareMeters;
            isLengthUnitVisbility = Visibility.Hidden;
            IsEdited = false;
            isEmptyChecked = true;
        }

        public bool IsEdited { get; set; }

        public DbfColumnType ColumnType
        {
            get { return columnType; }
            set
            {
                columnType = value;
                RaisePropertyChanged(() => ColumnType);

                switch (ColumnType)
                {
                    case DbfColumnType.Float:
                        DecimalLengthIsEnable = true;
                        LengthIsEnable = true;
                        Length = 10;
                        DecimalLength = 5;
                        break;

                    case DbfColumnType.Date:
                        Length = 8;
                        DecimalLength = 0;
                        LengthIsEnable = false;
                        DecimalLengthIsEnable = false;
                        break;

                    case DbfColumnType.Memo:
                        Length = 10;
                        DecimalLength = 0;
                        LengthIsEnable = false;
                        DecimalLengthIsEnable = false;
                        break;
                    case DbfColumnType.Numeric:
                        Length = 10;
                        DecimalLength = 0;
                        LengthIsEnable = true;
                        DecimalLengthIsEnable = false;
                        break;

                    case DbfColumnType.Character:
                        Length = 20;
                        DecimalLength = 0;
                        LengthIsEnable = true;
                        DecimalLengthIsEnable = false;
                        break;

                    case DbfColumnType.Logical:
                        Length = 1;
                        DecimalLength = 0;
                        LengthIsEnable = false;
                        DecimalLengthIsEnable = false;
                        break;
                    default:
                        DecimalLength = 0;
                        DecimalLengthIsEnable = false;
                        LengthIsEnable = true;
                        break;
                }
            }
        }

        public bool ColumnTypeEnable
        {
            get { return columnTypeEnable; }
            set
            {
                columnTypeEnable = value;
                RaisePropertyChanged(() => ColumnTypeEnable);
            }
        }

        public string ColumnName
        {
            get { return columnName; }
            set
            {
                columnName = value;
                AliasName = columnName;
                RaisePropertyChanged(() => ColumnName);
            }
        }

        public string AliasName
        {
            get { return aliasName; }
            set
            {
                string tempAliasName = aliasName;
                aliasName = value;
                if (tempAliasName != aliasName)
                {
                    IsEdited = true;
                }
                else
                {
                    IsEdited = false;
                }
                RaisePropertyChanged(() => AliasName);
            }
        }

        public int Length
        {
            get { return length; }
            set
            {
                length = value;
                RaisePropertyChanged(() => Length);
            }
        }

        public int DecimalLength
        {
            get { return decimalLength; }
            set
            {
                decimalLength = value;
                RaisePropertyChanged(() => DecimalLength);
            }
        }

        public bool DecimalLengthIsEnable
        {
            get { return decimalLengthIsEnable; }
            set
            {
                decimalLengthIsEnable = value;
                RaisePropertyChanged(() => DecimalLengthIsEnable);
            }
        }

        public Visibility IsLengthUnitVisbility
        {
            get { return isLengthUnitVisbility; }
            set
            {
                isLengthUnitVisbility = value;
                RaisePropertyChanged(() => IsLengthUnitVisbility);
            }
        }

        public Visibility IsMeasurementUnitVisbility
        {
            get { return isMeasurementUnitVisbility; }
            set
            {
                isMeasurementUnitVisbility = value;
                RaisePropertyChanged(() => IsMeasurementUnitVisbility);
            }
        }

        public bool LengthIsEnable
        {
            get { return lengthIsEnable; }
            set
            {
                lengthIsEnable = value;
                RaisePropertyChanged(() => LengthIsEnable);
            }
        }

        public bool IsEmptyChecked
        {
            get { return isEmptyChecked; }
            set
            {
                isEmptyChecked = value;
                if (isEmptyChecked) ColumnMode = DbfColumnMode.Empty;
                RaisePropertyChanged(() => IsEmptyChecked);
            }
        }

        public bool IsCalculatedChecked
        {
            get { return isCalculatedChecked; }
            set
            {
                isCalculatedChecked = value;
                if (isCalculatedChecked) ColumnMode = DbfColumnMode.Calculated;
                RaisePropertyChanged(() => IsCalculatedChecked);
            }
        }

        public DbfColumnMode ColumnMode
        {
            get { return columnMode; }
            set
            {
                columnMode = value;
                RaisePropertyChanged(() => ColumnMode);

                if (columnMode == DbfColumnMode.Calculated)
                {
                    IsCalculatedVisbility = Visibility.Visible;
                    ColumnType = DbfColumnType.Float;
                    ColumnTypeEnable = true;
                    LengthIsEnable = true;
                    Length = 20;
                    DecimalLength = 5;
                }
                else if (columnMode == DbfColumnMode.Empty)
                {
                    IsCalculatedVisbility = Visibility.Collapsed;
                    ColumnTypeEnable = true;
                    LengthIsEnable = true;
                }
            }
        }

        public AreaUnit MeasurementUnit
        {
            get { return measurementUnit; }
            set
            {
                measurementUnit = value;
                RaisePropertyChanged(() => MeasurementUnit);
            }
        }

        public DistanceUnit LengthUnit
        {
            get { return lengthUnit; }
            set
            {
                lengthUnit = value;
                RaisePropertyChanged(() => LengthUnit);
            }
        }

        public CalculatedDbfColumnType CalculationType
        {
            get { return calculationType; }
            set
            {
                calculationType = value;
                RaisePropertyChanged(() => CalculationType);

                if (calculationType == CalculatedDbfColumnType.Length || calculationType == CalculatedDbfColumnType.Perimeter)
                {
                    IsLengthUnitVisbility = Visibility.Visible;
                    IsMeasurementUnitVisbility = Visibility.Hidden;
                }
                else
                {
                    IsMeasurementUnitVisbility = Visibility.Visible;
                    IsLengthUnitVisbility = Visibility.Hidden;
                }
            }
        }

        public Visibility IsCalculatedVisbility
        {
            get { return isCalculatedVisbility; }
            set
            {
                isCalculatedVisbility = value;
                RaisePropertyChanged(() => IsCalculatedVisbility);
            }
        }

        public List<string> ColumnNames
        {
            get { return columnNames; }
            internal set
            {
                columnNames = value;
            }
        }

        public RelayCommand OKCommand
        {
            get
            {
                if (okCommand == null)
                {
                    okCommand = new RelayCommand(() =>
                    {
                        if (ColumnMode != DbfColumnMode.Calculated)
                        {
                            if (ColumnType == DbfColumnType.Float && DecimalLength < 1)
                            {
                                if (System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("AddNewColumnViewModelColumnChangedText"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                                {
                                    Messenger.Default.Send(
                                        !string.IsNullOrEmpty(ColumnName) &&
                                        !ColumnNames.Contains(ColumnName) &&
                                        length > 0 && length <= 254
                                        , this);
                                }
                            }
                            else
                                Messenger.Default.Send(
                                        !string.IsNullOrEmpty(ColumnName) &&
                                        !ColumnNames.Contains(ColumnName) &&
                                        length > 0 && length <= 254
                                        , this);
                        }
                        else
                        {
                            Messenger.Default.Send(
                                !string.IsNullOrEmpty(ColumnName) &&
                                !ColumnNames.Contains(ColumnName) &&
                                decimalLength >= 0 &&
                                (ColumnType == DbfColumnType.Float || ColumnType == DbfColumnType.Numeric)
                                , this);
                        }
                    });
                }
                return okCommand;
            }
        }
    }
}