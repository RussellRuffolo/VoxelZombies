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
    public float JumpSpeed;
    public float gravAcceleration;

    public float horizontalWaterSpeed;
    public float verticalWaterSpeed;

    private int serverTickNumber = 0;

    VoxelServer vServer;


    private void Awake()
    {
        vServer = GetComponent<VoxelServer>();
    }

    public void AddPlayer(ushort PlayerID, ushort stateTag, int xPos, int yPos, int zPos)
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

            playerRB.isKinematic = false;
            playerRB.velocity = PlayerVelocities[clientID];

            int numInputs = reader.ReadInt32();
            bool appliedInput = false;
            for(int i = 0; i < numInputs; i++)
            {
                Vector3 moveVector = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                bool Jump = reader.ReadBoolean();

                int clientTickNum = reader.ReadInt32();

                //make sure client sends zero at some point
                if(clientTickNum > TickDic[clientID])
                {
                    appliedInput = true;
                    ApplyInputs(clientID, new PlayerInputs(moveVector, Jump));

                    Physics.Simulate(Time.fixedDeltaTime);

                    TickDic[clientID]++;
                }

            }

            if(appliedInput)
            {
                vServer.SendPositionUpdate(clientID, targetPlayer.position, TickDic[clientID], playerRB.velocity);
            }

           PlayerVelocities[clientID] = playerRB.velocity;
            playerRB.isKinematic = true;
            
        }
    }

    private void ApplyInputs(ushort id, PlayerInputs inputs)
    {
        Transform playerTransform = PlayerDictionary[id];
        Rigidbody playerRB = playerTransform.GetComponent<Rigidbody>();

        float yVel = playerRB.velocity.y;
        inputs.moveState = playerTransform.GetComponent<ServerPositionTracker>().CheckPlayerState();
        if (inputs.moveState == 0) //normal movement
        {
            bool onGround = playerTransform.GetComponent<HalfBlockDetector>().CheckGrounded(); 

            if (onGround)
            {

                if (inputs.Jump)
                {
                    yVel = JumpSpeed;
                }

            }
            else
            {
                yVel -= gravAcceleration * Time.fixedDeltaTime;
            }

            playerRB.velocity = inputs.moveVector * PlayerSpeed;
            playerRB.velocity += yVel * Vector3.up;

        }
        else if (inputs.moveState == 1) //water movement
        {
            if (inputs.Jump)
            {
                yVel = verticalWaterSpeed;
            }
            else
            {
                yVel = -verticalWaterSpeed;
            }

            playerRB.velocity = inputs.moveVector * horizontalWaterSpeed;
            playerRB.velocity += yVel * Vector3.up;
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
