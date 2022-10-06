using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class RandomWalkMazeGenerator : MazeGeneratorBehavior
{
    public TileBase floorTile;
    public TileBase wallTile;
    public TileBase emptyTile;

    public int wallHeight = 1;
    public int padding = 1;
    public int minRoomSize = 3;
    public int maxRoomSize = 5;
    public int minCorridorSize = 2;
    public int maxCorridorSize = 3;
    public int numCorridorSegments = 1;
    public int numSteps = 3;

    static BoundsInt Shrink(BoundsInt bounds, int amount)
    {
        return Shrink(bounds, amount, amount, amount, amount);
    }

    static BoundsInt Shrink(BoundsInt bounds, int x0, int x1, int y0, int y1)
    {
        BoundsInt ret = bounds;
        ret.xMin += x0;
        ret.xMax -= x1;
        ret.yMin += y0;
        ret.yMax -= y1;
        return ret;
    }

    public class Room
    {
        public Room(RandomWalkMazeGenerator parent, BoundsInt tileBounds)
        {
            //this.paddedBounds = paddedBounds;
            this.tileBounds = tileBounds;
            this.floorBounds = Shrink(this.tileBounds, 0, 0, 0, parent.wallHeight);
            this.corridorsOut = new();
            this.corridorsIn = new();
        }

        public BoundsInt floorBounds { get; }
        public BoundsInt tileBounds { get; }
        public BoundsInt paddedBounds { get; }
        public List<Corridor> corridorsOut;
        public List<Corridor> corridorsIn;
    }
    public class Corridor
    {
        public Corridor(Room source, Vector3Int sourcePos, List<Vector3Int> segments, Room destination)
        {
            this.source = source;
            this.initialPos = sourcePos;
            this.segments = segments;
            this.destination = destination;
        }

        public Vector3Int initialPos { get; }
        public Vector3Int FinalPos()
        {
            Vector3Int pos = initialPos;
            foreach (Vector3Int x in segments)
            {
                pos += x;
            }
            return pos;
        }
        public List<Vector3Int> segments { get; }
        public Room source { get; }
        public Room destination { get; }

        public Corridor Reversed()
        {
            List<Vector3Int> reverseSegments = new();
            foreach (Vector3Int x in segments)
            {
                reverseSegments.Insert(0, -x);
            }
            return new Corridor(destination, FinalPos(), reverseSegments, source);
        }
    }
    public List<Room> rooms = new();

    public override void Clear(TileBase fillWith)
    {
        rooms.Clear();
        base.Clear(fillWith);
    }

    Corridor ConnectOneWay(Room from, Vector3Int pos, List<Vector3Int> segments, Room to)
    {
        Corridor corridor = new(from, pos, segments, to);
        from.corridorsOut.Add(corridor);
        to.corridorsIn.Add(corridor);
        return corridor;
    }

    Corridor[] ConnectTwoWay(Room from, Vector3Int pos, List<Vector3Int> segments, Room to)
    {
        Corridor forward = ConnectOneWay(from, pos, segments, to);
        Corridor backward = forward.Reversed();
        to.corridorsOut.Add(backward);
        from.corridorsIn.Add(backward);
        return new Corridor[] { forward, backward };
    }

    static BoundsInt Subtract(BoundsInt lhs, BoundsInt rhs)
    {
        BoundsInt ret = lhs;
        ret.xMax = Math.Min(ret.xMax, rhs.xMin);
        ret.yMax = Math.Min(ret.yMax, rhs.yMin);
        ret.xMin = Math.Max(ret.xMin, rhs.xMax);
        ret.yMin = Math.Max(ret.yMin, rhs.yMax);
        return ret;
    }

    BoundsInt RandomRoom(Vector3Int connectedTo, Vector3Int connectionDir)
    {
        int width = Random.Range(minRoomSize, maxRoomSize + 1);
        int height = Random.Range(minRoomSize, maxRoomSize + 1);
        int x = connectedTo.x;
        int y = connectedTo.y;
        if (connectionDir.x > 0)
        {
            y -= Random.Range(1, height - 1);
        }
        else if (connectionDir.x < 0)
        {
            x -= width - 1;
            y -= Random.Range(1, height - 1);
        }
        else if (connectionDir.y > 0)
        {
            x -= Random.Range(1, width - 1);
        }
        else if (connectionDir.y < 0)
        {
            x -= Random.Range(1, width - 1);
            y -= height - 1;
        }
        else
        {
            x -= Random.Range(1, width - 1);
            y -= Random.Range(1, height - 1);
        }
        BoundsInt ret = new();
        ret.xMin = x;
        ret.yMin = y;
        ret.xMax = x + width;
        ret.yMax = y + wallHeight + height;
        ret.zMin = 0;
        ret.zMax = 1;
        Debug.Log("RandomRoom @" + connectedTo + " dir " + connectionDir + " chose: " + ret);
        return ret;
    }

    static Vector3Int[] allDirs = new Vector3Int[4] { Vector3Int.left, Vector3Int.down, Vector3Int.right, Vector3Int.up };

    static Vector3Int RandomDir()
    {
        int r = Random.Range(0, 4);
        return allDirs[r];
    }

    static void RandomPointOnEdge(BoundsInt bounds, ref Vector3Int pos, ref Vector3Int normal)
    {
        normal = Vector3Int.zero;
        bool horizontal = Random.Range(0, 2) > 0;
        if (horizontal)
        {
            normal.y = 0;
            normal.x = Random.Range(0, 2) > 0 ? 1 : -1;
            pos.x = normal.x > 0 ? bounds.xMax - 1 : bounds.xMin;
            pos.y = Random.Range(bounds.yMin + 1, bounds.yMax - 1);
        }
        else
        {
            normal.x = 0;
            normal.y = Random.Range(0, 2) > 0 ? 1 : -1;
            pos.x = Random.Range(bounds.xMin + 1, bounds.xMax - 1);
            pos.y = normal.y > 0 ? bounds.yMax - 1 : bounds.yMin;
        }
        Debug.Log("bounds " + bounds + " chose point on edge: " + pos + " dir " + normal);
    }

    static Vector3Int Round(Vector3 x)
    {
        Vector3Int ret = new();
        ret.x = Mathf.RoundToInt(x.x);
        ret.y = Mathf.RoundToInt(x.y);
        ret.z = Mathf.RoundToInt(x.z);
        return ret;
    }

    static bool IsSameDirection(Vector3Int a, Vector3Int b)
    {
        int prod = a.x * b.x + a.y * b.y;
        return prod > 0 && prod * prod == a.sqrMagnitude * b.sqrMagnitude;
    }

    static bool Intersects(BoundsInt a, BoundsInt b)
    {
        bool outside = b.xMin >= a.xMax ||
            b.yMin >= a.yMax ||
            b.xMax <= a.xMin ||
            b.yMax <= a.yMin;
        return !outside;
    }

    static BoundsInt ToBounds(Vector3Int pos, Vector3Int dir)
    {
        int xMin = Math.Min(pos.x, pos.x + dir.x);
        int xMax = Math.Max(pos.x, pos.x + dir.x);
        int yMin = Math.Min(pos.y, pos.y + dir.y);
        int yMax = Math.Max(pos.y, pos.y + dir.y);
        BoundsInt ret = new();
        ret.xMin = xMin;
        ret.xMax = xMax + 1;
        ret.yMin = yMin;
        ret.yMax = yMax + 1;
        ret.zMin = 0;
        ret.zMax = 1;
        return ret;
    }

    static List<BoundsInt> ToBounds(Vector3Int initialPos, List<Vector3Int> segs)
    {
        Vector3Int pos = initialPos;
        List<BoundsInt> ret = new();
        foreach (Vector3Int seg in segs)
        {
            ret.Add(ToBounds(pos, seg));
            pos += seg;
        }
        return ret;
    }

    static void Union(BoundsInt a, ref BoundsInt dst)
    {
        dst.xMin = Math.Min(a.xMin, dst.xMin);
        dst.yMin = Math.Min(a.yMin, dst.yMin);
        dst.xMax = Math.Max(a.xMax, dst.xMax);
        dst.yMax = Math.Max(a.yMax, dst.yMax);
    }

    static BoundsInt Union(List<BoundsInt> bs)
    {
        BoundsInt ret = bs[0];
        foreach (BoundsInt b in bs)
        {
            Union(b, ref ret);
        }
        return ret;
    }

    static bool Intersects(BoundsInt a, Vector3Int initialPos, List<Vector3Int> segments)
    {
        Vector3Int pos = initialPos;
        foreach (Vector3Int seg in segments)
        {
            if (Intersects(a, ToBounds(pos, seg))) return true;
            pos += seg;
        }
        return false;
    }

    static bool Intersects(Room a, Room b)
    {
        return Intersects(a.tileBounds, b.tileBounds);
    }

    static bool Intersects(Room a, Corridor b)
    {
        return Intersects(a.tileBounds, b.initialPos, b.segments);
    }

    static bool Intersects(Corridor a, Corridor b)
    {
        List<BoundsInt> boundsA = ToBounds(a.initialPos, a.segments);
        List<BoundsInt> boundsB = ToBounds(b.initialPos, b.segments);
        // Fast path: bounding boxes don't intersect
        if (!Intersects(Union(boundsA), Union(boundsB))) return false;

        foreach (BoundsInt x in boundsA)
        {
            foreach (BoundsInt y in boundsB)
            {
                if (Intersects(x, y)) return true;
            }
        }
        return false;
    }

    Room FindRoom(BoundsInt query)
    {
        return rooms.Find(r => Intersects(r.tileBounds, query));
    }

    Corridor FindCorridor(BoundsInt query)
    {
        foreach (Room room in rooms)
        {
            foreach (Corridor hall in room.corridorsOut)
            {
                foreach (BoundsInt x in ToBounds(hall.initialPos, hall.segments))
                {
                    if (Intersects(x, query)) return hall;
                }
            }
        }
        return null;
    }

    Room FindRoom(Room excludeSource, Vector3Int pos, ref List<Vector3Int> segments)
    {
        List<Vector3Int> pathToFound = new();
        foreach (Vector3Int segment in segments)
        {
            Room existing = rooms.Find(r => r != excludeSource && Intersects(r.tileBounds, ToBounds(pos, segment)));
            if (existing != null)
            {
                BoundsInt trimmed = ToBounds(pos, segment);
                Subtract(trimmed, Shrink(existing.tileBounds, 1));
                pathToFound.Add(new Vector3Int(trimmed.xMax - trimmed.xMin - 1, trimmed.yMax - trimmed.yMin - 1));
                segments.Clear();
                segments.AddRange(pathToFound);
                return existing;
            }
            else
            {
                pathToFound.Add(segment);
                pos += segment;
            }
        }
        return null;
    }

    Corridor FindCorridor(Vector3Int pos, List<Vector3Int> segments)
    {
        foreach (BoundsInt x in ToBounds(pos, segments))
        {
            Corridor existing = FindCorridor(x);
            if (existing != null) return existing;
        }
        return null;
    }

    bool AnyIntersections(Room x)
    {
        foreach (Room existing in rooms)
        {
            if (Intersects(x, existing)) return true;
            foreach (Corridor hall in existing.corridorsOut)
            {
                if (Intersects(x, hall)) return true;
            }
        }
        return false;
    }

    bool AnyIntersections(Corridor x)
    {
        foreach (Room existing in rooms)
        {
            if (Intersects(existing, x)) return true;
            foreach (Corridor hall in existing.corridorsOut)
            {
                if (Intersects(x, hall)) return true;
            }
        }
        return false;
    }

    void RenderToTilemap(Room room)
    {
        foreach (Vector3Int pos in room.tileBounds.allPositionsWithin)
        {
            if (room.floorBounds.Contains(pos)) tilemap.SetTile(pos, floorTile);
            else tilemap.SetTile(pos, wallTile);
        }
    }

    void RenderToTilemap(Corridor hall)
    {
        Vector3Int pos = hall.initialPos;
        foreach (Vector3Int seg in hall.segments)
        {
            foreach (Vector3Int x in ToBounds(pos, seg).allPositionsWithin)
            {
                tilemap.SetTile(x, floorTile);
            }
            pos += seg;
        }
    }

    void RenderToTilemap()
    {
        foreach (Room room in rooms)
        {
            RenderToTilemap(room);
            foreach (Corridor hall in room.corridorsOut)
            {
                RenderToTilemap(hall);
            }
        }
    }

    public override void Generate()
    {
        Clear(emptyTile);
        Vector3Int pos = Round(tilemap.cellBounds.center);
        Vector3Int dir = Vector3Int.zero;
        Room curRoom = new Room(this, RandomRoom(pos, dir));
        rooms.Add(curRoom);
        for (int step = 0; step < numSteps; ++step)
        {
            Debug.Log("pos = " + pos + " dir = " + dir);
            RandomPointOnEdge(curRoom.floorBounds, ref pos, ref dir);
            Corridor existingPath = curRoom.corridorsOut.Find(x => IsSameDirection(dir, x.segments[0]));
            if (existingPath != null)
            {
                curRoom = existingPath.destination;
                continue;
            }

            // New random corridor
            List<Vector3Int> segments = new();
            Vector3Int sourcePos = pos;
            for (int i = 0; i < numCorridorSegments; ++i)
            {
                if (i > 0)
                {
                    List<Vector3Int> choices = new();
                    foreach (Vector3Int choice in allDirs)
                    {
                        if (!IsSameDirection(-choice, dir)) choices.Add(choice);
                    }
                    int choiceIdx = Random.Range(0, choices.Count);
                    dir = choices[choiceIdx];
                }
                Vector3Int segment = dir * Random.Range(minCorridorSize, maxCorridorSize + 1);
                pos += segment;
                segments.Add(segment);
            }
            // Check if new corridor would intersect something
            Corridor existingHall = FindCorridor(sourcePos, segments);
            if (existingHall != null)
            {
                Debug.Log("step result: corridor would intersect an existing corridor. Following the original corridor.");
                curRoom = existingHall.destination;
                continue;
            }
            Room existingRoom = FindRoom(curRoom, sourcePos, ref segments);
            if (existingRoom != null)
            {
                Debug.Log("step result: corridor would intersect an existing room. Connecting a new corridor to that room.");
                ConnectTwoWay(curRoom, sourcePos, segments, existingRoom);
                curRoom = existingRoom;
                continue;
            }
            // New random room at end of corridor, if there's not one already here
            BoundsInt nextBounds = RandomRoom(pos, dir);
            existingRoom = FindRoom(Shrink(nextBounds, -1));
            if (existingRoom != null)
            {
                Debug.Log("step result: new room would intersect an existing one. Jumping to that room.");
                curRoom = existingRoom;
                continue;
            }

            Debug.Log("step result: new corridor + new room.");
            Room nextRoom = new Room(this, nextBounds);
            rooms.Add(nextRoom);
            ConnectTwoWay(curRoom, sourcePos, segments, nextRoom);
            curRoom = nextRoom;
        }
        RenderToTilemap();
    }
}