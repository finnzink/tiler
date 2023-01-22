using UnityEngine;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{
    public Transform playerTransform;
    public int chunkSize = 16;
    public int viewDistance = 4;
    public GameObject cubePrefab;
    private Dictionary<Vector3, GameObject> chunks = new Dictionary<Vector3, GameObject>();

    // Use this for initialization
    void Start()
    {
        Debug.Log("hi");
        // create an initial cube at the center of the starting chunk
        Vector3 startChunk = new Vector3(0, 0, 0);
        CreateCube(startChunk);
    }

    // Update is called once per frame
    void Update()
    {
        // get the player's current chunk position
        Vector3 playerChunk = WorldToChunkCoord(playerTransform.position);

        // check if new chunks need to be generated
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int y = -viewDistance; y <= viewDistance; y++)
            {
                for (int z = -viewDistance; z <= viewDistance; z++)
                {
                    Vector3 chunkCoord = new Vector3(playerChunk.x + x, playerChunk.y + y, playerChunk.z + z);
                    if (!chunks.ContainsKey(chunkCoord))
                    {
                        // create new cube if the chunk has not been generated yet
                        CreateCube(chunkCoord);
                    }
                }
            }
        }

        // check if chunks need to be removed
        List<Vector3> chunksToRemove = new List<Vector3>();
        foreach (Vector3 chunkCoord in chunks.Keys)
        {
            if (Mathf.Abs(chunkCoord.x - playerChunk.x) > viewDistance ||
                Mathf.Abs(chunkCoord.y - playerChunk.y) > viewDistance ||
                Mathf.Abs(chunkCoord.z - playerChunk.z) > viewDistance)
            {
                chunksToRemove.Add(chunkCoord);
            }
        }
        // remove chunks that are too far away
       
        foreach (Vector3 chunkCoord in chunksToRemove)
        {
            GameObject chunk = chunks[chunkCoord];
            Destroy(chunk);
            chunks.Remove(chunkCoord);
        }
    }

    void CreateCube(Vector3 chunkCoord)
    {
        Vector3 cubePosition = ChunkToWorldCoord(chunkCoord) + new Vector3(chunkSize / 2, chunkSize / 2, chunkSize / 2);
        GameObject cube = Instantiate(cubePrefab, cubePosition, Quaternion.identity, this.transform);
        chunks.Add(chunkCoord, cube);
    }

    Vector3 WorldToChunkCoord(Vector3 worldPosition)
    {
        return new Vector3(Mathf.FloorToInt(worldPosition.x / chunkSize), Mathf.FloorToInt(worldPosition.y / chunkSize), Mathf.FloorToInt(worldPosition.z / chunkSize));
    }

    Vector3 ChunkToWorldCoord(Vector3 chunkCoord)
    {
        return new Vector3(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize, chunkCoord.z * chunkSize);
    }
}
