using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerGameManager : MonoBehaviour
{
    VoxelEngine vEngine;
    VoxelServer vServer;
    ServerPlayerManager pMananger;

    public float RoundTime;

    public bool inStartTime = false;

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
        StartCoroutine(startDelay());
    }

    public void EndRound(bool HumansWin)
    {
        if(HumansWin)
        {

        }
        else
        {

        }

    }

    private void Update()
    {
        if(RoundTime > 0)
        {
            RoundTime -= Time.deltaTime;
        }
        else if(RoundTime <= 0)
        {
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
        }
        
    }
}
