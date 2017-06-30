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
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class ProcessUtils
    {
        public static void OpenPath(string path, bool select = true)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo("explorer.exe");
            processStartInfo.Arguments = "/e,";
            if (select) processStartInfo.Arguments += "/select,";
            processStartInfo.Arguments += path;
            Process.Start(processStartInfo);
        }

        public static void OpenUri(Uri uri)
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri));
        }
    }
}
