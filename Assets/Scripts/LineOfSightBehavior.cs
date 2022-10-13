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
                dst = hits[0].point;
            }
            Debug.DrawLine(src, dst, Color.white);
            for (float d = 0; d <= (dst - src).magnitude + grid.cellSize.x/2f; d += grid.cellSize.x / 2f)
            {
                Vector3 pt = src + d * dir;
                Vector3Int cellPt = grid.WorldToCell(pt);
                if (!revealed.Contains(cellPt))
                {
                    Vector3 corner1 = grid.CellToWorld(cellPt);
                    Vector3 corner2 = corner1 + grid.cellSize;
                    var newMask = (GameObject)PrefabUtility.InstantiatePrefab(spriteMaskPrefab);
                    newMask.transform.parent = transform;
                    newMask.transform.position = (corner1 + corner2) / 2f;
                    newMask.transform.localScale = (corner2 - corner1);
                    revealed.Add(cellPt);
                }
            }
        }
    }
}
