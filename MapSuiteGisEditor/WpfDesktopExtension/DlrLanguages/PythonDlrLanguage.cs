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
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using ThinkGeo.MapSuite.WpfDesktop.Extension.Properties;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class PythonDlrLanguage : DlrLanguage
    {
        public PythonDlrLanguage()
            : base()
        {
            FileFiltersCore = "Python Code File|*.py";
            NameCore = "Python Script";
            Script = Resources.PythonSampleCode;
        }

        protected override object RunScriptCore()
        {
            ScriptEngine engine = Python.CreateEngine();
            ScriptScope scope = engine.CreateScope();

            ScriptSource sourceCode = engine.CreateScriptSourceFromString(Script);
            foreach (var item in Variables)
            {
                scope.SetVariable(item.Key, item.Value);
            }
            return sourceCode.Execute(scope);
        }
    }
}
