using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HypeRate.SDK.Samples
{
    public sealed class HypeRatePulseLabController : MonoBehaviour
    {
        private static readonly Color DeepNavy = new Color(0.012f, 0.016f, 0.035f);
        private static readonly Color PulseRed = new Color(1f, 0.015f, 0.17f);
        private static readonly Color PulseMagenta = new Color(1f, 0.04f, 0.34f);
        private static readonly Color VioletBlue = new Color(0.29f, 0.34f, 1f);

        [SerializeField] private HypeRateClient client;
        [SerializeField] private HypeRateSimulator simulator;
        [SerializeField] private Transform sphereRoot;
        [SerializeField] private Transform glassSphere;
        [SerializeField] private Transform energySphere;
        [SerializeField] private Transform coreSphere;
        [SerializeField] private Transform floorPulseRoot;
        [SerializeField] private LineRenderer heartWave;
        [SerializeField] private ParticleSystem dataParticles;
        [SerializeField] private Renderer glassRenderer;
        [SerializeField] private Renderer energyRenderer;
        [SerializeField] private Renderer coreRenderer;
        [SerializeField] private Renderer particleRenderer;
        [SerializeField] private Renderer[] roomRenderers;
        [SerializeField] private Renderer[] lightStripRenderers;
        [SerializeField] private Renderer[] floorPulseRenderers;
        [SerializeField] private Light coreLight;
        [SerializeField] private Light rimLight;
        [SerializeField] private Light roomLight;
        [SerializeField] private Camera sceneCamera;

        private readonly List<Object> runtimeResources = new List<Object>();
        private Material glassMaterial;
        private Material energyMaterial;
        private Material coreMaterial;
        private Material roomMaterial;
        private Material stripMaterial;
        private Material waveMaterial;
        private Material floorPulseMaterial;
        private Material particleMaterial;
        private Texture2D panelTexture;
        private Texture2D buttonTexture;
        private Texture2D activeButtonTexture;
        private Texture2D dividerTexture;
        private GUIStyle eyebrowStyle;
        private GUIStyle bpmStyle;
        private GUIStyle unitStyle;
        private GUIStyle statusStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;
        private GUIStyle activeButtonStyle;
        private float displayedHeartRate = 126f;
        private float beatClock;
        private float cameraBaseFieldOfView;
        private Vector3 cameraBasePosition;

        private void OnEnable()
        {
            if (client != null)
            {
                client.HeartRateChanged += HandleHeartRateChanged;
            }
        }

        private void Start()
        {
            CreateVisualResources();

            if (sceneCamera != null)
            {
                cameraBaseFieldOfView = sceneCamera.fieldOfView;
                cameraBasePosition = sceneCamera.transform.position;
            }
        }

        private void Update()
        {
            var targetHeartRate = client != null && client.CurrentHeartRate > 0
                ? client.CurrentHeartRate
                : displayedHeartRate;
            displayedHeartRate = Mathf.Lerp(
                displayedHeartRate,
                targetHeartRate,
                1f - Mathf.Exp(-Time.unscaledDeltaTime * 8f));

            var secondsPerBeat = 60f / Mathf.Max(30f, displayedHeartRate);
            beatClock = Mathf.Repeat(beatClock + Time.unscaledDeltaTime, secondsPerBeat);
            var phase = beatClock / secondsPerBeat;
            var primaryBeat = Mathf.Exp(-phase * 19f);
            var secondaryPhase = Mathf.Max(0f, phase - 0.2f);
            var secondaryBeat = phase >= 0.2f ? Mathf.Exp(-secondaryPhase * 29f) : 0f;
            var pulse = primaryBeat + secondaryBeat * 0.45f;

            AnimateSphere(pulse);
            AnimateWave(pulse);
            UpdateLighting(pulse);
        }

        private void OnDisable()
        {
            if (client != null)
            {
                client.HeartRateChanged -= HandleHeartRateChanged;
            }
        }

        private void OnDestroy()
        {
            foreach (var resource in runtimeResources)
            {
                if (resource != null)
                {
                    Destroy(resource);
                }
            }

            runtimeResources.Clear();
        }

        private void OnGUI()
        {
            EnsureGuiStyles();

            var scale = Mathf.Clamp(Screen.height / 1080f, 0.72f, 1.2f);
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var virtualWidth = Screen.width / scale;
            var virtualHeight = Screen.height / scale;

            var margin = Mathf.Clamp(virtualWidth * 0.026f, 26f, 46f);
            var panelWidth = Mathf.Clamp(virtualWidth * 0.245f, 360f, 425f);
            var panelHeight = Mathf.Clamp(virtualHeight - margin * 2f, 570f, 760f);
            var panelRect = new Rect(margin, margin, panelWidth, panelHeight);
            GUI.DrawTexture(panelRect, panelTexture, ScaleMode.StretchToFill, true);

            var x = panelRect.x + 30f;
            var width = panelRect.width - 60f;
            var y = panelRect.y + 28f;

            GUI.Label(new Rect(x, y, width, 22f), "HYPERATE  //  PULSE LAB", eyebrowStyle);
            y += 49f;
            GUI.Label(new Rect(x, y, 180f, 95f), Mathf.RoundToInt(displayedHeartRate).ToString(), bpmStyle);
            GUI.Label(new Rect(x + 150f, y + 55f, width - 150f, 28f), "BPM", unitStyle);
            y += 104f;

            var simulating = client != null && client.IsSimulating;
            var connected = client != null && client.IsConnected;
            var stateLabel = simulating ? "SIMULATION ACTIVE" : connected ? "LIVE DATA" : "DISCONNECTED";
            statusStyle.normal.textColor = simulating
                ? new Color(1f, 0.24f, 0.42f)
                : connected ? new Color(0.22f, 1f, 0.55f) : new Color(1f, 0.42f, 0.42f);
            GUI.Label(new Rect(x, y, width, 24f), $"●  {stateLabel}", statusStyle);
            y += 42f;

            GUI.Label(
                new Rect(x, y, width, 60f),
                "Real-time biometric data driving light, motion and atmosphere.",
                bodyStyle);
            y += 76f;
            GUI.DrawTexture(new Rect(x, y, width, 1f), dividerTexture);
            y += 30f;

            GUI.Label(new Rect(x, y, width, 22f), "SIMULATION PROFILE", eyebrowStyle);
            y += 32f;

            const float gap = 8f;
            var buttonWidth = (width - gap * 2f) / 3f;
            DrawPresetButton(new Rect(x, y, buttonWidth, 42f), "REST", HypeRateSimulationPreset.Resting);
            DrawPresetButton(new Rect(x + buttonWidth + gap, y, buttonWidth, 42f), "WORKOUT", HypeRateSimulationPreset.Workout);
            DrawPresetButton(new Rect(x + (buttonWidth + gap) * 2f, y, buttonWidth, 42f), "INTENSE", HypeRateSimulationPreset.Intense);
            y += 58f;

            if (GUI.Button(new Rect(x, y, width, 46f), "CONNECT LIVE DATA", buttonStyle))
            {
                simulator?.StopSimulation();
                client?.Connect();
            }

            y += 62f;
            if (client != null && !string.IsNullOrWhiteSpace(client.LastError))
            {
                var previousColor = bodyStyle.normal.textColor;
                bodyStyle.normal.textColor = new Color(1f, 0.5f, 0.5f);
                GUI.Label(new Rect(x, y, width, 70f), client.LastError, bodyStyle);
                bodyStyle.normal.textColor = previousColor;
            }

            GUI.Label(
                new Rect(x, panelRect.yMax - 43f, width, 24f),
                "Built with HypeRate for Unity 2.0",
                eyebrowStyle);

            GUI.matrix = previousMatrix;
        }

        private void DrawPresetButton(Rect rect, string label, HypeRateSimulationPreset targetPreset)
        {
            var active = simulator != null && simulator.IsRunning && simulator.Preset == targetPreset;
            if (!GUI.Button(rect, label, active ? activeButtonStyle : buttonStyle) || simulator == null)
            {
                return;
            }

            simulator.SetPreset(targetPreset);
            if (!simulator.IsRunning)
            {
                simulator.StartSimulation();
            }
        }

        private void AnimateSphere(float pulse)
        {
            var ambientMotion = Mathf.Sin(Time.unscaledTime * 0.5f) * 0.015f;

            if (sphereRoot != null)
            {
                sphereRoot.localPosition = new Vector3(
                    sphereRoot.localPosition.x,
                    0.5f + ambientMotion,
                    sphereRoot.localPosition.z);
            }

            if (glassSphere != null)
            {
                glassSphere.localScale = Vector3.one * (4.56f + pulse * 0.17f);
                glassSphere.Rotate(new Vector3(0.8f, 1.1f, 0.35f), 3.5f * Time.unscaledDeltaTime, Space.Self);
            }

            if (energySphere != null)
            {
                energySphere.localScale = Vector3.one * (3.04f + pulse * 0.36f);
                energySphere.localRotation = Quaternion.Euler(
                    Time.unscaledTime * -4f,
                    Time.unscaledTime * 7f,
                    Mathf.Sin(Time.unscaledTime * 0.4f) * 4f);
            }

            if (coreSphere != null)
            {
                coreSphere.localScale = Vector3.one * (1.36f + pulse * 0.4f);
            }

            if (floorPulseRoot != null)
            {
                floorPulseRoot.localScale = Vector3.one * (1f + pulse * 0.035f);
            }

            if (sceneCamera != null)
            {
                sceneCamera.fieldOfView = cameraBaseFieldOfView - pulse * 0.9f;
                sceneCamera.transform.position = cameraBasePosition + new Vector3(
                    Mathf.Sin(Time.unscaledTime * 0.16f) * 0.05f,
                    Mathf.Sin(Time.unscaledTime * 0.21f) * 0.025f,
                    0f);
            }

            var intensity = Mathf.InverseLerp(55f, 180f, displayedHeartRate);
            var energyColor = Color.Lerp(PulseRed, PulseMagenta, intensity);
            SetMaterialColor(coreMaterial, energyColor * 0.8f, energyColor * (4f + pulse * 10f));
            SetMaterialColor(energyMaterial, new Color(energyColor.r, energyColor.g, energyColor.b, 0.34f), energyColor * (3f + pulse * 8f));
            SetMaterialColor(glassMaterial, new Color(0.5f, 0.16f, 0.42f, 0.12f + pulse * 0.025f), energyColor * 0.6f);
            SetMaterialColor(waveMaterial, energyColor, energyColor * (5f + pulse * 8f));
            SetFloatIfPresent(glassMaterial, "_Pulse", pulse);
            SetFloatIfPresent(energyMaterial, "_Pulse", pulse);
        }

        private void AnimateWave(float pulse)
        {
            if (heartWave == null)
            {
                return;
            }

            const int pointCount = 128;
            if (heartWave.positionCount != pointCount)
            {
                heartWave.positionCount = pointCount;
            }

            var speed = Mathf.Lerp(1.25f, 2.2f, Mathf.InverseLerp(60f, 170f, displayedHeartRate));
            var travel = Time.unscaledTime * speed;
            for (var index = 0; index < pointCount; index++)
            {
                var normalized = index / (float)(pointCount - 1);
                var x = Mathf.Lerp(-2.05f, 2.05f, normalized);
                var envelope = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(x) / 2.1f), 0.45f);
                var wave = Mathf.Sin(normalized * Mathf.PI * 6f - travel * Mathf.PI * 2f) * 0.1f;
                var heartbeat = Mathf.Exp(-Mathf.Pow((normalized - 0.5f) * 18f, 2f)) *
                                Mathf.Sin((normalized - 0.5f) * 58f) * 0.28f;
                var y = (wave + heartbeat * (0.7f + pulse * 1.3f)) * envelope;
                var z = -Mathf.Sqrt(Mathf.Max(0f, 2.12f * 2.12f - x * x)) - 0.06f;
                heartWave.SetPosition(index, new Vector3(x, y, z));
            }

            heartWave.widthMultiplier = 0.024f + pulse * 0.01f;
        }

        private void UpdateLighting(float pulse)
        {
            var intensity = Mathf.InverseLerp(55f, 180f, displayedHeartRate);
            var energyColor = Color.Lerp(PulseRed, PulseMagenta, intensity);

            if (coreLight != null)
            {
                coreLight.color = energyColor;
                coreLight.intensity = 7f + intensity * 3f + pulse * 20f;
                coreLight.range = 8f + pulse * 1.5f;
            }

            if (rimLight != null)
            {
                rimLight.intensity = 3.4f + pulse * 4f;
            }

            if (roomLight != null)
            {
                roomLight.intensity = 1.4f + pulse * 1.1f;
            }

            if (sceneCamera != null)
            {
                sceneCamera.backgroundColor = Color.Lerp(DeepNavy, new Color(0.035f, 0.008f, 0.025f), intensity * 0.35f);
            }
        }

        private void HandleHeartRateChanged(int heartRate)
        {
            displayedHeartRate = Mathf.Max(1f, heartRate);
        }

        private void CreateVisualResources()
        {
            glassMaterial = CreatePulseMaterial(new Color(0.52f, 0.12f, 0.55f, 0.1f), PulseMagenta * 1.1f, 0.08f, 6f);
            energyMaterial = CreatePulseMaterial(new Color(1f, 0.015f, 0.16f, 0.24f), PulseRed * 2.4f, 0.2f, 11f);
            coreMaterial = CreateLitMaterial(new Color(0.72f, 0.01f, 0.08f), PulseRed * 4f, 0.1f, 0.94f);
            roomMaterial = CreateLitMaterial(new Color(0.018f, 0.024f, 0.045f), new Color(0.002f, 0.003f, 0.008f), 0.72f, 0.78f);
            stripMaterial = CreateLitMaterial(new Color(0.09f, 0.06f, 0.2f), VioletBlue * 1.35f, 0.25f, 0.84f);
            waveMaterial = CreateLitMaterial(PulseRed, PulseRed * 7f, 0f, 0.4f);
            floorPulseMaterial = CreateGlassMaterial(new Color(1f, 0.02f, 0.25f, 0.11f));
            particleMaterial = CreateParticleMaterial(PulseMagenta);

            AssignMaterial(glassRenderer, glassMaterial);
            AssignMaterial(energyRenderer, energyMaterial);
            AssignMaterial(coreRenderer, coreMaterial);
            AssignMaterial(roomRenderers, roomMaterial);
            AssignMaterial(lightStripRenderers, stripMaterial);
            AssignMaterial(floorPulseRenderers, floorPulseMaterial);
            AssignMaterial(particleRenderer, particleMaterial);

            if (heartWave != null)
            {
                heartWave.sharedMaterial = waveMaterial;
            }
        }

        private Material CreateLitMaterial(Color baseColor, Color emission, float metallic, float smoothness)
        {
            var material = new Material(FindLitShader()) { hideFlags = HideFlags.HideAndDontSave };
            runtimeResources.Add(material);
            SetMaterialColor(material, baseColor, emission);
            SetFloatIfPresent(material, "_Metallic", metallic);
            SetFloatIfPresent(material, "_Smoothness", smoothness);
            SetFloatIfPresent(material, "_Glossiness", smoothness);
            return material;
        }

        private Material CreateGlassMaterial(Color color)
        {
            var material = CreateLitMaterial(color, new Color(color.r, color.g, color.b) * 0.8f, 0.12f, 1f);
            material.renderQueue = (int)RenderQueue.Transparent;
            SetFloatIfPresent(material, "_Surface", 1f);
            SetFloatIfPresent(material, "_SurfaceType", 1f);
            SetFloatIfPresent(material, "_Blend", 0f);
            SetFloatIfPresent(material, "_ZWrite", 0f);

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }

            return material;
        }

        private Material CreatePulseMaterial(Color color, Color emission, float alpha, float density)
        {
            var shader = Shader.Find("HypeRate/Pulse Sphere");
            if (GraphicsSettings.currentRenderPipeline != null || shader == null || !shader.isSupported)
            {
                return CreateGlassMaterial(color);
            }

            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            runtimeResources.Add(material);
            material.SetColor("_Color", color);
            material.SetColor("_EmissionColor", emission);
            material.SetFloat("_Alpha", alpha);
            material.SetFloat("_Density", density);
            return material;
        }

        private Material CreateParticleMaterial(Color color)
        {
            var shader = Shader.Find("Particles/Standard Unlit") ??
                         Shader.Find("Mobile/Particles/Additive") ??
                         Shader.Find("Sprites/Default");
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            runtimeResources.Add(material);
            SetMaterialColor(material, color, color * 4f);
            return material;
        }

        private static Shader FindLitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit") ??
                   Shader.Find("HDRP/Lit") ??
                   Shader.Find("Standard") ??
                   Shader.Find("Sprites/Default");
        }

        private static void SetMaterialColor(Material material, Color baseColor, Color emission)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emission);
                material.EnableKeyword("_EMISSION");
            }

            if (material.HasProperty("_EmissiveColor"))
            {
                material.SetColor("_EmissiveColor", emission);
            }
        }

        private static void SetFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void AssignMaterial(Renderer targetRenderer, Material material)
        {
            if (targetRenderer != null)
            {
                targetRenderer.sharedMaterial = material;
            }
        }

        private static void AssignMaterial(Renderer[] renderers, Material material)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (var targetRenderer in renderers)
            {
                AssignMaterial(targetRenderer, material);
            }
        }

        private void EnsureGuiStyles()
        {
            if (panelTexture != null)
            {
                return;
            }

            panelTexture = CreateRoundedTexture(64, new Color(0.018f, 0.021f, 0.042f, 0.94f), 13f);
            buttonTexture = CreateRoundedTexture(32, new Color(0.055f, 0.063f, 0.11f, 0.96f), 8f);
            activeButtonTexture = CreateRoundedTexture(32, new Color(0.46f, 0.018f, 0.13f, 0.98f), 8f);
            dividerTexture = CreateRoundedTexture(4, new Color(0.25f, 0.3f, 0.44f, 0.32f), 1f);
            runtimeResources.Add(panelTexture);
            runtimeResources.Add(buttonTexture);
            runtimeResources.Add(activeButtonTexture);
            runtimeResources.Add(dividerTexture);

            eyebrowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.63f, 0.68f, 0.82f) }
            };
            bpmStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 78,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            unitStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.58f, 0.62f, 0.74f) }
            };
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.7f, 0.73f, 0.81f) }
            };
            buttonStyle = CreateButtonStyle(buttonTexture, Color.white);
            activeButtonStyle = CreateButtonStyle(activeButtonTexture, Color.white);
        }

        private static GUIStyle CreateButtonStyle(Texture2D background, Color textColor)
        {
            return new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = background, textColor = textColor },
                hover = { background = background, textColor = Color.white },
                active = { background = background, textColor = Color.white },
                border = new RectOffset(10, 10, 10, 10)
            };
        }

        private static Texture2D CreateRoundedTexture(int size, Color color, float radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            var corner = new Vector2(radius, radius);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var mirrored = new Vector2(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                    var distance = Vector2.Distance(mirrored, corner);
                    var alpha = mirrored.x >= radius || mirrored.y >= radius
                        ? 1f
                        : Mathf.Clamp01(radius + 1f - distance);
                    var pixel = color;
                    pixel.a *= alpha;
                    pixels[y * size + x] = pixel;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
