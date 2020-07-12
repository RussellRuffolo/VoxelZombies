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
        MapData EightBit = new MapData("8Bit", 7563, 42, 240, 14);
        mapList.Add(EightBit);

       // MapData ThreeSixty = new MapData("360", 7013, 48, 13, 25);
       // mapList.Add(ThreeSixty);

        //MapData runrunrun = new MapData("runrunrun", 8212, 3, 7, 63);
       // mapList.Add(runrunrun);

        MapData Asylum = new MapData("asylum", 7002, 25, 129, 30);
        mapList.Add(Asylum);

        MapData Carson = new MapData("carson", 26219, 10, 35, 120);
        mapList.Add(Carson);

       // MapData Pandoras_Box = new MapData("pandoras_box", 24293, 2, 67, 64);
      //  mapList.Add(Pandoras_Box);

       // MapData school = new MapData("school", 24293, 122, 68, 68);
       // mapList.Add(school);

       // MapData yggdrasil = new MapData("yggdrasil", 16771, 60, 3, 3);
       // mapList.Add(yggdrasil);

        MapData Sunspots = new MapData("Sunspots", 69943, 60, 112, 108);
        mapList.Add(Sunspots);

       // MapData AquaMansion = new MapData("aquamansion", 10228, 2, 34, 60);
       // mapList.Add(AquaMansion);

        MapData hawaii = new MapData("hawaii", 27075, 1, 67, 43);
        mapList.Add(hawaii);

        mapList.Add(hawaii);

        // MapData babel = new MapData("babel", 17974, 0, 0, 0);
        // mapList.Add(babel);

        
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
        wEngine.RenderWater();
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

    public int NumBytes;
    public int SpawnX;
    public int SpawnY;
    public int SpawnZ;

    public int Length;
    public int Width;
    public int Height;

    public MapData(string name, int numBytes, int spawnX, int spawnY, int spawnZ)
    {
        Name = name;
        //NumBytes = numBytes;

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
