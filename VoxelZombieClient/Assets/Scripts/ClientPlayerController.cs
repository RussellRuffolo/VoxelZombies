using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class ClientPlayerController : MonoBehaviour
    {
        public GameObject rotationTracker;

        public float playerSpeed;
        public float jumpSpeed;
        public float gravAcceleration;
        public float verticalWaterSpeed;
        public float horizontalWaterSpeed;


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
        private float lastRotation = 0;

        public ushort moveState = 0;

        VoxelClient vClient;
        ClientChatManager chatClient;
        HalfBlockDetector hbDetector;
        ClientPositionTracker pTracker;

        ClientInputs[] LoggedInputs = new ClientInputs[1024];
        PlayerState[] LoggedStates = new PlayerState[1024];

        private float timer = 0.0f;
        private int tickNumber = 0;

        public Rigidbody playerRB;

        private void Awake()
        {
            playerRB = GetComponent<Rigidbody>();
            vClient = GameObject.FindGameObjectWithTag("Network").GetComponent<VoxelClient>();
            chatClient = GameObject.FindGameObjectWithTag("Network").GetComponent<ClientChatManager>();
            hbDetector = GetComponent<HalfBlockDetector>();
            pTracker = GetComponent<ClientPositionTracker>();
            //LoggedInputs.Add(new ClientInputs(Vector3.zero, false, 0));
            //LoggedStates.Add(new PlayerState(transform.position, playerRB.velocity, 0));
            playerRB = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            GetMouseRotation();
            ClientInputs currentInputs = GetInputs();

            this.timer += Time.deltaTime;
            while (this.timer >= Time.fixedDeltaTime)
            {
                this.timer -= Time.fixedDeltaTime;

                int bufferIndex = tickNumber % 1024;
                // LoggedStates[bufferIndex].position = transform.position;
                //  LoggedStates[bufferIndex].velocity = playerRB.velocity;
                //  LoggedStates[bufferIndex].Tick = tickNumber;

                LoggedStates[bufferIndex] = new PlayerState(transform.position, playerRB.velocity, tickNumber);

                // LoggedInputs[bufferIndex].MoveVector = currentInputs.MoveVector;
                // LoggedInputs[bufferIndex].Jump = currentInputs.Jump;
                // LoggedInputs[bufferIndex].TickNumber = tickNumber;

                LoggedInputs[bufferIndex] = new ClientInputs(currentInputs.MoveVector, currentInputs.Jump, tickNumber);


                ApplyInputs(playerRB, currentInputs);
                

                Physics.Simulate(Time.fixedDeltaTime);

                tickNumber++;
            }

        }   

        void GetMouseRotation()
        {
            rotationY += Input.GetAxis("Mouse X") * sensitivityX;
            rotationTracker.transform.eulerAngles = new Vector3(0, rotationY, 0);
        }


        ClientInputs GetInputs()
        {

            //Vector3 playerForward = new Vector3(transform.forward.x, 0, transform.forward.z);    
            Vector3 playerForward = new Vector3(rotationTracker.transform.forward.x, 0, rotationTracker.transform.forward.z);
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

            int inputTickNumber = tickNumber;
          

            //if inputs have changed send the updated values to the server           
            if (speedVector != lastMoveVector || jump != lastJump)
            {             
                vClient.SendInputs(speedVector, jump, inputTickNumber);              
                lastMoveVector = speedVector;
                lastJump = jump;
            }


            ClientInputs currentInputs = new ClientInputs(speedVector, jump, inputTickNumber);
            return currentInputs;

             
          
        }

        public void ApplyInputs(Rigidbody playerRB, ClientInputs currentInputs)
        {
            //run inputs here        

            float yVel = playerRB.velocity.y;
            moveState = pTracker.CheckPlayerState(moveState);
            if (moveState == 0) //normal movement
            {
                bool onGround = hbDetector.grounded;

                if (onGround)
                {

                    if (currentInputs.Jump)
                    {
                        yVel = jumpSpeed;
                    }

                }
                else
                {
                    yVel -= gravAcceleration * Time.deltaTime;
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

        }

        public void ClientPrediction(Vector3 serverPosition, int ClientTickNumber, int ServerTickDelta, Vector3 serverVelocity)
        {
            //check error between position/velocity at the tick supplied
            int bufferIndex = (ClientTickNumber + ServerTickDelta) % 1024;
            if(LoggedStates[bufferIndex] == null)
            {          
                LoggedStates[bufferIndex] = new PlayerState(serverPosition, serverVelocity, ClientTickNumber + ServerTickDelta);
            }

            Vector3 positionError = LoggedStates[bufferIndex].position - serverPosition;

            if (positionError.sqrMagnitude > 0.00001f)
            {
                // Debug.Log("Found positon error with sqr magnitude: " + positionError.sqrMagnitude + " and " + (tickNumber - (ClientTickNumber + ServerTickDelta)) + " ticks ago");

                //rewind to the given tick and replay to current tick
                transform.position = serverPosition;
                playerRB.velocity = serverVelocity;

                ushort simulMoveState = moveState;

                int rewindTickNumber = ClientTickNumber + ServerTickDelta;
                while (rewindTickNumber < this.tickNumber)
                {
                    bufferIndex = rewindTickNumber % 1024;
                    ClientInputs currentInputs = LoggedInputs[bufferIndex];

                    float yVel = playerRB.velocity.y;

                    simulMoveState = pTracker.CheckPlayerState(simulMoveState);

                    if (simulMoveState == 0) //normal movement
                    {
                        bool onGround = hbDetector.grounded;

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

                    LoggedStates[rewindTickNumber % 1024] = new PlayerState(transform.position, playerRB.velocity, rewindTickNumber);


                    Physics.Simulate(Time.fixedDeltaTime);

                    rewindTickNumber++;

                }
            }
        }
        
        
    }

    public class ClientInputs
    {
        public Vector3 MoveVector;
        public bool Jump;
        public int TickNumber;

        public ClientInputs()
        {
            MoveVector = Vector3.zero;
            Jump = false;
            TickNumber = 0;
        }

        public ClientInputs(Vector3 moveVector, bool jump, int tickNumber)
        {
            MoveVector = moveVector;
            Jump = jump;
            TickNumber = tickNumber;
        }
    }

    public class PlayerState
    {
        public Vector3 position;
        public Vector3 velocity;
        public int Tick;    

        public PlayerState()
        {
            position = Vector3.zero;
            velocity = Vector3.zero;
            Tick = 0;
        }


        public PlayerState(Vector3 pos, Vector3 vel, int tick)
        {
            position = pos;
            velocity = vel;
            Tick = tick;
        }
    }

}

