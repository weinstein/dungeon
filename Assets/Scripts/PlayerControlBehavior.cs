using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerControlBehavior : MonoBehaviour
{
    private Vector3Int targetCell;
    private Vector3Int prevCell;
    private float tolerance;
    private float speed;
    public float cellSpeed;

    public Grid grid;

    [HideInInspector] new public SpriteRenderer renderer;
    private void Reset()
    {
        renderer = GetComponent<SpriteRenderer>();
    }
    
    void Start()
    {
        tolerance = 1e-3f;
        speed = Mathf.Max(grid.cellSize.x, grid.cellSize.y) * cellSpeed;
        targetCell = CurrentCell();
        prevCell = targetCell;
    }

    Vector3Int CurrentCell()
    {
        return grid.WorldToCell(transform.position);
    }

    Vector3 Target()
    {
        return grid.GetCellCenterWorld(targetCell);
    }

    bool IsMoving()
    {
        return (Target() - transform.position).sqrMagnitude > tolerance;
    }

    bool RayCast(Vector3Int src, Vector3Int dst)
    {
        Vector3 origin = grid.CellToWorld(src) + 0.5f * grid.cellSize;
        Vector3 dir = (grid.CellToWorld(dst) + 0.5f * grid.cellSize ) - origin;
        Debug.DrawRay(origin, dir, Color.red, 1.0f);
        List<RaycastHit2D> results = new();
        ContactFilter2D filter = new();
        filter.NoFilter();
        int hits = Physics2D.Raycast(origin, dir.normalized, filter, results, dir.magnitude);
        foreach (RaycastHit2D result in results)
        {
            if (!result.collider.CompareTag("Player") && result.fraction > 0)
            {
                return true;
            }
        }
        return false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        targetCell = prevCell;
    }

    void InputDirection()
    {
        Vector3Int next = CurrentCell();
        if (Input.GetKeyDown(KeyCode.A))
        {
            next += Vector3Int.left;
            renderer.flipX = true;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            next += Vector3Int.right;
            renderer.flipX = false;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            next += Vector3Int.up;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            next += Vector3Int.down;
        }

        targetCell = next;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsMoving())
        {
            prevCell = targetCell;
            InputDirection();
        } else
        {
            Vector3 delta = Target() - transform.position;
            float dist = speed * Time.deltaTime;
            if (delta.magnitude <= dist)
            {
                transform.position += delta;
            } else
            {
                transform.position += delta.normalized * dist;
            }
        }
    }
}
