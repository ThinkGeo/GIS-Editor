using System;
using System.Reflection;
class P{
  static void Main(){
    var asm=Assembly.LoadFrom("C:\\Source\\gis-editor\\packages\\ThinkGeo.Core.14.4.3\\lib\\net462\\ThinkGeo.Core.dll");
    var t=asm.GetType("ThinkGeo.Core.Projection");
    Console.WriteLine(t);
    foreach(var c in t.GetConstructors()){
      Console.WriteLine(c);
    }
  }
}
