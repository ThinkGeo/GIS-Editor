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
using System.Reflection;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class AliasExtension
    {
        [Obfuscation]
        private static Dictionary<string, Dictionary<string, string>> aliases;

        static AliasExtension()
        {
            aliases = new Dictionary<string, Dictionary<string, string>>();
        }

        public static Dictionary<string, Dictionary<string, string>> Aliases
        {
            get { return aliases; }
        }

        public static string GetColumnAlias(this FeatureSource featureSource, string columnName)
        {
            return GetAliasInternal(columnName, featureSource.Id);
        }

        public static void SetColumnAlias(this FeatureSource featureSource, string columnName, string alias)
        {
            SetColumnAlias(featureSource, columnName, alias, null);
        }

        public static void SetColumnAlias(this FeatureSource featureSource, string columnName, string alias, Action aliasDuplicated)
        {
            SetAliasInternal(featureSource.Id, columnName, alias, aliasDuplicated);
        }

        private static string GetAliasInternal(string columnName, string aliasKey)
        {
            string alias = columnName;
            if (aliases.ContainsKey(aliasKey))
            {
                alias = aliases[aliasKey]
                    .Where(c => c.Key.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Value)
                    .FirstOrDefault();
            }

            return string.IsNullOrEmpty(alias) ? columnName : alias;
        }

        private static void SetAliasInternal(string aliasKey, string columnName, string alias, Action aliasDuplicated)
        {
            Dictionary<string, string> resultAliases = null;
            if (aliases.ContainsKey(aliasKey))
            {
                resultAliases = aliases[aliasKey];
            }
            else
            {
                resultAliases = new Dictionary<string, string>();
                aliases[aliasKey] = resultAliases;
            }

            bool isDuplicated = resultAliases.Any(a => a.Value.Equals(alias, StringComparison.OrdinalIgnoreCase) && !a.Key.Equals(columnName, StringComparison.OrdinalIgnoreCase));
            if (isDuplicated)
            {
                if (aliasDuplicated != null)
                {
                    aliasDuplicated();
                }
            }
            else
            {
                resultAliases[columnName] = alias;
            }
        }
    }
}