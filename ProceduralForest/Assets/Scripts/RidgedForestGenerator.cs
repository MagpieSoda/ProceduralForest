using UnityEngine;

public class RidgedForestGenerator : MonoBehaviour
{
    [Header("Forest Setup")]
    public Terrain terrain;
    public GameObject treePrefab;

    [Header("Tree Placement")]
    [Min(1)]
    public int candidateCount = 1000;
    [Range(0.001f, 0.2f)]
    public float noiseScale = 0.03f;
    [Range(0f, 1f)]
    public float densityThreshold = 0.6f;

    [Header("Ridged Noise Settings")]
    [Range(1, 5)]
    public int octaves = 3;

    [Header("Randomness")]
    public int seed = 42;

    [Header("Tree Variation")]
    [Range(0.5f, 1.5f)]
    public float minTreeScale = 0.8f;
    [Range(0.5f, 2f)]
    public float maxTreeScale = 1.2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateForest();
    }
    void GenerateForest()
    {
        if (terrain == null || treePrefab == null)
        {
            Debug.LogError("Assign a Terrain and Tree Prefab first!");
            return;
        }

        System.Random random = new System.Random(seed);

        for (int i = 0; i < candidateCount; i++)
        {
            float x = (float)random.NextDouble() * terrain.terrainData.size.x;
            float z = (float)random.NextDouble() * terrain.terrainData.size.z;

            Vector3 position = terrain.transform.position + new Vector3(x, 0f, z);
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;

            // Sample Ridged Noise
            float noise = CalculateRidgedNoise(position.x * noiseScale, position.z * noiseScale);

            if (noise < densityThreshold)
                continue;

            float rotation = (float)random.NextDouble() * 360f;
            float scale = Mathf.Lerp(minTreeScale, maxTreeScale, (float)random.NextDouble());

            GameObject tree = Instantiate(treePrefab, position, Quaternion.Euler(0f, rotation, 0f), transform);
            tree.transform.localScale = tree.transform.localScale * scale;
        }
    }
    float CalculateRidgedNoise(float x, float z)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float weight = 1f;

        for (int i = 0; i < octaves; i++)
        {
            float signal = Mathf.PerlinNoise(x * frequency, z * frequency);

            // Make sharp ridges by inverting absolute Perlin values
            signal = 1.0f - Mathf.Abs(signal);
            signal *= signal; 
            signal *= weight;

            weight = Mathf.Clamp01(signal * 2f); 
            total += signal * amplitude;

            frequency *= 2.0f;
            amplitude *= 0.5f;
        }

        return Mathf.Clamp01(total);
    }

    
}
