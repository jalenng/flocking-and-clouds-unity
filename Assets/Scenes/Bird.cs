using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bird : MonoBehaviour
{
    [SerializeField] float xMoveSpeed = 0;

    Flock flock;

    Vector3 position;
    Vector3 velocity;
    Vector3 acceleration;
    float r;
    // [SerializeField] float maxforce = 2f;    // Maximum steering force
    // [SerializeField] float maxspeed = 0.03f;    // Maximum speed
        [SerializeField] float maxforce = 10f;    // Maximum steering force
    [SerializeField] float maxspeed = 1f;    // Maximum speed

    // Start is called before the first frame update
    void Start()
    {
        //get the parent flock object
        GameObject birds = transform.parent.gameObject; 
        flock = birds.GetComponent<Flock>();

        //insert self into the list
        flock.AddBird(this);

        //set initial state of the bird
        float angle = Random.Range(0, 6.2831853071f);
        velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), Mathf.Sin(angle));
        Debug.Log(velocity);
        position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        r = 2.0f;
        maxspeed = 2f;
        maxforce = 0.03f;

        // Quaternion rotation = Quaternion.LookRotation(new Vector3(1,0,0), Vector3.up);
        // transform.rotation = rotation;

    }

    // Update is called once per frame
    void Update()
    {

        Flock(flock.getAllBirds());
        UpdatePosition();


        // float xVal = Input.GetAxis("Horizontal") * Time.deltaTime * xMoveSpeed;
        // Debug.Log(xVal);
        // transform.Translate(xVal, 0, 0);
        // counter++;
        // Debug.Log("size: " + flock.getAllBirds());
    }

    private void Flock(List<Bird> birds){
        Vector3 sep = Separate(birds);   // Separation
        Vector3 ali = Align(birds);      // Alignment
        Vector3 coh = Cohesion(birds);   // Cohesion
        // Arbitrarily weight these forces
        sep *= 1.5f;
        ali *= 1.0f;
        coh *= 1.0f;
        // Add the force vectors to acceleration
        ApplyForce(sep);
        ApplyForce(ali);
        ApplyForce(coh);
    }

    private void UpdatePosition(){
        // Update velocity
        velocity += acceleration;
        // Limit speed
        if (velocity.magnitude > maxspeed)
        {
        velocity = velocity.normalized * maxspeed;
        }

        //update the angles too
        Quaternion rotationGoal = Quaternion.LookRotation(velocity.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotationGoal, .001f);


        transform.Translate(velocity * Time.deltaTime * 5);
        position = new Vector3(transform.position.x, transform.position.y, transform.position.z);



        // Reset accelertion to 0 each cycle
        acceleration *= 0;
    }

    private void ApplyForce(Vector3 force){
        acceleration += force;
    }

    Vector3 Seek(Vector3 target) {
        Vector3 desired = target - position;  // A vector pointing from the position to the target
        // Scale to maximum speed
        desired = desired.normalized;

        desired *= maxspeed;

        // Above two lines of code below could be condensed with new PVector setMag() method
        // Not using this method until Processing.js catches up
        // desired.setMag(maxspeed);

        // Steering = Desired minus Velocity
        Vector3 steer = desired - velocity;

        if (steer.magnitude > maxforce)
        {
        steer = velocity.normalized * maxforce;
        }
        return steer;
  }

  // Separation
  // Method checks for nearby boids and steers away
  Vector3 Separate (List<Bird> birds) {
    float desiredseparation = 8f;
    Vector3 steer = new Vector3(0, 0, 0);
    int count = 0;
    // For every boid in the system, check if it's too close
    foreach (Bird other in birds) {
      float d = Vector3.Distance(position, other.position);
      // If the distance is greater than 0 and less than an arbitrary amount (0 when you are yourself)
      if ((d > 0) && (d < desiredseparation)) {
        // Calculate vector pointing away from neighbor
        Vector3 diff = position - other.position;
        diff = diff.normalized;
        diff /= d;        // Weight by distance
        steer += diff;
        count++;            // Keep track of how many
      }
    }
    // Average -- divide by how many
    if (count > 0) {
        steer /= (float)count;
    //   steer.div((float)count);
    }

    // As long as the vector is greater than 0
    if (steer.magnitude > 0) {

      // Implement Reynolds: Steering = Desired - Velocity
      steer = steer.normalized;
      steer *= maxspeed;
      steer = steer - velocity;

        if (steer.magnitude > maxforce)
        {
        steer = steer.normalized * maxforce;
        }
    }
    return steer;
  }

   // Alignment
  // For every nearby boid in the system, calculate the average velocity
  Vector3 Align (List<Bird> birds) {
    float neighbordist = 50;
    Vector3 sum = new Vector3(0, 0, 0);
    int count = 0;
    foreach (Bird other in birds) {
      float d = Vector3.Distance(position, other.position);
      if ((d > 0) && (d < neighbordist)) {
        sum = sum + other.velocity;
        count++;
      }
    }
    if (count > 0) {
        sum = sum / (float)count;
      // First two lines of code below could be condensed with new PVector setMag() method
      // Not using this method until Processing.js catches up
      // sum.setMag(maxspeed);

      // Implement Reynolds: Steering = Desired - Velocity
      sum = sum.normalized * maxspeed;
      Vector3 steer = sum - velocity;
      if (steer.magnitude > maxforce)
        {
        steer = steer.normalized * maxforce;
        }
      return steer;
    } 
    else {
      return new Vector3(0, 0, 0);
    }
  }

    // Cohesion
  // For the average position (i.e. center) of all nearby boids, calculate steering vector towards that position
  Vector3 Cohesion (List<Bird> birds) {
    float neighbordist = 50;
    Vector3 sum = new Vector3(0, 0, 0);   // Start with empty vector to accumulate all positions
    int count = 0;
    foreach (Bird other in birds) {
      float d = Vector3.Distance(position, other.position);
      if ((d > 0) && (d < neighbordist)) {
        sum += other.position; // Add position
        count++;
      }
    }
    if (count > 0) {
      sum /= count;
      return Seek(sum);  // Steer towards the position
    } 
    else {
      return new Vector3(0, 0, 0);
    }
  }


}
