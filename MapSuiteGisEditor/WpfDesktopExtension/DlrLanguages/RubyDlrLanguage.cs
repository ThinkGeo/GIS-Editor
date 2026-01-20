using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    /// <summary>
    /// Ruby DLR language support.
    ///
    /// The legacy GIS Editor used IronRuby (and Microsoft.Scripting) shipped as binary dependencies
    /// in the v10 repository. This v14 upgrade removes those hard references. If IronRuby is not
    /// present at runtime, a NotSupportedException is thrown when executing scripts.
    /// </summary>
    [Serializable]
    public class RubyDlrLanguage : DlrLanguage
    {

        protected override object RunScriptCore()
        //protected override object RunScriptCore(string script, IDictionary<string, object> parameters)
        {
            var script = this.Script;
            var parameters  = this.Variables;
            if (script == null) throw new ArgumentNullException(nameof(script));

            var rubyType = Type.GetType("IronRuby.Ruby, IronRuby", throwOnError: false);
            if (rubyType == null)
            {
                TryLoadAssembly("IronRuby");
                rubyType = Type.GetType("IronRuby.Ruby, IronRuby", throwOnError: false);
            }

            if (rubyType == null)
            {
                throw new NotSupportedException(
                    "IronRuby could not be loaded. Add IronRuby assemblies to enable Ruby scripting.");
            }

            dynamic engine = rubyType.GetMethod("CreateEngine", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
                ?.Invoke(null, null);

            if (engine == null)
            {
                throw new InvalidOperationException("Unable to create an IronRuby script engine.");
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
            try
            {
                return engine.CreateScriptSourceFromString(script);
            }
            catch
            {
                var kindType = Type.GetType("Microsoft.Scripting.SourceCodeKind, Microsoft.Scripting", throwOnError: false);
                if (kindType == null)
                {
                    TryLoadAssembly("Microsoft.Scripting");
                    kindType = Type.GetType("Microsoft.Scripting.SourceCodeKind, Microsoft.Scripting", throwOnError: false);
                }

                if (kindType != null)
                {
                    object statements = Enum.GetValues(kindType).Cast<object>().FirstOrDefault(v => string.Equals(v.ToString(), "Statements", StringComparison.OrdinalIgnoreCase))
                                       ?? Enum.GetValues(kindType).Cast<object>().FirstOrDefault();

                    return engine.CreateScriptSourceFromString(script, statements);
                }

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
