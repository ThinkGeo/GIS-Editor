using System;
using System.IO;
using System.Reflection;
using System.Linq;

class Program
{
    static void Main()
    {
        var pluginDir = @"C:\Source\gis-editor\MapSuiteGisEditor\MapSuiteGisEditor\bin\Debug\Plugins";
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var name = new AssemblyName(e.Name).Name + ".dll";
            var candidate = Path.Combine(pluginDir, name);
            if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);
            var candidate2 = Path.Combine(pluginDir, "ThinkGeo", name);
            if (File.Exists(candidate2)) return Assembly.LoadFrom(candidate2);
            return null;
        };

        var infraPath = Path.Combine(pluginDir, "ThinkGeo", "GisEditorInfrastructure.dll");
        var toolkitsPath = Path.Combine(pluginDir, "ThinkGeo", "GisEditorToolkits.dll");
        Assembly.LoadFrom(toolkitsPath);
        var infra = Assembly.LoadFrom(infraPath);

        var mgrType = infra.GetType("ThinkGeo.MapSuite.GisEditor.ProjectPluginManager", true);
        var mgr = Activator.CreateInstance(mgrType);
        var getPlugins = mgrType.GetMethod("GetPlugins");
        var result = getPlugins.Invoke(mgr, null);
        var countProp = result.GetType().GetProperty("Count");
        Console.WriteLine("Project plugins: " + countProp.GetValue(result));
    }
}
