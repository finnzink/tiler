using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile {
    public Vector3 point;
    public Vector3[] vertices;
    public int[,] k;
    public Material material;
    public int pos;

    // for each (hashed) face in the Tile, will return the face index in the mesh's triangle array, OR -1 if its not in the mesh
    public Dictionary<Vector3,int> tris_in_mesh; 
    public bool filled;

    public Tile(Vector3 point, int[,] k, Vector3[] vertices, Material material, int pos, Dictionary<Vector3,int> tris_in_mesh, bool filled) {
        this.point = point;
        this.k = k;
        this.vertices = vertices;
        this.material = material;
        this.pos = pos;
        this.tris_in_mesh = tris_in_mesh;
    }
}

// struct Chunk {
//     public Vector3 coords;
//     public Vector3[,] faces;
// }

public class PenroseGenerator : MonoBehaviour
{   
    // some explanation for the below montrosity: 
    // first is needed to get pointers to the associated tiles. 
    // Second is for the normals, otherwise its impossible to tell which of the tiles is facing the player (although yes it is, just use filled)
    // third is the position of the face in the triangles array, so it can be added / deleted from the global mesh
    public Dictionary<Vector3, ((Tile, Tile), (Vector3, Vector3), (int, int))> faceMap = new Dictionary<Vector3, ((Tile, Tile), (Vector3, Vector3), (int, int))>();
    public List<Vector3> icos = new List<Vector3>();
    public List<Tile> tiles = new List<Tile>();
    public GameObject terrainObj;
    public GameObject spherePrefab;
    public GameObject spherePrefab2;
    public List<int> freeTriangles; // updated on deletions, checked on insertions
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

    public Vector3[] globalVertices;
    public Vector3[] globalNormals;
    public Mesh globalMesh;
    public int currVertex;

    public Material white_material;
    public int num_chunks;
    public int pos;

    // Start is called before the first frame update
    void Start()
    {
        num_chunks = 2;
        pos = 0;
        globalVertices= new Vector3[7000*num_chunks];
        globalNormals= new Vector3[7000*num_chunks];
        globalMesh = new Mesh();
        currVertex = 0;

        white_material = new Material(Shader.Find("Standard"));
        white_material.color = Color.white;

        terrainObj = new GameObject("terrain");
        // Mesh combinedMesh = new Mesh();
        // combinedMesh.CombineMeshes(combine.ToArray(), true, true);

        MeshFilter meshFilter = terrainObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainObj.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = terrainObj.AddComponent<MeshCollider>();


        meshRenderer.material = white_material;


        debugPlanes = false;
        showRhombs = true;
        loadedChunk.Add(new GameObject());
        mesh = new Mesh();

        Material red_material = new Material(Shader.Find("Standard"));
        red_material.color = Color.red;

        Material green_material = new Material(Shader.Find("Standard"));
        green_material.color = Color.green;

        triangleObj = new GameObject();
        MeshFilter triMeshFilter = triangleObj.AddComponent<MeshFilter>();
        MeshRenderer triMeshRenderer = triangleObj.AddComponent<MeshRenderer>();
        triMeshRenderer.material = green_material;

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

        for (int i = 0; i < 2; i++) {
            for (int j = 0; j < 1; j++) {
                genChunk(new Vector3(i*8, 0, j*8));
            }
        }

        
        foreach (Tile tile in tiles) {
            addRhombToMesh(tile);
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
            // Debug.Log("OOF");
            return;
        }

        // Vector3 offset = -.2f * playerCamera.transform.forward; 

        Mesh m = meshCollider.sharedMesh;

        Vector3 p0 = m.vertices[m.triangles[closestHit.triangleIndex * 3 + 0]];
        Vector3 p1 = m.vertices[m.triangles[closestHit.triangleIndex * 3 + 1]];
        Vector3 p2 = m.vertices[m.triangles[closestHit.triangleIndex * 3 + 2]];

        float d01 = Vector3.Distance(p0, p1);
        float d12 = Vector3.Distance(p1, p2);
        float d20 = Vector3.Distance(p2, p0);

        Vector3 toFlip = new Vector3();
        Vector3 toFlip1 = new Vector3();
        Vector3 toFlip2 = new Vector3();

        // this seems very inefficient, but it works for now
        if (Mathf.Abs(d01 - d12) < .01) {
            toFlip = p1;
            toFlip1 = p2;
            toFlip2 = p0;
        } else if (Mathf.Abs(d01 - d20) < .01) {
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

        if (faceMap[key].Item1.Item1 == null || faceMap[key].Item1.Item2 == null) { return; }// no neighbor

        Tile t = faceMap[key].Item1.Item1;
        Tile t2 = faceMap[key].Item1.Item2;

        if (Vector3.Dot(playerCamera.transform.forward, faceMap[key].Item2.Item1) < 0) {
            // flip t and t2
            Tile temp = t; 
            t = t2;
            t2 = temp;
        }

        if (Input.GetMouseButtonDown(1)) {
            addRhombToMesh(t);
        }
        if (Input.GetMouseButtonDown(0)) {
            deleteRhombFromMesh(t2);
        }

    }

    public void genChunk(Vector3 chunk) {
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

        Vector3 centroid = new Vector3(0,0,0);

        Material material = new Material(Shader.Find("Standard"));
        material.color = new Color(1 - ((chunk[0] ) / 20), 1-((chunk[1]) / 20), 1- ((chunk[2]) / 20), 1f);


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

                                    // if (k == 4 && l == 4 && m == 4) {
                                    //     centroid = position[0];
                                    // }

                                    addRhombToTiles(position, chunk);
                                    
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

        globalMesh.vertices = globalVertices;
        globalMesh.normals = globalNormals;


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

    // this method should really only 1. flip vertices 2. add to faceMap 3. call addRhomb
    void addRhombToTiles(Vector3[] vertices, Vector3 chunk) {
        // MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        // meshFilter.mesh = mesh;
        mesh.vertices = vertices;

        // calc center of rhomb
        Vector3 center = new Vector3(0, 0, 0);
        for (int i = 0; i < 8; i++) {
            center += vertices[i];
        }
        center /= 8;

        // testing if flipped
        bool testflip = false;

        Vector3 edge1 = vertices[1] - vertices[0];
        Vector3 edge2 = vertices[5] - vertices[0];
        Vector3 normal_1 = Vector3.Cross(edge1, edge2).normalized;

        Vector3 dirToCenter = center - vertices[0];

        float dotProduct = Vector3.Dot(normal_1, dirToCenter);

        if (dotProduct > 0)
        {
            // Debug.Log("Normals are pointing towards each other");
            testflip = true;
            // Instantiate(spherePrefab2, center, Quaternion.identity);

            Vector3[] tempVerts = mesh.vertices;

            // flip the vertices 
            Vector3 temp0 = mesh.vertices[0];
            Vector3 temp1 = mesh.vertices[1];
            tempVerts[0] = mesh.vertices[6];
            tempVerts[1] = mesh.vertices[7];
            tempVerts[6] = temp0;
            tempVerts[7] = temp1;

            mesh.vertices = tempVerts;
        }

        // double fofilter = 8;
        double fofilter = 8;
        Vector3 COI = chunk;

        if (center[0] < COI[0] || center[0] > COI[0] + fofilter
            || center[1] < COI[1] || center[1] > COI[1] + fofilter
            || center[2] < COI[2] || center[2] > COI[2] + fofilter) {
            return;
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

        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // pos needs to be
        Tile curr = new Tile(new Vector3(0,0,0), null, mesh.vertices, null, 0, new Dictionary<Vector3,int>(), false);
        double fofofilter = 4;
        if (center[0] < COI[0] + fofofilter && center[0] > COI[0] 
            && center[1] < COI[1] + fofofilter && center[1] > COI[1]
            && center[2] < COI[2] + fofofilter && center[2] > COI[2]) {
            curr.filled = true;
            tiles.Add(curr);
        }

        for (int i = 0; i < triangles.Length; i+=6 ) {
            Vector3 key = RoundToNearestHundredth(mesh.vertices[triangles[i]] + mesh.vertices[triangles[i+1]] + mesh.vertices[triangles[i+2]] + mesh.vertices[triangles[i+5]]);

            Vector3 ab = mesh.vertices[triangles[i+1]] - mesh.vertices[triangles[i]];
            Vector3 ac = mesh.vertices[triangles[i+2]] - mesh.vertices[triangles[i]];
            // return Vector3.Cross(ab, ac).normalized;

            if (faceMap.ContainsKey(key)){
                if (faceMap[key].Item1.Item2 != null) {
                    Debug.Log("MORE THAN 2 FACES, MUST BE AN ERROR");
                } else {
                    faceMap[key] = ((faceMap[key].Item1.Item1, curr), (faceMap[key].Item2.Item1, Vector3.Cross(ab, ac).normalized), (faceMap[key].Item3.Item1, i));
                    // Debug.Log("adding second");
                }
            } else {
                faceMap.Add(key, ((curr, null), (Vector3.Cross(ab, ac).normalized, new Vector3()), (i, -1)));

            }
        }
        for (int i = 0; i < 8; i++) {
            globalVertices[(currVertex*8) + i] = mesh.vertices[i];
            globalNormals[(currVertex*8) + i] = mesh.normals[i];
        }
        curr.pos = currVertex; 
        currVertex += 1; 
    }

    // for adding tiles, how to know vertices index?
    // --> vertices should be added by default to the mesh.vertices as soon as the tile is generated even if tile isn't filled.
    // --> from there, the Tile object can store the index of its vertices in the mesh, rather than in an array in the object itself

    void addRhombToMesh(Tile toAdd) {
        toAdd.filled = true;
        // check each of the six faces to see if the corresponding neighbor tiles are sharing a face that is in the mesh (using faceMap and Tile.tris_in_mesh)
        // if so, that means that face needs to be deleted from the mesh (so mesh's triangles assoc with the face are modified to 0,0,0 and freeTriangles is updated)
        // if not, that means the face should be added to the mesh (freeTriangles checked -> if empty increase triangles array by 3*2*8 and freeTriangles by 8, otherwise add to triangles directly)
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
        
        // Mesh currMesh = terrainObj.GetComponent<MeshFilter>().sharedMesh;
        Mesh currMesh = globalMesh;
        List<int> pretrianglesToAdd = new List<int>();
        List<int> trianglesToDel = new List<int>();
        List<Vector3> keys = new List<Vector3>();

        // check neighbor tiles
        for (int i = 0; i < triangles.Length; i+=6) {
            Vector3 key = RoundToNearestHundredth(toAdd.vertices[triangles[i]] + toAdd.vertices[triangles[i+1]] + toAdd.vertices[triangles[i+2]] + toAdd.vertices[triangles[i+5]]);
            Tile neighbor = (faceMap[key].Item1.Item1 == toAdd) ? faceMap[key].Item1.Item2 : faceMap[key].Item1.Item1;

            // if (neighbor.tris_in_mesh.ContainsKey(key)) {}
            // if (neighbor.tris_in_mesh == null) { Debug.Log("tris in mesh null");}

            if (neighbor != null && neighbor.tris_in_mesh.ContainsKey(key)) {
                // Tile.tris_in_mesh[key] needs to be deleted.
                for (int h = 0; h < 6; h++) {
                    trianglesToDel.Add(neighbor.tris_in_mesh[key] + h);
                }
                neighbor.tris_in_mesh.Remove(key);
            } else {
                // update Tile.tris_in_mesh, and add it to the mesh.
                keys.Add(key);
                for (int h = 0; h < 6; h++) {
                    pretrianglesToAdd.Add(triangles[i+h]);
                }
            }
        }
        int[] trianglesToAdd = pretrianglesToAdd.ToArray();

        int[] newTriangles;

        if (trianglesToAdd.Length > freeTriangles.Count) {
            newTriangles = new int[currMesh.triangles.Length + trianglesToAdd.Length];
            // add triangles
            int j =0;
            for (int i = 0; i < newTriangles.Length; i++) {
                
                if (i < currMesh.triangles.Length) {
                    newTriangles[i] = currMesh.triangles[i];
                } else {
                    if ((j %6) == 0) {toAdd.tris_in_mesh.Add(keys[j/6], i);}
                    newTriangles[i] = trianglesToAdd[j] + (toAdd.pos * 8);
                    // Debug.Log(toAdd.pos);
                    j++;
                }
            }
        } else {
            newTriangles = currMesh.triangles;
            for (int i = 0; i < trianglesToAdd.Length; i++) {
                // if (newTriangles[freeTriangles[0]] != 0) {Debug.Log("PROBLEM!");}
                // Debug.Log("adding index " + freeTriangles[0]);
                if ((i %6) == 0) {toAdd.tris_in_mesh.Add(keys[i/6], freeTriangles[0]);}
                newTriangles[freeTriangles[0]] = trianglesToAdd[i] + (toAdd.pos * 8);
                freeTriangles.RemoveAt(0);
            }
        }

        // delete triangles, replace with 0's
        for (int i = 0; i < trianglesToDel.Count; i++) {
            // Debug.Log("deleting index " + trianglesToDel[i]);
            newTriangles[trianglesToDel[i]] = 0;
            freeTriangles.Add(trianglesToDel[i]);
        }

        globalMesh.triangles = newTriangles;

        terrainObj.GetComponent<MeshFilter>().sharedMesh = globalMesh;
        terrainObj.GetComponent<MeshCollider>().sharedMesh = globalMesh;
    }

    void deleteRhombFromMesh(Tile toAdd) {
        toAdd.filled = false;
        // check each of the six faces to see if the corresponding neighbor is filled (using faceMap and Tile.filled)
        // if so, that means the corresponding face needs to be added to the mesh
        // also need to delete the faces of the Tile itself 

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
        
        Mesh currMesh = globalMesh;
        List<int> pretrianglesToAdd = new List<int>();
        List<int> trianglesToDel = new List<int>();

        // check neighbor tiles
        for (int i = 0; i < triangles.Length; i+=6) {
            Vector3 key = RoundToNearestHundredth(toAdd.vertices[triangles[i]] + toAdd.vertices[triangles[i+1]] + toAdd.vertices[triangles[i+2]] + toAdd.vertices[triangles[i+5]]);
            Tile neighbor; 
            int tri = -1;
            if (faceMap[key].Item1.Item1 == toAdd) {
                neighbor = faceMap[key].Item1.Item2;
                tri = faceMap[key].Item3.Item2;
            } else { 
                neighbor = faceMap[key].Item1.Item1;
                tri = faceMap[key].Item3.Item1;
            }

            if (neighbor == null) {
                Debug.Log("neighbor null?");
                return;
            }

            if (neighbor != null && neighbor.filled == true) {
                neighbor.tris_in_mesh.Add(key, pretrianglesToAdd.Count + currMesh.triangles.Length);
                for (int h = 0; h < 6; h++) {
                    pretrianglesToAdd.Add((triangles[tri+h]) + (neighbor.pos*8));
                }
            }

            foreach (int value in toAdd.tris_in_mesh.Values) {
                for (int h = 0; h < 6; h++) {
                    trianglesToDel.Add(value + h);
                }
            }
            toAdd.tris_in_mesh.Clear();
        }
        int[] trianglesToAdd = pretrianglesToAdd.ToArray();
        int[] newTriangles = new int[currMesh.triangles.Length + (trianglesToAdd.Length)];

        // add triangles
        int j =0;
        for (int i = 0; i < newTriangles.Length; i++) {
            if (i < currMesh.triangles.Length) {
                newTriangles[i] = currMesh.triangles[i];
            } else {
                newTriangles[i] = trianglesToAdd[i - currMesh.triangles.Length];
            }
        }

        // delete triangles, replace with 0's
        for (int i = 0; i < trianglesToDel.Count; i++) {
            newTriangles[trianglesToDel[i]] = 0;
        }

        globalMesh.triangles = newTriangles;

        terrainObj.GetComponent<MeshFilter>().sharedMesh = globalMesh;
        terrainObj.GetComponent<MeshCollider>().sharedMesh = globalMesh;

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
        int roundVal = 100;
        // return vector;
        return new Vector3(
            (float)Mathf.Round(vector.x * roundVal) / roundVal,
            (float)Mathf.Round(vector.y * roundVal) / roundVal,
            (float)Mathf.Round(vector.z * roundVal) / roundVal
        );
    }
}