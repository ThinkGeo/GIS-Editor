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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class ScriptFilterCondition : FilterCondition
    {
        private FilterStyleScriptType scriptType;

        public ScriptFilterCondition()
        { }

        public FilterStyleScriptType ScriptType
        {
            get { return scriptType; }
            set
            {
                scriptType = value;
            }
        }

        private bool IsMatchCore(Feature feature)
        {
            if (string.IsNullOrEmpty(Expression))
            {
                return true;
            }
            else
            {
                return GetMatchingFeatures(GetFeature(feature)).Count > 0;
            }
        }

        protected override Collection<Feature> GetMatchingFeaturesCore(IEnumerable<Feature> features)
        {
            var resultFeatures = GetFeaturesByScript(features);
            return new Collection<Feature>(resultFeatures.AsParallel().Where(f => IsMatchCore(f)).ToList());
        }

        private string GetResourceString(string resourceName)
        {
            string script = string.Empty;
            Stream resouceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            using (StreamReader streamReader = new StreamReader(resouceStream))
            {
                script = streamReader.ReadToEnd();
            }

            return script;
        }

        private string GetMatchScript(string functionString)
        {
            if (string.IsNullOrEmpty(Expression)) return string.Empty;

            functionString = GetResourceString(functionString);

            string script = Expression;
            string[] scripts = script.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            StringBuilder srciptBuilder = new StringBuilder();
            foreach (var subScript in scripts)
            {
                srciptBuilder.AppendLine("  " + subScript);
            }

            script = functionString.Replace("[expression]", script);
            return script;
        }

        private Collection<Feature> GetFeaturesByScript(IEnumerable<Feature> features)
        {
            string filterFunction = string.Empty;
            string script = string.Empty;
            DlrLanguage dlrLanguage = null;

            switch (scriptType)
            {
                case FilterStyleScriptType.Ruby:
                    filterFunction = "ThinkGeo.MapSuite.WpfDesktopEdition.Extension.DlrLanguages.CodeTemplates.RubyFilterFunction.rb";
                    dlrLanguage = new RubyDlrLanguage();
                    break;

                case FilterStyleScriptType.Python:
                    filterFunction = "ThinkGeo.MapSuite.WpfDesktopEdition.Extension.DlrLanguages.CodeTemplates.PythonFilterFunction.py";
                    dlrLanguage = new PythonDlrLanguage();
                    break;

                case FilterStyleScriptType.CSharp:
                    filterFunction = "ThinkGeo.MapSuite.WpfDesktopEdition.Extension.DlrLanguages.CodeTemplates.CSharpFilterFunction.cs";
                    dlrLanguage = new CSharpDlrLanguage();
                    break;

                default:
                    break;
            }

            script = GetMatchScript(filterFunction);
            if (dlrLanguage != null && !string.IsNullOrEmpty(script))
            {
                dlrLanguage.Variables.Add("Features", features);
                dlrLanguage.Script = script;
                return (Collection<Feature>)dlrLanguage.RunScript();
            }

            return new Collection<Feature>(features.ToList());
        }

        private IEnumerable<Feature> GetFeature(Feature feature)
        {
            yield return feature;
        }
    }
}