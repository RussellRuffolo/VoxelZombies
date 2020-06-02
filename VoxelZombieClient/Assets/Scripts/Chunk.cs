using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class Chunk : MonoBehaviour
{

    public World world;
    private UInt16[] voxels = new ushort[16 * 16 * 16];
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    List<int>[] TriangleLists = new List<int>[55];
    //0-48 are default MC ids offset back by 1 because 0 was air
    //49 is grass top
    //50 is wood top
    //51 is slabs top
    //52 is tnt top
    //53 is tnt bottom
    //54 is bookshelf top/bottom

    private List<Vector3> uvList = new List<Vector3>();

    public bool dirty = true;

    private Vector3[] _cubeVertices = new[] {
        new Vector3 (0, 0, 0),
        new Vector3 (1, 0, 0),
        new Vector3 (1, 1, 0),
        new Vector3 (0, 1, 0),
        new Vector3 (0, 1, 1),
        new Vector3 (1, 1, 1),
        new Vector3 (1, 0, 1),
        new Vector3 (0, 0, 1),
    };

    private Vector3[] _frontVertices = new[]
    {
        new Vector3 (0, 0, 0),
        new Vector3 (1, 0, 0),
        new Vector3 (1, 1, 0),
        new Vector3 (0, 1, 0),
    };



    private int[] _frontTriangles = new[]
    {
       0, 2, 1,
       0, 3, 2
    };

    private Vector3[] _topVertices = new[]
    {
        new Vector3 (1, 1, 0),
        new Vector3 (0, 1, 0),
        new Vector3 (0, 1, 1),
        new Vector3 (1, 1, 1),
    };

    private int[] _topTriangles = new[]
    {
        0,1,2,
        0,2,3
    };

    private Vector3[] _rightVertices = new[]
    {
        new Vector3 (1, 0, 0),
        new Vector3 (1, 1, 0),
        new Vector3 (1, 1, 1),
        new Vector3 (1, 0, 1)
    };

    private int[] _rightTriangles = new[]
    {
        0,1,2,
        0,2,3
    };

    private Vector3[] _leftVertices = new[]
   {
      new Vector3 (0, 0, 0),
      new Vector3 (0, 1, 0),
      new Vector3 (0, 1, 1),
      new Vector3 (0, 0, 1)
    };

    private int[] _leftTriangles = new[]
    {
        0,3,2,
        0,2,1
    };

    private Vector3[] _backVertices = new[]
   {
        new Vector3 (0, 1, 1),
        new Vector3 (1, 1, 1),
        new Vector3 (1, 0, 1),
        new Vector3 (0, 0, 1),
    };

    private int[] _backTriangles = new[]
    {
        2, 1, 0,
       2, 0, 3

    };

    private Vector3[] _bottomVertices = new[]
   {
       new Vector3 (0, 0, 0),
       new Vector3 (1, 0, 0),
       new Vector3 (1, 0, 1),
       new Vector3 (0, 0, 1)
    };

    private int[] _bottomTriangles = new[]
    {
        0,2,3,
        0,1,2
    };


    private int[] _cubeTriangles = new[] {
        // Front
        0, 2, 1,
        0, 3, 2,
        // Top
        2, 3, 4,
        2, 4, 5,
        // Right
        1, 2, 5,
        1, 5, 6,
        // Left
        0, 7, 4,
        0, 4, 3,
        // Back
        5, 4, 7,
        5, 7, 6,
        // Bottom
        0, 6, 7,
        0, 1, 6
    };

    private static Vector3[] _cubeNormals = new[]
    {
        Vector3.up,Vector3.up,Vector3.up,
        Vector3.up,Vector3.up,Vector3.up,
        Vector3.up,Vector3.up
    };

    private static Vector3[] _faceNormals = new[]
 {
        Vector3.up,Vector3.up,Vector3.up,
        Vector3.up
    };



    public UInt16 this[int x, int y, int z]
    {
        get
        {
            return voxels[x * 16 * 16 + y * 16 + z];
        }
        set
        {
            voxels[x * 16 * 16 + y * 16 + z] = value;
        }
    }

    private void Start()
    {
        for (int i = 0; i < 55; i++)
        {
            TriangleLists[i] = new List<int>();
        }
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        PhysicMaterial physMat = new PhysicMaterial();
        physMat.frictionCombine = PhysicMaterialCombine.Minimum;
        physMat.dynamicFriction = 0;
        physMat.staticFriction = 0;
        meshCollider.material = physMat;

    }

    private void Update()
    {
        if (dirty)
            RenderToMesh();
    }

    public void RenderToMesh()
    {
        var vertices = new List<Vector3>();

        foreach (List<int> triangleList in TriangleLists)
        {
            triangleList.Clear();
        }

        var normals = new List<Vector3>();

        uvList.Clear();

        for (var x = 0; x < 16; x++)
        {
            for (var y = 0; y < 16; y++)
            {
                for (var z = 0; z < 16; z++)
                {
                    var pos = new Vector3(x, y, z);
                    var verticesPos = vertices.Count;
                    var voxelType = this[x, y, z];
                    // If it is air we ignore this block
                    if (voxelType == 0)
                        continue;

                    //RENDER FRONT
                    int front;
                    if (z == 0) { front = 0; } else { front = this[x, y, z - 1]; }
                    if (front == 0 || front == 18 || front == 20)
                    {
                        foreach (var vert in _frontVertices)
                            vertices.Add(pos + vert);

                        uvList.Add(new Vector2(0, 0));
                        uvList.Add(new Vector2(1, 0));

                        uvList.Add(new Vector2(1, 1));
                        uvList.Add(new Vector2(0, 1));

                        AddTriangles(voxelType, verticesPos, _frontTriangles);


                        foreach (var normal in _faceNormals)
                            normals.Add(normal);
                    }

                    verticesPos = vertices.Count;

                    //RENDER TOP
                    int top;
                    if (y == 15) { top = 0; } else { top = this[x, y + 1, z]; }

                    if (top == 0 || top == 18 || top == 20)
                    {
                        foreach (var vert in _topVertices)
                            vertices.Add(pos + vert);

                        uvList.Add(new Vector2(0, 0));
                        uvList.Add(new Vector2(0, 1));
                        uvList.Add(new Vector2(1, 1));
                        uvList.Add(new Vector2(1, 0));

                        AddTriangles(voxelType, verticesPos, _topTriangles);

                        foreach (var normal in _faceNormals)
                            normals.Add(normal);

                    }

                    verticesPos = vertices.Count;

                    //RENDER RIGHT
                    int right;
                    if (x == 15) { right = 0; } else { right = this[x + 1, y, z]; }

                    if (right == 0 || right == 18 || right == 20)
                    {
                        foreach (var vert in _rightVertices)
                            vertices.Add(pos + vert);

                        uvList.Add(new Vector2(0, 0));
                        uvList.Add(new Vector2(0, 1));
                        uvList.Add(new Vector2(1, 1));
                        uvList.Add(new Vector2(1, 0));
                        AddTriangles(voxelType, verticesPos, _rightTriangles);


                        foreach (var normal in _faceNormals)
                            normals.Add(normal);

                    }

                    verticesPos = vertices.Count;

                    //RENDER LEFT
                    int left;
                    if (x == 0) { left = 0; } else { left = this[x - 1, y, z]; }

                    if (left == 0 || left == 18 || left == 20)
                    {
                        foreach (var vert in _leftVertices)
                            vertices.Add(pos + vert);

                        uvList.Add(new Vector2(0, 0));
                        uvList.Add(new Vector2(0, 1));
                        uvList.Add(new Vector2(1, 1));
                        uvList.Add(new Vector2(1, 0));
                        AddTriangles(voxelType, verticesPos, _leftTriangles);

                        foreach (var normal in _faceNormals)
                            normals.Add(normal);

                    }

                    verticesPos = vertices.Count;

                    //RENDER BACK
                    int back;
                    if (z == 15) { back = 0; } else { back = this[x, y, z + 1]; }

                    if (back == 0 || back == 18 || back == 20)
                    {
                        foreach (var vert in _backVertices)
                            vertices.Add(pos + vert);

                        uvList.Add(new Vector2(1, 1));
                        uvList.Add(new Vector2(0, 1));

                        uvList.Add(new Vector2(0, 0));
                        uvList.Add(new Vector2(1, 0));

                        AddTriangles(voxelType, verticesPos, _backTriangles);

                        foreach (var normal in _faceNormals)
                            normals.Add(normal);

                    }

                    verticesPos = vertices.Count;

                    //RENDER BOTTOM
                    int bottom;
                    if (y == 0) { bottom = 0; } else { bottom = this[x, y - 1, z]; }

                    if (bottom == 0 || bottom == 18 || bottom == 20)
                    {
                        foreach (var vert in _bottomVertices)
                            vertices.Add(pos + vert);

                        uvList.Add(new Vector2(0, 0));
                        uvList.Add(new Vector2(0, 1));
                        uvList.Add(new Vector2(1, 1));
                        uvList.Add(new Vector2(1, 0));
                        AddTriangles(voxelType, verticesPos, _bottomTriangles);

                        foreach (var normal in _faceNormals)
                            normals.Add(normal);

                    }

                }
            }
        }

        // Apply new mesh to MeshFilter
        var mesh = new Mesh();
        mesh.subMeshCount = 55;
        mesh.SetVertices(vertices);
        for (int i = 0; i < 55; i++)
        {
            mesh.SetTriangles(TriangleLists[i].ToArray(), i);
        }
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvList);
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        dirty = false;
    }

    private void AddTriangles(int vType, int vPos, int[] triangles)
    {
        //0-48 are default MC ids offset back by 1 because 0 was air
        //49 is grass top
        //50 is wood top
        //51 is slabs top
        //52 is tnt top
        //53 is bookshelf top
        switch (vType)
        {
            case (2):
                if (triangles == _topTriangles)
                {
                    foreach (var tri in triangles)
                        TriangleLists[49].Add(vPos + tri);
                }
                else
                {
                    foreach (var tri in triangles)
                        TriangleLists[vType - 1].Add(vPos + tri);
                }
                break;
            case (17):
                if (triangles == _topTriangles || triangles == _bottomTriangles)
                {
                    foreach (var tri in triangles)
                        TriangleLists[50].Add(vPos + tri);
                }
                else
                {
                    foreach (var tri in triangles)
                        TriangleLists[vType - 1].Add(vPos + tri);
                }
                break;
            case (43):
                if (triangles == _topTriangles || triangles == _bottomTriangles)
                {
                    foreach (var tri in triangles)
                        TriangleLists[51].Add(vPos + tri);
                }
                else
                {
                    foreach (var tri in triangles)
                        TriangleLists[vType - 1].Add(vPos + tri);
                }
                break;
            case (46):
                if (triangles == _topTriangles)
                {
                    foreach (var tri in triangles)
                        TriangleLists[52].Add(vPos + tri);
                }
                else if (triangles == _bottomTriangles)
                {
                    foreach (var tri in triangles)
                        TriangleLists[53].Add(vPos + tri);
                }
                else
                {
                    foreach (var tri in triangles)
                        TriangleLists[vType - 1].Add(vPos + tri);
                }
                break;
            case 47:
                if (triangles == _topTriangles || triangles == _bottomTriangles)
                {
                    foreach (var tri in triangles)
                        TriangleLists[54].Add(vPos + tri);
                }
                else
                {
                    foreach (var tri in triangles)
                        TriangleLists[vType - 1].Add(vPos + tri);
                }
                break;
            default:
                foreach (var tri in triangles)
                    TriangleLists[vType - 1].Add(vPos + tri);
                break;
        }


    }


}
