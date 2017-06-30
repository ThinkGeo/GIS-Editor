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
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class Cursors
    {
        private static readonly string uriFormat = "/GisEditorPluginCore;component/Images/{0}";
        private static Cursor pointCur;
        private static Cursor lineCur;
        private static Cursor polygonCur;
        private static Cursor circleCur;
        private static Cursor squareCur;
        private static Cursor rectCur;
        private static Cursor ellipseCur;
        private static Cursor textCur;

        internal static Cursor DrawPoint
        {
            get
            {
                if (pointCur == null)
                {
                    pointCur = new Cursor(GetCurStream(string.Format(uriFormat, "drawPoint.cur")));
                }

                return pointCur;
            }
        }

        internal static Cursor DrawLine
        {
            get
            {
                if (lineCur == null)
                {
                    lineCur = new Cursor(GetCurStream(string.Format(uriFormat, "drawLine.cur")));
                }

                return lineCur;
            }
        }

        internal static Cursor DrawPolygon
        {
            get
            {
                if (polygonCur == null)
                {
                    polygonCur = new Cursor(GetCurStream(string.Format(uriFormat, "drawPolygon.cur")));
                }

                return polygonCur;
            }
        }

        internal static Cursor DrawCircle
        {
            get
            {
                if (circleCur == null)
                {
                    circleCur = new Cursor(GetCurStream(string.Format(uriFormat, "drawCircle.cur")));
                }

                return circleCur;
            }
        }

        internal static Cursor DrawSquare
        {
            get
            {
                if (squareCur == null)
                {
                    squareCur = new Cursor(GetCurStream(string.Format(uriFormat, "drawSquare.cur")));
                }

                return squareCur;
            }
        }

        internal static Cursor DrawRectangle
        {
            get
            {
                if (rectCur == null)
                {
                    rectCur = new Cursor(GetCurStream(string.Format(uriFormat, "drawRectangle.cur")));
                }

                return rectCur;
            }
        }

        internal static Cursor DrawEllipse
        {
            get
            {
                if (ellipseCur == null)
                {
                    ellipseCur = new Cursor(GetCurStream(string.Format(uriFormat, "drawEllipse.cur")));
                }

                return ellipseCur;
            }
        }

        internal static Cursor DrawText
        {
            get
            {
                if (textCur == null)
                {
                    textCur = new Cursor(GetCurStream(string.Format(uriFormat, "Text Annotation Tool.cur")));
                }

                return textCur;
            }
        }

        internal static Cursor Select
        {
            get
            {
                return System.Windows.Input.Cursors.Arrow;
            }
        }

        private static Stream GetCurStream(string uri)
        {
            var streamInfo = Application.GetResourceStream(new Uri(uri, UriKind.RelativeOrAbsolute));
            return streamInfo.Stream;
        }
    }
}