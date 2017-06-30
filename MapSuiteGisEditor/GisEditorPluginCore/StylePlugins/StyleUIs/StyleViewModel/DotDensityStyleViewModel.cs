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
using System.Windows.Threading;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DotDensityStyleViewModel : StyleViewModel
    {
        private double pointValueRatioY;
        private PointStyleType selectedPointStyleType;
        private double recommendPointValueRatio;

        [NonSerialized]
        private DispatcherTimer timer;

        private DotDensityStyle actualDotDensityStyle;
        private Dictionary<string, string> requiredColumnNames;

        public DotDensityStyleViewModel(DotDensityStyle style, StyleBuilderArguments requiredValues)
            : base(style)
        {
            HelpKey = "DotDensityAreaStyleHelp";

            ActualObject = style;
            actualDotDensityStyle = style;
            if (style.PointToValueRatio == 1.0) RecommendPointValueRatio = 0.00001;
            else recommendPointValueRatio = style.PointToValueRatio;
            SelectedPointStyleType = GetPointStyleType(style.CustomPointStyle);
            InitRequiredColumnNames(requiredValues);
            InitializeWarningTimer();
            InitializePointValueRatio();
        }

        public Dictionary<string, string> RequiredColumnNames
        {
            get { return requiredColumnNames; }
        }

        public bool IsRecommandEnabled
        {
            get { return !string.IsNullOrEmpty(ColumnName); }
        }

        public string ColumnName
        {
            get { return SelectedColumnName.Key; }
        }

        public KeyValuePair<string, string> SelectedColumnName
        {
            get
            {
                return RequiredColumnNames.FirstOrDefault(c => c.Key == actualDotDensityStyle.ColumnName);
            }
            set
            {
                actualDotDensityStyle.ColumnName = value.Key;
                RaisePropertyChanged("SelectedColumnName");
                RaisePropertyChanged("IsRecommandEnabled");
            }
        }

        public double PointValueRatioY
        {
            get { return pointValueRatioY; }
            set
            {
                if (value <= 0)
                {
                }
                else
                {
                    pointValueRatioY = value;
                    RaisePropertyChanged("PointValueRatioY");
                    StartWarningPointValueRatioIsTooBig();
                }
            }
        }

        public double RecommendPointValueRatio
        {
            get
            {
                return recommendPointValueRatio;
            }
            set
            {
                recommendPointValueRatio = value;
                RaisePropertyChanged("RecommendPointValueRatio");
            }
        }

        public PointStyleType SelectedPointStyleType
        {
            get { return selectedPointStyleType; }
            set
            {
                selectedPointStyleType = value;

                PointStyle pointStyle = actualDotDensityStyle.CustomPointStyle;
                if (pointStyle != null && pointStyle.CheckIsValid())
                {
                    if (pointStyle.PointType != (PointType)((int)SelectedPointStyleType - 1))
                    {
                        Styles.Style newStyle = null;
                        switch (SelectedPointStyleType)
                        {
                            case PointStyleType.Simple:
                                newStyle = new SimplePointStylePlugin().GetDefaultStyle();
                                break;

                            case PointStyleType.CustomSymbol:
                                newStyle = new CustomSymbolPointStylePlugin().GetDefaultStyle();
                                break;

                            case PointStyleType.Font:
                                newStyle = new FontPointStylePlugin().GetDefaultStyle();
                                break;
                            default:
                                break;
                        }
                        if (newStyle != null) actualDotDensityStyle.CustomPointStyle = (PointStyle)newStyle;
                    }
                }
                RaisePropertyChanged("SelectedPointStyleType");
            }
        }

        private void InitializePointValueRatio()
        {
            if (!ActualObject.CheckIsValid())
            {
                PointValueRatioY = 100000;
                actualDotDensityStyle.PointToValueRatio = RecommendPointValueRatio;
            }
            else
            {
                PointValueRatioY = 1 / actualDotDensityStyle.PointToValueRatio;
            }
        }

        public void RaiseInnerPointStyleChanged()
        {
            RaisePropertyChanged("SelectedPointStyleType");
        }

        private void StartWarningPointValueRatioIsTooBig()
        {
            if (timer.IsEnabled) timer.Stop();
            timer.Start();
        }

        private void WarnPointValueRatioIsTooBig()
        {
            if (1 / (PointValueRatioY + 1) > RecommendPointValueRatio)
            {
                NotifyPointValueRatioIsBeyondTheRecommendedValue(actualDotDensityStyle);
            }
            else actualDotDensityStyle.PointToValueRatio = 1 / PointValueRatioY;
        }

        private void NotifyPointStyleTypeChanged(DotDensityStyle style)
        {
            var result = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ChangingPointTypeWillDeleteText"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Asterisk);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                style.CustomPointStyle = null;
            }
            else
            {
                SelectedPointStyleType = GetPointStyleType(style.CustomPointStyle);
            }
        }

        private void NotifyPointValueRatioIsBeyondTheRecommendedValue(DotDensityStyle style)
        {
            var result = System.Windows.Forms.MessageBox.Show(String.Format(GisEditor.LanguageManager.GetStringResource("CurrentDotsValueTooBigText"), Math.Round(1.0 / RecommendPointValueRatio)), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"), System.Windows.Forms.MessageBoxButtons.OKCancel, System.Windows.Forms.MessageBoxIcon.Warning);

            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                PointValueRatioY = 1 / RecommendPointValueRatio;
            }
            else
            {
                style.PointToValueRatio = 1 / PointValueRatioY;
            }
        }

        private void InitRequiredColumnNames(StyleBuilderArguments requiredValues)
        {
            requiredColumnNames = new Dictionary<string, string>();

            if (requiredValues != null && requiredValues.FeatureLayer != null)
            {
                foreach (var columnName in CollectNumberColumnNamesFromFeatureLayer(requiredValues.FeatureLayer))
                {
                    string alias = requiredValues.FeatureLayer.FeatureSource.GetColumnAlias(columnName);
                    requiredColumnNames.Add(columnName, alias);
                }
            }
        }

        private static Collection<string> CollectNumberColumnNamesFromFeatureLayer(FeatureLayer featureLayer)
        {
            Collection<string> columnNames = new Collection<string>();
            featureLayer.SafeProcess(() =>
            {
                var checkIsNumberColumn = new Func<FeatureSourceColumn, bool>(tmpColumn =>
                    tmpColumn != null &&
                    (tmpColumn.TypeName.Equals(DbfColumnType.Float.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    tmpColumn.TypeName.Equals(DbfColumnType.Numeric.ToString(), StringComparison.OrdinalIgnoreCase))
                );
                var columns = featureLayer.QueryTools.GetColumns()
                    .Where(tmpColumn => checkIsNumberColumn(tmpColumn))
                    .Select(tmpColom => tmpColom.ColumnName);

                foreach (var columnName in columns)
                {
                    columnNames.Add(columnName);
                }
            });

            return columnNames;
        }

        private static PointStyleType GetPointStyleType(Styles.PointStyle style)
        {
            switch (style.PointType)
            {
                case PointType.Bitmap:
                    return PointStyleType.CustomSymbol;
                case PointType.Character:
                    return PointStyleType.Font;
                case PointType.Symbol:
                default:
                    return PointStyleType.Simple;
            }
        }

        private void InitializeWarningTimer()
        {
            timer = new DispatcherTimer();
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                WarnPointValueRatioIsTooBig();
            };
        }
    }
}