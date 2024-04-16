using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagnetRay : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject emitPoint;
    [SerializeField] private GameObject endPoint;
    [SerializeField] private float RepusleStrength = 20;
    [SerializeField] private float AttractStrength = 20;

    //Detect when collision with a trigger is occurring
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(tag)) // If block of same color
        {
            // Repulse the player in the opposite direction of the ray
            Vector3 direction = endPoint.transform.position - emitPoint.transform.position;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            Vector2 force = -direction.normalized * RepusleStrength / 25;
            rb.AddForce(force, ForceMode2D.Impulse);
            //Add horizontal force to the player mouvements
            player.GetComponent<PlayerMouvement>().magnetHorizontal = force.x * 4;
        }
        else if (other.CompareTag(CompareTag("Blue") ? "Red" : "Blue")) // If block of different color
        {
            // Attract the player towards the magnet
            Vector3 direction = endPoint.transform.position - emitPoint.transform.position;
            player.GetComponent<Rigidbody2D>().AddForce(direction.normalized * AttractStrength, ForceMode2D.Force);
        }
    }
}
