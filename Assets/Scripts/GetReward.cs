using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetReward : MonoBehaviour
{
    private Vector3 pos = new Vector3();
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Tile"))
        {
            pos = collision.transform.position;
            Debug.Log(pos);
        }
    }
}
