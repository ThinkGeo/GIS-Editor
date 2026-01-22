using System;
using System.Reflection;
class P{
  static void Main(){
    var asm=Assembly.LoadFrom("C:\\Source\\gis-editor\\packages\\ThinkGeo.Core.14.4.3\\lib\\net462\\ThinkGeo.Core.dll");
    var t=asm.GetType("ThinkGeo.Core.Layer");
    var m=t.GetMethod("OpenCore", BindingFlags.Instance|BindingFlags.NonPublic);
    Console.WriteLine(m.IsAbstract+" "+m.IsVirtual);
    var c=t.GetMethod("CloseCore", BindingFlags.Instance|BindingFlags.NonPublic);
    Console.WriteLine(c.IsAbstract+" "+c.IsVirtual);
  }
}
