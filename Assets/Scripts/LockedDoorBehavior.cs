using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(AudioSource))]
public class LockedDoorBehavior : MonoBehaviour
{
    public Sprite openSprite;
    public float destroyDelaySec = 1.0f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            InventoryData inv = InventoryData.instance;
            if (inv.Contains("key", 1))
            {
                inv.Remove("key", 1);
                GetComponent<AudioSource>().Play();
                GetComponent<SpriteRenderer>().sprite = openSprite;
                GetComponent<Collider2D>().enabled = false;
                StartCoroutine(SelfDestruct());
            }
        }
    }

    IEnumerator SelfDestruct()
    {
        yield return new WaitForSeconds(destroyDelaySec);
        Destroy(gameObject);
    }
}
