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
using System.IO;
using System.Threading;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    [InheritedExport(typeof(FileProjectPlugin))]
    public class FileProjectPlugin : ProjectPlugin
    {
        private static readonly string protectedFileFilter = "Protected Project File (*.tgproj) | *.tgproj";

        [NonSerialized]
        private SaveFileDialog<SaveFileDialogAddOn> saveFileDialog;

        [NonSerialized]
        private OpenFileDialog<OpenFileDialogAddOn> openFileDialog;

        [NonSerialized]
        private ReaderWriterLockSlim lockObject;

        public FileProjectPlugin()
        {
            lockObject = new ReaderWriterLockSlim();
            openFileDialog = new OpenFileDialog<OpenFileDialogAddOn>();
            openFileDialog.InitialDirectory = FolderHelper.GetGisEditorFolder();
            openFileDialog.Filter = "Project File (*.tgproj) | *.tgproj";
            openFileDialog.Multiselect = false;
            openFileDialog.FileDlgStartLocation = AddonWindowLocation.Right;
            openFileDialog.FileDlgDefaultViewMode = NativeMethods.FolderViewMode.Default;

            saveFileDialog = new SaveFileDialog<SaveFileDialogAddOn>();
            saveFileDialog.InitialDirectory = FolderHelper.GetGisEditorFolder();
            saveFileDialog.CheckFileExists = false;
            saveFileDialog.FileDlgStartLocation = AddonWindowLocation.Right;
            saveFileDialog.FileDlgDefaultViewMode = NativeMethods.FolderViewMode.Default;
            Description = GisEditor.LanguageManager.GetStringResource("FileProjectPluginDescription");
            Name = GisEditor.LanguageManager.GetStringResource("FileProjectPluginName");

            Index = 0;

            SaveFileDialogAddOn.ChildControlLoaded += SaveFileDialogAddOn_ControlLoaded;
        }

        protected override bool ProjectExistsCore(Uri projectPath)
        {
            return File.Exists(projectPath.LocalPath);
        }

        protected override void LoadProjectStreamCore(ProjectStreamInfo projectStreamInfo)
        {
            if (projectStreamInfo.Uri == null)
            {
#if GISEditorUnitTest
                projectStreamInfo.Uri = new Uri(Path.GetFullPath(@"..\..\..\UnitTestData\tmpProject.tgproj"));
#else
                if (openFileDialog.ShowDialog().GetValueOrDefault())
                {
                    string projectFilePath = openFileDialog.FileName;

                    projectStreamInfo.Uri = new Uri(projectFilePath);
                }
                else projectStreamInfo.Uri = null;
#endif
            }

            if (ProjectExists(projectStreamInfo.Uri))
            {
                MemoryStream stream = new MemoryStream(File.ReadAllBytes(projectStreamInfo.Uri.LocalPath));
                stream.Seek(0, SeekOrigin.Begin);
                projectStreamInfo.Stream = stream;
            }
        }

        protected override string GetProjectShortNameCore(Uri projectPath)
        {
            string projectName = "Project1";

            if (projectPath != null)
            {
                projectName = Path.GetFileName(projectPath.LocalPath);
            }

            return projectName;
        }

        protected override string GetProjectFullNameCore(Uri projectPath)
        {
            if (projectPath != null)
            {
                return projectPath.LocalPath;
            }
            return string.Empty;
        }

        protected override ProjectSaveAsResult GetProjectSaveAsUriCore()
        {
            ProjectSaveAsResult result = new ProjectSaveAsResult();
            saveFileDialog.Filter = "Project File (*.tgproj) | *.tgproj";
            GetSaveFileDialogResult(result);

            return result;
        }

        protected override ProjectSaveAsResult GetPackageSaveAsUriCore()
        {
            ProjectSaveAsResult result = new ProjectSaveAsResult();
            saveFileDialog.Filter = "MapSuite GIS Editor Project Package (*.zip)|*.zip";
            GetSaveFileDialogResult(result);

            return result;
        }

        protected override void SaveProjectStreamCore(ProjectStreamInfo projectStreamInfo)
        {
            if (projectStreamInfo.Stream != null && GisEditor.ProjectManager.CanSaveProject(projectStreamInfo))
            {
                byte[] buffer = new byte[projectStreamInfo.Stream.Length];
                projectStreamInfo.Stream.Read(buffer, 0, buffer.Length);

                try
                {
                    lockObject.EnterWriteLock();
                    File.WriteAllBytes(projectStreamInfo.Uri.LocalPath, buffer);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
                finally
                {
                    lockObject.ExitWriteLock();
                }
            }
        }

        private void SaveFileDialogAddOn_ControlLoaded(object sender, EventArgs e)
        {
            bool hasPassword = GisEditor.ProjectManager.HasPassword();

            if (hasPassword) saveFileDialog.ChildWnd.keepPassword.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
            saveFileDialog.ChildWnd.keepPassword.IsChecked = hasPassword;
            saveFileDialog.ChildWnd.keepPassword.IsEnabled = hasPassword;
        }

        private void GetSaveFileDialogResult(ProjectSaveAsResult result)
        {
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                result.KeepPasswords = saveFileDialog.ChildWnd.keepPassword.IsChecked.Value;

                string projectFilePath = saveFileDialog.FileName;
                result.Uri = new Uri(projectFilePath);
            }
            else result.Canceled = true;
        }
    }
}