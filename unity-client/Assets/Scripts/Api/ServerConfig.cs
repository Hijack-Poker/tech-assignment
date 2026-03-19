namespace HijackPoker.Api
{
    /// <summary>
    /// Central server URL config. In WebGL builds served from the gateway,
    /// URLs are derived from the page origin (same-origin /api and /ws).
    /// In Editor and standalone builds, uses localhost with direct ports.
    /// </summary>
    public static class ServerConfig
    {
        private const string LanHost = "10.10.0.32";

#if UNITY_WEBGL && !UNITY_EDITOR
        public static string HttpBaseUrl
        {
            get
            {
                var uri = new System.Uri(UnityEngine.Application.absoluteURL);
                return $"{uri.Scheme}://{uri.Authority}/api";
            }
        }

        public static string WsBaseUrl
        {
            get
            {
                var uri = new System.Uri(UnityEngine.Application.absoluteURL);
                string ws = uri.Scheme == "https" ? "wss" : "ws";
                return $"{ws}://{uri.Authority}/ws";
            }
        }
#else
        private const string Host = "localhost";
        public const string HttpBaseUrl = "http://" + Host + ":3030";
        public const string WsBaseUrl   = "ws://"   + Host + ":3032";
#endif
    }
}
