using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/*
 * Chris Schiff (cxs5805@g.rit.edu), 12/11/16
 * This script allows the user to cycle through 5 different camera views of the
 * final scene by pressing the "C" key.
 */

public class FinalCameraManager : MonoBehaviour
{
	// camera attributes
	public int cameraIndex;
	private Camera[] cameras;
	public Camera birdsEyePath;
	public Camera birdsEyeFlock;
	public Camera smoothFollowPath;
	public Camera smoothFollowFlock;
	public Camera FirstPersonController;

	// GUI attributes
	public GUIText cameraGUI;

	// Use this for initialization
	void Start ()
	{
		// populate array manually with cameras
		cameras = new Camera[5];
		cameras[0] = birdsEyePath;
		cameras[1] = birdsEyeFlock;
		cameras[2] = smoothFollowPath;
		cameras[3] = smoothFollowFlock;
		cameras[4] = FirstPersonController;

		// by default, bird's-eye view of path is active, and no other one is
		cameraIndex = 0;
		cameras[0].gameObject.SetActive(true);
		cameras[1].gameObject.SetActive(false);
		cameras[2].gameObject.SetActive(false);
		cameras[3].gameObject.SetActive(false);
		cameras[4].gameObject.SetActive(false);
	}
	
	// Update is called once per frame
	void Update ()
	{
		// press "P" to to move forward in camera array
		if(Input.GetKeyDown(KeyCode.C))
		{
			cameraIndex++;

			// wrap around array back to first index
			if(cameraIndex >= cameras.Length)
			{
				cameraIndex = 0;
			}
		}

		// display information for changing the camera view
		cameraGUI.text = "Press 'c' to cycle through cameras\nCamera " + (cameraIndex + 1) + "\n";

		if (cameraIndex == 0)
		{
			cameras[0].gameObject.SetActive(true);
			cameras[1].gameObject.SetActive(false);
			cameras[2].gameObject.SetActive(false);
			cameras[3].gameObject.SetActive(false);
			cameras[4].gameObject.SetActive(false);
			cameraGUI.text += "Bird's-Eye View of Path";
		}
		else if (cameraIndex == 1)
		{
			cameras[0].gameObject.SetActive(false);
			cameras[1].gameObject.SetActive(true);
			cameras[2].gameObject.SetActive(false);
			cameras[3].gameObject.SetActive(false);
			cameras[4].gameObject.SetActive(false);
			cameraGUI.text += "Bird's-Eye View of Flock";
		}
		else if (cameraIndex == 2)
		{
			cameras[0].gameObject.SetActive(false);
			cameras[1].gameObject.SetActive(false);
			cameras[2].gameObject.SetActive(true);
			cameras[3].gameObject.SetActive(false);
			cameras[4].gameObject.SetActive(false);
			cameraGUI.text += "Smooth-Follow View of Path";
		}
		else if (cameraIndex == 3)
		{
			cameras[0].gameObject.SetActive(false);
			cameras[1].gameObject.SetActive(false);
			cameras[2].gameObject.SetActive(false);
			cameras[3].gameObject.SetActive(true);
			cameras[4].gameObject.SetActive(false);
			cameraGUI.text += "Smooth-Follow View of Flock";
		}
		else if (cameraIndex == 4)
		{
			cameras[0].gameObject.SetActive(false);
			cameras[1].gameObject.SetActive(false);
			cameras[2].gameObject.SetActive(false);
			cameras[3].gameObject.SetActive(false);
			cameras[4].gameObject.SetActive(true);
			cameraGUI.text += "FPS Controller";
		}
	}
}
