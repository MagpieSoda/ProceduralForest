using UnityEngine;

public class FBMForestGenerator :MonoBehaviour 
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
    public float densityThreshold = 0.45f;

    [Header("FBM Noise Settings")]
    [Range(1, 6)]
    public int octaves = 4;
    [Range(0.1f, 1f)]
    public float persistence = 0.5f;
    [Range(1f, 4f)]
    public float lacunarity = 2.0f;

    [Header("Randomness")]
    public int seed = 42;

    [Header("Tree Variation")]
    [Range(0.5f, 1.5f)]
    public float minTreeScale = 0.8f;
    [Range(0.5f, 2f)]
    public float maxTreeScale = 1.2f;

    private void Start()
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
        for(int i = 0;i < candidateCount; i++) 
        {
            float x = (float)random.NextDouble() * terrain.terrainData.size.x;
            float z = (float)random.NextDouble() * terrain.terrainData.size.z;

            Vector3 position = terrain.transform.position + new Vector3(x, 0f, z);
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
            //sample FBM noise 
            float noise = CalculateFBM(position.x * noiseScale, position.z * noiseScale);
            if (noise < densityThreshold)
                continue;

            float rotation = (float)random.NextDouble() * 360f;
            float scale = Mathf.Lerp(minTreeScale, maxTreeScale, (float)random.NextDouble());

            GameObject tree = Instantiate(treePrefab, position, Quaternion.Euler(0f, rotation, 0f), transform);
            tree.transform.localScale = tree.transform.localScale * scale;
        }
    }
    float CalculateFBM(float x, float z)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f; 

        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }

}
