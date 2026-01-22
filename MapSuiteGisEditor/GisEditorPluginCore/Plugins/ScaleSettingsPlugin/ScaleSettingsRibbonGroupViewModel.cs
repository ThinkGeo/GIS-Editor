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
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight;
using ThinkGeo.Core;

using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class ScaleSettingsRibbonGroupViewModel : ViewModelBase
    {
        private double value;
        private string scaleText;
        private ScaleWrapper selectedScale;
        private DistanceUnit selectedDistanceUnit;
        private ObservableCollection<ScaleWrapper> scales;

        public ScaleSettingsRibbonGroupViewModel()
        {
            scales = new ObservableCollection<ScaleWrapper>();
            selectedDistanceUnit = DistanceUnit.Feet;
        }

        public ObservableCollection<ScaleWrapper> Scales
        {
            get { return scales; }
        }

        public string ScaleText
        {
            get { return scaleText; }
            set
            {
                double tempScale = 0;
                if (double.TryParse(value, out tempScale))
                {
                    scaleText = value;
                    double unitScale = Conversion.ConvertMeasureUnits(tempScale, DistanceUnit.Inch, SelectedDistanceUnit);
                    this.value = unitScale;
                    RaisePropertyChanged(()=>ScaleText);
                    RaisePropertyChanged(()=>Value);
                }
            }
        }

        public ScaleWrapper SelectedScale
        {
            get { return selectedScale; }
            set
            {
                selectedScale = value;
                scaleText = GetZoomToScale().ToString("N2");
                RaisePropertyChanged(()=>SelectedScale);
                RaisePropertyChanged(()=>ScaleText);
            }
        }

        public double Value
        {
            get { return this.value; }
            set
            {
                this.value = value;
                scaleText = GetZoomToScale().ToString("N2");
                RaisePropertyChanged(()=>Value);
                RaisePropertyChanged(()=>ScaleText);
            }
        }

        public DistanceUnit SelectedDistanceUnit
        {
            get { return selectedDistanceUnit; }
            set
            {
                selectedDistanceUnit = value;
                RaisePropertyChanged(()=>SelectedDistanceUnit);
                if (GisEditor.ActiveMap != null)
                {
                    UpdateValues();
                    SelectedScale = scales.FirstOrDefault(s => s.Scale == GisEditor.ActiveMap.CurrentScale * Conversion.ConvertMeasureUnits(1, DistanceUnit.Inch, SelectedDistanceUnit));
                }
            }
        }

        //public RelayCommand SetScaleCommand
        //{
        //    get
        //    {
        //        if (setScaleCommand == null)
        //        {
        //            setScaleCommand = new RelayCommand(() =>
        //            {
        //                if (GisEditor.ActiveMap != null)
        //                {
        //                    double zoomToScale = -1;
        //                    if (value == SelectedScale.DisplayScale)
        //                    {
        //                        zoomToScale = Conversion.ConvertMeasureUnits(SelectedScale.Scale, SelectedDistanceUnit, DistanceUnit.Inch);
        //                    }
        //                    else
        //                    {
        //                        zoomToScale = Conversion.ConvertMeasureUnits(value, SelectedDistanceUnit, DistanceUnit.Inch);
        //                    }
        //                    if (zoomToScale > 0)
        //                    {
        //                        SetNewScale(zoomToScale);
        //                    }
        //                }
        //            });
        //        }
        //        return setScaleCommand;
        //    }
        //}

        internal void UpdateValues()
        {
            if (GisEditor.ActiveMap != null)
            {
                scales.Clear();
                foreach (var scale in GisEditor.ActiveMap.ZoomScales)
                {
                    double resultValue = scale * Conversion.ConvertMeasureUnits(1, DistanceUnit.Inch, SelectedDistanceUnit);
                    scales.Add(new ScaleWrapper(resultValue, GetSimplifiedNumber(resultValue)));
                }
                selectedScale = scales.FirstOrDefault(s => Math.Abs(s.Scale - GisEditor.ActiveMap.CurrentScale) < 1);
            }
        }

        private double GetSimplifiedNumber(double value)
        {
            if (value >= 1)
            {
                int valueInt = (int)value;
                double result = value - valueInt;
                if (result >= 0.5) return valueInt + 1;
                else return valueInt;
            }
            else
            {
                double resultNumber = 0;
                int decimals = 4;
                while ((resultNumber = Math.Round(value, decimals)) == 0)
                {
                    decimals += 2;
                }
                return resultNumber;
            }
        }

        private double GetZoomToScale()
        {
            double zoomToScale = -1;
            if (SelectedScale != null && value == SelectedScale.DisplayScale)
            {
                zoomToScale = Conversion.ConvertMeasureUnits(SelectedScale.Scale, SelectedDistanceUnit, DistanceUnit.Inch);
            }
            else
            {
                zoomToScale = Conversion.ConvertMeasureUnits(value, SelectedDistanceUnit, DistanceUnit.Inch);
            }
            return zoomToScale;
        }

        public static void SetNewScale(double zoomToScale, bool zoomToAuto = true)
        {
            PointShape centerPoint = GisEditor.ActiveMap.CurrentExtent.GetCenterPoint();
            SetNewScale(zoomToScale, centerPoint, zoomToAuto);
        }

        public static void SetNewScale(double zoomToScale, PointShape centerPoint, bool zoomToAuto = true)
        {
            int index = -1;
            var zoomScales = GisEditor.ActiveMap.ZoomScales;
            for (int i = 0; i < zoomScales.Count - 1; i++)
            {
                if (zoomScales[i] > zoomToScale && zoomScales[i + 1] <= zoomToScale)
                {
                    index = i + 1;
                    break;
                }
            }
            if (index == -1)
            {
                if (zoomScales.Count > 0 && zoomScales[0] < zoomToScale)
                {
                    index = 0;
                }
                else if (zoomScales.Count > 0 && zoomScales.LastOrDefault() > zoomToScale)
                {
                    index = zoomScales.Count;
                }
            }

            if (index >= 0 && index <= zoomScales.Count)
            {
                if (!zoomScales.Any(c => Math.Abs(c - zoomToScale) < 1))
                {
                    zoomScales.Insert(index, zoomToScale);
                    CommandHelper.ApplyNewZoomLevelSet(zoomScales);
                }
                if (zoomToAuto)
                {
                    GisEditor.ActiveMap.ZoomTo(centerPoint, zoomToScale);
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ScaleSettingsRibbonGroupViewModelScaleCannotAddText"), "Warning");
            }
        }
    }
}
