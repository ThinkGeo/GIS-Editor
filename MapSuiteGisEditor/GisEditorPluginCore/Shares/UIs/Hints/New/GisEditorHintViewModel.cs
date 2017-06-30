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


using System.Collections.Generic;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class GisEditorHintViewModel : ViewModelBase
    {
        private string title;
        private string description;
        private Collection<string> steps;
        private string gifUri;
        private bool donotShowAgain;

        public GisEditorHintViewModel(string title, string description, IEnumerable<string> steps, string gifUri)
        {
            this.steps = new Collection<string>();
            this.title = title;
            this.description = description;
            foreach (var item in steps)
            {
                this.steps.Add(item);
            }
            this.gifUri = gifUri;
            this.donotShowAgain = true;
        }

        public string Title
        {
            get { return title; }
        }

        public string Description
        {
            get { return description; }
        }

        public Collection<string> Steps
        {
            get { return steps; }
        }

        public string GifUri
        {
            get { return gifUri; }
        }

        public bool DonotShowAgain
        {
            get { return donotShowAgain; }
            set
            {
                donotShowAgain = value;
                RaisePropertyChanged(()=>DonotShowAgain);
            }
        }
    }
}
