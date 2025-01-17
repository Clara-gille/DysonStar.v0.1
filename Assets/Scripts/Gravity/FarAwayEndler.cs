using System.Collections.Generic;
using UnityEngine;

namespace Gravity
{
    public class FarAwayEndlerManager : MonoBehaviour {

        public float distanceThreshold = 1000;
        List<Transform> physicsObjects;
        SpaceShipController ship;
        Camera playerCamera;
    

        void Awake () {
            ship = FindObjectOfType<SpaceShipController> ();
            var bodies = FindObjectsOfType<SpaceObject> ();
            var spawnPoint = FindObjectOfType<SpawnPointThreeD> ();

            physicsObjects = new List<Transform> ();
            physicsObjects.Add (ship.transform);
            physicsObjects.Add (spawnPoint.transform);
            foreach (var c in bodies) {
                physicsObjects.Add (c.transform);
            }

            playerCamera = Camera.main;
        }

        void FixedUpdate () {
            Vector3 originOffset = playerCamera.transform.position;
            float dstFromOrigin = originOffset.magnitude;

            if (dstFromOrigin > distanceThreshold) {
                foreach (Transform t in physicsObjects) {
                    t.position -= originOffset;
                }
            }
        }
    
    }
}