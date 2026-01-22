using System;
using System.Drawing.Drawing2D;

namespace ThinkGeo.Core
{
    /// <summary>
    /// Compatibility shim for Map Suite v10's <c>PlatformGeoCanvas</c>.
    ///
    /// In ThinkGeo v14+, <see cref="SkiaGeoCanvas"/> is the default desktop canvas implementation.
    /// A lot of legacy GIS Editor code still instantiates <c>PlatformGeoCanvas</c> directly for
    /// operations like <c>MeasureText</c> and generating image streams.
    ///
    /// This shim keeps that legacy code compiling by simply inheriting from <see cref="SkiaGeoCanvas"/>.
    /// </summary>
    [Serializable]
    public class PlatformGeoCanvas : SkiaGeoCanvas
    {
        public CompositingQuality CompositingQuality { get; set; } = CompositingQuality.HighSpeed;

        public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.HighSpeed;

        // Intentionally empty. All functionality is inherited from SkiaGeoCanvas / GeoCanvas.
    }
}
