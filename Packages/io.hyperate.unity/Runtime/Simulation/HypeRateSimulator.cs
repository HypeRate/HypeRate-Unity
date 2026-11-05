using UnityEngine;

namespace HypeRate.SDK
{
    [AddComponentMenu("HypeRate/HypeRate Simulator")]
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class HypeRateSimulator : MonoBehaviour
    {
        [SerializeField] private HypeRateClient client;
        [SerializeField] private HypeRateSimulationPreset preset = HypeRateSimulationPreset.Workout;
        [SerializeField, Range(30, 240)] private int customHeartRate = 100;
        [SerializeField, Min(0.1f)] private float updateInterval = 0.25f;
        [SerializeField] private bool playOnStart = true;

        private bool running;
        private float nextUpdateTime;
        private float smoothedHeartRate;
        private float noiseOffset;

        public HypeRateSimulationPreset Preset => preset;
        public bool IsRunning => running;
        public int CurrentHeartRate => Mathf.RoundToInt(smoothedHeartRate);

        private void Awake()
        {
            ResolveClient();
            noiseOffset = 3.17f + transform.GetSiblingIndex() * 0.137f;
        }

        private void Start()
        {
            if (playOnStart)
            {
                StartSimulation();
            }
        }

        private void Update()
        {
            if (!running || client == null || Time.unscaledTime < nextUpdateTime)
            {
                return;
            }

            nextUpdateTime = Time.unscaledTime + updateInterval;
            var target = CalculateTargetHeartRate(Time.unscaledTime);
            smoothedHeartRate = Mathf.MoveTowards(smoothedHeartRate, target, 5f);
            client.SetSimulatedHeartRate(Mathf.Clamp(Mathf.RoundToInt(smoothedHeartRate), 30, 240));
        }

        private void OnDisable()
        {
            if (running)
            {
                StopSimulation();
            }
        }

        public void StartSimulation()
        {
            if (!ResolveClient())
            {
                Debug.LogError("[HypeRate] The simulator needs a HypeRate Client.", this);
                return;
            }

            var profile = GetProfile();
            smoothedHeartRate = profile.center;
            running = true;
            nextUpdateTime = 0f;
            client.BeginSimulation(Mathf.RoundToInt(smoothedHeartRate));
        }

        public void StopSimulation()
        {
            running = false;
            if (client != null)
            {
                client.EndSimulation();
            }
        }

        public void SetPreset(HypeRateSimulationPreset value)
        {
            preset = value;
            var profile = GetProfile();
            smoothedHeartRate = profile.center;

            if (running && client != null)
            {
                client.SetSimulatedHeartRate(Mathf.RoundToInt(smoothedHeartRate));
            }
        }

        public void SetCustomHeartRate(int heartRate)
        {
            customHeartRate = Mathf.Clamp(heartRate, 30, 240);
            SetPreset(HypeRateSimulationPreset.Custom);
        }

        private float CalculateTargetHeartRate(float time)
        {
            var profile = GetProfile();
            var slowWave = Mathf.Sin((time + noiseOffset) * 0.62f) * profile.variation * 0.35f;
            var noise = (Mathf.PerlinNoise(noiseOffset, time * 0.11f) - 0.5f) * 2f * profile.variation;
            return Mathf.Clamp(profile.center + slowWave + noise, 30f, 240f);
        }

        private (float center, float variation) GetProfile()
        {
            switch (preset)
            {
                case HypeRateSimulationPreset.Resting:
                    return (64f, 4f);
                case HypeRateSimulationPreset.Intense:
                    return (168f, 12f);
                case HypeRateSimulationPreset.Custom:
                    return (customHeartRate, 2f);
                default:
                    return (126f, 9f);
            }
        }

        private bool ResolveClient()
        {
            if (client == null)
            {
                client = GetComponent<HypeRateClient>();
            }

            return client != null;
        }
    }
}
