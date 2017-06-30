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
using System.Reflection;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BookmarkViewModel : ViewModelBase
    {
        [Obfuscation(Exclude = true)]
        private string name;

        [NonSerialized]
        private bool isRenaming;

        [NonSerialized]
        private bool isGlobal;

        [NonSerialized]
        private string imageUri;

        [NonSerialized]
        private DateTime dateCreated;

        [NonSerialized]
        private DateTime dateModified;

        public BookmarkViewModel()
            : this("New Bookmark")
        { }

        public BookmarkViewModel(string name)
            : this(name, DateTime.Now)
        {

        }

        public BookmarkViewModel(string name, DateTime dateCreated)
        {
            this.name = name;
            this.dateCreated = dateCreated;
            this.dateModified = dateCreated;
        }

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged(()=>Name);
                DateModified = DateTime.Now;
            }
        }

        public bool IsRenaming
        {
            get { return isRenaming; }
            set
            {
                isRenaming = value;
                RaisePropertyChanged(()=>IsRenaming);
            }
        }

        public bool IsGlobal
        {
            get { return isGlobal; }
            set
            {
                isGlobal = value;
                RaisePropertyChanged(()=>IsGlobal);
            }
        }

        public string ImageUri
        {
            get { return imageUri; }
            set { imageUri = value; }
        }

        [Obfuscation(Exclude = true)]
        public PointShape Center { get; set; }

        [Obfuscation(Exclude = true)]
        public double Scale { get; set; }

        [Obfuscation]
        public string InternalProj4Projection { get; set; }

        public DateTime DateCreated
        {
            get { return dateCreated; }
        }

        public DateTime DateModified
        {
            get { return dateModified; }
            set
            {
                dateModified = value;
                RaisePropertyChanged(()=>DateModified);
            }
        }
    }
}