using UnityEngine;

namespace HypeRate.SDK.Samples
{
    [DefaultExecutionOrder(200)]
    public sealed class HypeRateWidgetDemo : MonoBehaviour
    {
        [SerializeField] private HypeRateClient client;
        [SerializeField] private HypeRateSimulator simulator;

        private void Start()
        {
            if (client == null)
            {
                return;
            }

            if (client.HasConfiguration)
            {
                simulator?.StopSimulation();
                client.Connect();
            }
            else
            {
                simulator?.StartSimulation();
            }
        }
    }
}
