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
using System.Linq;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class TaskMonitorUIPlugin : UIPlugin
    {
        private TaskMonitorWindow taskMonitorWindow;

        public TaskMonitorUIPlugin()
        {
            GisEditor.TaskManager.UpdatingProgress += new EventHandler<UpdatingTaskProgressEventArgs>(TaskManager_UpdatingProgress);
        }

        private void TaskManager_UpdatingProgress(object sender, UpdatingTaskProgressEventArgs e)
        {
            if (GisEditor.TaskManager.RunProcessesLocally && Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    OnUpdatingProgressInternal(e);
                });
            }
            else
            {
                OnUpdatingProgressInternal(e);
            }
        }

        private void OnUpdatingProgressInternal(UpdatingTaskProgressEventArgs e)
        {
            if (taskMonitorWindow == null)
            {
                taskMonitorWindow = new TaskMonitorWindow();
            }

            TaskViewModel currentTask = taskMonitorWindow.RunningTasks.FirstOrDefault(t => t.TaskToken.Equals(e.TaskToken, StringComparison.Ordinal));
            if (currentTask == null && e.TaskState != TaskState.Canceled && e.TaskState != TaskState.Completed)
            {
                currentTask = new TaskViewModel(e.TaskToken);
                if (e.Parameters.ContainsKey("Name"))
                {
                    currentTask.Name = e.Parameters["Name"];
                }

                if (e.Parameters.ContainsKey("Description"))
                {
                    currentTask.Description = e.Parameters["Description"];
                }

                lock (taskMonitorWindow.RunningTasks)
                {
                    taskMonitorWindow.RunningTasks.Add(currentTask);
                }
            }
            else if (e.TaskState == TaskState.Updating)
            {
                currentTask.Progress = e.ProgressPercentage;
            }
            else if (e.TaskState == TaskState.Completed || e.TaskState == TaskState.Canceled)
            {
                lock (taskMonitorWindow.RunningTasks)
                {
                    taskMonitorWindow.RunningTasks.Remove(currentTask);
                    if (taskMonitorWindow.RunningTasks.Count == 0)
                    {
                        taskMonitorWindow.Close();
                        taskMonitorWindow = null;
                    }
                }
            }
        }
    }
}