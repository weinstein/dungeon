using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerControlBehavior : MonoBehaviour
{
    private Vector3Int targetCell;
    private float tolerance;
    private float speed;
    public float cellSpeed;

    private Grid grid;
    public Tilemap walls;

    [HideInInspector] new public SpriteRenderer renderer;
    private void Reset()
    {
        renderer = GetComponent<SpriteRenderer>();
    }

    // Start is called before the first frame update
    void Start()
    {
        grid = walls.layoutGrid;
        tolerance = 1e-6f;
        speed = Mathf.Max(grid.cellSize.x, grid.cellSize.y) * cellSpeed;
        targetCell = CurrentCell();
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

    void InputDirection()
    {
        Vector3Int next = CurrentCell();
        if (Input.GetKeyDown(KeyCode.A))
        {
            next += Vector3Int.left;
            renderer.flipX = true;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            next += Vector3Int.right;
            renderer.flipX = false;
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            next += Vector3Int.up;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            next += Vector3Int.down;
        }
        if (walls.GetTile(next) == null)
        {
            targetCell = next;
        } else
        {
            // collision!
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsMoving())
        {
            InputDirection();
        } else
        {
            //transform.position = Vector3.Lerp(transform.position, Target(), speed * Time.deltaTime);
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
