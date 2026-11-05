using System;
using System.IO;
using HypeRate.SDK;
using HypeRate.SDK.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HypeRate.ReleaseTools
{
    internal static class HeartRateWidgetSceneBuilder
    {
        private const string OutputDirectory = "Assets/HeartRateWidgetGenerated";

        public static void Build()
        {
            Directory.CreateDirectory(OutputDirectory);
            EditorSettings.serializationMode = SerializationMode.ForceText;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.013f, 0.018f);

            var widgetObject = new GameObject("HypeRate Heart Rate Widget");
            var client = widgetObject.AddComponent<HypeRateClient>();
            var simulator = widgetObject.AddComponent<HypeRateSimulator>();
            var widget = widgetObject.AddComponent<HypeRateHeartRateWidget>();
            var demo = widgetObject.AddComponent<HypeRateWidgetDemo>();

            SetObjectReference(simulator, "client", client);
            SetValue(simulator, "preset", (int)HypeRateSimulationPreset.Workout);
            SetValue(simulator, "playOnStart", false);
            SetValue(client, "connectOnStart", false);
            SetObjectReference(widget, "client", client);
            SetObjectReference(demo, "client", client);
            SetObjectReference(demo, "simulator", simulator);

            var scenePath = $"{OutputDirectory}/HeartRateWidget.unity";
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new InvalidOperationException($"Could not save {scenePath}.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"Generated HypeRate widget sample at {scenePath}");
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetValue(UnityEngine.Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetValue(UnityEngine.Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).enumValueIndex = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
