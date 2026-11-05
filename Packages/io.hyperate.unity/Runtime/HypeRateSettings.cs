using UnityEngine;

namespace HypeRate.SDK
{
    public sealed class HypeRateSettings : ScriptableObject
    {
        public const string DefaultEndpoint = "wss://app.hyperate.io/socket/websocket";
        internal const string EditorAssetPath = "Assets/HypeRateGenerated/Editor/HypeRateSettings.asset";

        [SerializeField] private string apiToken = string.Empty;
        [SerializeField] private string deviceId = "internal-testing";
        [SerializeField] private string endpoint = DefaultEndpoint;

        public string ApiToken => apiToken;
        public string DeviceId => deviceId;
        public string Endpoint => string.IsNullOrWhiteSpace(endpoint) ? DefaultEndpoint : endpoint.Trim();
        public bool IsConfigured => !string.IsNullOrWhiteSpace(apiToken) && !string.IsNullOrWhiteSpace(deviceId);

        internal static HypeRateSettings LoadEditorSettings()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<HypeRateSettings>(EditorAssetPath);
#else
            return null;
#endif
        }
    }
}
