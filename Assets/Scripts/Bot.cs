using UnityEngine;
using System.Collections;

public class Bot : PlayerController {

	public GameObject paddle;
	public GameObject ball;



	// Use this for initialization
	void Start () {
		team = GameController.TEAM_BLUE;
	}


	
	
	// Update is called once per frame
	void Update () {
		if(paddle != null && ball != null){
			up = false;
			down = false;

			if(paddle.transform.position.z > ball.transform.position.z + 0.5f){
				down = true; // Такой же способ управления, как и у игрока
			} else if(paddle.transform.position.z < ball.transform.position.z - 0.5f){
				up = true; // Такой же способ управления, как и у игрока
			}


		}
		
	}


}
