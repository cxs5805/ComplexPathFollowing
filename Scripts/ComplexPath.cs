using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/*
 * Chris Schiff (cxs5805@g.rit.edu), 12/11/16
 * This script contains data about the complex path in the scene, and it
 * implements complex path following for a leader, while followers follow the
 * leader while flocking.
 */
public class ComplexPath : MonoBehaviour
{
    // path attributes
    public int numWaypoints = 16; // number of waypoints in complex path
    public GameObject[] pathArray; // array to store each waypoint
    public Vector3[] segments; // array to store each path segment, index indicates starting waypoint
    public float radius; // radius of path

    // follower attributes
    public int numFollowers = 9; // number of followers
    public GameObject followerPrototype; // follower to instantiate
    public GameObject[] followers; // array to store followers
    public PathFollowing[] followerPFs; // array to store followers' movement scripts

	// leader attributes
	public GameObject leader; // leader to instantiate (at index 0 of followers)
	public float followingDistance; // distance behind leader of arrival point for followers
	public float followingRadius; // radius to define when other followers are to slow down
	public float leaderRadius; // radius to define when followers are to flee leader

    // temp attributes for update
	Vector3[] segsToFuturePos; // array to store vectors from the starting waypoint to the future position of the follower
	Vector3[] pointsToSeek; // array to store the points that each follower will seek

	// weight attributes
	public float wFollow = 1.0f;
	public float wLead = 1.0f;

	// debug attributes
	public Material orangeLine;
	public bool displayDebug = false;

	// Use this for initialization
	void Start ()
    {
		// initialize arrays
        pathArray = new GameObject[numWaypoints]; 
        segments = new Vector3[numWaypoints]; 
        followers = new GameObject[numFollowers]; 
        followerPFs = new PathFollowing[numFollowers]; 
		pointsToSeek = new Vector3[numFollowers];

		// instantiate the leader and keep track of movement script
		followers[0] = (GameObject)(Instantiate(leader));
		followerPFs[0] = followers[0].GetComponent<PathFollowing>();
		followerPFs[0].currentIndex = 0;
		
		// give set position and velocity
		followers[0].transform.position = new Vector3 (46f, 0.4f, 44f);
		followerPFs[0].velocity = new Vector3(-2f, 0f, 3f);

		// set leader radius
		leaderRadius = followers[0].GetComponent<CharacterController>().radius;

        // populate follower-related arrays
        for (int i = 1; i < numFollowers; ++i)
        {
            // instantiate a follower in the scene
            followers[i] = (GameObject)(Instantiate(followerPrototype));

            // keep track of follower's movement script
            followerPFs[i] = followers[i].GetComponent<PathFollowing>();

			// set all followers with random position and velocity
			followers[i].transform.position = new Vector3(UnityEngine.Random.Range (40, 48f), 0f, UnityEngine.Random.Range (30f, 35f));
			followerPFs[i].velocity = new Vector3(UnityEngine.Random.Range (-1f, 1.5f), 0f, UnityEngine.Random.Range (-1f, 1.5f));

			// set all followers' starting index to be 0, the start waypoint
			followerPFs[i].currentIndex = 0;
        }

        // populate path array with waypoints from scene
		for (int i = 0; i < numWaypoints; ++i)
		{
			pathArray[i] = GameObject.Find ("Barrel_Standard_final" + i);
		}

        // set radius of path
        radius = pathArray[0].GetComponent<BoundingSphere>().radius;

        // set all path segments
        int index, next;
        for (index = 0; index < numWaypoints; ++index)
        {
            // set index of next waypoint
            if (index == numWaypoints - 1)
            {
                next = 0;
            }
            else
            {
                next = index + 1;
            }

            // calculate path segment
            segments[index] = pathArray[next].transform.position - pathArray[index].transform.position;
        }

		// by default, all followers' first points to seek will be the first waypoint
		for (int i = 0; i < numFollowers; ++i)
		{
			pointsToSeek[i] = pathArray[0].transform.position;
			followerPFs[i].currentSeekingPoint = pointsToSeek[i];

			// also, display debug information by default
			followerPFs[i].displayDebug = true;
		}
    }

	// Update is called once per frame
	void Update ()
    {
		// read user input to toggle display of debug
		if(Input.GetKeyDown(KeyCode.L))
		{
			displayDebug = !displayDebug;
			for( int i = 0; i < numFollowers; ++i)
			{
				followerPFs[i].displayDebug = displayDebug;
			}
		}

		// update the leader
		FollowPath(0);

		// update each follower
        for (int i = 1; i < numFollowers; ++i)
        {
			followerPFs[i].currentIndex = followerPFs[0].currentIndex;
			Separate(i); // make follower separate from other followers
			Align(i, followerPFs[i].currentIndex); // make follower align with leader's current segment
			FollowLeader(i); // make follower arrive at a point behind the leader
        }
    }

	// make follower at index i follow the path
	void FollowPath(int i)
	{
		// get the point to seek
		pointsToSeek[i] = GetDesiredPointGivenSegmentIndex(followers[i], followerPFs[i], i);
		
		// generate seeking force
		Vector3 seekingForce = followerPFs[i].Seek (pointsToSeek[i]) * wFollow;
		
		// only apply the force if the future position is outside the radius
		// and not behind the current waypoint
		followerPFs[i].isInRadius = RadiusCheck(followers[i], followerPFs[i], pointsToSeek[i]);
		bool behindCheck = BehindCheck(followerPFs[i].currentIndex);
		if (followerPFs[i].isInRadius)
		{
			followerPFs[i].ApplyForce(seekingForce);
		}
		if (!behindCheck)
		{
			followerPFs[i].currentSeekingPoint = pointsToSeek[i];
		}
	}

	void Separate(int i)
	{
		// separate from other followers
		// by default assume there's no need to separate
		Vector3 fSeparate = Vector3.zero;
		
		foreach (GameObject neighbor in followers)
		{
			// don't apply forces on current flocker from itself!
			if (followers[i] == neighbor)
			{
				continue;
			}
			
			// calculate perceptual neighborhood and define separation force if inside PN
			if(followerPFs[i].IsInPN(neighbor, followerPFs[i].rSeparate, followerPFs[i].cosSeparate))
			{
				// add to the separation force
				Vector3 tempSeparate = followerPFs[i].SeparationForce(neighbor) * followerPFs[i].wSeparate;
				fSeparate += tempSeparate;
			}
		}

		// now apply the final separation force
		followerPFs[i].ApplyForce (fSeparate);
	}
	
	void Align(int i, int segIndex)
	{
		// align with the leader's current segment
		// by default, there's no need to align
		Vector3 fAlign = Vector3.zero;

		// get unit segment
		Vector3 unitSeg = segments[segIndex].normalized;
		
		// apply unit segment as force if in PN
		if(followerPFs[i].IsInPN(pathArray[segIndex].transform.position + segments[segIndex], followerPFs[i].rAlign, followerPFs[i].cosAlign))
		{
			fAlign = unitSeg * followerPFs[i].wAlign;
			followerPFs[i].ApplyForce(fAlign);
		}
	}

	void FollowLeader(int i)
	{
		// get distance between current follower's position and leader
		float distToLeader = Vector3.Distance (followers[i].transform.position, followers[0].transform.position);

		// calculate point behind leader
		Vector3 pointBehindLeader = followers[0].transform.position - followerPFs[0].velocity.normalized * followingDistance;

		// if current follower is too close to leader, flee
		if (distToLeader < leaderRadius)
		{
			followerPFs[i].ApplyForce (followerPFs[i].Flee (followers[0].transform.position));
		}
		// otherwise, do regular leader following algorithm
		else
		{
			// arrive at or seek point behind leader
			followerPFs[i].ApplyForce (followerPFs[i].Arrive (pointBehindLeader));
		}
	}

	Vector3 GetDesiredPointGivenSegmentIndex(GameObject follower, PathFollowing followerPF, int followerIndex)
	{
		// calculate future position
		Vector3 futurePos = follower.transform.position + followerPF.velocity;

		// get the vectors from each starting waypoint to the future position
		segsToFuturePos = new Vector3[numWaypoints];
		for (int i = 0; i < numWaypoints; ++i)
		{
			segsToFuturePos[i] = futurePos - pathArray[i].transform.position;
		}

		// get closest point on current segment
		Vector3 thePoint = GetClosestPointOnSegment(futurePos, followerPF);


		// if ahead of segment, get closest point on the NEXT segment
		if (AheadCheck(followerPF.currentIndex, thePoint))
		{
			// wrap back to zero if at last index
			if(followerPF.currentIndex == numWaypoints - 1)
			{
				followerPF.currentIndex = 0;
			}
			// increment the index
			else
			{
				followerPF.currentIndex++;
			}

			return GetClosestPointOnSegment(futurePos, followerPF);
		}

		// if behind segment, get closest point on the PREVIOUS segment
		if (BehindCheck (followerPF.currentIndex))
		{
			// wrap back to end if at first index
			if(followerPF.currentIndex == 0)
			{
				followerPF.currentIndex = numWaypoints - 1;
			}
			// decrement the index
			else
			{
				followerPF.currentIndex--;
			}

			return GetClosestPointOnSegment(futurePos, followerPF);
		}

		// now return a valid point that can be sought by the follower
		return thePoint;
	}

	bool AheadCheck(int startWPIndex, Vector3 thePoint)
	{
		// by default, assume the point is not ahead of the segment
		bool isAhead = false;

		// get the distance of the point along the segment
		float distAlongSegment = Vector3.Distance (pathArray[startWPIndex].transform.position, thePoint);

		// if it's longer than the segment, the point is ahead
		if (distAlongSegment > segments[startWPIndex].magnitude)
		{
			isAhead = true;
		}

		return isAhead;
	}

	bool BehindCheck(int startWPIndex)
	{
		// by default, assume the point is in front of the start waypoint
		bool isBehind = false;

		// get the cosine between the unit segment and the point
		float cos = Vector3.Dot (segments[startWPIndex].normalized, segsToFuturePos[startWPIndex])/(segsToFuturePos[startWPIndex].magnitude);

		// if the cosine is less than 0, then the point is behind the segment
		if (cos < 0)
		{
			isBehind = true;
		}

		return isBehind;
	}

	bool RadiusCheck(GameObject follower, PathFollowing followerPF, Vector3 potentialSeekingPoint)
	{
		// by default, assume that the follower's future position is inside the radius
		bool shouldSteer = false;

		float distance = Vector3.Distance (follower.transform.position + followerPF.velocity, potentialSeekingPoint);

		// however, the follower should steer if the future position is outside the radius
		if (distance > radius)
		{
			shouldSteer = true;
		}

		return shouldSteer;
	}

	Vector3 GetClosestPointOnSegment(Vector3 futurePos, PathFollowing followerPF)
	{
		// get the unit vector along the current segment
		Vector3 unitSeg = segments[followerPF.currentIndex].normalized;
		
		// calculate the dot product between these previous two vectors
		float dot = Vector3.Dot (segsToFuturePos[followerPF.currentIndex], unitSeg);

		// multiply normal vector by dot product and add to start waypoint position, then return
		Vector3 closestPoint = pathArray[followerPF.currentIndex].transform.position + (unitSeg * dot);
		return closestPoint;
	}
}
