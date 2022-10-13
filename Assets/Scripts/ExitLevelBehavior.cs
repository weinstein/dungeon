using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitLevelBehavior : MonoBehaviour
{
    GameObject parentTilemap;
    GraphMazeGenerator gen;
    LineOfSightBehavior lineOfSight;

    // Start is called before the first frame update
    void Start()
    {
        parentTilemap = transform.parent.gameObject;
        gen = parentTilemap.GetComponent<GraphMazeGenerator>();
        lineOfSight = GameObject.FindObjectOfType<LineOfSightBehavior>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            gen.Generate();
            PlayerControlBehavior playerControl = collision.gameObject.GetComponent<PlayerControlBehavior>();
            playerControl.ResetPosition(gen.StartingPosition());
            lineOfSight.Reset();
        }
    }
}
