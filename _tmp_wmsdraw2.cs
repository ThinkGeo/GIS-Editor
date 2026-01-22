using System;
using System.Linq;
using System.Reflection;
class P{
  static void Main(){
    var asm=Assembly.LoadFrom("C:\\Source\\gis-editor\\packages\\ThinkGeo.Core.14.4.3\\lib\\net462\\ThinkGeo.Core.dll");
    var t=asm.GetType("ThinkGeo.Core.WmsAsyncLayer");
    var methods = t.GetMethods(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)
                   .Where(m=>m.Name.Contains("Draw"));
    foreach(var m in methods){ Console.WriteLine(m); }
  }
}
