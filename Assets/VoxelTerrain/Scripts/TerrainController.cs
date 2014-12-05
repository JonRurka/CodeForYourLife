using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibNoise;

public class TerrainController : MonoBehaviour, IPageController {
    public static TerrainController Instance;
    public Texture2D textureAtlas;
    public Rect[] AtlasUvs;
    public Material chunkMaterial;
    public static string WorldThreadName = "WorldThread";
    public static string genThreadName = "GenerationThread";
    public static string setBlockThreadName = "SetBlockThread";


    public static Dictionary<byte, BlockType> blockTypes = new Dictionary<byte, BlockType>();
    public static Dictionary<Vector3Int, Chunk> Chunks = new Dictionary<Vector3Int, Chunk>();

    System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

    public GameObject player;
    public GameObject chunkPrefab;
    public Texture2D[] AllCubeTextures;
    public BlockType[] BlocksArray;
    public Texture2D MapTexture;
    public List<Texture2D> ChunkImgList = new List<Texture2D>();

    public int testX1 = 0;
    public int testX2Super = 0;
    public int testX2Chunk = 0;

    private Dictionary<Vector2Int, Texture2D> SurfaceImages = new Dictionary<Vector2Int, Texture2D>();
    public List<SurfaceImagesDictEntry> SurfaceImagesDictionary = new List<SurfaceImagesDictEntry>();

    //byte[,] SurfaceBytes;

    bool imagesSet = false;

    void Awake() {
        if (!Instance) {
            Instance = this;
        }
        else {
            Debug.Log("Only one world controller allowed per scene. Destroying duplicate.");
            Destroy(this);
        }
    }

	// Use this for initialization
	void Start () {
        Loom.AddAsyncThread(WorldThreadName);
        Loom.AddAsyncThread(genThreadName);
        Loom.AddAsyncThread(setBlockThreadName);
        if (VoxelSettings.randomSeed)
            VoxelSettings.seed = UnityEngine.Random.Range(-2147483648, 2147483647);
        textureAtlas = new Texture2D(0, 0);
        AtlasUvs = textureAtlas.PackTextures(AllCubeTextures, 10);
        MapTexture = new Texture2D(0, 0);
        AddBlockType(BaseType.air, "air", new int[] { -1, -1, -1, -1, -1, -1 }, null);
        AddBlockType(BaseType.solid, "Black", new int[] { 0, 0, 0, 0, 0, 0 }, null);
        AddBlockType(BaseType.solid, "White", new int[] { 1, 1, 1, 1, 1, 1 }, null);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI () {
        if (MapTexture != null)
        {
            GUI.DrawTexture(new Rect(10, 10, MapTexture.width, MapTexture.height), MapTexture);
        }
    }

    public void Init()
    {
        SpawnChunksAroundPoint(new Vector3(0, 0, 0));
    }

    public void SpawnChunksAroundPoint(Vector3 point) {
        SpawnChunkFeild();
        Loom.QueueAsyncTask(WorldThreadName, () => {
            MazGen module = new MazGen(VoxelSettings.SuperSizeX / 2, VoxelSettings.SuperSizeZ / 2, VoxelSettings.seed, 1);
            for (int x = -1; x <= VoxelSettings.maxChunksX; x++)
            {
                for (int z = -1; z <= VoxelSettings.maxChunksZ; z++)
                {
                    Vector3Int location3D = new Vector3Int(x, 0, z);
                    Vector2Int location2D = new Vector2Int(location3D.x, location3D.z);
                    try
                    {
                        if (Chunks.ContainsKey(location3D) && !Chunks[location3D].Generated)
                        {
                            float[][] surface = Chunks[location3D].GenerateChunk(module);
                            Chunks[location3D].Render(false);
                        }
                    }
                    catch (Exception e)
                    {
                        SafeDebug.LogException(e);
                    }
                }
            }
            SafeDebug.Log("Finished rendering.");

            Loom.QueueOnMainThread(() =>
            {
                MapTexture = module.GetTexture();
            });
        });
    }

    public void AddBlockType(BaseType _baseType, string _name, int[] _textures, GameObject _prefab) {
        byte index = (byte)blockTypes.Count;
        blockTypes.Add(index, new BlockType(_baseType, index, _name, _textures, _prefab));
        BlocksArray = GetBlockTypeArray(blockTypes.Values);
    }

    public bool BuilderExists(int x, int y, int z) { 
        return Chunks.ContainsKey(new Vector3Int(x, y, z));
    }

    public bool BuilderGenerated(int x, int y, int z) {
        if (BuilderExists(x, y, z)) {
            return Chunks[new Vector3Int(x, y, z)].Generated;
        }
        return false;
    } 

    public IVoxelBuilder GetBuilder(int x, int y, int z) { 
        IVoxelBuilder result = null;
        Vector3Int location = new Vector3Int(x, y, z);
        if (BuilderExists(x, y, z)) {
            result = Chunks[location].builder;
        }
        return result;
    } // needs to be changed if using superchunks.

    public byte GetBlock(int x, int y, int z)
    {
        Vector3Int chunk = VoxelConversions.VoxelToChunk(new Vector3Int(x, y, x));
        Vector3Int localVoxel = VoxelConversions.GlobalVoxToLocalChunkVoxCoord(chunk, new Vector3Int(x, y, z));
        byte result = 1;
        if (x >= 0 && y >= 0 && z >= 0 && Chunks.ContainsKey(chunk))
        {
            result = Chunks[chunk].GetBlock(x, y, z);
        }
        return result;
    }

    private void CreateImage(Vector2Int location, float[][] surfaceData) {
        try {
            Texture2D chunkImg = new Texture2D(VoxelSettings.ChunkSizeX, VoxelSettings.ChunkSizeZ);
            for (int x = 0; x < chunkImg.width; x++) {
                for (int z = 0, zr = chunkImg.height - 1; z < chunkImg.height; z++, zr--) {
                    byte colorVal = (byte)(surfaceData[x][z]);
                    chunkImg.SetPixel(x, zr, new Color(colorVal, colorVal, colorVal));
                }
            }
            chunkImg.Apply();
            ChunkImgList.Add(chunkImg);
            SurfaceImages.Add(location, chunkImg);
            SurfaceImagesDictionary.Add(new SurfaceImagesDictEntry(location, chunkImg));
        }
        catch (Exception e) {
            SafeDebug.LogException(e);
        }
    }

    private void SpawnChunkFeild() {
        for (int x = -1; x <= VoxelSettings.maxChunksX; x++) {
            for (int z = -1; z <= VoxelSettings.maxChunksZ; z++) {
                SpawnChunk(new Vector3Int(x, 0, z));
            }
        }
    }

    private void SpawnChunk(Vector3Int location) {
        if (!Chunks.ContainsKey(location)) {
            Chunk chunk = ((GameObject)Instantiate(chunkPrefab)).AddComponent<Chunk>();
            chunk.transform.parent = transform;
            chunk.name = string.Format("Chunk_{0}.{1}.{2}", location.x, location.y, location.z);
            chunk.Init(new Vector3Int(location.x, location.y, location.z));
            Chunks.Add(location, chunk);
        }
        else {
            Debug.Log("Chunk already exists :L");
        }
    }

    private static BlockType[] GetBlockTypeArray(Dictionary<byte, BlockType>.ValueCollection collection) {
        BlockType[] types = new BlockType[collection.Count];
        int i = 0;
        foreach (BlockType _type in collection) {
            types[i++] = _type;
        }
        return types;
    }
}

[Serializable]
public struct SurfaceImagesDictEntry {
    public Vector2Int Location;
    public Texture2D Texture;
    public SurfaceImagesDictEntry(Vector2Int _location, Texture2D _texture) {
        Location = _location;
        Texture = _texture;
    }
}
