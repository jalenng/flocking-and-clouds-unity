using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{
    List<Bird> birds;

    [SerializeField] public float maxforce = 2f;    // Maximum steering force
    [SerializeField] public float maxspeed = 10f;    // Maximum speed
    [SerializeField] public float screenRetentionForce = 5;    // Force applied when a boid hits the screen
    [SerializeField] public float boxSize = 15;    // Limit flying size of birds
    [SerializeField] public float desiredseparation = 3f;    // Desired separation distance
    [SerializeField] public float neighbordist = 10f;    // Maximum distance to consider for neighbors

    public List<Bird> getAllBirds(){
        return birds;
    }

    public void AddBird(Bird b){
        birds.Add(b);
    }

    Flock(){
        birds = new List<Bird>();
    }
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
