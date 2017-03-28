using UnityEngine;
using System.Collections;

public class Bot : PlayerController {

	public GameObject paddle;
	public GameObject ball;

	private bool isTracking = false;
	public float toughness = 0.5f; // сложность бота, предполагается в пределах 0...1, где 1 - непобедимый, а 0 - легкий

	private float contactRange = 0.9f; // +- В каких пределах "комфорта" он не бдует двигаться потому, что шар все равно ударится в ракетку
	private float lagRange = 1.5f; // +- на какое расстояние в сумме шар должен отлететь, чтобы бот обратил на него внимание


	// Use this for initialization
	void Start () {
		team = GameController.TEAM_BLUE;

		lagRange = contactRange + (1-toughness) * 1.2f; // Сложность в 0.5f дает оптимальную сложность
	}


	
	
	// Update is called once per frame
	void Update () {
		if(paddle != null && ball != null){
			up = false;
			down = false;

			if(isTracking){
				if(paddle.transform.position.z > ball.transform.position.z + contactRange){
					down = true; // Такой же способ управления, как и у игрока
				} else if(paddle.transform.position.z < ball.transform.position.z - contactRange){
					up = true; // Такой же способ управления, как и у игрока
				} else {
					isTracking = false; // Если шар в нашей области - немного тормозим
				}
			} else {
				if(paddle.transform.position.z > ball.transform.position.z + lagRange || paddle.transform.position.z < ball.transform.position.z - lagRange){
					isTracking = true; // Если шар ушел чуть дальше - начинаем реагировать
				}
			}
		}
		
	}


}
