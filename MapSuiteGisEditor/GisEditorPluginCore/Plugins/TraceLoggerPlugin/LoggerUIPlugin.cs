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


using System.Linq;
using System.Windows;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class LoggerUIPlugin : UIPlugin
    {
        private TraceLoggerRibbonGroup traceLoggerRibbonGroup;
        private RibbonEntry entry;

        public LoggerUIPlugin()
        {
            traceLoggerRibbonGroup = new TraceLoggerRibbonGroup();
            entry = new RibbonEntry(traceLoggerRibbonGroup, RibbonTabOrder.Help, "HelpRibbonTabHeader");
            Index = UIPluginOrder.LoggerUIPlugin;
            IsActive = false;
        }

        protected override void LoadCore()
        {
            if (!RibbonEntries.Contains(entry))
            {
                RibbonEntries.Add(entry);
            }
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            if (RibbonEntries.Contains(entry))
            {
                RibbonEntries.Remove(entry);
            }
        }

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            foreach (var map in GisEditor.GetMaps())
            {
                foreach (var overlay in map.Overlays.OfType<TileOverlay>())
                {
                    overlay.DrawingException -= Overlay_DrawingException;
                    overlay.DrawingException += Overlay_DrawingException;
                }
            }

            base.RefreshCore(currentMap, refreshArgs);
        }

        private void Overlay_DrawingException(object sender, DrawingExceptionTileOverlayEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Warning, e.Exception.Message, e.Exception);
            });
        }
    }
}
