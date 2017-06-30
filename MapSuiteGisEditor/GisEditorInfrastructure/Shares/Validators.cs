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

namespace ThinkGeo.MapSuite.GisEditor
{
    internal static class Validators
    {
        //public static void CheckIsProjectExist(Uri uri, ProjectManager projectMananger)
        //{
        //    if (uri != null && !projectMananger.ProjectExists(uri))
        //    {
        //        throw new FileNotFoundException("Project file not exists.", "uri");
        //    }
        //}

        /// <summary>
        /// Checks the is project exist.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="projectPluginManager">The project plugin manager.</param>
        /// <exception cref="System.IO.FileNotFoundException">Project file not exists.;uri</exception>
        public static void CheckIsProjectExist(Uri uri, ProjectPluginManager projectPluginManager)
        {
            if (uri != null && !projectPluginManager.ProjectExists(uri) && uri.AbsolutePath != "blank" && !uri.Scheme.ToLower().Contains("backup"))
            {
                throw new FileNotFoundException("Project file not exists.", "uri");
            }
        }

        /// <summary>
        /// Checks the are same projects opened.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="projectPluginManager">The project plugin manager.</param>
        /// <exception cref="System.ArgumentException">This project is already open.;uri</exception>
        public static void CheckAreSameProjectsOpened(Uri uri, ProjectPluginManager projectPluginManager)
        {
            if (projectPluginManager.IsLoaded && projectPluginManager.ProjectUri != null
                && uri != null
                && projectPluginManager.ProjectUri.AbsolutePath.Equals(uri.AbsolutePath, StringComparison.OrdinalIgnoreCase)
                && !projectPluginManager.ProjectUri.AbsolutePath.Equals("blank", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(GisEditor.LanguageManager.GetStringResource("GisEditorUserControlAlreadyOpenMessage"), "uri");
            }
        }
    }
}