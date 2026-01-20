using System;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    /// <summary>
    /// Compatibility event args kept for older GIS Editor extension APIs.
    /// In ThinkGeo v14+, some web based overlays expose different request events.
    /// These lightweight args keep the extension buildable without depending on
    /// specific internal event argument types.
    /// </summary>
    [Serializable]
    public class SendingWebRequestEventArgs : EventArgs
    {
        public SendingWebRequestEventArgs(Uri requestUri = null)
        {
            RequestUri = requestUri;
        }

        public Uri RequestUri { get; }
    }

    /// <summary>
    /// Compatibility event args kept for older GIS Editor extension APIs.
    /// </summary>
    [Serializable]
    public class SentWebRequestEventArgs : EventArgs
    {
        public SentWebRequestEventArgs(Uri requestUri = null)
        {
            RequestUri = requestUri;
        }

        public Uri RequestUri { get; }
    }
}
