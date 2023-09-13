using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainType
{
    public string name;
    public float height;
    public Color color;
}

[RequireComponent (typeof(MeshFilter), typeof(MeshRenderer))]
public class GridScript : MonoBehaviour
{
    [SerializeField] private TerrainType[] terrainTypes;
    [SerializeField] private int xSize;
    [SerializeField] private int ySize;
    [SerializeField] private int maxHeight;
    [SerializeField] AnimationCurve heightCurve;
    [SerializeField] private float perlinScale;

    // Start is called before the first frame update
    void Start()
    {
        Generator();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Generates a 2D float array for a Noise Card
    public float[,] GenerateNoiseMap(int zGrid, int xGrid, float scale)
    {
        float[,] noise = new float[zGrid, xGrid];
        for (int zIndex = 0; zIndex < zGrid; zIndex++)
        {
            for (int xIndex = 0; xIndex < xGrid; xIndex++)
            {
                float sampleX = xIndex / scale;
                float sampleZ = zIndex / scale;

                noise[zIndex, xIndex] = Mathf.PerlinNoise(sampleX, sampleZ);
            }
        }

        return noise;
    }

    private TerrainType ChooseTerrainType(float height)
    {
        foreach(TerrainType terType in terrainTypes)
        {
            if (height < terType.height)
            {
                return terType;
            }
        }
        return terrainTypes[terrainTypes.Length - 1];
    }

    // Converts the Noise into a Texture
    private Texture2D BuildTexture(float[,] heightMap)
    {
        int depth = heightMap.GetLength(0);
        int width = heightMap.GetLength(1);

        Color[] colorMap = new Color[depth * width];
        for (int zIndex = 0; zIndex < depth; zIndex++)
        {
            for (int xIndex = 0; xIndex < width; xIndex++)
            {
                int colorIndex = zIndex * width + xIndex;
                float height = heightMap[zIndex, xIndex];
                TerrainType terrType = ChooseTerrainType(height);
                colorMap[colorIndex] = terrType.color;

                // Black and white Texture
                // colorMap[colorIndex] = Color.Lerp(Color.black, Color.white, height);
            }
        }

        Texture2D tex = new Texture2D(width, depth);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels(colorMap);
        tex.Apply();
        return tex;
    }

    private void Generator()
    {
        // Anzahl von Punkten (Vektoren/Vertices) in 3D-Raum
        Vector3[] vertexbuffer = new Vector3[(xSize + 1) * (ySize + 1)];
        // Anzahl von Verbindungen (Indices) in 3D-Raum
        int[] indexbuffer = new int[xSize * ySize * 6];
        // 2D Punkte (Koordinaten) der Textur die angesprochen werden um in den 3D Raum gepackt zu werden.
        Vector2[] uvBuffer = new Vector2[(xSize + 1) * (ySize + 1)];
        // Noise = Fernsehrauschen (schwarz weiss punkte) - kreiiert unregelmäßige oberfläche
        float[,] heightMap = GenerateNoiseMap(ySize+1, xSize+1, perlinScale);
        // Heightmap auf die textur übertragen (transferieren)
        Texture2D heightTex = BuildTexture(heightMap);

        // Höhenvarianz auf punkte und texturpunkte übertragen
        for (int i = 0, y = 0; y <= ySize; y++)
        {
            for (int x = 0; x <= xSize; x++, i++)
            {
                vertexbuffer[i] = new Vector3(x, heightCurve.Evaluate(heightMap[x, y]) * maxHeight, y);
                uvBuffer[i] = new Vector2((float)y / (float)ySize, (float)x / (float)xSize);
            }
        }

        // Verbindung zwischen den punkten erzeugen (kreiieren)
        for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
        {
            for (int x = 0; x < xSize; x++, ti += 6, vi++)
            {
                indexbuffer[ti] = vi;
                indexbuffer[ti + 3] = indexbuffer[ti + 2] = vi + 1;
                indexbuffer[ti + 4] = indexbuffer[ti + 1] = vi + xSize + 1;
                indexbuffer[ti + 5] = vi + xSize + 2;
            }
        }

        // Übertragung (transferierung) auf die Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertexbuffer;
        mesh.triangles = indexbuffer;
        mesh.uv = uvBuffer;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material.mainTexture = heightTex;
    }

    // Useful to see errors
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;

        // For every Vertices it draws a small Sphere
        for (int i = 0; i < GetComponent<MeshFilter>().sharedMesh.vertexCount; i++)
        {
            Gizmos.DrawSphere(GetComponent<MeshFilter>().sharedMesh.vertices[i], 0.1f);
        }
    }
}
