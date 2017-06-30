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
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// UnhandledErrorReportViewModel
    /// UnhandledErrorReportOptionUserControl
    /// </summary>
    [Serializable]
    public class ErrorReportSetting : Setting
    {
        private bool needReportUnhandledErrors;

        public ErrorReportSetting()
        {
            //pop up by default
            NeedReportUnhandledErrors = true;
        }

        [DataMember]
        public bool NeedReportUnhandledErrors
        {
            get { return needReportUnhandledErrors; }
            set { needReportUnhandledErrors = value; }
        }

        public override Dictionary<string, string> SaveState()
        {
            Dictionary<string, string> state = base.SaveState();
            state["NeedReportUnhandledErrors"] = NeedReportUnhandledErrors.ToString();
            return state;
        }

        public override void LoadState(Dictionary<string, string> state)
        {
            base.LoadState(state);
            PluginHelper.RestoreBoolean(state, "NeedReportUnhandledErrors", v => NeedReportUnhandledErrors = v);
        }
    }
}