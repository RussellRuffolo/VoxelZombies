﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class World 
{
    public Dictionary<ChunkID, Chunk> Chunks = new Dictionary<ChunkID, Chunk>();

    public UInt16 this[int x, int y, int z]
    {
        get
        {
            var chunk = Chunks[ChunkID.FromWorldPos(x, y, z)];
            return chunk[x & 0xF, y & 0xF, z & 0xF];
        }
        set
        {
            var chunk = Chunks[ChunkID.FromWorldPos(x, y, z)];
            chunk[x & 0xF, y & 0xF, z & 0xF] = value;
        }
    }
}
