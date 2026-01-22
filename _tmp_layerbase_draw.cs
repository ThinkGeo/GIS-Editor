using System;
using System.Linq;
using System.Reflection;
class P{
  static void Main(){
    var asm=Assembly.LoadFrom("C:\\Source\\gis-editor\\packages\\ThinkGeo.Core.14.4.3\\lib\\net462\\ThinkGeo.Core.dll");
    var t=asm.GetType("ThinkGeo.Core.LayerBase");
    var m = t.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Where(x=>x.Name=="Draw");
    foreach(var x in m){ Console.WriteLine(x); }
  }
}
