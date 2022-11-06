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

    List<Vector3Int> PossibleSpawnPositions(GraphMazeGenerator mazeGen)
    {
        Vector3Int center = mazeGen.tilemap.WorldToCell(transform.position);
        List<Vector3Int> choices = new();
        for (int dx = -Mathf.FloorToInt(spawnRadius); dx <= Mathf.CeilToInt(spawnRadius); ++dx)
        {
            for (int dy = -Mathf.FloorToInt(spawnRadius); dy <= Mathf.CeilToInt(spawnRadius); ++dy)
            {
                Vector3Int cellPos = center + new Vector3Int(dx, dy);
                if (dx * dx + dy * dy > spawnRadius * spawnRadius) continue;
                if (mazeGen.CellPosIsOutOfBounds(cellPos)) continue;
                if (mazeGen.FindClutterAtCellPos(cellPos) != null) continue;
                choices.Add(cellPos);
            }
        }
        return choices;
    }

    void Empty()
    {
        renderer.sprite = emptySprite;
        state = State.EMPTY;
        audioSrc.PlayOneShot(dispenseSfx);

        GraphMazeGenerator mazeGen = transform.parent.GetComponent<GraphMazeGenerator>();
        List<Vector3Int> choices = PossibleSpawnPositions(mazeGen);
        RandomUtil.Shuffle(choices);
        for (int i = 0; i < contents.Count && i < choices.Count; ++i)
        {
            GameObject prefab = contents[i];
            Vector3Int cellPos = choices[i];
            mazeGen.SpawnClutterAtCellPos(cellPos, prefab);
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
