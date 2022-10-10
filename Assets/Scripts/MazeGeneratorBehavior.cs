using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public abstract class MazeGeneratorBehavior : MonoBehaviour
{
    [HideInInspector] public Tilemap tilemap;
    private void Reset()
    {
        tilemap = GetComponent<Tilemap>();
    }

    public virtual void Clear(TileBase fillWith)
    {
        tilemap.CompressBounds();
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            tilemap.SetTile(pos, null);
            tilemap.SetTile(pos, fillWith);
        }
    }

    public abstract void Generate();
}
