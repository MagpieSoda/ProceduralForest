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
    //generate the forest by sampling the terrain and placing trees based on Fractional Brownian Motion noise
    void GenerateForest() 
    { 
        if (terrain == null || treePrefab == null) 
        {
            Debug.LogError("Assign a Terrain and Tree Prefab first!");
            return;
        }

        //creates a random number generator with a seed so the same forest will be generated with the same seed
        System.Random random = new System.Random(seed);

        for(int i = 0;i < candidateCount; i++) 
        {
            //generate a random position on the terrain by sampling the terrain
            float x = (float)random.NextDouble() * terrain.terrainData.size.x;
            float z = (float)random.NextDouble() * terrain.terrainData.size.z;

            Vector3 position = terrain.transform.position + new Vector3(x, 0f, z);
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y;//find the height of the terrain at the position to place the tree on the ground

            //sample fbm noise 
            float noise = CalculateFBM(position.x * noiseScale, position.z * noiseScale);
            if (noise < densityThreshold)// if noise is below the threshold, skip that tree
                continue;

            float rotation = (float)random.NextDouble() * 360f;// random rotation for the tree to make it look more natural
            float scale = Mathf.Lerp(minTreeScale, maxTreeScale, (float)random.NextDouble());// random scale for the tree to make it look more natural

            //instantiate the tree prefab at the position with the random rotation and scale, and make it a child of the forest generator object
            GameObject tree = Instantiate(treePrefab, position, Quaternion.Euler(0f, rotation, 0f), transform);
            tree.transform.localScale = tree.transform.localScale * scale;
        }
    }
    //calculate the Fractional Brownian Motion noise value at a given position using Perlin noise
    float CalculateFBM(float x, float z)
    {   //initialize total, frequency, amplitude, and maxValue 
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f; 

        for (int i = 0; i < octaves; i++)
        {  //sampiling perlin noise gets a smooth value and multiplyes it by x and z
            total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
            maxValue += amplitude; //add current layer ampliitude to max volume
            amplitude *= persistence; //shrinks the amplitude to get rid of small values
            frequency *= lacunarity; 
        }

        return total / maxValue; //deviding by max value to get a value between 0 and 1
    }

}
