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
using System.IO;
using System.Printing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for PrintPreviewWindow.xaml
    /// </summary>
    public partial class PrintPreviewWindow : Window
    {
        //we only keep one instance of print dialog
        private static PrintDialog printDialog = new PrintDialog();

        private static string printPreviewTempPathFileName;

        private XpsDocument printDoc;
        private PrinterOrientation printerOrientation;
        private PrinterOrientation targetPrinterOrientation;
        private FrameworkElement printElement;

        static PrintPreviewWindow()
        {
            printPreviewTempPathFileName = Path.Combine(FolderHelper.GetGisEditorFolder(), "PrintPreviewTemp.xps");
        }

        public PrintPreviewWindow(Image image = null, PrinterOrientation orientation = PrinterOrientation.Portrait)
        {
            InitializeComponent();
            printElement = image;
            printerOrientation = orientation;
            targetPrinterOrientation = orientation;
            if (image == null)
            {
                int width = (int)Math.Max(printDialog.PrintableAreaHeight, printDialog.PrintableAreaWidth);
                int height = (int)Math.Min(printDialog.PrintableAreaHeight, printDialog.PrintableAreaWidth);
                image = GisEditor.ActiveMap.GetPrintingImage(width, height);
            }
            GeneratePreview(image, orientation);
            docViewer.FitToMaxPagesAcross();
        }

        private void GeneratePreview(FrameworkElement printElement, PrinterOrientation orientation)
        {
            if (printDoc != null) printDoc.Close();
            printDoc = VisualToXpsDocument(printElement, orientation);
            docViewer.Document = printDoc.GetFixedDocumentSequence();
        }

        private XpsDocument VisualToXpsDocument(FrameworkElement printContent, PrinterOrientation orientation)
        {
            if (File.Exists(printPreviewTempPathFileName)) File.Delete(printPreviewTempPathFileName);
            XpsDocument xpsDocument = new XpsDocument(printPreviewTempPathFileName, FileAccess.ReadWrite);

            FixedPage page = new FixedPage();
            if (printContent.Parent != null && printContent.Parent is FixedPage)
            {
                ((FixedPage)printContent.Parent).Children.Remove(printContent);
            }
            page.Children.Add(printContent);

            if (this.printerOrientation == orientation)
            {
                page.Width = printContent.Width;
                page.Height = printContent.Height;
            }
            else
            {
                page.Width = printContent.Height;
                page.Height = printContent.Width;
            }

            PageContent pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(page);
            FixedDocument doc = new FixedDocument();
            doc.Pages.Add(pageContent);
            XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
            writer.Write(doc);
            return xpsDocument;
        }

        [Obfuscation]
        private void Print_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        [Obfuscation]
        private void Print_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (printDialog.ShowDialog().GetValueOrDefault())
            {
                printDialog.PrintTicket.PageOrientation =
                    targetPrinterOrientation == PrinterOrientation.Portrait ? PageOrientation.Portrait : PageOrientation.Landscape;
                printDialog.PrintVisual(printElement, "Print Map.");
                Close();
            }
        }

        [Obfuscation]
        private void Landscape_Click(object sender, RoutedEventArgs e)
        {
            targetPrinterOrientation = PrinterOrientation.Landscape;
            GeneratePreview(printElement, PrinterOrientation.Landscape);
        }

        [Obfuscation]
        private void Portrait_Click(object sender, RoutedEventArgs e)
        {
            targetPrinterOrientation = PrinterOrientation.Portrait;
            GeneratePreview(printElement, PrinterOrientation.Portrait);
        }

        [Obfuscation]
        private void Window_Closed(object sender, System.EventArgs e)
        {
            printDoc.Close();
            if (printDoc != null) printDoc.Close();
            if (File.Exists(printPreviewTempPathFileName)) File.Delete(printPreviewTempPathFileName);
        }
    }
}