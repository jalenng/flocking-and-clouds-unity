using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{
    [SerializeField] float numberOfBirds = 50;
    [SerializeField] GameObject birdPrefab;

    List<Bird> birds;

    [SerializeField] public float maxforce = 2f;    // Maximum steering force
    [SerializeField] public float maxspeed = 10f;    // Maximum speed
    [SerializeField] public float screenRetentionForce = 5;    // Force applied when a boid hits the screen
    [SerializeField] public Vector3 boxSize = new Vector3(5, 5, 5);    // Limit flying size of birds
    [SerializeField] public float desiredseparation = 3f;    // Desired separation distance
    [SerializeField] public float neighbordist = 10f;    // Maximum distance to consider for neighbors

    public List<Bird> getAllBirds()
    {
        return birds;
    }

    public void AddBird(Bird b)
    {
        birds.Add(b);
    }

    void Awake()
    {
        birds = new List<Bird>();
    }
    // Start is called before the first frame update
    void Start()
    {
        SpawnBirds();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void SpawnBirds()
    {
        if (birdPrefab)
        {
            for (int i = 0; i < numberOfBirds; i++)
            {
                // Spawn bird
                GameObject newBird = Instantiate(
                    birdPrefab, 
                    new Vector3(
                        Random.Range(-boxSize.x, boxSize.x), 
                        Random.Range(-boxSize.y, boxSize.y),
                        Random.Range(-boxSize.z, boxSize.z)
                    ), 
                    Quaternion.identity
                );

                // Set parent to flock
                newBird.transform.parent = transform;

                // Add bird to list
                birds.Add(newBird.GetComponent<Bird>());
            }
        }
    }
}
