using UnityEngine;
using System.Collections;

public class CameraOrbit : MonoBehaviour {

	[SerializeField] private float xRotation;
	[SerializeField] private float yYRotation;

	void Update ()
	{

		transform.eulerAngles += new Vector3(0, xRotation, -yYRotation);
	}

}
