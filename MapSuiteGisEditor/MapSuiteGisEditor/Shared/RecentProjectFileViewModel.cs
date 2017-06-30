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
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public class RecentProjectModel : ViewModelBase
    {
        private int index;
        private string label;
        private Uri fullPath;
        private string projectPluginType;
        private bool isEnabled;

        public RecentProjectModel(Uri path, string projectPluginType, int index = 1, int trimFromCharIndex = 40)
        {
            FullPath = path;
            Index = index;
            Label = GisEditorHelper.SimplifyPath(path.LocalPath, trimFromCharIndex);
            ProjectPluginType = projectPluginType;
            isEnabled = true;
        }

        public int Index
        {
            get { return index; }
            set { index = value; RaisePropertyChanged(()=>Index); }
        }

        public string Label
        {
            get { return label; }
            set { label = value; }
        }

        public Uri FullPath
        {
            get { return fullPath; }
            set { fullPath = value;}
        }

        public string ProjectPluginType
        {
            get { return projectPluginType; }
            set { projectPluginType = value; }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set 
            { 
                isEnabled = value;
                RaisePropertyChanged(()=>IsEnabled);
            }
        }
    }
}