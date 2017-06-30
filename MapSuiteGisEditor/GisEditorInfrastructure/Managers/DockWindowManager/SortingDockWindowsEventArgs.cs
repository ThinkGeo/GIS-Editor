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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    public class SortingDockWindowsEventArgs : EventArgs
    {
        private Collection<DockWindow> dockWindows;
        private bool cancel;

        public SortingDockWindowsEventArgs(IEnumerable<DockWindow> dockWindows)
        {
            this.dockWindows = new Collection<DockWindow>();
            foreach (var item in dockWindows)
            {
                this.dockWindows.Add(item);
            }
        }

        public Collection<DockWindow> DockWindows
        {
            get { return dockWindows; }
        }

        public bool Cancel
        {
            get { return cancel; }
            set { cancel = value; }
        }
    }
}
