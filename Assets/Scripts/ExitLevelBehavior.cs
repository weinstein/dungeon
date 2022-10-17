using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitLevelBehavior : MonoBehaviour
{
    GameObject parentTilemap;
    GraphMazeGenerator gen;
    LineOfSightBehavior lineOfSight;
    public AudioClip sfx;

    private GameObject player = null;

    // Start is called before the first frame update
    void Start()
    {
        parentTilemap = transform.parent.gameObject;
        gen = parentTilemap.GetComponent<GraphMazeGenerator>();
        lineOfSight = GameObject.FindObjectOfType<LineOfSightBehavior>();
    }

    private void Update()
    {
        if (player != null)
        {
            player.GetComponent<AudioSource>().PlayOneShot(sfx);
            var oldMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;
            gen.Generate();
            PlayerControlBehavior playerControl = player.GetComponent<PlayerControlBehavior>();
            playerControl.ResetPosition(gen.StartingPosition());
            Physics2D.SyncTransforms();
            Physics2D.Simulate(Time.fixedDeltaTime);
            Physics2D.SyncTransforms();
            Physics2D.simulationMode = oldMode;
            lineOfSight.ResetRevealedTiles();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            player = collision.gameObject;
        }
    }
}
