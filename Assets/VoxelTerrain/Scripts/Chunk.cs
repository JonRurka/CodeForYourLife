using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

public class Chunk : MonoBehaviour {
    public Vector3Int ChunkPosition;
    public IVoxelBuilder builder;

    public bool Generated {
        get { return generated; }
    }

    MeshFilter _filter;
    MeshRenderer _renderer;
    MeshCollider _collider;
    GameObject player;

    ManualResetEvent resetEvent = new ManualResetEvent(false);

    bool enableTest = false;
    bool generated = false;
    bool rendered = false;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

	}

    public void Init(Vector3Int chunkPos) {
        ChunkPosition = chunkPos;
        transform.position = VoxelConversions.ChunkCoordToWorld(chunkPos);
        _renderer = gameObject.GetComponent<MeshRenderer>();
        _filter = gameObject.GetComponent<MeshFilter>();
        _collider = gameObject.GetComponent<MeshCollider>();
        _renderer.material.SetTexture("_MainTex", TerrainController.Instance.textureAtlas);
        player = TerrainController.Instance.player;
        createChunkBuilder();
    }

    public void createChunkBuilder() {
        builder = new VoxelBuilder( TerrainController.Instance,
                                    ChunkPosition,
                                    VoxelSettings.voxelsPerMeter,
                                    VoxelSettings.MeterSizeX,
                                    VoxelSettings.MeterSizeY,
                                    VoxelSettings.MeterSizeZ);
        builder.SetBlockTypes(TerrainController.Instance.BlocksArray, TerrainController.Instance.AtlasUvs);
    }

    public float[][] GenerateChunk(LibNoise.IModule module)
    {
        return ((VoxelBuilder)builder).Generate( module,
                                                 VoxelSettings.seed,
                                                 VoxelSettings.enableCaves,
                                                 VoxelSettings.amplitude,
                                                 VoxelSettings.caveDensity,
                                                 VoxelSettings.groundOffset,
                                                 VoxelSettings.grassOffset);
    }

    public float[][] GenerateChunk() {
        return builder.Generate( VoxelSettings.seed,
                                 VoxelSettings.enableCaves,
                                 VoxelSettings.amplitude,
                                 VoxelSettings.caveDensity,
                                 VoxelSettings.groundOffset,
                                 VoxelSettings.grassOffset );
    }

    public void Render(bool renderOnly) {
        MeshData meshData = RenderChunk(renderOnly);
        Loom.QueueOnMainThread(() => {
            Mesh mesh = new Mesh();
            mesh.vertices = meshData.vertices;
            mesh.triangles = meshData.triangles;
            mesh.uv = meshData.UVs;
            mesh.RecalculateNormals();

            _filter.sharedMesh = mesh;
            _collider.sharedMesh = mesh;
            _renderer.material.SetTexture("_MainTex", TerrainController.Instance.textureAtlas);

            meshData.vertices = null;
            meshData.triangles = null;
            meshData.UVs = null;
            generated = true;
            rendered = true;
        });
    }

    private MeshData RenderChunk(bool renderOnly) {
        return builder.Render(ChunkPosition, renderOnly);
    }
}
