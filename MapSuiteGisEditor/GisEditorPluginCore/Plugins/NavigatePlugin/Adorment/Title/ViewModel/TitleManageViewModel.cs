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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TitleManageViewModel : ViewModelBase
    {
        private const string titlePattern = "(?<=New Title)\\d+";
        private TitleAdornmentLayer layer;
        private TitleViewModel selectedTitle;
        private ObservableCollection<TitleViewModel> titles;

        [NonSerialized]
        private BitmapImage previewSource;
        [NonSerialized]
        private RelayCommand addTitleCommand;
        [NonSerialized]
        private RelayCommand removeTitleCommand;
        [NonSerialized]
        private RelayCommand cancelCommand;
        private ObservedCommand applyCommand;
        private ObservedCommand okCommand;

        public TitleManageViewModel()
        {
            Dictionary<string, TitleAdornmentLayer> layerDictionary = GetTitleAdornmentLayers();
            if (layerDictionary.Count > 0)
            {
                titles = new ObservableCollection<TitleViewModel>();
                foreach (var layerPair in layerDictionary)
                {
                    TitleViewModel entity = new TitleViewModel() { ID = layerPair.Key };
                    entity.Load(layerPair.Value);
                    entity.PropertyChanged += (sender, e) => { ChangePreview(); };
                    titles.Add(entity);
                }
            }
            else
            {
                TitleViewModel entity = new TitleViewModel();
                entity.PropertyChanged += (sender, e) => { ChangePreview(); };
                titles = new ObservableCollection<TitleViewModel> { entity };
            }
            SelectedTitle = Titles.FirstOrDefault();
        }

        public RelayCommand AddTitleCommand
        {
            get
            {
                if (addTitleCommand == null)
                {
                    addTitleCommand = new RelayCommand(AddNewTitle);
                }
                return addTitleCommand;
            }
        }

        public RelayCommand RemoveTitleCommand
        {
            get
            {
                if (removeTitleCommand == null)
                {
                    removeTitleCommand = new RelayCommand(RemoveSelectedTitle);
                }
                return removeTitleCommand;
            }
        }

        public ObservedCommand ApplyCommand
        {
            get
            {
                if (applyCommand == null)
                {
                    applyCommand = new ObservedCommand(Apply, () => CanExecute());
                }
                return applyCommand;
            }
        }

        public ObservedCommand OkCommand
        {
            get
            {
                if (okCommand == null)
                {
                    okCommand = new ObservedCommand(() =>
                    {
                        Apply();
                        MessengerInstance.Send(GisEditor.LanguageManager.GetStringResource("CloseWindowMessage"), this);
                    }, () => CanExecute());
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

        public ObservableCollection<TitleViewModel> Titles
        {
            get { return titles; }
        }

        public TitleViewModel SelectedTitle
        {
            get { return selectedTitle; }
            set
            {
                selectedTitle = value;
                RaisePropertyChanged(()=>SelectedTitle);
                if (selectedTitle != null)
                    ChangePreview();
                else
                    PreviewSource = null;
            }
        }

        public BitmapImage PreviewSource
        {
            get { return previewSource; }
            set
            {
                previewSource = value;
                RaisePropertyChanged(()=>PreviewSource);
            }
        }

        public double ImageWidth
        {
            get { return 350; }
        }

        public double ImageHeight
        {
            get { return 100; }
        }

        public void ChangePreview()
        {
            if (layer == null)
                layer = new TitleAdornmentLayer();
            SetPropertiesForTitleAdronmentLayer(layer, SelectedTitle);
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            MemoryStream ms = new MemoryStream();
            layer.DrawSample(ms, ImageWidth, ImageHeight);
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            PreviewSource = bitmapImage;
        }

        public void AddNewTitle()
        {
            TitleViewModel entity = new TitleViewModel();
            entity.Title = String.Format(CultureInfo.InvariantCulture, "{0}{1}", "New Title", GetMaxIndex() + 1);
            entity.PropertyChanged += (sender, e) => { ChangePreview(); };
            Titles.Add(entity);
            SelectedTitle = Titles[Titles.Count - 1];
        }

        public void RemoveSelectedTitle()
        {
            if (SelectedTitle != null)
            {
                Titles.Remove(SelectedTitle);
                if (Titles.Count > 0)
                {
                    SelectedTitle = Titles[Titles.Count - 1];
                }
            }
        }

        public void AddTitlesToMap()
        {
            var map = GisEditor.ActiveMap;

            var layersToRemove = map.FixedAdornmentOverlay.Layers.OfType<TitleAdornmentLayer>().ToList();
            layersToRemove.ForEach(l => map.FixedAdornmentOverlay.Layers.Remove(l));

            foreach (TitleViewModel title in Titles)
            {
                if (!map.FixedAdornmentOverlay.Layers.Contains(title.ID))
                {
                    map.FixedAdornmentOverlay.Layers.Add(title.ID, new TitleAdornmentLayer());
                }
                TitleAdornmentLayer layer = map.FixedAdornmentOverlay.Layers[title.ID] as TitleAdornmentLayer;
                SetPropertiesForTitleAdronmentLayer(layer, title);
            }
        }

        private bool CanExecute()
        {
            if (SelectedTitle == null)
            {
                return true;
            }
            else
            {
                return !string.IsNullOrEmpty(SelectedTitle.Title);
            }
        }

        private Dictionary<string, TitleAdornmentLayer> GetTitleAdornmentLayers()
        {
            if (GisEditor.ActiveMap != null)
            {
                var map = GisEditor.ActiveMap;
                Collection<string> keys = map.FixedAdornmentOverlay.Layers.GetKeys();
                Dictionary<string, TitleAdornmentLayer> dictionary = new Dictionary<string, TitleAdornmentLayer>();
                for (int i = 0; i < keys.Count; i++)
                {
                    if (map.FixedAdornmentOverlay.Layers[i] is TitleAdornmentLayer)
                    {
                        dictionary.Add(keys[i], (TitleAdornmentLayer)map.FixedAdornmentOverlay.Layers[i]);
                    }
                }
                return dictionary;
            }

            return null;
        }

        private void Apply()
        {
            AddTitlesToMap();
            GisEditor.ActiveMap.Refresh();
        }

        private int GetMaxIndex()
        {
            int maxIndex = 0;
            int number = 0;
            foreach (var titleEntity in Titles)
            {
                string result = Regex.Match(titleEntity.Title, titlePattern).Value;
                if (!String.IsNullOrEmpty(result) && int.TryParse(result, out number))
                {
                    maxIndex = maxIndex > number ? maxIndex : number;
                }
            }
            return maxIndex;
        }

        private void SetPropertiesForTitleAdronmentLayer(TitleAdornmentLayer titleAdornmentLayer, TitleViewModel entity)
        {
            titleAdornmentLayer.Title = entity.Title;
            DrawingFontStyles drawingFontStyles = DrawingFontStyles.Regular;
            if (entity.IsBold)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Bold;
            if (entity.IsItalic)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Italic;
            if (entity.IsStrikeout)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Strikeout;
            if (entity.IsUnderline)
                drawingFontStyles = drawingFontStyles | DrawingFontStyles.Underline;

            titleAdornmentLayer.TitleFont = new GeoFont(entity.FontName.Source, entity.FontSize, drawingFontStyles);
            titleAdornmentLayer.XOffsetInPixel = entity.Left;
            titleAdornmentLayer.YOffsetInPixel = entity.Top;
            titleAdornmentLayer.FontColor = entity.FontColor;
            titleAdornmentLayer.Rotation = entity.Angle;
            titleAdornmentLayer.Location = entity.TitleLocation;
            titleAdornmentLayer.HaloPen = entity.DoesAddHalo ? new GeoPen(entity.HaloColor, entity.HaloThickness) : null;

            if (entity.IsEnableMask)
            {
                titleAdornmentLayer.MaskFillColor = entity.MaskFillColor;
                titleAdornmentLayer.MaskOutlineColor = entity.MaskOutlineColor;
                titleAdornmentLayer.MaskOutlineThickness = entity.MaskOutlineThickness;
                titleAdornmentLayer.MaskMargin = entity.MaskMarginValue;
            }
            else
            {
                titleAdornmentLayer.MaskFillColor = null;
                titleAdornmentLayer.MaskOutlineColor = null;
            }
        }
    }
}