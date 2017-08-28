using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Chris Schiff (cxs5805@g.rit.edu), 11/29/16
 * This script manages how the flockers interact with other game objects
 * in the scene. Specifically, it makes the flockers flock, displays the
 * average position and velocity of the whole flock, and checks collisions
 * between the flockers and their target in the scene.
 */
public class FlockManager : MonoBehaviour
{
	// flocker attributes
	public GameObject flocker;
	public List<GameObject> flockers;
	public List<FlockingForces> flockersFF;
	public int initialNumFlockers = 9;

	// target attributes
	public GameObject targetPrototype;
	public GameObject target;

	// average position and velocity attributes
	public Vector3 avgPos;
	public Vector3 avgV;
	public GameObject centroidPrototype;
	private GameObject centroid;
	
	// debug attributes
	private bool displayDebug = true;
	public Material matOrange;

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

		// error check for flocker object before instantiating in scene
		if (flocker == null)
		{
			Debug.Log ("Flocker not assigned in game editor");
			Debug.Break ();
		}
		
		// now do the same for the flockers' target
		if (targetPrototype == null)
		{
			Debug.Log ("Target prototype not assigned in game editor");
			Debug.Break ();
		}
		
		// spawn target and randomize its position
		target = (GameObject)Instantiate (targetPrototype);
		RandomizePosition(target);

		// make flocker-related lists
		flockers = new List<GameObject>();
		flockersFF = new List<FlockingForces> ();
		for (int i = 0; i < initialNumFlockers; ++i)
		{
			// instantiate each flocker and add to list
			flockers.Add ((GameObject)Instantiate (flocker));
			
			// initialize data structure for velocities and steering forces
			flockersFF.Add(flockers[i].GetComponent<FlockingForces>());
			
			// initialize to random positions
			RandomizePosition(flockers[i]);
			// initialize to random velocities
			flockersFF[i].velocity = new Vector3(Random.Range(2.0f, 2.5f), 0f, Random.Range(3.0f, 3.2f));
			
			// assign target to each flocker
			flockersFF[i].target = target;            
		}
		
		// initialize and position centroid
		centroid = (GameObject)Instantiate(centroidPrototype);
		UpdateCentroid();
	}
	
	// Update is called once per frame
	void Update ()
	{
		Flock(); // separate, cohere, and align
		UpdateCentroid(); // update position and velocity of centroid
		CheckCollisions(); // check collisions with the target
	}
	
	// method to make each flocker separate, cohere, and align with its neighbors as needed
	void Flock()
	{
		// check each flocker for neighbors to determine steering forces
		foreach (GameObject f in flockers)
		{
			// get index of current flocker, needed for getting perceptual neighborhoods
			int index = flockers.IndexOf(f);
			
			// set up attributes needed for calculating forces
			Vector3 posCOM = Vector3.zero; // position of neighbors' center of mass 
			Vector3 vCOM = Vector3.zero; // velocity of neighbors' center of mass
			Vector3 fSeparate = Vector3.zero; // current separation force
			int posNum = 0; // current number of neighbor positions
			int vNum = 0; // current number of neighbor velocities
			
			// now check each neighbor of current flocker
			foreach (GameObject neighbor in flockers)
			{
				// don't apply forces on current flocker from itself!
				if (f == neighbor)
				{
					continue;
				}
				
				// calculate perceptual neighborhoods and define forces if inside said PNs
				// separation
				if(flockersFF[index].IsInSeparationPN(neighbor))
				{
					// add to the separation force
					Vector3 tempSeparate = flockersFF[index].SeparationForce(neighbor);
					fSeparate += tempSeparate;
				}
				
				// cohesion
				if(flockersFF[index].IsInCohesionPN(neighbor))
				{
					// add to the position of the center of mass and increment count for average
					posCOM += neighbor.transform.position;
					++posNum;
				}
				
				// alignment
				if(flockersFF[index].IsInAlignmentPN(neighbor))
				{
					// add to the velocity of the center of mass and increment count for average
					vCOM += neighbor.GetComponent<FlockingForces>().velocity;
					++vNum;
				}
			}
			// calculate cohesion and alignment forces (separation already done in inner loop)
			Vector3 fCohesion = Vector3.zero;
			Vector3 fAlignment = Vector3.zero;
			if(posNum != 0)
			{
				fCohesion = flockersFF[index].CohesionForce(posCOM, posNum);
			}
			if(vNum != 0)
			{
				fAlignment = flockersFF[index].AlignmentForce(vCOM, vNum);
			}
			
			// add all forces together and apply to current flocker
			Vector3 fFinal = (flockersFF[index].wSeparate * fSeparate)
				+ (flockersFF[index].wCohere * fCohesion)
					+ (flockersFF[index].wAlign * fAlignment);
			flockersFF[index].ApplyForce(fFinal);
		}
	}
	
	// method to update position and velocity of centroid
	void UpdateCentroid()
	{
		// calculate average position
		Vector3 sumPos = Vector3.zero;
		for (int i = 0; i < flockers.Count; ++i)
		{
			sumPos += flockers[i].transform.position;
		}
		avgPos = sumPos / flockers.Count;
		avgPos = new Vector3(avgPos.x, 1.0f, avgPos.z);
		
		// calculate average velocity
		Vector3 sumV = Vector3.zero;
		for (int i = 0; i < flockers.Count; ++i)
		{
			sumV += flockersFF[i].velocity;
		}
		avgV = sumV / flockers.Count;
		
		// display debug line
		// press "D" to toggle the debug lines on or off
		if (Input.GetKeyDown(KeyCode.L))
		{
			displayDebug = !displayDebug;
		}
		
		// set centroid position to average position
		centroid.transform.position = avgPos;
		centroid.transform.forward = avgV.normalized;
	}
	
	// method to check flocker-target collisions
	void CheckCollisions()
	{
		foreach(GameObject f in flockers)
		{
			// get bounding sphere of current flocker
			BoundingSphere fBoundingSphere = f.GetComponent<BoundingSphere>();
			
			// now do the same for thge target
			BoundingSphere targetBoundingSphere = target.GetComponent<BoundingSphere>();
			
			// check for collision
			if(fBoundingSphere.IsColliding(targetBoundingSphere))
			{
				// if colliding, shift target to a new random position
				RandomizePosition (target);
				return;
			}
		}
	}
	
	// randomize position of param object
	void RandomizePosition(GameObject theObject)
	{
		// set position of target based on the size of the box in which the trashcan will appear
		Vector3 position = new Vector3 (Random.Range(12f, 31f), 0.0f,
		                                Random.Range(12f, 31f));

		float correctHeight = terrainHeightInfo.GetHeight(position);

		// properly adjust trashcan's height
		if (theObject.tag == "Target")
		{
			correctHeight += 0.3435f;
		}
		position.y = correctHeight;


		// set the position of target back
		theObject.transform.position = position;
	}
}
