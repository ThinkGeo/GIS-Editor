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
using System.Runtime.Serialization;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [DataContract(Namespace = "http://www.thinkgeo.com/errorreport")]
    [Serializable]
    public class ErrorReport
    {
        private string stackTrace;
        private string source;
        private string message;
        private string additionalComment;
        private string senderEmailAddress;

        [DataMember]
        public string SenderEmailAddress
        {
            get { return senderEmailAddress; }
            set { senderEmailAddress = value; }
        }

        [DataMember]
        public string StackTrace
        {
            get { return stackTrace; }
            set { stackTrace = value; }
        }

        [DataMember]
        public string Source
        {
            get { return source; }
            set { source = value; }
        }

        [DataMember]
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        [DataMember]
        public string AdditionalComment
        {
            get { return additionalComment; }
            set { additionalComment = value; }
        }
    }
}