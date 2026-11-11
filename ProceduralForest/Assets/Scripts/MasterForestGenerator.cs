using UnityEngine;

public class MasterForestGenerator : MonoBehaviour
{
    // Enum to select the noise type for tree placement
    public enum NoiseType
    {
        StandardPerlin,
        FBM,
        Cellular,
        RidgedMultifractal
    }
    //forest setup section in the inspector
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
        // Check if terrain and tree prefab are assigned
        if (terrain == null || treePrefab == null)
        {
            Debug.LogError("Assign a Terrain and Tree Prefab first!");
            return;
        }

        // Clear out old trees 
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        System.Random random = new System.Random(seed);

        // Loop through candidate positions and place trees based on the selected noise type
        for (int i = 0; i < candidateCount; i++)
        {
            float x = (float)random.NextDouble() * terrain.terrainData.size.x;
            float z = (float)random.NextDouble() * terrain.terrainData.size.z;

            // Calculate the world position based on terrain position and sample height
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

            float rotation = (float)random.NextDouble() * 360f;// random rotation for the tree to make it look more natural
            float scale = Mathf.Lerp(minTreeScale, maxTreeScale, (float)random.NextDouble());// random scale for the tree to make it look more natural

            // Instantiate the tree prefab at the position with the random rotation and scale, and make it a child of the forest generator object
            GameObject tree = Instantiate(treePrefab, position, Quaternion.Euler(0f, rotation, 0f), transform);
            tree.transform.localScale = tree.transform.localScale * scale;
        }
    }

    // coordinate to the correct noise calculation based on choice
    float SampleSelectedNoise(float x, float z)
    {
        switch (noiseType)
        { //use the selected noise type to calculate the noise value at the given position
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

    //calculate the Cellular noise value at a given position using a simple distance-based approach
    float CalculateCellular(float x, float z)
    { //determans which integer grid cell value for any given x,z coordinate
        int intX = Mathf.FloorToInt(x);
        int intZ = Mathf.FloorToInt(z);
        float minDist = 1.0f;

        //check the 3x3 grid of neighboring cells to find the closest cell point
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
            {
                //get the neighboring cell coordinates
                int neighborX = intX + offsetX;
                int neighborZ = intZ + offsetZ;

                //for each nebor it calls get random vector to get a uniqe random point inside that cell.
                Vector2 cellPoint = new Vector2(neighborX, neighborZ) + GetRandomVector(neighborX, neighborZ);
                float dist = Vector2.Distance(new Vector2(x, z), cellPoint);//calculate the distance from the current position to the cell point

                //if the distance is less than the current minimum distance, update the minimum distance
                if (dist < minDist)
                {
                    minDist = dist;
                }
            }
        }
        //return the clamped minimum distance
        return Mathf.Clamp01(minDist);
    }

    //ensures that every grid cell gets a consistant , repetable random point regardless of how many times or in what order the cells are checked.
    Vector2 GetRandomVector(int x, int z)
    {
        //takes global seed and combines it with the cells coordinates multiplied by large prime numbers to create a unique seed for each cell
        int s = seed + x * 1619 + z * 31337;

        //creates local random number generator with the unique seed for the cell and returns a random vector within the cell
        System.Random prng = new System.Random(s);
        return new Vector2((float)prng.NextDouble(), (float)prng.NextDouble());
    }

    //calculate the Ridged Multifractal noise value at a given position using Perlin noise
    float CalculateRidgedNoise(float x, float z)
    {
        float total = 0f;//accumulates the final noise value
        float frequency = 1f;//controls the scale of the noise, higher frequency means more detail
        float amplitude = 1f;
        float weight = 1f;//influences the contribution of each octave to the final noise value, higher weight means more influence

        //iterates through the number of octaves to generate the ridged multifractal noise
        for (int i = 0; i < octaves; i++)
        {
            float signal = Mathf.PerlinNoise(x * frequency, z * frequency);//samples standard 2d Perlin noise at the current coordinate scailed by frequency
            signal = 1.0f - Mathf.Abs(signal);//takes the absolute value and subtracting from 1 to create ridges in the noise pattern

            //squaring the signal to increase the contrast between ridges and valleys, making the ridges more pronounced
            signal *= signal;
            signal *= weight;

            //it multiplies the signl by the current wheight , then updates the weight for the next octave based on the current signal value.
            weight = Mathf.Clamp01(signal * 2f);
            total += signal * amplitude;

            //doubles the frequency for the next octave, introducing finer details.
            frequency *= 2.0f;
            amplitude *= 0.5f;
        }
        //clamps the final noise value between 0 and 1 to ensure it stays within a usable range for tree placement decisions.
        return Mathf.Clamp01(total);
    }
}