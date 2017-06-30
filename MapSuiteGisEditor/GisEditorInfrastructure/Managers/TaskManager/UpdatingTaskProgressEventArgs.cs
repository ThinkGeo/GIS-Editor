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
using System.Collections.Generic;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class UpdatingTaskProgressEventArgs : EventArgs
    {
        private Dictionary<string, string> parameters;
        private int progressPercentage;
        private object userState;
        private int current;
        private int upperBound;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatingTaskProgressEventArgs" /> class.
        /// </summary>
        public UpdatingTaskProgressEventArgs()
            : this(TaskState.Normal)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatingTaskProgressEventArgs" /> class.
        /// </summary>
        /// <param name="taskState">State of the task.</param>
        public UpdatingTaskProgressEventArgs(TaskState taskState)
            : this(taskState, 0, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatingTaskProgressEventArgs" /> class.
        /// </summary>
        /// <param name="taskState">State of the task.</param>
        /// <param name="progressPercentage">The progress percentage.</param>
        public UpdatingTaskProgressEventArgs(TaskState taskState, int progressPercentage)
            : this(taskState, progressPercentage, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatingTaskProgressEventArgs" /> class.
        /// </summary>
        /// <param name="taskState">State of the task.</param>
        /// <param name="progressPercentage">The progress percentage.</param>
        /// <param name="userState">State of the user.</param>
        public UpdatingTaskProgressEventArgs(TaskState taskState, int progressPercentage, object userState)
            : this(taskState, progressPercentage, userState, string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatingTaskProgressEventArgs" /> class.
        /// </summary>
        /// <param name="taskState">State of the task.</param>
        /// <param name="progressPercentage">The progress percentage.</param>
        /// <param name="userState">State of the user.</param>
        /// <param name="taskToken">The task token.</param>
        public UpdatingTaskProgressEventArgs(TaskState taskState, int progressPercentage, object userState, string taskToken)
        {
            this.parameters = new Dictionary<string, string>();
            this.progressPercentage = progressPercentage;
            this.userState = userState;
            this.TaskState = taskState;
            this.Current = progressPercentage;
            this.UpperBound = 100;
            this.TaskToken = taskToken;
        }

        /// <summary>
        /// Gets the progress percentage.
        /// </summary>
        /// <value>
        /// The progress percentage.
        /// </value>
        public int ProgressPercentage { get { return progressPercentage; } }

        /// <summary>
        /// Gets the state of the user.
        /// </summary>
        /// <value>
        /// The state of the user.
        /// </value>
        public object UserState { get { return userState; } }

        /// <summary>
        /// Gets or sets the state of the task.
        /// </summary>
        /// <value>
        /// The state of the task.
        /// </value>
        public TaskState TaskState { get; set; }

        /// <summary>
        /// Gets or sets the current.
        /// </summary>
        /// <value>
        /// The current.
        /// </value>
        public int Current
        {
            get { return current; }
            set
            {
                if (current != value)
                {
                    current = value;
                    if(upperBound != 0) progressPercentage = current * 100 / upperBound;
                }
            }
        }

        /// <summary>
        /// Gets or sets the upper bound.
        /// </summary>
        /// <value>
        /// The upper bound.
        /// </value>
        public int UpperBound
        {
            get { return upperBound; }
            set
            {
                if (upperBound != value)
                {
                    upperBound = value;
                    if (upperBound != 0) progressPercentage = current * 100 / upperBound;
                }
            }
        }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message { get; set; }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public Dictionary<string, string> Parameters { get { return parameters; } }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public ExceptionInfo Error { get; set; }

        /// <summary>
        /// Gets or sets the task token.
        /// </summary>
        /// <value>
        /// The task token.
        /// </value>
        public string TaskToken { get; set; }
    }
}
