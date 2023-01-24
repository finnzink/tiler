using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenroseGenerator : MonoBehaviour
{
    public List<Vector3> icos = new List<Vector3>();
    public GameObject spherePrefab;
    public Material material;

    // Start is called before the first frame update
    void Start()
    {

        material.color = new Color(1, 1, 1, .5f);
        // generate offsets
        int seed = (int)System.DateTime.Now.Ticks;
        Random.InitState(seed);
        float[] randomNumbers = new float[6];

        for (int i = 0; i < randomNumbers.Length; i++) {
            randomNumbers[i] = Random.Range(0f, 1f);
            Debug.Log(randomNumbers);
            // randomNumbers[i] = .5f;
        }

        Debug.Log(randomNumbers);

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

        Debug.Log(string.Join(", ", icos));

        int p = 1; // number of parallel planes
        Plane[,] planes = new Plane[6, p];

        for (int i = 0; i < 6; i++) {
            for (int j = 0; j < p; j++) {
                planes[i, j] = new Plane(icos[i], randomNumbers[i] + j);
            }
        }

         
        List<Vector3> points = new List<Vector3>();

        // first three loops are for which 3 intersecting bases
        for (int h = 0; h < 6; h++) {
            for (int i = 0; i < 6; i++) {
                for (int j = 0; j < 6; j++) {
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
                                    Debug.Log(intersection);
                                    points.Add(intersection);
                                }   
                            }
                        }
                    }
                }
            }
        }

        // display all the points
        foreach (Vector3 point in points)
        {
            Instantiate(spherePrefab, point, Quaternion.identity);
        }
        GameObject prefabInstance = GameObject.Find("origin");
        prefabInstance.GetComponent<MeshRenderer>().enabled = false;

        // display the planes
        for (int i = 0; i < planes.GetLength(0); i++)
        {
            for (int j = 0; j < planes.GetLength(1); j++)
            {
                renderPlane(planes[i, j].flipped);
                renderPlane(planes[i, j]);
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        
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