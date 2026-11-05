using System.IO;
using HypeRate.SDK;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HypeRate.SDK.Editor
{
    public sealed class HypeRateSetupWindow : EditorWindow
    {
        private const string SettingsFolder = "Assets/HypeRateGenerated";
        private const string EditorSettingsFolder = SettingsFolder + "/Editor";
        private const string SettingsPath = EditorSettingsFolder + "/HypeRateSettings.asset";
        private const string LegacySettingsPath = SettingsFolder + "/Resources/HypeRateSettings.asset";
        private const string GitIgnoreRule = "/Assets/HypeRateGenerated/";

        private string apiToken = string.Empty;
        private string deviceId = "internal-testing";
        private string endpoint = HypeRateSettings.DefaultEndpoint;
        private string feedbackMessage = string.Empty;
        private double feedbackVisibleUntil;
        private Vector2 scrollPosition;

        [MenuItem("Tools/HypeRate/Setup", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<HypeRateSetupWindow>();
            window.titleContent = new GUIContent("HypeRate Setup");
            window.minSize = new Vector2(440f, 470f);
            window.Show();
        }

        [MenuItem("GameObject/HypeRate/HypeRate Client", false, 10)]
        private static void AddClientFromMenu(MenuCommand command)
        {
            CreateOrSelectClient();
        }

        private void OnEnable()
        {
            LoadSettingsIntoForm();
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying || feedbackVisibleUntil > EditorApplication.timeSinceStartup)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("HypeRate Setup", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Connect a HypeRate device without writing WebSocket or reconnect code.",
                EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space(14f);
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            apiToken = EditorGUILayout.PasswordField(new GUIContent("API Token", "Your HypeRate WebSocket API token."), apiToken);
            deviceId = EditorGUILayout.TextField(new GUIContent("Device ID", "The Session ID shown in the HypeRate app."), deviceId);

            EditorGUILayout.Space(6f);
            using (new EditorGUI.IndentLevelScope())
            {
                endpoint = EditorGUILayout.TextField("Endpoint", endpoint);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "The local test settings are stored below Assets/HypeRateGenerated/Editor, excluded from Git, " +
                "and not included in player builds. Configure production credentials at runtime.",
                MessageType.Info);

            if (GUILayout.Button("Save Settings", GUILayout.Height(32f)))
            {
                SaveSettings();
            }

            DrawFeedback();

            EditorGUILayout.Space(18f);
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            var client = FindClient();
            EditorGUILayout.LabelField("HypeRate Client", client == null ? "Not in the current scene" : client.name);

            if (GUILayout.Button(client == null ? "Add HypeRate Client to Scene" : "Select HypeRate Client", GUILayout.Height(32f)))
            {
                if (SaveSettings())
                {
                    CreateOrSelectClient();
                }
            }

            EditorGUILayout.Space(18f);
            DrawRuntimeStatus(client);

            EditorGUILayout.Space(16f);
            EditorGUILayout.HelpBox(
                "Quick test: save the settings, add the client, then enter Play Mode. " +
                "The client connects automatically and its Inspector shows the current BPM.",
                MessageType.None);

            EditorGUILayout.EndScrollView();
        }

        private void DrawRuntimeStatus(HypeRateClient client)
        {
            EditorGUILayout.LabelField("Live Status", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                DrawConnectionStatus(HypeRateConnectionState.Disconnected, "Not running");

                if (client != null && GUILayout.Button("Start Connection Test", GUILayout.Height(32f)))
                {
                    StartConnectionTest();
                }

                return;
            }

            if (client == null)
            {
                EditorGUILayout.HelpBox("No HypeRate Client exists in the active scene.", MessageType.Warning);
                return;
            }

            DrawConnectionStatus(client.State);
            EditorGUILayout.LabelField("Heart Rate", client.CurrentHeartRate > 0 ? $"{client.CurrentHeartRate} BPM" : "Waiting for data");

            if (!string.IsNullOrWhiteSpace(client.LastError))
            {
                EditorGUILayout.HelpBox(client.LastError, MessageType.Warning);
            }

            if (client.IsConnected)
            {
                if (GUILayout.Button("Disconnect"))
                {
                    client.Disconnect();
                }
            }
            else if (GUILayout.Button("Connect"))
            {
                client.Connect();
            }
        }

        internal static void DrawConnectionStatus(HypeRateConnectionState state, string overrideText = null)
        {
            GetStatusPresentation(state, out var text, out var color);
            if (!string.IsNullOrWhiteSpace(overrideText))
            {
                text = overrideText;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Status");
                var dotStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 17,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = color }
                };
                GUILayout.Label("●", dotStyle, GUILayout.Width(18f));
                GUILayout.Label(text, EditorStyles.label);
            }
        }

        private static void GetStatusPresentation(HypeRateConnectionState state, out string text, out Color color)
        {
            switch (state)
            {
                case HypeRateConnectionState.Connected:
                    text = "Connected";
                    color = new Color(0.18f, 0.82f, 0.35f);
                    return;
                case HypeRateConnectionState.Connecting:
                    text = "Connecting";
                    color = new Color(1f, 0.68f, 0.08f);
                    return;
                case HypeRateConnectionState.Reconnecting:
                    text = "Reconnecting";
                    color = new Color(1f, 0.68f, 0.08f);
                    return;
                case HypeRateConnectionState.Disconnecting:
                    text = "Disconnecting";
                    color = new Color(1f, 0.68f, 0.08f);
                    return;
                case HypeRateConnectionState.Simulating:
                    text = "Simulation";
                    color = new Color(0.18f, 0.58f, 1f);
                    return;
                case HypeRateConnectionState.Error:
                    text = "Error";
                    color = new Color(1f, 0.24f, 0.24f);
                    return;
                default:
                    text = "Disconnected";
                    color = new Color(1f, 0.24f, 0.24f);
                    return;
            }
        }

        private void LoadSettingsIntoForm()
        {
            var settings = AssetDatabase.LoadAssetAtPath<HypeRateSettings>(SettingsPath) ??
                           AssetDatabase.LoadAssetAtPath<HypeRateSettings>(LegacySettingsPath);
            if (settings == null)
            {
                return;
            }

            var serializedSettings = new SerializedObject(settings);
            apiToken = serializedSettings.FindProperty("apiToken").stringValue;
            deviceId = serializedSettings.FindProperty("deviceId").stringValue;
            endpoint = serializedSettings.FindProperty("endpoint").stringValue;
        }

        private bool SaveSettings()
        {
            if (string.IsNullOrWhiteSpace(apiToken) || string.IsNullOrWhiteSpace(deviceId))
            {
                EditorUtility.DisplayDialog("HypeRate Setup", "API Token and Device ID are required.", "OK");
                return false;
            }

            EnsureFolder("Assets", "HypeRateGenerated");
            EnsureFolder(SettingsFolder, "Editor");

            var settings = AssetDatabase.LoadAssetAtPath<HypeRateSettings>(SettingsPath);
            if (settings == null)
            {
                var legacySettings = AssetDatabase.LoadAssetAtPath<HypeRateSettings>(LegacySettingsPath);
                if (legacySettings != null)
                {
                    var moveError = AssetDatabase.MoveAsset(LegacySettingsPath, SettingsPath);
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        EditorUtility.DisplayDialog(
                            "HypeRate Setup",
                            $"Could not move the legacy test settings out of Resources: {moveError}",
                            "OK");
                        return false;
                    }

                    settings = AssetDatabase.LoadAssetAtPath<HypeRateSettings>(SettingsPath);
                }
                else
                {
                    settings = CreateInstance<HypeRateSettings>();
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                }
            }

            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("apiToken").stringValue = apiToken.Trim();
            serializedSettings.FindProperty("deviceId").stringValue = deviceId.Trim();
            serializedSettings.FindProperty("endpoint").stringValue = string.IsNullOrWhiteSpace(endpoint)
                ? HypeRateSettings.DefaultEndpoint
                : endpoint.Trim();
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            EnsureGitIgnore();

            feedbackMessage = "Editor test settings saved locally.";
            feedbackVisibleUntil = EditorApplication.timeSinceStartup + 3d;
            return true;
        }

        private void DrawFeedback()
        {
            if (string.IsNullOrEmpty(feedbackMessage))
            {
                return;
            }

            if (EditorApplication.timeSinceStartup >= feedbackVisibleUntil)
            {
                feedbackMessage = string.Empty;
                return;
            }

            EditorGUILayout.HelpBox(feedbackMessage, MessageType.Info);
        }

        private void StartConnectionTest()
        {
            if (!SaveSettings())
            {
                return;
            }

            CreateOrSelectClient();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }

        private static HypeRateClient CreateOrSelectClient()
        {
            var client = FindClient();
            if (client == null)
            {
                var gameObject = new GameObject("HypeRate Client");
                Undo.RegisterCreatedObjectUndo(gameObject, "Add HypeRate Client");
                client = gameObject.AddComponent<HypeRateClient>();
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }

            Selection.activeGameObject = client.gameObject;
            EditorGUIUtility.PingObject(client.gameObject);
            return client;
        }

        private static HypeRateClient FindClient()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<HypeRateClient>();
#else
            return Object.FindObjectOfType<HypeRateClient>();
#endif
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void EnsureGitIgnore()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return;
            }

            var gitIgnorePath = Path.Combine(projectRoot, ".gitignore");
            var existing = File.Exists(gitIgnorePath) ? File.ReadAllText(gitIgnorePath) : string.Empty;
            if (existing.Contains(GitIgnoreRule))
            {
                return;
            }

            var prefix = existing.Length == 0 || existing.EndsWith("\n") ? string.Empty : "\n";
            File.AppendAllText(gitIgnorePath, $"{prefix}\n# Local HypeRate credentials\n{GitIgnoreRule}\n");
        }
    }
}
