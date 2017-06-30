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
using System.IO;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    internal class RecordHeader
    {
        private string dataType;
        private string logicalLevel;
        private string nameFormatType;
        private Dictionary<string, string> variableData;
        private string revisionDate;
        private string sourceCode;
        private string polygonRangeBox;
        private string stateAndZoneCode;
        private string numberOfPolygonParts;
        private string meridianCode;
        private Dictionary<string, string> headerColumns;

        public RecordHeader()
        {
            variableData = new Dictionary<string, string>();
            headerColumns = new Dictionary<string, string>();
        }

        public Dictionary<string, string> HeaderColumns
        {
            get
            {
                headerColumns = new Dictionary<string, string>();

                if (IsInDefaultColumns("LogicalLevel"))
                {
                    headerColumns.Add("LogicalLevel", this.logicalLevel);
                }

                if (IsInDefaultColumns("NameFormatType"))
                {
                    headerColumns.Add("NameFormatType", this.nameFormatType);
                }

                if (IsInDefaultColumns("RevisionDate"))
                {
                    headerColumns.Add("RevisionDate", this.revisionDate);
                }

                if (IsInDefaultColumns("SourceCode"))
                {
                    headerColumns.Add("SourceCode", this.sourceCode);
                }

                if (IsInDefaultColumns("PolygonRangeBox"))
                {
                    headerColumns.Add("PolygonRangeBox", this.polygonRangeBox);
                }

                if (IsInDefaultColumns("StateAndZoneCode"))
                {
                    headerColumns.Add("StateAndZoneCode", this.stateAndZoneCode);
                }

                if (IsInDefaultColumns("NumberOfPolygonParts"))
                {
                    headerColumns.Add("NumberOfPolygonParts", this.numberOfPolygonParts);
                }

                if (IsInDefaultColumns("MeridianCode"))
                {
                    headerColumns.Add("MeridianCode", this.meridianCode);
                }
                foreach (KeyValuePair<string, string> item in this.variableData)
                {
                    headerColumns.Add(item.Key, item.Value);
                }
                return headerColumns;
            }
        }

        // col 2 - 3
        public string LogicalLevel
        {
            get { return this.logicalLevel; }
            set
            {
                logicalLevel = value;
            }
        }

        // col 4
        public string NameFormatType
        {
            get { return this.nameFormatType; }
            set
            {
                nameFormatType = value;
            }
        }

        // col 5 - 32
        // those columns are uncertain, as it based on the format type.
        public Dictionary<string, string> VariableData
        {
            get
            {
                if (variableData == null)
                {
                    return new Dictionary<string, string>();
                }
                else
                {
                    return variableData;
                }
            }
        }

        // col 33 - 38
        public string RevisionDate
        {
            get { return this.revisionDate; }
            set
            {
                revisionDate = value;
            }
        }

        // col 39
        public string SourceCode
        {
            get { return this.sourceCode; }
            set
            {
                sourceCode = value;
            }
        }

        // col 40 - 69
        public string PolygonRangeBox
        {
            get { return this.polygonRangeBox; }
            set
            {
                polygonRangeBox = value;
            }
        }

        // col 70 - 73
        public string StateAndZoneCode
        {
            get { return this.stateAndZoneCode; }
            set
            {
                stateAndZoneCode = value;
            }
        }

        // col 74 - 75
        public string NumberOfPolygonParts
        {
            get { return this.numberOfPolygonParts; }
            set
            {
                numberOfPolygonParts = value;
            }
        }

        // col 76 - 78
        public string MeridianCode
        {
            get { return this.meridianCode; }
            set
            {
                meridianCode = value;
            }
        }

        public void Read(BinaryReader reader)
        {
            dataType = reader.ReadChar().ToString();
            logicalLevel = new string(reader.ReadChars(2));
            nameFormatType = reader.ReadChar().ToString();

            long currentPosition = reader.BaseStream.Position;
            ReadFormatByNameFormatType(nameFormatType, reader);
            reader.BaseStream.Seek(currentPosition + 28, SeekOrigin.Begin);

            revisionDate = new string(reader.ReadChars(6));
            sourceCode = reader.ReadChar().ToString();
            polygonRangeBox = new string(reader.ReadChars(30));
            stateAndZoneCode = new string(reader.ReadChars(4));
            numberOfPolygonParts = new string(reader.ReadChars(2));
            meridianCode = new string(reader.ReadChars(3));

            reader.BaseStream.Seek(2, SeekOrigin.Current); // skip 2 unused charactors.
        }

        private void ReadFormatByNameFormatType(string NameFormatType, BinaryReader reader)
        {
            switch (NameFormatType)
            {
                case "0":
                    HandleFormat0(reader);
                    break;

                case "1":
                    HandleFormat1(reader);
                    break;

                case "2":
                    HandleFormat2(reader);
                    break;

                case "3":
                    HandleFormat3(reader);
                    break;

                case "4":
                    HandleFormat4(reader);
                    break;

                case "5":
                    HandleFormat5(reader);
                    break;

                case "6":
                    HandleFormat6(reader);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void HandleFormat0(BinaryReader reader)
        {
            //// col 5 - 6
            //APIStateCode,
            //// col 7 - 9
            //APICountyCode,
            //// 11 - 13
            //TownshipNumber,
            //// 14
            //TownshipDirection,
            //// 15 - 17
            //RangeNumber,
            //// 18
            //RangeDirection,
            //// 19  -21
            //SectionNumber,
            string APIStateCode = new string(reader.ReadChars(2));
            string APICountyCode = new string(reader.ReadChars(3));
            reader.BaseStream.Seek(1, SeekOrigin.Current);
            string townshipNumber = new string(reader.ReadChars(3));
            string townshipDir = new string(reader.ReadChars(1));
            string rangeNumber = new string(reader.ReadChars(3));
            string rangeDir = new string(reader.ReadChars(1));
            string sectionNum = new string(reader.ReadChars(3));

            variableData.Add(BasNameFormatType0.APIStateCode.ToString(), APIStateCode);
            variableData.Add(BasNameFormatType0.APICountyCode.ToString(), APICountyCode);
            variableData.Add(BasNameFormatType0.TownshipNumber.ToString(), townshipNumber);
            variableData.Add(BasNameFormatType0.TownshipDirection.ToString(), townshipDir);
            variableData.Add(BasNameFormatType0.RangeNumber.ToString(), rangeNumber);
            variableData.Add(BasNameFormatType0.RangeDirection.ToString(), rangeDir);
            variableData.Add(BasNameFormatType0.SectionNumber.ToString(), sectionNum);
        }

        private void HandleFormat1(BinaryReader reader)
        {
            //// col 5 - 6
            //APIStateCode,
            //// col 7 - 9
            //APICountyCode,
            //// 10
            //Prefix,
            //// 11 - 16 :Abstract/Tract/Block Number
            //ATBNumber,
            //// 17 - 21
            //SurveyNumber,
            string APIStateCode = new string(reader.ReadChars(2));
            string APICountyCode = new string(reader.ReadChars(3));
            string prefix = new string(reader.ReadChars(1));
            string atbNumber = new string(reader.ReadChars(6));
            string serveryNum = new string(reader.ReadChars(5));

            variableData.Add(BasNameFormatType1.APIStateCode.ToString(), APIStateCode);
            variableData.Add(BasNameFormatType1.APICountyCode.ToString(), APICountyCode);
            variableData.Add(BasNameFormatType1.Prefix.ToString(), prefix);
            variableData.Add(BasNameFormatType1.ATBNumber.ToString(), atbNumber);
            variableData.Add(BasNameFormatType1.SurveyNumber.ToString(), serveryNum);
        }

        private void HandleFormat2(BinaryReader reader)
        {
            //// col 5 - 6
            //APIStateCode,
            //// col 7 - 9
            //APICountyCode,
            //// 11 - 13
            //TownshipValue,
            //// 14
            //TownshipDirection,
            //// 15 - 17
            //RangeNumber,
            //// 18
            //RangeDirection,
            //// 19 - 23
            //AlphaSectionName,
            string APIStateCode = new string(reader.ReadChars(2));
            string APICountyCode = new string(reader.ReadChars(3));
            string townshipValue = new string(reader.ReadChars(3));
            string townshipDir = new string(reader.ReadChars(1));
            string rangeNum = new string(reader.ReadChars(3));
            string rangeDir = new string(reader.ReadChars(1));
            string alphaSectionName = new string(reader.ReadChars(5));

            variableData.Add(BasNameFormatType2.APIStateCode.ToString(), APIStateCode);
            variableData.Add(BasNameFormatType2.APICountyCode.ToString(), APICountyCode);
            variableData.Add(BasNameFormatType2.TownshipValue.ToString(), townshipValue);
            variableData.Add(BasNameFormatType2.TownshipDirection.ToString(), townshipDir);
            variableData.Add(BasNameFormatType2.RangeNumber.ToString(), rangeNum);
            variableData.Add(BasNameFormatType2.RangeDirection.ToString(), rangeDir);
            variableData.Add(BasNameFormatType2.AlphaSectionName.ToString(), alphaSectionName);
        }

        private void HandleFormat3(BinaryReader reader)
        {
            //// col 5 - 6
            //APIStateCode,
            //// col 7 - 9
            //APICountyCode,
            //// 10
            //Prefix,
            //// 11 - 16
            //AlphaAbstractName,
            //// 18 - 22
            //SurveyNumber,

            string APIStateCode = new string(reader.ReadChars(2));
            string APICountyCode = new string(reader.ReadChars(3));
            string prefix = new string(reader.ReadChars(1));
            string alphaAbstractName = new string(reader.ReadChars(6));
            reader.BaseStream.Seek(1, SeekOrigin.Current);
            string surveyNumber = new string(reader.ReadChars(5));

            variableData.Add(BasNameFormatType3.APIStateCode.ToString(), APIStateCode);
            variableData.Add(BasNameFormatType3.APICountyCode.ToString(), APICountyCode);
            variableData.Add(BasNameFormatType3.Prefix.ToString(), prefix);
            variableData.Add(BasNameFormatType3.AlphaAbstractName.ToString(), alphaAbstractName);
            variableData.Add(BasNameFormatType3.SurveyNumber.ToString(), surveyNumber);
        }

        private void HandleFormat4(BinaryReader reader)
        {
            //// col 5 - 6
            //APIStateCode,
            //// col 7 - 9
            //APICountyCode,
            //// 10 - 23
            //StateCountyName
            string APIStateCode = new string(reader.ReadChars(2));
            string APICountyCode = new string(reader.ReadChars(3));
            string stateCountryName = new string(reader.ReadChars(14));

            variableData.Add(BasNameFormatType4.APIStateCode.ToString(), APIStateCode);
            variableData.Add(BasNameFormatType4.APICountyCode.ToString(), APICountyCode);
            variableData.Add(BasNameFormatType4.StateCountyName.ToString(), stateCountryName);
        }

        private void HandleFormat5(BinaryReader reader)
        {
            // 5 - 23
            // AlphaName
            string alphaName = new string(reader.ReadChars(19));
            variableData.Add(BasNameFormatType5.AlphaName.ToString(), alphaName);
        }

        private void HandleFormat6(BinaryReader reader)
        {
            string APIStateCode = new string(reader.ReadChars(2));
            string APICountyCode = new string(reader.ReadChars(3));
            string SurveyCodeListNumber = new string(reader.ReadChars(3));
            string BLT = new string(reader.ReadChars(1)); // Block/League/Township

            variableData.Add(BasNameFormatType6.APIStateCode.ToString(), APIStateCode);
            variableData.Add(BasNameFormatType6.APICountyCode.ToString(), APICountyCode);
            variableData.Add(BasNameFormatType6.SurveyCodeListNumber.ToString(), SurveyCodeListNumber);
            variableData.Add(BasNameFormatType6.BLTFlag.ToString(), BLT);

            switch (BLT)
            {
                case "B":
                    string BlockName = new string(reader.ReadChars(6));
                    reader.BaseStream.Seek(1, SeekOrigin.Current);

                    VariableData.Add(BasNameFormatType6.BlockName.ToString(), BlockName);
                    break;

                case "L":
                    string LeagueName = new string(reader.ReadChars(6));
                    reader.BaseStream.Seek(1, SeekOrigin.Current);

                    VariableData.Add(BasNameFormatType6.LeagueName.ToString(), LeagueName);
                    break;

                case "T":
                    string BlockNumber = new string(reader.ReadChars(4));
                    string TownshipNumber = new string(reader.ReadChars(2));
                    string TownshipDirection = new string(reader.ReadChars(1));

                    VariableData.Add(BasNameFormatType6.BlockName.ToString(), BlockNumber);
                    VariableData.Add(BasNameFormatType6.BlockName.ToString(), TownshipNumber);
                    VariableData.Add(BasNameFormatType6.BlockName.ToString(), TownshipDirection);
                    break;

                default:
                    // there is a possible that BLT is blank.
                    reader.BaseStream.Seek(7, SeekOrigin.Current);
                    break;
            }

            string SLTFlag = new string(reader.ReadChars(1)); // Section/Labor/Tract (S/L/T) Flag
            string SLTNumber = new string(reader.ReadChars(5)); // Section/Labor/Tract (S/L/T) Number
            string SAFlag = new string(reader.ReadChars(1)); // Survey/Abstract (S/A) Flag
            string SANumber = new string(reader.ReadChars(5)); // Survey/Abstract (S/A) Number

            variableData.Add(BasNameFormatType6.SLTFlag.ToString(), SLTFlag);
            variableData.Add(BasNameFormatType6.SLTNumber.ToString(), SLTNumber);
            variableData.Add(BasNameFormatType6.SAFlag.ToString(), SAFlag);
            variableData.Add(BasNameFormatType6.SANumber.ToString(), SANumber);
        }

        private bool IsInDefaultColumns(string col)
        {
            bool contains = false;
            foreach (var defaultCol in Enum.GetValues(typeof(BasDefaultColumns)))
            {
                if (defaultCol.ToString().ToUpperInvariant() == col.ToUpperInvariant())
                {
                    contains = true;
                    break;
                }
            }
            return contains;
        }
    }

    /// <summary>
    /// here defined part of columns from header. all the columns should be the sum of the default columns and the columns from the uncertain format type.
    /// For example: if bas name format type is 0
    /// </summary>
    public enum BasDefaultColumns
    {
        TobinLevel,
        RevisionDate,
        SourceCode
    }

    public enum BasNameFormatType0
    {
        // col 5 - 6
        APIStateCode,

        // col 7 - 9
        APICountyCode,

        // 11 - 13
        TownshipNumber,

        // 14
        TownshipDirection,

        // 15 - 17
        RangeNumber,

        // 18
        RangeDirection,

        // 19  -21
        SectionNumber,
    }

    /// <summary>
    /// Format 1 is generally used with abstract/tract/block data.
    /// </summary>
    public enum BasNameFormatType1
    {
        // col 5 - 6
        APIStateCode,

        // col 7 - 9
        APICountyCode,

        // 10
        Prefix,

        // 11 - 16 :Abstract/Tract/Block Number
        ATBNumber,

        // 17 - 21
        SurveyNumber,
    }

    public enum BasNameFormatType2
    {
        // col 5 - 6
        APIStateCode,

        // col 7 - 9
        APICountyCode,

        // 11 - 13
        TownshipValue,

        // 14
        TownshipDirection,

        // 15 - 17
        RangeNumber,

        // 18
        RangeDirection,

        // 19 - 23
        AlphaSectionName,
    }

    /// <summary>
    /// Format 3 is generally used with abstract data.
    /// </summary>
    public enum BasNameFormatType3
    {
        // col 5 - 6
        APIStateCode,

        // col 7 - 9
        APICountyCode,

        // 10
        Prefix,

        // 11 - 16
        AlphaAbstractName,

        // 18 - 22
        SurveyNumber,
    }

    /// <summary>
    /// Format 4 is used for State and County boundaries.
    /// </summary>
    public enum BasNameFormatType4
    {
        // col 5 - 6
        APIStateCode,

        // col 7 - 9
        APICountyCode,

        // 10 - 23
        StateCountyName
    }

    /// <summary>
    ///
    /// </summary>
    public enum BasNameFormatType5
    {
        // 5 - 23
        AlphaName
    }

    /// <summary>
    /// Format 6 is used for Abstract/Block/League data in Texas.
    /// </summary>
    public enum BasNameFormatType6
    {
        // col 5 - 6
        APIStateCode,

        // col 7 - 9
        APICountyCode,

        // 10 - 12
        SurveyCodeListNumber,

        // 13 - 20 : Block/League/Township (B/L/T).If Column 13 is B or L, then Column 14-19 have the block or league name. If Column 13 is T, the Column 14-17 has the block number, column 18-19 have the
        //township number and column 20 has the township direction. Block names and townships are right justified.
        BLTFlag,

        BlockName,
        LeagueName,
        BlockNumber,
        TownshipNumber,
        TownshipDirection,

        // 21
        SLTFlag,

        // 21 - 26
        SLTNumber,

        // 27
        SAFlag,

        // 28 - 32
        SANumber
    }
}