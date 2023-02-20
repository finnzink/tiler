using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public GameObject terrainGenator;
    public GameObject spherePrefab;
    public GameObject instantiatedSphere;
    private PenroseGenerator script1;
    public GameObject cam;
    public List<Vector3> icos = new List<Vector3>();
    public float moveSpeed; // adjust as desired
    public float jumpForce = 10f;
    public float mouseSensitivity = 100f; // adjust as desired
    public float upDownRange = 80.0f; // adjust as desired
    public float upDownSpeed = 2f; // adjust as desired
    public Transform playerBody;
    float rotX = 0f;
    public Vector3 currChunk = new Vector3(0, 0, 0);
    public float[] basisOffset = new float[6];
    public bool physicsOn = false;
    public Vector3 climbPoint = new Vector3();
    public Vector3 minClimb = new Vector3();
    public Vector3 maxClimb = new Vector3();

    private Rigidbody rb;
    private bool startingSnap;

    void Start()
    {
        instantiatedSphere = GameObject.Instantiate(spherePrefab);
        instantiatedSphere.name = "head";
        startingSnap = true;
        script1 = terrainGenator.GetComponent<PenroseGenerator>();
        moveSpeed = .5f;
        cam = GameObject.Find("Player Camera");
        if (cam == null) {
            Debug.Log("can't find camera");
        }

        Cursor.lockState = CursorLockMode.Locked;
        float sqrt5 = Mathf.Sqrt(5);
        for (int n = 0; n < 5; n++)
        {
            float x = (2.0f / sqrt5) * Mathf.Cos(2 * Mathf.PI * n / 5);
            float y = (2.0f / sqrt5) * Mathf.Sin(2 * Mathf.PI * n / 5);
            float z = 1.0f / sqrt5;
            icos.Add(new Vector3(x, y, z));
        }
        icos.Add(new Vector3(0.0f, 0.0f, 1.0f));
        terrainGenator = GameObject.Find("TerrainController");
    }

    void OnGUI()
    {
        Vector3 playerPosition = playerBody.position;
        GUI.backgroundColor = Color.black;
        // string[] formattedStrings = new string[basisOffset.Length];
        // for (int i = 0; i < basisOffset.Length; i++)
        // {
        //     formattedStrings[i] = string.Format("{0:0.##}", basisOffset[i]);
        // }

        Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);

        // GUI.color = Color.white;
        GUI.DrawTexture(new Rect(center.x - 10 / 2, center.y, 10, 1), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(center.x, center.y - 10 / 2, 1, 10), Texture2D.whiteTexture);

        GUI.Box(new Rect(0, 0, 300, 75), 
        "Position: " + playerPosition.ToString() 
        // + "\nBasis: " + string.Join(",", formattedStrings) 
        + "\nChunk: " + string.Join(",", currChunk)
        + "\nFPS: " + (1.0f / Time.deltaTime).ToString());
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.P)) {
            physicsOn = !physicsOn;
            startingSnap = true;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, -upDownRange, upDownRange);

        cam.transform.localEulerAngles = new Vector3(rotX, cam.transform.localEulerAngles.y, 0);
        cam.transform.Rotate(Vector3.up * mouseX);

        // create a rotation matrix based on the current rotation along the y-axis (left and right)
        Quaternion rotation = Quaternion.Euler(0, cam.transform.localEulerAngles.y, 0);

        if (!physicsOn) {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");
            float moveY = 0f;

            // check for upward movement
            if (Input.GetKey(KeyCode.Space)){
                moveY += upDownSpeed * Time.deltaTime;
            }

            // check for downward movement
            if (Input.GetKey(KeyCode.LeftShift)){
                moveY -= upDownSpeed * Time.deltaTime;
            }

            // move forward and right based on the direction the player is facing on the xz plane
            Vector3 moveDirection = rotation * (new Vector3(moveX, moveY, moveZ));
            moveDirection = moveDirection.normalized * moveSpeed * 5 * Time.deltaTime;
            transform.position += moveDirection;

        } else { // zero intersections
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 moveDirection = rotation * (new Vector3(moveX, 0f, moveZ));
            moveDirection = moveDirection.normalized * moveSpeed * 2 * Time.deltaTime;
            Vector3 newPos = transform.position + moveDirection;

            Vector3 old_head = transform.position + new Vector3(0,1,0);
            Vector3 old_tail = old_head - new Vector3(0, 2, 0);

            Vector3 head = newPos + new Vector3(0,1,0);
            Vector3 tail = head - new Vector3(0, 2, 0);

            instantiatedSphere.GetComponent<Transform>().position = tail;
            // Debug.Log("yooooo1");
            Vector3[] posPts = getCollisionPoints(head, tail);
            Vector3[] oldPosPts = getCollisionPoints(old_head, old_tail);

            if (startingSnap) {
                Debug.Log("snapping to " + posPts[2].y);
                newPos.y = posPts[3].y + 1;
                transform.position = newPos;
                startingSnap = false;
            } else if (posPts[1] != Vector3.zero) {
                Debug.Log("midbumped");
                if (climbPoint == Vector3.zero) {climbPoint = posPts[1];}
                if (oldPosPts[0] != Vector3.zero && Mathf.Abs(climbPoint.y - oldPosPts[0].y) > .2) {maxClimb = oldPosPts[0];}
                if (oldPosPts[2] != Vector3.zero && Mathf.Abs(climbPoint.y - oldPosPts[0].y) > .2) {minClimb = oldPosPts[2];}
                Vector3 propPos = new Vector3(transform.position.x, transform.position.y + (moveZ == 0 ? 0 : Mathf.Sign(moveZ)) * moveSpeed * Time.deltaTime, transform.position.z);
                bool wontHitCeil = (maxClimb == Vector3.zero || propPos.y + 1 < maxClimb.y);
                bool wontHitFloor = (minClimb == Vector3.zero || propPos.y - 1 > minClimb.y);
                bool wontExceedClimbPt = (propPos.y - 1.05 < climbPoint.y && propPos.y + 1.05 > climbPoint.y);
                if (wontHitCeil && wontHitFloor && wontExceedClimbPt) {
                    transform.position = propPos;
                }
            } else if (posPts[0] != Vector3.zero) {
                if (posPts[2] != Vector3.zero) {return;}
                Debug.Log("head bump");
                newPos.y = posPts[0].y - 1;
                transform.position = newPos;
                climbPoint = minClimb = maxClimb = Vector3.zero;
            } else if (posPts[2] != Vector3.zero) {
                // Debug.Log("tail bump");
                newPos.y = posPts[2].y + 1;
                transform.position = newPos;
                climbPoint = minClimb = maxClimb = Vector3.zero;
            } else { // needs to support empty space
                if (climbPoint == Vector3.zero) { 
                    if (oldPosPts[0] != Vector3.zero) {climbPoint = oldPosPts[0];}
                    if (oldPosPts[2] != Vector3.zero) {climbPoint = oldPosPts[2];}
                } 
                if (posPts[0] != Vector3.zero && Mathf.Abs(climbPoint.y - posPts[0].y) > .2) {maxClimb = posPts[0];}
                if (posPts[2] != Vector3.zero && Mathf.Abs(climbPoint.y - posPts[2].y) > .2) {minClimb = posPts[2];}
                Vector3 propPos = new Vector3(transform.position.x, transform.position.y + (moveZ == 0 ? 0 : Mathf.Sign(moveZ)) * moveSpeed * Time.deltaTime, transform.position.z);
                bool wontHitCeil = (maxClimb == Vector3.zero || propPos.y + .9 < maxClimb.y);
                bool wontHitFloor = (minClimb == Vector3.zero || propPos.y - .9 > minClimb.y);
                bool wontExceedClimbPt = (propPos.y - 1.1 < climbPoint.y && propPos.y + 1.1 > climbPoint.y);
                if (wontHitCeil && wontHitFloor && wontExceedClimbPt) {
                    transform.position = propPos;
                }
            }
            startingSnap = false;
            // physicsOn = !physicsOn;
        }

        Vector3 newChunk = new Vector3(0, 0, 0);
        bool chunkChanged = false;

        for (int i = 0; i < 6; i++) {
            float dotProd = Vector3.Dot(icos[i], playerBody.position);
            basisOffset[i] = dotProd; 
        }

        for (int i = 0; i < 3; i++) {
            float dotProd = Vector3.Dot(icos[i], playerBody.position);
            basisOffset[i] = dotProd;
            newChunk[i] = (int)((dotProd - (Mathf.Sign(dotProd) * 4))/8);
            newChunk[i] = (int)(playerBody.position[i]/8);
            if (newChunk[i] != currChunk[i]) {
                chunkChanged = true;
            }
        }
        if (chunkChanged) {
            currChunk = newChunk;
            
            // script1.genChunk(newChunk);
        }
    }

    // this returns an array with 3 values: head collision, mid collision, tail collision. Will be zero if none are found 
    Vector3[] getCollisionPoints(Vector3 head, Vector3 tail) {
        // Debug.Log("getting collisions");
        Vector3[] to_return = new Vector3[4];
        // get intersections with mesh
        MeshCollider collider = script1.terrainObj.GetComponent<MeshCollider>();
        
        Vector3 direction_down = new Vector3(0, -2, 0);
        Vector3 direction_up = new Vector3(0, 1, 0);

        // Cast a ray from head in the direction of the line segment
        Ray head_ray = new Ray(head, direction_down);
        Ray up_ray = new Ray(tail, direction_up);
        RaycastHit[] down_hits = Physics.RaycastAll(head_ray);
        RaycastHit[] up_hits = Physics.RaycastAll(up_ray);

        foreach (RaycastHit hit in down_hits) {
            if (hit.collider != collider) {continue;}
            if (head.y - hit.point.y < .1 ) {
                to_return[0] = hit.point;
            } else if (Mathf.Abs(hit.point.y - tail.y) < .1) {
                to_return[2] = hit.point;
            } else if (head.y - hit.point.y < 2) {
                to_return[1] = hit.point;
            } else {
                to_return[3] = hit.point;
            }
        }

        foreach (RaycastHit hit in up_hits) {
            if (hit.collider != collider) {continue;}
            if (hit.point.y - tail.y < .1 ) {
                to_return[2] = hit.point;
            } else if (Mathf.Abs(hit.point.y - head.y) < .1) {
                to_return[0] = hit.point;
            } else if (hit.point.y - tail.y < 2) {
                to_return[1] = hit.point;
            }
        }
        return to_return;

    }
}
