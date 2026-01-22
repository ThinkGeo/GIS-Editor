using System;
using System.Linq;
using System.Reflection;
class P{
  static void Main(){
    var asm=Assembly.LoadFrom("C:\\Source\\gis-editor\\packages\\ThinkGeo.Core.14.4.3\\lib\\net462\\ThinkGeo.Core.dll");
    var t=asm.GetType("ThinkGeo.Core.Layer");
    foreach(var m in t.GetMethods(BindingFlags.Instance|BindingFlags.Public).Where(m=>m.Name=="Open" || m.Name=="Close"))
    {
      Console.WriteLine(m);
    }
  }
}
