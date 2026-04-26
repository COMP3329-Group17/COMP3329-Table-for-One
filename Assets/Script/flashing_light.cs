using UnityEngine;

public class LanternFlicker : MonoBehaviour
{
    public Light lanternLight;
    public float baseIntensity = 2f;
    public float flickerAmount = 0.5f;
    public float flickerSpeed = 8f;

    void Update()
    {
        if (lanternLight == null) return;

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        lanternLight.intensity = baseIntensity + (noise - 0.5f) * flickerAmount;
    }
}