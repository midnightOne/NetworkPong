using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PaddleController : NetworkBehaviour {

	public bool up = false;
	public bool down = false;
	[SerializeField] private GameObject spawnPositionObject;

	public void moveUp(){
		up = true;
	}
	public void moveDown(){
		down = true;
	}

	void reset(){
		gameObject.transform.position = spawnPositionObject.transform.position;
		gameObject.transform.rotation = spawnPositionObject.transform.rotation;
	}
	

	// Двигаем рактетку только на сервере чтобы избежать нечесной игры.
	void Update () {
		if (isServer) {
			if (up != down) { // Если оба направления - стоим на месте, иначе - двигаемся
				if (up && gameObject.transform.position.z < -0.2f) {
					gameObject.transform.Translate (new Vector3 (0, 0.1f, 0));
				} else if (down && gameObject.transform.position.z > -9.1) {
					gameObject.transform.Translate (new Vector3 (0, -0.1f, 0));
				}
			}
			up = false;
			down = false;
		}
	}

}
