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
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class OutputUserControlViewModel : ViewModelBase
    {
        private string defaultPrefix;
        private string extensionFilter;
        private string tempFileName;
        private string outputPathFileName;
        private OutputMode outputMode;
        private ObservedCommand openSaveFileDialogCommand;

        public OutputUserControlViewModel()
            : this(string.Empty)
        {
        }

        public OutputUserControlViewModel(string extensionFilter)
            : this(extensionFilter, string.Empty)
        {
        }

        public OutputUserControlViewModel(string extensionFilter, string defaultPrefix)
        {
            this.defaultPrefix = defaultPrefix;
            this.extensionFilter = string.IsNullOrEmpty(extensionFilter) ? "Shape Files|*.shp" : extensionFilter;
            outputMode = OutputMode.ToTemporary;
        }

        public string DefaultPrefix
        {
            get { return defaultPrefix; }
            set { defaultPrefix = value; }
        }

        public string ExtensionFilter
        {
            get { return extensionFilter; }
            set { extensionFilter = value; }
        }

        public OutputMode OutputMode
        {
            get { return outputMode; }
            set
            {
                outputMode = value;
                RaisePropertyChanged(()=>OutputMode);
            }
        }

        public string TempFileName
        {
            get { return tempFileName; }
            set
            {
                tempFileName = value;
                RaisePropertyChanged(()=>TempFileName);
            }
        }

        public string OutputPathFileName
        {
            get { return outputPathFileName; }
            set
            {
                outputPathFileName = value;
                RaisePropertyChanged(()=>OutputPathFileName);
            }
        }

        public ObservedCommand OpenSaveFileDialogCommand
        {
            get
            {
                if (openSaveFileDialogCommand == null)
                {
                    openSaveFileDialogCommand = new ObservedCommand(() =>
                    {
                        SaveFileDialog dialog = new SaveFileDialog();
                        dialog.Filter = ExtensionFilter;
                        dialog.FileName = tempFileName;
                        if (dialog.ShowDialog().GetValueOrDefault())
                        {
                            string noExtentionPath = Path.Combine(Path.GetDirectoryName(dialog.FileName), Path.GetFileNameWithoutExtension(dialog.FileName));
                            string pathFileName = dialog.FileName;
                            string extension = Path.GetExtension(dialog.FileName);
                            int i = 0;
                            bool isExist = false;
                            while (File.Exists(pathFileName))
                            {
                                i++;
                                pathFileName = noExtentionPath + i.ToString() + extension;
                                isExist = true;
                            }
                            if (isExist) MessageBox.Show(string.Format(GisEditor.LanguageManager.GetStringResource("TheOutPutfilehasrenamedText"), Path.GetFileName(pathFileName)));
                            OutputPathFileName = pathFileName;
                        }
                    }, () => true);
                }

                return openSaveFileDialogCommand;
            }
        }
    }
}