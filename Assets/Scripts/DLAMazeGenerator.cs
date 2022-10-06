using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DLAMazeGenerator : MazeGeneratorBehavior
{
    public TileBase floorTile;
    public TileBase wallTile;
    public TileBase emptyTile;

    //public int wallHeight = 1;
    //public int padding = 1;
    //public int minRoomSize = 3;
    //public int maxRoomSize = 5;

    public int numSteps = 3;
    public int stepDepth = 1;

    HashSet<Vector3Int> pointsSet = new();
    List<Vector3Int> orderedPoints = new();

    public override void Clear(TileBase fillWith)
    {
        pointsSet.Clear();
        orderedPoints.Clear();
        base.Clear(fillWith);
    }

    void AddPoint(Vector3Int p)
    {
        if (!pointsSet.Contains(p))
        {
            pointsSet.Add(p);
            orderedPoints.Add(p);
        }
    }

    Vector3Int RandomPoint()
    {
        int idx = Random.Range(0, orderedPoints.Count);
        return orderedPoints[idx];
    }

    static Vector3Int[] allDirs = new Vector3Int[4] { Vector3Int.left, Vector3Int.down, Vector3Int.right, Vector3Int.up };
    static Vector3Int RandomDir()
    {
        return allDirs[Random.Range(0, 4)];
    }

    void RenderToTilemap()
    {
        foreach (Vector3Int pt in pointsSet)
        {
            tilemap.SetTile(pt, floorTile);
        }
    }

    static Vector3Int Round(Vector3 x)
    {
        Vector3Int ret = new();
        ret.x = Mathf.RoundToInt(x.x);
        ret.y = Mathf.RoundToInt(x.y);
        ret.z = Mathf.RoundToInt(x.z);
        return ret;
    }

    public override void Generate()
    {
        Clear(emptyTile);
        AddPoint(Round(tilemap.cellBounds.center));
        for (int i = 0; i < numSteps; ++i)
        {
            Vector3Int dir = RandomDir();
            Vector3Int pt = RandomPoint() + dir;
            for (int j = 0; j < stepDepth; ++j)
            {
                while (pointsSet.Contains(pt)) pt += dir;
                AddPoint(pt);
            }
        }
        RenderToTilemap();
    }
}
