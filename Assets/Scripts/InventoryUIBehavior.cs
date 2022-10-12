using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIBehavior : MonoBehaviour
{
    public Text text;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(!child.gameObject.activeSelf);
            }
        }

        text.text = "";
        foreach (KeyValuePair<string, int> kv in InventoryData.instance.items)
        {
            text.text += kv.Key + " x" + kv.Value + "\n";
        }
    }
}
