using System;
using System.Collections.Generic;
using System.IO;
using HypeRate.SDK;
using HypeRate.SDK.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HypeRate.ReleaseTools
{
    internal static class PulseLabSceneBuilder
    {
        private const string SampleRoot = "Assets/PulseLab";

        public static void Build()
        {
            Directory.CreateDirectory(SampleRoot);
            EditorSettings.serializationMode = SerializationMode.ForceText;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floorRingMesh = CreateOrReplaceMesh($"{SampleRoot}/EnergyRingMesh.asset", CreateTorusMesh());

            ConfigureEnvironment();
            var sceneCamera = CreateCamera();
            var roomRoot = new GameObject("PULSE LAB // ARCHITECTURE").transform;
            var roomRenderers = new List<Renderer>();
            var lightStripRenderers = new List<Renderer>();
            BuildRoom(roomRoot, roomRenderers, lightStripRenderers);

            var sphereRoot = new GameObject("BIOMETRIC SPHERE").transform;
            sphereRoot.position = new Vector3(2.05f, 0.5f, 0f);

            var glassSphere = CreateSphere("Glass Shell", sphereRoot, Vector3.zero, 4.56f, out var glassRenderer);
            var energySphere = CreateSphere("Energy Field", sphereRoot, Vector3.zero, 3.04f, out var energyRenderer);
            var coreSphere = CreateSphere("Pulse Core", sphereRoot, Vector3.zero, 1.36f, out var coreRenderer);

            var waveObject = new GameObject("Heart Rate Wave");
            waveObject.transform.SetParent(sphereRoot, false);
            var heartWave = waveObject.AddComponent<LineRenderer>();
            heartWave.useWorldSpace = false;
            heartWave.alignment = LineAlignment.View;
            heartWave.numCapVertices = 6;
            heartWave.numCornerVertices = 3;
            heartWave.positionCount = 128;
            heartWave.widthMultiplier = 0.025f;
            heartWave.shadowCastingMode = ShadowCastingMode.Off;
            heartWave.receiveShadows = false;

            var particleObject = new GameObject("Biometric Data Particles");
            particleObject.transform.SetParent(sphereRoot, false);
            var particles = particleObject.AddComponent<ParticleSystem>();
            ConfigureParticles(particles);
            var particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            particleRenderer.mesh = glassSphere.GetComponent<MeshFilter>().sharedMesh;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            var floorPulseRoot = new GameObject("Floor Pulse").transform;
            floorPulseRoot.SetParent(roomRoot, false);
            floorPulseRoot.position = new Vector3(2.05f, -1.95f, 0f);
            var floorPulseRenderers = new Renderer[2];
            for (var index = 0; index < floorPulseRenderers.Length; index++)
            {
                var ring = new GameObject($"Floor Pulse {index + 1}");
                ring.transform.SetParent(floorPulseRoot, false);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ring.transform.localScale = Vector3.one * (2.45f + index * 0.38f);
                ring.AddComponent<MeshFilter>().sharedMesh = floorRingMesh;
                floorPulseRenderers[index] = ring.AddComponent<MeshRenderer>();
            }

            var coreLight = CreateLight(
                "Pulse Core Light",
                sphereRoot,
                new Vector3(0f, -0.15f, -1.35f),
                LightType.Point,
                new Color(1f, 0.015f, 0.16f),
                9f,
                8f);
            coreLight.shadows = LightShadows.Soft;

            var rimLightObject = new GameObject("Violet Rim Light");
            rimLightObject.transform.SetParent(roomRoot, false);
            rimLightObject.transform.position = new Vector3(6.5f, 5.4f, -3f);
            LookAt(rimLightObject.transform, sphereRoot.position);
            var rimLight = rimLightObject.AddComponent<Light>();
            rimLight.type = LightType.Spot;
            rimLight.color = new Color(0.34f, 0.39f, 1f);
            rimLight.range = 18f;
            rimLight.spotAngle = 58f;
            rimLight.intensity = 4.5f;
            rimLight.shadows = LightShadows.Soft;

            var roomLight = CreateLight(
                "Room Bounce Light",
                roomRoot,
                new Vector3(-4.6f, 0.5f, 1.5f),
                LightType.Point,
                new Color(1f, 0.08f, 0.22f),
                12f,
                1.8f);

            var topLightObject = new GameObject("Soft Top Light");
            topLightObject.transform.SetParent(roomRoot, false);
            topLightObject.transform.position = new Vector3(1.7f, 6.2f, -1.5f);
            LookAt(topLightObject.transform, sphereRoot.position);
            var topLight = topLightObject.AddComponent<Light>();
            topLight.type = LightType.Spot;
            topLight.color = new Color(1f, 0.18f, 0.32f);
            topLight.range = 14f;
            topLight.spotAngle = 72f;
            topLight.intensity = 3.1f;
            topLight.shadows = LightShadows.Soft;

            var reflectionObject = new GameObject("Realtime Reflection Probe");
            reflectionObject.transform.SetParent(roomRoot, false);
            reflectionObject.transform.position = new Vector3(2f, 0.3f, 0f);
            var reflectionProbe = reflectionObject.AddComponent<ReflectionProbe>();
            reflectionProbe.mode = ReflectionProbeMode.Realtime;
            reflectionProbe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            reflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            reflectionProbe.size = new Vector3(17f, 9f, 15f);
            reflectionProbe.intensity = 1.15f;
            reflectionProbe.resolution = 128;
            reflectionProbe.boxProjection = true;

            var systems = new GameObject("HypeRate Pulse Lab");
            var client = systems.AddComponent<HypeRateClient>();
            var simulator = systems.AddComponent<HypeRateSimulator>();
            var controller = systems.AddComponent<HypeRatePulseLabController>();

            SetSerializedProperty(client, "connectOnStart", false);
            SetSerializedProperty(simulator, "client", client);
            SetSerializedProperty(simulator, "preset", (int)HypeRateSimulationPreset.Workout);
            SetSerializedProperty(simulator, "playOnStart", true);

            var controllerObject = new SerializedObject(controller);
            controllerObject.FindProperty("client").objectReferenceValue = client;
            controllerObject.FindProperty("simulator").objectReferenceValue = simulator;
            controllerObject.FindProperty("sphereRoot").objectReferenceValue = sphereRoot;
            controllerObject.FindProperty("glassSphere").objectReferenceValue = glassSphere;
            controllerObject.FindProperty("energySphere").objectReferenceValue = energySphere;
            controllerObject.FindProperty("coreSphere").objectReferenceValue = coreSphere;
            controllerObject.FindProperty("floorPulseRoot").objectReferenceValue = floorPulseRoot;
            controllerObject.FindProperty("heartWave").objectReferenceValue = heartWave;
            controllerObject.FindProperty("dataParticles").objectReferenceValue = particles;
            controllerObject.FindProperty("glassRenderer").objectReferenceValue = glassRenderer;
            controllerObject.FindProperty("energyRenderer").objectReferenceValue = energyRenderer;
            controllerObject.FindProperty("coreRenderer").objectReferenceValue = coreRenderer;
            controllerObject.FindProperty("particleRenderer").objectReferenceValue = particleRenderer;
            SetObjectArray(controllerObject.FindProperty("roomRenderers"), roomRenderers.ToArray());
            SetObjectArray(controllerObject.FindProperty("lightStripRenderers"), lightStripRenderers.ToArray());
            SetObjectArray(controllerObject.FindProperty("floorPulseRenderers"), floorPulseRenderers);
            controllerObject.FindProperty("coreLight").objectReferenceValue = coreLight;
            controllerObject.FindProperty("rimLight").objectReferenceValue = rimLight;
            controllerObject.FindProperty("roomLight").objectReferenceValue = roomLight;
            controllerObject.FindProperty("sceneCamera").objectReferenceValue = sceneCamera;
            controllerObject.ApplyModifiedPropertiesWithoutUndo();

            var scenePath = $"{SampleRoot}/PulseLab.unity";
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new InvalidOperationException($"Could not save {scenePath}.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"Generated HypeRate Pulse Lab at {scenePath}");
        }

        private static void ConfigureEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.035f, 0.042f, 0.09f);
            RenderSettings.ambientEquatorColor = new Color(0.018f, 0.022f, 0.052f);
            RenderSettings.ambientGroundColor = new Color(0.006f, 0.007f, 0.016f);
            RenderSettings.ambientIntensity = 0.72f;
            RenderSettings.reflectionIntensity = 1.05f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.fogColor = new Color(0.008f, 0.009f, 0.022f);
        }

        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var sceneCamera = cameraObject.AddComponent<Camera>();
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.012f, 0.016f, 0.035f);
            sceneCamera.fieldOfView = 43f;
            sceneCamera.nearClipPlane = 0.1f;
            sceneCamera.farClipPlane = 80f;
            sceneCamera.allowHDR = true;
            sceneCamera.allowMSAA = true;
            cameraObject.transform.position = new Vector3(1.1f, 0.75f, -12.5f);
            LookAt(cameraObject.transform, new Vector3(1.35f, 0.35f, 0f));
            return sceneCamera;
        }

        private static void BuildRoom(
            Transform roomRoot,
            List<Renderer> roomRenderers,
            List<Renderer> lightStripRenderers)
        {
            roomRenderers.Add(CreateCube("Reflective Floor", roomRoot, new Vector3(0.5f, -2.15f, 1f), new Vector3(20f, 0.28f, 18f)));
            roomRenderers.Add(CreateCube("Back Wall", roomRoot, new Vector3(0.5f, 2.9f, 6.5f), new Vector3(20f, 10f, 0.35f)));
            roomRenderers.Add(CreateCube("Left Wall", roomRoot, new Vector3(-9.2f, 2.6f, 0.8f), new Vector3(0.35f, 9.5f, 12f)));
            roomRenderers.Add(CreateCube("Right Wall", roomRoot, new Vector3(10.2f, 2.6f, 0.8f), new Vector3(0.35f, 9.5f, 12f)));
            roomRenderers.Add(CreateCube("Ceiling", roomRoot, new Vector3(0.5f, 7.2f, 0.5f), new Vector3(20f, 0.25f, 12f)));

            for (var index = 0; index < 5; index++)
            {
                var x = -6.4f + index * 3.35f;
                roomRenderers.Add(CreateCube(
                    $"Wall Panel {index + 1}",
                    roomRoot,
                    new Vector3(x, 3.05f, 6.28f),
                    new Vector3(2.78f, 7.4f, 0.13f)));
            }

            roomRenderers.Add(CreateCube("Upper Left Beam", roomRoot, new Vector3(-4.7f, 6f, 3f), new Vector3(8.5f, 0.22f, 0.38f), new Vector3(0f, 7f, 0f)));
            roomRenderers.Add(CreateCube("Upper Right Beam", roomRoot, new Vector3(5.2f, 5.8f, 3.1f), new Vector3(8f, 0.22f, 0.38f), new Vector3(0f, -8f, 0f)));

            lightStripRenderers.Add(CreateCube("Warm Ceiling Strip", roomRoot, new Vector3(-3.9f, 5.86f, 2.78f), new Vector3(7f, 0.035f, 0.08f), new Vector3(0f, 7f, 0f)));
            lightStripRenderers.Add(CreateCube("Violet Ceiling Strip", roomRoot, new Vector3(5.1f, 5.66f, 2.88f), new Vector3(6.7f, 0.035f, 0.08f), new Vector3(0f, -8f, 0f)));
            lightStripRenderers.Add(CreateCube("Left Floor Strip", roomRoot, new Vector3(-5.2f, -1.93f, 5.95f), new Vector3(6.4f, 0.035f, 0.07f)));
            lightStripRenderers.Add(CreateCube("Right Floor Strip", roomRoot, new Vector3(6.3f, -1.93f, 5.95f), new Vector3(5.2f, 0.035f, 0.07f)));

            for (var index = 0; index < 7; index++)
            {
                lightStripRenderers.Add(CreateCube(
                    $"Right Vertical Strip {index + 1}",
                    roomRoot,
                    new Vector3(8.8f + index * 0.13f, 1.1f, 5.93f),
                    new Vector3(0.035f, 4.8f - index * 0.25f, 0.05f)));
            }
        }

        private static Transform CreateSphere(
            string name,
            Transform parent,
            Vector3 localPosition,
            float localScale,
            out Renderer sphereRenderer)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localScale = Vector3.one * localScale;
            RemoveCollider(sphere);
            sphereRenderer = sphere.GetComponent<Renderer>();
            return sphere.transform;
        }

        private static Renderer CreateCube(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Vector3? rotation = null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.transform.localEulerAngles = rotation ?? Vector3.zero;
            RemoveCollider(cube);
            return cube.GetComponent<Renderer>();
        }

        private static Light CreateLight(
            string name,
            Transform parent,
            Vector3 localPosition,
            LightType type,
            Color color,
            float range,
            float intensity)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;
            var targetLight = lightObject.AddComponent<Light>();
            targetLight.type = type;
            targetLight.color = color;
            targetLight.range = range;
            targetLight.intensity = intensity;
            return targetLight;
        }

        private static void ConfigureParticles(ParticleSystem particles)
        {
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.09f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.055f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.08f, 0.3f, 0.35f),
                new Color(1f, 0.32f, 0.58f, 0.9f));
            main.maxParticles = 260;

            var emission = particles.emission;
            emission.rateOverTime = 22f;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2.75f;
            shape.radiusThickness = 0.34f;

            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.08f;
            noise.damping = true;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.1f, 0.3f), 0f),
                    new GradientColorKey(new Color(0.55f, 0.2f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.2f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;
        }

        private static Mesh CreateTorusMesh()
        {
            const int ringSegments = 96;
            const int tubeSegments = 8;
            const float majorRadius = 1f;
            const float minorRadius = 0.012f;
            var vertices = new Vector3[ringSegments * tubeSegments];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[ringSegments * tubeSegments * 6];

            for (var ring = 0; ring < ringSegments; ring++)
            {
                var u = ring / (float)ringSegments * Mathf.PI * 2f;
                var radial = new Vector3(Mathf.Cos(u), Mathf.Sin(u), 0f);
                for (var tube = 0; tube < tubeSegments; tube++)
                {
                    var v = tube / (float)tubeSegments * Mathf.PI * 2f;
                    var normal = radial * Mathf.Cos(v) + Vector3.forward * Mathf.Sin(v);
                    var index = ring * tubeSegments + tube;
                    vertices[index] = radial * majorRadius + normal * minorRadius;
                    normals[index] = normal;
                }
            }

            var triangleIndex = 0;
            for (var ring = 0; ring < ringSegments; ring++)
            {
                var nextRing = (ring + 1) % ringSegments;
                for (var tube = 0; tube < tubeSegments; tube++)
                {
                    var nextTube = (tube + 1) % tubeSegments;
                    var a = ring * tubeSegments + tube;
                    var b = nextRing * tubeSegments + tube;
                    var c = ring * tubeSegments + nextTube;
                    var d = nextRing * tubeSegments + nextTube;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = d;
                }
            }

            var mesh = new Mesh { name = "HypeRate Floor Pulse Ring" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateOrReplaceMesh(string path, Mesh mesh)
        {
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void SetSerializedProperty(UnityEngine.Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedProperty(UnityEngine.Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).enumValueIndex = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedProperty(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray<T>(SerializedProperty property, T[] values) where T : UnityEngine.Object
        {
            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void LookAt(Transform source, Vector3 target)
        {
            source.rotation = Quaternion.LookRotation(target - source.position, Vector3.up);
        }
    }
}
