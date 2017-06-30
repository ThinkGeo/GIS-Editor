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


using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for DataGridElementWindow.xaml
    /// </summary>
    public partial class DataGridElementWindow : Window
    {
        private DataGridViewModel entity;

        public DataGridElementWindow()
        {
            InitializeComponent();
            entity = new DataGridViewModel();
            DataContext = entity;
            HelpContainer.Content = HelpResourceHelper.GetHelpButton("PrintMapDataGridHelp", HelpButtonMode.NormalButton);
        }

        [Obfuscation]
        private void OKClick(object sender, RoutedEventArgs e)
        {
            dgv.EndEdit();
            DialogResult = true;
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        [Obfuscation]
        private void AddClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(entity.AddingColumnName.Trim()))
            {
                if (!entity.CurrentDataTable.Columns.Contains(entity.AddingColumnName))
                {
                    entity.CurrentDataTable.Columns.Add(entity.AddingColumnName);
                    dgv.DataSource = entity.CurrentDataTable;
                    entity.AddingColumnName = string.Empty;
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("DuplicateColumnNameAlert"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"));
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ColumnNameInvalidAlert"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"));
            }
        }

        [Obfuscation]
        private void RemoveClick(object sender, RoutedEventArgs e)
        {
            if (entity.RemovingColumnName != null)
            {
                if (entity.CurrentDataTable.Columns.Contains(entity.RemovingColumnName))
                {
                    entity.CurrentDataTable.Columns.Remove(entity.RemovingColumnName);
                    dgv.DataSource = entity.CurrentDataTable;
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("NoSelectedItemAlert"), GisEditor.LanguageManager.GetStringResource("GeneralMessageBoxAlertCaption"));
            }
        }

        internal void SetProperties(PrinterLayer printerLayer)
        {
            DataGridPrinterLayer dataGridPrinterLayer = printerLayer as DataGridPrinterLayer;
            if (dataGridPrinterLayer != null)
            {
                entity.CurrentDataTable = dataGridPrinterLayer.DataTable;
                dgv.DataSource = entity.CurrentDataTable;
                entity.FontName = new FontFamily(dataGridPrinterLayer.TextFont.FontName);
                entity.FontSize = dataGridPrinterLayer.TextFont.Size;
                entity.FontColor = dataGridPrinterLayer.TextBrush;
                entity.IsBold = (dataGridPrinterLayer.TextFont.Style & DrawingFontStyles.Bold) == DrawingFontStyles.Bold;
                entity.IsItalic = (dataGridPrinterLayer.TextFont.Style & DrawingFontStyles.Italic) == DrawingFontStyles.Italic;
                entity.IsStrikeout = (dataGridPrinterLayer.TextFont.Style & DrawingFontStyles.Strikeout) == DrawingFontStyles.Strikeout;
                entity.IsUnderline = (dataGridPrinterLayer.TextFont.Style & DrawingFontStyles.Underline) == DrawingFontStyles.Underline;
                entity.ResizeMode = dataGridPrinterLayer.ResizeMode;
                entity.DragMode = dataGridPrinterLayer.DragMode;
            }
        }

        [Obfuscation]
        private void ColumnNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddClick(sender, e);
            }
        }

        [Obfuscation]
        private void ComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RemoveClick(sender, e);
            }
        }
    }
}