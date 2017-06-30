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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal class Validator
    {
        internal static void CheckRecordLength(string Record)
        {
            if (Record.Length != 80)
            {
                throw new ArgumentException("Length incorrect");
            }
        }

        //internal static void CheckRecordType(string Record, BasEnum.DataType targetType)
        //{
        //    CheckRecordLength(Record);

        //    if (ConvertHelper.GetType(Record) != targetType)
        //    {
        //        throw new ArgumentException("RecordType incorrect");
        //    }
        //}

        internal static void CheckTobinBasFileName(string path)
        {
            if (!path.ToUpperInvariant().EndsWith("BAS"))
            {
                throw new FileNotFoundException("Bas file format is incorrect.");
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Can't find the file.");
            }
        }
    }
}