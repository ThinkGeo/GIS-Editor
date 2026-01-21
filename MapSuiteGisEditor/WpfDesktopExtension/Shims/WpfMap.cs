using System;

namespace ThinkGeo.UI.Wpf
{
    /// <summary>
    /// Compatibility shim for Map Suite v10's <c>WpfMap</c> control.
    ///
    /// ThinkGeo v14's primary WPF map control is <see cref="MapView"/>. The GIS Editor codebase
    /// (and many plugins) still references <c>WpfMap</c> as a type.
    ///
    /// This shim keeps those references compiling by deriving from <see cref="MapView"/>.
    /// </summary>
    [Serializable]
    public class WpfMap : MapView
    {
        public WpfMap()
        {
        }
    }
}
