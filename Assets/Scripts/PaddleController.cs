using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PaddleController : NetworkBehaviour {

	[SerializeField] private Material blueMaterial;
	public NetworkInstanceId netId;

	void Start ()
	{
		netId = GetComponent<NetworkIdentity>().netId;
	}

	public void setRed(){
		// Поскольку NetworkManager не умеет детерминированно спавнить даже при RoundRobin, то сами вычисляем где какой спавн.

		GameObject tempObject = GameObject.Find ("RedSpawnPosition");
		gameObject.transform.position = tempObject.transform.position;
		gameObject.transform.rotation = tempObject.transform.rotation;
	}

	public void setBlue(){
		GameObject tempObject = GameObject.Find ("BlueSpawnPosition");
		gameObject.transform.position = tempObject.transform.position;
		gameObject.transform.rotation = tempObject.transform.rotation;

		if(blueMaterial != null){
			gameObject.GetComponent<Renderer> ().material = blueMaterial;
		}
	}


	void Update () {
		if(isLocalPlayer){ // выполняем только для игрока
			if(Input.GetKey(KeyCode.UpArrow)){
				gameObject.transform.Translate(new Vector3(0,0.1f,0));
			} else if(Input.GetKey(KeyCode.DownArrow)){
				gameObject.transform.Translate(new Vector3(0,-0.1f,0));
			}
		} 

	}
}
