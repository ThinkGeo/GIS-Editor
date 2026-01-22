using System.Threading;

namespace ThinkGeo.Core
{
    public static class LayerBaseLegacyExtensions
    {
        public static void Open(this LayerBase layerBase)
        {
            if (layerBase == null || layerBase.IsOpen) return;

            if (layerBase is Layer syncLayer)
            {
                syncLayer.Open();
            }
            else if (layerBase is AsyncLayer asyncLayer)
            {
                asyncLayer.OpenAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        public static void Close(this LayerBase layerBase)
        {
            if (layerBase == null || !layerBase.IsOpen) return;

            if (layerBase is Layer syncLayer)
            {
                syncLayer.Close();
            }
            else if (layerBase is AsyncLayer asyncLayer)
            {
                asyncLayer.CloseAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }
    }
}
