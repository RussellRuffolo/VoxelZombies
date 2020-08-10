using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

public class ServerPlayerManager : MonoBehaviour
{
    public GameObject PlayerPrefab;

    public Dictionary<ushort, Transform> PlayerDictionary = new Dictionary<ushort, Transform>();
    public Dictionary<ushort, PlayerInputs> InputDictionary = new Dictionary<ushort, PlayerInputs>();

    public Dictionary<ushort, int> TickDic = new Dictionary<ushort, int>();
    public Dictionary<ushort, Vector3> PlayerVelocities = new Dictionary<ushort, Vector3>();

    public float PlayerSpeed;
    public float AirAcceleration;
    public float JumpSpeed;
    public float gravAcceleration;

    public float horizontalWaterSpeed;
    public float verticalWaterMaxSpeed;
    public float verticalWaterAcceleration;

    public float waterExitSpeed;

    private int serverTickNumber = 0;

    VoxelServer vServer;
    VoxelEngine vEngine;


    private void Awake()
    {
        vServer = GetComponent<VoxelServer>();
        vEngine = GetComponent<VoxelEngine>();
    }

    public void AddPlayer(ushort PlayerID, ushort stateTag, float xPos, float yPos, float zPos)
    {
        Vector3 spawnPosition = new Vector3((float)xPos, (float)yPos, (float)zPos);

        GameObject newPlayer = GameObject.Instantiate(PlayerPrefab, spawnPosition, Quaternion.identity);
        newPlayer.GetComponent<ServerPositionTracker>().ID = PlayerID;
        newPlayer.GetComponent<ServerPositionTracker>().stateTag = stateTag;
        PlayerDictionary.Add(PlayerID, newPlayer.transform);
        InputDictionary.Add(PlayerID, new PlayerInputs(Vector3.zero, false));
        TickDic.Add(PlayerID, -1);
        PlayerVelocities.Add(PlayerID, Vector3.zero);

        
    }

    public void RemovePlayer(ushort PlayerID)
    {
        GameObject toDestroy = PlayerDictionary[PlayerID].gameObject;
        PlayerDictionary.Remove(PlayerID);
        InputDictionary.Remove(PlayerID);
        Destroy(toDestroy);
    }

    public void ReceiveInputs(MessageReceivedEventArgs e)
    {
        using (DarkRiftReader reader = e.GetMessage().GetReader())
        {
            ushort clientID = e.Client.ID;
            Transform targetPlayer = PlayerDictionary[clientID];
            Rigidbody playerRB = targetPlayer.GetComponent<Rigidbody>();

            //allow this player to be moved and reassign its velocity
            //having all players be kinematic allows each one to be simulated 
            //seperately as inputs arrive
            playerRB.isKinematic = false;
            playerRB.velocity = PlayerVelocities[clientID];

            //number of inputs received from the client
            int numInputs = reader.ReadInt32();
      
            bool appliedInput = false;
            for(int i = 0; i < numInputs; i++)
            {
                Vector3 moveVector = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                bool Jump = reader.ReadBoolean();

                int clientTickNum = reader.ReadInt32();

                //If this input is from a later tick than the most recent
                //Simulate the tick on the player with the clients inputs
                if(clientTickNum > TickDic[clientID])
                {
                    appliedInput = true;

                    //apply the clients inputs to change the player velocity
                    ApplyInputs(clientID, new PlayerInputs(moveVector, Jump));

                    //simulate one tick
                    if(!vEngine.loadingMap)
                    {
                        Physics.Simulate(Time.fixedDeltaTime);
                    }
                    else
                    {
                        Debug.Log("Skipped tick due to map loading");
                    }
                  

                    //update the clients most recent tick
                    TickDic[clientID]++;
                }
            }
            if(appliedInput)
            {
                //send the client a state update with the corresponding tick
                vServer.SendPositionUpdate(clientID, targetPlayer.position, TickDic[clientID], playerRB.velocity);
            }
            //store the players velocity and remove it from simulation
           PlayerVelocities[clientID] = playerRB.velocity;
            playerRB.isKinematic = true;
            
        }
    }

    private void ApplyInputs(ushort id, PlayerInputs inputs)
    {
        Transform playerTransform = PlayerDictionary[id];
        Rigidbody playerRB = playerTransform.GetComponent<Rigidbody>();

        float yVel = playerRB.velocity.y;
        Vector3 horizontalSpeed = new Vector3(playerRB.velocity.x, 0, playerRB.velocity.z);
        inputs.moveState = playerTransform.GetComponent<ServerPositionTracker>().CheckPlayerState();
        if (inputs.moveState == 0) //normal movement
        {
            bool onGround = playerTransform.GetComponent<HalfBlockDetector>().CheckGrounded(); 

            if (onGround && yVel <= 0)
            {

                if (inputs.Jump)
                {
                    horizontalSpeed = inputs.moveVector.normalized * PlayerSpeed;
                    yVel = JumpSpeed;
                }
                else
                {
                    horizontalSpeed = inputs.moveVector.normalized * PlayerSpeed;
                }

            }
            else
            {
                horizontalSpeed += inputs.moveVector.normalized * AirAcceleration * Time.fixedDeltaTime;

                if(horizontalSpeed.magnitude > PlayerSpeed)
                {
                    horizontalSpeed = inputs.moveVector.normalized * PlayerSpeed;
                }
                yVel -= gravAcceleration * Time.fixedDeltaTime;
            }

            playerRB.velocity = horizontalSpeed;
            playerRB.velocity += yVel * Vector3.up;

        }
        else if (inputs.moveState == 1) //water movement
        {
            if (inputs.Jump)
            {
                if(yVel >= verticalWaterMaxSpeed)
                {
                    yVel = verticalWaterMaxSpeed;
                }
                else
                {
                    yVel += verticalWaterAcceleration * Time.fixedDeltaTime;
                }
            }
            else
            {
                if(yVel < -verticalWaterMaxSpeed)
                {
                    yVel += verticalWaterAcceleration * Time.fixedDeltaTime;
                    if(yVel > -verticalWaterMaxSpeed)
                    {
                        yVel = -verticalWaterMaxSpeed;
                    }
                }
                else
                {
                    yVel -= verticalWaterAcceleration * Time.fixedDeltaTime;
                    if(yVel < -verticalWaterMaxSpeed)
                    {
                        yVel = -verticalWaterMaxSpeed;
                    }
                }
            }

            playerRB.velocity = inputs.moveVector * horizontalWaterSpeed;
            playerRB.velocity += yVel * Vector3.up;
        }
        else if(inputs.moveState == 3) //exiting water
        {
            if(inputs.Jump && playerTransform.GetComponent<ServerPositionTracker>().CheckWaterJump())
            {
                Debug.Log("Water Jump");
                playerTransform.GetComponent<ServerPositionTracker>().UseWaterJump();
                Vector3 waterJump = new Vector3(inputs.moveVector.x * horizontalWaterSpeed, waterExitSpeed, inputs.moveVector.z * horizontalWaterSpeed);
                playerRB.velocity = waterJump;
            }
            else
            {
                yVel -= gravAcceleration * Time.fixedDeltaTime;
                playerRB.velocity = inputs.moveVector * PlayerSpeed;
                playerRB.velocity += yVel * Vector3.up;
            }
          
        }

        playerTransform.GetComponent<HalfBlockDetector>().CheckSteps();

    }

}

public class PlayerInputs
{
    public Vector3 moveVector;
    public bool Jump;
    public int ClientTickNumber;
    public int ServerTickNumber;

    //0 is normal, 1 is water, 2 is lava
    public ushort moveState;

    public PlayerInputs(Vector3 moveVec, bool jump)
    {
        moveVector = moveVec;
        Jump = jump;
        moveState = 0;
        ClientTickNumber = 0;
        ServerTickNumber = 0;

    }
}
