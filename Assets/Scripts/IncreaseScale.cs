using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Red tile behavior; it grows when the player collides whit it.
 */
public class GrowScale : MonoBehaviour
{
    [SerializeField] private float scaleRate = 0.01f; 
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if(transform.localScale.x < 0.9)
                transform.localScale += new Vector3(scaleRate, scaleRate, 0);
        }
    }
}
