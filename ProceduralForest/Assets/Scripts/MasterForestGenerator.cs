using UnityEngine;

public class MasterForestGenerator : MonoBehaviour
{
    public enum NoiseType
    {
        StandardPerlin,
        FBM,
        Cellular,
        RidgedMultifractal
    }

    [Header("Forest Setup")]
    public Terrain terrain;
    public GameObject treePrefab;

    [Header("Noise Selection")]
    public NoiseType noiseType = NoiseType.FBM;

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

    [ContextMenu("Regenerate Forest")]
    void GenerateForest()
    {
        if (terrain == null || treePrefab == null)
        {
            Debug.LogError("Assign a Terrain and Tree Prefab first!");
            return;
        }

        // Clear out old trees if regenerating in the editor
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        System.Random random = new System.Random(seed);

        for (int i = 0; i < candidateCount; i++)
        {
            float x = (float)random.NextDouble() * terrain.terrainData.size.x;
            float z = (float)random.NextDouble() * terrain.terrainData.size.z;

            Vector3 position = terrain.transform.position + new Vector3(x, 0f, z);
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;

            
            //shared rules 
            

            // Sample the selected noise type
            float noiseValue = SampleSelectedNoise(position.x * noiseScale, position.z * noiseScale);

            // Check threshold (Cellular uses inverse logic, others use standard)
            bool shouldSkip = (noiseType == NoiseType.Cellular)
                ? (noiseValue > densityThreshold)
                : (noiseValue < densityThreshold);

            if (shouldSkip)
                continue;

            float rotation = (float)random.NextDouble() * 360f;
            float scale = Mathf.Lerp(minTreeScale, maxTreeScale, (float)random.NextDouble());

            GameObject tree = Instantiate(treePrefab, position, Quaternion.Euler(0f, rotation, 0f), transform);
            tree.transform.localScale = tree.transform.localScale * scale;
        }
    }

    // coordinate to the correct noise calculation based on choice
    float SampleSelectedNoise(float x, float z)
    {
        switch (noiseType)
        {
            case NoiseType.StandardPerlin:
                return Mathf.PerlinNoise(x, z);

            case NoiseType.FBM:
                return CalculateFBM(x, z);

            case NoiseType.Cellular:
                return CalculateCellular(x, z);

            case NoiseType.RidgedMultifractal:
                return CalculateRidgedNoise(x, z);

            default:
                return Mathf.PerlinNoise(x, z);
        }
    }

    //NOISE ALGORITHMS

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

    float CalculateCellular(float x, float z)
    {
        int intX = Mathf.FloorToInt(x);
        int intZ = Mathf.FloorToInt(z);
        float minDist = 1.0f;

        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
            {
                int neighborX = intX + offsetX;
                int neighborZ = intZ + offsetZ;

                Vector2 cellPoint = new Vector2(neighborX, neighborZ) + GetRandomVector(neighborX, neighborZ);
                float dist = Vector2.Distance(new Vector2(x, z), cellPoint);

                if (dist < minDist)
                {
                    minDist = dist;
                }
            }
        }
        return Mathf.Clamp01(minDist);
    }

    Vector2 GetRandomVector(int x, int z)
    {
        int s = seed + x * 1619 + z * 31337;
        System.Random prng = new System.Random(s);
        return new Vector2((float)prng.NextDouble(), (float)prng.NextDouble());
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