using System.Collections.Generic;
using UnityEngine;

namespace HypeRate.SDK
{
    [AddComponentMenu("HypeRate/Heart Rate Widget")]
    [DisallowMultipleComponent]
    public sealed class HypeRateHeartRateWidget : MonoBehaviour
    {
        private const float CardWidth = 720f;
        private const float CardHeight = 380f;
        private const int GraphWidth = 512;
        private const int GraphHeight = 132;
        private const int MaximumSamples = 20;

        private static readonly Color CardColor = new Color(0.035f, 0.038f, 0.048f, 0.98f);
        private static readonly Color MutedText = new Color(0.64f, 0.65f, 0.72f);
        private static readonly Color GraphBlue = new Color(0.06f, 0.45f, 0.96f);
        private static readonly Color HeartRed = new Color(1f, 0.08f, 0.12f);
        private static readonly Color StatusRed = new Color(1f, 0.22f, 0.24f);
        private static readonly Color StatusYellow = new Color(1f, 0.72f, 0.16f);
        private static readonly Color StatusGreen = new Color(0.16f, 0.86f, 0.43f);
        private static readonly Color StatusBlue = new Color(0.21f, 0.62f, 1f);

        [SerializeField] private HypeRateClient client;
        [SerializeField, Range(0.5f, 1.5f)] private float displayScale = 1f;
        [SerializeField] private Vector2 screenOffset;
        [SerializeField] private bool visible = true;

        private readonly List<int> samples = new List<int>(MaximumSamples);
        private Texture2D cardTexture;
        private Texture2D shadowTexture;
        private Texture2D graphTexture;
        private Texture2D whiteTexture;
        private GUIStyle valueStyle;
        private GUIStyle unitStyle;
        private GUIStyle rangeStyle;
        private GUIStyle statusDotStyle;
        private GUIStyle statusTextStyle;
        private int currentHeartRate;
        private bool graphDirty = true;

        public HypeRateClient Client
        {
            get => client;
            set
            {
                if (client == value)
                {
                    return;
                }

                Unsubscribe();
                client = value;
                Subscribe();
            }
        }

        public bool Visible
        {
            get => visible;
            set => visible = value;
        }

        public int CurrentHeartRate => currentHeartRate;

        private void Awake()
        {
            ResolveClient();
        }

        private void OnEnable()
        {
            ResolveClient();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            DestroyTexture(cardTexture);
            DestroyTexture(shadowTexture);
            DestroyTexture(graphTexture);
            DestroyTexture(whiteTexture);
        }

        private void OnGUI()
        {
            if (!visible || Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureVisualResources();
            UpdateGraphTexture();

            var fit = Mathf.Min(Screen.width / (CardWidth + 80f), Screen.height / (CardHeight + 80f));
            var scale = Mathf.Clamp(fit * displayScale, 0.5f, 1.35f);
            var cardX = (Screen.width / scale - CardWidth) * 0.5f + screenOffset.x;
            var cardY = (Screen.height / scale - CardHeight) * 0.5f + screenOffset.y;
            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.DrawTexture(new Rect(cardX - 18f, cardY + 10f, CardWidth + 36f, CardHeight + 34f), shadowTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cardX, cardY, CardWidth, CardHeight), cardTexture);

            var value = currentHeartRate > 0 ? currentHeartRate : 0;
            GUI.Label(new Rect(cardX + 62f, cardY + 30f, 285f, 135f), value > 0 ? value.ToString() : "—", valueStyle);
            GUI.Label(new Rect(cardX + 324f, cardY + 104f, 90f, 40f), "BPM", unitStyle);
            DrawPixelHeart(new Vector2(cardX + 610f, cardY + 70f), value > 0);
            DrawConnectionStatus(cardX + 552f, cardY + 148f);

            var graphRect = new Rect(cardX + 55f, cardY + 187f, GraphWidth, GraphHeight);
            GUI.DrawTexture(graphRect, graphTexture);

            var minimum = samples.Count > 0 ? Mathf.Min(samples.ToArray()) : 0;
            var maximum = samples.Count > 0 ? Mathf.Max(samples.ToArray()) : 0;
            GUI.Label(new Rect(cardX + 590f, cardY + 185f, 82f, 32f), maximum > 0 ? maximum.ToString() : "—", rangeStyle);
            GUI.Label(new Rect(cardX + 590f, cardY + 300f, 82f, 32f), minimum > 0 ? minimum.ToString() : "—", rangeStyle);

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        public void PushHeartRate(int heartRate)
        {
            if (heartRate <= 0)
            {
                return;
            }

            currentHeartRate = heartRate;
            if (samples.Count == 0)
            {
                SeedHistory(heartRate, client != null && client.IsSimulating);
            }
            else
            {
                samples.Add(heartRate);
                if (samples.Count > MaximumSamples)
                {
                    samples.RemoveAt(0);
                }
            }

            graphDirty = true;
        }

        private void HandleHeartRateChanged(int heartRate)
        {
            PushHeartRate(heartRate);
        }

        private void SeedHistory(int heartRate, bool simulated)
        {
            samples.Clear();
            for (var index = 0; index < MaximumSamples; index++)
            {
                var offset = simulated
                    ? Mathf.Sin(index * 0.9f) * 8f + Mathf.Sin(index * 0.37f + 1.4f) * 4f
                    : 0f;
                samples.Add(Mathf.Max(1, heartRate + Mathf.RoundToInt(offset)));
            }
        }

        private void DrawPixelHeart(Vector2 origin, bool active)
        {
            var pattern = new[]
            {
                "0110110",
                "1111111",
                "1111111",
                "0111110",
                "0011100",
                "0001000"
            };
            const float pixel = 10f;
            var color = active ? HeartRed : new Color(0.35f, 0.15f, 0.17f);
            GUI.color = color;
            for (var row = 0; row < pattern.Length; row++)
            {
                for (var column = 0; column < pattern[row].Length; column++)
                {
                    if (pattern[row][column] == '1')
                    {
                        GUI.DrawTexture(
                            new Rect(origin.x + column * pixel, origin.y + row * pixel, pixel - 2f, pixel - 2f),
                            whiteTexture);
                    }
                }
            }

            GUI.color = Color.white;
        }

        private void DrawConnectionStatus(float x, float y)
        {
            GetConnectionStatus(out var text, out var color);

            GUI.color = color;
            GUI.Label(new Rect(x, y - 3f, 24f, 28f), "●", statusDotStyle);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 24f, y, 122f, 26f), text, statusTextStyle);
        }

        private void GetConnectionStatus(out string text, out Color color)
        {
            var state = client != null ? client.State : HypeRateConnectionState.Disconnected;
            switch (state)
            {
                case HypeRateConnectionState.Connected:
                    text = "LIVE";
                    color = StatusGreen;
                    return;
                case HypeRateConnectionState.Connecting:
                    text = "CONNECTING";
                    color = StatusYellow;
                    return;
                case HypeRateConnectionState.Reconnecting:
                    text = "RECONNECTING";
                    color = StatusYellow;
                    return;
                case HypeRateConnectionState.Disconnecting:
                    text = "DISCONNECTING";
                    color = StatusYellow;
                    return;
                case HypeRateConnectionState.Simulating:
                    text = "SIMULATION";
                    color = StatusBlue;
                    return;
                case HypeRateConnectionState.Error:
                    text = "ERROR";
                    color = StatusRed;
                    return;
                default:
                    text = "OFFLINE";
                    color = StatusRed;
                    return;
            }
        }

        private void EnsureVisualResources()
        {
            if (cardTexture != null)
            {
                return;
            }

            cardTexture = CreateRoundedTexture(
                (int)CardWidth,
                (int)CardHeight,
                CardColor,
                new Color(0.16f, 0.17f, 0.21f, 0.9f),
                28f,
                2f);
            shadowTexture = CreateRoundedTexture(
                (int)CardWidth + 36,
                (int)CardHeight + 34,
                new Color(0f, 0f, 0f, 0.7f),
                Color.clear,
                34f,
                0f);
            graphTexture = new Texture2D(GraphWidth, GraphHeight, TextureFormat.RGBA32, false)
            {
                name = "HypeRate Widget Graph",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "HypeRate Widget Pixel",
                hideFlags = HideFlags.HideAndDontSave
            };
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();

            valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 112,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            unitStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = MutedText }
            };
            rangeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 21,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = MutedText }
            };
            statusDotStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            statusTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = MutedText }
            };
        }

        private void UpdateGraphTexture()
        {
            if (!graphDirty || graphTexture == null)
            {
                return;
            }

            graphDirty = false;
            var pixels = new Color32[GraphWidth * GraphHeight];
            if (samples.Count < 2)
            {
                graphTexture.SetPixels32(pixels);
                graphTexture.Apply(false, false);
                return;
            }

            var minimum = Mathf.Min(samples.ToArray());
            var maximum = Mathf.Max(samples.ToArray());
            var padding = Mathf.Max(6f, (maximum - minimum) * 0.25f);
            var low = minimum - padding;
            var high = maximum + padding;

            for (var x = 0; x < GraphWidth; x++)
            {
                var samplePosition = x / (float)(GraphWidth - 1) * (samples.Count - 1);
                var left = Mathf.FloorToInt(samplePosition);
                var right = Mathf.Min(left + 1, samples.Count - 1);
                var value = Mathf.Lerp(samples[left], samples[right], samplePosition - left);
                var lineY = Mathf.Lerp(10f, GraphHeight - 10f, Mathf.InverseLerp(low, high, value));

                for (var y = 0; y < GraphHeight; y++)
                {
                    var index = y * GraphWidth + x;
                    var distance = Mathf.Abs(y - lineY);
                    if (distance <= 2.4f)
                    {
                        var alpha = (byte)Mathf.RoundToInt(Mathf.Lerp(255f, 110f, distance / 2.4f));
                        pixels[index] = new Color32(18, 116, 245, alpha);
                    }
                    else if (y < lineY)
                    {
                        var alpha = (byte)Mathf.RoundToInt(Mathf.InverseLerp(0f, lineY, y) * 52f);
                        pixels[index] = new Color32(10, 79, 172, alpha);
                    }
                }
            }

            var lastValue = samples[samples.Count - 1];
            var lastY = Mathf.RoundToInt(Mathf.Lerp(10f, GraphHeight - 10f, Mathf.InverseLerp(low, high, lastValue)));
            DrawDot(pixels, GraphWidth - 10, lastY, 11, new Color32(30, 127, 255, 255));
            graphTexture.SetPixels32(pixels);
            graphTexture.Apply(false, false);
        }

        private static void DrawDot(Color32[] pixels, int centerX, int centerY, int radius, Color32 color)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    var distance = Mathf.Sqrt(x * x + y * y);
                    if (distance > radius)
                    {
                        continue;
                    }

                    var targetX = centerX + x;
                    var targetY = centerY + y;
                    if (targetX < 0 || targetX >= GraphWidth || targetY < 0 || targetY >= GraphHeight)
                    {
                        continue;
                    }

                    var alpha = distance <= radius * 0.55f
                        ? 255
                        : Mathf.RoundToInt(Mathf.Lerp(255f, 0f, Mathf.InverseLerp(radius * 0.55f, radius, distance)));
                    pixels[targetY * GraphWidth + targetX] = new Color32(color.r, color.g, color.b, (byte)alpha);
                }
            }
        }

        private static Texture2D CreateRoundedTexture(
            int width,
            int height,
            Color fill,
            Color border,
            float radius,
            float borderWidth)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[width * height];
            var cornerCenter = new Vector2(radius, radius);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var mirrored = new Vector2(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    var isCorner = mirrored.x < radius && mirrored.y < radius;
                    var distance = isCorner ? Vector2.Distance(mirrored, cornerCenter) : 0f;
                    var outside = isCorner && distance > radius;
                    var edge = borderWidth > 0f && !outside &&
                               (isCorner
                                   ? distance > radius - borderWidth
                                   : mirrored.x < borderWidth || mirrored.y < borderWidth);
                    pixels[y * width + x] = outside ? Color.clear : edge ? border : fill;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private void ResolveClient()
        {
            if (client == null)
            {
                client = GetComponent<HypeRateClient>();
            }
        }

        private void Subscribe()
        {
            if (client != null)
            {
                client.HeartRateChanged -= HandleHeartRateChanged;
                client.HeartRateChanged += HandleHeartRateChanged;
                if (client.CurrentHeartRate > 0)
                {
                    PushHeartRate(client.CurrentHeartRate);
                }
            }
        }

        private void Unsubscribe()
        {
            if (client != null)
            {
                client.HeartRateChanged -= HandleHeartRateChanged;
            }
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }
}
