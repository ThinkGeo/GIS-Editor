using System;
using System.Linq;
using System.Reflection;
class P{static void Main(){
var asm=Assembly.LoadFrom(@"C:\Source\gis-editor\packages\ThinkGeo.Core.14.4.3\lib\net462\ThinkGeo.Core.dll");
var t=asm.GetType("ThinkGeo.Core.GeoColor");
Console.WriteLine("GeoColor props:");
foreach(var p in t.GetProperties(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static)) Console.WriteLine(p.Name+" "+p.PropertyType);
Console.WriteLine("fields:");
foreach(var f in t.GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static)) Console.WriteLine(f.Name+" "+f.FieldType);
}
}
