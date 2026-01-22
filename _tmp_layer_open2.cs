using System;
using System.Linq;
using System.Reflection;
class P{
  static void Main(){
    var asm=Assembly.LoadFrom("C:\\Source\\gis-editor\\packages\\ThinkGeo.Core.14.4.3\\lib\\net462\\ThinkGeo.Core.dll");
    var t=asm.GetType("ThinkGeo.Core.Layer");
    var methods = t.GetMethods(BindingFlags.Instance|BindingFlags.NonPublic).Where(m=>m.Name.Contains("Open") || m.Name.Contains("Close"));
    foreach(var m in methods){ Console.WriteLine(m); }
  }
}
