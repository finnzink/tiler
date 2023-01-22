using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // adjust as desired
    public float mouseSensitivity = 100f; // adjust as desired
    public float upDownRange = 80.0f; // adjust as desired
    public float upDownSpeed = 2f; // adjust as desired
    public Transform playerBody;
    float rotX = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
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
    }
}
