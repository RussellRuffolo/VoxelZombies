using System.Collections;
using System.Collections.Generic;
using fNbt;
using System.IO;
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

        LoadMap("asylum");
    }

    public void LoadMap(string mapName)
    {

        if(world.Chunks.Count != 0)
        {
            UnloadMap();
        }

        string fileName;
        
        if(Application.platform == RuntimePlatform.OSXPlayer)
        {
            fileName = Application.dataPath + "/Resources" + "/Data" + "/StreamingAssets/" + mapName + ".bin";           
        }
        else
        {
            fileName = Application.streamingAssetsPath + "\\" + mapName + ".bin";       
        }

        BinaryReader binReader = new BinaryReader(new FileStream(fileName, FileMode.Open));

       
        short length = binReader.ReadInt16();
        short width = binReader.ReadInt16();
        short height = binReader.ReadInt16();

        byte[] mapBytes = binReader.ReadBytes(length * width * height);

        Length = length;
         Width = width;
         Height = height;

        bController.SetMapBoundaries(Length, Width, Height);

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

                    chunk.GetComponent<Chunk>().init();
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
                    world[x, y, z] = mapBytes[blockCount];
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
