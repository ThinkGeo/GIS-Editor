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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Data;
using ThinkGeo.Core;

namespace ThinkGeo.MapSuite.GisEditor
{
    public static class AppHelper
    {
        private static readonly string defaultThemeResource = "/MapSuiteGisEditor;component/Resources/General.xaml";
        private static GisEditorSplashScreen splashScreen;
        private static Collection<string> defaultHelpResources;

        public static Window WindowOwner
        {
            get
            {
                return Application.Current == null ? null : Application.Current.MainWindow;
            }
        }

        public static GisEditorSplashScreen SplashScreen
        {
            get
            {
                if (splashScreen == null)
                {
                    splashScreen = new GisEditorSplashScreen("Images/splashScreen.png");
                }

                return splashScreen;
            }
        }

        private static System.Windows.Forms.Screen startScreen = null;

        public static void Startup()
        {
            string targetFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "MapSuiteGisEditor", "location.txt");
            if (File.Exists(targetFilePath))
            {
                double left, top, width, height;

                string text = File.ReadAllText(targetFilePath);
                string[] array = text.Split(',');
                if (array != null && array.Length == 4)
                {
                    if (double.TryParse(array[0], out left) &&
                        double.TryParse(array[1], out top) &&
                        double.TryParse(array[2], out width) &&
                        double.TryParse(array[3], out height))
                    {
                        double centerX = left + width * 0.5;
                        double centerY = top + height * 0.5;

                        System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens.OrderBy(s => s.Bounds.X).ToArray();
                        for (int i = 0; i < screens.Length; i++)
                        {
                            int x = screens[i].Bounds.X;
                            double screenWidth = screens[i].Bounds.Width;
                            int y = screens[i].Bounds.Y;
                            double screenHeight = screens[i].Bounds.Height;
                            if (x <= centerX && screenWidth + x >= centerX
                                && y <= centerY && screenHeight + y >= centerY)
                            {
                                startScreen = screens[i];
                                break;
                            }
                        }
                    }
                }
            }

            if (startScreen == null)
            {
                startScreen = System.Windows.Forms.Screen.PrimaryScreen;
            }
            if (startScreen != null)
            {
                StreamResourceInfo resourceInfo = Application.GetResourceStream(new Uri("Images/splashScreen.png", UriKind.Relative));
                using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(resourceInfo.Stream))
                {
                    double splashLeft = startScreen.Bounds.X + startScreen.Bounds.Width * 0.5 - bitmap.Width * 0.5;
                    double splashTop = startScreen.Bounds.Y + startScreen.Bounds.Height * 0.5 - bitmap.Height * 0.5;
                    SplashScreen.Left = splashLeft;
                    SplashScreen.Top = splashTop;
                }
            }

            SplashScreen.Show(false, false);

            InitializeHelpUris();
            MergeDefaultThemeResouce();
        }

        public static void Started(string startupProjectPath)
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            GisEditorUserControl gisEditorUserControl = new GisEditorUserControl(startupProjectPath);
            Window window = new Window();
            Application.Current.MainWindow = window;
            window.Title = GisEditor.LanguageManager.GetStringResource("ShellDscMapSuiteGISDscTitle");
            window.Icon = new BitmapImage(new Uri("pack://application:,,,/MapSuiteGisEditor;component/Images/logo.png", UriKind.Absolute));
            window.SetBinding(Window.FlowDirectionProperty, new Binding() { Source = GisEditor.LanguageManager, Path = new PropertyPath("FlowDirection") });

            ApplyWindowStyle(window);
            window.Content = gisEditorUserControl;
            gisEditorUserControl.Load();
            GisEditorHelper.RestoreWindowLocation(window, startScreen);
            window.Loaded += new RoutedEventHandler(window_Loaded);
            window.Show();
        }

        private static void window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                splashScreen.Close();
            });
        }

        public static void ParseArguments(string[] arguments, ref string startupProjectPath, string startupConfigurationPath)
        {
            ParseArguments(arguments, ref startupProjectPath, ref startupConfigurationPath);

            if (!string.IsNullOrEmpty(startupConfigurationPath))
            {
                GisEditor.InfrastructureManager.SettingsPath = startupConfigurationPath;
            }
        }

        private static string GetTileCachesFolderPath()
        {
            return GisEditor.InfrastructureManager.TemporaryPath + "\\TileCaches";
        }

        public static void CopyFilesToDocumentFolder()
        {
            var documentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Map Suite Gis Editor");
            if (!Directory.Exists(documentFolder)) Directory.CreateDirectory(documentFolder);
            var currentExecutingFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var sourceFolder = Path.Combine(Path.GetFullPath(currentExecutingFolder), "Miscellaneous", "InstallationFiles");

            if (!Directory.Exists(sourceFolder)) sourceFolder = Path.Combine(Path.GetFullPath(currentExecutingFolder), "InstallationFiles");

            ExtractZipFileToFolder(documentFolder, sourceFolder, "Sample Data.zip");
            ExtractZipFileToFolder(documentFolder, sourceFolder, "StyleLibrary.zip");
            ExtractZipFileToFolder(documentFolder, sourceFolder, "Images.zip");

            FixStyles(documentFolder);
        }

        [System.Diagnostics.Conditional("GISEditorUnitTest")]
        private static void ApplyWindowStyle(Window window)
        {
            ResourceDictionary resourceDic = new ResourceDictionary();
            resourceDic.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("../MapSuiteGisEditor;component/Resources/General.xaml", UriKind.RelativeOrAbsolute) });
            window.Resources = resourceDic;
        }

        private static void ExtractZipFileToFolder(string documentFolder, string sourceFolder, string zipFileName)
        {
            var zipFilePath = Path.Combine(sourceFolder, zipFileName);

            var targetFolder = Path.Combine(documentFolder, Path.GetFileNameWithoutExtension(zipFilePath));
            if (File.Exists(zipFilePath) && !Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                using (var shapeFileAdapter = ZipFileAdapterManager.CreateInstance(zipFilePath))
                {
                    shapeFileAdapter.ExtractAll(targetFolder);
                }
            }
        }

        private static void FixStyles(string documentFolder)
        {
            //string[] files = Directory.GetFiles(documentFolder, "*.tgsty", SearchOption.AllDirectories);

            //Stream fixResourceStream = typeof(GeoSerializationFormatter).Assembly.GetManifestResourceStream("ThinkGeo.MapSuite.Serialize.Serializer.ResolveSerializedIssue.xml");
            //System.Xml.Linq.XElement fixElement = System.Xml.Linq.XElement.Load(fixResourceStream);

            //foreach (var file in files)
            //{
            //    string content = File.ReadAllText(file);
            //    bool needSave = false;
            //    foreach (var item in fixElement.Descendants("Pair"))
            //    {
            //        string oldContent = item.Element("Old").Value;
            //        string newContent = item.Element("New").Value;

            //        if (content.Contains(oldContent))
            //        {
            //            content = content.Replace(oldContent, newContent);
            //            needSave = true;
            //        }
            //    }

            //    if (needSave) File.WriteAllText(file, content);
            //}
        }

        private static void ParseArguments(string[] arguments, ref string startupProjectPath, ref string startupConfigurationPath)
        {
            foreach (var argument in arguments)
            {
                if (argument.ToUpperInvariant().EndsWith(".TGPROJ"))
                {
                    startupProjectPath = argument;
                }
                else
                {
                    startupConfigurationPath = argument;
                }
            }
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = args.Name.Split(',').FirstOrDefault().Trim() + ".dll";
            string directoryRoot = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (var directory in GetAssemblySearchDirectories(directoryRoot))
            {
                string[] candidateFiles = Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories);
                foreach (string assemblyPath in candidateFiles)
                {
                    if (Path.GetFileName(assemblyPath).Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (IsMismatchedArchitecture(assemblyPath) || !IsManagedAssembly(assemblyPath))
                        {
                            continue;
                        }
                        Assembly assembly;
                        if (TryLoadAssembly(assemblyPath, out assembly))
                        {
                            return assembly;
                        }
                    }
                }
            }

            return null;
        }

        private static bool TryLoadAssembly(string path, out Assembly assembly)
        {
            assembly = null;
            try
            {
                assembly = Assembly.LoadFile(path);
                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
            catch (FileLoadException)
            {
                return false;
            }
        }

        private static bool IsManagedAssembly(string path)
        {
            try
            {
                AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsMismatchedArchitecture(string path)
        {
            if (Environment.Is64BitProcess)
            {
                if (path.IndexOf("Windows-X86", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("win-x86", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            else
            {
                if (path.IndexOf("Windows-X64", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("win-x64", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            try
            {
                var name = AssemblyName.GetAssemblyName(path);
                if (Environment.Is64BitProcess && name.ProcessorArchitecture == ProcessorArchitecture.X86)
                {
                    return true;
                }
                if (!Environment.Is64BitProcess && name.ProcessorArchitecture == ProcessorArchitecture.Amd64)
                {
                    return true;
                }
            }
            catch
            {
                // Ignore non-managed assemblies here; IsManagedAssembly handles them.
            }

            return false;
        }

        private static Collection<string> GetAssemblySearchDirectories(string baseDirectory)
        {
            var result = new Collection<string>();

            foreach (var directory in GetCandidateDirectories(baseDirectory))
            {
                if (!result.Contains(directory))
                {
                    result.Add(directory);
                }
            }

            string current = baseDirectory;
            for (int i = 0; i < 4 && !string.IsNullOrEmpty(current); i++)
            {
                string pluginsDirectory = Path.Combine(current, "Plugins");
                if (Directory.Exists(pluginsDirectory) && !result.Contains(pluginsDirectory))
                {
                    result.Add(pluginsDirectory);
                }

                var parent = Directory.GetParent(current);
                current = parent == null ? null : parent.FullName;
            }

            return result;
        }

        private static Collection<string> GetCandidateDirectories(string baseDirectory)
        {
            var result = new Collection<string>();
            string current = baseDirectory;
            for (int i = 0; i < 4 && !string.IsNullOrEmpty(current); i++)
            {
                if (Directory.Exists(current) && !result.Contains(current))
                {
                    result.Add(current);
                }

                var parent = Directory.GetParent(current);
                current = parent == null ? null : parent.FullName;
            }

            return result;
        }

        private static void MergeDefaultThemeResouce()
        {
            if (Application.Current != null)
            {
                MergeDefaultThemeResource(defaultThemeResource);
                defaultHelpResources.ForEach(tempUri => MergeDefaultThemeResource(tempUri));
            }
        }

        private static void MergeDefaultThemeResource(string resourceUri)
        {
            if (!Application.Current.Resources.MergedDictionaries.Any(m => m.Source != null
                && m.Source.ToString().Equals(resourceUri, StringComparison.OrdinalIgnoreCase)))
            {
                var resourseDicionary = new ResourceDictionary();
                resourseDicionary.Source = new Uri(resourceUri, UriKind.RelativeOrAbsolute);
                Application.Current.Resources.MergedDictionaries.Add(resourseDicionary);
            }
        }

        private static void InitializeHelpUris()
        {
            defaultHelpResources = new Collection<string>();
            defaultHelpResources.Add("/MapSuiteGisEditor;component/GisEditorHelpUrls.xaml");

            string helpUri = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GisEditorHelpUrls.xaml");
            if (File.Exists(helpUri))
            {
                defaultHelpResources.Add(helpUri);
            }
        }
    }
}
