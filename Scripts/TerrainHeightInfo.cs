using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Chris Schiff (cxs5805@g.rit.edu), 12/11/16
 * This script allows other scripts to get the height of the terrain
 * at any given position on the terrain.
 */
public class TerrainHeightInfo : MonoBehaviour {


	// Use this for initialization
	void Start ()
	{

	}
	
	// Update is called once per frame
	void Update ()
	{

	}

	//returns the height of the terrain at the specified position
	public float GetHeight(Vector3 position)
	{
		return Terrain.activeTerrain.SampleHeight(position);
	}
}
