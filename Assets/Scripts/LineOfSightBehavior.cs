using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LineOfSightBehavior : MonoBehaviour
{
    public GameObject player;
    public GameObject spriteMaskPrefab;
    public Grid grid;
    public int numRays = 64;
    public float viewDistance = 64f;

    private HashSet<Vector3Int> revealed = new();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    static List<Vector3Int> PlotLine(Vector3Int src, Vector3Int dst)
    {
        int dx = Math.Abs(src.x - dst.x);
        int sx = src.x < dst.x ? 1 : -1;
        int dy = -Math.Abs(src.y - dst.y);
        int sy = src.y < dst.y ? 1 : -1;
        int error = dx + dy;
        List<Vector3Int> outputs = new();
        while (true)
        {
            outputs.Add(src);
            if (src.x == dst.x && src.y == dst.y) return outputs;
            int e2 = 2 * error;
            if (e2 >= dy)
            {
                if (src.x == dst.x) return outputs;
                error += dy;
                src.x += sx;
            }
            if (e2 <= dx)
            {
                if (src.y == dst.y) return outputs;
                error += dx;
                src.y += sy;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (float angle = 0; angle < 2 * Mathf.PI; angle += 2 * Mathf.PI / numRays)
        {
            Vector3 src = player.transform.position;
            Vector3 dir = Vector3.zero;
            dir.x = Mathf.Cos(angle);
            dir.y = Mathf.Sin(angle);
            var filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = false;
            List<RaycastHit2D> hits = new();
            Physics2D.Raycast(src, dir, filter, hits, viewDistance);
            for (int i = hits.Count; i --> 0;)
            {
                if (hits[i].collider.CompareTag("Player") || hits[i].fraction <= 0) hits.RemoveAt(i);
            }
            Vector3 dst;
            if (hits.Count == 0)
            {
                dst = src + viewDistance * dir;
                
            } else
            {
                dst = (Vector3)hits[0].point + dir * 0.01f;
            }
            Debug.DrawLine(src, dst, Color.white);
            foreach (Vector3Int cellPos in PlotLine(grid.WorldToCell(src), grid.WorldToCell(dst)))
            {
                if (!revealed.Contains(cellPos))
                {
                    Vector3 corner1 = grid.CellToWorld(cellPos);
                    Vector3 corner2 = corner1 + grid.cellSize;
                    var newMask = (GameObject)PrefabUtility.InstantiatePrefab(spriteMaskPrefab);
                    newMask.transform.parent = transform;
                    newMask.transform.position = (corner1 + corner2) / 2f;
                    newMask.transform.localScale = (corner2 - corner1);
                    revealed.Add(cellPos);
                }
            }
        }
    }
}
