using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Chris Schiff (cxs5805@g.rit.edu), 11/29/16
 * This script contains kinematic and dynamic properties for each flocker, 
 * the methods for the flocking forces' perceptual neighborhoods, and the 
 * methods for every possible force to apply, including the three flocking
 * forces.
 */
public class FlockingForces : MonoBehaviour
{
	public Vector3 direction; // where we are facing, forward vector
	public Vector3 rightVector; // the right vector of the object
	public Vector3 velocity; // current velocity moving the object
	public Vector3 acceleration; // sum of forces acting on the object divided by object's mass
	
	// flocking attributes for radii, angles (in degrees), and weights defining perceptual neighborhoods
	// for separation
	public float rSeparate = 7.5f;
	public float aSeparate = 120f;
	public float cosSeparate;
	public float wSeparate = 6f;
	// for cohesion
	public float rCohere = 7.5f;
	public float aCohere = 120f;
	public float cosCohere;
	public float wCohere = 4f;
	// for alignment
	public float rAlign = 7.5f;
	public float aAlign = 120f;
	public float cosAlign;
	public float wAlign = 1f;
	
	// dynamics attributes
	public float mass = 1.0f;
	public float maxSpeed = 65.0f;
	public float maxAcceleration = 10.0f;
	
	// scene-related attributes
	private Vector3 center; // store the center position of the plane
	public bool inBounds = true; // Is the flocker in bounds?
	
	// target attributes
	public GameObject target;
	public float wTargetSeek;

	// terrain attributes
	public TerrainHeightInfo terrainHeightInfo;
	
	// Use this for initialization
	void Start ()
	{
		// get terrain height info script
		terrainHeightInfo = GameObject.Find ("Terrain").GetComponent<TerrainHeightInfo>();
		// error check for script
		if (terrainHeightInfo == null)
		{
			Debug.Log ("Terrain height info script not assigned in editor");
			Debug.Break ();
		}

		// find scene manager
		GameObject flockManagerObject = GameObject.Find("FinalSceneManager");
		if(null == flockManagerObject)
		{
			Debug.Log("Error in " + gameObject.name + 
			          ": Requires a SceneManager object in the scene.");
			Debug.Break();
		}

		// check that mass is initialized to something, as mass cannot be negative
		if (mass <= 0.0f)
		{
			mass = 0.01f;
		}
		
		// calculate cosines of all constant angles once for future use
		cosSeparate = Mathf.Cos(aSeparate * Mathf.Deg2Rad);
		cosCohere = Mathf.Cos(aCohere * Mathf.Deg2Rad);
		cosAlign = Mathf.Cos(aAlign * Mathf.Deg2Rad);
		
		// get center of the area in which flockers will roam
		center = new Vector3(27f / 2, 0, 27f / 2);

		// error check target
		if (target == null)
		{
			Debug.Log ("Target not assigned");
			Debug.Break ();
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		// check if flocker is in bounds
		CheckIfInBounds();
		
		// stay in bounds
		if(!inBounds)
		{
			Vector3 approachCenterForce = Seek (center) * maxSpeed;
			ApplyForce (approachCenterForce);
		}

		// seek target
		Vector3 seekTarget = Seek(target.transform.position) * wTargetSeek;
		ApplyForce (seekTarget);
		
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
		
		// Step 2: Add vel to position * Time
		transform.position += velocity * Time.deltaTime;

		// fix height of position
		float correctHeight = terrainHeightInfo.GetHeight(transform.position);
		transform.position = new Vector3(transform.position.x, correctHeight, transform.position.z);

		// Step 3: Reset Acceleration vector
		acceleration = Vector3.zero;
		// Step 4: Calculate direction (to know where we are facing)
		direction = velocity.normalized;
		// Step 5: Calculate right vector
		rightVector = Vector3.Cross (Vector3.up.normalized, direction);
		// Step 6: Make flocker face the correct direction
		transform.forward = direction;
	}
	
	// Apply force to flocker
	public void ApplyForce (Vector3 force)
	{
		acceleration += force / mass;
	}
	
	// seek a target
	public Vector3 Seek(Vector3 targetPosition)
	{
		// Step 1: Calculate the desired unclamped velocity
		// which is from this flocker to target's position
		Vector3 desiredVelocity = targetPosition - transform.position;
		
		// Step 2: Calculate maximum speed
		// so the flocker does not move faster than it should
		desiredVelocity.Normalize ();
		desiredVelocity *= maxSpeed;
		
		// Step 3: Calculate steering force
		Vector3 steeringForce = desiredVelocity - velocity;
		
		// Step 4: return the force so it can be applied to this flocker
		return steeringForce;
	}

	// helper methods for calculating perceptual neighborhoods
	
	// separation
	public bool IsInSeparationPN (GameObject neighbor)
	{
		// as far as we know, the neighbor is not in PN
		bool isIn = false;
		
		// compute offset vector
		Vector3 offset = neighbor.transform.position - transform.position;
		offset = new Vector3(offset.x, 0f, offset.z);
		
		// check if neighbor is in range
		if (offset.magnitude <= rSeparate)
		{
			// calculate cosine of angle between current flocker and its neighbor
			float cosNeighbor = Vector3.Dot(offset, velocity) / (offset.magnitude * velocity.magnitude);
			
			// now check if neighbor is within line of sight
			// only return true if it is
			if (cosNeighbor >= cosSeparate)
			{
				isIn = true;
			}
		}
		
		return isIn;
	}
	
	// cohesion
	public bool IsInCohesionPN (GameObject neighbor)
	{
		// as far as we know, the neighbor is not in PN
		bool isIn = false;
		
		// compute offset vector
		Vector3 offset = neighbor.transform.position - transform.position;
		offset = new Vector3(offset.x, 0f, offset.z);
		
		// check if neighbor is in range
		if (offset.magnitude <= rCohere)
		{
			// calculate cosine of angle between current flocker and its neighbor
			float cosNeighbor = Vector3.Dot(offset, velocity) / (offset.magnitude * velocity.magnitude);
			
			// now check if neighbor is within line of sight
			// only return true if it is
			if (cosNeighbor >= cosCohere)
			{
				isIn = true;
			}
		}
		
		return isIn;
	}
	
	// alignment
	public bool IsInAlignmentPN (GameObject neighbor)
	{
		// as far as we know, the neighbor is not in PN
		bool isIn = false;
		
		// compute offset vector
		Vector3 offset = neighbor.transform.position - transform.position;
		offset = new Vector3(offset.x, 0f, offset.z);
		
		// check if neighbor is in range
		if (offset.magnitude <= rAlign)
		{
			// calculate cosine of angle between current flocker and its neighbor
			float cosNeighbor = Vector3.Dot(offset, velocity) / (offset.magnitude * velocity.magnitude);
			
			// now check if neighbor is within line of sight
			// only return true if it is
			if (cosNeighbor >= cosAlign)
			{
				isIn = true;
			}
		}
		
		return isIn;
	}
	
	// helper methods for the 3 steering forces
	
	// separation
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
	
	// cohesion
	public Vector3 CohesionForce(Vector3 sumOfPos, int num)
	{
		// calculate steering force and return it
		Vector3 cohForce = (sumOfPos / num) - transform.position;
		cohForce = new Vector3(cohForce.x, 0f, cohForce.z);
		return cohForce;
	}
	
	// alignment
	public Vector3 AlignmentForce(Vector3 sumOfVel, int num)
	{
		// calculate steering force and return it
		Vector3 aliForce = (sumOfVel / num) - velocity;
		aliForce = new Vector3(aliForce.x, 0f, aliForce.z);
		return aliForce;
	}
	
	// when the object reaches the boundaries of the game space, flag it as out of bounds
	void CheckIfInBounds()
	{
		if((transform.position.x > 35f) || (transform.position.x < 8f) ||
		   (transform.position.z > 35f) || (transform.position.z < 8f))
		{
			inBounds = false;
		}
		else
		{
			inBounds = true;
		}
	}
}
