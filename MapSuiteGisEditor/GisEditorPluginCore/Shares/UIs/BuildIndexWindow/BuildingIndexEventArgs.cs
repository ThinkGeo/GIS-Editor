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
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class BuildingIndexEventArgs : EventArgs
    {
        private int recordCount;
        private int currentRecordIndex;
        private Feature currentFeature;
        private DateTime startProcessTime;
        private bool cancel;

        /// <summary>
        /// This is the default constructor of the event args.
        /// </summary>
        /// <remarks>If you use this constructor, you have to set the properties manually.</remarks>
        public BuildingIndexEventArgs()
            : this(0, 0, new Feature(), DateTime.Now, false)
        {
        }

        public BuildingIndexEventArgs(int recordCount, int currentRecordIndex, Feature currentFeature, DateTime startProcessTime, bool cancel)
            : base()
        {
            this.recordCount = recordCount;
            this.currentRecordIndex = currentRecordIndex;
            this.currentFeature = currentFeature;
            this.startProcessTime = startProcessTime;
            this.cancel = cancel;
        }

        /// <summary>
        /// Gets the total record count to build rTree index.
        /// </summary>
        public int RecordCount
        {
            get { return recordCount; }
        }

        /// <summary>
        /// Gets the current record index for building rTree index.
        /// </summary>
        public int CurrentRecordIndex
        {
            get { return currentRecordIndex; }
        }

        /// <summary>
        /// Gets the current feature for building rTree index.
        /// </summary>
        public Feature CurrentFeature
        {
            get { return currentFeature; }
        }

        /// <summary>
        /// Gets the starting process time for building the index.
        /// </summary>
        public DateTime StartProcessTime
        {
            get { return startProcessTime; }
        }

        /// <summary>
        /// Gets or sets to see if we need to cancel the building index of current record.
        /// </summary>
        public bool Cancel
        {
            get { return cancel; }
            set { cancel = value; }
        }
    }
}