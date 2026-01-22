using System;
using System.Linq;
using System.Reflection;
class P{
  static void Main(){
    var asm=Assembly.LoadFrom("C:\\Source\\gis-editor\\packages\\ThinkGeo.Core.14.4.3\\lib\\net462\\ThinkGeo.Core.dll");
    Type[] types;
    try{ types = asm.GetExportedTypes(); }
    catch(ReflectionTypeLoadException ex){ types = ex.Types.Where(t=>t!=null).ToArray(); }
    foreach(var t in types.Where(t => t.Name.Contains("Wms"))){
      Console.WriteLine(t.FullName + " : " + t.BaseType);
    }
  }
}
