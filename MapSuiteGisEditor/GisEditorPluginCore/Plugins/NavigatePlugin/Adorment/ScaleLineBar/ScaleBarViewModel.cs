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
using System.Globalization;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class ScaleBarViewModel : ViewModelBase
    {
        private ObservableCollection<ScaleLineOrBarAdornmentLayerViewModel> scaleLayers = new ObservableCollection<ScaleLineOrBarAdornmentLayerViewModel>();
        private ScaleLineOrBarAdornmentLayerViewModel selectedLayer;
        [NonSerialized]
        private RelayCommand applyCommand;
        [NonSerialized]
        private RelayCommand okCommand;
        [NonSerialized]
        private RelayCommand cancelCommand;
        [NonSerialized]
        private RelayCommand addNewCommand;
        [NonSerialized]
        private RelayCommand removeCommand;

        public ScaleBarViewModel()
        {
            InitLayers();
            RaisePropertyChanged(()=>IsSettingEnabled);
        }

        public RelayCommand ApplyCommand
        {
            get
            {
                if (applyCommand == null)
                {
                    applyCommand = new RelayCommand(ApplyScaleSettings);
                }
                return applyCommand;
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
                        ApplyScaleSettings();
                        MessengerInstance.Send(GisEditor.LanguageManager.GetStringResource("CloseWindowMessage"), this);
                    });
                }
                return okCommand;
            }
        }

        public RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(() =>
                    {
                        MessengerInstance.Send(GisEditor.LanguageManager.GetStringResource("CloseWindowMessage"), this);
                    });
                }
                return cancelCommand;
            }
        }

        public RelayCommand AddNewCommand
        {
            get
            {
                if (addNewCommand == null)
                {
                    addNewCommand = new RelayCommand(AddANewLayer);
                }
                return addNewCommand;
            }
        }

        public RelayCommand RemoveCommand
        {
            get
            {
                if (removeCommand == null)
                {
                    removeCommand = new RelayCommand(() =>
                    {
                        RemoveSelectedLayer();
                    }, () => CanRemove);
                }
                return removeCommand;
            }
        }

        public ObservableCollection<ScaleLineOrBarAdornmentLayerViewModel> ScaleLayers { get { return scaleLayers; } }

        public ScaleLineOrBarAdornmentLayerViewModel SelectedLayer
        {
            get { return selectedLayer; }
            set
            {
                selectedLayer = value;
                RaisePropertyChanged(()=>SelectedLayer);
                RaisePropertyChanged(()=>IsSettingEnabled);
            }
        }

        public bool IsSettingEnabled
        {
            get { return SelectedLayer != null; }
        }

        public bool CanRemove
        {
            get { return SelectedLayer != null && ScaleLayers.Contains(SelectedLayer); }
        }

        private void AddANewLayer()
        {
            var newLayer = new ScaleLineOrBarAdornmentLayerViewModel();
            newLayer.Name = GenerateScaleName();
            newLayer.Location = AdornmentLocation.LowerLeft;
            ScaleLayers.Add(newLayer);
            SelectedLayer = newLayer;
        }

        private void RemoveSelectedLayer()
        {
            if (CanRemove)
            {
                ScaleLayers.Remove(SelectedLayer);
                if (ScaleLayers.Count > 0)
                {
                    SelectedLayer = ScaleLayers[ScaleLayers.Count - 1];
                }
            }
        }

        private void ApplyScaleSettings()
        {
            GisEditorWpfMap wpfMap = GisEditor.ActiveMap;
            var scaleLayers = wpfMap.FixedAdornmentOverlay.Layers.Where(tmpAdornmentLayer => (tmpAdornmentLayer is ScaleLineAdornmentLayer || tmpAdornmentLayer is ScaleBarAdornmentLayer)).ToArray();
            foreach (var tmpLayer in scaleLayers)
            {
                if (wpfMap.FixedAdornmentOverlay.Layers.Contains(tmpLayer))
                {
                    wpfMap.FixedAdornmentOverlay.Layers.Remove(tmpLayer);
                }
            }

            foreach (var tmpLayer in ScaleLayers)
            {
                wpfMap.FixedAdornmentOverlay.Layers.Add(tmpLayer.ToActualAdornmentLayer());
            }

            wpfMap.Refresh(wpfMap.FixedAdornmentOverlay);
        }

        private string GenerateScaleName()
        {
            int maxIndex = 0;
            if (ScaleLayers.Count > 0)
            {
                maxIndex = ScaleLayers.Max(tmpLayer =>
                {
                    int index = 0;
                    if (tmpLayer.Name.StartsWith("Scale ", StringComparison.Ordinal))
                    {
                        string indexString = tmpLayer.Name.Substring(tmpLayer.Name.LastIndexOf(" ") + 1);
                        Int32.TryParse(indexString, out index);
                    }

                    return index;
                });
            }

            return String.Format(CultureInfo.InvariantCulture, "Scale {0}", ++maxIndex);
        }

        private void InitLayers()
        {
            if (GisEditor.ActiveMap != null)
            {
                GisEditorWpfMap wpfMap = GisEditor.ActiveMap;

                var scaleLayers = wpfMap.FixedAdornmentOverlay.Layers.Where(tmpAdornmentLayer => (tmpAdornmentLayer is ScaleLineAdornmentLayer || tmpAdornmentLayer is ScaleBarAdornmentLayer));
                foreach (var tmpLayer in scaleLayers)
                {
                    ScaleLineOrBarAdornmentLayerViewModel newLayer = ScaleLineOrBarAdornmentLayerViewModel.CreateInstance(tmpLayer);
                    ScaleLayers.Add(newLayer);
                }

                SelectedLayer = ScaleLayers.FirstOrDefault();
            }
        }
    }
}