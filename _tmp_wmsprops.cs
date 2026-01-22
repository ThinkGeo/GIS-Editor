using System;
using System.Linq;
using System.Reflection;
class P{
  static void Main(){
    var asm=Assembly.LoadFrom("C:\\Source\\gis-editor\\packages\\ThinkGeo.Core.14.4.3\\lib\\net462\\ThinkGeo.Core.dll");
    var t=asm.GetType("ThinkGeo.Core.WmsAsyncLayer");
    foreach(var p in t.GetProperties(BindingFlags.Instance|BindingFlags.Public)){
      if (p.Name.Contains("Projection") || p.Name.Contains("Crs") || p.Name.Contains("Layer"))
        Console.WriteLine(p.Name+" : "+p.PropertyType);
    }
  }
}
