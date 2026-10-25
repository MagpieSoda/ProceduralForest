using UnityEngine;

public class CellularForestGenerator : MonoBehaviour
{
    [Header("Forest Setup")]
    public Terrain terrain;
    public GameObject treePrefab;

    [Header("Tree Placement")]
    [Min(1)]
    public int candidateCount = 1500;
    [Range(0.01f, 0.5f)]
    public float cellScale = 0.05f;
    [Range(0f, 1f)]
    public float densityThreshold = 0.35f;

    [Header("Randomness")]
    public int seed = 42;

    [Header("Tree Variation")]
    [Range(0.5f, 1.5f)]
    public float minTreeScale = 0.8f;
    [Range(0.5f, 2f)]
    public float maxTreeScale = 1.2f;

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

            float noiseValue = GetCellularNoise(position.x * cellScale, position.z * cellScale);

            if (noiseValue > densityThreshold)
                continue;

            float rotation = (float)random.NextDouble() * 360f;
            float scale = Mathf.Lerp(minTreeScale, maxTreeScale, (float)random.NextDouble());

            GameObject tree = Instantiate(treePrefab, position, Quaternion.Euler(0f, rotation, 0f), transform);
            tree.transform.localScale = tree.transform.localScale * scale;
        }
    }

    //  procedural Cellular noise implementation
    float GetCellularNoise(float x, float z)
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
}