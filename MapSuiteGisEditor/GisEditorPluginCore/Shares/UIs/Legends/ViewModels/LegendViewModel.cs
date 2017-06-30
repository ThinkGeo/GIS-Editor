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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class LegendAdornmentLayerViewModel : ViewModelBase
    {
        [Obfuscation(Exclude = true)]
        private LegendAdornmentLayer coreLayer;

        [Obfuscation(Exclude = true)]
        private LegendSizeMode legendSizeMode;

        [NonSerialized]
        private LegendItemViewModel selectedLegendItem;

        [Obfuscation(Exclude = true)]
        private SymbolSizeMode symbolSizeMode;

        [NonSerialized]
        private Collection<SimpleCandidate> simpleCandidates;

        [Obfuscation(Exclude = true)]
        private GeoCollection<LegendItemViewModel> legendItems;

        [Obfuscation(Exclude = true)]
        private int previewWidth;

        [Obfuscation(Exclude = true)]
        private int previewHeight;

        [NonSerialized]
        private DispatcherTimer delayRenderTimer;

        [Obfuscation(Exclude = true)]
        private float minWidth;

        [Obfuscation(Exclude = true)]
        private float minHeight;

        [Obfuscation(Exclude = true)]
        private float fixedSymbolWidth;

        [Obfuscation(Exclude = true)]
        private float fixedSymbolHeight;

        [NonSerialized]
        private bool useTextWrapping;

        //TODO: this is not implemented.
        [Obfuscation(Exclude = true)]
        private int wordWrapLength;

        [Obfuscation(Exclude = true)]
        private LegendManagerViewModel parent;

        [Obfuscation(Exclude = true)]
        private bool isVisible;

        [NonSerialized]
        private RelayCommand addNewCommand;

        [NonSerialized]
        private RelayCommand editCommand;

        [NonSerialized]
        private RelayCommand configureBackgroundStyleCommand;

        [NonSerialized]
        private RelayCommand applyCommand;

        [NonSerialized]
        private ObservedCommand okCommand;

        [NonSerialized]
        private RelayCommand cancelCommand;

        [NonSerialized]
        private RelayCommand deleteItemCommand;

        [NonSerialized]
        private RelayCommand duplicateItemCommand;

        [NonSerialized]
        private RelayCommand<LegendItemType> setTitleFooterCommand;

        [NonSerialized]
        private RelayCommand toTopCommand;

        [NonSerialized]
        private RelayCommand toBottomCommand;

        [NonSerialized]
        private RelayCommand moveUpCommand;

        [NonSerialized]
        private RelayCommand moveDownCommand;

        [NonSerialized]
        private RelayCommand importFromMapCommand;

        public LegendAdornmentLayerViewModel()
            : this(new LegendAdornmentLayer { Name = LegendHelper.GenerateLegendName() })
        {
            var legendItemViewModel = new LegendItemViewModel();
            legendItemViewModel.Text = GisEditor.LanguageManager.GetStringResource("PreferenceWindowTitleText");
            legendItemViewModel.LegendItemType = LegendItemType.Header;
            legendItemViewModel.TextLeftPadding = 10;
            legendItemViewModel.NotifiedGeoFont = new GeoFontViewModel();
            legendItemViewModel.NotifiedGeoFont.FontSize = 10;
            LegendItems.Add(legendItemViewModel);
        }

        protected LegendAdornmentLayerViewModel(LegendAdornmentLayer legendLayer)
        {
            this.coreLayer = legendLayer;
            this.simpleCandidates = new Collection<SimpleCandidate>();
            this.InitDelayRenderTimer();
            this.legendItems = new GeoCollection<LegendItemViewModel>();
            this.legendItems.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(()=>CanMoveUp);
                RaisePropertyChanged(()=>CanMoveDown);
                LazyRenderPreview();

                if (e.NewItems != null)
                {
                    foreach (LegendItemViewModel item in e.NewItems)
                    {
                        item.Parent = this;
                    }
                }
            };
            this.Width = legendLayer.Width;
            this.Height = legendLayer.Height;
            this.BackgroundMask = legendLayer.BackgroundMask;
            this.FixedSymbolWidth = 16;
            this.FixedSymbolHeight = 16;
            this.IsVisible = legendLayer.IsVisible;
        }

        public LegendManagerViewModel Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public bool CanAutoImportLegend { get { return GisEditor.ActiveMap != null && GisEditor.ActiveMap.GetFeatureLayers(true).Count() != 0; } }

        public bool CanMoveUp { get { return SelectedLegendItem != null && LegendItems.Count > 0 && LegendItems.IndexOf(SelectedLegendItem) != 0 && SelectedLegendItem.LegendItemType != LegendItemType.Footer; } }

        public bool CanMoveDown { get { return SelectedLegendItem != null && LegendItems.Count > 0 && LegendItems.IndexOf(SelectedLegendItem) != (LegendItems.Count - 1) && SelectedLegendItem.LegendItemType != LegendItemType.Header; } }

        public GeoCollection<LegendItemViewModel> LegendItems { get { return legendItems; } }

        public bool IsLegendItemOperationEnabled { get { return SelectedLegendItem != null; } }

        public BitmapImage PreviewSource
        {
            get
            {
                return GetImageSource();
            }
        }

        public int PreviewWidth
        {
            get { return previewWidth; }
            set { previewWidth = value; }
        }

        public int PreviewHeight
        {
            get { return previewHeight; }
            set { previewHeight = value; }
        }

        public LegendItemViewModel SelectedLegendItem
        {
            get { return selectedLegendItem; }
            set
            {
                selectedLegendItem = value;
                RaisePropertyChanged(()=>CanMoveUp);
                RaisePropertyChanged(()=>CanMoveDown);
                RaisePropertyChanged(()=>SelectedLegendItem);
                RaisePropertyChanged(()=>IsLegendItemOperationEnabled);
                LazyRenderPreview();
            }
        }

        public string Name
        {
            get { return coreLayer.Name; }
            set
            {
                coreLayer.Name = value;
                RaisePropertyChanged(()=>Name);
                LazyRenderPreview();
            }
        }

        public AdornmentLocation Location
        {
            get { return coreLayer.Location; }
            set
            {
                coreLayer.Location = value;
                RaisePropertyChanged(()=>Location);
                RaisePropertyChanged(()=>IsLocationEnabled);
                LazyRenderPreview();
            }
        }

        public bool IsLocationEnabled
        {
            get { return Location == AdornmentLocation.UseOffsets; }
        }

        public float XOffsetInPixel
        {
            get { return coreLayer.XOffsetInPixel; }
            set
            {
                coreLayer.XOffsetInPixel = value;
                RaisePropertyChanged(()=>XOffsetInPixel);
                LazyRenderPreview();
            }
        }

        public float YOffsetInPixel
        {
            get { return coreLayer.YOffsetInPixel; }
            set
            {
                coreLayer.YOffsetInPixel = value;
                RaisePropertyChanged(()=>YOffsetInPixel);
                LazyRenderPreview();
            }
        }

        public LegendSizeMode LegendSizeMode
        {
            get { return legendSizeMode; }
            set
            {
                legendSizeMode = value;
                RaisePropertyChanged(()=>LegendSizeMode);
                RaisePropertyChanged(()=>IsLegendSizeEnabled);
                LazyRenderPreview();
            }
        }

        public bool IsLegendSizeEnabled
        {
            get { return LegendSizeMode == LegendSizeMode.FixedSize; }
        }

        public float Width
        {
            get { return coreLayer.Width; }
            set
            {
                if (value <= 2)
                {
                    throw new ArgumentException("Width");
                }
                coreLayer.Width = value;
                RaisePropertyChanged(()=>Width);
                LazyRenderPreview();
            }
        }

        public float Height
        {
            get { return coreLayer.Height; }
            set
            {
                if (value <= 2)
                {
                    throw new ArgumentException("Height");
                }
                coreLayer.Height = value;
                RaisePropertyChanged(()=>Height);
                LazyRenderPreview();
            }
        }

        public float FixedSymbolWidth
        {
            get { return fixedSymbolWidth; }
            set
            {
                fixedSymbolWidth = value;
                RaisePropertyChanged(()=>FixedSymbolWidth);
                LazyRenderPreview();
            }
        }

        public float FixedSymbolHeight
        {
            get { return fixedSymbolHeight; }
            set
            {
                fixedSymbolHeight = value;
                RaisePropertyChanged(()=>FixedSymbolHeight);
                LazyRenderPreview();
            }
        }

        public float MinWidth
        {
            get { return minWidth; }
            set
            {
                minWidth = value;
                RaisePropertyChanged(()=>MinWidth);
                LazyRenderPreview();
            }
        }

        public float MinHeight
        {
            get { return minHeight; }
            set
            {
                minHeight = value;
                RaisePropertyChanged(()=>MinHeight);
                LazyRenderPreview();
            }
        }

        public AreaStyle BackgroundMask
        {
            get { return coreLayer.BackgroundMask; }
            set
            {
                coreLayer.BackgroundMask = value;
                RaisePropertyChanged(()=>BackgroundMask);
                LazyRenderPreview();
            }
        }

        public SymbolSizeMode SymbolSizeMode
        {
            get { return symbolSizeMode; }
            set
            {
                symbolSizeMode = value;
                RaisePropertyChanged(()=>SymbolSizeMode);
                RaisePropertyChanged(()=>IsSymbolSizeEnabled);
                LazyRenderPreview();
            }
        }

        public bool IsSymbolSizeEnabled
        {
            get { return SymbolSizeMode != SymbolSizeMode.None; }
        }

        public bool UseTextWrapping
        {
            get { return useTextWrapping; }
            set
            {
                useTextWrapping = value;
                RaisePropertyChanged(()=>UseTextWrapping);
                LazyRenderPreview();
            }
        }

        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                isVisible = value;
                RaisePropertyChanged(()=>IsVisible);
            }
        }

        #region Commands

        public RelayCommand AddNewCommand
        {
            get
            {
                if (addNewCommand == null)
                {
                    addNewCommand = new RelayCommand(() =>
                    {
                        LegendItemViewModel newItem = new LegendItemViewModel { Parent = this };
                        if (LegendItems.Count > 0)
                        {
                            newItem = LegendItems.Last().Clone();
                        }
                        newItem.Text = LegendHelper.GenerateLegendItemName(LegendItems);
                        newItem.LegendItemType = LegendItemType.Item;

                        LegendItemEditor legendItemEditor = new LegendItemEditor(newItem);
                        if (legendItemEditor.ShowDialog().GetValueOrDefault())
                        {
                            if (!LegendItems.Contains(newItem)) LegendItems.Add(newItem);
                        }
                    });
                }
                return addNewCommand;
            }
        }

        public RelayCommand EditCommand
        {
            get
            {
                if (editCommand == null)
                {
                    editCommand = new RelayCommand(() =>
                    {
                        if (selectedLegendItem.Parent == null) selectedLegendItem.Parent = this;
                        LegendItemEditor itemEditor = new LegendItemEditor(SelectedLegendItem);
                        if (itemEditor.ShowDialog().GetValueOrDefault())
                        {
                            RaisePropertyChanged(()=>PreviewSource);
                        }
                    });
                }
                return editCommand;
            }
        }

        public RelayCommand ConfigureBackgroundStyleCommand
        {
            get
            {
                if (configureBackgroundStyleCommand == null)
                {
                    configureBackgroundStyleCommand = new RelayCommand(() =>
                    {
                        if (BackgroundMask != null)
                            BackgroundMask.Name = GisEditor.StyleManager.GetStylePluginByStyle(coreLayer.BackgroundMask).Name;

                        if (BackgroundMask != null
                            && BackgroundMask.CustomAreaStyles.Count == 0)
                        {
                            var tempStyle = new AreaStyle();
                            tempStyle.Name = BackgroundMask.Name;
                            tempStyle = (AreaStyle)coreLayer.BackgroundMask.CloneDeep();
                            this.BackgroundMask.CustomAreaStyles.Add(tempStyle);
                        }

                        LazyRenderPreview();

                        StyleBuilderArguments styleArguments = new StyleBuilderArguments();
                        styleArguments.AvailableStyleCategories = StyleCategories.Area;
                        styleArguments.AvailableUIElements = StyleBuilderUIElements.StyleList;
                        styleArguments.AppliedCallback = (result) =>
                        {
                            AreaStyle areaStyle = new AreaStyle();
                            foreach (var item in result.CompositeStyle.Styles.OfType<AreaStyle>())
                            {
                                areaStyle.CustomAreaStyles.Add(item);
                            }
                            BackgroundMask = areaStyle;
                        };

                        AreaStyle style = (AreaStyle)BackgroundMask.CloneDeep();
                        style.Name = BackgroundMask.Name;
                        var resultStyle = GisEditor.StyleManager.EditStyles(styleArguments, style);
                        if (resultStyle != null)
                            BackgroundMask = resultStyle;
                    });
                }
                return configureBackgroundStyleCommand;
            }
        }

        public RelayCommand ApplyCommand
        {
            get
            {
                if (applyCommand == null)
                {
                    applyCommand = new RelayCommand(() =>
                    {
                        LazyRenderPreview();
                        AdornmentRibbonGroup.RefreshLegendOverlay(CollectNotifiedLegendLayers());
                    });
                }
                return applyCommand;
            }
        }

        public ObservedCommand OKCommand
        {
            get
            {
                if (okCommand == null)
                {
                    okCommand = new ObservedCommand(() =>
                    {
                        Messenger.Default.Send(true, this);
                    }, () => LegendItems.Count > 0);
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
                        Messenger.Default.Send(false, this);
                    });
                }
                return cancelCommand;
            }
        }

        public RelayCommand DeleteItemCommand
        {
            get
            {
                if (deleteItemCommand == null)
                {
                    deleteItemCommand = new RelayCommand(() =>
                    {
                        if (SelectedLegendItem != null && LegendItems.Contains(SelectedLegendItem))
                        {
                            LegendItems.Remove(SelectedLegendItem);
                        }
                    });
                }
                return deleteItemCommand;
            }
        }

        public RelayCommand DuplicateItemCommand
        {
            get
            {
                if (duplicateItemCommand == null)
                {
                    duplicateItemCommand = new RelayCommand(() =>
                    {
                        if (SelectedLegendItem != null)
                        {
                            LegendItemViewModel duplicateItem = SelectedLegendItem.Clone();
                            duplicateItem.Text = SelectedLegendItem.Text;
                            LegendItems.Add(duplicateItem);
                        }
                    });
                }
                return duplicateItemCommand;
            }
        }

        public RelayCommand<LegendItemType> SetTitleFooterCommand
        {
            get
            {
                if (setTitleFooterCommand == null)
                {
                    setTitleFooterCommand = new RelayCommand<LegendItemType>((parameter) =>
                    {
                        LegendItemType targetLegendItemType = parameter;

                        if (targetLegendItemType == LegendItemType.Header)
                        {
                            ToTopCommand.Execute(null);
                        }
                        else if (targetLegendItemType == LegendItemType.Footer)
                        {
                            ToBottomCommand.Execute(null);
                        }

                        foreach (var item in LegendItems.Where(tmpItem => tmpItem.LegendItemType == targetLegendItemType))
                        {
                            item.LegendItemType = LegendItemType.Item;
                        }

                        SelectedLegendItem.LegendItemType = targetLegendItemType;
                        RaisePropertyChanged(()=>LegendItems);
                        RaisePropertyChanged(()=>CanMoveUp);
                        RaisePropertyChanged(()=>CanMoveDown);
                    });
                } return setTitleFooterCommand;
            }
        }

        public RelayCommand ToTopCommand
        {
            get
            {
                if (toTopCommand == null)
                {
                    toTopCommand = new RelayCommand(() =>
                    {
                        if (SelectedLegendItem != null && LegendItems.Contains(SelectedLegendItem))
                        {
                            var tmpSelectedItem = SelectedLegendItem;
                            LegendItems.Remove(tmpSelectedItem);
                            LegendItems.Insert(0, tmpSelectedItem);
                            SelectedLegendItem = tmpSelectedItem;
                        }
                    });
                }
                return toTopCommand;
            }
        }

        public RelayCommand ToBottomCommand
        {
            get
            {
                if (toBottomCommand == null)
                {
                    toBottomCommand = new RelayCommand(() =>
                    {
                        if (SelectedLegendItem != null && LegendItems.Contains(SelectedLegendItem))
                        {
                            var tmpSelectedItem = SelectedLegendItem;
                            LegendItems.Remove(tmpSelectedItem);
                            LegendItems.Add(tmpSelectedItem);
                            SelectedLegendItem = tmpSelectedItem;
                        }
                    });
                }
                return toBottomCommand;
            }
        }

        public RelayCommand MoveUpCommand
        {
            get
            {
                if (moveUpCommand == null)
                {
                    moveUpCommand = new RelayCommand(() =>
                    {
                        if (SelectedLegendItem != null && LegendItems.Contains(SelectedLegendItem))
                        {
                            int index = LegendItems.IndexOf(SelectedLegendItem);
                            var tmpSelectedItem = SelectedLegendItem;
                            LegendItems.Remove(tmpSelectedItem);
                            LegendItems.Insert(index - 1, tmpSelectedItem);
                            SelectedLegendItem = tmpSelectedItem;
                        }
                    });
                }
                return moveUpCommand;
            }
        }

        public RelayCommand MoveDownCommand
        {
            get
            {
                if (moveDownCommand == null)
                {
                    moveDownCommand = new RelayCommand(() =>
                    {
                        if (SelectedLegendItem != null && LegendItems.Contains(SelectedLegendItem))
                        {
                            int index = LegendItems.IndexOf(SelectedLegendItem);
                            var tmpSelectedItem = SelectedLegendItem;
                            LegendItems.Remove(tmpSelectedItem);
                            LegendItems.Insert(index + 1, tmpSelectedItem);
                            SelectedLegendItem = tmpSelectedItem;
                        }
                    });
                }
                return moveDownCommand;
            }
        }

        public RelayCommand ImportFromMapCommand
        {
            get
            {
                if (importFromMapCommand == null)
                {
                    importFromMapCommand = new RelayCommand(() =>
                    {
                        if (GisEditor.ActiveMap != null)
                        {
                            LegendImporterWindow importerWindow = new LegendImporterWindow();
                            if (importerWindow.ShowDialog().GetValueOrDefault())
                            {
                                var notifiedItems = importerWindow.CollectLegendItems();
                                foreach (var notifiedItem in notifiedItems)
                                {
                                    LegendItems.Add(notifiedItem);
                                }
                            }
                        }
                    });
                }
                return importFromMapCommand;
            }
        }

        #endregion Commands

        public LegendAdornmentLayer ToLegendAdornmentLayer()
        {
            LegendAdornmentLayer legendAdornmentLayer = new LegendAdornmentLayer();
            legendAdornmentLayer.BackgroundMask = BackgroundMask;
            legendAdornmentLayer.Height = Height;
            legendAdornmentLayer.Location = Location;
            legendAdornmentLayer.Name = Name;
            legendAdornmentLayer.Width = Width;
            legendAdornmentLayer.XOffsetInPixel = XOffsetInPixel;
            legendAdornmentLayer.YOffsetInPixel = YOffsetInPixel;
            legendAdornmentLayer.IsVisible = IsVisible;

            //ApplySymbolSizeMode(legendAdornmentLayer.LegendItems);
            ApplySymbolSizeMode(LegendItems);
            foreach (var item in LegendItems)
            {
                if (item.LegendItemType == LegendItemType.Header)
                {
                    legendAdornmentLayer.Title = item.ToLegendItem();
                }
                else if (item.LegendItemType == LegendItemType.Footer)
                {
                    legendAdornmentLayer.Footer = item.ToLegendItem();
                }
                else
                {
                    legendAdornmentLayer.LegendItems.Add(item.ToLegendItem());
                }
            }

            var rect = Measure(new PlatformGeoCanvas());
            legendAdornmentLayer.Width = rect.Width + 1;
            legendAdornmentLayer.Height = rect.Height + 1;

            var tmpItems = new Collection<LegendItem>();
            if (legendAdornmentLayer.Title != null) tmpItems.Add(legendAdornmentLayer.Title);
            if (legendAdornmentLayer.Footer != null) tmpItems.Add(legendAdornmentLayer.Footer);

            foreach (var tmpItem in legendAdornmentLayer.LegendItems)
            {
                tmpItems.Add(tmpItem);
            }

            foreach (var tmpItem in tmpItems)
            {
                tmpItem.Width = rect.Width - tmpItem.LeftPadding - tmpItem.RightPadding;
            }

            return legendAdornmentLayer;
        }

        public LegendAdornmentLayerViewModel CloneDeep()
        {
            var cloneObject = new LegendAdornmentLayerViewModel(this.coreLayer.CloneDeep() as LegendAdornmentLayer)
            {
                fixedSymbolHeight = this.fixedSymbolHeight,
                fixedSymbolWidth = this.fixedSymbolWidth,
                Height = this.Height,
                legendSizeMode = this.legendSizeMode,
                Location = this.Location,
                minHeight = this.minHeight,
                minWidth = this.minWidth,
                parent = this.parent,
                previewHeight = this.previewHeight,
                previewWidth = this.previewWidth,
                selectedLegendItem = this.selectedLegendItem,
                simpleCandidates = this.simpleCandidates,
                symbolSizeMode = this.symbolSizeMode,
                useTextWrapping = this.useTextWrapping,
                wordWrapLength = this.wordWrapLength,
            };

            foreach (var item in this.legendItems)
            {
                cloneObject.legendItems.Add(item.Clone());

            }

            return cloneObject;
        }

        public IEnumerable<LegendAdornmentLayerViewModel> CollectNotifiedLegendLayers()
        {
            if (Parent != null)
            {
                foreach (var legend in Parent.Legends)
                {
                    yield return legend;
                }

                if (!Parent.Legends.Contains(this))
                {
                    yield return this;
                }
            }
        }

        protected void LazyRenderPreview()
        {
            if (delayRenderTimer.IsEnabled) delayRenderTimer.Stop();
            delayRenderTimer.Start();
        }

        public SizeF Measure(GeoCanvas geoCanvas)
        {
            float width = 0;
            float height = 0;

            if (LegendSizeMode == LegendSizeMode.Auto)
            {
                foreach (var item in LegendItems)
                {
                    var size = item.Measure(geoCanvas);
                    item.Height = size.Height;
                    float tmpWidth = size.Width + item.LeftPadding + item.RightPadding;
                    width = width > tmpWidth ? width : tmpWidth;
                    height += size.Height;
                    height += item.TopPadding;
                    height += item.BottomPadding;
                }

                if (width < 192) width = 192;
                var ratio = width / 192;
                var headerFooterHeight = legendItems.Where(l => l.LegendItemType == LegendItemType.Footer || l.LegendItemType == LegendItemType.Header).Sum(l => l.Height + l.BottomPadding + l.TopPadding);
                height -= headerFooterHeight;
                height *= ratio;
                height += headerFooterHeight + 2;
            }
            else if (LegendSizeMode == LegendSizeMode.FixedSize)
            {
                width = Width;
                height = Height;
            }

            return new SizeF(width, height);
        }

        private void InitDelayRenderTimer()
        {
            this.delayRenderTimer = new DispatcherTimer();
            this.delayRenderTimer.Interval = TimeSpan.FromMilliseconds(400);
            this.delayRenderTimer.Tick += (s, e) => { RaisePropertyChanged(()=>PreviewSource); ((DispatcherTimer)s).Stop(); };
        }

        private BitmapImage GetImageSource()
        {
            MemoryStream streamSource = null;
            if (PreviewWidth != 0 && PreviewHeight != 0 && Width != 0 && Height != 0 && LegendItems.Count > 0)
            {
                Bitmap nativeImage = new Bitmap(PreviewWidth, PreviewHeight);
                PlatformGeoCanvas geoCanvas = new PlatformGeoCanvas
                {
                    CompositingQuality = CompositingQuality.HighSpeed,
                    SmoothingMode = SmoothingMode.HighSpeed,
                    DrawingQuality = DrawingQuality.CanvasSettings,
                };

                LegendAdornmentLayer tmpLegendAdornmentLayer = ToLegendAdornmentLayer();
                double left = -tmpLegendAdornmentLayer.Width * .5;
                double top = tmpLegendAdornmentLayer.Height * .5;
                double right = left + tmpLegendAdornmentLayer.Width;
                double bottom = top - tmpLegendAdornmentLayer.Height;

                geoCanvas.BeginDrawing(nativeImage, new RectangleShape(left, top, right, bottom), GeographyUnit.Meter);
                simpleCandidates.Clear();
                tmpLegendAdornmentLayer.Draw(geoCanvas, simpleCandidates);
                geoCanvas.EndDrawing();

                streamSource = new MemoryStream();
                nativeImage.Save(streamSource, ImageFormat.Png);
            }

            BitmapImage previewSource = new BitmapImage();
            if (streamSource != null)
            {
                previewSource.BeginInit();
                previewSource.StreamSource = streamSource;
                previewSource.EndInit();
                previewSource.Freeze();
            }
            return previewSource;
        }

        private void ApplySymbolSizeMode(IEnumerable<LegendItemViewModel> legendItems)
        {
            if (legendItems.Count() != 0)
            {
                switch (SymbolSizeMode)
                {
                    case SymbolSizeMode.MatchLargest:
                        float maxSymbolWidth = legendItems.Select(item => item.ImageWidth).Max();
                        float maxSymbolHeight = legendItems.Select(item => item.ImageHeight).Max();
                        foreach (var item in legendItems)
                        {
                            item.ImageHeight = maxSymbolHeight;
                            item.ImageWidth = maxSymbolWidth;
                        }
                        break;
                    case SymbolSizeMode.MatchSmallest:
                        float minSymbolWidth = legendItems.Select(item => item.ImageWidth).Min();
                        float minSymbolHeight = legendItems.Select(item => item.ImageHeight).Min();
                        foreach (var item in legendItems)
                        {
                            item.ImageHeight = minSymbolHeight;
                            item.ImageWidth = minSymbolWidth;
                        }
                        break;
                    case SymbolSizeMode.Fixed:
                        foreach (var item in legendItems)
                        {
                            item.ImageWidth = FixedSymbolWidth;
                            item.ImageHeight = FixedSymbolHeight;
                        }
                        break;
                    case SymbolSizeMode.None:
                    default:
                        break;
                }
            }
        }
    }
}