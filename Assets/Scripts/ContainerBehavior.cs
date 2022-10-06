using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContainerBehavior : MonoBehaviour
{
    public List<ItemDescriptor> contents = new();
    public float spawnRadius = 4f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (contents.Count > 0 && collision.gameObject.CompareTag("Player"))
        {
            foreach (ItemDescriptor desc in contents)
            {
                float angle = Random.Range(0, 2 * Mathf.PI);
                float radius = spawnRadius * Mathf.Sqrt(Random.Range(0f, 1f));
                float dx = radius * Mathf.Cos(angle);
                float dy = radius * Mathf.Sin(angle);
                Vector3 pos = transform.position;
                pos.x += dx;
                pos.y += dy;
                GameObject o = Instantiate(desc.prefab, pos, transform.rotation);
            }
            contents.Clear();
            Destroy(gameObject);
        }
    }
}
