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
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
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
                foreach (var zoomLevel in GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels)
                {
                    double resultValue = zoomLevel.Scale * Conversion.ConvertMeasureUnits(1, DistanceUnit.Inch, SelectedDistanceUnit);
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
            for (int i = 0; i < GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count - 1; i++)
            {
                if (GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels[i].Scale > zoomToScale && GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels[i + 1].Scale <= zoomToScale)
                {
                    index = i + 1;
                    break;
                }
            }
            if (index == -1)
            {
                if (GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels[0].Scale < zoomToScale)
                {
                    index = 0;
                }
                else if (GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.LastOrDefault().Scale > zoomToScale)
                {
                    index = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count;
                }
            }

            if (index >= 0 && index <= GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Count)
            {
                if (!GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Any(c => Math.Abs(c.Scale - zoomToScale) < 1))
                {
                    ZoomLevel deletedZoomLevel = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.FirstOrDefault(c => c is PreciseZoomLevel);
                    if (deletedZoomLevel != null)
                    {
                        GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Remove(deletedZoomLevel);
                    }

                    PreciseZoomLevel newZoomLevel = new PreciseZoomLevel(zoomToScale);
                    GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.Insert(index, newZoomLevel);

                    ZoomLevel[] zoomLevels = GisEditor.ActiveMap.ZoomLevelSet.CustomZoomLevels.ToArray();
                    CommandHelper.ApplyNewZoomLevelSet(zoomLevels);
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