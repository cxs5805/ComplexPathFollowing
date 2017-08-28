using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Chris Schiff (cxs5805@g.rit.edu), 12/11/16
 * This script contains kinematic and dynamic properties for the leader
 * and each follower. It enables the leader to do complex path following
 * while enabling the followers to seek, flee, and flock.
 */
public class PathFollowing : MonoBehaviour
{
    public Vector3 direction; // where we are facing, forward vector
    public Vector3 rightVector; // the right vector of the object
    public Vector3 velocity; // current velocity moving the object
    public Vector3 acceleration; // sum of forces acting on the object divided by object's mass

    // dynamics attributes
    public float mass = 1.0f;
    public float maxSpeed = 65.0f;
    public float maxAcceleration = 10.0f;

	// path attributes
	public ComplexPath path;
	public int currentIndex; // indicates the index of the current segment along which to travel

	// separation attributes
	public float rSeparate = 7.5f;
	public float aSeparate = 120f;
	public float cosSeparate;
	public float wSeparate = 6f;

	// cohesion attributes
	public float rCohere = 7.5f;
	public float aCohere = 120f;
	public float cosCohere;
	public float wCohere = 4f;

	// alignment attributes
	public float rAlign = 7.5f;
	public float aAlign = 120f;
	public float cosAlign;
	public float wAlign = 1f;

	// debug attributes
	public Vector3 currentSeekingPoint;
	public bool isInRadius;
	public bool displayDebug = false;

	// terrain attributes
	//public Terrain theTerrain;
	public TerrainHeightInfo terrainHeightInfo;

	// Use this for initialization
	void Start ()
    {

		// get the actual script to get correct height values
		terrainHeightInfo = GameObject.Find ("Terrain").GetComponent<TerrainHeightInfo>();

		// error check for height script
		if (terrainHeightInfo == null)
		{
			Debug.Log ("Terrain height info script not assigned in editor");
			Debug.Break ();
		}

		path = GameObject.Find ("FinalSceneManager").GetComponent<ComplexPath>();

		// error check for path script
		if (path == null)
		{
			Debug.Log ("Path script not assigned in editor");
			Debug.Break ();
		}

        // check that mass is initialized to something, as mass cannot be negative
        if (mass <= 0.0f)
        {
            mass = 0.01f;
        }

		// by default, assume follower is outside the path radius
		isInRadius = false;

		// calculate cosine of separation angle for future use in separation force
		cosSeparate = Mathf.Cos(aSeparate * Mathf.Deg2Rad);
	}
	
	// Update is called once per frame
	void Update ()
    {
		// cap acceleration at max acceleration if necessary
		if (acceleration.magnitude > maxAcceleration)
		{
			acceleration.Normalize();
			acceleration *= maxAcceleration;
		}
		
		// Step 1: Add Acceleration to Velocity * Time
		velocity += acceleration * Time.deltaTime;
		
		// make sure velocity stays at correct height
		velocity = new Vector3(velocity.x, 0f, velocity.z);
		
		// cap velocity at max speed if necessary
		if (velocity.magnitude > maxSpeed)
		{
			velocity.Normalize();
			velocity *= maxSpeed;
		}

		// make sure velocity doesn't get too low
		if(velocity.magnitude < 0.3)
		{
			velocity.Normalize();
			velocity *= 2f;
		}
		
		// Step 2: Add vel to position * Time
		transform.position += velocity * Time.deltaTime;
		
		// fix height of position
		float correctHeight = terrainHeightInfo.GetHeight (transform.position);
		if (gameObject.tag == "Leader")
		{
			transform.position = new Vector3(transform.position.x, correctHeight + 0.4f, transform.position.z);
		}
		else
		{
			transform.position = new Vector3(transform.position.x, correctHeight, transform.position.z);
		}

		
		// Step 3: Reset Acceleration vector
		acceleration = Vector3.zero;
		// Step 4: Calculate direction (to know where we are facing)
		direction = velocity.normalized;
		// Step 5: Calculate right vector
		rightVector = Vector3.Cross (Vector3.up.normalized, direction);
		// Step 6: Make follower face the correct direction
		transform.forward = direction;
	}

    // Apply force to follower
    public void ApplyForce (Vector3 force)
    {
        acceleration += force / mass;
    }

    // seek a target
    public Vector3 Seek(Vector3 targetPosition)
    {
        // Step 1: Calculate the desired unclamped velocity
        // which is from this follower to target's position
        Vector3 desiredVelocity = targetPosition - transform.position;

        // Step 2: Calculate maximum speed
        // so the follower does not move faster than it should
        desiredVelocity.Normalize ();
        desiredVelocity *= maxSpeed;

        // Step 3: Calculate steering force
        Vector3 steeringForce = desiredVelocity - velocity;

        // Step 4: return the force so it can be applied to this follower
        return steeringForce;
    }

	// flee a target
	public Vector3 Flee(Vector3 targetPosition)
	{
		// Step 1: Calculate the desired unclamped velocity
		// which is from this follower to target's position
		Vector3 desiredVelocity = transform.position - targetPosition;
		
		// Step 2: Calculate maximum speed
		// so the follower does not move faster than it should
		desiredVelocity.Normalize ();
		desiredVelocity *= maxSpeed;
		
		// Step 3: Calculate steering force
		Vector3 steeringForce = desiredVelocity - velocity;
		
		// Step 4: return the force so it can be applied to this follower
		return steeringForce;
	}

	// arrive at or seek a target
	public Vector3 Arrive(Vector3 targetPosition)
	{
		// get unclamped velocity
		Vector3 desiredVelocity = targetPosition - transform.position;

		// get a copy of its current magnitude before normalizing it
		float dist = desiredVelocity.magnitude;
		desiredVelocity.Normalize();

		// check how close we are to the target point
		if (dist < path.followingRadius)
		{
			// the closer we are, the more we will slow down
			float mag = dist * 0.8f;
			desiredVelocity *= mag;
		}
		// otherwise, just seek at maximum speed
		else
		{
			desiredVelocity *= maxSpeed;
		}

		// now calculate steering force and return it
		Vector3 steeringForce = desiredVelocity - velocity;
		return steeringForce;
	}


	// check if in perceptual neighborhood
	public bool IsInPN (GameObject neighbor, float radius, float cosine)
	{
		// as far as we know, the neighbor is not in PN
		bool isIn = false;
		
		// compute offset vector
		Vector3 offset = neighbor.transform.position - transform.position;
		offset = new Vector3(offset.x, 0f, offset.z);
		
		// check if neighbor is in range
		if (offset.magnitude <= radius)
		{
			// calculate cosine of angle between current follower and its neighbor
			float cosNeighbor = Vector3.Dot(offset, velocity) / (offset.magnitude * velocity.magnitude);
			
			// now check if neighbor is within line of sight
			// only return true if it is
			if (cosNeighbor >= cosine)
			{
				isIn = true;
			}
		}
		
		return isIn;
	}

	// overload of previous method (needed for alignment)
	public bool IsInPN (Vector3 segment, float radius, float cosine)
	{
		// as far as we know, the segment is not in PN
		bool isIn = false;
		
		// compute offset vector
		Vector3 offset = segment - transform.position;
		offset = new Vector3(offset.x, 0f, offset.z);
		
		// check if segment is in range
		if (offset.magnitude <= radius)
		{
			// calculate cosine of angle between current follower and the segment
			float cosSegment = Vector3.Dot(offset, velocity) / (offset.magnitude * velocity.magnitude);
			
			// now check if segment is within line of sight
			// only return true if it is
			if (cosSegment >= cosine)
			{
				isIn = true;
			}
		}
		
		return isIn;
	}

	// separation method
	// calculate and return steering force
	public Vector3 SeparationForce(GameObject neighbor)
	{
		// compute offset vector and normalize
		Vector3 offset = neighbor.transform.position - transform.position;
		offset = new Vector3(offset.x, 0f, offset.z);
		offset.Normalize();
		
		// calculate steering force and return it
		Vector3 sepForce = -(offset / Mathf.Pow(offset.magnitude, 2));
		return sepForce;
	}

	// cohesion method
	public Vector3 CohesionForce(Vector3 sumOfPos, int num)
	{
		// calculate steering force and return it
		Vector3 cohForce = (sumOfPos / num) - transform.position;
		cohForce = new Vector3(cohForce.x, 0f, cohForce.z);
		return cohForce;
	}

	// alignment method
	public Vector3 AlignmentForce(Vector3 sumOfVel, int num)
	{
		// calculate steering force and return it
		Vector3 aliForce = (sumOfVel / num) - velocity;
		aliForce = new Vector3(aliForce.x, 0f, aliForce.z);
		return aliForce;
	}
}
