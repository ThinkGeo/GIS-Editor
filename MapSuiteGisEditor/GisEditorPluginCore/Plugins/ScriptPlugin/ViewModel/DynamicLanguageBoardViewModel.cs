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
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class DynamicLanguageBoardViewModel : ViewModelBase
    {
        private string scriptText;
        [NonSerialized]
        private RelayCommand openScriptFileCommand;
        [NonSerialized]
        private RelayCommand saveScriptFileCommand;
        [NonSerialized]
        private RelayCommand runScriptCommand;
        [NonSerialized]
        private OpenFileDialog openFileDialog;
        [NonSerialized]
        private SaveFileDialog saveFileDialog;
        private DlrLanguage currentLanguage;

        public DynamicLanguageBoardViewModel()
        {
            openFileDialog = new OpenFileDialog();
            saveFileDialog = new SaveFileDialog();
        }

        public RelayCommand OpenScriptFileCommand
        {
            get
            {
                if (openScriptFileCommand == null)
                {
                    openScriptFileCommand = new RelayCommand(() =>
                    {
                        if (CurrentLanguage != null)
                        {
                            openFileDialog.Filter = CurrentLanguage.FileFilters;
                            if (openFileDialog.ShowDialog().GetValueOrDefault())
                            {
                                using (StreamReader streamReader = new StreamReader(openFileDialog.FileName))
                                {
                                    ScriptText = streamReader.ReadToEnd();
                                }
                            }
                        }
                    });
                }
                return openScriptFileCommand;
            }
        }

        public RelayCommand SaveScriptFileCommand
        {
            get
            {
                if (saveScriptFileCommand == null)
                {
                    saveScriptFileCommand = new RelayCommand(() =>
                    {
                        if (CurrentLanguage != null && !string.IsNullOrEmpty(ScriptText))
                        {
                            saveFileDialog.Filter = CurrentLanguage.FileFilters;
                            if (saveFileDialog.ShowDialog().GetValueOrDefault())
                            {
                                using (StreamWriter streamWriter = new StreamWriter(saveFileDialog.FileName))
                                {
                                    streamWriter.Write(ScriptText);
                                }
                            }
                        }
                    });
                }
                return saveScriptFileCommand;
            }
        }

        public RelayCommand RunScriptCommand
        {
            get
            {
                if (runScriptCommand == null)
                {
                    runScriptCommand = new RelayCommand(() =>
                    {
                        if (CurrentLanguage != null && !String.IsNullOrEmpty(ScriptText))
                        {
                            CurrentLanguage.Script = ScriptText;
                            CurrentLanguage.Variables.Clear();
                            CurrentLanguage.Variables.Add("Map", GisEditor.ActiveMap);
                            try
                            {
                                CurrentLanguage.RunScript();
                            }
                            catch (Exception ex)
                            {
                                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                                System.Windows.Forms.MessageBox.Show(ex.Message);
                            }
                        }
                    });
                }
                return runScriptCommand;
            }
        }

        public string ScriptText
        {
            get { return scriptText; }
            set
            {
                scriptText = value;
                RaisePropertyChanged(()=>ScriptText);
            }
        }

        public DlrLanguage CurrentLanguage
        {
            get { return currentLanguage; }
            set { currentLanguage = value; }
        }
    }
}