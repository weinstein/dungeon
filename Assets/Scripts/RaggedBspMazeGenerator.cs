using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class RaggedBspMazeGenerator : MonoBehaviour
{
    public TileBase wall;
    public TileBase floor;

    [HideInInspector] public Tilemap tilemap;
    private void Reset()
    {
        tilemap = GetComponent<Tilemap>();
    }


    public void EraseAll()
    {
        tilemap.CompressBounds();
        Vector3Int pos = new Vector3Int();
        for (pos.x = tilemap.cellBounds.xMin; pos.x < tilemap.cellBounds.xMax; ++pos.x)
        {
            for (pos.y = tilemap.cellBounds.yMin; pos.y < tilemap.cellBounds.yMax; ++pos.y)
            {
                tilemap.SetTile(pos, floor);
            }
        }
    }

    static void RandomShuffle<T>(List<T> data)
    {
        for (int i = data.Count - 1; i >= 1; --i)
        {
            int j = Random.Range(0, i);
            T tmp = data[i];
            data[i] = data[j];
            data[j] = tmp;
        }
    }

    static HashSet<Vector3Int> Adjacent(Vector3Int pos)
    {
        HashSet<Vector3Int> ret = new HashSet<Vector3Int>();
        ret.Add(pos + Vector3Int.left);
        ret.Add(pos + Vector3Int.right);
        ret.Add(pos + Vector3Int.up);
        ret.Add(pos + Vector3Int.down);
        return ret;
    }

    static HashSet<Vector3Int> AdjacentDiagonals(Vector3Int pos)
    {
        HashSet<Vector3Int> ret = Adjacent(pos);
        ret.Add(pos + Vector3Int.left + Vector3Int.up);
        ret.Add(pos + Vector3Int.left + Vector3Int.down);
        ret.Add(pos + Vector3Int.right + Vector3Int.up);
        ret.Add(pos + Vector3Int.right + Vector3Int.down);
        return ret;
    }

    public void GenerateImpl(int depth, HashSet<Vector3Int> tiles)
    {
        if (depth <= 0)
        {
            return;
        }
        List<Vector3Int> shuffledTiles = tiles.ToList();
        RandomShuffle(shuffledTiles);

        HashSet<Vector3Int> setA = new HashSet<Vector3Int>();
        setA.Add(shuffledTiles.Last());
        shuffledTiles.RemoveAt(shuffledTiles.Count - 1);
        HashSet<Vector3Int> setB = new HashSet<Vector3Int>();
        setB.Add(shuffledTiles.Last());
        shuffledTiles.RemoveAt(shuffledTiles.Count - 1);

        while (true)
        {
            int countBefore = shuffledTiles.Count;
            for (int i = 0; i < shuffledTiles.Count; ++i)
            {
                Vector3Int a = shuffledTiles[i];
                if (Adjacent(a).Overlaps(setA))
                {
                    setA.Add(a);
                    shuffledTiles.RemoveAt(i);
                    break;
                }
            }
            for (int i = 0; i < shuffledTiles.Count; ++i)
            {
                Vector3Int b = shuffledTiles[i];
                if (Adjacent(b).Overlaps(setB))
                {
                    setB.Add(b);
                    shuffledTiles.RemoveAt(i);
                    break;
                }
            }
            if (shuffledTiles.Count == countBefore) break;
        }
        List<Vector3Int> newWalls = new List<Vector3Int>();
        foreach (Vector3Int x in tiles)
        {
            if (setA.Contains(x) && AdjacentDiagonals(x).Overlaps(setB))
            {
                newWalls.Add(x);
            } else if (setB.Contains(x) && Adjacent(x).Overlaps(setA))
            {
                //newWalls.Add(x);
            }
        }
        foreach (Vector3Int x in newWalls)
        {
            tilemap.SetTile(x, wall);
            setA.Remove(x);
            setB.Remove(x);
        }
        GenerateImpl(depth - 1, setA);
        GenerateImpl(depth - 1, setB);
    }

    public void Generate(int depth)
    {
        EraseAll();
        HashSet<Vector3Int> allTiles = new HashSet<Vector3Int>();
        tilemap.CompressBounds();
        for (int x = tilemap.cellBounds.xMin; x < tilemap.cellBounds.xMax; ++x)
        {
            for (int y = tilemap.cellBounds.yMin; y < tilemap.cellBounds.yMax; ++y)
            {
                Vector3Int pos = new Vector3Int();
                pos.x = x;
                pos.y = y;
                allTiles.Add(pos);
            }
        }
        GenerateImpl(depth, allTiles);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
