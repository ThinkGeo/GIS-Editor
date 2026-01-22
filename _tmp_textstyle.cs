using System;
using System.Reflection;
class P{
  static void Main(){
    var asm=Assembly.LoadFrom("C:\\Source\\gis-editor\\packages\\ThinkGeo.Core.14.4.3\\lib\\net462\\ThinkGeo.Core.dll");
    var t=asm.GetType("ThinkGeo.Core.TextStyle");
    foreach(var p in t.GetProperties()){
      if (p.Name.Contains("Custom")) Console.WriteLine(p.Name+" : "+p.PropertyType);
    }
  }
}
