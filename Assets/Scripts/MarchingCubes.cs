using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubes : MonoBehaviour
{
    [SerializeField] private int width = 30;
    [SerializeField] private int height = 10;

    [SerializeField] float resolution = 1;
    [SerializeField] float noiseScale = 1;

    [SerializeField] private float heightTresshold = 0.5f;

    [SerializeField] bool visualizeNoise;
    [SerializeField] bool use3DNoise;
    public bool isSmoothShading = false;

    [SerializeField] public Material mat;

    private Dictionary<Vector3, int> vertexDict = new Dictionary<Vector3, int>();
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private float[,,] heights;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        UpdateMesh(); 
        StartCoroutine(TestAll());
    }

    void Update()
    {

    }

    public void AddHeightSphere(Vector3 pos, float radius, float power)
    {
        if (pos == null) return;
        Vector3Int center = PositionToIndex(pos);
        int r = Mathf.CeilToInt(radius / resolution);

        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                for (int z = -r; z <= r; z++)
                {
                    Vector3Int index = center + new Vector3Int(x, y, z);

                    if (index.x < 0 || index.x >= width + 1 ||
                        index.y < 0 || index.y >= 2 * height + 1 ||
                        index.z < 0 || index.z >= width + 1)
                        continue;

                    Vector3 worldPos = IndexToPosition(index);
                    float dist = Vector3.Distance(worldPos, pos);

                    if (dist > radius) continue;

                    float falloff = 1 - (dist / radius); // 가까울수록 강하게
                    heights[index.x, index.y, index.z] += power * falloff;
                }
            }
        }
        MarchCubes();
    }


    // position(Vector3) -> height 배열 인덱스(Vector3Int) 변환
    private Vector3Int PositionToIndex(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / resolution);
        int y = Mathf.RoundToInt(position.y / resolution);
        int z = Mathf.RoundToInt(position.z / resolution);

        return new Vector3Int(x, y, z);
    }

    // height 배열 인덱스(Vector3Int) -> 실제 position(Vector3) 변환
    private Vector3 IndexToPosition(Vector3Int index)
    {
        float x = index.x * resolution;
        float y = index.y * resolution;
        float z = index.z * resolution;

        return new Vector3(x, y, z);
    }


    private IEnumerator TestAll()
    {
        while (true)
        {
            SetMesh();
            yield return new WaitForSeconds(0.1f);
        }
    }

    [ContextMenu("Execute UpdateMesh")]
    private void UpdateMesh()
    { 
        SetHeights();
        MarchCubes();
    }

    private void SetMesh()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

    }
    private void SetHeights()
    {
        heights = new float[width + 1, 2 * height + 1, width + 1];

        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < 2 * height + 1; y++)
            {
                for (int z = 0; z < width + 1; z++)
                {
                    if (use3DNoise)
                    {
                        float currentHeight = PerlinNoise3D((float)x / width * noiseScale, (float)y / height * noiseScale, (float)z / width * noiseScale);

                        heights[x, y, z] = currentHeight;
                    }
                    else
                    {
                        float currentHeight = height * Mathf.PerlinNoise(x * noiseScale,
                                                                         z * noiseScale);
                        float distToSufrace;

                        if (y <= currentHeight - 0.5f)
                            distToSufrace = 0f;
                        else if (y > currentHeight + 0.5f)
                            distToSufrace = 1f;
                        else if (y > currentHeight)
                            distToSufrace = y - currentHeight;
                        else
                            distToSufrace = currentHeight - y;

                        heights[x, y, z] = distToSufrace;
                    }
                }
            }
        }
    }

    private float PerlinNoise3D (float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float xz = Mathf.PerlinNoise(x, z);
        float yz = Mathf.PerlinNoise(y, z);

        float yx = Mathf.PerlinNoise(y, x);
        float zx = Mathf.PerlinNoise(z, x);
        float zy = Mathf.PerlinNoise(z, y);

        return (xy + xz + yz + yx + zx + zy) / 6;
    }

    private int GetConfigIndex (float[] cubeCorners)
    {
        int configIndex = 0;

        for (int i = 0; i < 8; i++)
        {
            if (cubeCorners[i] > heightTresshold)
            {
                configIndex |= 1 << i;
            }
        }

        return configIndex;
    }

    public void MarchCubes()
    {
        vertices.Clear();
        triangles.Clear();
        vertexDict.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < 2 * height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    float[] cubeCorners = new float[8];

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int corner = new Vector3Int(x, y, z) + MarchingTable.Corners[i];
                        cubeCorners[i] = heights[corner.x, corner.y, corner.z];
                    }

                    MarchCube(new Vector3(x, y, z), cubeCorners);
                }
            }
        }
    }

    private void MarchCube (Vector3 position, float[] cubeCorners)
    {
        int configIndex = GetConfigIndex(cubeCorners);

        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }

        int edgeIndex = 0;
        for (int t = 0; t < 5; t++)
        {
            for (int v = 0; v < 3; v++)
            {
                int triTableValue = MarchingTable.Triangles[configIndex, edgeIndex];

                if (triTableValue == -1)
                {
                    return;
                }

                Vector3 edgeStart = position + MarchingTable.Edges[triTableValue, 0];
                Vector3 edgeEnd = position + MarchingTable.Edges[triTableValue, 1];

                Vector3 vertex = (edgeStart + edgeEnd) / 2;
                if(isSmoothShading)
                {
                    if (!vertexDict.ContainsKey(vertex))
                    {
                        vertexDict[vertex] = vertices.Count;
                        vertices.Add(vertex);
                    }
                    triangles.Add(vertexDict[vertex]);
                }
                else
                {
                    vertices.Add(vertex);
                    triangles.Add(vertices.Count - 1);
                }

                edgeIndex++;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!visualizeNoise || !Application.isPlaying)
        {
            return;
        }

        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < width + 1; z++)
                {
                    Gizmos.color = new Color(heights[x, y, z], heights[x, y, z], heights[x, y, z], 1);
                    Gizmos.DrawSphere(new Vector3(x * resolution, y * resolution, z * resolution), 0.2f * resolution);
                }
            }
        }
    }
}
