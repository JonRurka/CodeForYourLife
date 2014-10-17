using UnityEngine;
using System.Collections;

public interface IPageController {
    bool BuilderExists(int x, int y, int z);
    bool BuilderGenerated(int x, int y, int z);
    IVoxelBuilder GetBuilder(int x, int y, int z);
}
