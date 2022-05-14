using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{
    List<Bird> birds;

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
        // birds = new List<Bird>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
