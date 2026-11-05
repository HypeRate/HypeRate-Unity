using HypeRate.SDK;
using UnityEditor;
using UnityEngine;

namespace HypeRate.SDK.Editor
{
    [CustomEditor(typeof(HypeRateClient))]
    public sealed class HypeRateClientEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                HypeRateSettings.LoadEditorSettings() == null
                    ? "HypeRate is not configured yet. Open Tools > HypeRate > Setup."
                    : "Connection settings are managed under Tools > HypeRate > Setup.",
                HypeRateSettings.LoadEditorSettings() == null ? MessageType.Warning : MessageType.Info);

            if (GUILayout.Button("Open HypeRate Setup"))
            {
                HypeRateSetupWindow.Open();
            }

            EditorGUILayout.Space(6f);
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            var client = (HypeRateClient)target;
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
            HypeRateSetupWindow.DrawConnectionStatus(client.State);
            EditorGUILayout.LabelField("Heart Rate", client.CurrentHeartRate > 0 ? $"{client.CurrentHeartRate} BPM" : "—");

            if (!string.IsNullOrWhiteSpace(client.LastError))
            {
                EditorGUILayout.HelpBox(client.LastError, MessageType.Warning);
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test the connection.", MessageType.None);
                return;
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

            Repaint();
        }
    }
}
