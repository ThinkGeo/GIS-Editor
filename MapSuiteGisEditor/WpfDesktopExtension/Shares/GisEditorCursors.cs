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
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public static class GisEditorCursors
    {
        private static readonly string cursorPath = "/ThinkGeo.MapSuite.WpfDesktop.Extension;component/Images/{0}";

        public readonly static Cursor Normal = Cursors.Arrow;
        public readonly static Cursor Edit = Cursors.Arrow;
        public readonly static Cursor Pan = new Cursor(GetManifestResource("cursor_hand.cur"));
        public readonly static Cursor Grab = new Cursor(GetManifestResource("cursor_drag_hand.cur"));
        public readonly static Cursor TrackZoom = new Cursor(GetManifestResource("track_zoom.cur"));
        public readonly static Cursor DrawCircle = new Cursor(GetManifestResource("drawCircle.cur"));
        public readonly static Cursor DrawEllipse = new Cursor(GetManifestResource("drawEllipse.cur"));
        public readonly static Cursor DrawLine = new Cursor(GetManifestResource("drawLine.cur"));
        public readonly static Cursor DrawPoint = new Cursor(GetManifestResource("drawPoint.cur"));
        public readonly static Cursor DrawPolygon = new Cursor(GetManifestResource("drawPolygon.cur"));
        public readonly static Cursor DrawRectangle = new Cursor(GetManifestResource("drawRectangle.cur"));
        public readonly static Cursor DrawSqure = new Cursor(GetManifestResource("drawSquare.cur"));
        public readonly static Cursor DrawText = new Cursor(GetManifestResource("drawText.cur"));
        public readonly static Cursor Cross = new Cursor(GetManifestResource("cursor_cross.cur"));
        public readonly static Cursor Identify = Cursors.Help;

        private static Stream GetManifestResource(string fileName)
        {
            return Application.GetResourceStream(new Uri(string.Format(CultureInfo.InvariantCulture, cursorPath, fileName), UriKind.RelativeOrAbsolute)).Stream;
        }
    }
}