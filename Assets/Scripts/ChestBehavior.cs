using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ChestBehavior : MonoBehaviour
{
    public List<ItemDescriptor> contents = new();
    public float spawnRadius = 4f;

    public Sprite openSprite;
    public Sprite emptySprite;

    private State state = State.CLOSED;
    private enum State
    {
        CLOSED,
        OPEN,
        EMPTY,
    }

    [HideInInspector] public new SpriteRenderer renderer;
    private void Reset()
    {
        renderer = GetComponent<SpriteRenderer>();
    }

    void Open()
    {
        renderer.sprite = openSprite;
        state = State.OPEN;
    }

    void Empty()
    {
        renderer.sprite = emptySprite;
        state = State.EMPTY;

        foreach (ItemDescriptor desc in contents)
        {
            float angle = Random.Range(0, 2 * Mathf.PI);
            float radius = spawnRadius * Mathf.Sqrt(Random.Range(0f, 1f));
            float dx = radius * Mathf.Cos(angle);
            float dy = radius * Mathf.Sin(angle);
            Vector3 pos = transform.position;
            pos.x += dx;
            pos.y += dy;
            GameObject o = Instantiate(desc.prefab);
            o.transform.parent = transform;
            o.transform.position = pos;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (state == State.CLOSED) Open();
            else if (state == State.OPEN) Empty();
        }
    }
}
