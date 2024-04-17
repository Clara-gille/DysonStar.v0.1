using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class jungleMovingPlatform : MonoBehaviour
{

    public bool canMove;

    [SerializeField] private float speed = 10f;
    [SerializeField] private int startPoint;
    [SerializeField] public Transform[] points;

    public int destinationPoint;

    private void Start()
    {
        transform.position = points[startPoint].position;
        destinationPoint = startPoint;
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, points[destinationPoint].position) < 0.01f)
        {
            if (destinationPoint == points.Length - 1)
            {
                destinationPoint--;
            }
            else if (destinationPoint == 0)
            {
                destinationPoint++;
            }
        }

        if (canMove)
        {
            transform.position = Vector3.MoveTowards(transform.position, points[destinationPoint].position, speed * Time.deltaTime);
        }
    }
}