using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class RoomMazeGenerator : MonoBehaviour
{
    public int minWidth = 3;
    public int maxWidth = 6;
    public int minHeight = 3;
    public int maxHeight = 6;
    public int wallHeight = 1;
    public int padding = 2;
    public int connectionDist = 5;

    public int numRooms = 3;

    public TileBase floor;
    public TileBase wall;
    public TileBase empty;

    public class Room
    {
        public BoundsInt tileBounds;
        public BoundsInt floorBounds;
        public List<Room> neighbors = new();
    }
    List<Room> rooms = new();

    List<List<Vector3Int>> corridors = new();

    [HideInInspector] public Tilemap tilemap;
    private void Reset()
    {
        tilemap = GetComponent<Tilemap>();
    }

    public void Clear()
    {
        rooms.Clear();
        corridors.Clear();
        tilemap.CompressBounds();
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            tilemap.SetTile(pos, empty);
        }
    }

    BoundsInt RandomRect(int wMin, int wMax, int hMin, int hMax)
    {
        int width = Random.Range(wMin, wMax + 1);
        int height = Random.Range(hMin, hMax + 1);
        int xMin = Random.Range(tilemap.cellBounds.xMin + padding, tilemap.cellBounds.xMax - padding - width + 1);
        int yMin = Random.Range(tilemap.cellBounds.yMin + padding, tilemap.cellBounds.yMax - padding - height + 1);
        BoundsInt ret = new();
        ret.xMin = xMin;
        ret.xMax = xMin + width;
        ret.yMin = yMin;
        ret.yMax = yMin + height;
        ret.zMin = 0;
        ret.zMax = 1;
        return ret;
    }

    Room FindRoomOverlapping(BoundsInt b, int padding)
    {
        foreach (Room existing in rooms)
        {
            bool outside = existing.tileBounds.xMin >= b.xMax + padding ||
                existing.tileBounds.xMax <= b.xMin - padding ||
                existing.tileBounds.yMin >= b.yMax + padding ||
                existing.tileBounds.yMax <= b.yMin - padding;
            if (!outside)
            {
                return existing;
            }
        }
        return null;
    }

    Room NextRoom(int maxTries)
    {
        Room room = null;
        for (int attempt = 0; attempt < maxTries; ++attempt)
        {
            BoundsInt b = RandomRect(minWidth, maxWidth, minHeight + wallHeight, maxHeight + wallHeight);
            if (FindRoomOverlapping(b, padding) == null)
            {
                room = new();
                room.tileBounds = b;
                room.floorBounds = b;
                room.floorBounds.yMax -= wallHeight;
                break;
            }
        }
        return room;
    }

    static Vector3Int Round(Vector3 x)
    {
        Vector3Int ret = new();
        ret.x = Mathf.RoundToInt(x.x);
        ret.y = Mathf.RoundToInt(x.y);
        ret.z = Mathf.RoundToInt(x.z);
        return ret;
    }

    void DrawCorridor(Room a, Room b)
    {
        Vector3Int src = Round(a.floorBounds.center);
        Vector3Int dst = Round(b.floorBounds.center);
        bool horizontal = Random.Range(0, 2) > 0;
        Vector3Int dir;
        if (horizontal)
        {
            dir = dst.x > src.x ? Vector3Int.right : Vector3Int.left;
        } else
        {
            dir = dst.y > src.y ? Vector3Int.up : Vector3Int.down;
        }
        DrawCorridor(src, dst, dir);
    }

    void DrawCorridor(Vector3Int src, Vector3Int dst, Vector3Int startingDir)
    {
        Debug.Log("DrawCorridor(" + src + ", " + dst + ", " + startingDir + ")");
        Vector3Int cur = src;
        Vector3Int dir = startingDir;
        for (; cur != dst; cur += dir)
        {
            tilemap.SetTile(cur, floor);
            //Vector3Int above = cur + Vector3Int.up;
            //if (dir.x != 0 && dir.y == 0 && rooms.Find(x => x.tileBounds.Contains(above)) == null)
            //{
            //    tilemap.SetTile(above, wall);
            //}

            Vector3Int delta = dst - cur;
            if (dir.x != 0 && delta.x == 0)
            {
                dir.x = 0;
                dir.y = delta.y > 0 ? 1 : -1;
            }
            else if (dir.y != 0 && delta.y == 0)
            {
                dir.y = 0;
                dir.x = delta.x > 0 ? 1 : -1;
            }
        }
    }

    static int ManhattanDistance(BoundsInt a, BoundsInt b)
    {
        int dx = 0;
        if (a.xMin >= b.xMax)
        {
            dx = a.xMin - b.xMax;
        } else if (a.xMax <= b.xMin)
        {
            dx = b.xMin - a.xMax;
        }

        int dy = 0;
        if (a.yMin >= b.yMax)
        {
            dy = a.yMin - b.yMax;
        } else if (a.yMax <= b.yMin)
        {
            dy = b.yMin - a.yMax;
        }

        return dx + dy;
    }

    static int ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        Vector3Int x = a - b;
        return Mathf.Abs(x.x) + Mathf.Abs(x.y);
    }

    static Vector3Int[] Adjacent(Vector3Int x)
    {
        return new Vector3Int[]{ x + Vector3Int.up,
                x + Vector3Int.down,
                x + Vector3Int.left,
                x + Vector3Int.right};
    }

    static List<Vector3Int> FindPath(Vector3Int src, Vector3Int dst, HashSet<Vector3Int> forbidden)
    {
        HashSet<Vector3Int> openSet = new();
        openSet.Add(src);
        Dictionary<Vector3Int, Vector3Int> predecessor = new();
        Dictionary<Vector3Int, int> score = new();
        score.Add(src, 0);
        Dictionary<Vector3Int, int> estScore = new();
        estScore.Add(src, ManhattanDistance(src, dst));
        while (openSet.Count > 0)
        {
            Vector3Int cur = new();
            int curEstScore = int.MaxValue;
            foreach (Vector3Int x in openSet)
            {
                if (estScore[x] < curEstScore)
                {
                    cur = x;
                    curEstScore = estScore[x];
                }
            }

            if (cur == dst)
            {
                List<Vector3Int> ret = new();
                ret.Add(cur);
                while (cur != src)
                {
                    cur = predecessor[cur];
                    ret.Insert(0, cur);
                }
                return ret;
            }

            openSet.Remove(cur);
            foreach (Vector3Int neighbor in Adjacent(cur))
            {
                if (forbidden.Contains(neighbor)) continue;
                int tentativeScore = score[cur] + 1;
                if (tentativeScore < score.GetValueOrDefault(neighbor, int.MaxValue))
                {
                    predecessor[neighbor] = cur;
                    score[neighbor] = tentativeScore;
                    estScore[neighbor] = tentativeScore + ManhattanDistance(neighbor, dst);
                    openSet.Add(neighbor);
                }
            }
        }
        return null;
    }

    List<Vector3Int> FindPath(Room a, Room b)
    {
        Vector3Int src = Round(a.floorBounds.center);
        Vector3Int dst = Round(b.floorBounds.center);
        HashSet<Vector3Int> forbidden = new();
        foreach (Room room in rooms)
        {
            if (room == a || room == b) continue;
            BoundsInt bounds = room.tileBounds;
            bounds.xMin -= padding;
            bounds.yMin -= padding;
            bounds.xMax += padding;
            bounds.yMax += padding;
            foreach (Vector3Int x in bounds.allPositionsWithin)
            {
                forbidden.Add(x);
            }
        }
        foreach (List<Vector3Int> corridor in corridors)
        {
            foreach (Vector3Int x in corridor) forbidden.Add(x);
        }
        return FindPath(src, dst, forbidden);
    }

    public void Generate()
    {
        Clear();
        for (int i = 0; i < numRooms; ++i)
        {
            Room next = NextRoom(128);
            if (next == null)
            {
                Debug.LogWarning("skipped a room");
            }
            else
            {
                Debug.Log("Room: " + next.floorBounds);
                rooms.Add(next);
            }
        }

        foreach (Room a in rooms)
        {
            foreach (Room b in rooms)
            {
                if (a == b) continue;
                if (ManhattanDistance(a.floorBounds, b.floorBounds) <= connectionDist && !a.neighbors.Contains(b))
                {
                    a.neighbors.Add(b);
                }
            }
        }

        foreach (Room room in rooms)
        {
            foreach (Vector3Int pos in room.tileBounds.allPositionsWithin)
            {
                if (room.floorBounds.Contains(pos))
                {
                    tilemap.SetTile(pos, floor);
                } else
                {
                    tilemap.SetTile(pos, wall);
                }
            }
        }

        HashSet<Room> visited = new();
        foreach (Room room in rooms)
        {
            foreach (Room other in room.neighbors)
            {
                if (room != other && !visited.Contains(other))
                {
                    List<Vector3Int> corridor = FindPath(room, other);
                    corridors.Add(corridor);
                    foreach (Vector3Int x in corridor)
                    {
                        tilemap.SetTile(x, floor);
                    }
                }
            }
            visited.Add(room);
        }
    }
}
