using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile {
    public Vector3 point;
    public Vector3[] vertices;
    public int[,] k;
    public Material material;

    public Tile(Vector3 point, int[,] k, Vector3[] vertices, Material material) {
        this.point = point;
        this.k = k;
        this.vertices = vertices;
        this.material = material;
    }
}

// struct Chunk {
//     public Vector3 coords;
//     public Vector3[,] faces;
// }

public class PenroseGenerator : MonoBehaviour
{
    public Dictionary<Vector3, (Tile, Tile)> faceMap = new Dictionary<Vector3, (Tile, Tile)>();
    public List<Vector3> icos = new List<Vector3>();
    public GameObject spherePrefab;
    public GameObject spherePrefab2;
    // public Material material;
    // public MeshFilter meshFilter;
    // public MeshCollider meshCollider;
    public List<GameObject> loadedChunk;
    public GameObject triangleObj;
    public GameObject triangleObj2;
    public Mesh mesh;
    public float[] randomNumbers = new float[6];
    public bool debugPlanes;
    public bool showRhombs;

    // Start is called before the first frame update
    void Start()
    {
        debugPlanes = false;
        showRhombs = true;
        loadedChunk.Add(new GameObject());
        mesh = new Mesh();

        Material red_material = new Material(Shader.Find("Standard"));
        red_material.color = Color.red;

        triangleObj = new GameObject();
        MeshFilter triMeshFilter = triangleObj.AddComponent<MeshFilter>();
        MeshRenderer triMeshRenderer = triangleObj.AddComponent<MeshRenderer>();
        triMeshRenderer.material = red_material;

        triangleObj2 = new GameObject();
        MeshFilter tri2MeshFilter = triangleObj2.AddComponent<MeshFilter>();
        MeshRenderer tri2MeshRenderer = triangleObj2.AddComponent<MeshRenderer>();
        tri2MeshRenderer.material = red_material;
        // meshFilter = GetComponent<MeshFilter>();
        // meshCollider = GetComponent<MeshCollider>();
        // material.color = new Color(1, 1, 1, 1);
        // generate offsets
        int seed = (int)System.DateTime.Now.Ticks;
        Random.InitState(seed);
        

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

        for (int i = 0; i < 1; i++) {
            for (int j = 0; j < 1; j++) {
                genChunk(new Vector3(i*8, 0, j*8));
            }
        }
        
        // prefabInstance.GetComponent<MeshRenderer>().enabled = false;

    }

    // Update is called once per frame
    void Update()
    {
        // return; 
        // Debug.Log("SHOULD NOT PRINT");
        Camera playerCamera = GameObject.Find("Player Camera").GetComponent<Camera>();

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        RaycastHit closestHit = new RaycastHit();
        float closestDistance = float.MaxValue;

        RaycastHit[] hits = Physics.RaycastAll(ray);
        foreach (RaycastHit hit in hits)
        {
            if (hit.distance < closestDistance)
            {
                closestHit = hit;
                closestDistance = hit.distance;
            }
        }
        MeshCollider meshCollider = closestHit.collider as MeshCollider;

        if (hits.Length == 0 || meshCollider == null)
        {
            return;
        }

        Vector3 offset = -.2f * playerCamera.transform.forward; 

        Vector3 p0 = meshCollider.sharedMesh.vertices[meshCollider.sharedMesh.triangles[closestHit.triangleIndex * 3 + 0]];
        Vector3 p1 = meshCollider.sharedMesh.vertices[meshCollider.sharedMesh.triangles[closestHit.triangleIndex * 3 + 1]];
        Vector3 p2 = meshCollider.sharedMesh.vertices[meshCollider.sharedMesh.triangles[closestHit.triangleIndex * 3 + 2]];

        float d01 = Vector3.Distance(p0, p1);
        float d12 = Vector3.Distance(p1, p2);
        float d20 = Vector3.Distance(p2, p0);

        Vector3 toFlip = new Vector3();
        Vector3 toFlip1 = new Vector3();
        Vector3 toFlip2 = new Vector3();

        // this seems very inefficient, but it works for now
        if (Mathf.Abs(d01 - d12) < .001) {
            toFlip = p1;
            toFlip1 = p2;
            toFlip2 = p0;
        } else if (Mathf.Abs(d01 - d20) < .001) {
            toFlip = p0; // corr
            toFlip1 = p2;
            toFlip2 = p1;
        } else {
            toFlip = p2;
            toFlip1 = p1;
            toFlip2 = p0;
        }

        Vector3 midPoint = (toFlip1 + toFlip2);
        Vector3 finalFlip = midPoint - toFlip;

        Vector3 key = RoundToNearestHundredth(p0 + p1 + p2 + finalFlip); 

        if (!faceMap.ContainsKey(key)) {
            Debug.Log("FLOAT ERROR"); 
        }

        if (faceMap[key].Item1 == null || faceMap[key].Item2 == null) { return;}// no neighbor

        Tile t = new Tile(faceMap[key].Item1.point, faceMap[key].Item1.k, faceMap[key].Item1.vertices, faceMap[key].Item1.material);
        Tile t2 = new Tile(faceMap[key].Item2.point, faceMap[key].Item2.k, faceMap[key].Item2.vertices, faceMap[key].Item2.material);

        GameObject tempObj = renderRhomb(t);
        GameObject tempObj2 = renderRhomb(t2);

        MeshFilter triangleMeshFilter = triangleObj.GetComponent<MeshFilter>();
        MeshFilter tempMeshFilter = tempObj.GetComponent<MeshFilter>();
        triangleMeshFilter.sharedMesh = tempMeshFilter.sharedMesh;

        MeshFilter triangleMeshFilter2 = triangleObj2.GetComponent<MeshFilter>();
        MeshFilter tempMeshFilter2 = tempObj2.GetComponent<MeshFilter>();
        triangleMeshFilter2.sharedMesh = tempMeshFilter2.sharedMesh;

        Destroy(tempObj);
        Destroy(tempObj2);

        // Mesh triangleMesh = new Mesh();
        // triangleMesh.vertices = new Vector3[] { p0 + offset, p1 + offset, p2 + offset, finalFlip + offset, toFlip2 + offset, toFlip1 + offset};
        // triangleMesh.triangles = new int[] { 0, 1, 2, 3, 4, 5, 4, 3, 5 };

        // triangleMesh.RecalculateNormals();

        // MeshFilter triangleMeshFilter = triangleObj.GetComponent<MeshFilter>();
        // triangleMeshFilter.sharedMesh = triangleMesh;
    }

    public void genChunk(Vector3 chunk) {
        // GameObject toDestroy = loadedChunk; 
        // loadedChunk = new GameObject();
        // Destroy(toDestroy);
        
        Debug.Log("GENERATING NEW CHUNK" + string.Join(",", chunk));
        Debug.Log("RANDS: " + string.Join(",", randomNumbers));
        int yFunk = 0;
        int p = 8; // number of parallel planes
        Plane[,] planes = new Plane[6, p];

        // THIS IS BEST FOR CENTROIDS, TRY NOT TO CHANGE UNLESS YOU CHANGE THE CHUNK SIZE
        Vector3 testPoint = (chunk * .5f)+ new Vector3(2, 2, 2);
        // GameObject instantiatedSphere = Instantiate(spherePrefab, offsetPoint, Quaternion.identity);
        // instantiatedSphere.transform.SetParent(loadedChunk.transform);
        // float dist = Vector3.Distance(offsetPoint, Vector3.zero);

        int[] planeOffsets = new int[6];

        for (int i = 0; i < 6; i++) {
            planeOffsets[i] = (int)(Vector3.Dot(testPoint, icos[i]));
            for (int j = 0; j < p; j++) {
                // Vector3 exactOffset = offsetPoint + icos[i] * (j - (p/2) + randomNumbers[i]);
                // planeOffsets[i,j] = exactOffset;

                planes[i, j] = new Plane(icos[i], j - (p/2) + randomNumbers[i] + (int)(Vector3.Dot(testPoint, icos[i])));
                // Debug.Log("OFFSET: " + (randomNumbers[i] + j - (p/2) + (chunk[i] * p)).ToString());
            }
        }

         
        List<Vector3> points = new List<Vector3>();

        List<Tile> tiles = new List<Tile>();

        Vector3 centroid = new Vector3(0,0,0);

        Material material = new Material(Shader.Find("Standard"));
        material.color = new Color(1 - ((chunk[0] ) / 20), 1-((chunk[1]) / 20), 1- ((chunk[2]) / 20), 1f);

        double fofilter = 8;
        Vector3 COI = chunk;
        // can change chunk to centroid in the below loop if you want to see the chunk's center of mass

        List<CombineInstance> combine = new List<CombineInstance>();

        // first three loops are for which 3 intersecting bases
        for (int h = 0; h < 6; h++) {
            for (int i = h + 1; i < 6; i++) {
                for (int j = i + 1; j < 6; j++) {
                    // last three are for which basis plane for each given basis
                    // if (h != i && i != j && j != h) {
                        for (int k = 0; k < planes.GetLength(1); k++) {
                            for (int l = 0; l < planes.GetLength(1); l++) {
                                for (int m = 0; m < planes.GetLength(1); m++) {

                                    if (Mathf.Abs(k - (planes.GetLength(1) / 2)) * Mathf.Abs(l - (planes.GetLength(1) / 2)) * Mathf.Abs(m - (planes.GetLength(1) / 2)) > 20) {
                                        continue;
                                    }
                                    var det = Vector3.Dot( Vector3.Cross( planes[j, m].normal, planes[i, l].normal ), planes[h, k].normal );
                                    
                                    Vector3 intersection = 
                                        -1*( ( planes[h, k].distance * Vector3.Cross( planes[i, l].normal, planes[j, m].normal ) ) +
                                        ( planes[i, l].distance * Vector3.Cross( planes[j, m].normal, planes[h, k].normal ) ) +
                                        ( planes[j, m].distance * Vector3.Cross( planes[h, k].normal, planes[i, l].normal ) ) ) / det;

                                    if (debugPlanes) {
                                    Instantiate(spherePrefab, intersection, Quaternion.identity);
                                    }

                                    // check if intersection is within chunk
                                    bool outside = false;

                                    points.Add(intersection);
                                    
                                    int[,] starter_k = new int[6,8];
                                    for (int ind = 0; ind < 6; ind++) {
                                        for (int ind2 = 0; ind2 < 8; ind2++) {
                                            // magic formula which I don't understand :)
                                            starter_k[ind, ind2] = Mathf.CeilToInt(Vector3.Dot(icos[ind], intersection) - randomNumbers[ind]);
                                        }
                                    }

                                    for (int indy = 0; indy < 8; indy++) {
                                        // replace the 3 k values assoc. w/ intersecting plane values with their offset indices
                                        starter_k[h,indy] = planeOffsets[h] + k - (planes.GetLength(1)/2) + (indy & 1);
                                        starter_k[i,indy] = planeOffsets[i] + l - (planes.GetLength(1)/2) + (indy >> 1 & 1);
                                        starter_k[j,indy] = planeOffsets[j] + m - (planes.GetLength(1)/2) + (indy >> 2 & 1);
                                    }

                                    Vector3[] position = new Vector3[8];

                                    
                                    for (int indexy = 0; indexy < 8; indexy++) {
                                        for (int index = 0; index < 6; index++) {
                                            position[indexy] += starter_k[index, indexy] * icos[index];
                                        }
                                    }

                                    if (k == 4 && l == 4 && m == 4) {
                                        centroid = position[0];
                                    }
                                    
                                    Tile curr = new Tile(intersection, starter_k, position, material);
                                    tiles.Add(curr);

                                    if (curr.vertices[0][0] < COI[0] || curr.vertices[0][0] > COI[0] + fofilter
                                        || curr.vertices[0][1] < COI[1] || curr.vertices[0][1] > COI[1] + fofilter
                                        || curr.vertices[0][2] < COI[2] || curr.vertices[0][2] > COI[2] + fofilter) {
                                        continue;
                                    }
                                    if (showRhombs) {
                                        GameObject currRhomb = renderRhomb(curr);
                                        if (curr.vertices[0][1] < 5) {
                                            combine.Add(new CombineInstance(){
                                                mesh = currRhomb.GetComponent<MeshFilter>().sharedMesh,
                                                transform = currRhomb.GetComponent<MeshFilter>().transform.localToWorldMatrix
                                            });
                                        }
                                        Destroy(currRhomb);
                                    }
                                }   
                            }
                        }
                    // }
                }
            }
        }

        bool displayPoints = true;
        int numTiles = tiles.Count;
        // display all the points
        if (displayPoints) {
            for (int i = 0; i < numTiles; i++) {
                // foreach (Vector3 vertex in tiles[i].vertices)
                // Instantiate(spherePrefab, vertex, Quaternion.identity);
            }
        }

        GameObject terrainObj = new GameObject();
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine.ToArray(), true, true);

        MeshFilter meshFilter = terrainObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainObj.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = terrainObj.AddComponent<MeshCollider>();

        meshCollider.sharedMesh = combinedMesh;
        meshRenderer.material = material;
        meshFilter.sharedMesh = combinedMesh;

        renderChunk(chunk);

        // display the planes
        if (debugPlanes) {
            Debug.Log(debugPlanes);
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < planes.GetLength(1); j++){
                    if (chunk[0] == 0 && chunk[1] == 0 && chunk[2] == 0) {
                        // Debug.Log("plane0");
                        renderPlane(planes[i, j].flipped, true);
                        renderPlane(planes[i, j], true);
                    }
                    else {
                        // Debug.Log("plane1");
                        renderPlane(planes[i, j].flipped, false);
                        renderPlane(planes[i, j], false);
                    }
                }
            }
        }
    }

    GameObject renderRhomb(Tile curr) {
        // MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        // meshFilter.mesh = mesh;
        mesh.vertices = curr.vertices;

        // calc center of rhomb
        Vector3 center = new Vector3(0, 0, 0);
        for (int i = 0; i < 8; i++) {
            center += curr.vertices[i];
        }
        center /= 8;

        // testing if flipped
        bool testflip = false;

        Vector3 edge1 = curr.vertices[1] - curr.vertices[0];
        Vector3 edge2 = curr.vertices[5] - curr.vertices[0];
        Vector3 normal_1 = Vector3.Cross(edge1, edge2).normalized;

        Vector3 dirToCenter = center - curr.vertices[0];

        float dotProduct = Vector3.Dot(normal_1, dirToCenter);

        if (dotProduct > 0)
        {
            // Debug.Log("Normals are pointing towards each other");
            testflip = true;
            // Instantiate(spherePrefab2, center, Quaternion.identity);
        }


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
            3, 6, 7,
        };

        int[] triangles2 = new int[]
        {
            2, 0, 1, // 0, 1, 2, 3
            2, 1, 3,

            1, 0, 5, // 0, 1, 4, 5
            5, 0, 4,

            4, 0, 6, // 0, 2, 4, 6
            6, 0, 2,

            5, 6, 7, // 4, 5, 6, 7
            6, 5, 4,

            3, 1, 7, // 1, 3, 5, 7
            7, 1, 5,
            
            6, 2, 3, // 2, 3, 6, 7
            6, 3, 7,
        };

        for (int i = 0; i < triangles.Length; i+=6 ) {
            Vector3 key = RoundToNearestHundredth(curr.vertices[triangles[i]] + curr.vertices[triangles[i+1]] + curr.vertices[triangles[i+2]] + curr.vertices[triangles[i+5]]);
            if (faceMap.ContainsKey(key)){
                if (faceMap[key].Item2 != null) {
                    // quick and dirty, because this same func is used for add preview, but shouldn't add to map in that case.
                    // Debug.Log("MORE THAN 2 FACES, MUST BE AN ERROR");
                } else {
                    faceMap[key] = (faceMap[key].Item1, curr);
                    // Debug.Log("adding second");
                }
            } else {
                faceMap.Add(key, (curr, null));

            }
        }

        if (testflip) {
            mesh.triangles = triangles2;
        }
        else {
            mesh.triangles = triangles;
        }

        mesh.RecalculateNormals();

        GameObject terrainMesh = new GameObject();
        MeshFilter meshFilter = terrainMesh.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainMesh.AddComponent<MeshRenderer>();

        // meshCollider.sharedMesh = mesh;
        // meshRenderer.material = curr.material;

        // Set the mesh and material properties
        meshFilter.mesh = mesh;
        meshRenderer.material = curr.material;

        // Set the parent of the mesh object to the current object
        // terrainMesh.transform.SetParent(loadedChunk.transform);
        
        for (int i = 0; i < curr.vertices.Length; i++) {
            // Instantiate(spherePrefab, vertices[i], Quaternion.identity);
        }

        return terrainMesh;

    }

    void renderPlane(Plane plane, bool side) {
        float length = 10;
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

        // Position the square GameObject by setting its transform.position to the Plane's normal vector multiplied by the Plane.distance
        square.transform.position = plane.normal * plane.distance;

        // Rotate the square GameObject to align with the Plane's normal vector
        square.transform.rotation = Quaternion.FromToRotation(Vector3.up, plane.normal);

        

        // Scale the square GameObject to have a length of n
        square.transform.localScale = new Vector3(length, length, length);
        Material material = new Material(Shader.Find("Standard"));
        if (side) {material.color = Color.green;}
        else {material.color = Color.blue;}
        meshRenderer.material = material;

        // square.transform.SetParent(loadedChunk.transform);
    }

    void renderChunk(Vector3 chunk) {
        // Create a new GameObject and add a LineRenderer component to it
        GameObject gameObject = new GameObject("Chunk");
        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();

        Material material = new Material(Shader.Find("Standard"));
        material.color = Color.white;

        lineRenderer.material = material;
        lineRenderer.widthMultiplier = 0.1f;

        // generate vertices
        Vector3[] vertices = new Vector3[8];
        for (int i = 0; i < 8; i++) {
            vertices[i] = new Vector3(chunk.x + (i & 1) * 8, chunk.y + (i >> 1 & 1) * 8, chunk.z + (i >> 2 & 1) * 8);
        }

        // Connect the vertices to form the wireframe box.
        lineRenderer.positionCount = 16;
        lineRenderer.SetPosition(0, vertices[0]);
        lineRenderer.SetPosition(1, vertices[1]);
        lineRenderer.SetPosition(2, vertices[3]);
        lineRenderer.SetPosition(3, vertices[2]);
        lineRenderer.SetPosition(4, vertices[0]);

        lineRenderer.SetPosition(5, vertices[4]);

        lineRenderer.SetPosition(6, vertices[5]);
        lineRenderer.SetPosition(7, vertices[1]);
        lineRenderer.SetPosition(8, vertices[5]);

        lineRenderer.SetPosition(9, vertices[7]);
        lineRenderer.SetPosition(10, vertices[3]);
        lineRenderer.SetPosition(11, vertices[7]);

        lineRenderer.SetPosition(12, vertices[6]);
        lineRenderer.SetPosition(13, vertices[2]);
        lineRenderer.SetPosition(14, vertices[6]);

        lineRenderer.SetPosition(15, vertices[4]);
    }
    Vector3 RoundToNearestHundredth(Vector3 vector)
    {
        // return vector;
        return new Vector3(
            (float)Mathf.Round(vector.x * 100) / 100,
            (float)Mathf.Round(vector.y * 100) / 100,
            (float)Mathf.Round(vector.z * 100) / 100
        );
    }
}