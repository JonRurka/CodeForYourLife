using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Linq;
using System.Text;

namespace LibNoise
{
    class MazGen : IModule
    {
        public class Maze
        {
            public enum Direction
            {
                N = 1,
                W = 2
            }

            Stack s_stack;
            Random rand;

            public int MSIZEX;
            public int MSIZEY;
            public int[] maze_base;
            public byte[,] maze_data;

            private int iSmooth;

            #region Generating
            public void GenerateMaze(int sizeX, int sizeY, int seed, int smoothness)
            {
                iSmooth = smoothness;
                MSIZEX = sizeX;
                MSIZEY = sizeY;
                maze_base = new int[MSIZEX * MSIZEY];
                maze_data = new Byte[MSIZEX, MSIZEY];

                s_stack = new Stack();
                rand = new Random(seed);

                MazeInit(rand);

                cMazeState state = new cMazeState(rand.Next() % MSIZEX, rand.Next() % MSIZEY, 0);
                analyze_cell(state, rand);
            }

            void analyze_cell(cMazeState s, Random r)
            {
                bool bEnd = false, found;
                int indexSrc, indexDest, tDir = 0, prevDir = 0;

                while (true)
                {
                    if (s.dir == 15)
                    {
                        while (s.dir == 15)
                        {
                            s = (cMazeState)s_stack.pop();
                            if (s == null)
                            {
                                bEnd = true;
                                break;
                            }
                        }
                        if (bEnd == true) break;
                    }
                    else
                    {
                        do
                        {
                            prevDir = tDir;
                            tDir = (int)System.Math.Pow(2, r.Next() % 4);

                            if ((r.Next() % 32) < iSmooth)
                                if ((s.dir & prevDir) == 0)
                                    tDir = prevDir;

                            if ((s.dir & tDir) != 0)
                                found = true;
                            else
                                found = false;
                        } while (found == true && s.dir != 15);

                        s.dir |= tDir;

                        indexSrc = cell_index(s.x, s.y);

                        // direction W
                        if (tDir == 1 && s.x > 0)
                        {
                            indexDest = cell_index(s.x - 1, s.y);
                            if (base_cell(indexSrc) != base_cell(indexDest))
                            {
                                merge(indexSrc, indexDest);
                                maze_data[s.x, s.y] |= (byte)Direction.W;

                                s_stack.push(new cMazeState(s));
                                s.x -= 1; s.dir = 0;
                            }
                        }

                        // direction E
                        if (tDir == 2 && s.x < MSIZEX - 1)
                        {
                            indexDest = cell_index(s.x + 1, s.y);
                            if (base_cell(indexSrc) != base_cell(indexDest))
                            {
                                merge(indexSrc, indexDest);
                                maze_data[s.x + 1, s.y] |= (byte)Direction.W;

                                s_stack.push(new cMazeState(s));
                                s.x += 1; s.dir = 0;
                            }
                        }

                        // direction N
                        if (tDir == 4 && s.y > 0)
                        {
                            indexDest = cell_index(s.x, s.y - 1);
                            if (base_cell(indexSrc) != base_cell(indexDest))
                            {
                                merge(indexSrc, indexDest);
                                maze_data[s.x, s.y] |= (byte)Direction.N;

                                s_stack.push(new cMazeState(s));
                                s.y -= 1; s.dir = 0;
                            }
                        }

                        // direction S
                        if (tDir == 8 && s.y < MSIZEY - 1)
                        {
                            indexDest = cell_index(s.x, s.y + 1);
                            if (base_cell(indexSrc) != base_cell(indexDest))
                            {
                                merge(indexSrc, indexDest);
                                maze_data[s.x, s.y + 1] |= (byte)Direction.N;

                                s_stack.push(new cMazeState(s));
                                s.y += 1; s.dir = 0;
                            }
                        }
                    } // else
                } // while 
            } // function
            #endregion
            #region Bitmap
            public Bitmap GetBitmap(int xS, int yS)
            {
                int i, j;

                Bitmap tB = new Bitmap(xS + 1, yS + 1);
                Graphics g = Graphics.FromImage((Image)tB);

                Brush w = Brushes.White;
                Brush b = Brushes.Black;

                Pen bp = new Pen(b, 1);

                // background
                g.FillRectangle(w, 0, 0, tB.Width - 1, tB.Height - 1);
                g.DrawRectangle(bp, 0, 0, tB.Width - 1, tB.Height - 1);

                int xSize = xS / MSIZEX;
                int ySize = yS / MSIZEY;

                for (i = 0; i < MSIZEX; i++)
                    for (j = 0; j < MSIZEY; j++)
                    {
                        // inspect the maze
                        if ((maze_data[i, j] & (byte)Direction.N) == 0)
                            g.DrawLine(bp,
                                new Point(xSize * i, ySize * j),
                                new Point(xSize * (i + 1), ySize * j));

                        if ((maze_data[i, j] & (byte)Direction.W) == 0)
                            g.DrawLine(bp,
                                new Point(xSize * i, ySize * j),
                                new Point(xSize * i, ySize * (j + 1)));

                    }

                // start & end
                g.DrawLine(bp, 0, 0, xSize, 0);
                g.DrawLine(bp, 0, 0, 0, xSize);
                g.DrawLine(bp, xS, yS, xS - xSize, yS);
                g.DrawLine(bp, xS, yS, xS, yS - ySize);

                g.Dispose();

                return tB;
            }
            #endregion
            #region Cell functions
            int cell_index(int x, int y)
            {
                return MSIZEX * y + x;
            }
            int base_cell(int tIndex)
            {
                int index = tIndex;
                while (maze_base[index] >= 0)
                {
                    index = maze_base[index];
                }
                return index;
            }
            void merge(int index1, int index2)
            {
                // merge both lists
                int base1 = base_cell(index1);
                int base2 = base_cell(index2);
                maze_base[base2] = base1;
            }
            #endregion
            #region MazeInit
            void MazeInit(Random r)
            {
                int i, j;

                // maze data
                for (i = 0; i < MSIZEX; i++)
                    for (j = 0; j < MSIZEY; j++)
                    {
                        maze_base[cell_index(i, j)] = -1;
                        maze_data[i, j] = 0;
                    }
            }


            #endregion
        }

        Maze myMaze;
        Bitmap myMazeBitmap;
        UnityEngine.Texture2D MazeTexture;
        public float[,] HeightData;

        public MazGen() : this(2, 2, 0, 1) { }

        public MazGen(int mazeSizeX, int mazeSizeY, int seed, int smooth)
        {
            try
            {
                myMaze = new Maze();
                myMaze.GenerateMaze(mazeSizeX, mazeSizeY, seed, smooth);
                myMazeBitmap = myMaze.GetBitmap(mazeSizeX * 2, mazeSizeY * 2);
                SetHeightmapData();
            }
            catch (Exception e)
            {
                SafeDebug.LogException(e);
            }
        }

        public double GetValue(double x, double y, double z)
        {
            if (HeightData != null)
            {
                try
                {
                    if (IsInBounds((int)x, (int)z))
                        return (double)HeightData[(int)x, (int)z];
                    else return -1;
                }
                catch (Exception e)
                {
                    SafeDebug.LogError(string.Format("Message: {0}, \nfunction: GetValue, \nStacktrace: {1}, \nValues: x={2}/{3}, z={4}/{5}.",
                        e.Message, e.StackTrace, x, HeightData.GetLength(0), z, HeightData.GetLength(1)));
                }
            }
            return 0;
        }

        public UnityEngine.Texture2D GetTexture()
        {
            return MazeTexture;
        }

        private void SetHeightmapData()
        {
            if (myMaze != null)
            {
                ManualResetEvent resetEvent = new ManualResetEvent(false);
                resetEvent.Reset();
                Loom.QueueOnMainThread(() =>
                {
                    System.IO.MemoryStream memStream = new System.IO.MemoryStream();
                    myMazeBitmap.Save(memStream, ImageFormat.Png);
                    MazeTexture = new UnityEngine.Texture2D(myMazeBitmap.Width, myMazeBitmap.Height);
                    MazeTexture.LoadImage(memStream.ToArray());
                    MazeTexture.Apply();
                    memStream.Close();
                    memStream = null;
                    HeightData = new float[myMazeBitmap.Width, myMazeBitmap.Height];
                    for (int x = 0; x < MazeTexture.width; x++)
                    {
                        for (int z = 0; z < MazeTexture.height; z++)
                        {
                            HeightData[x, z] = MazeTexture.GetPixel(x, z).grayscale;
                        }
                    }
                    resetEvent.Set();
                });
                resetEvent.WaitOne();
            }
        }

        private bool IsInBounds(int x, int z)
        {
            return ((x <= HeightData.GetLength(0) - 1) && x >= 0) && ((z <= HeightData.GetLength(1) - 1) && z >= 0);
        }
    }

    public class Stack
    {
        ArrayList tStack;

        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)tStack.GetEnumerator();
        }

        public int Count
        {
            get { return tStack.Count; }
        }

        public object push(object o)
        {
            tStack.Add(o);
            return o;
        }

        public object pop()
        {
            if (tStack.Count > 0)
            {
                object val = tStack[tStack.Count - 1];
                tStack.RemoveAt(tStack.Count - 1);
                return val;
            }
            else
                return null;
        }

        public object top()
        {
            if (tStack.Count > 0)
                return tStack[tStack.Count - 1];
            else
                return null;
        }

        public bool empty()
        {
            return (tStack.Count == 0);
        }

        public Stack() { tStack = new ArrayList(); }
    }

    public class cMazeState
    {
        public int x, y, dir;
        public cMazeState(int tx, int ty, int td) { x = tx; y = ty; dir = td; }
        public cMazeState(cMazeState s) { x = s.x; y = s.y; dir = s.dir; }
    }

    public class cCellPosition
    {
        public int x, y;
        public cCellPosition() { }
        public cCellPosition(int xp, int yp) { x = xp; y = yp; }
    }
}
