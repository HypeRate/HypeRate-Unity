using HypeRate.SDK;
using UnityEngine;

namespace HypeRate.Samples
{
    public sealed class HeartRateLogger : MonoBehaviour
    {
        [SerializeField] private HypeRateClient client;

        private void OnEnable()
        {
            if (client != null)
            {
                client.HeartRateChanged += HandleHeartRateChanged;
            }
        }

        private void OnDisable()
        {
            if (client != null)
            {
                client.HeartRateChanged -= HandleHeartRateChanged;
            }
        }

        private static void HandleHeartRateChanged(int heartRate)
        {
            Debug.Log($"HypeRate: {heartRate} BPM");
        }
    }
}
