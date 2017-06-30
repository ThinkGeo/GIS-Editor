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


using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class LoggerMessageViewModel : ViewModelBase
    {
        private static int baseIndex;

        private int index;
        private LoggerMessage loggerMessage;
        private double time;

        public LoggerMessageViewModel(LoggerMessage loggerMessage)
        {
            this.loggerMessage = loggerMessage;
            index = baseIndex + 1;
            baseIndex = index;
        }

        public static int BaseIndex
        {
            get { return baseIndex; }
            set { baseIndex = value; }
        }

        public int Index
        {
            get { return index; }
        }

        public LoggerMessage LoggerMessage
        {
            get { return loggerMessage; }
        }

        public double Time
        {
            get { return time; }
            set
            {
                time = value;
                RaisePropertyChanged(()=>Time);
            }
        }

        public override string ToString()
        {
            return loggerMessage.ToString();
        }
    }
}
