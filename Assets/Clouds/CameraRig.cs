using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [Range(0.1f, 10f)] [SerializeField] float moveSpeed = 3f;
    [Range(0.1f, 1000f)] [SerializeField] float lookSpeed = 100f;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        Translate();
        Rotate();
        RotateCamera();
    }

    void Translate()
    {
        // Left (A) and right (D)
        float x = 0;
        if (Input.GetKey(KeyCode.A))
            x += -moveSpeed;
        if (Input.GetKey(KeyCode.D))
            x += moveSpeed;

        // Forward (W) and backward (S)
        float z = 0;
        if (Input.GetKey(KeyCode.W))
            z += moveSpeed;
        if (Input.GetKey(KeyCode.S))
            z += -moveSpeed;

        // Up (E) and down (Q)
        float y = 0;
        if (Input.GetKey(KeyCode.E))
            y += moveSpeed;
        if (Input.GetKey(KeyCode.Q))
            y += -moveSpeed;

        Vector3 translateVec = new Vector3(x, y, z) * moveSpeed * Time.deltaTime;
        transform.Translate(translateVec);
    }

    void Rotate()
    {
        float y = 0;

        // Mouse
        if (Input.GetMouseButton(0))
            y += Input.GetAxis("Mouse X");

        // Left and right
        if (Input.GetKey(KeyCode.LeftArrow))
            y += -moveSpeed;
        if (Input.GetKey(KeyCode.RightArrow))
            y += moveSpeed;

        y = Mathf.Clamp(y, -1f, 1f);

        Vector3 rotateVec = new Vector3(0, y, 0) * lookSpeed * Time.deltaTime;
        transform.eulerAngles += rotateVec;
    }

    void RotateCamera()
    {
        float x = 0;

        // Mouse
        if (Input.GetMouseButton(0))
            x += -Input.GetAxis("Mouse Y");

        // Up and down
        if (Input.GetKey(KeyCode.UpArrow))
            x += -moveSpeed;
        if (Input.GetKey(KeyCode.DownArrow))
            x += moveSpeed;

        Vector3 rotateVec = new Vector3(x, 0, 0) * lookSpeed * Time.deltaTime;
        cameraTransform.eulerAngles += rotateVec;
    }
}
