using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BarrelBehavior : MonoBehaviour
{
    public GameObject contents;

    private bool opened = false;
    public Sprite emptySprite;
    public float destroyDelaySec = 1.0f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !opened)
        {
            GetComponent<AudioSource>().Play();

            if (contents != null)
            {
                GraphMazeGenerator mazeGen = transform.parent.GetComponent<GraphMazeGenerator>();
                mazeGen.RemoveClutterAtWorldPos(transform.position);
                mazeGen.SpawnClutterAtWorldPos(transform.position, contents);
            }

            opened = true;
            GetComponent<SpriteRenderer>().sprite = emptySprite;
            GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject, destroyDelaySec);
        }
    }
}
