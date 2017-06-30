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
using System.Runtime.Serialization;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Serialize;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// EditorOptionViewModel
    /// EditorOptionUserControl
    /// </summary>
    [Serializable]
    public class EditorSetting : Setting
    {
        private bool isAttributePrompted;
        private CompositeStyle editCompositeStyle;
        private int vertexCountInQuarter = 9;

        public EditorSetting()
        {
            IsAttributePrompted = true;
            AreaStyle editAreaStyle = new AreaStyle(new GeoPen(GeoColor.StandardColors.Pink, 3));
            editAreaStyle.Name = "Edit Area Style";
            LineStyle editLineStyle = new LineStyle(new GeoPen(GeoColor.FromArgb(255, 0, 0, 255), 3));
            editLineStyle.Name = "Edit Line Style";
            PointStyle editPointStyle = new PointStyle();
            editPointStyle.Name = "Edit Point Style";
            editPointStyle.SymbolPen = new GeoPen(GeoColor.FromArgb(255, 0, 0, 255), 5);

            editCompositeStyle = new CompositeStyle();
            editCompositeStyle.Styles.Add(editAreaStyle);
            editCompositeStyle.Styles.Add(editLineStyle);
            editCompositeStyle.Styles.Add(editPointStyle);
        }

        [DataMember]
        public bool IsAttributePrompted
        {
            get { return isAttributePrompted; }
            set { isAttributePrompted = value; }
        }

        public CompositeStyle EditCompositeStyle
        {
            get { return editCompositeStyle; }
            set { editCompositeStyle = value; }
        }

        [DataMember]
        public int VertexCountInQuarter
        {
            get { return vertexCountInQuarter; }
            set
            {
                if (value > 0)
                {
                    vertexCountInQuarter = value;

                    if (GisEditor.ActiveMap != null
                        && GisEditor.ActiveMap.TrackOverlay != null)
                        GisEditor.ActiveMap.TrackOverlay.VertexCountInQuarter = vertexCountInQuarter;
                }
            }
        }

        public override Dictionary<string, string> SaveState()
        {
            Dictionary<string, string> state = base.SaveState();
            state.Add("IsAttributePrompted", IsAttributePrompted.ToString());
            state.Add("VertexCountInQuarter", VertexCountInQuarter.ToString());

            try
            {
                GeoSerializer serializer = new GeoSerializer();
                state.Add("EditCompositeStyle", serializer.Serialize(EditCompositeStyle));
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            return state;
        }

        public override void LoadState(Dictionary<string, string> state)
        {
            base.LoadState(state);
            PluginHelper.RestoreBoolean(state, "IsAttributePrompted", v => IsAttributePrompted = v);
            PluginHelper.RestoreInteger(state, "VertexCountInQuarter", v => VertexCountInQuarter = v);
            PluginHelper.Restore(state, "EditCompositeStyle", v =>
            {
                GeoSerializer serializer = new GeoSerializer();
                try
                {
                    EditCompositeStyle = (CompositeStyle)serializer.Deserialize(v);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
            });
        }
    }
}