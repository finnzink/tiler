using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public GameObject terrainGenator;
    private PenroseGenerator script1;
    public List<Vector3> icos = new List<Vector3>();
    public float moveSpeed = 5f; // adjust as desired
    public float mouseSensitivity = 100f; // adjust as desired
    public float upDownRange = 80.0f; // adjust as desired
    public float upDownSpeed = 2f; // adjust as desired
    public Transform playerBody;
    float rotX = 0f;
    public int[] currChunk = new int[6];

    void Start()
    {
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
    }

    void OnGUI()
    {
        Vector3 playerPosition = playerBody.position;
        GUI.backgroundColor = Color.black;
        GUI.Box(new Rect(0, 0, 300, 50), "Position: " + playerPosition.ToString() + "\nChunk: " + string.Join(",", currChunk));
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        float moveY = 0f;

        // check for upward movement
        if (Input.GetKey(KeyCode.Space))
        {
            moveY += upDownSpeed * Time.deltaTime;
        }

        // check for downward movement
        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveY -= upDownSpeed * Time.deltaTime;
        }

        // create a rotation matrix based on the current rotation along the y-axis (left and right)
        Quaternion rotation = Quaternion.Euler(0, transform.localEulerAngles.y, 0);

        // move forward and right based on the direction the player is facing on the xz plane
        Vector3 moveDirection = rotation * (new Vector3(moveX, moveY, moveZ));
        moveDirection = moveDirection.normalized * moveSpeed * Time.deltaTime;
        transform.position += moveDirection;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, -upDownRange, upDownRange);

        transform.localEulerAngles = new Vector3(rotX, transform.localEulerAngles.y, 0);
        transform.Rotate(Vector3.up * mouseX);

        int[] newChunk = new int[6];
        bool chunkChanged = false;

        for (int i = 0; i < 6; i++) {
            newChunk[i] = (int)(Vector3.Dot(icos[i], playerBody.position) / 8);
            if (newChunk[i] != currChunk[i]) {
                chunkChanged = true;
            }
        }
        if (chunkChanged) {
            currChunk = newChunk;
            script1 = terrainGenator.GetComponent<PenroseGenerator>();
            // script1.genChunk(newChunk);
        }
    }
}
