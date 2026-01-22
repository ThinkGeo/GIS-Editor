using System;
using System.Reflection;
class P{
  static void Main(){
    var asm=Assembly.LoadFrom("C:\\Source\\gis-editor\\packages\\ThinkGeo.Core.14.4.3\\lib\\net462\\ThinkGeo.Core.dll");
    var layer = asm.GetType("ThinkGeo.Core.Layer");
    var layerBase = asm.GetType("ThinkGeo.Core.LayerBase");
    Console.WriteLine("Layer base: "+layer.BaseType);
    Console.WriteLine("LayerBase base: "+layerBase.BaseType);
    var wms = asm.GetType("ThinkGeo.Core.WmsAsyncLayer");
    Console.WriteLine("Wms base: "+wms.BaseType);
    Console.WriteLine("Is Layer assignable from Wms: "+layer.IsAssignableFrom(wms));
    Console.WriteLine("Is LayerBase assignable from Wms: "+layerBase.IsAssignableFrom(wms));
  }
}
