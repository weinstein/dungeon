using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(AudioSource))]
public class ChestBehavior : MonoBehaviour
{
    public List<GameObject> contents = new();
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

    [HideInInspector] public AudioSource audioSrc;
    public AudioClip hitSfx;
    public AudioClip dispenseSfx;

    [HideInInspector] public new SpriteRenderer renderer;
    private void Reset()
    {
        renderer = GetComponent<SpriteRenderer>();
        audioSrc = GetComponent<AudioSource>();
    }

    void Open()
    {
        renderer.sprite = openSprite;
        state = State.OPEN;
        audioSrc.PlayOneShot(hitSfx);
    }

    void Empty()
    {
        renderer.sprite = emptySprite;
        state = State.EMPTY;
        audioSrc.PlayOneShot(dispenseSfx);

        foreach (GameObject prefab in contents)
        {
            // TODO: should pick an empty grid cell within the radius
            float angle = Random.Range(0, 2 * Mathf.PI);
            float radius = spawnRadius * Mathf.Sqrt(Random.Range(0f, 1f));
            float dx = radius * Mathf.Cos(angle);
            float dy = radius * Mathf.Sin(angle);
            Vector3 pos = transform.position;
            pos.x += dx;
            pos.y += dy;
            GameObject o = Instantiate(prefab);
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
