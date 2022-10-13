using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
    public int corridorStraightness = 4;

    public int numClutterMin = 0;
    public int numClutterMax = 2;
    [Serializable]
    public struct WeightedObject
    {
        public GameObject o;
        public int weight;
    }
    public List<WeightedObject> clutterTable = new();

    public GameObject exit;

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
        for (int i = transform.childCount; i-- > 0;)
        {
            GameObject o = transform.GetChild(i).gameObject;
            Debug.Log("destroy child " + o.name);
            if (!Application.isPlaying) DestroyImmediate(o);
            else Destroy(o);
        }
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

    static void RandomShuffle<T>(List<T> elems)
    {
        for (int i = elems.Count - 1; i >= 1; --i)
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
    static int ManhattanDistance(Vector3Int p, BoundsInt b)
    {
        int xDist = 0;
        if (p.x >= b.xMax) xDist = p.x - b.xMax + 1;
        else if (p.x < b.xMin) xDist = b.xMin - p.x;
        int yDist = 0;
        if (p.y >= b.yMax) yDist = p.y - b.yMax + 1;
        else if (p.y < b.yMin) yDist = b.yMin - p.y;
        int zDist = 0;
        if (p.z >= b.zMax) zDist = p.z - b.zMax + 1;
        else if (p.z < b.zMin) zDist = b.zMin - p.z;
        return xDist + yDist + zDist;
    }

    static int LInfDistance(Vector3Int p, BoundsInt b)
    {
        int xDist = 0;
        if (p.x >= b.xMax) xDist = p.x - b.xMax + 1;
        else if (p.x < b.xMin) xDist = b.xMin - p.x;
        int yDist = 0;
        if (p.y >= b.yMax) yDist = p.y - b.yMax + 1;
        else if (p.y < b.yMin) yDist = b.yMin - p.y;
        int zDist = 0;
        if (p.z >= b.zMax) zDist = p.z - b.zMax + 1;
        else if (p.z < b.zMin) zDist = b.zMin - p.z;
        return Math.Max(xDist, Math.Max(yDist, zDist));
    }

    static bool CameFromDirIsStraight(Vector3Int p, Dictionary<Vector3Int, Vector3Int> cameFrom)
    {
        if (!cameFrom.ContainsKey(p)) return true;
        Vector3Int p1 = cameFrom[p];
        if (!cameFrom.ContainsKey(p1)) return true;
        Vector3Int p2 = cameFrom[p1];
        return (p - p1) == (p1 - p2);
    }

    List<Vector3Int> CorridorSearch(Room srcRoom, Room dstRoom)
    {
        BoundsInt src = srcRoom.UnpadBounds(padding);
        BoundsInt dst = dstRoom.UnpadBounds(padding);

        List<Vector3Int> candidates = new();
        Dictionary<Vector3Int, int> gScore = new();
        Dictionary<Vector3Int, int> fScore = new();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new();
        foreach (Vector3Int pos in src.allPositionsWithin)
        {
            candidates.Add(pos);
            gScore[pos] = 0;
            fScore[pos] = LInfDistance(pos, dst);
        }
        while (candidates.Count > 0)
        {
            candidates.Sort((x, y) => {
                int yF = fScore.GetValueOrDefault(y, 9999)/corridorStraightness;
                int xF = fScore.GetValueOrDefault(x, 9999)/corridorStraightness;
                if (xF != yF) return yF - xF;
                bool xStraight = CameFromDirIsStraight(x, cameFrom);
                bool yStraight = CameFromDirIsStraight(y, cameFrom);
                if (yStraight && !xStraight) return -1;
                if (xStraight && !yStraight) return 1;
                return 0;
            });
            Vector3Int cur = candidates[candidates.Count - 1];
            candidates.RemoveAt(candidates.Count - 1);
            if (dst.Contains(cur))
            {
                List<Vector3Int> ret = new() { cur };
                while (!src.Contains(cur))
                {
                    cur = cameFrom[cur];
                    ret.Insert(0, cur);
                }
                return ret;
            }
            foreach (Vector3Int dir in allDirs)
            {
                Vector3Int neighbor = cur + dir;
                
                if (!tilemap.cellBounds.Contains(neighbor)) continue;
                bool disallowed = false;
                for (int dx = -1; dx <= 1; ++dx)
                {
                    for (int dy = -1; dy <= 1; ++dy)
                    {
                        Vector3Int x = neighbor;
                        x.x += dx;
                        x.y += dy;
                        bool diagonal = dx != 0 && dy != 0;
                        bool isEmpty = tilemap.GetTile(x) == emptyTile;
                        bool isSrcWall = tilemap.GetTile(x) == wallTile && src.Contains(x);
                        bool isDstWall = tilemap.GetTile(x) == wallTile && dst.Contains(x);
                        disallowed |= (isSrcWall && !diagonal && dir != Vector3Int.up)
                            || (isDstWall && !diagonal && dir != Vector3Int.down)
                            || (!src.Contains(x) && !dst.Contains(x) && !isEmpty);
                    }
                }
                if (disallowed) continue;

                int tentativeScore = gScore[cur] + 1;
                if (tentativeScore/corridorStraightness < gScore.GetValueOrDefault(neighbor, 9999)/corridorStraightness)
                {
                    cameFrom[neighbor] = cur;
                    gScore[neighbor] = tentativeScore;
                    fScore[neighbor] = tentativeScore + ManhattanDistance(neighbor, dst);
                    candidates.Add(neighbor);
                }
            }
        }
        return null;
    }

    static Vector3Int[] allDirs = new Vector3Int[] { Vector3Int.left, Vector3Int.down, Vector3Int.right, Vector3Int.up };

    bool RenderCorridorBetween(Room r1, Room r2)
    {
        List<Vector3Int> path = CorridorSearch(r1, r2);
        if (path == null) return false;
        path.ForEach(p => RenderTile(p, floorTile));
        return true;
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
        List<ValueTuple<Room, Room>> unpathable = new();
        for (int i = 1; i < criticalPath.Count; ++i)
        {
            Room r1 = criticalPath[i - 1];
            Room r2 = criticalPath[i];
            if (!RenderCorridorBetween(r1, r2)) unpathable.Add((r1, r2));
        }        
        roomGraph.TraverseDepthFirst(r => { }, (src, dst, len) => {
            if (criticalPath.Contains(src) && criticalPath.Contains(dst)) return;
            if (!RenderCorridorBetween(src, dst))
            {
                unpathable.Add((src, dst));
            }
        });
        foreach (ValueTuple<Room, Room> e in unpathable)
        {
            Debug.LogError("unpathable rooms: " + e.Item1.bounds + " => " + e.Item2.bounds);
            roomGraph.RemoveUndirected(e.Item1, e.Item2);
        }
        PruneUnreachableRooms(criticalPath[0]);

        Room startingRoom = criticalPath[0];
        RenderTile(Center(startingRoom.UnpadFloorBounds(padding, wallHeight)), startTile);
        
        Room endingRoom = criticalPath[criticalPath.Count - 1];
        Vector3Int endingCell = Center(endingRoom.UnpadFloorBounds(padding, wallHeight));
        //RenderTile(endingCell, endTile);
        var nextExit = (GameObject)PrefabUtility.InstantiatePrefab(exit);
        nextExit.transform.parent = transform;
        nextExit.transform.position = tilemap.CellToWorld(endingCell) + 0.5f * tilemap.cellSize;
        nextExit.SetActive(true);

        tilemap.RefreshAllTiles();
    }

    private DistanceGraph<Vector2> triangulation = new();
    private List<Room> criticalPath = new();

    void DebugDrawLine(Vector2 gridPt1, Vector2 gridPt2, Color c) {
        Vector3 worldPos1 = tilemap.CellToWorld(Vector3Int.zero) + new Vector3(gridPt1.x * tilemap.cellSize.x, gridPt1.y * tilemap.cellSize.y);
        Vector3 worldPos2 = tilemap.CellToWorld(Vector3Int.zero) + new Vector3(gridPt2.x * tilemap.cellSize.x, gridPt2.y * tilemap.cellSize.y);
        Debug.DrawLine(worldPos1, worldPos2, c);
    }

    void DebugDrawArrow(Vector2 gridPt1, Vector2 gridPt2, Color c) {
        Vector3 worldPos1 = tilemap.CellToWorld(Vector3Int.zero) + new Vector3(gridPt1.x * tilemap.cellSize.x, gridPt1.y * tilemap.cellSize.y);
        Vector3 worldPos2 = tilemap.CellToWorld(Vector3Int.zero) + new Vector3(gridPt2.x * tilemap.cellSize.x, gridPt2.y * tilemap.cellSize.y);
        Debug.DrawRay(worldPos1, worldPos2 - worldPos1, c);
    }

    public void OnDrawGizmos()
    {
        triangulation.ForEachEdge((p1, p2, d) =>
        {
            DebugDrawArrow(p1, p2, Color.gray);
        });
        roomGraph.ForEachEdge((r1, r2, d) => {
            DebugDrawArrow(r1.bounds.center, r2.bounds.center, Color.green);
        });
        for (int i = 1; i < criticalPath.Count; ++i)
        {
            Room r1 = criticalPath[i - 1];
            Room r2 = criticalPath[i];
            Color c = (roomGraph.ContainsEdge(r1, r2) || roomGraph.ContainsEdge(r2, r1)) ? Color.blue : Color.red;
            DebugDrawArrow(r1.bounds.center, r2.bounds.center, c);
        }
    }

    GameObject RandomClutter()
    {
        int totalWeight = 0;
        foreach (WeightedObject elem in clutterTable) totalWeight += elem.weight;
        int r = Random.Range(0, totalWeight);
        foreach (WeightedObject elem in clutterTable)
        {
            if (r < elem.weight) return elem.o;
            r -= elem.weight;
        }
        return null;
    }

    void PopulateWithClutter(Room r)
    {
        List<Vector3Int> options = new();
        foreach (Vector3Int pos in r.UnpadFloorBounds(padding, wallHeight).allPositionsWithin)
        {
            if (tilemap.GetTile(pos) == floorTile) options.Add(pos);
        }
        RandomShuffle(options);
        int n = Random.Range(numClutterMin, numClutterMax + 1);
        for (int i = 0; i < n && i < options.Count; ++i)
        {
            GameObject o = RandomClutter();
            Vector3Int pos = options[i];
            Vector3 worldPos = tilemap.CellToWorld(pos) + 0.5f * tilemap.cellSize;
            var item = (GameObject)PrefabUtility.InstantiatePrefab(o);
            item.transform.parent = transform;
            item.transform.transform.position = worldPos;
            item.SetActive(true);
        }
    }

    void PruneUnreachableRooms(Room start)
    {
        HashSet<Room> unreachable = new();
        rooms.ForEach(r => unreachable.Add(r));
        roomGraph.TraverseBreadthFirst(start, r => unreachable.Remove(r), (_, _, _) => { });
        foreach (Room r in unreachable)
        {
            rooms.Remove(r);
            roomGraph.Remove(r);
            foreach (Vector3Int pos in r.bounds.allPositionsWithin)
            {
                RenderTile(pos, emptyTile);
            }
        }
        if (unreachable.Count > 0) Debug.LogWarning("pruned " + unreachable.Count + " unreachable rooms");
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
        rooms.ForEach(PopulateWithClutter);
    }

    public Vector3 StartingPosition()
    {
        Vector3Int cell = Center(criticalPath[0].UnpadFloorBounds(padding, wallHeight));
        return tilemap.CellToWorld(cell) + tilemap.cellSize * 0.5f;
    }
}
