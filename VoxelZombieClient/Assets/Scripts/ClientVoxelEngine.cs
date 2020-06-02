using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientVoxelEngine : MonoBehaviour
{
    public World world = new World();
    public List<Material> materialList;

    private void Awake()
    {
        foreach(Material mat in materialList)
        {
            mat.SetFloat("_Glossiness", 0);
        }
    }

    public void LoadMap(int Width, int Length, int Height, byte[] mapBytes)
    {

        if(world.Chunks.Count != 0)
        {
            UnloadMap();
        }

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
                    world.Chunks.Add(new ChunkID(x, y, z), chunk);
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
