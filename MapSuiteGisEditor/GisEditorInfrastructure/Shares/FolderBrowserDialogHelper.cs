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
using System.Windows.Forms;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    internal static class FolderBrowserDialogHelper
    {
        private static string lastSelectedFolder = string.Empty;

        /// <summary>
        /// Opens the dialog.
        /// </summary>
        /// <param name="openedAction">The opened action.</param>
        /// <param name="initializeAction">The initialize action.</param>
        public static void OpenDialog(Action<FolderBrowserDialog, DialogResult> openedAction, Action<FolderBrowserDialog> initializeAction = null)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = GisEditor.LanguageManager.GetStringResource("FolderBrowserDialogHelperSelectFolderDescritpion");
                if (Directory.Exists(lastSelectedFolder))
                {
                    if (initializeAction != null) initializeAction(dialog);
                    dialog.SelectedPath = lastSelectedFolder;
                }
                var result = dialog.ShowDialog();
                if (result == DialogResult.OK || result == DialogResult.Yes)
                {
                    lastSelectedFolder = dialog.SelectedPath;
                }
                if (openedAction != null) openedAction(dialog, result);
            }
        }
    }
}