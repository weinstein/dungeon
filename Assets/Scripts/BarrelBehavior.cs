using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BarrelBehavior : MonoBehaviour
{
    public List<ItemDescriptor> contents = new();
    public float spawnRadius = 4f;

    private bool opened = false;
    public Sprite emptySprite;
    public float destroyDelaySec = 1.0f;

    [HideInInspector] public AudioSource audioSrc;

    private void Reset()
    {
        audioSrc = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (!opened)
            {
                audioSrc.Play();
                foreach (ItemDescriptor desc in contents)
                {
                    // TODO: should pick an empty grid cell within the radius
                    float angle = Random.Range(0, 2 * Mathf.PI);
                    float radius = spawnRadius * Mathf.Sqrt(Random.Range(0f, 1f));
                    float dx = radius * Mathf.Cos(angle);
                    float dy = radius * Mathf.Sin(angle);
                    Vector3 pos = transform.position;
                    pos.x += dx;
                    pos.y += dy;
                    GameObject o = Instantiate(desc.prefab, pos, transform.rotation);
                }
                opened = true;
                GetComponent<SpriteRenderer>().sprite = emptySprite;
                GetComponent<Collider2D>().enabled = false;
                StartCoroutine(Delete());
            }
        }
    }

    IEnumerator Delete()
    {
        yield return new WaitForSeconds(destroyDelaySec);
        Destroy(gameObject);
    }
}
