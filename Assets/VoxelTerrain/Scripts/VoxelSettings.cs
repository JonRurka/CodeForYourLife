using UnityEngine;
using System.Collections;

public static class VoxelSettings {
    // world settings.
    public static int seed = 0;
    public static bool randomSeed = true;
    public static bool CircleGen = false;
    public static bool enableCaves = true;
    public static float caveDensity = 8;
    public static float amplitude = 5;
    public static float groundOffset = -35;
    public static float grassOffset = 4;
    public static int maxSuperChunksX = 1;
    public static int maxSuperChunksY = 1;
    public static int maxSuperChunksZ = 1;
    public static int ViewDistanceX = 5;
    public static int ViewDistanceY = 5;
    public static int ViewDistanceZ = 5;
    public static int ViewRadius = 2;

    // chunk settings.
    public static int voxelsPerMeter = 1;
    public static int MeterSizeX = 15;
    public static int MeterSizeY = 15;
    public static int MeterSizeZ = 15;
    public static int ChunkSizeX = MeterSizeX * voxelsPerMeter;
    public static int ChunkSizeY = MeterSizeY * voxelsPerMeter;
    public static int ChunkSizeZ = MeterSizeZ * voxelsPerMeter;
    public static float half = ((1f / (float)voxelsPerMeter) / 2f);

    // Super chunks settings.
    public static int maxChunksX = 5;
    public static int maxChunksY = 1;
    public static int maxChunksZ = 5;
    public static int SuperSizeX = ChunkSizeX * maxChunksX;
    public static int SuperSizeY = ChunkSizeY * maxChunksY;
    public static int SuperSizeZ = ChunkSizeZ * maxChunksZ;
}

