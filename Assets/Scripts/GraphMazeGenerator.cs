using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class GraphMazeGenerator : MazeGeneratorBehavior
{
    public TileBase floorTile;
    public TileBase wallTile;
    public TileBase emptyTile;
    public TileBase startTile;
    public TileBase endTile;

    public int minRoomSize = 3;
    public int maxRoomSize = 3;
    public int padding = 1;
    public int wallHeight = 1;
    public int numRooms = 3;
    public float edgeDensity = 0.1f;

    public class Room
    {
        public BoundsInt bounds = new();
        public BoundsInt UnpadBounds(int padding)
        {
            BoundsInt ret = bounds;
            ret.xMin += padding;
            ret.xMax -= padding;
            ret.yMin += padding;
            ret.yMax -= padding;
            return ret;
        }

        public BoundsInt UnpadFloorBounds(int padding, int wallHeight)
        {
            BoundsInt ret = UnpadBounds(padding);
            ret.yMax -= wallHeight;
            return ret;
        }

        public override string ToString()
        {
            return bounds.ToString();
        }
    }
    List<Room> rooms = new();
    DistanceGraph<Room> roomGraph = new();

    public override void Clear(TileBase fillWith)
    {
        rooms.Clear();
        roomGraph.Clear();
        base.Clear(fillWith);
    }

    int RandomRangeApproxNormal(int minIncl, int maxExcl)
    {
        int res = 4;
        int ret = 0;
        for (int i = 0; i < res; ++i)
        {
            ret += Random.Range(minIncl, maxExcl);
        }
        return ret / res;
    }

     BoundsInt RandomBounds()
    {
        int w = RandomRangeApproxNormal(minRoomSize + 2 * padding, maxRoomSize + 2 * padding + 1);
        int h = RandomRangeApproxNormal(minRoomSize + 2 * padding, maxRoomSize + 2 * padding + 1);
        int x = RandomRangeApproxNormal(tilemap.cellBounds.xMin, tilemap.cellBounds.xMax - w - 1);
        int y = RandomRangeApproxNormal(tilemap.cellBounds.yMin, tilemap.cellBounds.yMax - h - 1);
        BoundsInt ret = new();
        ret.xMin = x;
        ret.yMin = y;
        ret.xMax = x + w;
        ret.yMax = y + h;
        ret.zMin = 0;
        ret.zMax = 1;
        return ret;
    }

    bool Overlaps(BoundsInt lhs, BoundsInt rhs)
    {
        bool outside = lhs.xMin >= rhs.xMax ||
            lhs.yMin >= rhs.yMax ||
            lhs.xMax <= rhs.xMin ||
            lhs.yMax <= rhs.yMin;
        return !outside;
    }

     Vector2Int MinOverlap(BoundsInt anchor, BoundsInt other)
    {
        if (!Overlaps(anchor, other)) return Vector2Int.zero;

        int x0 = -Math.Max(0, anchor.xMax - other.xMin);
        int x1 = Math.Max(0, other.xMax - anchor.xMin);
        int y0 = -Math.Max(0, anchor.yMax - other.yMin);
        int y1 = Math.Max(0, other.yMax - anchor.yMin);
        Vector2Int ret = Vector2Int.zero;
        ret.x -= (-x0 <= x1) ? x0 : x1;
        ret.y -= (-y0 <= y1) ? y0 : y1;
        return ret;
        /*
        int dx = Math.Max(-x0, x1);
        int dy = Math.Max(-y0, y1);
        if (dx <= dy)
        {
            return Vector2Int.left * ((-x0 <= x1) ? x0 : x1);
        }
        else
        {
            return Vector2Int.down * ((-y0 <= y1) ? y0 : y1);
        }
        */
    }

     Vector2Int JiggleRooms(Room anchor, Room other)
    {
        Vector2Int overlap = MinOverlap(anchor.UnpadBounds(padding), other.bounds);
        other.bounds.x += overlap.x;
        other.bounds.y += overlap.y;
        return overlap;
    }

    Vector2Int JiggleRoomAgainstTileMapBoundary(Room room)
    {
        Vector2Int overlap = Vector2Int.zero;
        if (room.bounds.xMin < tilemap.cellBounds.xMin)
        {
            overlap.x = tilemap.cellBounds.xMin - room.bounds.xMin;
        }
        if (room.bounds.xMax > tilemap.cellBounds.xMax)
        {
            overlap.x = tilemap.cellBounds.xMax - room.bounds.xMax;
        }
        if (room.bounds.yMin < tilemap.cellBounds.yMin)
        {
            overlap.y = tilemap.cellBounds.yMin - room.bounds.yMin;
        }
        if (room.bounds.yMax > tilemap.cellBounds.yMax)
        {
            overlap.y = tilemap.cellBounds.yMax - room.bounds.yMax;
        }
        room.bounds.x += overlap.x;
        room.bounds.y += overlap.y;
        return overlap;
    }

    static void RandomShuffle<T>(T[] elems)
    {
        for (int i = elems.Length - 1; i >= 1; --i)
        {
            int j = Random.Range(0, i);
            T tmp = elems[i];
            elems[i] = elems[j];
            elems[j] = tmp;
        }
    }

     bool JiggleRooms()
    {
        Room[] shuffledAnchors = new Room[rooms.Count];
        rooms.CopyTo(shuffledAnchors);
        RandomShuffle(shuffledAnchors);
        Room[] shuffledRooms = new Room[rooms.Count];
        rooms.CopyTo(shuffledRooms);
        RandomShuffle(shuffledRooms);

        bool anyJiggled = false;
        foreach (Room x in shuffledAnchors)
        {
            foreach (Room y in shuffledRooms)
            {
                if (x == y) continue;
                Vector2Int jiggle = JiggleRooms(x, y);
                anyJiggled |= (jiggle != Vector2Int.zero);
            }
        }
        foreach (Room x in shuffledRooms)
        {
            Vector2Int jiggle = JiggleRoomAgainstTileMapBoundary(x);
            anyJiggled |= (jiggle != Vector2Int.zero);
        }
        return anyJiggled;
    }
    static Vector3Int Center(BoundsInt b)
    {
        Vector3Int ret = new();
        ret.x = (b.xMin + b.xMax - 1) / 2;
        ret.y = (b.yMin + b.yMax - 1) / 2;
        ret.z = (b.zMin + b.zMax - 1) / 2;
        return ret;
    }


    void RenderCorridorBetween(BoundsInt b1, BoundsInt b2)
    {
        bool xOverlaps = b2.xMin + 1 < b1.xMax - 1 && b2.xMax - 1 > b1.xMin + 1;
        bool yOverlaps = b2.yMin + 1 < b1.yMax - 1 && b2.yMax - 1 > b1.yMin + 1;
        if (xOverlaps)
        {
            int xMin = Math.Max(b1.xMin, b2.xMin);
            int xMax = Math.Min(b1.xMax, b2.xMax);
            int x = Random.Range(xMin + 1, xMax - 1);
            for (int y = Math.Min(b1.yMax, b2.yMax) - 1; y <= Math.Max(b1.yMin, b2.yMin); ++y)
            {
                RenderTile(new Vector3Int(x, y), floorTile);
            }
        } else if (yOverlaps)
        {
            int yMin = Math.Max(b1.yMin, b2.yMin);
            int yMax = Math.Min(b1.yMax, b2.yMax);
            int y = Random.Range(yMin + 1, yMax - 1);
            for (int x = Math.Min(b1.xMax, b2.xMax) - 1; x <= Math.Max(b1.xMin, b2.xMin); ++x)
            {
                RenderTile(new Vector3Int(x, y), floorTile);
            }
        } else
        {
            Vector3Int c1 = Center(b1);
            Vector3Int c2 = Center(b2);
            int dirX = c2.x - c1.x >= 0 ? 1 : -1;
            int dirY = c2.y - c1.y >= 0 ? 1 : -1;
            Vector3Int cur = c1;
            for (; cur.x != c2.x; cur.x += dirX)
            {
                RenderTile(cur, floorTile);
            }
            for (; cur.y != c2.y; cur.y += dirY)
            {
                RenderTile(cur, floorTile);
            }
        }
    }

    void RenderTile(int x, int y, TileBase t)
    {
        RenderTile(new Vector3Int(x, y), t);
    }

    void RenderTile(Vector3Int pos, TileBase t)
    {
        tilemap.SetTile(pos, null);
        tilemap.SetTile(pos, t);
    }

    void RenderRoom(Room r)
    {
        BoundsInt floor = r.UnpadFloorBounds(padding, wallHeight);
        foreach (Vector3Int pos in r.UnpadBounds(padding).allPositionsWithin)
        {
            bool isFloor = floor.Contains(pos);
            RenderTile(pos, isFloor ? floorTile : wallTile);
        }
    }

    void RenderToTilemap()
    {
        foreach (Room r in rooms) RenderRoom(r);
        roomGraph.TraverseDepthFirst(r => { }, (src, dst, len) => {
            RenderCorridorBetween(src.UnpadFloorBounds(padding, wallHeight), dst.UnpadFloorBounds(padding, wallHeight));
        });
        Room startingRoom = criticalPath[0];
        RenderTile(Center(startingRoom.UnpadFloorBounds(padding, wallHeight)), startTile);
        Room endingRoom = criticalPath[criticalPath.Count - 1];
        RenderTile(Center(endingRoom.UnpadFloorBounds(padding, wallHeight)), endTile);
        tilemap.RefreshAllTiles();
    }

    private DistanceGraph<Vector2> triangulation = new();
    private List<Room> criticalPath = new();

    void DebugDrawLine(Vector2 gridPt1, Vector2 gridPt2, Color c) {
        Vector3 worldPos1 = tilemap.CellToWorld(Vector3Int.zero) + new Vector3(gridPt1.x * tilemap.cellSize.x, gridPt1.y * tilemap.cellSize.y);
        Vector3 worldPos2 = tilemap.CellToWorld(Vector3Int.zero) + new Vector3(gridPt2.x * tilemap.cellSize.x, gridPt2.y * tilemap.cellSize.y);
        Debug.DrawLine(worldPos1, worldPos2, c);
    }

    void DebugDrawArrow(Vector2 gridPt1, Vector2 gridPt2, Color c, float size) {
        Vector3 worldPos1 = tilemap.CellToWorld(Vector3Int.zero) + new Vector3(gridPt1.x * tilemap.cellSize.x, gridPt1.y * tilemap.cellSize.y);
        Vector3 worldPos2 = tilemap.CellToWorld(Vector3Int.zero) + new Vector3(gridPt2.x * tilemap.cellSize.x, gridPt2.y * tilemap.cellSize.y);
        
        size *= (worldPos1 - worldPos2).magnitude;
        Debug.DrawLine(worldPos1, worldPos2, c);
        Debug.DrawLine(worldPos2 + size * new Vector3(-1, -1), worldPos2 + size * new Vector3(1, 1), c);
        Debug.DrawLine(worldPos2 + size * new Vector3(-1, 1), worldPos2 + size * new Vector3(1, -1), c);
    }

    public void Update()
    {
        triangulation.ForEachEdge((p1, p2, d) =>
        {
            DebugDrawLine(p1, p2, Color.gray);
        });
        roomGraph.ForEachEdge((r1, r2, d) => {
            DebugDrawLine(r1.bounds.center, r2.bounds.center, Color.green);
        });
        for (int i = 1; i < criticalPath.Count; ++i) {
            Room r1 = criticalPath[i-1];
            Room r2 = criticalPath[i];
            DebugDrawLine(r1.bounds.center, r2.bounds.center, Color.blue);
        }
    }

    public override void Generate()
    {
        Clear(emptyTile);
        for (int i = 0; i < numRooms; ++i)
        {
            Room room = new();
            room.bounds = RandomBounds();
            rooms.Add(room);
        }
        for (int i = 0; i < 1000 && JiggleRooms(); ++i) {  }

        Dictionary<Vector2, Room> roomByCenter = new();
        rooms.ForEach(r => roomByCenter.Add(r.bounds.center, r));
        triangulation = Delaunay.Triangulate(roomByCenter.Keys);
        Debug.Log(triangulation);
        DistanceGraph<Room> roomTriangulation = new();
        triangulation.ForEachEdge((p1, p2, _) =>
        {
            Room r1 = roomByCenter[p1];
            Room r2 = roomByCenter[p2];
            float dist = Mathf.Abs(p2.x - p1.x) + Mathf.Abs(p2.y - p1.y);
            roomTriangulation.AddUndirected(r1, r2, dist);
        });
        roomGraph = roomTriangulation.MinimalSpanningTree();
        roomTriangulation.ForEachEdge((r1, r2, d) => {
            bool hasEdge = roomGraph.ContainsEdge(r1, r2) || roomGraph.ContainsEdge(r2, r1);
            bool include = Random.Range(0f, 1f) <= edgeDensity;
            if (!hasEdge && include) {
                roomGraph.AddDirected(r1, r2, d);
            }
        });
        Debug.Log(roomGraph);
        criticalPath = roomGraph.Undirected().LongestPath();
        RenderToTilemap();
    }
}
