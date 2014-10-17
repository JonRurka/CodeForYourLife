using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential), Serializable]
public struct Vector3Int {
    public int x;
    public int y;
    public int z;
    public Vector3Int(int _x, int _y, int _z) {
        x = _x;
        y = _y;
        z = _z;
    }
    public static Vector3Int[] Vector2ArrayToSVector2Int(Vector3[] inVector) {
        Vector3Int[] result = new Vector3Int[inVector.Length];
        for (int i = 0; i < inVector.Length; i++) {
            result[i] = inVector[i];
        }
        return result;
    }
    public static Vector3[] SVector3ArrayToVector3(Vector3Int[] inVector) {
        Vector3[] result = new Vector3[inVector.Length];
        for (int i = 0; i < inVector.Length; i++) {
            result[i] = inVector[i];
        }
        return result;
    }
    public static implicit operator Vector3Int(Vector3 inVector) {
        return new Vector3Int((int)inVector.x, (int)inVector.y, (int)inVector.z);
    }
    public static implicit operator Vector3(Vector3Int inVector) {
        return new Vector3(inVector.x, inVector.y, inVector.z);
    }
    public static bool operator ==(Vector3Int first, Vector3Int second) {
        return (first.x == second.x && first.y == second.y && first.z == second.z);
    }
    public static bool operator !=(Vector3Int first, Vector3Int second) {
        return (first.x != second.x || first.y != second.y || first.z != second.z);
    }
    public static Vector3Int operator +(Vector3Int first, Vector3Int second) {
        Vector3Int result = new Vector3Int();
        result.x = first.x + second.x;
        result.y = first.y + second.y;
        result.z = first.z + second.z;
        return result;
    }
    public static Vector3Int operator *(Vector3Int first, Vector3Int second) {
        Vector3Int result = new Vector3Int();
        result.x = first.x * second.x;
        result.y = first.y * second.y;
        result.z = first.z * second.z;
        return result;
    }
    public static float Distance(Vector3Int a, Vector3Int b) {
        int deltaX = b.x - a.x;
        int deltaY = b.y - a.y;
        int deltaZ = b.z - a.z;
        return Mathf.Abs(Mathf.Sqrt((deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ)));
    }
    public override bool Equals(object obj) {
        return base.Equals(obj);
    }
    public override int GetHashCode() {
        return base.GetHashCode();
    }
    public override string ToString() {
        return string.Format("({0}, {1}, {2})", x, y, z);
    }
}

[StructLayout(LayoutKind.Sequential), Serializable]
public struct Vector2Int {
    public int x;
    public int y;
    public Vector2Int(int _x, int _y) {
        x = _x;
        y = _y;
    }
    public static implicit operator Vector2(Vector2Int inVector) {
        return new Vector2(inVector.x, inVector.y);
    }
    public static implicit operator Vector2Int(Vector2 inVector) {
        return new Vector2Int((int)inVector.x, (int)inVector.y);
    }
    public static Vector2Int[] Vector2ArrayToSVector2Int(Vector2[] inVector) {
        Vector2Int[] result = new Vector2Int[inVector.Length];
        for (int i = 0; i < inVector.Length; i++) {
            result[i] = inVector[i];
        }
        return result;
    }
    public static Vector2[] SVector3ArrayToVector3(Vector2Int[] inVector) {
        Vector2[] result = new Vector2[inVector.Length];
        for (int i = 0; i < inVector.Length; i++) {
            result[i] = inVector[i];
        }
        return result;
    }
    public static bool operator ==(Vector2Int first, Vector2Int second) {
        return (first.x == second.x && first.y == second.y);
    }
    public static bool operator !=(Vector2Int first, Vector2Int second) {
        return (first.x != second.x || first.y != second.y);
    }
    public static Vector2Int operator +(Vector2Int first, Vector2Int second) {
        Vector2Int result = new Vector2Int();
        result.x = first.x + second.x;
        result.y = first.y + second.y;
        return result;
    }
    public static Vector2Int operator *(Vector2Int first, Vector2Int second) {
        Vector2Int result = new Vector2Int();
        result.x = first.x * second.x;
        result.y = first.y * second.y;
        return result;
    }
    public static float Distance(Vector2Int a, Vector2Int b) {
        int deltaX = b.x - a.x;
        int deltaY = b.y - a.y;
        return Mathf.Abs(Mathf.Sqrt((deltaX * deltaX) + (deltaY * deltaY)));
    }
    public override bool Equals(object obj) {
        return base.Equals(obj);
    }
    public override int GetHashCode() {
        return base.GetHashCode();
    }
    public override string ToString() {
        return string.Format("({0}, {1})", x, y);
    }
}

[Serializable]
public struct MeshData {
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] UVs;
    public MeshData(Vector3[] _vertices, int[] _triangles, Vector2[] _UVs) {
        vertices = _vertices;
        triangles = _triangles;
        UVs = _UVs;
    }
}
