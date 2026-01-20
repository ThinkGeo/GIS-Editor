using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    /// <summary>
    /// Python DLR language support.
    ///
    /// The legacy GIS Editor relied on IronPython (and Microsoft.Scripting) assemblies that were
    /// shipped as raw binaries in the v10 repository. This v14 upgrade avoids a hard compile-time
    /// dependency by using reflection/dynamic dispatch. If the IronPython assemblies are not
    /// present at runtime, a NotSupportedException will be thrown when executing scripts.
    /// </summary>
    [Serializable]
    public class PythonDlrLanguage : DlrLanguage
    {
        protected override object RunScriptCore()
        {
            var script = this.Script;
            var parameters = this.Variables;
            if (script == null) throw new ArgumentNullException(nameof(script));

            // Resolve IronPython.Hosting.Python from the IronPython assembly.
            var pythonType = Type.GetType("IronPython.Hosting.Python, IronPython", throwOnError: false);
            if (pythonType == null)
            {
                TryLoadAssembly("IronPython");
                pythonType = Type.GetType("IronPython.Hosting.Python, IronPython", throwOnError: false);
            }

            if (pythonType == null)
            {
                throw new NotSupportedException(
                    "IronPython could not be loaded. Add the 'IronPython' NuGet package (and its dependencies) to enable Python scripting.");
            }

            // dynamic keeps us free from compile-time references to Microsoft.Scripting.Hosting types.
            dynamic engine = pythonType.GetMethod("CreateEngine", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
                ?.Invoke(null, null);

            if (engine == null)
            {
                throw new InvalidOperationException("Unable to create an IronPython script engine.");
            }

            dynamic scope = engine.CreateScope();
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    scope.SetVariable(kvp.Key, kvp.Value);
                }
            }

            dynamic source = CreateScriptSource(engine, script);
            return source.Execute(scope);
        }

        private static dynamic CreateScriptSource(dynamic engine, string script)
        {
            // Try common overloads: CreateScriptSourceFromString(string) or (string, SourceCodeKind)
            try
            {
                return engine.CreateScriptSourceFromString(script);
            }
            catch
            {
                // Fall back to CreateScriptSourceFromString(string, SourceCodeKind)
                var kindType = Type.GetType("Microsoft.Scripting.SourceCodeKind, Microsoft.Scripting", throwOnError: false);
                if (kindType == null)
                {
                    TryLoadAssembly("Microsoft.Scripting");
                    kindType = Type.GetType("Microsoft.Scripting.SourceCodeKind, Microsoft.Scripting", throwOnError: false);
                }

                if (kindType != null)
                {
                    // Prefer "Statements" if available.
                    object statements = Enum.GetValues(kindType).Cast<object>().FirstOrDefault(v => string.Equals(v.ToString(), "Statements", StringComparison.OrdinalIgnoreCase))
                                       ?? Enum.GetValues(kindType).Cast<object>().FirstOrDefault();

                    return engine.CreateScriptSourceFromString(script, statements);
                }

                // As a last resort, rethrow.
                throw;
            }
        }

        private static void TryLoadAssembly(string simpleName)
        {
            try
            {
                Assembly.Load(simpleName);
            }
            catch
            {
                // ignored
            }
        }
    }
}
