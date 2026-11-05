using TMPro;
using HypeRate.SDK;
using UnityEngine;

public class Example : MonoBehaviour
{
    private TMP_Text textBox;
    private HypeRateClient client;

    private void Start()
    {
        textBox = GetComponent<TMP_Text>();
        client = FindClient();

        if (client == null)
        {
            textBox.text = "Open Tools > HypeRate > Setup";
            return;
        }

        client.HeartRateChanged += ChangeText;
        textBox.text = "Connecting to HypeRate…";
    }

    private void OnDestroy()
    {
        if (client != null)
        {
            client.HeartRateChanged -= ChangeText;
        }
    }

    private void ChangeText(int heartRate)
    {
        textBox.text = $"{heartRate} BPM";
    }

    private static HypeRateClient FindClient()
    {
#if UNITY_2023_1_OR_NEWER
        return FindAnyObjectByType<HypeRateClient>();
#else
        return FindObjectOfType<HypeRateClient>();
#endif
    }
}
