using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ItemPickupBehavior : MonoBehaviour
{
    new public string name;
    public int count = 1;
    private bool isEnabled = true;

    [HideInInspector] public AudioSource audioSrc;
    private void Reset()
    {
        audioSrc = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isEnabled && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("player got " + name + " x" + count);
            audioSrc.Play();
            InventoryData.instance.Add(name, count);
            isEnabled = false;
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Collider2D>().enabled = false;
            StartCoroutine(SelfDestruct());
        }
    }

    IEnumerator SelfDestruct()
    {
        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }
}
