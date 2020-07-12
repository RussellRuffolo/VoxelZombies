using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class ClientPlayerController : MonoBehaviour
    {

        public float playerSpeed;
        public float jumpSpeed;
        public float gravAcceleration;
        public float verticalWaterSpeed;
        public float horizontalWaterSpeed;

        Camera playerCam;

        public float minimumX = -60f;
        public float maximumX = 60f;

        public float minimumY = -360f;
        public float maximumY = 360f;

        public float sensitivityX = 5f;
        public float sensitivityY = 5f;

        private float rotationY = 0f;
        private float rotationX = 0f;

        private Vector3 lastMoveVector = Vector3.zero;
        private bool lastJump = false;

        public ushort moveState = 0;

        VoxelClient vClient;
        ClientChatManager chatClient;
        HalfBlockDetector hbDetector;
        ClientPositionTracker pTracker;

        List<ClientInputs> LoggedInputs = new List<ClientInputs>();

        private void Awake()
        {
            vClient = GameObject.FindGameObjectWithTag("Network").GetComponent<VoxelClient>();
            chatClient = GameObject.FindGameObjectWithTag("Network").GetComponent<ClientChatManager>();
            hbDetector = GetComponent<HalfBlockDetector>();
            pTracker = GetComponent<ClientPositionTracker>();
            LoggedInputs.Add(new ClientInputs(Vector3.zero, false, 0));
        }

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
        

        }

        private void FixedUpdate()
        {
            MovementInput();
        }

        void CameraLook()
        {
            rotationY += Input.GetAxis("Mouse X") * sensitivityX;
            rotationX += Input.GetAxis("Mouse Y") * sensitivityY;

            rotationX = Mathf.Clamp(rotationX, minimumX, maximumX);


            playerCam.transform.localEulerAngles = new Vector3(-rotationX, 0, 0);
            transform.eulerAngles = new Vector3(0, rotationY, 0);

        }

        void MovementInput()
        {
            Vector3 playerForward = new Vector3(transform.forward.x, 0, transform.forward.z);
            Vector3 playerRight = Quaternion.AngleAxis(90, Vector3.up) * playerForward;
            Vector3 speedVector = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                speedVector += new Vector3(playerForward.x, 0, playerForward.z);
            }
            if (Input.GetKey(KeyCode.D))
            {
                speedVector += new Vector3(playerRight.x, 0, playerRight.z);
            }
            if (Input.GetKey(KeyCode.A))
            {
                speedVector -= new Vector3(playerRight.x, 0, playerRight.z);
            }
            if (Input.GetKey(KeyCode.S))
            {
                speedVector -= new Vector3(playerForward.x, 0, playerForward.z);
            }


            bool jump = Input.GetKey(KeyCode.Space);

            //can't move or jump if chatting
            if (chatClient.chatEnabled)
            {
                speedVector = Vector3.zero;
                jump = false;
            }

            //run inputs here
            Rigidbody playerRB = GetComponent<Rigidbody>();

            float yVel = playerRB.velocity.y;

            if (moveState == 0) //normal movement
            {
                bool onGround = hbDetector.CheckGrounded();

                if (onGround)
                {
                    if (jump)
                    {
                        yVel = jumpSpeed;
                    }

                }
                else
                {
                    yVel -= gravAcceleration * Time.deltaTime;
                }

                playerRB.velocity = speedVector * playerSpeed;
                playerRB.velocity += yVel * Vector3.up;

               

            }
            else if (moveState == 1) //water movement
            {
                if (jump)
                {
                    yVel = verticalWaterSpeed;
                }
                else
                {
                    yVel = -verticalWaterSpeed;
                }

                playerRB.velocity = speedVector * horizontalWaterSpeed;
                playerRB.velocity += yVel * Vector3.up;
            }
            //if inputs have changed send the updated values to the server
            if (speedVector != lastMoveVector || jump != lastJump)
            {
                float inputTimeStamp = Time.time;
                vClient.SendInputs(speedVector, jump, inputTimeStamp);
                LoggedInputs.Add(new ClientInputs(speedVector, jump, inputTimeStamp));
                lastMoveVector = speedVector;
                lastJump = jump;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            
        }

        public void ClientPrediction(Vector3 serverPosition, float ClientTimeStamp, float ServerTimeDelta, Vector3 velocity)
        {
            Vector3 currentPosition = transform.position;

            ClientInputs currentInputs = GetCurrentInputs(ClientTimeStamp);

            float simulTime = ClientTimeStamp + ServerTimeDelta;

            Rigidbody playerRB = GetComponent<Rigidbody>();
            playerRB.velocity = velocity;
            transform.position = serverPosition;

            ushort simulMoveState = moveState;

            Physics.autoSimulation = false;
            while (simulTime < Time.time)
            {
                float yVel = playerRB.velocity.y;
                simulMoveState = pTracker.CheckPlayerState(simulMoveState);

                //need to calculate new movestate for each simul tick
                if (simulMoveState == 0) //normal movement
                {
                    bool onGround = hbDetector.CheckGrounded();

                    if (onGround)
                    {
                        if (currentInputs.Jump)
                        {
                            yVel = jumpSpeed;
                        }

                    }
                    else
                    {
                        yVel -= gravAcceleration * Time.fixedDeltaTime;
                    }

                    playerRB.velocity = currentInputs.MoveVector * playerSpeed;
                    playerRB.velocity += yVel * Vector3.up;


                }
                else if (moveState == 1) //water movement
                {
                    if (currentInputs.Jump)
                    {
                        yVel = verticalWaterSpeed;
                    }
                    else
                    {
                        yVel = -verticalWaterSpeed;
                    }

                    playerRB.velocity = currentInputs.MoveVector * horizontalWaterSpeed;
                    playerRB.velocity += yVel * Vector3.up;
                }

                Physics.Simulate(Time.fixedDeltaTime);
                simulTime += Time.fixedDeltaTime;
                currentInputs = GetCurrentInputs(simulTime);
            }
            Physics.autoSimulation = true;

            if(Vector3.Distance(currentPosition, transform.position) > .01f)
            {
                Debug.Log("Found error: " + Vector3.Distance(currentPosition, transform.position));
             
            }

         

        }

        public ClientInputs GetCurrentInputs(float OldestTimeStamp)
        {
            ClientInputs currentInput = new ClientInputs(Vector3.zero,false,0);
            int removeCount = 0;

            for(int i = 0; i < LoggedInputs.Count; i++)
            {
                if(LoggedInputs[i].TimeStamp < OldestTimeStamp)
                {
                    currentInput = LoggedInputs[i];
                    removeCount++;
                }
                else
                {
                    break;
                }

            }

            LoggedInputs.RemoveRange(0, removeCount);

            return currentInput;
        }
    }

    public class ClientInputs
    {
        public Vector3 MoveVector;
        public bool Jump;
        public float TimeStamp;

        public ClientInputs(Vector3 moveVector, bool jump, float timeStamp)
        {
            MoveVector = moveVector;
            Jump = jump;
            TimeStamp = timeStamp;
        }
    }

}

