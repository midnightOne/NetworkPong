using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class BallController : NetworkBehaviour {

	private Vector3 newVelocity = Vector3.zero;
	private Vector3 startPos;

	private Vector3 velocityCache;

	public float lastHitTime = 0; //Когда в последний раз шар касался ракеток или гола, нужно для вычисления застревания.

	public int goalHit = 0; 

	void Start () {
		startPos = gameObject.transform.position;

		if(!isServer){ // убираем коллайдер у клиентов, их шар все равно позиционируется сервером. В этом примере я не буду симулировать шар на клиенте, хотя обычно это хорошая идея.
			GetComponent<Collider>().enabled = false;
			return;
		}

		freeze ();
		GetComponent<Rigidbody> ().velocity = new Vector3 (2,0,0);

	}



	void OnCollisionEnter(Collision collision) {
		foreach (ContactPoint contact in collision.contacts) {
			if(contact.otherCollider.gameObject.tag == "Paddle"){ 

				//Если шар ударился о ракетку, отправляем его в обратную сторону (по нормали касания), и немного вверх для красоты. 
				newVelocity = contact.normal * 10 + new Vector3(0,1.5f,0);
				lastHitTime=Time.time;
			} else if(contact.otherCollider.gameObject.tag == "Goal-Red"){ 
				goalHit = GameController.GOAL_RED;
				lastHitTime=Time.time;
			} else if(contact.otherCollider.gameObject.tag == "Goal-Blue"){
				goalHit = GameController.GOAL_BLUE;
				lastHitTime=Time.time;
			}

		}

		// ничего не делаем сразу - записываем в переменные чтобы обновить состояние в Update
		
	}

	public void freeze(){
		velocityCache = GetComponent<Rigidbody> ().velocity;
		GetComponent<Rigidbody> ().isKinematic = true;
	}

	public void unfreeze(){
		GetComponent<Rigidbody> ().isKinematic = false;
		GetComponent<Rigidbody> ().velocity = velocityCache;
	}

	public void reset(){
		GetComponent<Collider> ().enabled = isServer; // Если клиент стал сервером, то без этой строчки у него шар бы остался без коллайдера

		goalHit = 0;
		gameObject.transform.position = startPos;
		GetComponent<Rigidbody> ().velocity = new Vector3 (randomDirection()*2,0,0);
		lastHitTime=Time.time;
	}

	float randomDirection(){
		return Mathf.Round (Random.value) * 2 - 1; // -1 или 1
	}


	void FixedUpdate () {
		if(newVelocity != Vector3.zero){ // можно было сделать Nullable, но тогда при оспользовании он бы кастился к обычному, так оптимальнее. (а случай с нулевым новым ускорением не может произойти)
			GetComponent<Rigidbody> ().velocity = newVelocity;
			newVelocity = Vector3.zero;
		}
	}
}
