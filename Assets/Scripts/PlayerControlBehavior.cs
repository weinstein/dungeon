using System;
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
        ResetPosition(transform.position);
    }

    Vector3Int CurrentCell()
    {
        return grid.WorldToCell(transform.position);
    }

    Vector3 Target()
    {
        return grid.GetCellCenterWorld(targetCell);
    }

    public void ResetPosition(Vector3 x)
    {
        transform.position = x;
        targetCell = CurrentCell();
        prevCell = targetCell;
    }

    bool IsMoving()
    {
        return (Target() - transform.position).sqrMagnitude > tolerance;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        targetCell = prevCell;
    }

    void InputDirection()
    {
        Vector3Int next = CurrentCell();
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h < 0)
        {
            next += Vector3Int.left;
            renderer.flipX = true;
        }
        else if (h > 0)
        {
            next += Vector3Int.right;
            renderer.flipX = false;
        }
        else if (v > 0)
        {
            next += Vector3Int.up;
        }
        else if (v < 0)
        {
            next += Vector3Int.down;
        }

        targetCell = next;
    }

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
