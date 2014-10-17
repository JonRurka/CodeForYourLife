using UnityEngine;
using System.Collections;

public interface IVoxelBuilder {
    void SetBlockTypes(BlockType[] _blockTypeList, Rect[] _AtlasUvs);
    MeshData Render(Vector3Int chunk, bool renderOnly);
    float[][] Generate(int _seed, bool _enableCaves, float _amp, float _caveDensity, float _groundOffset, float _grassOffset);
    void SetBlock(int _x, int _y, int _z, byte type);
    byte GetBlock(int _x, int _y, int _z);
}
