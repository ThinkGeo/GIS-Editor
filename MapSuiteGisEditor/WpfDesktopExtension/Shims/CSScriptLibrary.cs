using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

// A very small subset of the original CS-Script API used by the legacy GIS Editor.
//
// The original WpfDesktopExtension depended on an external CSScriptLibrary.dll.
// To keep the project self-contained (and compilable without extra binaries),
// we provide a minimal compatible implementation here.
//
// NOTE: This is *not* a full replacement for CS-Script; it only supports the
// methods actually used by the GIS Editor extension (LoadCode + AsmHelper.Invoke).
namespace CSScriptLibrary
{
    public static class CSScript
    {
        /// <summary>
        /// Compiles C# source code into an in-memory assembly.
        /// </summary>
        public static Assembly LoadCode(string code)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));

            using (var provider = new CSharpCodeProvider())
            {
                var parameters = new CompilerParameters
                {
                    GenerateExecutable = false,
                    GenerateInMemory = true,
                    IncludeDebugInformation = false,
                    TreatWarningsAsErrors = false,
                    CompilerOptions = "/optimize"
                };

                // Reference all currently loaded assemblies that have a physical location.
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        if (asm.IsDynamic) continue;
                        if (string.IsNullOrWhiteSpace(asm.Location)) continue;
                        if (!parameters.ReferencedAssemblies.Contains(asm.Location))
                        {
                            parameters.ReferencedAssemblies.Add(asm.Location);
                        }
                    }
                    catch
                    {
                        // Ignore dynamic / reflection-only assemblies.
                    }
                }

                // Ensure some common references exist.
                AddReferenceIfMissing(parameters, typeof(object).Assembly);
                AddReferenceIfMissing(parameters, typeof(Enumerable).Assembly);
                AddReferenceIfMissing(parameters, typeof(Uri).Assembly);

                CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);

                if (results.Errors.HasErrors)
                {
                    var errors = results.Errors
                        .Cast<CompilerError>()
                        .Where(e => !e.IsWarning)
                        .Select(e => $"{e.FileName}({e.Line},{e.Column}): {e.ErrorNumber}: {e.ErrorText}");

                    throw new InvalidOperationException(
                        "Failed to compile C# script." + Environment.NewLine + string.Join(Environment.NewLine, errors));
                }

                return results.CompiledAssembly;
            }
        }

        private static void AddReferenceIfMissing(CompilerParameters parameters, Assembly assembly)
        {
            try
            {
                var location = assembly.Location;
                if (!string.IsNullOrWhiteSpace(location) && !parameters.ReferencedAssemblies.Contains(location))
                {
                    parameters.ReferencedAssemblies.Add(location);
                }
            }
            catch
            {
                // Ignore.
            }
        }
    }

    /// <summary>
    /// Helper that locates and invokes a method from a compiled assembly.
    /// The method name can be in the form "Type.Method" or "Namespace.Type.Method".
    /// </summary>
    public sealed class AsmHelper : IDisposable
    {
        private readonly Assembly assembly;

        public AsmHelper(Assembly assembly)
        {
            this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        public object Invoke(string methodName, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Method name is required.", nameof(methodName));
            }

            // Split "Namespace.Type.Method" -> "Namespace.Type" + "Method".
            int lastDot = methodName.LastIndexOf('.');
            string typeName = lastDot > 0 ? methodName.Substring(0, lastDot) : null;
            string shortMethodName = lastDot > 0 ? methodName.Substring(lastDot + 1) : methodName;

            Type targetType = null;

            if (!string.IsNullOrEmpty(typeName))
            {
                targetType = assembly.GetType(typeName, throwOnError: false, ignoreCase: false)
                    ?? assembly.GetTypes().FirstOrDefault(t => string.Equals(t.Name, typeName, StringComparison.Ordinal));
            }

            // Fallback: pick the first type that has the requested method.
            if (targetType == null)
            {
                targetType = assembly.GetTypes().FirstOrDefault(t =>
                    t.GetMethod(shortMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance) != null);
            }

            if (targetType == null)
            {
                throw new MissingMemberException($"Could not locate a type containing method '{methodName}'.");
            }

            MethodInfo method = targetType.GetMethod(shortMethodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

            if (method == null)
            {
                throw new MissingMethodException(targetType.FullName, shortMethodName);
            }

            object instance = null;
            if (!method.IsStatic)
            {
                instance = Activator.CreateInstance(targetType);
            }

            return method.Invoke(instance, args);
        }

        public void Dispose()
        {
            // Nothing to dispose.
        }
    }
}
