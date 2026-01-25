using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Hosting;
using ThinkGeo.MapSuite.GisEditor;

class Probe
{
    static int Main(string[] args)
    {
        var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => ResolveAssembly(baseDir, e.Name);

        var pluginDir = args.Length > 0 ? args[0] : Path.Combine(baseDir, "Plugins");
        Console.WriteLine("Base dir: " + baseDir);
        Console.WriteLine("Plugin dir: " + pluginDir);
        try
        {
            var catalog = new MultiDirectoryCatalog(new [] { pluginDir }, "*.dll");
            var container = new CompositionContainer(catalog);
            var exports = container.GetExports<ProjectPlugin>().ToList();
            Console.WriteLine("ProjectPlugin exports: " + exports.Count);
            foreach (var ex in exports)
            {
                Console.WriteLine("- " + ex.Value.GetType().FullName + " | Active=" + ex.Value.IsActive + " | Index=" + ex.Value.Index);
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("EX: " + ex);
            return 1;
        }
    }

    static Assembly ResolveAssembly(string baseDir, string name)
    {
        string assemblyName = name.Split(',')[0].Trim() + ".dll";
        foreach (var dir in GetSearchDirs(baseDir))
        {
            try
            {
                foreach (var file in Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories))
                {
                    if (!string.Equals(Path.GetFileName(file), assemblyName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!IsManaged(file)) continue;
                    return Assembly.LoadFile(file);
                }
            }
            catch { }
        }
        return null;
    }

    static Collection<string> GetSearchDirs(string baseDir)
    {
        var result = new Collection<string>();
        string current = baseDir;
        for (int i = 0; i < 4 && !string.IsNullOrEmpty(current); i++)
        {
            if (Directory.Exists(current) && !result.Contains(current)) result.Add(current);
            string plugins = Path.Combine(current, "Plugins");
            if (Directory.Exists(plugins) && !result.Contains(plugins)) result.Add(plugins);
            var parent = Directory.GetParent(current);
            current = parent == null ? null : parent.FullName;
        }
        return result;
    }

    static bool IsManaged(string path)
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
}
