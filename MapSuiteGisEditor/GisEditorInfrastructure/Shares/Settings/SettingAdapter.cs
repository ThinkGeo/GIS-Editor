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
using System.Xml.Linq;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    internal abstract class SettingAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingAdapter" /> class.
        /// </summary>
        protected SettingAdapter()
        { }

        /// <summary>
        /// To the XML.
        /// </summary>
        /// <param name="settingDictionary">The setting dictionary.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static XElement ToXml(Dictionary<string, string> settingDictionary, Type type)
        {
            if (settingDictionary.Count == 0) return null;

            string typeFullName = type.FullName;
            XElement newElement = new XElement("State", new XAttribute("Name", typeFullName));

            foreach (var item in settingDictionary)
            {
                var innerContent = item.Value;
                if (!string.IsNullOrEmpty(innerContent))
                {
                    object contentObject = innerContent;
                    if (item.Value.StartsWith("<") && item.Value.EndsWith(">"))
                    {
                        try
                        {
                            contentObject = XElement.Parse(item.Value);
                        }
                        catch (Exception e)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                        }
                    }
                    newElement.Add(new XElement("Item", new XAttribute("Key", item.Key), contentObject));
                }
            }

            return newElement;
        }

        /// <summary>
        /// Froms the XML.
        /// </summary>
        /// <param name="stateXml">The state XML.</param>
        /// <returns></returns>
        public static Dictionary<string, string> FromXml(XElement stateXml)
        {
            Dictionary<string, string> stateDictionary = new Dictionary<string, string>();
            if (stateXml != null)
            {
                foreach (var itemXml in stateXml.Elements("Item"))
                {
                    var keyAttri = itemXml.Attribute("Key");
                    if (keyAttri != null)
                    {
                        var firstElement = itemXml.Elements().FirstOrDefault();
                        string innerXml = string.Empty;
                        if (firstElement != null)
                        {
                            innerXml = firstElement.ToString();
                        }
                        else
                        {
                            innerXml = itemXml.Value;
                        }
                        stateDictionary.Add(keyAttri.Value, innerXml);
                    }
                }
            }

            return stateDictionary;
        }

        public abstract Dictionary<string, string> GetSetting(StorableSettings settings);

        public abstract Type GetCoreType();
    }

    [Serializable]
    internal class InfrastructureSettingAdapter : SettingAdapter
    {
        private IStorableSettings setting;

        public InfrastructureSettingAdapter(IStorableSettings setting)
        {
            this.setting = setting;
        }

        public override Dictionary<string, string> GetSetting(StorableSettings settings)
        {
            return settings.GlobalSettings;
        }

        public override Type GetCoreType()
        {
            return setting.GetType();
        }
    }

    [Serializable]
    internal class ProjectSettingAdapter : SettingAdapter
    {
        private IStorableSettings setting;

        public ProjectSettingAdapter(IStorableSettings setting)
        {
            this.setting = setting;
        }

        public override Dictionary<string, string> GetSetting(StorableSettings settings)
        {
            return settings.ProjectSettings;
        }

        public override Type GetCoreType()
        {
            return setting.GetType();
        }
    }
}