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
using System.Diagnostics;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class HelpViewModel : ViewModelBase
    {
        private ObservedCommand searchCommand;
        private ObservedCommand askQuestionCommand;
        private ObservedCommand requestCommand;
        private ObservedCommand reportCommand;
        private ObservedCommand findCommand;

        public HelpViewModel()
        { }

        public ObservedCommand FindCommand
        {
            get
            {
                if (findCommand == null)
                {
                    findCommand = new ObservedCommand(() =>
                    {
                        Process.Start(GisEditor.LanguageManager.GetStringResource("FindAnExampleHelp"));
                    }, () => true);
                }
                return findCommand;
            }
        }

        public ObservedCommand ReportCommand
        {
            get
            {
                if (reportCommand == null)
                {
                    reportCommand = new ObservedCommand(() =>
                    {
                        Process.Start(GisEditor.LanguageManager.GetStringResource("ReportABugHelp"));
                    }, () => true);
                }
                return reportCommand;
            }
        }

        public ObservedCommand RequestCommand
        {
            get
            {
                if (requestCommand == null)
                {
                    requestCommand = new ObservedCommand(() =>
                    {
                        Process.Start(GisEditor.LanguageManager.GetStringResource("RequestAnEnhancementHelp"));
                    }, () => true);
                }
                return requestCommand;
            }
        }

        public ObservedCommand AskQuestionCommand
        {
            get
            {
                if (askQuestionCommand == null)
                {
                    askQuestionCommand = new ObservedCommand(() =>
                    {
                        Process.Start(GisEditor.LanguageManager.GetStringResource("AskAQuestionHelp"));
                    }, () => true);
                }
                return askQuestionCommand;
            }
        }

        public ObservedCommand SearchCommand
        {
            get
            {
                if (searchCommand == null)
                {
                    searchCommand = new ObservedCommand(() =>
                    {
                        Process.Start(GisEditor.LanguageManager.GetStringResource("SearchHelp"));
                    }, () => true);
                }
                return searchCommand;
            }
        }
    }
}
