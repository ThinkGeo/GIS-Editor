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
using System.Linq;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class NewLayoutViewModel : ViewModelBase
    {
        private static string imagePathFormat = "pack://application:,,,/GisEditorPluginCore;component/Images/template{0}_{1}.png";
        private ObservableCollection<LayoutViewModel> allLayoutEntities;

        private LayoutViewModel selectedLayout;
        private PrinterPageSize selectedPageSize;
        private FilterPageOrientation selectedOrientation;
        private ObservableCollection<LayoutViewModel> resultLayoutEntities;

        public NewLayoutViewModel(Dictionary<PrinterPageSize, RectangleShape> boundingBoxes)
        {
            selectedOrientation = FilterPageOrientation.AllOrientations;
            selectedPageSize = PrinterPageSize.AnsiA;
            allLayoutEntities = new ObservableCollection<LayoutViewModel>();
            foreach (PrinterPageSize pageSize in Enum.GetValues(typeof(PrinterPageSize)))
            {
                if (pageSize != PrinterPageSize.Custom)
                {
                    foreach (var item in GetAllLayouts(boundingBoxes[pageSize], GetOppositeRectangleShape(boundingBoxes[pageSize]), pageSize))
                    {
                        allLayoutEntities.Add(item);
                    }
                }
            }

            resultLayoutEntities = new ObservableCollection<LayoutViewModel>(allLayoutEntities);
            selectedLayout = allLayoutEntities[0];
            UpdateSearchResult();
        }

        public LayoutViewModel SelectedLayout
        {
            get { return selectedLayout; }
            set
            {
                selectedLayout = value;
                RaisePropertyChanged(() => SelectedLayout);
                //RaisePropertyChanged(()=>Preview);
            }
        }

        public PrinterPageSize SelectedPageSize
        {
            get { return selectedPageSize; }
            set
            {
                selectedPageSize = value;
                RaisePropertyChanged(() => SelectedPageSize);
                UpdateSearchResult();
            }
        }

        public FilterPageOrientation SelectedOrientation
        {
            get { return selectedOrientation; }
            set
            {
                selectedOrientation = value;
                RaisePropertyChanged(() => SelectedOrientation);
                UpdateSearchResult();
            }
        }

        public ObservableCollection<LayoutViewModel> AllLayoutEntities
        {
            get { return allLayoutEntities; }
        }

        public ObservableCollection<LayoutViewModel> ResultLayoutEntities
        {
            get { return resultLayoutEntities; }
        }

        internal static RectangleShape GetOppositeRectangleShape(RectangleShape rectangle)
        {
            var leftPoint = new PointShape(-rectangle.UpperLeftPoint.Y, -rectangle.UpperLeftPoint.X);
            var rightPoint = new PointShape(-rectangle.LowerRightPoint.Y, -rectangle.LowerRightPoint.X);
            return new RectangleShape(leftPoint, rightPoint);
        }

        public static IEnumerable<LayoutViewModel> GetAllLayouts(RectangleShape portraitPageBoudingBox, RectangleShape landscapePageBoudingBox, PrinterPageSize pageSize)
        {
            LayoutViewModel entity1 = new LayoutViewModel() { Orientation = FilterPageOrientation.Portrait, PageSize = pageSize, Image = string.Format(CultureInfo.InvariantCulture, imagePathFormat, 1, "small"), Description = GisEditor.LanguageManager.GetStringResource("NewLayoutViewModelSimpleLayoutDescription") };
            var titleWidth = portraitPageBoudingBox.Width * 0.25;
            var titleHeight = titleWidth * 0.5;
            entity1.PrinterLayers.Add(PrinterLayerHelper.GetMapPrinterLayer(portraitPageBoudingBox.Width * 0.8, portraitPageBoudingBox.Height * 0.8, 0, -titleHeight * 0.7));
            entity1.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Title", titleWidth, titleHeight, 0, portraitPageBoudingBox.UpperLeftPoint.Y - titleHeight));
            yield return entity1;

            LayoutViewModel entity2 = new LayoutViewModel() { Orientation = FilterPageOrientation.Portrait, PageSize = pageSize, Image = string.Format(CultureInfo.InvariantCulture, imagePathFormat, 2, "small"), Description = GisEditor.LanguageManager.GetStringResource("NewLayoutViewModelTitleImageTextDescription") };
            entity2.PrinterLayers.Add(PrinterLayerHelper.GetMapPrinterLayer(portraitPageBoudingBox.Width * 0.8, portraitPageBoudingBox.Height * 0.5, 0, 0));
            entity2.PrinterLayers.Add(PrinterLayerHelper.GetImagePrinterLayer(portraitPageBoudingBox.Width * 0.2, portraitPageBoudingBox.Width * 0.2, -portraitPageBoudingBox.Width * 0.25, portraitPageBoudingBox.LowerLeftPoint.Y + titleHeight));
            entity2.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Text", titleWidth, titleHeight, portraitPageBoudingBox.Width * 0.25, portraitPageBoudingBox.LowerLeftPoint.Y + titleHeight));
            entity2.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Title", titleWidth, titleHeight, 0, portraitPageBoudingBox.UpperLeftPoint.Y - titleHeight));
            yield return entity2;

            LayoutViewModel entity3 = new LayoutViewModel() { Orientation = FilterPageOrientation.Portrait, PageSize = pageSize, Image = string.Format(CultureInfo.InvariantCulture, imagePathFormat, 3, "small"), Description = GisEditor.LanguageManager.GetStringResource("NewLayoutViewModelFeatureTitleDescription") };
            entity3.PrinterLayers.Add(PrinterLayerHelper.GetMapPrinterLayer(portraitPageBoudingBox.Width * 0.8, portraitPageBoudingBox.Height * 0.5, 0, titleHeight * 0.5));
            entity3.PrinterLayers.Add(PrinterLayerHelper.GetDataGridPrinterLayer(portraitPageBoudingBox.Width * 0.8, portraitPageBoudingBox.Height * 0.2, 0, portraitPageBoudingBox.LowerLeftPoint.Y + titleHeight * 1.5));
            entity3.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Title", titleWidth, titleHeight, 0, portraitPageBoudingBox.UpperLeftPoint.Y - titleHeight));
            yield return entity3;

            LayoutViewModel entity4 = new LayoutViewModel() { Orientation = FilterPageOrientation.Portrait, PageSize = pageSize, Image = string.Format(CultureInfo.InvariantCulture, imagePathFormat, 4, "small"), Description = GisEditor.LanguageManager.GetStringResource("NewLayoutViewModelCondensedLayoutDescription") };
            var map = PrinterLayerHelper.GetMapPrinterLayer(portraitPageBoudingBox.Width * 0.9, portraitPageBoudingBox.Height * 0.5, 0, titleHeight * 0.5);
            entity4.PrinterLayers.Add(map);
            entity4.PrinterLayers.Add(PrinterLayerHelper.GetLegendPrinterLayer(portraitPageBoudingBox.Width * 0.3, portraitPageBoudingBox.Height * 0.2, portraitPageBoudingBox.Width * -0.3, portraitPageBoudingBox.LowerLeftPoint.Y + titleHeight * 1.5));
            entity4.PrinterLayers.Add(PrinterLayerHelper.GetScaleBarPrinterLayer(map, portraitPageBoudingBox.Width * 0.2, portraitPageBoudingBox.Height * 0.02, 0, portraitPageBoudingBox.LowerLeftPoint.Y + titleHeight * 2.5));
            entity4.PrinterLayers.Add(PrinterLayerHelper.GetImagePrinterLayer(portraitPageBoudingBox.Width * 0.3, portraitPageBoudingBox.Height * 0.18, 0, portraitPageBoudingBox.LowerLeftPoint.Y + titleHeight * 1.5));
            entity4.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Text", portraitPageBoudingBox.Width * 0.3, portraitPageBoudingBox.Height * 0.2, portraitPageBoudingBox.Width * 0.3, portraitPageBoudingBox.LowerLeftPoint.Y + titleHeight * 1.8));
            entity4.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Title", titleWidth, titleHeight, 0, portraitPageBoudingBox.UpperLeftPoint.Y - titleHeight));
            yield return entity4;

            LayoutViewModel entity5 = new LayoutViewModel() { Orientation = FilterPageOrientation.Landscape, PageSize = pageSize, Image = string.Format(CultureInfo.InvariantCulture, imagePathFormat, 5, "small"), Description = GisEditor.LanguageManager.GetStringResource("NewLayoutViewModelSimpleLayoutDescription") };
            entity5.PrinterLayers.Add(PrinterLayerHelper.GetMapPrinterLayer(landscapePageBoudingBox.Width * 0.9, landscapePageBoudingBox.Height * 0.7, 0, -titleHeight));
            entity5.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Title", titleWidth, titleHeight, 0, landscapePageBoudingBox.UpperLeftPoint.Y - titleHeight));
            yield return entity5;

            LayoutViewModel entity6 = new LayoutViewModel() { Orientation = FilterPageOrientation.Landscape, PageSize = pageSize, Image = string.Format(CultureInfo.InvariantCulture, imagePathFormat, 6, "small"), Description = GisEditor.LanguageManager.GetStringResource("NewLayoutViewModelTitleImageTextDescription") };
            entity6.PrinterLayers.Add(PrinterLayerHelper.GetMapPrinterLayer(landscapePageBoudingBox.Width * 0.65, landscapePageBoudingBox.Height * 0.7, -landscapePageBoudingBox.Width * 0.15, -titleHeight));
            entity6.PrinterLayers.Add(PrinterLayerHelper.GetImagePrinterLayer(landscapePageBoudingBox.Width * 0.2, landscapePageBoudingBox.Width * 0.2, landscapePageBoudingBox.Width * 0.3, landscapePageBoudingBox.Height * 0.15));
            entity6.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Text", titleWidth, titleHeight, landscapePageBoudingBox.Width * 0.3, landscapePageBoudingBox.Height * -0.25));
            entity6.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Title", titleWidth, titleHeight, 0, landscapePageBoudingBox.UpperLeftPoint.Y - titleHeight));
            yield return entity6;

            LayoutViewModel entity7 = new LayoutViewModel() { Orientation = FilterPageOrientation.Landscape, PageSize = pageSize, Image = string.Format(CultureInfo.InvariantCulture, imagePathFormat, 7, "small"), Description = GisEditor.LanguageManager.GetStringResource("NewLayoutViewModelFeatureTitleDescription") };
            entity7.PrinterLayers.Add(PrinterLayerHelper.GetMapPrinterLayer(landscapePageBoudingBox.Width * 0.6, landscapePageBoudingBox.Height * 0.7, landscapePageBoudingBox.Width * -0.16, -titleHeight * 0.8));
            entity7.PrinterLayers.Add(PrinterLayerHelper.GetDataGridPrinterLayer(landscapePageBoudingBox.Width * 0.3, landscapePageBoudingBox.Height * 0.7, landscapePageBoudingBox.Width * 0.32, -titleHeight * 0.8));
            entity7.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Title", titleWidth, titleHeight, 0, landscapePageBoudingBox.UpperLeftPoint.Y - titleHeight));
            yield return entity7;

            LayoutViewModel entity8 = new LayoutViewModel() { Orientation = FilterPageOrientation.Landscape, PageSize = pageSize, Image = string.Format(CultureInfo.InvariantCulture, imagePathFormat, 8, "small"), Description = GisEditor.LanguageManager.GetStringResource("NewLayoutViewModelFeatureTitleDescription") };
            var map8 = PrinterLayerHelper.GetMapPrinterLayer(landscapePageBoudingBox.Width * 0.6, landscapePageBoudingBox.Height * 0.7, landscapePageBoudingBox.Width * -0.16, -titleHeight * 0.8);
            entity8.PrinterLayers.Add(map8);
            entity8.PrinterLayers.Add(PrinterLayerHelper.GetScaleBarPrinterLayer(map8, landscapePageBoudingBox.Width * 0.15, landscapePageBoudingBox.Height * 0.1, landscapePageBoudingBox.Width * -0.36, landscapePageBoudingBox.Height * -0.45));
            entity8.PrinterLayers.Add(PrinterLayerHelper.GetImagePrinterLayer(landscapePageBoudingBox.Width * 0.2, landscapePageBoudingBox.Height * 0.2, landscapePageBoudingBox.Width * 0.3, landscapePageBoudingBox.Height * 0.25));
            entity8.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Text", landscapePageBoudingBox.Width * 0.2, landscapePageBoudingBox.Height * 0.2, landscapePageBoudingBox.Width * 0.3, 0));
            entity8.PrinterLayers.Add(PrinterLayerHelper.GetLegendPrinterLayer(landscapePageBoudingBox.Width * 0.2, landscapePageBoudingBox.Height * 0.3, landscapePageBoudingBox.Width * 0.3, landscapePageBoudingBox.Height * -0.25));
            entity8.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Title", titleWidth, titleHeight, 0, landscapePageBoudingBox.UpperLeftPoint.Y - titleHeight));
            yield return entity8;

            LayoutViewModel entity9 = new LayoutViewModel() { Orientation = FilterPageOrientation.Landscape, PageSize = pageSize, Image = string.Format(CultureInfo.InvariantCulture, imagePathFormat, 9, "small"), Description = GisEditor.LanguageManager.GetStringResource("NewLayoutViewModelSimpleLayoutDescription") };
            entity9.PrinterLayers.Add(PrinterLayerHelper.GetMapPrinterLayer(landscapePageBoudingBox.Width * 0.9, landscapePageBoudingBox.Height * 0.7, 0, -titleHeight));
            entity9.PrinterLayers.Add(PrinterLayerHelper.GetLabelPrinterLayer("Title", titleWidth, titleHeight, 0, landscapePageBoudingBox.UpperLeftPoint.Y - titleHeight));
            entity9.PrinterLayers.Add(PrinterLayerHelper.GetCurrentDatePrinterLayer(2, 1, 3.5, -3.5));
            yield return entity9;
        }

        private IEnumerable<LayoutViewModel> Search()
        {
            var searchResults = selectedOrientation != FilterPageOrientation.AllOrientations ?
                allLayoutEntities.Where(layerEntity => layerEntity.Orientation == selectedOrientation && layerEntity.PageSize == selectedPageSize)
                : allLayoutEntities.Where(layerEntity => layerEntity.PageSize == selectedPageSize);
            return searchResults;
        }

        private void UpdateSearchResult()
        {
            resultLayoutEntities.Clear();
            foreach (var item in Search())
            {
                resultLayoutEntities.Add(item);
            }
            if (resultLayoutEntities.Count > 0)
            {
                SelectedLayout = resultLayoutEntities.FirstOrDefault();
            }
        }
    }
}