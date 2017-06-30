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


//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Data;
//using System.Globalization;
//using System.IO;
//using System.Text.RegularExpressions;
//using System.Xml.Linq;

//namespace ThinkGeo.MapSuite.GisEditor.Plugins
//{
//    internal class ResourceHelper
//    {
//        private const string connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=D:\Godspeed\Lib\Projection.accdb;Persist Security Info=False;";
//        //private static Collection<Proj4Model> Proj4s = new Collection<Proj4Model>();
//        private static Collection<DetailProj4Model> Proj4s = new Collection<DetailProj4Model>();

//        public static void Generate()
//        {
//            OleDbUtil dbUtil = new OleDbUtil(connectionString);

//            ////string statementForZoneInStatePlane = "SELECT DISTINCT A.Name FROM ([Zone] A INNER JOIN Proj4 B ON A.ID = B.ZoneID) WHERE (B.ProjectionID = 27) order by A.Name asc";
//            //string statementForZoneInUTM = "SELECT DISTINCT A.Name FROM ([Zone] A INNER JOIN Proj4 B ON A.ID = B.ZoneID) WHERE (B.ProjectionID = 3 and A.Name not like '%deprecated%') order by A.Name asc";

//            //DataTable dataTable = dbUtil.GetDataTable(statementForZoneInUTM);
//            ////using (StreamWriter sw = new StreamWriter("d:\\zoneForStatePlane.db"))
//            //using (StreamWriter sw = new StreamWriter("d:\\zoneForUtm.db"))
//            //{
//            //    foreach (DataRow dr in dataTable.Rows)
//            //    {
//            //        string name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dr[0].ToString());
//            //        sw.WriteLine(name);
//            //    }
//            //}

//            //Proj4s.Clear();
//            //ParseOtherProjection(@"C:\Users\chenhao\Desktop\Projection In Txt\_EPSG.txt", SearchProjectionType.EPSG);
//            //ParseOtherProjection(@"C:\Users\chenhao\Desktop\Projection In Txt\_ESRI.txt", SearchProjectionType.ESRI);
//            //ParseOtherProjection(@"C:\Users\chenhao\Desktop\Projection In Txt\_ESRI.extra.txt", SearchProjectionType.ESRI);

//            //string statementForStatePlane = "SELECT A.SRS, A.Proj4String, A.Name, B.Name AS ZoneName, C.Name AS DatumName"
//            //    + " FROM ((Proj4 A LEFT OUTER JOIN"
//            //    + " [Zone] B ON A.ZoneID = B.ID) LEFT OUTER JOIN"
//            //    + " Datum C ON A.DatumID = C.ID)"
//            //    + " WHERE (A.ProjectionID = 27)"
//            //    + " ORDER BY A.Name";

//            string statementForUTM = "SELECT DISTINCT A.SRS, A.Proj4String, A.Name, B.Name AS ZoneName, C.Name AS DatumName"
//                + " FROM ((Proj4 A LEFT OUTER JOIN"
//                + " [Zone] B ON A.ZoneID = B.ID) LEFT OUTER JOIN"
//                + " Datum C ON A.DatumID = C.ID)"
//                + " WHERE (A.ProjectionID = 3) AND (C.Name IN ('WGS84', 'NAD83', 'NAD27')) AND (A.Proj4String NOT LIKE '% no_defs')"
//                + " ORDER BY A.SRS";

//            DataTable dataTable = dbUtil.GetDataTable(statementForUTM);
//            foreach (DataRow dr in dataTable.Rows)
//            {
//                string datum = dr["DatumName"] as String;
//                string name = dr["Name"].ToString();
//                if (String.IsNullOrEmpty(datum))
//                {
//                    datum = name.Substring(name.LastIndexOf(":") + 1).Trim();
//                }

//                DetailProj4Model model = new DetailProj4Model
//                {
//                    Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dr["Name"].ToString()),
//                    Proj4Parameter = dr["Proj4String"].ToString(),
//                    SRS = Int32.Parse(dr["SRS"].ToString()),
//                    ProjectionType = ProjectionType.UTM,
//                    Datum = (DatumType)Enum.Parse(typeof(DatumType), datum, true),
//                    Zone = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dr["ZoneName"].ToString())
//                };

//                Proj4s.Add(model);
//            }

//            XElement xml = new XElement("Proj4s");
//            foreach (var proj in Proj4s)
//            {
//                XElement child = new XElement("Proj4",
//                    new XAttribute("SRS", proj.SRS),
//                    new XAttribute("Name", proj.Name),
//                    new XAttribute("Type", proj.ProjectionType),
//                    new XAttribute("Parameter", proj.Proj4Parameter),
//                    new XAttribute("Datum", proj.Datum),
//                    new XAttribute("Zone", proj.Zone));
//                xml.Add(child);
//            }
//            //xml.Save("d:\\projOther.xml");
//            //xml.Save("d:\\projStatePlane.xml");
//            xml.Save("d:\\projUTM.db");
//        }

//        // in EPSG.txt, ESRI.txt and ESRI.extra.txt
//        private static void ParseOtherProjection(string fileName, SearchProjectionType searchProjectionType)
//        {
//            Collection<List<string>> proj4Strings = new Collection<List<string>>();
//            StreamReader sr = new StreamReader(fileName);
//            string line = String.Empty;
//            bool isStarted = false;
//            List<string> currentProj4String = null;
//            while (!String.IsNullOrEmpty(line = sr.ReadLine()))
//            {
//                if (line.Trim().StartsWith("#") && !isStarted)
//                {
//                    currentProj4String = new List<string>();
//                    isStarted = true;
//                }
//                else if (isStarted && line.Trim().Contains("<>"))
//                {
//                    proj4Strings.Add(currentProj4String);
//                    isStarted = false;
//                }
//                currentProj4String.Add(line);
//            }
//            sr.Close();

//            foreach (List<string> proj4String in proj4Strings)
//            {
//                string name = String.Empty;
//                string proj = String.Empty;
//                SearchProjectionType type = searchProjectionType;
//                int srs = 0;

//                foreach (var proj4Line in proj4String)
//                {
//                    string tmpLine = proj4Line.Trim();
//                    if (tmpLine.StartsWith("#"))
//                    {
//                        name += tmpLine.Replace("#", "");
//                        name += "|";
//                    }
//                    else if (tmpLine.StartsWith("<"))
//                    {
//                        Regex regex = new Regex(@"<\d+>");
//                        var matches = regex.Matches(tmpLine);
//                        if (matches.Count == 1)
//                        {
//                            string srsString = matches[0].Value.Trim('<', '>');
//                            srs = Int32.Parse(srsString);
//                            tmpLine = tmpLine.Substring(tmpLine.IndexOf('>')).Trim();
//                            proj = tmpLine.Trim('<', '>').Trim();
//                        }
//                        else
//                        {
//                        }
//                    }
//                }

//                Proj4Model model = new Proj4Model { Name = name.TrimEnd('|').Trim(), Proj4Parameter = proj, SRS = srs, ProjectionType = type };
//                //Proj4s.Add(model);
//            }
//        }
//    }
//}