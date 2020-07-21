using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using fNbt;
using System.IO;

public class VoxelEngine : MonoBehaviour
{
    public World world = new World();
    public List<Material> materialList;

    public byte[] mapBytes;

    public List<MapData> mapList = new List<MapData>();
    public MapData currentMap;

   

    public BoundaryController bController;
    public WaterEngine wEngine;


  
    const ushort MAP_TAG = 4;
    private void Awake()
    {

        MapData excitebike = new MapData("excitebike", 24, 34, 36);
        MapData dwarves = new MapData("dwarves", 122, 2, 7);
        mapList.Add(dwarves);

        MapData diametric = new MapData("diametric", 46, 19, 26);
        mapList.Add(diametric);

        MapData Asylum = new MapData("asylum",  25, 129, 30);
        mapList.Add(Asylum);

        MapData Carson = new MapData("carson",  10, 35, 120);
        mapList.Add(Carson);    

        MapData Sunspots = new MapData("Sunspots",  60, 112, 108);
        mapList.Add(Sunspots);

        MapData hawaii = new MapData("hawaii",  1, 67, 43);
        mapList.Add(hawaii);

        MapData colony = new MapData("colony", 56, 67, 8);
        mapList.Add(colony);

        MapData italy = new MapData("italy",  53, 89, 63);
        mapList.Add(italy);
        MapData swiss = new MapData("swiss",  29, 50, 12);
        mapList.Add(swiss);

        mapList.Add(hawaii);
    }

    public void LoadMap(MapData map)
    {      
        UnloadMap();

        currentMap = map;
        
        var mapFile = new NbtFile();
        mapFile.LoadFromFile(Application.dataPath + "/StreamingAssets/" + map.Name + ".schematic");

        NbtCompound mapCompoundTag = mapFile.RootTag;

        short length = mapCompoundTag["Length"].ShortValue;
        short width = mapCompoundTag["Width"].ShortValue;
        short height = mapCompoundTag["Height"].ShortValue;

        map.Length = length;
        map.Width = width;
        map.Height = height;

        bController.SetMapBoundaries(map.Length, map.Width, map.Height);

        mapBytes = mapCompoundTag["Blocks"].ByteArrayValue;
        byte[] dataBytes = mapCompoundTag["Data"].ByteArrayValue;            
       

        string namePrefix = "Chunk ";
        
        for (int z = 0; z < width / 16; z++)
        {
            for (int x = 0; x < length / 16; x++)
            {
                for (int y = 0; y < height / 16; y++)
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
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < length; x++)
            {
                for (int z = 0; z < width; z++)
                {
                    byte toAdd = mapBytes[blockCount];
                    if(toAdd == 35)
                    {
                        switch(dataBytes[blockCount])
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
       // wEngine.RenderWater();
    }
    
    public  void UnloadMap()
    {
        foreach(Chunk toDestroy in world.Chunks.Values)
        {
            Destroy(toDestroy.gameObject);
        }
        world.Chunks.Clear();
    }

    //returns a map other than current map
    public MapData GetRandomMap()
    {
        int mapIndex = Random.Range(0, mapList.Count);
        if(currentMap != mapList[mapIndex])
        {
            return mapList[mapIndex];
        }
        else
        {
            return GetRandomMap();
        }
    }

    
}



public class MapData
{
    public string Name;


    public int SpawnX;
    public int SpawnY;
    public int SpawnZ;

    public int Length;
    public int Width;
    public int Height;

    public MapData(string name, int spawnX, int spawnY, int spawnZ)
    {
        Name = name;
      

        SpawnX = spawnX;
        SpawnY = spawnY;
        SpawnZ = spawnZ;
    }
 }

public class VoxelCoordinate
{  

    public int x;
    public int y;
    public int z;

    public VoxelCoordinate(int xPos,  int yPos, int zPos)
    {
        x = xPos;
        y = yPos;
        z = zPos;
    }

    public override bool Equals(object obj)
    {
        if(obj is VoxelCoordinate)
        {
            VoxelCoordinate testObj = (VoxelCoordinate)obj;
            if(testObj.x == x && testObj.y == y && testObj.z == z)
            {
                return true;
            }
        }
        return false;
    }

    public Vector3 WorldPosition()
    {
        return new Vector3(x, y, z);
    }
}
