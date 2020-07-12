using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerGameManager : MonoBehaviour
{
    VoxelEngine vEngine;
    VoxelServer vServer;
    ServerPlayerManager pMananger;

    public float RoundTime;
    public float MinuteCounter;

    public bool inStartTime = false;

    public int map1Votes, map2Votes, map3Votes;

    //TODO- MAP VOTING

    private void Awake()
    {
        vEngine = GetComponent<VoxelEngine>();
        vServer = GetComponent<VoxelServer>();
        pMananger = GetComponent<ServerPlayerManager>();
     
    }

    private void Start()
    {
        StartRound();
    }

    public void StartRound()
    {
        inStartTime = true;
        int newMapIndex = Random.Range(0, vEngine.mapList.Count - 1);
        while(vEngine.currentMap == vEngine.mapList[newMapIndex])
        {
            newMapIndex = Random.Range(0, vEngine.mapList.Count - 1);
        }
        vEngine.LoadMap(vEngine.mapList[newMapIndex]);
        vServer.StartRound();
        RoundTime = 10 * 60;
        MinuteCounter = RoundTime - 60;
        StartCoroutine(startDelay());
    }

    public void EndRound()
    {
        vServer.SendPublicChat("Vote for the next map:", 2);
        MapData map1 = vEngine.GetRandomMap();
        MapData map2 = vEngine.GetRandomMap();
        while(map2 == map1)
        {
            map2 = vEngine.GetRandomMap();
        }
        MapData map3 = vEngine.GetRandomMap();
        while(map3 == map2 || map3 == map1)
        {
            map3 = vEngine.GetRandomMap();
        }

        vServer.SendPublicChat("1: " + map1.Name + " 2: " + map2.Name + " 3: " + map3.Name, 2);

        StartCoroutine(VoteDelay());

    }

    private void Update()
    {
        if(RoundTime > 0)
        {
            RoundTime -= Time.deltaTime;
            if(RoundTime < MinuteCounter)
            {
                vServer.SendPublicChat("There are " + MinuteCounter / 60 + " minutes left in this round.", 2);
                MinuteCounter -= 60;
            }
        }
        else if(RoundTime <= 0)
        {
            vServer.SendPublicChat("Humans win!", 2);
            StartRound();
        }
    }

    public void SubtractTime()
    {
        RoundTime -= 60;
    }

    public void CheckZombieWin()
    {
        foreach(Transform playerTransform in pMananger.PlayerDictionary.Values)
        {
            if(playerTransform.GetComponent<ServerPositionTracker>().stateTag == 0)
            {
                //If a player is a human return- zombies haven't won yet
                return;
            }
        }

        vServer.SendPublicChat("Zombies win!", 2);

        //if no players were human then zombies win
    }

    IEnumerator startDelay()
    {
        yield return new WaitForSeconds(20);
        inStartTime = false;
        ushort firstZombie = vServer.GetRandomPlayer();
        //get random player returns 1000 if no players are connected
        if(firstZombie != 1000)
        {
            vServer.UpdatePlayerState(firstZombie, 1);
            vServer.SendPublicChat("The Infection begins with " + vServer.playerNames[firstZombie] + "!", 2);
        }
        
    }

    IEnumerator VoteDelay()
    {
        yield return new WaitForSeconds(20);
    }
}
