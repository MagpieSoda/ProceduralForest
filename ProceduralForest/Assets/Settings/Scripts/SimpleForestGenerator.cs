 
 using UnityEngine;
public class SimpleForestGenerator : MonoBehaviour
{
    [Header("Forest Setup")]
    public Terrain terrain;
    public GameObject treePrefab;

    [Header("Tree Placment")]

    [Min(1)]
    public int candidateCount = 1000;
    [Range(0.001f, 0.2f)]
    public float noiseScale = 0.03f;
    [Range(0f, 1f)]
    public float densityThreshold = 0.45f;

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
        if(terrain == null || treePrefab == null) 
        {
            Debug.LogError("Assign a Terrain and Tree Prefab first!");
            return;
        }
        System.Random random = new System.Random(seed);
        for(int i = 0; i < candidateCount; i++)
        {
            float x = (float)random.NextDouble() * terrain.terrainData.size.x;
            float z = (float)random.NextDouble() * terrain.terrainData.size.z;

            Vector3 position = terrain.transform.position + new Vector3(x, 0f, z);

            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;

            //Sample perlin noise
            float noise = Mathf.PerlinNoise(position.x * noiseScale, position.z * noiseScale);

            //Skip locations with low values
            if (noise < densityThreshold)
                continue;
            float rotation = (float)random.NextDouble() * 360f;
            float scale = Mathf.Lerp(minTreeScale, maxTreeScale, (float)random.NextDouble());
            GameObject tree = Instantiate(treePrefab, position, Quaternion.Euler(0f, rotation, 0f), transform);
            tree.transform.localScale = tree.transform.localScale * scale;

        }
    
    }

}
