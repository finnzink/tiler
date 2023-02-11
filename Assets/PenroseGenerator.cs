using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Tile {
    public Vector3 point;
    public Vector3[] vertices;
    public int[,] k;

    public Tile(Vector3 point, int[,] k, Vector3[] vertices) {
        this.point = point;
        this.k = k;
        this.vertices = vertices;
    }
}

public class PenroseGenerator : MonoBehaviour
{
    public List<Vector3> icos = new List<Vector3>();
    public GameObject spherePrefab;
    public Material material;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public Mesh mesh;

    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        // meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        material.color = new Color(1, 1, 1, 1);
        // generate offsets
        int seed = (int)System.DateTime.Now.Ticks;
        Random.InitState(seed);
        float[] randomNumbers = new float[6];

        for (int i = 0; i < randomNumbers.Length; i++) {
            randomNumbers[i] = Random.Range(0f, 1f);
            // Debug.Log(randomNumbers);
            // randomNumbers[i] = 0f;
        }

        // Debug.Log(randomNumbers);

        // generate the icos basis
        float sqrt5 = Mathf.Sqrt(5);
        for (int n = 0; n < 5; n++)
        {
            float x = (2.0f / sqrt5) * Mathf.Cos(2 * Mathf.PI * n / 5);
            float y = (2.0f / sqrt5) * Mathf.Sin(2 * Mathf.PI * n / 5);
            float z = 1.0f / sqrt5;
            icos.Add(new Vector3(x, y, z));
        }
        icos.Add(new Vector3(0.0f, 0.0f, 1.0f));

        // Debug.Log(string.Join(", ", icos));

        int p = 2; // number of parallel planes
        Plane[,] planes = new Plane[6, p];

        for (int i = 0; i < 6; i++) {
            for (int j = 0; j < p; j++) {
                planes[i, j] = new Plane(icos[i], randomNumbers[i] + j);
            }
        }

         
        List<Vector3> points = new List<Vector3>();

        List<Tile> tiles = new List<Tile>();

        // first three loops are for which 3 intersecting bases
        for (int h = 0; h < 6; h++) {
            for (int i = h + 1; i < 6; i++) {
                for (int j = i + 1; j < 6; j++) {
                    // last three are for which basis plane for each given basis
                    if (h != i && i != j && j != h) {
                        for (int k = 0; k < p; k++) {
                            for (int l = 0; l < p; l++) {
                                for (int m = 0; m < p; m++) {
                                    var det = Vector3.Dot( Vector3.Cross( planes[j, m].normal, planes[i, l].normal ), planes[h, k].normal );
                                    
                                    Vector3 intersection = 
                                        ( -( planes[h, k].distance * Vector3.Cross( planes[i, l].normal, planes[j, m].normal ) ) -
                                        ( planes[i, l].distance * Vector3.Cross( planes[j, m].normal, planes[h, k].normal ) ) -
                                        ( planes[j, m].distance * Vector3.Cross( planes[h, k].normal, planes[i, l].normal ) ) ) / det;

                                    points.Add(intersection);
                                    
                                    int[,] starter_k = new int[6,8];
                                    for (int ind = 0; ind < 6; ind++) {
                                        for (int ind2 = 0; ind2 < 8; ind2++) {
                                            // magic formula which I don't understand :)
                                            starter_k[ind, ind2] = Mathf.CeilToInt(Vector3.Dot(icos[ind], intersection) - randomNumbers[ind]);
                                        }
                                    }

                                    for (int indy = 0; indy < 8; indy++) {
                                        // replace the 3 plane values with their offset indices
                                        starter_k[h,indy] = k + (indy & 1);
                                        starter_k[i,indy] = l + (indy >> 1 & 1);
                                        starter_k[j,indy] = m + (indy >> 2 & 1);
                                    }

                                    Vector3[] position = new Vector3[8];

                                    
                                    for (int indexy = 0; indexy < 8; indexy++) {
                                        for (int index = 0; index < 6; index++) {
                                            position[indexy] += starter_k[index, indexy] * icos[index];
                                        }
                                    }

                                    // Make the Tile object
                                    Tile curr = new Tile(intersection, starter_k, position);
                                    tiles.Add(curr);
                                }   
                            }
                        }
                    }
                }
            }
        }

        bool displayPlanes = false;
        bool displayPoints = true;
        int numTiles = tiles.Count;
        // display all the points
        if (displayPoints) {
            for (int i = 0; i < numTiles; i++) {
                foreach (Vector3 vertex in tiles[i].vertices)
                Instantiate(spherePrefab, vertex, Quaternion.identity);
            }
        }
        GameObject prefabInstance = GameObject.Find("origin");

        // display the planes
        if (displayPlanes) {
            for (int i = 0; i < planes.GetLength(0); i++)
            {
                for (int j = 0; j < planes.GetLength(1); j++)
                {
                    renderPlane(planes[i, j].flipped);
                    renderPlane(planes[i, j]);
                }
            }
        }

        // int tileInd = 1;
        // for(int i = 0; i < 8; i++) {
        //     Instantiate(spherePrefab, tiles[tileInd].vertices[i], Quaternion.identity);
        //     Debug.Log(tiles[tileInd].vertices[i]);
        // }
        for (int i = 0; i < numTiles; i++) {
            renderRhomb(tiles[i].vertices);
        }

        
        // prefabInstance.GetComponent<MeshRenderer>().enabled = false;

    }

    // Update is called once per frame
    void Update()
    {
        Camera playerCamera = GameObject.Find("PlayerCamera").GetComponent<Camera>();

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            if (hit.triangleIndex != -1) {
                Mesh mesh = hit.collider.GetComponent<MeshFilter>().mesh;
                int triangleIndex = hit.triangleIndex;

                GetComponent<MeshFilter>().mesh = mesh;
                // Update the mesh with the new colors

                Material material = new Material(Shader.Find("Unlit/Color"));
                material.color = Color.red;
                mesh.RecalculateNormals();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
                meshRenderer.material = material;
            }
        }
    }

    void renderRhomb(Vector3[] vertices) {
        // MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        // meshFilter.mesh = mesh;
        mesh.vertices = vertices;

        int[] triangles = new int[]
        {
            0, 2, 1, // 0, 1, 2, 3
            1, 2, 3,

            0, 1, 5, // 0, 1, 4, 5
            0, 5, 4,

            0, 4, 6, // 0, 2, 4, 6
            0, 6, 2,

            6, 5, 7, // 4, 5, 6, 7
            5, 6, 4,

            1, 3, 7, // 1, 3, 5, 7
            1, 7, 5,
            
            2, 6, 3, // 2, 3, 6, 7
            3, 6, 7
        };

        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        // MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        

        GameObject terrainMesh = new GameObject();
        MeshFilter meshFilter = terrainMesh.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainMesh.AddComponent<MeshRenderer>();

        meshCollider.sharedMesh = mesh;
        meshRenderer.material = material;

        // Set the mesh and material properties
        meshFilter.mesh = mesh;
        meshRenderer.material = material;

        // Set the parent of the mesh object to the current object
        terrainMesh.transform.SetParent(transform);
    }

    void renderPlane(Plane plane) {
        float length = 5;
        GameObject square = new GameObject("Square");
        MeshFilter meshFilter = square.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = square.AddComponent<MeshRenderer>();

        // Define the vertices, triangles, and UVs of the square
        Vector3[] vertices = new Vector3[] {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, -0.5f)
        };
        int[] triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        Vector2[] uvs = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0)
        };

        // Assign the vertices, triangles, and UVs to the MeshFilter's mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;


        // Rotate the square GameObject to align with the Plane's normal vector
        square.transform.rotation = Quaternion.FromToRotation(Vector3.up, plane.normal);

        // Position the square GameObject by setting its transform.position to the Plane's normal vector multiplied by the Plane.distance
        square.transform.position = plane.normal * plane.distance;

        

        // Scale the square GameObject to have a length of n
        square.transform.localScale = new Vector3(length, length, length);

        meshRenderer.material = material;
    }

}