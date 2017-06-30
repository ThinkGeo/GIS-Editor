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
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class StyleSetting : Setting
    {
        //private Style defaultStyle;
        private StylePlugin stylePlugin;

        public StyleSetting(StylePlugin stylePlugin)
        {
            this.stylePlugin = stylePlugin;
        }

        public ObservableCollection<Style> DefaultStyles
        {
            get { return stylePlugin.StyleCandidates; }
        }

        public StylePlugin StylePlugin
        {
            get { return stylePlugin; }
            set { stylePlugin = value; }
        }

        //[DataMember]
        //public Style DefaultStyle
        //{
        //    get
        //    {
        //        return defaultStyle;
        //    }
        //    set
        //    {
        //        defaultStyle = value;
        //    }
        //}

        [DataMember]
        public bool UseRandomColors
        {
            get { return stylePlugin.UseRandomColor; }
            set { stylePlugin.UseRandomColor = value; }
        }

        public override Dictionary<string, string> SaveState()
        {
            Dictionary<string, string> resultXml = base.SaveState();
            resultXml["UseRandomColors"] = UseRandomColors.ToString();
            XElement xElement = new XElement("DefaultStyles", DefaultStyles.Select(d => XElement.Parse(GisEditor.Serializer.Serialize(d))));
            resultXml["DefaultStyles"] = xElement.ToString();
            return resultXml;
        }

        public override void LoadState(Dictionary<string, string> state)
        {
            base.LoadState(state);
            PluginHelper.RestoreBoolean(state, "UseRandomColors", v => UseRandomColors = v);
            bool isCleared = false;
            PluginHelper.Restore(state, "DefaultStyle", v =>
            {
                Style style = (Style)GisEditor.Serializer.Deserialize(v);
                stylePlugin.StyleCandidates.Clear();
                isCleared = true;
                stylePlugin.StyleCandidates.Add(style);
            });
            PluginHelper.Restore(state, "DefaultStyles", v =>
            {
                if (!string.IsNullOrEmpty(v))
                {
                    XElement xElement = XElement.Parse(v);
                    var styles = xElement.Elements();
                    if (styles.Count() > 0)
                    {
                        if (!isCleared) stylePlugin.StyleCandidates.Clear();
                        foreach (var item in styles)
                        {
                            Style style = GisEditor.Serializer.Deserialize(item.ToString()) as Style;
                            stylePlugin.StyleCandidates.Add(style);
                        }
                    }
                }
            });
        }
    }
}