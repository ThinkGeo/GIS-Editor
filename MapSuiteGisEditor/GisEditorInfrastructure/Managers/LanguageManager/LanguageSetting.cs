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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    internal class LanguageSetting
    {
        private string resourcePath;
        private CultureInfo language;
        private Collection<CultureInfo> languages;
        private static LanguageSetting languageOption;

        protected LanguageSetting()
        { }

        public static LanguageSetting Instance
        {
            get
            {
                if (languageOption == null) languageOption = new LanguageSetting();
                return languageOption;
            }
        }

        public CultureInfo Language
        {
            get { return language; }
            set { language = value; }
        }

        public Collection<CultureInfo> GetAvailableLanguages()
        {
            if (languages == null)
            {
                languages = new Collection<CultureInfo>();
                string path = Application.Current.GetType().Assembly.CodeBase.TrimEnd('/');
                path = path.Remove(path.LastIndexOf('/'));

                resourcePath = String.Format(CultureInfo.InvariantCulture, "{0}/languages/", path);
                resourcePath = new Uri(resourcePath).LocalPath;

                if (Directory.Exists(resourcePath))
                {
                    foreach (DirectoryInfo subDirectory in new DirectoryInfo(resourcePath).GetDirectories())
                    {
                        try
                        {
                            CultureInfo culture = new CultureInfo(subDirectory.Name);
                            if (!String.IsNullOrEmpty(culture.NativeName))
                            {
                                languages.Add(culture);
                            }
                        }
                        catch (CultureNotFoundException exception)
                        {
                            LoggerMessage loggerMessage = new LoggerMessage(LoggerLevel.Error, "Invalid folder name for language.");
                            loggerMessage.Error = new ExceptionInfo(exception.Message, exception.StackTrace, exception.Source);
                            GisEditor.LoggerManager.Log(loggerMessage);
                        }
                    }
                }
                if (!languages.Any(lan => lan.Name.Equals("en")))
                {
                    languages.Add(new CultureInfo("en"));
                }
            }

            return languages;
        }
    }
}