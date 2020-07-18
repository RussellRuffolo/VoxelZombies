using System.Collections;
using System.Collections.Generic;
using fNbt;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientVoxelEngine : MonoBehaviour
{
    public World world = new World();
    public List<Material> materialList;

    public BoundaryController bController;

    public int Length, Width, Height;

    private void Awake()
    {
        foreach(Material mat in materialList)
        {
            mat.SetFloat("_Glossiness", 0);
        }

        if(SceneManager.GetActiveScene().name == "LoginScene")
        {
            LoadMap("zombiecity");
        }
    }

    public void LoadMap(string mapName)
    {

        if(world.Chunks.Count != 0)
        {
            UnloadMap();
        }

        var mapFile = new NbtFile();
        if(Application.platform == RuntimePlatform.OSXPlayer)
        {
            mapFile.LoadFromFile(Application.dataPath + "/Resources" + "/Data" + "/StreamingAssets/" + mapName + ".schematic");
        }
        else if(Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            mapFile.LoadFromFile(Application.streamingAssetsPath + "\\" + mapName + ".schematic");
        }
      

        NbtCompound mapCompoundTag = mapFile.RootTag;

        short length = mapCompoundTag["Length"].ShortValue;
        short width = mapCompoundTag["Width"].ShortValue;
        short height = mapCompoundTag["Height"].ShortValue;

         Length = length;
         Width = width;
         Height = height;

        bController.SetMapBoundaries(Length, Width, Height);

        byte[] mapBytes = mapCompoundTag["Blocks"].ByteArrayValue;
        byte[] dataBytes = mapCompoundTag["Data"].ByteArrayValue;


        string namePrefix = "Chunk ";

        for (int z = 0; z < Width / 16; z++)
        {
            for (int x = 0; x < Length / 16; x++)
            {
                for (int y = 0; y < Height / 16; y++)
                {
                    var newChunkObj = new GameObject(namePrefix + x.ToString() + "," + y.ToString() + "," + z.ToString());
                    newChunkObj.transform.position = new Vector3(x * 16, y * 16, z * 16);


                    var chunk = newChunkObj.AddComponent<Chunk>();
                    chunk.world = world;
                    chunk.GetComponent<MeshRenderer>().materials = materialList.ToArray();
                    ChunkID newID = new ChunkID(x, y, z);
                    world.Chunks.Add(newID, chunk);
                    chunk.ID = newID;
                }
            }
        }

        int blockCount = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Length; x++)
            {
                for (int z = 0; z < Width; z++)
                {
                    byte toAdd = mapBytes[blockCount];
                    if (toAdd == 35)
                    {
                        switch (dataBytes[blockCount])
                        {
                            //ID - 1 == Material List Index
                            case 0:
                                toAdd = 36; //0 -> White
                                break;
                            case 1:
                                toAdd = 22; //1 -> Orange
                                break;
                            case 2:
                                toAdd = 32; //THIS IS WRONG
                                break;
                            case 3:
                                toAdd = 22; //THIS IS WRONG
                                break;
                            case 4:
                                toAdd = 23; //4 -> Yellow
                                break;
                            case 5:
                                toAdd = 26;
                                break;
                            case 6:
                                toAdd = 33;
                                break;
                            case 7:
                                toAdd = 27;
                                break;
                            case 8:
                                toAdd = 35; //8 -> Light Gray
                                break;
                            case 9:
                                toAdd = 28; //check
                                break;
                            case 10:
                                toAdd = 30;
                                break;
                            case 11:
                                toAdd = 29; //11 -> Ultramarine
                                break;
                            case 12:
                                toAdd = 32;
                                break;
                            case 13:
                                toAdd = 33;
                                break;
                            case 14:
                                toAdd = 21; //14 -> Red
                                break;
                            case 15:
                                toAdd = 34; //15 -> DarkGray
                                break;

                        }
                        mapBytes[blockCount] = toAdd;
                    }

                    world[x, y, z] = toAdd;
                    blockCount++;
                }
            }
        }

    }

    public void UnloadMap()
    {
        foreach (Chunk toDestroy in world.Chunks.Values)
        {
            Destroy(toDestroy.gameObject);
        }
        world.Chunks.Clear();
    }
}
