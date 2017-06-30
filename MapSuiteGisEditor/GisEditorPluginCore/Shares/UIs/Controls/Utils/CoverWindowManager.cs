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


using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Input;
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class CoverWindowManager
    {
        private static List<CoverWindow> windows = new List<CoverWindow>();
        private static bool isShowing;

        static CoverWindowManager()
        {
            foreach (var screen in Screen.AllScreens)
            {
                CoverWindow window = new CoverWindow(screen);

                window.KeyUp += (s, e) =>
                {
                    if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
                    {
                        HideAllCoverWindows();
                    }
                };

                windows.Add(window);
            }
        }

        public static void CoverUpAllScreens()
        {
            if (!isShowing)
            {
                var allScreens = Screen.AllScreens.ToArray();
                if (allScreens.Length == windows.Count)
                {
                    for (int i = 0; i < allScreens.Length; i++)
                    {
                        var currentWindow = windows[i];
                        currentWindow.ResizeToScreen(allScreens[i]);
                        currentWindow.Topmost = true;
                        currentWindow.Show();
                        currentWindow.Focus();
                    }
                }
                else
                {
                    foreach (var window in windows)
                    {
                        window.Topmost = true;
                        window.Show();
                        window.Focus();
                    }
                }

                isShowing = true;
            }
        }

        public static void HideAllCoverWindows()
        {
            if (isShowing)
            {
                foreach (var window in windows)
                {
                    window.Hide();
                    window.ReleaseMouseCapture();
                }

                isShowing = false;
            }
        }
    }
}
