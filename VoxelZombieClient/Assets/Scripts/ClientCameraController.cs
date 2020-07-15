using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientCameraController : MonoBehaviour
{
    public Transform LocalPlayerSim;
    public float minimumX = -60f;
    public float maximumX = 60f;

    public float minimumY = -360f;
    public float maximumY = 360f;

    public float sensitivityX = 5f;
    public float sensitivityY = 5f;

    private float rotationY = 0f;
    private float rotationX = 0f;


    Camera playerCam;

    // Start is called before the first frame update
    void Start()
    {
        playerCam = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

    }

    // Update is called once per frame
    void Update()
    {
        CameraLook();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;      
        }
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;         
        }
    }

    private void LateUpdate()
    {
        
        float clientError = Vector3.Distance(transform.position, LocalPlayerSim.position);
        if(clientError > 2 || clientError < 0.05f)
        {

            transform.position = LocalPlayerSim.position;    
         
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, LocalPlayerSim.position, .5f);
        }
        
    }

    void CameraLook()
    {
        rotationY += Input.GetAxis("Mouse X") * sensitivityX;
        rotationX += Input.GetAxis("Mouse Y") * sensitivityY;

        rotationX = Mathf.Clamp(rotationX, minimumX, maximumX);


        playerCam.transform.localEulerAngles = new Vector3(-rotationX, rotationY, 0); 

    }
}
