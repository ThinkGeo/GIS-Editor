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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for PrintMapWindow.xaml
    /// </summary>
    public partial class PrintMapWindow : Window
    {
        private PrinterPageSize savedSize;
        private float savedWidth;
        private float savedHeight;

        private SimplifyMapPrinterLayer geoMapPrinterLayer;
        private BoundingBoxSelectorMapTool boundingBoxSelector;

        public PrintMapWindow()
        {
            InitializeComponent();
            printMapViewModel.RemovingItem += new EventHandler<RoutedEventArgs>(Entity_RemovingItem);
            printMapViewModel.EditingItem += new EventHandler<RoutedEventArgs>(Entity_EditingItem);
            printMapViewModel.SetPosition += new EventHandler<RoutedEventArgs>(PrintMapViewModel_SetPosition);
            printMapViewModel.EditPrinterLayer = new Action<PrinterLayer>(Edit);
            PreviewMouseMove += new MouseEventHandler(PrintMapWindow_MouseMove);
            HelpContainer.Content = HelpResourceHelper.GetHelpButton("PrintMapHelp", HelpButtonMode.NormalButton);
            printMapViewModel.PropertyChanged += new PropertyChangedEventHandler(PrintMapViewModel_PropertyChanged);
            PreserveExtentRadioButton.IsChecked = !AppMenuUIPlugin.PreserveScale;
            PreserveScaleRadioButton.IsChecked = AppMenuUIPlugin.PreserveScale;
            KeyUp += new KeyEventHandler(PrintMapWindow_KeyUp);
            KeyDown += new KeyEventHandler(PrintMapWindow_KeyDown);
            Closing += new CancelEventHandler(PrintMapWindow_Closing);
        }

        private void PrintMapWindow_Closing(object sender, CancelEventArgs e)
        {
            printMapViewModel.SavePrinterlayerToProject();
            AppMenuUIPlugin.AllSignatures = printMapViewModel.SaveSignatures();
            if (printMapViewModel.SelectedSignature != null)
            {
                AppMenuUIPlugin.SelectedSignatureName = printMapViewModel.SelectedSignature.Name;
            }
        }

        private void PrintMapWindow_KeyDown(object sender, KeyEventArgs e)
        {
            printMapViewModel.KeyDown(e);
        }

        private void PrintMapWindow_KeyUp(object sender, KeyEventArgs e)
        {
            printMapViewModel.KeyUP(e);
        }

        private void PrintMapViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!AppMenuUIPlugin.PreserveScale)
            {
                if (e.PropertyName.Equals("Width"))
                {
                    savedWidth = printMapViewModel.Width;
                }
                else if (e.PropertyName.Equals("Height"))
                {
                    savedHeight = printMapViewModel.Height;
                }
                else if (e.PropertyName.Equals("Size"))
                {
                    savedSize = printMapViewModel.Size;
                }
            }
        }

        #region Events

        [System.Reflection.Obfuscation]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(AppMenuUIPlugin.AllSignatures))
            {
                PrintLayersSerializeProxy serializeProxy = GisEditor.Serializer.Deserialize(AppMenuUIPlugin.AllSignatures) as PrintLayersSerializeProxy;
                if (serializeProxy != null)
                {
                    foreach (var labelPrinterLayer in serializeProxy.PrinterLayers.OfType<LabelPrinterLayer>())
                    {
                        printMapViewModel.Signatures.Add(new SignatureViewModel(labelPrinterLayer.Name, labelPrinterLayer));
                    }
                }
            }
            if (!string.IsNullOrEmpty(AppMenuUIPlugin.SelectedSignatureName))
            {
                printMapViewModel.SelectedSignature = printMapViewModel.Signatures.FirstOrDefault(s => s.Name == AppMenuUIPlugin.SelectedSignatureName);
            }

            printMapViewModel.IsBusy = true;
            printMapViewModel.BusyContent = "Loading layout...";
            Dispatcher.BeginInvoke(() =>
            {
                printMapViewModel.SetUpMap();

                var appMenuUIPlugin = GisEditor.UIManager.GetActiveUIPlugins<AppMenuUIPlugin>().FirstOrDefault();
                if (appMenuUIPlugin != null && !string.IsNullOrEmpty(appMenuUIPlugin.PrintedLayoutXml))
                {
                    printMapViewModel.LoadLayout(appMenuUIPlugin.PrintedLayoutXml, false);
                }
                else if (GisEditor.ActiveMap != null)
                {
                    printMapViewModel.LoadPrinterLayersFromActiveMap();
                }

                if (AppMenuUIPlugin.PreserveScale)
                {
                    SimplifyMapPrinterLayer simplifyMapPrinterLayer = printMapViewModel.PrinterOverlay.PrinterLayers.Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer)).FirstOrDefault() as SimplifyMapPrinterLayer;
                    if (simplifyMapPrinterLayer != null)
                    {
                        AdjustPageSize(simplifyMapPrinterLayer);
                    }
                }
                if (printMapViewModel.SelectedSignature != null)
                {
                    printMapViewModel.ApplySignatureCommand.Execute(null);
                }

                printMapViewModel.CurrentZoom = "80%";
                printMapViewModel.IsBusy = false;
            });
        }

        private void PrintMapWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Cursor = null;
        }

        [System.Reflection.Obfuscation]
        private void Entity_RemovingItem(object sender, RoutedEventArgs e)
        {
            MapPrinterLayer mapPrinterLayer = null;
            PrinterLayer printerLayer = (sender as MenuItem).Tag as PrinterLayer;
            if ((mapPrinterLayer = printerLayer as MapPrinterLayer) != null)
            {
                var scalePrinterLayers = printMapViewModel.PrinterOverlay.PrinterLayers
                    .Where(l =>
                    {
                        var scaleLinePrinterLayer = l as ScaleLinePrinterLayer;
                        var scaleBarPrinterLayer = l as ScaleBarPrinterLayer;
                        return (scaleLinePrinterLayer != null && scaleLinePrinterLayer.MapPrinterLayer == mapPrinterLayer) || (scaleBarPrinterLayer != null && scaleBarPrinterLayer.MapPrinterLayer == mapPrinterLayer);
                    })
                    .ToArray();
                if (scalePrinterLayers.Length > 0)
                {
                    if (MessageBox.Show(GisEditor.LanguageManager.GetStringResource("RemoveMapElementAlert"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        foreach (var item in scalePrinterLayers)
                        {
                            printMapViewModel.PrinterOverlay.PrinterLayers.Remove(item);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
            printMapViewModel.PrinterOverlay.PrinterLayers.Remove(printerLayer);
            printMapViewModel.PrintMap.Refresh();
        }

        [System.Reflection.Obfuscation]
        private void Entity_EditingItem(object sender, RoutedEventArgs e)
        {
            var printerLayer = (sender as MenuItem).Tag as PrinterLayer;
            if (printerLayer != null)
            {
                Edit(printerLayer);
            }
        }

        private void Edit(PrinterLayer printerLayer)
        {
            Type actualType = printerLayer.GetType();

            if (actualType == typeof(LabelPrinterLayer))
            {
                TextElementWindow textElementWindow = new TextElementWindow();
                textElementWindow.SetProperties(printerLayer);
                ShowTextElementWindow(textElementWindow, printerLayer);
            }
            else if (actualType == typeof(ProjectPathPrinterLayer))
            {
                string projectPath = ((ProjectPathPrinterLayer)printerLayer).ProjectPath;
                Uri uri = GisEditor.ProjectManager.ProjectUri;
                if (!File.Exists(projectPath) && File.Exists(uri.LocalPath))
                {
                    projectPath = uri.LocalPath;
                    ((ProjectPathPrinterLayer)printerLayer).ProjectPath = projectPath;
                }
                ProjectPathElementWindow projectPathElementWindow = new ProjectPathElementWindow(projectPath) { Owner = this };
                projectPathElementWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                projectPathElementWindow.SetProperties((ProjectPathPrinterLayer)printerLayer);
                ShowDateElementWindow(projectPathElementWindow, printerLayer);
            }
            else if (actualType == typeof(DatePrinterLayer))
            {
                DateElementWindow dateElementWindow = new DateElementWindow { Owner = this };
                dateElementWindow.SetProperties((DatePrinterLayer)printerLayer);
                ShowDateElementWindow(dateElementWindow, printerLayer);
            }
            else if (actualType == typeof(ImagePrinterLayer))
            {
                ImageElementWindow imageElementWindow = new ImageElementWindow { Owner = this };
                imageElementWindow.SetProperties(printerLayer);
                ShowImageElementWindow(imageElementWindow, printerLayer);
            }
            else if (actualType == typeof(ScaleLinePrinterLayer))
            {
                ScaleLineElementWindow scaleLineElementWindow = new ScaleLineElementWindow((printerLayer as ScaleLinePrinterLayer).MapPrinterLayer) { Owner = this };
                scaleLineElementWindow.SetProperties(printerLayer);
                ShowScaleLineElementWindow(scaleLineElementWindow, printerLayer);
            }
            else if (actualType == typeof(ScaleBarPrinterLayer))
            {
                ScaleBarElementWindow scaleBarElementWindow = new ScaleBarElementWindow((printerLayer as ScaleBarPrinterLayer).MapPrinterLayer) { Owner = this };
                scaleBarElementWindow.SetProperties(printerLayer);
                ShowScaleBarElementWindow(scaleBarElementWindow, printerLayer);
            }
            else if (actualType == typeof(DataGridPrinterLayer))
            {
                DataGridElementWindow dataGridElementWindow = new DataGridElementWindow { Owner = this };
                dataGridElementWindow.SetProperties(printerLayer);
                ShowDataGridElementWindow(dataGridElementWindow, printerLayer);
            }
            else if (actualType == typeof(LegendPrinterLayer) || actualType.IsSubclassOf(typeof(LegendPrinterLayer)))
            {
                LegendPrinterLayer legendPrinterLayer = printerLayer as LegendPrinterLayer;
                if (legendPrinterLayer != null)
                {
                    LegendAdornmentLayerViewModel notifiedLegendAdornmentLayer = new LegendAdornmentLayerViewModel();
                    notifiedLegendAdornmentLayer.LegendItems.Clear();
                    notifiedLegendAdornmentLayer.BackgroundMask = legendPrinterLayer.BackgroundMask;
                    notifiedLegendAdornmentLayer.Height = legendPrinterLayer.Height;
                    notifiedLegendAdornmentLayer.Width = legendPrinterLayer.Width;
                    notifiedLegendAdornmentLayer.XOffsetInPixel = legendPrinterLayer.XOffsetInPixel;
                    notifiedLegendAdornmentLayer.YOffsetInPixel = legendPrinterLayer.YOffsetInPixel;
                    if (legendPrinterLayer.Title != null)
                    {
                        LegendItemViewModel notifiedLegendItem = new LegendItemViewModel() { LegendItemType = LegendItemType.Header };
                        notifiedLegendItem.LoadFromLegendItem(legendPrinterLayer.Title);
                        notifiedLegendAdornmentLayer.LegendItems.Add(notifiedLegendItem);
                    }
                    foreach (var item in legendPrinterLayer.LegendItems)
                    {
                        LegendItemViewModel notifiedLegendItem = new LegendItemViewModel();
                        notifiedLegendItem.LoadFromLegendItem(item);
                        notifiedLegendAdornmentLayer.LegendItems.Add(notifiedLegendItem);
                    }
                    if (legendPrinterLayer.Footer != null)
                    {
                        LegendItemViewModel notifiedLegendItem = new LegendItemViewModel() { LegendItemType = LegendItemType.Footer };
                        notifiedLegendItem.LoadFromLegendItem(legendPrinterLayer.Footer);
                        notifiedLegendAdornmentLayer.LegendItems.Add(notifiedLegendItem);
                    }

                    LegendEditor legendEditor = new LegendEditor(notifiedLegendAdornmentLayer) { Owner = this, HideNameAndLocationPanel = true };
                    ShowLegendWindow(notifiedLegendAdornmentLayer, legendEditor, legendPrinterLayer);
                }
            }
        }

        #endregion Events

        #region Add & Edit Elements

        [System.Reflection.Obfuscation]
        private void MapElementClick(object sender, RoutedEventArgs e)
        {
            if (GisEditor.ActiveMap != null)
            {
                //remove IsDrawing = true, when it is ture, the layer has style(gradient fills, hatch fills or texture fills) will be displayed in white
                var mapPrinterLayer = new SimplifyMapPrinterLayer();
                mapPrinterLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;

                var adapter = new MapPrinterLayerAdapter(GisEditor.ActiveMap);
                adapter.LoadFromActiveMap(mapPrinterLayer);
                mapPrinterLayer.SetDescriptionLayerBackground();
                mapPrinterLayer.Open();
                RectangleShape rectangleShape = printMapViewModel.GetPageBoundingBox(PrintingUnit.Inch);
                var pageBoundingBox = printMapViewModel.GetPageBoundingBox(PrintingUnit.Inch);
                var mapCount = printMapViewModel.PrinterOverlay.PrinterLayers.Count(l => l.GetType() == typeof(SimplifyMapPrinterLayer));
                if (mapCount == 0)
                    mapPrinterLayer.SetPosition(pageBoundingBox.Width - 2, pageBoundingBox.Height - 2, 0, 0, PrintingUnit.Inch);
                else
                    mapPrinterLayer.SetPosition(pageBoundingBox.Width - 3, pageBoundingBox.Height - 3, 0, 0, PrintingUnit.Inch);
                printMapViewModel.PrinterOverlay.PrinterLayers.Add(mapPrinterLayer);
                printMapViewModel.PrinterOverlay.Refresh();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("NoMapAlert"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"));
            }
        }

        [System.Reflection.Obfuscation]
        private void TextElementClick(object sender, RoutedEventArgs e)
        {
            TextElementWindow textElementWindow = new TextElementWindow { Owner = this };
            ShowTextElementWindow(textElementWindow, null);
        }

        [Obfuscation]
        private void CurrentDateClick(object sender, RoutedEventArgs e)
        {
            DateElementWindow dateElementWindow = new DateElementWindow { Owner = this };
            ShowDateElementWindow(dateElementWindow, null);
        }

        [Obfuscation]
        private void ProjectPathClick(object sender, RoutedEventArgs e)
        {
            Uri uri = GisEditor.ProjectManager.ProjectUri;
            string projectPath = uri.LocalPath;
            if (!File.Exists(projectPath))
            {
                projectPath = "Current project Path";
            }
            ProjectPathElementWindow projectPathElementWindow = new ProjectPathElementWindow(projectPath) { Owner = this };
            ShowDateElementWindow(projectPathElementWindow, null);
        }

        [System.Reflection.Obfuscation]
        private void ImageElementClick(object sender, RoutedEventArgs e)
        {
            ImageElementWindow imageElementWindow = new ImageElementWindow { Owner = this };
            ShowImageElementWindow(imageElementWindow, null);
        }

        [System.Reflection.Obfuscation]
        private void ScaleLineClick(object sender, RoutedEventArgs e)
        {
            if (printMapViewModel.SelectedPrinterLayer is MapPrinterLayer)
            {
                ScaleLineElementWindow scaleLineElementWindow = new ScaleLineElementWindow((MapPrinterLayer)printMapViewModel.SelectedPrinterLayer) { Owner = this };
                ShowScaleLineElementWindow(scaleLineElementWindow, null);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("NeedMapElementAlert"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"));
            }
        }

        [System.Reflection.Obfuscation]
        private void ScaleBarClick(object sender, RoutedEventArgs e)
        {
            if (printMapViewModel.SelectedPrinterLayer is MapPrinterLayer)
            {
                ScaleBarElementWindow scaleBarElementWindow = new ScaleBarElementWindow((MapPrinterLayer)printMapViewModel.SelectedPrinterLayer) { Owner = this };
                ShowScaleBarElementWindow(scaleBarElementWindow, null);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("NeedMapElementAlert"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"));
            }
        }

        [System.Reflection.Obfuscation]
        private void DataGridClick(object sender, RoutedEventArgs e)
        {
            DataGridElementWindow dataGridElementWindow = new DataGridElementWindow { Owner = this };
            ShowDataGridElementWindow(dataGridElementWindow, null);
        }

        [System.Reflection.Obfuscation]
        private void LegendClick(object sender, RoutedEventArgs e)
        {
            if (GisEditor.ActiveMap != null)
            {
                LegendAdornmentLayerViewModel notifiedLegendAdornmentLayer = new LegendAdornmentLayerViewModel();
                LegendEditor legendEditor = new LegendEditor(notifiedLegendAdornmentLayer) { Owner = this, HideNameAndLocationPanel = true };
                ShowLegendWindow(notifiedLegendAdornmentLayer, legendEditor, null);
            }
            else System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("NoMapAlert"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"));
        }

        private void ShowDateElementWindow(ProjectPathElementWindow projectPathElementWindow, PrinterLayer printerlayer)
        {
            if (projectPathElementWindow.ShowDialog().GetValueOrDefault())
            {
                ProjectPathElementViewModel projectPathElementViewModel = projectPathElementWindow.DataContext as ProjectPathElementViewModel;
                if (projectPathElementViewModel != null)
                {
                    if (printerlayer == null)
                    {
                        ProjectPathPrinterLayer projectPathPrinterLayer = new ProjectPathPrinterLayer();
                        projectPathPrinterLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;
                        projectPathPrinterLayer.LoadFromViewModel(projectPathElementViewModel);
                        projectPathPrinterLayer.Open();
                        projectPathPrinterLayer.SetPosition(2, 1, 0, -3, PrintingUnit.Inch);

                        if (projectPathPrinterLayer.PrinterWrapMode == PrinterWrapMode.AutoSizeText)
                        {
                            var oldWorldBBox = projectPathPrinterLayer.GetPosition(PrintingUnit.Point);
                            var textWorldCenter = oldWorldBBox.GetCenterPoint();
                            ResetPosition(projectPathPrinterLayer, textWorldCenter);
                        }

                        printMapViewModel.PrinterOverlay.PrinterLayers.Add(projectPathPrinterLayer);
                        var centerPoint = projectPathPrinterLayer.GetBoundingBox().GetCenterPoint();
                        printMapViewModel.PrinterOverlay.MouseClick(new InteractionArguments() { WorldX = centerPoint.X, WorldY = centerPoint.Y });
                    }
                    else
                    {
                        ProjectPathPrinterLayer projectPathPrinterLayer = (ProjectPathPrinterLayer)printerlayer;
                        projectPathPrinterLayer.Open();
                        projectPathPrinterLayer.LoadFromViewModel(projectPathElementViewModel);

                        if (projectPathPrinterLayer.PrinterWrapMode == PrinterWrapMode.AutoSizeText)
                        {
                            var oldWorldBBox = projectPathPrinterLayer.GetPosition(PrintingUnit.Point);
                            var textWorldCenter = oldWorldBBox.GetCenterPoint();
                            ResetPosition(projectPathPrinterLayer, textWorldCenter);
                        }
                    }
                    printMapViewModel.PrintMap.Refresh();
                }
            }
        }

        private void ShowDateElementWindow(DateElementWindow dateElementWindow, PrinterLayer printerlayer)
        {
            if (dateElementWindow.ShowDialog().GetValueOrDefault())
            {
                DateElementViewModel dateElementViewModel = dateElementWindow.DataContext as DateElementViewModel;
                if (dateElementViewModel != null)
                {
                    if (printerlayer == null)
                    {
                        DatePrinterLayer datePrinterLayer = new DatePrinterLayer();
                        datePrinterLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;
                        datePrinterLayer.LoadFromViewModel(dateElementViewModel);
                        datePrinterLayer.Open();
                        datePrinterLayer.SetPosition(2, 1, 3.5, -3, PrintingUnit.Inch);

                        if (datePrinterLayer.PrinterWrapMode == PrinterWrapMode.AutoSizeText)
                        {
                            var oldWorldBBox = datePrinterLayer.GetPosition(PrintingUnit.Point);
                            var textWorldCenter = oldWorldBBox.GetCenterPoint();
                            ResetPosition(datePrinterLayer, textWorldCenter);
                        }

                        printMapViewModel.PrinterOverlay.PrinterLayers.Add(datePrinterLayer);
                        var centerPoint = datePrinterLayer.GetBoundingBox().GetCenterPoint();
                        printMapViewModel.PrinterOverlay.MouseClick(new InteractionArguments() { WorldX = centerPoint.X, WorldY = centerPoint.Y });
                    }
                    else
                    {
                        DatePrinterLayer datePrinterLayer = (DatePrinterLayer)printerlayer;
                        datePrinterLayer.Open();
                        datePrinterLayer.LoadFromViewModel(dateElementViewModel);

                        if (datePrinterLayer.PrinterWrapMode == PrinterWrapMode.AutoSizeText)
                        {
                            var oldWorldBBox = datePrinterLayer.GetPosition(PrintingUnit.Point);
                            var textWorldCenter = oldWorldBBox.GetCenterPoint();
                            ResetPosition(datePrinterLayer, textWorldCenter);
                        }
                    }
                    printMapViewModel.PrintMap.Refresh();
                }
            }
        }

        private void ShowTextElementWindow(TextElementWindow textElementWindow, PrinterLayer printerlayer)
        {
            if (textElementWindow.ShowDialog().GetValueOrDefault())
            {
                TextElementViewModel textElementViewModel = textElementWindow.DataContext as TextElementViewModel;
                if (textElementViewModel != null)
                {
                    if (printerlayer == null)
                    {
                        LabelPrinterLayer labelPrinterLayer = new LabelPrinterLayer();
                        labelPrinterLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;
                        labelPrinterLayer.LoadFromViewModel(textElementViewModel);
                        labelPrinterLayer.Open();
                        labelPrinterLayer.SetPosition(2, 1, 0, 0, PrintingUnit.Inch);

                        if (labelPrinterLayer.PrinterWrapMode == PrinterWrapMode.AutoSizeText)
                        {
                            var oldWorldBBox = labelPrinterLayer.GetPosition(PrintingUnit.Point);
                            var textWorldCenter = oldWorldBBox.GetCenterPoint();
                            ResetPosition(labelPrinterLayer, textWorldCenter);
                        }

                        printMapViewModel.PrinterOverlay.PrinterLayers.Add(labelPrinterLayer);
                        var centerPoint = labelPrinterLayer.GetBoundingBox().GetCenterPoint();
                        printMapViewModel.PrinterOverlay.MouseClick(new InteractionArguments() { WorldX = centerPoint.X, WorldY = centerPoint.Y });
                    }
                    else
                    {
                        var labelPrinterLayer = (LabelPrinterLayer)printerlayer;
                        labelPrinterLayer.Open();
                        labelPrinterLayer.LoadFromViewModel(textElementViewModel);

                        if (labelPrinterLayer.PrinterWrapMode == PrinterWrapMode.AutoSizeText)
                        {
                            var oldWorldBBox = labelPrinterLayer.GetPosition(PrintingUnit.Point);
                            var textWorldCenter = oldWorldBBox.GetCenterPoint();
                            //var newScreenBBox = new GdiPlusGeoCanvas().MeasureText(labelPrinterLayer.Text, labelPrinterLayer.Font);
                            //var newHalfWorldWidth = newScreenBBox.Width * oldWorldBBox.Width * .5 / oldScreenBBox.Width;
                            //var textWorldBBox = new RectangleShape(textWorldCenter.X - newHalfWorldWidth, oldWorldBBox.UpperLeftPoint.Y, textWorldCenter.X + newHalfWorldWidth, oldWorldBBox.LowerRightPoint.Y);
                            //labelPrinterLayer.SetPosition(textWorldBBox);
                            //printMapViewModel.PrinterOverlay.MouseClick(new InteractionArguments() { WorldX = textWorldCenter.X, WorldY = textWorldCenter.Y, MouseButton = MapMouseButton.Left });
                            ResetPosition(labelPrinterLayer, textWorldCenter);
                        }
                    }
                    printMapViewModel.PrintMap.Refresh();
                }
            }
        }

        private void ResetPosition(ProjectPathPrinterLayer projectPathPrinterLayer, PointShape textWorldCenter)
        {
            var newScreenBBox = new PlatformGeoCanvas().MeasureText(projectPathPrinterLayer.ProjectPath, projectPathPrinterLayer.Font);
            var scaledWidth = newScreenBBox.Width * .55;
            var scaledHeight = newScreenBBox.Height * .55;
            var upperLeft = new PointShape(textWorldCenter.X - scaledWidth, textWorldCenter.Y + scaledHeight);
            var lowerRight = new PointShape(textWorldCenter.X + scaledWidth, textWorldCenter.Y - scaledHeight);
            var textBoudingBoxInInch = new RectangleShape(upperLeft, lowerRight);
            projectPathPrinterLayer.SetPosition(textBoudingBoxInInch, PrintingUnit.Point);
            printMapViewModel.PrinterOverlay.MouseClick(new InteractionArguments() { WorldX = textWorldCenter.X, WorldY = textWorldCenter.Y, MouseButton = MapMouseButton.Left });
        }

        private void ResetPosition(DatePrinterLayer datePrinterLayer, PointShape textWorldCenter)
        {
            var newScreenBBox = new PlatformGeoCanvas().MeasureText(datePrinterLayer.DateString, datePrinterLayer.Font);
            var scaledWidth = newScreenBBox.Width * .55;
            var scaledHeight = newScreenBBox.Height * .55;
            var upperLeft = new PointShape(textWorldCenter.X - scaledWidth, textWorldCenter.Y + scaledHeight);
            var lowerRight = new PointShape(textWorldCenter.X + scaledWidth, textWorldCenter.Y - scaledHeight);
            var textBoudingBoxInInch = new RectangleShape(upperLeft, lowerRight);
            datePrinterLayer.SetPosition(textBoudingBoxInInch, PrintingUnit.Point);
            printMapViewModel.PrinterOverlay.MouseClick(new InteractionArguments() { WorldX = textWorldCenter.X, WorldY = textWorldCenter.Y, MouseButton = MapMouseButton.Left });
        }

        private void ResetPosition(LabelPrinterLayer labelPrinterLayer, PointShape textWorldCenter)
        {
            var newScreenBBox = new PlatformGeoCanvas().MeasureText(labelPrinterLayer.Text, labelPrinterLayer.Font);
            var scaledWidth = newScreenBBox.Width * .55;
            var scaledHeight = newScreenBBox.Height * .55;
            var upperLeft = new PointShape(textWorldCenter.X - scaledWidth, textWorldCenter.Y + scaledHeight);
            var lowerRight = new PointShape(textWorldCenter.X + scaledWidth, textWorldCenter.Y - scaledHeight);
            var textBoudingBoxInInch = new RectangleShape(upperLeft, lowerRight);
            labelPrinterLayer.SetPosition(textBoudingBoxInInch, PrintingUnit.Point);
            printMapViewModel.PrinterOverlay.MouseClick(new InteractionArguments() { WorldX = textWorldCenter.X, WorldY = textWorldCenter.Y, MouseButton = MapMouseButton.Left });
        }

        private void ShowImageElementWindow(ImageElementWindow imageElementWindow, PrinterLayer printerLayer)
        {
            if (imageElementWindow.ShowDialog().GetValueOrDefault())
            {
                ImageElementViewModel imageElementViewModel = imageElementWindow.DataContext as ImageElementViewModel;
                if (imageElementViewModel != null)
                {
                    //TODO: Here is a workaround, because when you use the default constructor and set DragMode as Fixed, image won't show up in the map
                    GeoImage geoImage = null;

                    geoImage = new GeoImage(new MemoryStream(imageElementViewModel.SelectedImage));

                    //geoImage = new GeoImage(streamInfo.Stream);

                    if (printerLayer == null)
                    {
                        ImagePrinterLayer imagePrinterLayer = new ImagePrinterLayer(geoImage, 0, 0, PrintingUnit.Inch) { DrawingExceptionMode = DrawingExceptionMode.DrawException };
                        imagePrinterLayer.LoadFromViewModel(imageElementViewModel);
                        int height = imagePrinterLayer.Image.GetHeight();
                        int width = imagePrinterLayer.Image.GetWidth();
                        imagePrinterLayer.Open();
                        imagePrinterLayer.SetPosition(width, height, imagePrinterLayer.GetPosition().GetCenterPoint(), PrintingUnit.Point);
                        printMapViewModel.PrinterOverlay.PrinterLayers.Add(imagePrinterLayer);
                    }
                    else
                    {
                        var imgPrinterLayer = printerLayer as ImagePrinterLayer;
                        imgPrinterLayer.LoadFromViewModel(imageElementViewModel);
                        imgPrinterLayer.Image = geoImage;
                    }
                    printMapViewModel.PrintMap.Refresh();
                }
            }
        }

        private void ShowScaleLineElementWindow(ScaleLineElementWindow scaleLineElementWindow, PrinterLayer printerlayer)
        {
            if (scaleLineElementWindow.ShowDialog().GetValueOrDefault())
            {
                ScaleLineElementViewModel scaleLineViewModel = scaleLineElementWindow.DataContext as ScaleLineElementViewModel;
                if (scaleLineViewModel != null)
                {
                    if (printerlayer == null)
                    {
                        ScaleLinePrinterLayer scaleLinePrinterLayer = ScaleLinePrinterLayerAdapter.GetScaleLinePrinterLayer(scaleLineViewModel);
                        printMapViewModel.PrinterOverlay.PrinterLayers.Add(scaleLinePrinterLayer);
                    }
                    else
                    {
                        (printerlayer as ScaleLinePrinterLayer).LoadFromViewModel(scaleLineViewModel);
                    }
                    printMapViewModel.PrintMap.Refresh();
                }
            }
        }

        private void ShowScaleBarElementWindow(ScaleBarElementWindow scaleBarElementWindow, PrinterLayer printerLayer)
        {
            if (scaleBarElementWindow.ShowDialog().GetValueOrDefault())
            {
                ScaleBarElementViewModel scaleBarViewModel = scaleBarElementWindow.DataContext as ScaleBarElementViewModel;
                if (scaleBarViewModel != null)
                {
                    if (printerLayer == null)
                    {
                        ScaleBarPrinterLayer scaleBarPrinterLayer = ScaleBarPrinterLayerAdapter.GetScaleBarPrinterLayer(scaleBarViewModel);
                        printMapViewModel.PrinterOverlay.PrinterLayers.Add(scaleBarPrinterLayer);
                    }
                    else
                    {
                        (printerLayer as ScaleBarPrinterLayer).LoadFromViewModel(scaleBarViewModel);
                    }
                    printMapViewModel.PrintMap.Refresh();
                }
            }
        }

        private void ShowDataGridElementWindow(DataGridElementWindow dataGridElementWindow, PrinterLayer printerLayer)
        {
            if (dataGridElementWindow.ShowDialog().GetValueOrDefault())
            {
                DataGridViewModel dataGridEntity = dataGridElementWindow.DataContext as DataGridViewModel;
                if (dataGridEntity != null)
                {
                    if (printerLayer == null)
                    {
                        DataGridPrinterLayer dataGridPrinterLayer = new DataGridPrinterLayer() { DrawingExceptionMode = DrawingExceptionMode.DrawException };
                        dataGridPrinterLayer.LoadFromViewModel(dataGridEntity);
                        dataGridPrinterLayer.Open();
                        dataGridPrinterLayer.SetPosition(2, 1, 0, 0, PrintingUnit.Inch);
                        printMapViewModel.PrinterOverlay.PrinterLayers.Add(dataGridPrinterLayer);
                    }
                    else
                    {
                        (printerLayer as DataGridPrinterLayer).LoadFromViewModel(dataGridEntity);
                    }
                    printMapViewModel.PrintMap.Refresh();
                }
            }
        }

        private void ShowLegendWindow(LegendAdornmentLayerViewModel notifiedLegendAdornmentLayer, LegendEditor legendEditor, PrinterLayer printerLayer)
        {
            if (legendEditor.ShowDialog().GetValueOrDefault())
            {
                if (!(notifiedLegendAdornmentLayer.LegendSizeMode == LegendSizeMode.Auto && notifiedLegendAdornmentLayer.LegendItems.Count == 0))
                {
                    LegendPrinterLayer legendPrinterLayer = new GisEditorLegendPrinterLayer(notifiedLegendAdornmentLayer.ToLegendAdornmentLayer()) { DrawingExceptionMode = DrawingExceptionMode.DrawException };
                    legendPrinterLayer.BackgroundMask.SetDrawingLevel();

                    var copiedLegendItems = new Collection<LegendItem>();
                    foreach (var legendItem in legendPrinterLayer.LegendItems)
                    {
                        var copiedItem = PrinterLayerHelper.CloneDeep<LegendItem>(legendItem);
                        if (copiedItem != null)
                        {
                            copiedItem.SetDrawingLevel();
                            copiedLegendItems.Add(copiedItem);
                        }
                    }
                    legendPrinterLayer.LegendItems.Clear();
                    foreach (var item in copiedLegendItems)
                    {
                        legendPrinterLayer.LegendItems.Add(item);
                    }

                    legendPrinterLayer.DragMode = PrinterDragMode.Draggable;
                    legendPrinterLayer.ResizeMode = PrinterResizeMode.Resizable;
                    legendPrinterLayer.Open();
                    var widthInInch = PrinterLayerHelper.GetInchValue(SizeUnit.Pixels, printMapViewModel.Dpi, legendPrinterLayer.Width);
                    var heightInInch = PrinterLayerHelper.GetInchValue(SizeUnit.Pixels, printMapViewModel.Dpi, legendPrinterLayer.Height);
                    if (printerLayer != null)
                    {
                        printerLayer.SafeProcess(() =>
                        {
                            legendPrinterLayer.SetPosition(widthInInch, heightInInch, printerLayer.GetPosition(PrintingUnit.Inch).GetCenterPoint(), PrintingUnit.Inch);
                        });
                        //printerLayer.Open();
                        //legendPrinterLayer.SetPosition(widthInInch, heightInInch, printerLayer.GetPosition(PrintingUnit.Inch).GetCenterPoint(), PrintingUnit.Inch);
                        //printerLayer.Close();
                    }
                    else
                    {
                        legendPrinterLayer.SetPosition(widthInInch, heightInInch, 0, 0, PrintingUnit.Inch);
                    }

                    int index = printMapViewModel.PrinterOverlay.PrinterLayers.IndexOf(printerLayer);
                    printMapViewModel.PrinterOverlay.PrinterLayers.Remove(printerLayer);
                    if (index >= 0)
                        printMapViewModel.PrinterOverlay.PrinterLayers.Insert(index, legendPrinterLayer);
                    else
                        printMapViewModel.PrinterOverlay.PrinterLayers.Add(legendPrinterLayer);
                    printMapViewModel.PrintMap.Refresh();
                }
            }
        }

        #endregion Add & Edit Elements

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PrintMapViewModel_SetPosition(object sender, RoutedEventArgs e)
        {
            PrintMapViewModel.CanUsePrint = false;
            Hide();
            geoMapPrinterLayer = (sender as MenuItem).Tag as SimplifyMapPrinterLayer;
            if (geoMapPrinterLayer != null)
            {
                InitializeBoundingBoxSelector();
            }
        }

        private void InitializeBoundingBoxSelector()
        {
            var boudingBox = geoMapPrinterLayer.GetPosition(PrintingUnit.Point);
            if (boundingBoxSelector == null) boundingBoxSelector = new BoundingBoxSelectorMapTool();
            boundingBoxSelector.Width = boudingBox.Width;
            boundingBoxSelector.Height = boudingBox.Height;
            boundingBoxSelector.PreserveSizeRatio = !AppMenuUIPlugin.PreserveScale;

            ClearBBoxSelectorFromAllMaps();
            GisEditor.ActiveMap.MapTools.Add(boundingBoxSelector);
            boundingBoxSelector.BoundingBoxSelectCompletedClick -= BoundingBoxSelectCompletedClick;
            boundingBoxSelector.BoundingBoxSelectCompletedClick += BoundingBoxSelectCompletedClick;
            boundingBoxSelector.IsEnabled = true;
            boundingBoxSelector.Loaded += new RoutedEventHandler(boundingBoxSelector_Loaded);
        }

        private void boundingBoxSelector_Loaded(object sender, RoutedEventArgs e)
        {
            boundingBoxSelector.AdjustSizeRatio();
            boundingBoxSelector.Loaded -= new RoutedEventHandler(boundingBoxSelector_Loaded);
        }

        private void ClearBBoxSelectorFromAllMaps()
        {
            if (boundingBoxSelector == null) return;

            foreach (var map in GisEditor.DockWindowManager.DocumentWindows.Select(d => d.Content).OfType<WpfMap>())
            {
                if (map.MapTools.Contains(boundingBoxSelector))
                {
                    map.MapTools.Remove(boundingBoxSelector);
                }
            }
        }

        private void RepositionScalePrinterLayer(PrinterLayer mapPrinterLayer, SimplifyMapPrinterLayer simplifyMapPrinterLayer)
        {
            RectangleShape pageBoundingbox = simplifyMapPrinterLayer.GetPosition(PrintingUnit.Inch);
            PointShape lowerLeftPoint = pageBoundingbox.LowerLeftPoint;
            double elementWidth = pageBoundingbox.Width * 0.15;
            double elementHeight = 0.5;
            mapPrinterLayer.SetPosition(elementWidth, elementHeight, lowerLeftPoint.X + elementWidth * 0.5, lowerLeftPoint.Y + elementHeight * .5, PrintingUnit.Inch);
        }

        private void BoundingBoxSelectCompletedClick(object sender, BoundingBoxSelectCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                PrintMapViewModel.CanUsePrint = true;
                var boundingBoxSelectorMapTool = sender as BoundingBoxSelectorMapTool;
                if (!e.Cancel && geoMapPrinterLayer != null)
                {
                    if (AppMenuUIPlugin.PreserveScale && e.ResultScale > 0)
                    {
                        ScaleSettingsRibbonGroupViewModel.SetNewScale(e.ResultScale, false);
                    }

                    printMapViewModel.IsBusy = true;
                    geoMapPrinterLayer.Layers.Clear();
                    var adapter = new MapPrinterLayerAdapter(GisEditor.ActiveMap, true);
                    adapter.LoadFromActiveMap(geoMapPrinterLayer);
                    if (e.ImageBytes != null)
                    {
                        geoMapPrinterLayer.MapImageCache = new GeoImage(new MemoryStream(e.ImageBytes));
                    }
                    geoMapPrinterLayer.DrawingMode = MapPrinterDrawingMode.Raster;
                    geoMapPrinterLayer.SetDescriptionLayerBackground();

                    if (AppMenuUIPlugin.PreserveScale && e.ResultScale > 0)
                    {
                        geoMapPrinterLayer.MapExtent = e.ResultBoundingBox;
                        PointShape centerPoint = geoMapPrinterLayer.GetPosition(PrintingUnit.Point).GetCenterPoint();

                        double resolution = MapUtils.GetResolutionFromScale(e.ResultScale, geoMapPrinterLayer.MapUnit);
                        double newWidth = e.ResultBoundingBox.Width / resolution;
                        double newHeight = e.ResultBoundingBox.Height / resolution;
                        double area = newWidth * newHeight;
                        if (area < 100000000)
                        {
                            geoMapPrinterLayer.SetPosition(newWidth, newHeight, centerPoint, PrintingUnit.Point);
                            printMapViewModel.PrinterOverlay.MouseClick(new InteractionArguments());

                            ScaleLinePrinterLayer[] scaleLinePrinterLayers = printMapViewModel.PrinterOverlay.PrinterLayers.OfType<ScaleLinePrinterLayer>().Where(s => s.MapPrinterLayer == geoMapPrinterLayer).ToArray();
                            ScaleBarPrinterLayer[] scaleBarPrinterLayers = printMapViewModel.PrinterOverlay.PrinterLayers.OfType<ScaleBarPrinterLayer>().Where(s => s.MapPrinterLayer == geoMapPrinterLayer).ToArray();

                            foreach (var item in scaleLinePrinterLayers)
                            {
                                this.RepositionScalePrinterLayer(item, geoMapPrinterLayer);
                            }
                            foreach (var item in scaleBarPrinterLayers)
                            {
                                this.RepositionScalePrinterLayer(item, geoMapPrinterLayer);
                            }
                            AdjustPageSize(geoMapPrinterLayer);
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("PrintMapWindowChooseAnotherScaleLabel"), GisEditor.LanguageManager.GetStringResource("MessageBoxWarningTitle"));
                        }
                    }
                    else
                    {
                        RectangleShape targetMapExtent = MapPrinterLayerAdapter.GetFixedScaledExtent(geoMapPrinterLayer.GetPosition(PrintingUnit.Inch), GisEditor.ActiveMap.CurrentResolution, e.ResultBoundingBox);
                        MapPrinterLayerAdapter.ResetFixedExtent(geoMapPrinterLayer, targetMapExtent);
                    }

                    var scaleLineLayer = printMapViewModel.PrinterOverlay.PrinterLayers.OfType<ScaleLinePrinterLayer>().FirstOrDefault();
                    var outOfRange = geoMapPrinterLayer.MapUnit == GeographyUnit.DecimalDegree && !PrinterLayerHelper.CheckDecimalDegreeIsInRange(e.ResultBoundingBox);
                    if (scaleLineLayer != null)
                    {
                        scaleLineLayer.MapUnit = geoMapPrinterLayer.MapUnit;
                        if (outOfRange)
                        {
                            printMapViewModel.PrinterOverlay.PrinterLayers.Remove(scaleLineLayer);
                        }
                    }
                    var tmpScaleLineLayer = ScaleLinePrinterLayerAdapter.GetScaleLinePrinterLayer(new ScaleLineElementViewModel(geoMapPrinterLayer));
                    printMapViewModel.IsSelectMapElement = !outOfRange && ScaleLineElementViewModel.IsValid(tmpScaleLineLayer);
                    printMapViewModel.PrintMap.Refresh();
                }

                ClearBBoxSelectorFromAllMaps();
                boundingBoxSelectorMapTool.BoundingBoxSelectCompletedClick -= BoundingBoxSelectCompletedClick;
                printMapViewModel.IsBusy = false;
            });
            ShowDialog();
        }

        private void AdjustPageSize(SimplifyMapPrinterLayer geoMapPrinterLayer)
        {
            if (printMapViewModel.AutoSelectPageSize)
            {
                PrinterPageSize resultPrinterPageSize = PrinterPageSize.Custom;
                RectangleShape currentBbox = geoMapPrinterLayer.GetPosition(PrintingUnit.Inch);
                foreach (PrinterPageSize item in Enum.GetValues(typeof(PrinterPageSize)))
                {
                    if (printMapViewModel.SizeBoundingBoxes.ContainsKey(item))
                    {
                        double width = 0;
                        double height = 0;
                        if (printMapViewModel.Orientation == PrinterOrientation.Landscape)
                        {
                            width = printMapViewModel.SizeBoundingBoxes[item].Height;
                            height = printMapViewModel.SizeBoundingBoxes[item].Width;
                        }
                        else
                        {
                            width = printMapViewModel.SizeBoundingBoxes[item].Width;
                            height = printMapViewModel.SizeBoundingBoxes[item].Height;
                        }

                        if (width >= currentBbox.Width && height >= currentBbox.Height)
                        {
                            resultPrinterPageSize = item;
                            break;
                        }
                    }
                }
                if (resultPrinterPageSize != PrinterPageSize.Custom)
                {
                    if (printMapViewModel.Size != resultPrinterPageSize)
                    {
                        printMapViewModel.Size = resultPrinterPageSize;
                    }
                }
                else
                {
                    printMapViewModel.Size = PrinterPageSize.Custom;
                    printMapViewModel.SelectedSizeUnit = SizeUnit.Inches;
                    if (printMapViewModel.Orientation == PrinterOrientation.Landscape)
                    {
                        printMapViewModel.Width = (float)currentBbox.Height + 2;
                        printMapViewModel.Height = (float)currentBbox.Width + 2;
                    }
                    else
                    {
                        printMapViewModel.Width = (float)currentBbox.Width + 2;
                        printMapViewModel.Height = (float)currentBbox.Height + 2;
                    }
                }
            }
        }

        [Obfuscation]
        private void PointTrackClick(object sender, RoutedEventArgs e)
        {
            if (printMapViewModel.IsPoint)
            {
                var result = printMapViewModel.EditTrackLayerPointStyle(null);
                if (!result) printMapViewModel.IsPoint = false;
                else printMapViewModel.PrintMap.Cursor = GisEditorCursors.DrawPoint;
            }
            else printMapViewModel.PrintMap.Cursor = GisEditorCursors.Normal;
            SetTrackMode(sender, TrackMode.Point);
            printMapViewModel.IsLine = false;
            printMapViewModel.IsPolygon = false;
            printMapViewModel.IsCircle = false;
            printMapViewModel.IsRectangle = false;
            printMapViewModel.IsSquare = false;
            printMapViewModel.IsEllipse = false;
        }

        [Obfuscation]
        private void LineTrackClick(object sender, RoutedEventArgs e)
        {
            if (printMapViewModel.IsLine)
            {
                var result = printMapViewModel.EditTrackLayerLineStyle(null);
                if (!result) printMapViewModel.IsLine = false;
                else printMapViewModel.PrintMap.Cursor = GisEditorCursors.DrawLine;
            }
            else printMapViewModel.PrintMap.Cursor = GisEditorCursors.Normal;

            SetTrackMode(sender, TrackMode.Line);
            printMapViewModel.IsPoint = false;
            printMapViewModel.IsPolygon = false;
            printMapViewModel.IsCircle = false;
            printMapViewModel.IsRectangle = false;
            printMapViewModel.IsSquare = false;
            printMapViewModel.IsEllipse = false;
        }

        [Obfuscation]
        private void PolygonTrackClick(object sender, RoutedEventArgs e)
        {
            if (printMapViewModel.IsPolygon)
            {
                var result = printMapViewModel.EditTrackLayerAreaStyle(null);
                if (!result) printMapViewModel.IsPolygon = false;
                else printMapViewModel.PrintMap.Cursor = GisEditorCursors.DrawPolygon;
            }
            else printMapViewModel.PrintMap.Cursor = GisEditorCursors.Normal;

            SetTrackMode(sender, TrackMode.Polygon);
            printMapViewModel.IsPoint = false;
            printMapViewModel.IsLine = false;
            printMapViewModel.IsCircle = false;
            printMapViewModel.IsRectangle = false;
            printMapViewModel.IsSquare = false;
            printMapViewModel.IsEllipse = false;
        }

        [Obfuscation]
        private void CircleTrackClick(object sender, RoutedEventArgs e)
        {
            if (printMapViewModel.IsCircle)
            {
                var result = printMapViewModel.EditTrackLayerAreaStyle(null);
                if (!result) printMapViewModel.IsCircle = false;
                else printMapViewModel.PrintMap.Cursor = GisEditorCursors.DrawCircle;
            }
            else printMapViewModel.PrintMap.Cursor = GisEditorCursors.Normal;

            SetTrackMode(sender, TrackMode.Circle);
            printMapViewModel.IsPoint = false;
            printMapViewModel.IsLine = false;
            printMapViewModel.IsPolygon = false;
            printMapViewModel.IsRectangle = false;
            printMapViewModel.IsSquare = false;
            printMapViewModel.IsEllipse = false;
        }

        [Obfuscation]
        private void RectangleTrackClick(object sender, RoutedEventArgs e)
        {
            if (printMapViewModel.IsRectangle)
            {
                var result = printMapViewModel.EditTrackLayerAreaStyle(null);
                if (!result) printMapViewModel.IsRectangle = false;
                else printMapViewModel.PrintMap.Cursor = GisEditorCursors.DrawRectangle;
            }
            else printMapViewModel.PrintMap.Cursor = GisEditorCursors.Normal;

            SetTrackMode(sender, TrackMode.Rectangle);
            printMapViewModel.IsPoint = false;
            printMapViewModel.IsLine = false;
            printMapViewModel.IsPolygon = false;
            printMapViewModel.IsCircle = false;
            printMapViewModel.IsSquare = false;
            printMapViewModel.IsEllipse = false;
        }

        [Obfuscation]
        private void SquareTrackClick(object sender, RoutedEventArgs e)
        {
            if (printMapViewModel.IsSquare)
            {
                var result = printMapViewModel.EditTrackLayerAreaStyle(null);
                if (!result) printMapViewModel.IsSquare = false;
                else printMapViewModel.PrintMap.Cursor = GisEditorCursors.DrawSqure;
            }
            else printMapViewModel.PrintMap.Cursor = GisEditorCursors.Normal;

            SetTrackMode(sender, TrackMode.Square);
            printMapViewModel.IsPoint = false;
            printMapViewModel.IsLine = false;
            printMapViewModel.IsPolygon = false;
            printMapViewModel.IsCircle = false;
            printMapViewModel.IsRectangle = false;
            printMapViewModel.IsEllipse = false;
        }

        [Obfuscation]
        private void EllipseTrackClick(object sender, RoutedEventArgs e)
        {
            if (printMapViewModel.IsEllipse)
            {
                var result = printMapViewModel.EditTrackLayerAreaStyle(null);
                if (!result) printMapViewModel.IsEllipse = false;
                else printMapViewModel.PrintMap.Cursor = GisEditorCursors.DrawEllipse;
            }
            else printMapViewModel.PrintMap.Cursor = GisEditorCursors.Normal;

            SetTrackMode(sender, TrackMode.Ellipse);
            printMapViewModel.IsPoint = false;
            printMapViewModel.IsLine = false;
            printMapViewModel.IsPolygon = false;
            printMapViewModel.IsCircle = false;
            printMapViewModel.IsRectangle = false;
            printMapViewModel.IsSquare = false;
        }

        private void SetTrackMode(object sender, TrackMode trackMode)
        {
            var isChecked = (sender as ToggleButton).IsChecked.GetValueOrDefault();
            if (isChecked)
            {
                printMapViewModel.PrintMap.TrackOverlay.TrackMode = trackMode;
                printMapViewModel.PrinterOverlay.IsEditable = false;
                printMapViewModel.PrinterOverlay.IsEditable = true;
                printMapViewModel.PrintMap.Refresh();
            }
            else printMapViewModel.PrintMap.TrackOverlay.TrackMode = TrackMode.None;
        }

        [Obfuscation]
        private void GroupBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                if (printMapViewModel.SelectedPrinterLayer != null)
                {
                    printMapViewModel.Copy(printMapViewModel.SelectedPrinterLayer);
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                printMapViewModel.Paste();
            }
            else if (e.Key == Key.Delete)
            {
                if (!(printMapViewModel.SelectedPrinterLayer is PagePrinterLayer))
                {
                    printMapViewModel.PrinterOverlay.PrinterLayers.Remove(printMapViewModel.SelectedPrinterLayer);
                    printMapViewModel.PrintMap.Refresh();
                }
            }
        }

        [Obfuscation]
        private void PreserveScaleChecked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                AppMenuUIPlugin.PreserveScale = true;
                AdjustPageSizeAndMapPosition();
            }
        }

        [Obfuscation]
        private void PreserveExtentChecked(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                AppMenuUIPlugin.PreserveScale = false;
                AdjustPageSizeAndMapPosition();
            }
        }

        private void AdjustPageSizeAndMapPosition()
        {
            if (boundingBoxSelector != null)
            {
                boundingBoxSelector.PreserveSizeRatio = !AppMenuUIPlugin.PreserveScale;
            }
            SimplifyMapPrinterLayer simplifyMapPrinterLayer = printMapViewModel.PrinterOverlay.PrinterLayers.Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer)).FirstOrDefault() as SimplifyMapPrinterLayer;
            if (simplifyMapPrinterLayer != null)
            {
                int index = printMapViewModel.PrinterOverlay.PrinterLayers.IndexOf(simplifyMapPrinterLayer);
                simplifyMapPrinterLayer.Layers.Clear();

                var mapPrinterLayer = new MapPrinterLayerAdapter(GisEditor.ActiveMap, true);
                mapPrinterLayer.LoadFromActiveMap(simplifyMapPrinterLayer);

                if (AppMenuUIPlugin.PreserveScale)
                {
                    simplifyMapPrinterLayer.SetPosition(GisEditor.ActiveMap.ActualWidth, GisEditor.ActiveMap.ActualHeight, 0, 0, PrintingUnit.Point);
                    PointShape centerPoint = simplifyMapPrinterLayer.GetBoundingBox().GetCenterPoint();
                    InteractionArguments interactionArguments = new InteractionArguments();
                    interactionArguments.WorldX = centerPoint.X;
                    interactionArguments.WorldY = centerPoint.Y;
                    printMapViewModel.PrinterOverlay.MouseClick(interactionArguments);
                    AdjustPageSize(simplifyMapPrinterLayer);
                }
                else
                {
                    if (savedSize == PrinterPageSize.Custom)
                    {
                        printMapViewModel.Width = savedWidth;
                        printMapViewModel.Height = savedHeight;
                    }
                    else
                    {
                        printMapViewModel.Size = savedSize;
                    }
                    RectangleShape bbox = printMapViewModel.GetPageBoundingBox(PrintingUnit.Inch, printMapViewModel.Size, printMapViewModel.Orientation);
                    simplifyMapPrinterLayer.SetPosition(bbox.Width - 2, bbox.Height - 2, 0, 0, PrintingUnit.Inch);
                    PointShape bboxInPoint = simplifyMapPrinterLayer.GetBoundingBox().GetCenterPoint();
                    InteractionArguments interactionArguments = new InteractionArguments();
                    interactionArguments.WorldX = bboxInPoint.X;
                    interactionArguments.WorldY = bboxInPoint.Y;
                    printMapViewModel.PrinterOverlay.MouseClick(interactionArguments);
                }
                //printMapViewModel.CurrentZoom = printMapViewModel.CurrentZoom;
                printMapViewModel.PrintMap.Refresh();
            }
        }

        [Obfuscation]
        private void RefreshMapElementClick(object sender, RoutedEventArgs e)
        {
            List<SimplifyMapPrinterLayer> mapLayers = printMapViewModel.PrinterOverlay.PrinterLayers.Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer)).OfType<SimplifyMapPrinterLayer>().ToList();

            var dateLayers = printMapViewModel.PrinterOverlay.PrinterLayers.OfType<DatePrinterLayer>();
            foreach (var layer in dateLayers)
            {
                string currentDate = DateTime.Now.ToString(layer.DateFormat);
                layer.DateString = currentDate;
            }
            var projectLayers = printMapViewModel.PrinterOverlay.PrinterLayers.OfType<ProjectPathPrinterLayer>();
            foreach (var layer in projectLayers)
            {
                string projectPath = layer.ProjectPath;
                Uri uri = GisEditor.ProjectManager.ProjectUri;
                if (!File.Exists(projectPath) && File.Exists(uri.LocalPath))
                {
                    layer.ProjectPath = uri.LocalPath;
                }
            }

            if (mapLayers.Count > 0)
            {
                foreach (var mapLayer in mapLayers)
                {
                    mapLayer.MapImageCache = null;
                }
                printMapViewModel.PrinterOverlay.Refresh();
            }
        }

        [Obfuscation]
        private void AnnotationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            List<SimplifyMapPrinterLayer> mapLayers = printMapViewModel.PrinterOverlay.PrinterLayers.Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer)).OfType<SimplifyMapPrinterLayer>().ToList();

            foreach (var item in mapLayers)
            {
                var layer1 = item.Layers.FirstOrDefault(y => y.Name == "AnnotationLayer");
                if (layer1 != null)
                {
                    item.Layers.Remove(layer1);
                }
            }

            if (mapLayers.Count > 0)
            {
                foreach (var mapLayer in mapLayers)
                {
                    mapLayer.MapImageCache = null;
                }
                printMapViewModel.PrinterOverlay.Refresh();
            }
        }

        [Obfuscation]
        private void AnnotationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var annotationOverlay = GisEditor.ActiveMap.InteractiveOverlays.OfType<AnnotationTrackInteractiveOverlay>().FirstOrDefault();
            if (annotationOverlay != null && annotationOverlay.TrackShapeLayer.InternalFeatures.Count > 0)
            {
                List<SimplifyMapPrinterLayer> mapLayers = printMapViewModel.PrinterOverlay.PrinterLayers.Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer)).OfType<SimplifyMapPrinterLayer>().ToList();
                if (mapLayers.Count > 0)
                {
                    mapLayers[0].Layers.Add(annotationOverlay.TrackShapeLayer);

                    foreach (var mapLayer in mapLayers)
                    {
                        mapLayer.MapImageCache = null;
                    }
                    printMapViewModel.PrinterOverlay.Refresh();
                }
            }
        }

        [Obfuscation]
        private void GridlinesClick(object sender, RoutedEventArgs e)
        {
            GridlinesSettingsWindow gridlinesSettingsWindow = new GridlinesSettingsWindow(printMapViewModel.PrinterOverlay.GridLayer);
            if (gridlinesSettingsWindow.ShowDialog().GetValueOrDefault())
            {
                if (gridlinesSettingsWindow.ViewModel.ShowGridlines)
                    printMapViewModel.PrinterOverlay.GridLayer = gridlinesSettingsWindow.ViewModel.ToGridlinesPrinterLayer();
                else if (printMapViewModel.PrinterOverlay.GridLayer != null)
                {
                    printMapViewModel.PrinterOverlay.GridLayer.IsVisible = false;
                }
                printMapViewModel.PrinterOverlay.Refresh();
            }
        }

        [Obfuscation]
        private void RenameTextBlock_TextRenamed(object sender, TextRenamedEventArgs e)
        {
            if (e.OldText != e.NewText)
            {
                if (!string.IsNullOrEmpty(e.NewText) && printMapViewModel.Signatures.FirstOrDefault(s => s.Name.Equals(e.NewText, StringComparison.InvariantCultureIgnoreCase)) == null)
                {
                    SignatureViewModel signatureViewModel = sender.GetDataContext<SignatureViewModel>();
                    signatureViewModel.Name = e.NewText;
                }
                else
                {
                    e.IsCancelled = true;
                    System.Windows.Forms.MessageBox.Show("The new name is illegal. Bookmark name cannot be empty or a duplicate.", GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxInfoCaption"));
                }
            }
        }
    }
}