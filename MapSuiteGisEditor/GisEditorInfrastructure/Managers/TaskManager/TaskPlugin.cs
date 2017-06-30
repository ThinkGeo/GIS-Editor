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

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(TaskPlugin))]
    public abstract class TaskPlugin : Plugin
    {
        /// <summary>
        /// Occurs when [updating progress].
        /// </summary>
        public event EventHandler<UpdatingTaskProgressEventArgs> UpdatingProgress;
        private bool isCanceled;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskPlugin" /> class.
        /// </summary>
        protected TaskPlugin() { }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            UpdatingTaskProgressEventArgs args = new UpdatingTaskProgressEventArgs(TaskState.Updating);
            OnUpdatingProgress(args);
            try
            {
                RunCore();
            }
            catch (Exception e)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                args.Error = new ExceptionInfo(e.Message, e.StackTrace, e.Source);
                isCanceled = true;
            }
            finally
            {
                args.TaskState = isCanceled ? TaskState.Canceled : TaskState.Completed;
                OnUpdatingProgress(args);
            }
        }

        /// <summary>
        /// Runs the core.
        /// </summary>
        protected abstract void RunCore();

        /// <summary>
        /// This method raises when load this plugin.
        /// </summary>
        protected override void LoadCore()
        {
            base.LoadCore();
            Name = GetType().Name;
            Description = GetType().Name;
        }

        /// <summary>
        /// Raises the <see cref="E:UpdatingProgress" /> event.
        /// </summary>
        /// <param name="e">The <see cref="UpdatingTaskProgressEventArgs" /> instance containing the event data.</param>
        protected virtual void OnUpdatingProgress(UpdatingTaskProgressEventArgs e)
        {
            var handler = UpdatingProgress;
            if (handler != null)
            {
                handler(this, e);
                if (e.TaskState == TaskState.Canceled)
                {
                    isCanceled = true;
                }
            }
        }
    }
}