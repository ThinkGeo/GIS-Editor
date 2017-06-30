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
using System.ComponentModel.Composition;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents a plugin for project.
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(ProjectPlugin))]
    public abstract class ProjectPlugin : Plugin
    {
        [NonSerialized]
        private ImageSource openProjectIcon;

        [NonSerialized]
        private ImageSource saveProjectIcon;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectPlugin" /> class.
        /// </summary>
        protected ProjectPlugin()
        {
            openProjectIcon = new BitmapImage(new Uri("/MapSuiteGisEditor;component/Images/folder-open_32.png", UriKind.RelativeOrAbsolute));
            saveProjectIcon = new BitmapImage(new Uri("/MapSuiteGisEditor;component/Images/save_as.png", UriKind.RelativeOrAbsolute));
        }

        /// <summary>
        /// Gets the open project icon.
        /// </summary>
        /// <returns>An icon for current project plugin</returns>
        public ImageSource GetOpenProjectIcon()
        {
            return GetOpenProjectIconCore();
        }

        /// <summary>
        /// Gets the open project icon core.
        /// </summary>
        /// <returns>An icon for open project</returns>
        protected virtual ImageSource GetOpenProjectIconCore()
        {
            return openProjectIcon;
        }

        /// <summary>
        /// Gets the save project icon.
        /// </summary>
        /// <returns>An icon for saving project</returns>
        public ImageSource GetSaveProjectIcon()
        {
            return GetSaveProjectIconCore();
        }

        /// <summary>
        /// Gets the save project icon core.
        /// </summary>
        /// <returns>An icon for saving project</returns>
        protected virtual ImageSource GetSaveProjectIconCore()
        {
            return saveProjectIcon;
        }

        /// <summary>
        /// Gets the short name of the project.
        /// </summary>
        /// <param name="projectUri">The project URI.</param>
        /// <returns>The project shut name</returns>
        public string GetProjectShortName(Uri projectUri)
        {
            return GetProjectShortNameCore(projectUri);
        }

        /// <summary>
        /// Gets the project short name core.
        /// </summary>
        /// <param name="projectUri">The project URI.</param>
        /// <returns>The project short name</returns>
        protected virtual string GetProjectShortNameCore(Uri projectUri)
        {
            return GetProjectFullName(projectUri);
        }

        /// <summary>
        /// Gets the full name of the project.
        /// </summary>
        /// <param name="projectUri">The project URI.</param>
        /// <returns>The project full name</returns>
        public string GetProjectFullName(Uri projectUri)
        {
            return GetProjectFullNameCore(projectUri);
        }

        /// <summary>
        /// Gets the project full name core.
        /// </summary>
        /// <param name="projectUri">The project URI.</param>
        /// <returns>The project full name</returns>
        protected virtual string GetProjectFullNameCore(Uri projectUri)
        {
            if (projectUri != null)
            {
                return projectUri.AbsolutePath;
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the project save as URI.
        /// </summary>
        /// <returns>An project URI result</returns>
        public ProjectSaveAsResult GetProjectSaveAsUri()
        {
            return GetProjectSaveAsUriCore();
        }

        /// <summary>
        /// Gets the project save as URI core.
        /// </summary>
        /// <returns>An project URI result</returns>
        protected abstract ProjectSaveAsResult GetProjectSaveAsUriCore();

        /// <summary>
        /// Gets the package save as URI.
        /// </summary>
        /// <returns>An package URI result</returns>
        public ProjectSaveAsResult GetPackageSaveAsUri()
        {
            return GetPackageSaveAsUriCore();
        }

        /// <summary>
        /// Gets the package save as URI core.
        /// </summary>
        /// <returns>An package URI result</returns>
        protected abstract ProjectSaveAsResult GetPackageSaveAsUriCore();

        /// <summary>
        /// Projects the exists.
        /// </summary>
        /// <param name="projectUri">The project URI.</param>
        /// <returns>project exists</returns>
        public bool ProjectExists(Uri projectUri)
        {
            if (projectUri != null) return ProjectExistsCore(projectUri);
            else return false;
        }

        /// <summary>
        /// Projects the exists core.
        /// </summary>
        /// <param name="projectUri">The project URI.</param>
        /// <returns>project exists</returns>
        protected abstract bool ProjectExistsCore(Uri projectUri);

        /// <summary>
        /// Loads the project stream.
        /// </summary>
        /// <param name="projectStreamInfo">The project stream info.</param>
        public void LoadProjectStream(ProjectStreamInfo projectStreamInfo)
        {
            LoadProjectStreamCore(projectStreamInfo);
        }

        /// <summary>
        /// Loads the project stream core.
        /// </summary>
        /// <param name="projectStreamInfo">The project stream info.</param>
        protected abstract void LoadProjectStreamCore(ProjectStreamInfo projectStreamInfo);

        /// <summary>
        /// Saves the project stream.
        /// </summary>
        /// <param name="projectStreamInfo">The project stream info.</param>
        public void SaveProjectStream(ProjectStreamInfo projectStreamInfo)
        {
            SaveProjectStreamCore(projectStreamInfo);
        }

        /// <summary>
        /// Saves the project stream core.
        /// </summary>
        /// <param name="projectStreamInfo">The project stream info.</param>
        protected abstract void SaveProjectStreamCore(ProjectStreamInfo projectStreamInfo);
    }
}