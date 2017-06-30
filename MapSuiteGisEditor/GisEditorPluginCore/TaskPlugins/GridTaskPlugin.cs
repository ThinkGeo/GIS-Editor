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
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class GridTaskPlugin : TaskPlugin
    {
        private string outputPathFileName;
        private GridDefinition gridDefinition;
        private GridInterpolationModel gridInterpolationModel;

        public GridTaskPlugin()
        {
        }

        public string OutputPathFileName
        {
            get { return outputPathFileName; }
            set { outputPathFileName = value; }
        }

        public GridInterpolationModel GridInterpolationModel
        {
            get { return gridInterpolationModel; }
            set { gridInterpolationModel = value; }
        }

        public GridDefinition GridDefinition
        {
            get { return gridDefinition; }
            set { gridDefinition = value; }
        }

        protected override void LoadCore()
        {
            Name = GisEditor.LanguageManager.GetStringResource("GridTaskPluginOperationText");
            Description = Name + " " + GisEditor.LanguageManager.GetStringResource("TaskPluginCreatingText") + " " + Path.GetFileName(OutputPathFileName);
        }

        protected override void RunCore()
        {
            try
            {
                using (FileStream fileStream = File.Create(OutputPathFileName))
                {
                    try
                    {
                        GridFeatureSource.GeneratingGrid += GridFeatureSource_GeneratingGrid;
                        GridFeatureLayer.GenerateGrid(GridDefinition, GridInterpolationModel, fileStream);
                    }
                    catch (Exception e)
                    {
                        GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                        if(e is OutOfMemoryException) throw e;
                    }
                    finally
                    {
                        GridFeatureSource.GeneratingGrid -= GridFeatureSource_GeneratingGrid;
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                var args = new UpdatingTaskProgressEventArgs(TaskState.Canceled);
                args.Error = new ExceptionInfo("Small grid cell size causes out of memory exception. Please set a larger one.", string.Empty, string.Empty);
                OnUpdatingProgress(args);
            }
            catch (Exception ex)
            {
                var args = new UpdatingTaskProgressEventArgs(TaskState.Error);
                args.Error = new ExceptionInfo(ex.Message, string.Empty, string.Empty);
                OnUpdatingProgress(args);
            }
        }

        private void GridFeatureSource_GeneratingGrid(object sender, GeneratingGridFeatureSourceEventArgs e)
        {
            if (e.GridPathFilename.Equals(OutputPathFileName, StringComparison.OrdinalIgnoreCase))
            {
                int progressPercentage = e.GridIndex * 100 / e.GridCount;
                var updatingArgs = new UpdatingTaskProgressEventArgs(TaskState.Updating, progressPercentage);
                updatingArgs.Current = e.GridIndex;
                updatingArgs.UpperBound = e.GridCount;
                OnUpdatingProgress(updatingArgs);
                if (updatingArgs.TaskState == TaskState.Canceled)
                {
                    e.IsCanceled = true;
                }
            }
        }
    }
}