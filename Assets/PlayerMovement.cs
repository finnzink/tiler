using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public GameObject terrainGenator;
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

    private Rigidbody rb;
    private bool isGrounded;
    private bool startingSnap;

    void Start()
    {
        startingSnap = true;
        script1 = terrainGenator.GetComponent<PenroseGenerator>();
        moveSpeed = 2f;
        cam = GameObject.Find("Player Camera");
        if (cam == null) {
            Debug.Log("can't find camera");
        }
        // rb = GetComponent<Rigidbody>();

        // PhysicMaterial playerMaterial = new PhysicMaterial();
        // playerMaterial.staticFriction = 1.0f;
        // playerMaterial.dynamicFriction = 0.1f;
        // playerMaterial.bounciness = 0.0f;
        // Collider collider = GetComponent<Collider>();
        // collider.material = playerMaterial;

        // rb.constraints = RigidbodyConstraints.FreezeRotation;
        // rb.isKinematic = true;
        // isGrounded = true;
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

    void OnCollisionEnter(Collision collision) {
        // Set isGrounded to true if the player collides with the ground
        // if (collision.gameObject.CompareTag("Ground")) {
            isGrounded = true;
        // }
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

        } else {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            Vector3 moveDirection = rotation * (new Vector3(moveX, 0f, moveZ));
            moveDirection = moveDirection.normalized * moveSpeed * 5 * Time.deltaTime;
            Vector3 newPos = transform.position + moveDirection;

            Vector3 head = newPos;
            Vector3 tail = head - new Vector3(0, -2, 0);

            // Debug.Log(head);

            // get intersections with mesh
            MeshCollider collider = script1.terrainObj.GetComponent<MeshCollider>();
            
            if (collider == null) {
                Debug.Log("collider not set correctly");
            }
            
            Vector3 direction_down = new Vector3(0, -2, 0);
            Vector3 direction_up = new Vector3(0, 1, 0);

            // Cast a ray from head in the direction of the line segment
            Ray head_ray = new Ray(head, direction_down);
            Ray up_ray = new Ray(head, direction_up);
            RaycastHit[] down_hits = Physics.RaycastAll(head_ray);
            RaycastHit[] up_hits = Physics.RaycastAll(head_ray);

            // Find all intersection points with the collider
            List<Vector3> inner_intersections = new List<Vector3>();
            Vector3 above_head = new Vector3(); 
            Vector3 below_head = new Vector3();
            foreach (RaycastHit hit in down_hits) {
                if (hit.collider == collider && head.y - hit.point.y < 2 ) {
                    // This hit is with the desired collider, so add the intersection point to the list
                    inner_intersections.Add(hit.point);
                }
                else if (hit.collider == collider) {
                    below_head = hit.point;
                    break;
                }
            }

            if (up_hits.Length > 0) {
                above_head = up_hits[0].point;
            }
            
            // the below is temporary, just a waypoint
            if (inner_intersections.Count > 0) { // 
                newPos.y = inner_intersections[inner_intersections.Count - 1].y + 1;
                transform.position = newPos;

            } else if (below_head != Vector3.zero && (startingSnap || tail.y - below_head.y < .1)){ // second part is to check for cliff
                newPos.y = below_head.y + 1;
                transform.position = newPos;

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
}
