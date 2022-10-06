using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickupBehavior : MonoBehaviour
{
    new public string name;
    public int count = 1;
    private bool isEnabled = true;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isEnabled && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("player got " + name + " x" + count);
            isEnabled = false;
            Destroy(gameObject);
        }
    }
}
