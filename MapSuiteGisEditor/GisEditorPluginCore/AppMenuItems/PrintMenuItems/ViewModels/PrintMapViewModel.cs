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
using Microsoft.Win32;
using PdfSharp;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class PrintMapViewModel : ViewModelBase
    {
        public event EventHandler<RoutedEventArgs> EditingItem;
        public event EventHandler<RoutedEventArgs> RemovingItem;
        public event EventHandler<RoutedEventArgs> SetPosition;

        public static bool CanUsePrint = true;
        private static PrintDialog printDialog;

        private Cursor previousCursor;
        private System.Windows.Point originPosition;
        private bool isDragging;
        private PagePrinterLayer pagePrinterLayer;
        private PrinterLayer copiedPrinterLayer;
        private PrinterLayer selectedPrinterLayer;
        private AdvancedPrinterInteractiveOverlay printerOverlay;
        private PrinterOrientation orientation;
        private PrinterPageSize size;
        private GisEditorWpfMap printMap;
        private SizeUnit selectedSizeUnit;
        private DistanceUnit rightUnit;
        private float width;
        private float height;
        private double dpi;
        private string currentZoom;
        private string currentScale;
        private string busyContent;
        private bool isBusy;
        private bool skipRefresh;
        private bool isPoint;
        private bool isLine;
        private bool isPolygon;
        private bool isCircle;
        private bool isRectangle;
        private bool isSquare;
        private bool isEllipse;
        private bool isSelectMapElement;
        private bool refreshButtonEnabled;
        private AreaStyle defaultAreaStyle;
        private LineStyle defaultLineStyle;
        private PointStyle defaultPointStyle;
        private Action<PrinterLayer> editPrinterLayer;
        private Collection<string> zooms;
        private Dictionary<PrinterPageSize, RectangleShape> sizeBoundingBoxes;
        private ObservableCollection<SignatureViewModel> signatures;
        private SignatureViewModel selectedSignature;
        private ObservedCommand setScaleCommand;

        [NonSerialized]
        private RelayCommand toBitmapCommand;
        [NonSerialized]
        private RelayCommand toPdfCommand;
        [NonSerialized]
        private RelayCommand printCommand;
        [NonSerialized]
        private RelayCommand printPreviewCommand;
        [NonSerialized]
        private RelayCommand loadLayoutCommand;
        [NonSerialized]
        private RelayCommand loadLayoutFromCurrentMapCommand;
        [NonSerialized]
        private RelayCommand saveLayoutCommand;
        [NonSerialized]
        private RelayCommand newLayoutCommand;
        private ObservedCommand newLayoutFromTemplateCommand;
        private ObservedCommand deleteSignatureCommand;
        private RelayCommand newSignatureCommand;
        private ObservedCommand renameSignatureCommand;
        private ObservedCommand applySignatureCommand;

        public PrintMapViewModel()
        {
            pagePrinterLayer = new PagePrinterLayer(PrinterPageSize.AnsiA, PrinterOrientation.Landscape) { DrawingExceptionMode = DrawingExceptionMode.DrawException };
            InitializeMap();
            isBusy = false;
            signatures = new ObservableCollection<SignatureViewModel>();
            //signatures.Add("test", null);
            busyContent = GisEditor.LanguageManager.GetStringResource("PrintPrintingLabel");
            orientation = PrinterOrientation.Landscape;
            size = PrinterPageSize.AnsiA;
            dpi = 96;
            zooms = new Collection<string>();
            selectedSizeUnit = SizeUnit.Inches;
            foreach (ZoomLevel level in PrintMap.ZoomLevelSet.GetZoomLevels())
            {
                Zooms.Add(((PrinterZoomLevelSet)PrintMap.ZoomLevelSet).GetZoomPercentage(level) + "%");
            }
            sizeBoundingBoxes = new Dictionary<PrinterPageSize, RectangleShape>();
            foreach (PrinterPageSize item in Enum.GetValues(typeof(PrinterPageSize)))
            {
                if (item != PrinterPageSize.Custom) sizeBoundingBoxes.Add(item, GetPageBoundingBox(PrintingUnit.Inch, item));
            }
            defaultAreaStyle = new AreaStyle(new GeoPen(GeoColor.StandardColors.Black), new GeoSolidBrush(GeoColor.StandardColors.White)) { Name = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginAreaHeader") };
            defaultLineStyle = new LineStyle(new GeoPen(GeoColor.StandardColors.Black)) { Name = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginLineHeader") };
            defaultPointStyle = new PointStyle(PointSymbolType.Circle, new GeoSolidBrush(GeoColor.StandardColors.White), new GeoPen(GeoColor.StandardColors.Black), 8) { Name = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginPointHeader") };
        }

        public string CurrentScale
        {
            get { return currentScale; }
            set
            {
                currentScale = value;
                RaisePropertyChanged(() => CurrentScale);
            }
        }

        public DistanceUnit RightUnit
        {
            get { return rightUnit; }
            set
            {
                rightUnit = value;
                RaisePropertyChanged(() => RightUnit);
            }
        }

        public ObservedCommand SetScaleCommand
        {
            get
            {
                if (setScaleCommand == null)
                {
                    setScaleCommand = new ObservedCommand(() =>
                    {
                        double scale = double.Parse(CurrentScale);
                        double leftScale = Conversion.ConvertMeasureUnits(1, DistanceUnit.Inch, DistanceUnit.Meter);
                        double rightScale = Conversion.ConvertMeasureUnits(scale, RightUnit, DistanceUnit.Meter);
                        double zoomToScale = rightScale / leftScale;
                        ZoomLevel zoomLevel = new ZoomLevel(Math.Round(zoomToScale, 6));
                        PrintMap.ZoomLevelSet.CustomZoomLevels.Clear();
                        List<ZoomLevel> zoomLevels = PrintMap.ZoomLevelSet.GetZoomLevels().ToList();
                        foreach (var item in zoomLevels)
                        {
                            item.Scale = Math.Round(item.Scale, 6);
                            if (item.Scale < zoomToScale && !PrintMap.ZoomLevelSet.CustomZoomLevels.Contains(zoomLevel))
                            {
                                PrintMap.ZoomLevelSet.CustomZoomLevels.Add(zoomLevel);
                            }
                            PrintMap.ZoomLevelSet.CustomZoomLevels.Add(item);
                        }
                        PrintMap.CurrentScale = zoomToScale;
                        PrintMap.Refresh();
                    },
                    () =>
                    {
                        double result = 0;
                        return double.TryParse(CurrentScale, out result);
                    });
                }
                return setScaleCommand;
            }
        }

        public PrinterLayer SelectedPrinterLayer
        {
            get { return selectedPrinterLayer; }
        }

        public AdvancedPrinterInteractiveOverlay PrinterOverlay
        {
            get { return printerOverlay; }
            set
            {
                printerOverlay = value;
                size = pagePrinterLayer.PageSize;
                selectedSizeUnit = SizeUnit.Pixels;
                if (size == PrinterPageSize.Custom)
                {
                    Width = pagePrinterLayer.CustomWidth;
                    Height = pagePrinterLayer.CustomHeight;
                }
                orientation = pagePrinterLayer.Orientation;
                RaisePropertyChanged(() => SelectedSizeUnit);
                RaisePropertyChanged(() => Size);
                RaisePropertyChanged(() => Orientation);
                RaisePropertyChanged(() => CustomSizePanelVisibility);
                RaisePropertyChanged(() => DpiPanelVisibility);
            }
        }

        public Collection<string> Zooms
        {
            get { return zooms; }
        }

        public PrinterPageSize Size
        {
            get { return size; }
            set
            {
                size = value;
                RaisePropertyChanged(() => Size);
                RaisePropertyChanged(() => CustomSizePanelVisibility);
                RaisePropertyChanged(() => DpiPanelVisibility);

                if (size == PrinterPageSize.Custom)
                {
                    float customHeight = 0;
                    float customWidth = 0;
                    float tmpWidth = 0;
                    float tmpHeight = 0;

                    if (selectedSizeUnit == SizeUnit.Cm)
                    {
                        var rectangleInCM = pagePrinterLayer.GetPosition(PrintingUnit.Centimeter);
                        customHeight = (float)PrinterHelper.ConvertLength(rectangleInCM.Height, PrintingUnit.Centimeter, PrintingUnit.Point);
                        customWidth = (float)PrinterHelper.ConvertLength(rectangleInCM.Width, PrintingUnit.Centimeter, PrintingUnit.Point);
                        tmpWidth = (float)rectangleInCM.Width;
                        tmpHeight = (float)rectangleInCM.Height;
                    }
                    else if (selectedSizeUnit == SizeUnit.Inches)
                    {
                        var rectangleInInch = pagePrinterLayer.GetPosition(PrintingUnit.Inch);
                        customHeight = (float)(dpi * rectangleInInch.Height);
                        customWidth = (float)(dpi * rectangleInInch.Width);
                        tmpWidth = (float)rectangleInInch.Width;
                        tmpHeight = (float)rectangleInInch.Height;
                    }
                    else
                    {
                        var rectangleInPoint = pagePrinterLayer.GetPosition(PrintingUnit.Point);
                        customHeight = (float)rectangleInPoint.Height;
                        customWidth = (float)rectangleInPoint.Width;
                        tmpWidth = (float)rectangleInPoint.Width;
                        tmpHeight = (float)rectangleInPoint.Height;
                    }

                    if (orientation == PrinterOrientation.Landscape)
                    {
                        width = tmpHeight;
                        height = tmpWidth;
                        pagePrinterLayer.CustomHeight = customWidth;
                        pagePrinterLayer.CustomWidth = customHeight;
                    }
                    else
                    {
                        width = tmpWidth;
                        height = tmpHeight;
                        pagePrinterLayer.CustomHeight = customHeight;
                        pagePrinterLayer.CustomWidth = customWidth;
                    }

                    pagePrinterLayer.PageSize = Size;
                    RaisePropertyChanged(() => Width);
                    RaisePropertyChanged(() => Height);
                }
                else
                {
                    pagePrinterLayer.PageSize = Size;
                    PrintMap.CurrentExtent = pagePrinterLayer.GetBoundingBox();
                }
                ChangeScaleBarLineWidth();
                PrintMap.Refresh();
            }
        }

        public PrinterOrientation Orientation
        {
            get { return orientation; }
            set
            {
                var oldOrientation = orientation;
                orientation = value;
                RaisePropertyChanged(() => Orientation);

                pagePrinterLayer.Orientation = Orientation;
                if (orientation != oldOrientation)
                {
                    // clear control points.
                    printerOverlay.IsEditable = !printerOverlay.IsEditable;
                    printerOverlay.IsEditable = !printerOverlay.IsEditable;

                    var mapPrinterLayers = printerOverlay.PrinterLayers.OfType<MapPrinterLayer>().Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer));
                    mapPrinterLayers.ForEach(l =>
                    {
                        var position = l.GetPosition();
                        var halfWidth = position.Width * .5;
                        var halfHeight = position.Height * .5;
                        var center = position.GetCenterPoint();
                        var newPosition = new RectangleShape(center.X - halfHeight, center.Y + halfWidth, center.X + halfHeight, center.Y - halfWidth);
                        ResetScalePosition(l, position, center, newPosition);
                    });
                }

                PrintMap.Refresh();
            }
        }

        public float Width
        {
            get { return (float)Math.Round(width, 2); }
            set
            {
                width = value;
                RaisePropertyChanged(() => Width);
                bool needConvert = selectedSizeUnit == SizeUnit.Inches || selectedSizeUnit == SizeUnit.Cm;
                pagePrinterLayer.CustomWidth = needConvert ? (float)(Width * dpi) : Width;
                PrintMap.Refresh();
            }
        }

        public float Height
        {
            get { return (float)Math.Round(height, 2); }
            set
            {
                height = value;
                RaisePropertyChanged(() => Height);

                bool needConvert = selectedSizeUnit == SizeUnit.Inches || selectedSizeUnit == SizeUnit.Cm;
                pagePrinterLayer.CustomHeight = needConvert ? (float)(Height * dpi) : Height;
                PrintMap.Refresh();
            }
        }

        public bool CustomSizePanelVisibility
        {
            get { return size == PrinterPageSize.Custom; }
        }

        public bool DpiPanelVisibility
        {
            get { return selectedSizeUnit != SizeUnit.Pixels && size == PrinterPageSize.Custom; }
        }

        public GisEditorWpfMap PrintMap
        {
            get { return printMap; }
            set
            {
                printMap = value;
                RaisePropertyChanged(() => PrintMap);
            }
        }

        public ObservableCollection<SignatureViewModel> Signatures
        {
            get { return signatures; }
        }

        public SignatureViewModel SelectedSignature
        {
            get { return selectedSignature; }
            set
            {
                selectedSignature = value;
                RaisePropertyChanged(() => SelectedSignature);
            }
        }

        public string CurrentZoom
        {
            get { return currentZoom; }
            set
            {
                currentZoom = value;
                RaisePropertyChanged(() => CurrentZoom);

                if (!skipRefresh)
                {
                    double zoomStr = Double.Parse(currentZoom.Replace("%", ""));
                    PrintMap.CurrentScale = PrinterHelper.GetPointsPerGeographyUnit(PrintMap.MapUnit) / (zoomStr / 100);
                    PrintMap.Refresh();
                }

                skipRefresh = false;
            }
        }

        public SizeUnit SelectedSizeUnit
        {
            get { return selectedSizeUnit; }
            set
            {
                selectedSizeUnit = value;
                if (selectedSizeUnit == SizeUnit.Cm) Dpi = 37.8;
                else if (selectedSizeUnit == SizeUnit.Inches) Dpi = 96;
                var tmpHeight = height;
                var tmpWidth = width;
                Height = tmpHeight;
                Width = tmpWidth;

                RaisePropertyChanged(() => SelectedSizeUnit);
                RaisePropertyChanged(() => DpiPanelVisibility);
            }
        }

        public double Dpi
        {
            get { return dpi; }
            set
            {
                dpi = value;
                RaisePropertyChanged(() => Dpi);
            }
        }

        public string BusyContent
        {
            get { return busyContent; }
            set
            {
                busyContent = value;
                RaisePropertyChanged(() => BusyContent);
            }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                RaisePropertyChanged(() => IsBusy);
                RaisePropertyChanged(() => IsEnabled);
            }
        }

        public bool IsEnabled
        {
            get { return !isBusy; }
        }

        public bool RefreshButtonEnabled
        {
            get { return refreshButtonEnabled; }
            set
            {
                refreshButtonEnabled = value;
                RaisePropertyChanged(() => RefreshButtonEnabled);
            }
        }

        public Dictionary<PrinterPageSize, RectangleShape> SizeBoundingBoxes
        {
            get { return sizeBoundingBoxes; }
        }

        public Action<PrinterLayer> EditPrinterLayer
        {
            get { return editPrinterLayer; }
            set { editPrinterLayer = value; }
        }

        public bool IsPoint
        {
            get { return isPoint; }
            set
            {
                isPoint = value;
                RaisePropertyChanged(() => IsPoint);
            }
        }

        public bool IsLine
        {
            get { return isLine; }
            set
            {
                isLine = value;
                RaisePropertyChanged(() => IsLine);
            }
        }

        public bool IsPolygon
        {
            get { return isPolygon; }
            set
            {
                isPolygon = value;
                RaisePropertyChanged(() => IsPolygon);
            }
        }

        public bool IsCircle
        {
            get { return isCircle; }
            set
            {
                isCircle = value;
                RaisePropertyChanged(() => IsCircle);
            }
        }

        public bool IsRectangle
        {
            get { return isRectangle; }
            set
            {
                isRectangle = value;
                RaisePropertyChanged(() => IsRectangle);
            }
        }

        public bool IsSquare
        {
            get { return isSquare; }
            set
            {
                isSquare = value;
                RaisePropertyChanged(() => IsSquare);
            }
        }

        public bool IsEllipse
        {
            get { return isEllipse; }
            set
            {
                isEllipse = value;
                RaisePropertyChanged(() => IsEllipse);
            }
        }

        public bool IsSelectMapElement
        {
            get { return isSelectMapElement; }
            set
            {
                isSelectMapElement = value;
                RaisePropertyChanged(() => IsSelectMapElement);
            }
        }

        public bool AutoSelectPageSize
        {
            get { return AppMenuUIPlugin.AutoSelectPageSize; }
            set
            {
                AppMenuUIPlugin.AutoSelectPageSize = value;
                RaisePropertyChanged(() => AutoSelectPageSize);
            }
        }

        public RelayCommand ToBitmapCommand
        {
            get
            {
                if (toBitmapCommand == null)
                {
                    toBitmapCommand = new RelayCommand(() =>
                    {
                        if (CheckAllMapElementsHasLayers())
                        {
                            Bitmap bitmap = null;
                            try
                            {
                                BusyContent = GisEditor.LanguageManager.GetStringResource("PrintProcessingLabel");
                                IsBusy = true;
                                Task.Factory.StartNew(new Action(() =>
                                {
                                    bitmap = PrinterLayerHelper.GetPreviewBitmap(PrintMap);
                                }));
                                while (bitmap == null) { System.Windows.Forms.Application.DoEvents(); }
                                IsBusy = false;
                                SaveFileDialog saveFileDialog = new SaveFileDialog();
                                saveFileDialog.Filter = "Png Images(*.png)|*.png";
                                if (saveFileDialog.ShowDialog().GetValueOrDefault())
                                {
                                    bitmap.Save(saveFileDialog.FileName);
                                    Process.Start(saveFileDialog.FileName);
                                    SavePrinterlayerToProject();
                                }
                            }
                            catch (Exception ex)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                                System.Windows.Forms.MessageBox.Show(ex.Message);
                            }
                            finally
                            {
                                if (bitmap != null) bitmap.Dispose();
                            }
                        }
                    });
                }
                return toBitmapCommand;
            }
        }

        public RelayCommand ToPdfCommand
        {
            get
            {
                if (toPdfCommand == null)
                {
                    toPdfCommand = new RelayCommand(() =>
                    {
                        if (CheckAllMapElementsHasLayers())
                        {
                            PdfDocument pdfDocument = null;
                            Bitmap bitmap = null;
                            try
                            {
                                pdfDocument = new PdfDocument();
                                PdfPage pdfPage = pdfDocument.AddPage();
                                pdfPage.Orientation = pagePrinterLayer.Orientation == PrinterOrientation.Portrait ? PageOrientation.Portrait : PageOrientation.Landscape;
                                pdfPage.Size = GetPdfPageSize(pagePrinterLayer.PageSize);

                                PdfGeoCanvas pdfGeoCanvas = new PdfGeoCanvas();
                                pdfGeoCanvas.DrawingExceptionMode = DrawingExceptionMode.DrawException;
                                pdfGeoCanvas.BeginDrawing(pdfPage, pagePrinterLayer.GetBoundingBox(), PrintMap.MapUnit);

                                foreach (var printerLayer in printerOverlay.PrinterLayers.Where(l => !(l is PagePrinterLayer)))
                                {
                                    printerLayer.IsDrawing = true;
                                    SaveAndRestoreMapPrinterLayer(printerLayer, tmpLayer => tmpLayer.Draw(pdfGeoCanvas, new Collection<SimpleCandidate>()));
                                    printerLayer.IsDrawing = false;
                                }

                                pdfGeoCanvas.EndDrawing();
                                SaveFileDialog saveFileDialog = new SaveFileDialog();
                                saveFileDialog.Filter = "Pdf Document(*.pdf)|*.pdf";
                                if (saveFileDialog.ShowDialog().GetValueOrDefault())
                                {
                                    pdfDocument.Save(saveFileDialog.FileName);
                                    Process.Start(saveFileDialog.FileName);
                                }
                            }
                            catch (NotSupportedException ex)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                                System.Windows.Forms.MessageBox.Show(ex.Message, GisEditor.LanguageManager.GetStringResource("PrintNotSupportWarningCaption"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                            }
                            finally
                            {
                                if (pdfDocument != null) pdfDocument.Dispose();
                                if (bitmap != null) bitmap.Dispose();
                            }
                        }
                    });
                }
                return toPdfCommand;
            }
        }

        public RelayCommand PrintCommand
        {
            get
            {
                if (printCommand == null)
                {
                    printCommand = new RelayCommand(() =>
                    {
                        if (CheckAllMapElementsHasLayers())
                        {
                            bool continuePrint = true;
                            SimplifyMapPrinterLayer simplifyMapPrinterLayer = PrinterOverlay.PrinterLayers.Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer)).FirstOrDefault() as SimplifyMapPrinterLayer;
                            if (simplifyMapPrinterLayer != null)
                            {
                                RectangleShape pageBoundingBox = GetPageBoundingBox(PrintingUnit.Inch);
                                RectangleShape mapBoundingBox = simplifyMapPrinterLayer.GetPosition(PrintingUnit.Inch);
                                if (mapBoundingBox.Width > pageBoundingBox.Width || mapBoundingBox.Height > pageBoundingBox.Height)
                                {
                                    continuePrint = System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("PrintMapWindowmapIsLargerThanLabel"), GisEditor.LanguageManager.GetStringResource("PrintMapWindowPageSizeText"), System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes;
                                }
                            }
                            if (continuePrint)
                            {
                                if (printDialog == null) printDialog = new PrintDialog();
                                BusyContent = GisEditor.LanguageManager.GetStringResource("PrintPrintingLabel");
                                IsBusy = true;
                                Task.Factory.StartNew(new Action(() =>
                                {
                                    bool hasException = false;
                                    Bitmap bitmap = null;
                                    try
                                    {
                                        printDialog.PrintTicket.PageOrientation = (pagePrinterLayer.Orientation == PrinterOrientation.Portrait)
                                            ? System.Printing.PageOrientation.Portrait : System.Printing.PageOrientation.Landscape;

                                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                        {
                                            try
                                            {
                                                PrinterGeoCanvas printerGeoCanvas = new PrinterGeoCanvas();

                                                System.Windows.Forms.PrintDialog printWindowDialog = new System.Windows.Forms.PrintDialog();
                                                PrintDocument printDocument = PrinterLayerHelper.GetPrintDocument(PrintMap, printerGeoCanvas);
                                                printWindowDialog.Document = printDocument;
                                                if (printWindowDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                                {
                                                    printDocument.DefaultPageSettings.PaperSize = PrinterLayerHelper.GetPrintPreviewPaperSize(pagePrinterLayer);
                                                    printDocument.PrinterSettings.DefaultPageSettings.PaperSize = PrinterLayerHelper.GetPrintPreviewPaperSize(pagePrinterLayer);
                                                    printerGeoCanvas.EndDrawing();
                                                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                                    {
                                                        var currentWindow = Application.Current.Windows.OfType<PrintMapWindow>().FirstOrDefault();
                                                        if (currentWindow != null && !hasException)
                                                        {
                                                            currentWindow.Close();
                                                        }
                                                    }));
                                                    SavePrinterlayerToProject();
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                                                hasException = true;
                                                System.Windows.Forms.MessageBox.Show(ex.Message, GisEditor.LanguageManager.GetStringResource("PrintErrorWarningCaption"));
                                            }
                                        }));
                                    }
                                    catch (Exception ex)
                                    {
                                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                                        hasException = true;
                                        System.Windows.Forms.MessageBox.Show(ex.Message, GisEditor.LanguageManager.GetStringResource("PrintErrorWarningCaption"));
                                    }
                                    finally
                                    {
                                        IsBusy = false;
                                        if (bitmap != null) bitmap.Dispose();
                                    }
                                }));
                            }
                        }
                    });
                }
                return printCommand;
            }
        }

        public RelayCommand PrintPreviewCommand
        {
            get
            {
                if (printPreviewCommand == null)
                {
                    printPreviewCommand = new RelayCommand(() =>
                    {
                        if (CheckAllMapElementsHasLayers())
                        {
                            try
                            {
                                SavePrinterlayerToProject();
                                PrinterGeoCanvas printerGeoCanvas = new PrinterGeoCanvas();
                                System.Windows.Forms.PrintPreviewDialog printPreviewDialog = new System.Windows.Forms.PrintPreviewDialog();
                                HidePagerButton(printPreviewDialog);
                                printPreviewDialog.Document = PrinterLayerHelper.GetPrintDocument(PrintMap, printerGeoCanvas);
                                printPreviewDialog.ShowIcon = false;
                                printPreviewDialog.ShowDialog();
                            }
                            catch (Exception ex)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                                MessageBox.Show(ex.Message, "Error");
                            }
                        }
                    });
                }
                return printPreviewCommand;
            }
        }

        public RelayCommand LoadLayoutFromCurrentMapCommand
        {
            get
            {
                if (loadLayoutFromCurrentMapCommand == null)
                {
                    loadLayoutFromCurrentMapCommand = new RelayCommand(() =>
                    {
                        if (System.Windows.Forms.DialogResult.Yes == System.Windows.Forms.MessageBox.Show(@"This will rebuild the print layout based up on your current map.  If you have added any custom print layout elements such as logo�s, text or drawing�s these will be lost unless you save your current print layout to a file first. Would you like to continue?", "Load Layout From Map", System.Windows.Forms.MessageBoxButtons.YesNo))
                        {
                            ClearAllPrinterLayers();
                            LoadPrinterLayersFromActiveMap();
                            if (selectedSignature != null)
                            {
                                ApplySignature();
                            }
                            PrintMap.Refresh();
                        }
                    });
                }
                return loadLayoutFromCurrentMapCommand;
            }
        }

        public RelayCommand LoadLayoutCommand
        {
            get
            {
                if (loadLayoutCommand == null)
                {
                    loadLayoutCommand = new RelayCommand(() =>
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog { Title = GisEditor.LanguageManager.GetStringResource("PrintLoadPageLayoutLabel"), Filter = "Layout File(*.tglayt)|*.tglayt" };
                        if (openFileDialog.ShowDialog().GetValueOrDefault())
                        {
                            BusyContent = GisEditor.LanguageManager.GetStringResource("PrintLoadingLabel");
                            LoadLayout(openFileDialog.FileName);
                        }
                    });
                }
                return loadLayoutCommand;
            }
        }

        public RelayCommand SaveLayoutCommand
        {
            get
            {
                if (saveLayoutCommand == null)
                {
                    saveLayoutCommand = new RelayCommand(() =>
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog { Title = GisEditor.LanguageManager.GetStringResource("PrintSavePageLayoutLabel"), Filter = "Layout File(*.tglayt)|*.tglayt" };
                        if (saveFileDialog.ShowDialog().GetValueOrDefault())
                        {
                            BusyContent = GisEditor.LanguageManager.GetStringResource("PrintSavingLabel");
                            IsBusy = true;
                            Task.Factory.StartNew(new Action(() =>
                            {
                                SavePrinterLayerLayout(saveFileDialog.FileName);
                                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    IsBusy = false;
                                }));
                            }));
                        }
                    });
                }
                return saveLayoutCommand;
            }
        }

        public RelayCommand NewLayoutCommand
        {
            get
            {
                if (newLayoutCommand == null)
                {
                    newLayoutCommand = new RelayCommand(() =>
                    {
                        ClearAllPrinterLayers();
                        if (selectedSignature != null)
                        {
                            ApplySignature();
                        }
                        PrintMap.Refresh();
                    });
                }
                return newLayoutCommand;
            }
        }

        public ObservedCommand NewLayoutFromTemplateCommand
        {
            get
            {
                if (newLayoutFromTemplateCommand == null)
                {
                    newLayoutFromTemplateCommand = new ObservedCommand(() =>
                    {
                        var newLayoutFromTemplateWindow = new NewLayoutFromTemplateWindow(SizeBoundingBoxes);
                        if (newLayoutFromTemplateWindow.ShowDialog().GetValueOrDefault())
                        {
                            IsBusy = true;
                            ClearAllPrinterLayers();
                            NewLayoutViewModel newLayoutEntity = newLayoutFromTemplateWindow.DataContext as NewLayoutViewModel;
                            LoadLayoutFromTemplate(newLayoutEntity.SelectedLayout);
                            orientation = newLayoutEntity.SelectedLayout.Orientation == FilterPageOrientation.Landscape ? PrinterOrientation.Landscape : PrinterOrientation.Portrait;
                            RaisePropertyChanged(() => Orientation);
                            pagePrinterLayer.Orientation = orientation;
                            if (selectedSignature != null)
                            {
                                ApplySignature();
                            }
                            Size = newLayoutEntity.SelectedLayout.PageSize;
                            IsBusy = false;
                        }
                    },
                    () => GisEditor.ActiveMap != null);
                }
                return newLayoutFromTemplateCommand;
            }
        }

        public ObservedCommand DeleteSignatureCommand
        {
            get
            {
                if (deleteSignatureCommand == null)
                {
                    deleteSignatureCommand = new ObservedCommand(() =>
                    {
                        if (System.Windows.Forms.MessageBox.Show("Are you sure you want to delete the selected signature? The page using this signature will no longer have a signature.", "Delete Signature", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                        {
                            if (printerOverlay.PrinterLayers.Contains(selectedSignature.SignaturePrinterLayer))
                            {
                                printerOverlay.PrinterLayers.Remove(selectedSignature.SignaturePrinterLayer);
                                printMap.Refresh();
                            }
                            signatures.Remove(selectedSignature);
                            if (signatures.Count > 0)
                            {
                                SelectedSignature = signatures[0];
                            }
                        }

                    }, () => selectedSignature != null);
                }
                return deleteSignatureCommand;
            }
        }

        public RelayCommand NewSignatureCommand
        {
            get
            {
                if (newSignatureCommand == null)
                {
                    newSignatureCommand = new RelayCommand(() =>
                    {
                        TextElementWindow textElementWindow = new TextElementWindow(LabelMode.Signature);
                        if (textElementWindow.ShowDialog().GetValueOrDefault())
                        {
                            if (signatures.FirstOrDefault(s => s.Name.Equals(textElementWindow.SignatureName, StringComparison.InvariantCultureIgnoreCase)) == null)
                            {
                                LabelPrinterLayer labelPrinterLayer = new LabelPrinterLayer();
                                labelPrinterLayer.DrawingExceptionMode = DrawingExceptionMode.DrawException;
                                labelPrinterLayer.LoadFromViewModel((TextElementViewModel)textElementWindow.DataContext);

                                SignatureViewModel signatureViewModel = new SignatureViewModel(textElementWindow.SignatureName, labelPrinterLayer);
                                signatures.Add(signatureViewModel);
                            }
                            else
                            {
                                System.Windows.Forms.MessageBox.Show("A duplicate signature name is not allowed, please try another one.", "Duplicate Signature Name");
                            }
                        }
                    });
                }
                return newSignatureCommand;
            }
        }

        public ObservedCommand RenameSignatureCommand
        {
            get
            {
                if (renameSignatureCommand == null)
                {
                    renameSignatureCommand = new ObservedCommand(() =>
                    {
                        selectedSignature.IsRenaming = true;
                    }, () => selectedSignature != null);
                }
                return renameSignatureCommand;
            }
        }

        public ObservedCommand ApplySignatureCommand
        {
            get
            {
                if (applySignatureCommand == null)
                {
                    applySignatureCommand = new ObservedCommand(() =>
                    {
                        ApplySignature();
                        printMap.Refresh();

                    }, () => selectedSignature != null);
                }
                return applySignatureCommand;
            }
        }

        private void ApplySignature()
        {
            foreach (var item in signatures)
            {
                printerOverlay.PrinterLayers.Remove(item.SignaturePrinterLayer);
            }

            selectedSignature.SignaturePrinterLayer.Open();

            var newScreenBBox = new PlatformGeoCanvas().MeasureText(selectedSignature.SignaturePrinterLayer.Text, selectedSignature.SignaturePrinterLayer.Font);
            var scaledWidth = newScreenBBox.Width * 1.1;
            var scaledHeight = newScreenBBox.Height * 1.1;

            double width = PrinterHelper.ConvertLength(scaledWidth, PrintingUnit.Point, PrintingUnit.Inch);
            double height = PrinterHelper.ConvertLength(scaledHeight, PrintingUnit.Point, PrintingUnit.Inch);
            var boundingBox = pagePrinterLayer.GetPosition(PrintingUnit.Inch);
            selectedSignature.SignaturePrinterLayer.SetPosition(width, height, boundingBox.UpperRightPoint.X - width * 0.5, boundingBox.LowerLeftPoint.Y + height * 0.5, PrintingUnit.Inch);
            printerOverlay.PrinterLayers.Add(selectedSignature.SignaturePrinterLayer);
            var centerPoint = selectedSignature.SignaturePrinterLayer.GetBoundingBox().GetCenterPoint();
        }

        public void SetUpMap()
        {
            PrintMap.CurrentScaleChanged += (s, e) =>
            {
                if (PrintMap.ActualWidth != 0)
                {
                    PrinterZoomLevelSet printerZoomLevelSet = (PrinterZoomLevelSet)PrintMap.ZoomLevelSet;
                    ZoomLevel currentZoomLevel = printerZoomLevelSet.GetZoomLevel(PrintMap.CurrentExtent, PrintMap.ActualWidth, PrintMap.MapUnit);

                    skipRefresh = true;
                    CurrentZoom = (int)printerZoomLevelSet.GetZoomPercentage(currentZoomLevel) + "%";
                }
            };

            if (pagePrinterLayer != null) PrintMap.CurrentExtent = pagePrinterLayer.GetBoundingBox();
        }

        public void LoadLayoutFromTemplate(LayoutViewModel newLayoutEntity)
        {
            var mapPrinterLayer = newLayoutEntity.PrinterLayers.OfType<SimplifyMapPrinterLayer>().FirstOrDefault();
            foreach (var printerLayer in newLayoutEntity.PrinterLayers)
            {
                PrinterLayerAdapter.Create(printerLayer, mapPrinterLayer).LoadFromActiveMap(printerLayer);
                printerOverlay.PrinterLayers.Add(printerLayer);
            }
        }

        public void LoadPrinterLayersFromActiveMap()
        {
            var pageBoundingBox = GetPageBoundingBox(PrintingUnit.Inch, Size, Orientation);
            var mapPrinterLayer = new MapPrinterLayerAdapter(GisEditor.ActiveMap, true).GetPrinterLayerFromActiveMap(pageBoundingBox) as SimplifyMapPrinterLayer;
            mapPrinterLayer.DrawingMode = MapPrinterDrawingMode.Raster;
            mapPrinterLayer.Open();
            var mapBoudingBox = mapPrinterLayer.GetPosition(PrintingUnit.Inch);
            AddToPrinterOverlayIfNotNull(mapPrinterLayer);

            var logo = GisEditor.ActiveMap.MapTools.OfType<AdornmentLogo>().FirstOrDefault();
            AddToPrinterOverlayIfNotNull(new ImagePrinterLayerAdapter(logo).GetPrinterLayerFromActiveMap(mapBoudingBox));

            var northArrow = GisEditor.ActiveMap.MapTools.OfType<NorthArrowMapTool>().FirstOrDefault();
            AddToPrinterOverlayIfNotNull(new ImagePrinterLayerAdapter(northArrow).GetPrinterLayerFromActiveMap(mapBoudingBox));

            foreach (var item in GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.OfType<TitleAdornmentLayer>())
            {
                AddToPrinterOverlayIfNotNull(new LabelPrinterLayerAdapter(item).GetPrinterLayerFromActiveMap(mapBoudingBox));
            }

            foreach (var item in GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.OfType<LegendManagerAdornmentLayer>().SelectMany(l => l.LegendLayers.Select(ll => ll.ToLegendAdornmentLayer())))
            {
                AddToPrinterOverlayIfNotNull(new LegendPrinterLayerAdapter(item).GetPrinterLayerFromActiveMap(mapBoudingBox));
            }

            foreach (var item in GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.OfType<ScaleBarAdornmentLayer>())
            {
                AddToPrinterOverlayIfNotNull(new ScaleBarPrinterLayerAdapter(mapPrinterLayer, item).GetPrinterLayerFromActiveMap(mapBoudingBox));
            }

            if (!(mapPrinterLayer.MapUnit == GeographyUnit.DecimalDegree && !PrinterLayerHelper.CheckDecimalDegreeIsInRange(mapPrinterLayer.MapExtent)))
            {
                foreach (var item in GisEditor.ActiveMap.FixedAdornmentOverlay.Layers.OfType<ScaleLineAdornmentLayer>())
                {
                    var scaleLinePrinterLayer = new ScaleLinePrinterLayerAdapter(mapPrinterLayer, item).GetPrinterLayerFromActiveMap(mapBoudingBox) as ScaleLinePrinterLayer;
                    if (scaleLinePrinterLayer != null && ScaleLineElementViewModel.IsValid(scaleLinePrinterLayer))
                    {
                        AddToPrinterOverlayIfNotNull(scaleLinePrinterLayer);
                    }
                }
            }
        }

        public void LoadLayout(string layout, bool loadFromFile = true)
        {
            GeoImage mapImageCache = new GeoImage(new MemoryStream(BoundingBoxSelectorMapTool.GetCroppedMapPreviewImage(GisEditor.ActiveMap, new System.Windows.Int32Rect(0, 0, (int)GisEditor.ActiveMap.RenderSize.Width, (int)GisEditor.ActiveMap.RenderSize.Height))));
            IsBusy = true;
            Task.Factory.StartNew(new Action(() =>
            {
                PrintLayersSerializeProxy serializationWrapper = null;
                try
                {
                    if (loadFromFile)
                        serializationWrapper = GisEditor.Serializer.Deserialize(layout, GeoFileReadWriteMode.Read) as PrintLayersSerializeProxy;
                    else
                        serializationWrapper = GisEditor.Serializer.Deserialize(layout) as PrintLayersSerializeProxy;
                    if (serializationWrapper != null)
                    {
                        SimplifyMapPrinterLayer simplifyMapPrinterLayer = serializationWrapper.PrinterLayers.OfType<SimplifyMapPrinterLayer>().FirstOrDefault();
                        if (simplifyMapPrinterLayer != null)
                        {
                            simplifyMapPrinterLayer.MapImageCache = mapImageCache;
                        }

                        RemoveInvalidLayer(serializationWrapper);
                        printerOverlay.PrinterLayers.Clear();
                        pagePrinterLayer = serializationWrapper.PrinterLayers.OfType<PagePrinterLayer>().FirstOrDefault();
                        foreach (var printerLayer in serializationWrapper.PrinterLayers)
                        {
                            printerOverlay.PrinterLayers.Add(printerLayer);
                        }
                        foreach (var mapPrinterLayer in printerOverlay.PrinterLayers.Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer)).OfType<SimplifyMapPrinterLayer>())
                        {
                            mapPrinterLayer.IsDrawing = false;
                            mapPrinterLayer.Layers.Clear();
                            mapPrinterLayer.SetDescriptionLayerBackground();
                        }
                        size = pagePrinterLayer.PageSize;
                        orientation = pagePrinterLayer.Orientation;
                        width = pagePrinterLayer.CustomWidth / (float)dpi;
                        height = pagePrinterLayer.CustomHeight / (float)dpi;

                        if (serializationWrapper.GridPrinterLayer != null)
                        {
                            PrinterOverlay.GridLayer = serializationWrapper.GridPrinterLayer;
                        }

                        RaisePropertyChanged(() => Size);
                        RaisePropertyChanged(() => CustomSizePanelVisibility);
                        RaisePropertyChanged(() => DpiPanelVisibility);
                        RaisePropertyChanged(() => Orientation);
                        RaisePropertyChanged(() => Width);
                        RaisePropertyChanged(() => Height);
                    }
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
                finally
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (!loadFromFile)
                        {
                            var mapElements = PrinterOverlay.PrinterLayers.Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer));
                            var mapPrinterLayerAdapter = new MapPrinterLayerAdapter(GisEditor.ActiveMap, true);
                            foreach (var item in mapElements.OfType<SimplifyMapPrinterLayer>())
                            {
                                mapPrinterLayerAdapter.LoadFromActiveMap(item);
                                item.LastmapExtent = item.MapExtent;
                            }
                        }
                        pagePrinterLayer.SafeProcess(() =>
                        {
                            printMap.CurrentExtent = pagePrinterLayer.GetBoundingBox();
                        });
                        PrintMap.Refresh();
                        IsBusy = false;
                    }));
                }
            }));
        }

        public RectangleShape GetPageBoundingBox(PrintingUnit unit)
        {
            return pagePrinterLayer.GetPosition(unit);
        }

        public PageSize GetPdfPageSize(PrinterPageSize pageSize)
        {
            PageSize pdfPageSize = PageSize.Letter;
            switch (pageSize)
            {
                case PrinterPageSize.AnsiA:
                    pdfPageSize = PageSize.Letter;
                    break;
                case PrinterPageSize.AnsiB:
                    pdfPageSize = PageSize.Ledger;
                    break;
                case PrinterPageSize.AnsiC:
                    pdfPageSize = PageSize.A2;
                    break;
                case PrinterPageSize.AnsiD:
                    pdfPageSize = PageSize.A1;
                    break;
                case PrinterPageSize.AnsiE:
                    pdfPageSize = PageSize.A0;
                    break;
                case PrinterPageSize.Custom:
                    throw new NotSupportedException();
                default:
                    throw new NotSupportedException();
            }
            return pdfPageSize;
        }

        public void ClearAllPrinterLayers()
        {
            var printerLayers = printerOverlay.PrinterLayers.Where(printerLayer => !(printerLayer is PagePrinterLayer)).ToList();

            foreach (var printerLayer in printerLayers)
            {
                printerOverlay.PrinterLayers.Remove(printerLayer);
            }
        }

        private void ChangeScaleBarLineWidth()
        {
            var currentPageRectangle = GetPageBoundingBox(PrintingUnit.Inch);
            if (currentPageRectangle.Width != 0)
            {
                foreach (var item in this.printerOverlay.PrinterLayers.Where(l => l is ScaleBarPrinterLayer || l is ScaleLinePrinterLayer))
                {
                    var currentBoundongBox = item.GetPosition(PrintingUnit.Inch);
                    item.SetPosition(currentPageRectangle.Width * 0.15, currentBoundongBox.Height, currentBoundongBox.GetCenterPoint(), PrintingUnit.Inch);
                }
            }
        }

        private void ResetScalePosition(MapPrinterLayer l, RectangleShape position, PointShape center, RectangleShape newPosition)
        {
            var relatedScaleBars = printerOverlay.PrinterLayers.OfType<ScaleBarPrinterLayer>().Where(scaleBar => scaleBar.MapPrinterLayer == l);
            var relatedScaleLines = printerOverlay.PrinterLayers.OfType<ScaleLinePrinterLayer>().Where(scaleLine => scaleLine.MapPrinterLayer == l);
            foreach (var scale in relatedScaleBars.Cast<PrinterLayer>().Concat(relatedScaleLines))
            {
                var scaleBox = scale.GetPosition();
                var scaleCenter = scaleBox.GetCenterPoint();
                var isLeft = scaleCenter.X < center.X;
                var isTop = scaleCenter.Y > center.Y;

                double offsetX = 0;
                double offsetY = 0;
                if (isLeft) offsetX = scaleBox.UpperLeftPoint.X - position.UpperLeftPoint.X;
                else offsetX = position.LowerRightPoint.X - scaleBox.LowerRightPoint.X;

                if (isTop) offsetY = position.UpperLeftPoint.Y - scaleBox.UpperLeftPoint.Y;
                else offsetY = scaleBox.LowerRightPoint.Y - position.LowerRightPoint.Y;

                double scaleLeft = newPosition.UpperLeftPoint.X + offsetX;
                double scaleTop = newPosition.LowerRightPoint.Y + offsetY + scaleBox.Height;
                double scaleRight = scaleLeft + scaleBox.Width;
                double scaleBottom = scaleTop - scaleBox.Height;

                if (!isLeft)
                {
                    scaleLeft = newPosition.LowerRightPoint.X - offsetX - scaleBox.Width;
                    scaleRight = scaleLeft + scaleBox.Width;
                }

                if (isTop)
                {
                    scaleTop = newPosition.UpperLeftPoint.Y - offsetY;
                    scaleBottom = scaleTop - scaleBox.Height;
                }

                var newScaleBox = new RectangleShape(scaleLeft, scaleTop, scaleRight, scaleBottom);
                scale.SetPosition(newScaleBox);
            }

            l.SetPosition(newPosition);
        }

        private static void HidePagerButton(System.Windows.Forms.PrintPreviewDialog printPreviewDialog)
        {
            try
            {
                printPreviewDialog.Controls[1].Controls[0].Visible = false;
                var toolStrip = (System.Windows.Forms.ToolStrip)printPreviewDialog.Controls[1];
                new int[] { 3, 4, 5, 6, 7, 11 }.ForEach(i => toolStrip.Items[i].Visible = false);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
        }

        private void AddToPrinterOverlayIfNotNull(PrinterLayer printerLayer)
        {
            if (printerLayer != null) printerOverlay.PrinterLayers.Add(printerLayer);
        }

        private void SaveAndRestoreMapPrinterLayer(PrinterLayer printerLayer, Action<PrinterLayer> doWork)
        {
            MapPrinterLayer mapPrinterLayer = printerLayer as MapPrinterLayer;
            SimplifyMapPrinterLayer simplifyMapPrinterLayer = printerLayer as SimplifyMapPrinterLayer;
            GeoImage previousImageCache = null;
            if (simplifyMapPrinterLayer != null) previousImageCache = simplifyMapPrinterLayer.MapImageCache;
            if (doWork != null) doWork(printerLayer);
            if (simplifyMapPrinterLayer != null) simplifyMapPrinterLayer.MapImageCache = previousImageCache;
        }

        private void RemoveInvalidLayer(PrintLayersSerializeProxy wrapper)
        {
            var mapLayer = wrapper.PrinterLayers.OfType<MapPrinterLayer>().FirstOrDefault();
            if (mapLayer != null)
            {
                mapLayer.IsDrawing = true;
                var invalidShpLayers = mapLayer.Layers.OfType<ShapeFileFeatureLayer>().Where(l => !File.Exists(l.ShapePathFilename)).ToArray();
                foreach (var item in invalidShpLayers)
                {
                    System.Windows.Forms.MessageBox.Show(string.Format(CultureInfo.InvariantCulture, GisEditor.LanguageManager.GetStringResource("InvalidLayerAlert"), item.ShapePathFilename), GisEditor.LanguageManager.GetStringResource("DataNotFoundlAlertTitle"));
                    mapLayer.Layers.Remove(item);
                }
            }

            PlatformGeoCanvas gdiPlusGeoCanvas = new PlatformGeoCanvas();

            var invalidImgLayers = wrapper.PrinterLayers.OfType<ImagePrinterLayer>().Where(imgLayer =>
            {
                string imagePathFilename = imgLayer.Image.PathFilename;
                if (string.IsNullOrEmpty(imagePathFilename))
                    return imgLayer.Image.GetImageStream() == null;
                else
                    return !File.Exists(imagePathFilename);
            }).ToArray();

            foreach (var item in invalidImgLayers)
            {
                string imagePathFilename = item.Image.PathFilename;
                System.Windows.Forms.MessageBox.Show(string.Format(CultureInfo.InvariantCulture, GisEditor.LanguageManager.GetStringResource("InvalidLayerAlert"), imagePathFilename), GisEditor.LanguageManager.GetStringResource("DataNotFoundlAlertTitle"));
                wrapper.PrinterLayers.Remove(item);
            }
        }

        public RectangleShape GetPageBoundingBox(PrintingUnit unit, PrinterPageSize pageSize, PrinterOrientation orientation = PrinterOrientation.Portrait)
        {
            var backupOrientation = pagePrinterLayer.Orientation;
            pagePrinterLayer.Orientation = orientation;
            var backUpSize = pagePrinterLayer.PageSize;
            pagePrinterLayer.PageSize = pageSize;
            var result = pagePrinterLayer.GetPosition(unit);
            pagePrinterLayer.Orientation = backupOrientation;
            pagePrinterLayer.PageSize = backUpSize;
            return result;
        }

        private void InitializeMap()
        {
            PrintMap = new GisEditorWpfMap();
            PrintMap.IsMapLoaded = true;
            PrintMap.TrackOverlay.TrackMode = TrackMode.None;
            PrintMap.TrackOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = new AreaStyle(new GeoPen(GeoColor.StandardColors.Black), new GeoSolidBrush(GeoColor.StandardColors.White));
            PrintMap.TrackOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle.DrawingLevel = DrawingLevel.LabelLevel;
            PrintMap.TrackOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = new LineStyle(new GeoPen(GeoColor.StandardColors.Black, 2), new GeoPen(GeoColor.StandardColors.Black, 1));
            PrintMap.TrackOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle.SetDrawingLevel();
            PrintMap.TrackOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = new PointStyle(PointSymbolType.Circle, new GeoSolidBrush(GeoColor.StandardColors.White), new GeoPen(GeoColor.StandardColors.Black), 8);
            PrintMap.TrackOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle.DrawingLevel = DrawingLevel.LabelLevel;
            PrintMap.TrackOverlay.TrackShapeLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
            PrintMap.TrackOverlay.TrackEnded += new EventHandler<TrackEndedTrackInteractiveOverlayEventArgs>(TrackOverlay_TrackEnded);
            PrintMap.MapTools.Logo.IsEnabled = false;
            var switchPanZoomBar = PrintMap.MapTools.OfType<SwitcherPanZoomBarMapTool>().FirstOrDefault();
            if (switchPanZoomBar != null) switchPanZoomBar.IsEnabled = false;
            var scaleLine = PrintMap.MapTools.OfType<ScaleLineMapTool>().FirstOrDefault();
            if (scaleLine != null) PrintMap.MapTools.Remove(scaleLine);
            PrintMap.FixedAdornmentOverlay.Layers.Clear();
            PrintMap.FixedAdornmentOverlay.IsVisible = false;
            PrintMap.ExtentOverlay.DoubleLeftClickMode = MapDoubleLeftClickMode.Disabled;
            PrintMap.ExtentOverlay.DoubleRightClickMode = MapDoubleRightClickMode.Disabled;
            PrintMap.MapDoubleClick += (s, e) =>
            {
                if (PrintMap.InteractiveOverlays.Contains("PrintPreviewOverlay"))
                {
                    for (int i = PrinterOverlay.PrinterLayers.Count - 1; i >= 0; i--)
                    {
                        if (PrinterOverlay.PrinterLayers[i].GetType() != typeof(PagePrinterLayer))
                        {
                            RectangleShape boundingBox = PrinterOverlay.PrinterLayers[i].GetPosition();
                            if (boundingBox.Contains(e.WorldLocation))
                            {
                                EditPrinterLayer(PrinterOverlay.PrinterLayers[i]);
                                break;
                            }
                        }
                    }
                }
            };

            PrintMap.MapUnit = GeographyUnit.Meter;
            PrintMap.ZoomLevelSet = new PrinterZoomLevelSet(PrintMap.MapUnit, PrinterHelper.GetPointsPerGeographyUnit(PrintMap.MapUnit));
            PrintMap.BackgroundOverlay.BackgroundBrush = new GeoSolidBrush(GeoColor.StandardColors.LightGray);

            PrinterOverlay = new AdvancedPrinterInteractiveOverlay { DrawingExceptionMode = DrawingExceptionMode.DrawException };

            var trackOverlay = PrintMap.InteractiveOverlays.OfType<GisEditorTrackInteractiveOverlay>().FirstOrDefault();
            if (trackOverlay != null)
            {
                var index = PrintMap.InteractiveOverlays.IndexOf(trackOverlay);
                if (index + 1 <= PrintMap.InteractiveOverlays.Count)
                {
                    PrintMap.InteractiveOverlays.Insert(index + 1, "PrintPreviewOverlay", PrinterOverlay);
                }
            }

            pagePrinterLayer.Open();
            PrinterOverlay.PrinterLayers.Add("PageLayer", pagePrinterLayer);
            PrinterOverlay.MapMouseClick += new EventHandler<MapMouseClickInteractiveOverlayEventArgs>(PrinterOverlay_MapMouseClick);
            PrintMap.CurrentExtent = new RectangleShape(-631.249660084684, 662.599643203345, 631.249660084684, -662.599643203345);

            PrintMap.MinimumScale = PrintMap.ZoomLevelSet.ZoomLevel20.Scale;

            PrintMap.ContextMenu = null;
        }

        private void TrackOverlay_TrackEnded(object sender, TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            Feature feature = new Feature(e.TrackShape);
            RectangleShape featureBBox = feature.GetBoundingBox();

            if (e.TrackShape is PointShape)
            {
                featureBBox = featureBBox.Buffer(printMap.CurrentResolution * defaultPointStyle.SymbolSize, GeographyUnit.Meter, DistanceUnit.Meter).GetBoundingBox();
            }
            else featureBBox.ScaleUp(2);

            var height = (int)(featureBBox.Height / PrintMap.CurrentExtent.Height * printerOverlay.MapArguments.ActualHeight);
            var width = (int)(featureBBox.Width / PrintMap.CurrentExtent.Width * printerOverlay.MapArguments.ActualWidth);
            if (height > 0 && width > 0)
            {
                InMemoryFeatureLayer featureLayer = new InMemoryFeatureLayer();
                featureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = defaultAreaStyle;
                featureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = defaultLineStyle;
                featureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = defaultPointStyle;
                featureLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;
                featureLayer.InternalFeatures.Add(feature);

                DrawingFeatureSimplifyMapPrinterLayer drawingFeaturePrinterLayer = new DrawingFeatureSimplifyMapPrinterLayer();
                drawingFeaturePrinterLayer.Layers.Add(featureLayer);
                drawingFeaturePrinterLayer.MapUnit = GeographyUnit.Meter;
                drawingFeaturePrinterLayer.MapExtent = featureBBox;
                drawingFeaturePrinterLayer.Open();
                drawingFeaturePrinterLayer.SetPosition(featureBBox);
                printerOverlay.PrinterLayers.Add(drawingFeaturePrinterLayer);
                printMap.TrackOverlay.TrackShapeLayer.InternalFeatures.Clear();
                printMap.Refresh();
                printMap.TrackOverlay.TrackMode = TrackMode.None;
                IsPoint = false;
                IsLine = false;
                IsPolygon = false;
                IsRectangle = false;
                IsSquare = false;
                IsCircle = false;
                IsEllipse = false;
                printMap.Cursor = GisEditorCursors.Normal;
            }
        }

        private void PrinterOverlay_MapMouseClick(object sender, MapMouseClickInteractiveOverlayEventArgs e)
        {
            selectedPrinterLayer = null;
            IsSelectMapElement = false;
            RefreshButtonEnabled = false;
            if (PrintMap.InteractiveOverlays.Contains("PrintPreviewOverlay"))
            {
                var worldLocation = new PointShape(e.InteractionArguments.WorldX, e.InteractionArguments.WorldY);
                for (int i = PrinterOverlay.PrinterLayers.Count - 1; i >= 0; i--)
                {
                    var currentElementType = PrinterOverlay.PrinterLayers[i].GetType();
                    selectedPrinterLayer = PrinterOverlay.PrinterLayers[i];
                    if (currentElementType != typeof(PagePrinterLayer))
                    {
                        RectangleShape boundingBox = PrinterOverlay.PrinterLayers[i].GetPosition();
                        if (boundingBox.Contains(worldLocation))
                        {
                            var contextMenu = new ContextMenu();
                            if (currentElementType == (typeof(SimplifyMapPrinterLayer)))
                            {
                                RefreshButtonEnabled = true;
                                var selectedMapPrinterLayer = PrinterOverlay.PrinterLayers[i] as MapPrinterLayer;
                                var scaleLineLayer = ScaleLinePrinterLayerAdapter.GetScaleLinePrinterLayer(new ScaleLineElementViewModel(selectedMapPrinterLayer));
                                var outOfRange = selectedMapPrinterLayer.MapUnit == GeographyUnit.DecimalDegree && !PrinterLayerHelper.CheckDecimalDegreeIsInRange(selectedMapPrinterLayer.MapExtent);
                                IsSelectMapElement = !outOfRange && ScaleLineElementViewModel.IsValid(scaleLineLayer);
                                if (e.InteractionArguments.MouseButton == MapMouseButton.Right)
                                {
                                    contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintSetExtentMenuItemLabel"), PrinterOverlay.PrinterLayers[i], true, SetPositionClick));
                                    contextMenu.Items.Add(GetMenuItem("Margins", PrinterOverlay.PrinterLayers[i], true, SetMarginsClick));
                                    contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintAddScaleLineMenuItemLabel"), PrinterOverlay.PrinterLayers[i], IsSelectMapElement, AddScaleLineItemClick));

                                    contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintAddScaleBarMenuItemLabel"), PrinterOverlay.PrinterLayers[i], IsSelectMapElement, AddScaleBarItemClick));
                                }
                            }
                            else
                            {
                                if (e.InteractionArguments.MouseButton == MapMouseButton.Right)
                                {
                                    if (currentElementType == typeof(DrawingFeatureSimplifyMapPrinterLayer)) contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintEditStyleMenuItemLabel"), PrinterOverlay.PrinterLayers[i], true, EditStyleMenuItemClick));
                                    else contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintEditMenuItemLabel"), PrinterOverlay.PrinterLayers[i], true, EditItemClick));
                                }
                            }

                            if (e.InteractionArguments.MouseButton == MapMouseButton.Right)
                            {
                                contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("GeneralRemoveContent"), PrinterOverlay.PrinterLayers[i], true, RemoveMenuItemClick));
                                contextMenu.Items.Add(new Separator());
                                contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintLockRatioMenuItemLabel"), PrinterOverlay.PrinterLayers[i], true, LockAspectRatioMenuItemClick, PrinterOverlay.PrinterLayers[i].ResizeMode == PrinterResizeMode.MaintainAspectRatio));
                                contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintLockPositionMenuItemLabel"), PrinterOverlay.PrinterLayers[i], true, LockPositionMenuItemClick, PrinterOverlay.PrinterLayers[i].DragMode == PrinterDragMode.Fixed));
                                contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintBringForwardMenuItemLabel"), PrinterOverlay.PrinterLayers[i], true, BringForwardMenuItemClick));
                                contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintBringToFrontMenuItemLabel"), PrinterOverlay.PrinterLayers[i], true, BringToFrontMenuItemClick));
                                contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintSendBackwardMenuItemLabel"), PrinterOverlay.PrinterLayers[i], true, SendBackwardMenuItemClick));
                                contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintSendToBackMenuItemLabel"), PrinterOverlay.PrinterLayers[i], true, SendToBackMenuItemClick));
                                contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintCopyMenuItemLabel"), PrinterOverlay.PrinterLayers[i], true, CopyMenuItemClick));
                                contextMenu.Items.Add(GetMenuItem(GisEditor.LanguageManager.GetStringResource("PrintPasteMenuItemLabel"), PrinterOverlay.PrinterLayers[i], copiedPrinterLayer != null, PasteMenuItemClick));
                                contextMenu.IsOpen = true;
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void EditStyleMenuItemClick(object sender, RoutedEventArgs e)
        {
            var mapPrinterLayer = GetPrinterLayerFromMenuItemTag<DrawingFeatureSimplifyMapPrinterLayer>(sender);
            if (mapPrinterLayer != null && mapPrinterLayer.Feature != null)
            {
                var wellKnownType = mapPrinterLayer.Feature.GetWellKnownType();
                switch (wellKnownType)
                {
                    case WellKnownType.Point:
                    case WellKnownType.Multipoint:
                        EditTrackLayerPointStyle(mapPrinterLayer.FeatureLayer);
                        break;
                    case WellKnownType.Line:
                    case WellKnownType.Multiline:
                        EditTrackLayerLineStyle(mapPrinterLayer.FeatureLayer);
                        break;
                    case WellKnownType.Multipolygon:
                    case WellKnownType.Polygon:
                        EditTrackLayerAreaStyle(mapPrinterLayer.FeatureLayer);
                        break;
                    default:
                        break;
                }
                //To clear cache and make this layer can be redrawn
                mapPrinterLayer.IsDrawing = true;
                printMap.Refresh();
                mapPrinterLayer.IsDrawing = false;
            }
        }

        private Collection<Feature> GetEditingFeaturesInterseting(InMemoryFeatureLayer featureLayer, MapMouseClickInteractiveOverlayEventArgs e)
        {
            PointShape pointShape = new PointShape(e.InteractionArguments.WorldX, e.InteractionArguments.WorldY);

            double searchTorlerence = e.InteractionArguments.SearchingTolerance;
            var searchArea = new RectangleShape(pointShape.X - searchTorlerence,
                pointShape.Y + searchTorlerence, pointShape.X + searchTorlerence,
                pointShape.Y - searchTorlerence);

            Collection<Feature> features = new Collection<Feature>();
            lock (featureLayer)
            {
                if (!featureLayer.IsOpen) featureLayer.Open();
                var intersectingFeatures = featureLayer.QueryTools.GetFeaturesIntersecting(searchArea, featureLayer.GetDistinctColumnNames());
                foreach (var feature in intersectingFeatures)
                {
                    features.Add(feature);
                }
            }
            return features;
        }

        private void AddScaleBarItemClick(object sender, RoutedEventArgs e)
        {
            var printerLayer = GetPrinterLayerFromMenuItemTag<MapPrinterLayer>(sender);
            var viewModel = new ScaleBarElementViewModel(printerLayer);
            printerOverlay.PrinterLayers.Add(ScaleBarPrinterLayerAdapter.GetScaleBarPrinterLayer(viewModel));
            printMap.Refresh();
        }

        private void AddScaleLineItemClick(object sender, RoutedEventArgs e)
        {
            var printerLayer = GetPrinterLayerFromMenuItemTag<MapPrinterLayer>(sender);
            var viewModel = new ScaleLineElementViewModel(printerLayer);
            printerOverlay.PrinterLayers.Add(ScaleLinePrinterLayerAdapter.GetScaleLinePrinterLayer(viewModel));
            printMap.Refresh();
        }

        private void LockAspectRatioMenuItemClick(object sender, RoutedEventArgs e)
        {
            var printerLayer = GetPrinterLayerFromMenuItemTag<PrinterLayer>(sender);
            printerLayer.ResizeMode = printerLayer.ResizeMode != PrinterResizeMode.MaintainAspectRatio ? PrinterResizeMode.MaintainAspectRatio : PrinterResizeMode.Resizable;
        }

        private void LockPositionMenuItemClick(object sender, RoutedEventArgs e)
        {
            var printerLayer = GetPrinterLayerFromMenuItemTag<PrinterLayer>(sender);
            printerLayer.DragMode = printerLayer.DragMode != PrinterDragMode.Fixed ? PrinterDragMode.Fixed : PrinterDragMode.Draggable;
        }

        private void EditItemClick(object sender, RoutedEventArgs e)
        {
            OnEditingItem(sender, e);
        }

        private void SetPositionClick(object sender, RoutedEventArgs e)
        {
            OnSetPositionItem(sender, e);
        }

        private void SetMarginsClick(object sender, RoutedEventArgs e)
        {
            SetMarginsWindow setMarginsWindow = new SetMarginsWindow();
            MapPrinterLayer mapPrinterLayer = ((MenuItem)sender).Tag as MapPrinterLayer;
            RectangleShape pageBBoxInInch = GetPageBoundingBox(PrintingUnit.Inch);
            RectangleShape mapBBox = mapPrinterLayer.GetPosition(PrintingUnit.Inch);
            setMarginsWindow.MarginLeft = Math.Round(mapBBox.UpperLeftPoint.X - pageBBoxInInch.UpperLeftPoint.X, 1);
            setMarginsWindow.MarginTop = Math.Round(pageBBoxInInch.UpperLeftPoint.Y - mapBBox.UpperLeftPoint.Y, 1);
            setMarginsWindow.MarginRight = Math.Round(pageBBoxInInch.LowerRightPoint.X - mapBBox.LowerRightPoint.X, 1);
            setMarginsWindow.MarginBottom = Math.Round(mapBBox.LowerRightPoint.Y - pageBBoxInInch.LowerRightPoint.Y, 1);

            if (setMarginsWindow.ShowDialog().GetValueOrDefault())
            {
                if (mapPrinterLayer != null)
                {
                    RectangleShape pageBBox = GetPageBoundingBox(setMarginsWindow.ResultUnit);
                    PointShape upperLeftPoint = new PointShape(pageBBox.UpperLeftPoint.X + setMarginsWindow.MarginLeft, pageBBox.UpperLeftPoint.Y - setMarginsWindow.MarginTop);
                    PointShape lowerRightPoint = new PointShape(pageBBox.LowerRightPoint.X - setMarginsWindow.MarginRight, pageBBox.LowerRightPoint.Y + setMarginsWindow.MarginBottom);
                    mapPrinterLayer.SetPosition(new RectangleShape(upperLeftPoint, lowerRightPoint), setMarginsWindow.ResultUnit);

                    InteractionArguments interactionArguments = new InteractionArguments();
                    PointShape newCenterPoint = mapPrinterLayer.GetBoundingBox().GetCenterPoint();
                    interactionArguments.WorldX = newCenterPoint.X;
                    interactionArguments.WorldY = newCenterPoint.Y;
                    printerOverlay.MouseClick(interactionArguments);
                    printerOverlay.Refresh();
                }
            }
        }

        private void RemoveMenuItemClick(object sender, RoutedEventArgs e)
        {
            OnRemovingItem(sender, e);
        }

        private void SendToBackMenuItemClick(object sender, RoutedEventArgs e)
        {
            var printerLayer = GetPrinterLayerFromMenuItemTag<PrinterLayer>(sender);
            printerOverlay.PrinterLayers.Remove(printerLayer);
            printerOverlay.PrinterLayers.Insert(1, printerLayer);
            printMap.Refresh();
        }

        private void SendBackwardMenuItemClick(object sender, RoutedEventArgs e)
        {
            var printerLayer = GetPrinterLayerFromMenuItemTag<PrinterLayer>(sender);
            var index = printerOverlay.PrinterLayers.IndexOf(printerLayer);
            if (index - 1 > 0)
            {
                printerOverlay.PrinterLayers.Remove(printerLayer);
                printerOverlay.PrinterLayers.Insert(index - 1, printerLayer);
                printMap.Refresh();
            }
        }

        private void BringToFrontMenuItemClick(object sender, RoutedEventArgs e)
        {
            var printerLayer = GetPrinterLayerFromMenuItemTag<PrinterLayer>(sender);
            printerOverlay.PrinterLayers.Remove(printerLayer);
            printerOverlay.PrinterLayers.Add(printerLayer);
            printMap.Refresh();
        }

        private void BringForwardMenuItemClick(object sender, RoutedEventArgs e)
        {
            var printerLayer = GetPrinterLayerFromMenuItemTag<PrinterLayer>(sender);
            var index = printerOverlay.PrinterLayers.IndexOf(printerLayer);
            if (index + 1 <= printerOverlay.PrinterLayers.Count - 1)
            {
                printerOverlay.PrinterLayers.RemoveAt(index);
                printerOverlay.PrinterLayers.Insert(index + 1, printerLayer);
                printMap.Refresh();
            }
        }

        private void CopyMenuItemClick(object sender, RoutedEventArgs e)
        {
            var printerLayer = GetPrinterLayerFromMenuItemTag<PrinterLayer>(sender);
            Copy(printerLayer);
        }

        private void PasteMenuItemClick(object sender, RoutedEventArgs e)
        {
            Paste();
        }

        private MenuItem GetMenuItem(string text, object tag, bool isEnabled, RoutedEventHandler clickHandler, bool isChecked = false)
        {
            MenuItem menuItem = new MenuItem();
            menuItem.Tag = tag;
            menuItem.IsChecked = isChecked;
            menuItem.IsEnabled = isEnabled;
            menuItem.Header = text;
            menuItem.Icon = new System.Windows.Controls.Image();
            menuItem.Click += clickHandler;
            return menuItem;
        }

        internal void Copy(PrinterLayer printerLayer)
        {
            bool isOpen = false;
            if (printerLayer.IsOpen)
            {
                isOpen = true;
                printerLayer.Close();
            }
            copiedPrinterLayer = printerLayer.CloneDeep() as PrinterLayer;
            if (isOpen) printerLayer.Open();
        }

        internal void Paste()
        {
            if (copiedPrinterLayer == null) return;
            if (!copiedPrinterLayer.IsOpen) copiedPrinterLayer.Open();
            var pos = copiedPrinterLayer.GetPosition(PrintingUnit.Inch);
            copiedPrinterLayer.SetPosition(pos.Width, pos.Height, 0, 0, PrintingUnit.Inch);
            printerOverlay.PrinterLayers.Add(copiedPrinterLayer);
            copiedPrinterLayer = null;
            printMap.Refresh();
        }

        internal bool EditTrackLayerAreaStyle(InMemoryFeatureLayer inMemoryFeatureLayer)
        {
            if (inMemoryFeatureLayer == null)
            {
                inMemoryFeatureLayer = new InMemoryFeatureLayer();
                inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = defaultAreaStyle;
            }

            var areaStyle = (AreaStyle)inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle.CloneDeep();

            var styleArguments = new StyleBuilderArguments();
            styleArguments.AvailableUIElements = StyleBuilderUIElements.StyleList;
            styleArguments.FeatureLayer = inMemoryFeatureLayer;
            styleArguments.AvailableStyleCategories = StyleCategories.Area;
            styleArguments.AppliedCallback = (result) =>
            {
                AreaStyle tempAreaStyle = new AreaStyle();
                foreach (var item in result.CompositeStyle.Styles.OfType<AreaStyle>())
                {
                    tempAreaStyle.CustomAreaStyles.Add(item);
                }
                defaultAreaStyle = tempAreaStyle;
                defaultAreaStyle.DrawingLevel = DrawingLevel.LabelLevel;
                inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = defaultAreaStyle;
            };

            var resultStyle = GisEditor.StyleManager.EditStyles(styleArguments, areaStyle);
            if (resultStyle != null)
            {
                defaultAreaStyle = resultStyle;
                defaultAreaStyle.DrawingLevel = DrawingLevel.LabelLevel;
                inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = defaultAreaStyle;
                return true;
            }
            return false;
        }

        internal bool EditTrackLayerLineStyle(InMemoryFeatureLayer inMemoryFeatureLayer)
        {
            if (inMemoryFeatureLayer == null)
            {
                inMemoryFeatureLayer = new InMemoryFeatureLayer();
                inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = defaultLineStyle;
            }

            var lineStyle = (LineStyle)inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle.CloneDeep();

            var styleArguments = new StyleBuilderArguments();
            styleArguments.AvailableUIElements = StyleBuilderUIElements.StyleList;
            styleArguments.FeatureLayer = inMemoryFeatureLayer;
            styleArguments.AvailableStyleCategories = StyleCategories.Line;
            styleArguments.AppliedCallback = (result) =>
            {
                LineStyle tempLineStyle = new LineStyle();
                foreach (var item in result.CompositeStyle.Styles.OfType<LineStyle>())
                {
                    tempLineStyle.CustomLineStyles.Add(item);
                }
                defaultLineStyle = tempLineStyle;
                defaultLineStyle.SetDrawingLevel();
                inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = defaultLineStyle;
            };

            var resultStyle = GisEditor.StyleManager.EditStyles(styleArguments, lineStyle);
            if (resultStyle != null)
            {
                defaultLineStyle = resultStyle;
                defaultLineStyle.SetDrawingLevel();
                inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultLineStyle = defaultLineStyle;
                return true;
            }
            return false;
        }

        internal bool EditTrackLayerPointStyle(InMemoryFeatureLayer inMemoryFeatureLayer)
        {
            if (inMemoryFeatureLayer == null)
            {
                inMemoryFeatureLayer = new InMemoryFeatureLayer();
                inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = defaultPointStyle;
            }

            var pointStyle = (PointStyle)inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle.CloneDeep();

            var styleArguments = new StyleBuilderArguments();
            styleArguments.AvailableUIElements = StyleBuilderUIElements.StyleList;
            styleArguments.FeatureLayer = inMemoryFeatureLayer;
            styleArguments.AvailableStyleCategories = StyleCategories.Point;
            styleArguments.AppliedCallback = (result) =>
            {
                PointStyle tempPointStyle = new PointStyle();
                foreach (var item in result.CompositeStyle.Styles.OfType<PointStyle>())
                {
                    tempPointStyle.CustomPointStyles.Add(item);
                }
                defaultPointStyle = tempPointStyle;
                defaultPointStyle.DrawingLevel = DrawingLevel.LabelLevel;
                inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = defaultPointStyle;
            };

            var resultStyle = GisEditor.StyleManager.EditStyles(styleArguments, pointStyle);
            if (resultStyle != null)
            {
                defaultPointStyle = resultStyle;
                PointStyle customPointStype = resultStyle.CustomPointStyles.OfType<PointStyle>().FirstOrDefault();
                if (customPointStype != null)
                {
                    defaultPointStyle = customPointStype;
                }

                defaultPointStyle.DrawingLevel = DrawingLevel.LabelLevel;
                inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultPointStyle = defaultPointStyle;

                return true;
            }
            return false;
        }

        private void OnEditingItem(object sender, RoutedEventArgs e)
        {
            EventHandler<RoutedEventArgs> handler = EditingItem;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void OnSetPositionItem(object sender, RoutedEventArgs e)
        {
            EventHandler<RoutedEventArgs> handler = SetPosition;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void OnRemovingItem(object sender, RoutedEventArgs e)
        {
            EventHandler<RoutedEventArgs> handler = RemovingItem;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void ZoomTo(MapPrinterLayer mapPrinterLayer, double newScale)
        {
            var mapBoundingBox = mapPrinterLayer.GetBoundingBox();
            mapPrinterLayer.MapExtent = ExtentHelper.ZoomToScale(newScale, mapPrinterLayer.MapExtent, mapPrinterLayer.MapUnit, (float)mapBoundingBox.Width, (float)mapBoundingBox.Height);
            PrintMap.Refresh();
        }

        private void SavePrinterLayerLayout(string layoutFilePath)
        {
            Dictionary<SimplifyMapPrinterLayer, List<Layer>> layers = new Dictionary<SimplifyMapPrinterLayer, List<Layer>>();
            PrintLayersSerializeProxy wrapper = new PrintLayersSerializeProxy(printerOverlay.PrinterLayers);
            wrapper.GridPrinterLayer = PrinterOverlay.GridLayer;
            foreach (var mapPrinterLayer in wrapper.PrinterLayers.Where(l => l.GetType() == typeof(SimplifyMapPrinterLayer)).OfType<SimplifyMapPrinterLayer>())
            {
                layers.Add(mapPrinterLayer, mapPrinterLayer.Layers.ToList());
                mapPrinterLayer.Layers.Clear();
            }
            GisEditor.Serializer.Serialize(wrapper, layoutFilePath);
            foreach (var item in layers)
            {
                foreach (var layer in item.Value)
                {
                    item.Key.Layers.Add(layer);
                }
            }
        }

        internal string SaveSignatures()
        {
            foreach (var item in signatures)
            {
                item.SignaturePrinterLayer.Name = item.Name;
            }
            PrintLayersSerializeProxy wrapper = new PrintLayersSerializeProxy(signatures.Select(s => s.SignaturePrinterLayer));
            return GisEditor.Serializer.Serialize(wrapper);
        }

        internal void SavePrinterlayerToProject()
        {
            try
            {
                var tmpFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "temp");
                if (!Directory.Exists(tmpFolder)) Directory.CreateDirectory(tmpFolder);
                var layoutFile = Path.Combine(tmpFolder, "PrintedLayout.tglayt");
                SavePrinterLayerLayout(layoutFile);
                var x = XElement.Load(layoutFile);
                var appMenuUIPlugin = GisEditor.UIManager.GetActiveUIPlugins<AppMenuUIPlugin>().FirstOrDefault();
                if (appMenuUIPlugin != null)
                {
                    appMenuUIPlugin.PrintedLayoutXml = x.ToString();
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
        }

        private bool CheckAllMapElementsHasLayers()
        {
            var result = PrinterOverlay.PrinterLayers.OfType<MapPrinterLayer>().All(m => m.Layers.Count > 0);
            if (!result) System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("PrintSetExtentWarningLabel"), GisEditor.LanguageManager.GetStringResource("PrintNoteWarningCaption"));
            return result;
        }

        private T GetPrinterLayerFromMenuItemTag<T>(object sender) where T : PrinterLayer
        {
            var menuItem = sender as MenuItem;
            return menuItem.Tag as T;
        }


        #region drag events

        public void KeyUP(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                printMap.PreviewMouseDown -= new MouseButtonEventHandler(ActiveMapDrag_MouseDown);
                printMap.PreviewMouseMove -= new MouseEventHandler(ActiveMapDrag_MouseMove);
                printMap.PreviewMouseUp -= new MouseButtonEventHandler(ActiveMapDrag_MouseUp);

                printMap.Cursor = previousCursor;

                EndPanningBySpaceKey();
            }
        }

        public void KeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (!e.IsRepeat)
            {
                if (e.Key == Key.Space)
                {
                    //printerOverlay.IsEditable = false;
                    printMap.PreviewMouseDown -= new MouseButtonEventHandler(ActiveMapDrag_MouseDown);
                    printMap.PreviewMouseMove -= new MouseEventHandler(ActiveMapDrag_MouseMove);
                    printMap.PreviewMouseUp -= new MouseButtonEventHandler(ActiveMapDrag_MouseUp);

                    printMap.PreviewMouseDown += new MouseButtonEventHandler(ActiveMapDrag_MouseDown);
                    printMap.PreviewMouseMove += new MouseEventHandler(ActiveMapDrag_MouseMove);
                    printMap.PreviewMouseUp += new MouseButtonEventHandler(ActiveMapDrag_MouseUp);

                    previousCursor = printMap.Cursor;
                    printMap.Cursor = GisEditorCursors.Pan;

                    e.Handled = true;
                }
            }
        }

        private void ActiveMapDrag_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            EndPanningBySpaceKey();
            e.Handled = true;
        }

        private void EndPanningBySpaceKey()
        {
            if (isDragging)
            {
                isDragging = false;
                printMap.Refresh(printMap.InteractiveOverlays);
            }
        }

        private void ActiveMapDrag_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                System.Windows.Point currentPosition = e.GetPosition(printMap);
                double currentResolution = printMap.CurrentResolution;
                double offsetScreenX = currentPosition.X - originPosition.X;
                double offsetScreenY = currentPosition.Y - originPosition.Y;

                printMap.Pan(-offsetScreenX, offsetScreenY);
                printMap.Cursor = GisEditorCursors.Grab;
                originPosition = currentPosition;
                e.Handled = true;
            }
        }

        private void ActiveMapDrag_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            originPosition = e.GetPosition(printMap);
            isDragging = true;
            e.Handled = true;
        }

        #endregion drag events
    }
}