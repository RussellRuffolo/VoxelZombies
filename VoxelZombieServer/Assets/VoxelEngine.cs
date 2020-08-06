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

    //public byte[] mapBytes;

    public List<MapData> mapList = new List<MapData>();
    public MapData currentMap;

   

    public BoundaryController bController;
    public WaterEngine wEngine;

    
  
    const ushort MAP_TAG = 4;
    private void Awake()
    {
       MapData gurka = new MapData("gurka", 55, 42, 28);
       mapList.Add(gurka);

        MapData prison = new MapData("prison", 2, 100, 2);
        mapList.Add(prison);

        MapData stadium = new MapData("stadium", 112, 65, 65);
        mapList.Add(stadium);

       // MapData sewers = new MapData("sewers", 20, 57, 51);
      //  mapList.Add(sewers);
 

       MapData excitebike = new MapData("excitebike", 24, 35, 36);
       mapList.Add(excitebike);

      //  MapData dwarves = new MapData("dwarves", 122, 2, 7);
     //   mapList.Add(dwarves);

      //  MapData diametric = new MapData("diametric", 46, 19, 26);
      //  mapList.Add(diametric);

       // MapData asylum = new MapData("asylum",  25, 129, 30);
      //  mapList.Add(asylum);

        

       // MapData Carson = new MapData("carson",  10, 35, 120);
      //  mapList.Add(Carson);    

       // MapData Sunspots = new MapData("Sunspots",  60, 112, 108);
       // mapList.Add(Sunspots);

       MapData hawaii = new MapData("hawaiiMod",  1, 67, 43);
       mapList.Add(hawaii);

     //   MapData colony = new MapData("colony", 56, 67, 8);
      //  mapList.Add(colony);

      //  MapData italy = new MapData("italy",  53, 89, 63);
     //   mapList.Add(italy);
     //   MapData swiss = new MapData("swiss",  29, 50, 12);
      //  mapList.Add(swiss);

       // mapList.Add(hawaii);
    }

    public void LoadMap(MapData map)
    {      
        UnloadMap();

        currentMap = map;
   
        
        string fileName = Application.dataPath + "/StreamingAssets/" + map.Name + ".bin";
        BinaryReader binReader = new BinaryReader(new FileStream(fileName, FileMode.Open));

        short length = binReader.ReadInt16();
        short width = binReader.ReadInt16();
        short height = binReader.ReadInt16();

        map.Length = length;
        map.Width = width;
        map.Height = height;

        bController.SetMapBoundaries(length, width, height);
       
       
        byte[] mapBytes = binReader.ReadBytes(length * width * height);

        
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
                    world[x, y, z] = mapBytes[blockCount];
                    blockCount++;                           
                }
            }
        }

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
