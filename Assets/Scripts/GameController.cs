using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GameController : NetworkBehaviour  {

	[SerializeField] private Text scoreText;
	[SerializeField] private GameObject ball;
	[SerializeField] private float noHitResetSeconds=15f; // через сколько времени после последнего касания шаром ракетки ресетнуть его
	[SerializeField] private MenuController menu;

	private GameObject[] playerObjects;

	[SyncVar]
	public int playersConnected = -1;

	public const int GOAL_RED = 1;
	public const int GOAL_BLUE = 2;

	private BallController ballController;
	private int redScore = 0;
	private int blueScore = 0;

	public bool isPaused = false;


	void Start () {
		if(scoreText == null){ 	throw new UnityException("GameController: Score text is not assigned!"); }
		if(ball == null){ 		throw new UnityException("GameController: Ball is not assigned!"); }
		if(menu == null){ 		throw new UnityException("GameController: menu is not assigned!"); }

		ballController = ball.GetComponent<BallController> ();
		redScore = 0;
		blueScore = 0;
	}


	void Update () {


		if(!isServer){
			return;
		}

		//Поскольку счет количества игроков идет только на сервере, записываем результат в синхронихируемую переменную
		if(playersConnected != NetworkManagerExtension.playersConnected){
			playersConnected = NetworkManagerExtension.playersConnected;
			Debug.Log("PLAYERS: " + playersConnected);
			if(playersConnected == 2){

				//StartCoroutine("delayedGameStart");
				//startGame();
			}
		}

		if(isPaused){
			return;
		}


		if(ballController.goalHit == GOAL_RED){									//Красный гол
			blueScore++;
			RpcResetRound(redScore, blueScore);
		} else if(ballController.goalHit == GOAL_BLUE){ 						//Синий гол
			redScore++;
			RpcResetRound(redScore, blueScore);
		} else if(noHitResetSeconds < Time.time - ballController.lastHitTime){	//Мяч долго не касался ракеток и голов, скорее всего застрял -> ресетим
			RpcResetRound(redScore, blueScore);
		}


	}

	void setupPlayerObjects(){
		playerObjects = GameObject.FindGameObjectsWithTag ("Paddle");
		PaddleController controller_0;
		PaddleController controller_1;


		if (playerObjects.Length == 2) {
			controller_0 = playerObjects[0].GetComponent<PaddleController>();
			controller_1 = playerObjects[1].GetComponent<PaddleController>();

			if(controller_0.creationTime < controller_1.creationTime){ // Объект 0 был создан раньше - > он красный
				controller_0.setRed();
				controller_1.setBlue();
			} else {
				controller_1.setRed();
				controller_0.setBlue();
			}
		} else {
			Debug.LogError("Only one player object! Something gone wrong.");
		}
	}

	// Игра с ботом
	public void startPracticeGame(){

	}

	//Игра с человеком
	public void startMultiplayerGame(){
		setupPlayerObjects ();

		ballController.unfreeze();
		ballController.reset(); 
		//Сообщаем клиентам, что пора начинать игру
		//RpcGameStart ();
	}

	IEnumerator delayedGameStart(){

		// 
		yield return new WaitForSeconds(0.5f);

		//Физика мяча считается только на сервере, поэтому манипуляции с ним нужно провести только там
		ballController.unfreeze();
		ballController.reset(); 
		RpcGameStart ();
		yield break;

	}

	// По требованию сервера запускаем игру
	[ClientRpc]
	void RpcGameStart(){
		//menu.showGame ();
	}


	void updateScoreText(){
		scoreText.text = redScore + " | " + blueScore;
	}

	// По требованию сервера производим ресет раунда
	[ClientRpc]
	void RpcResetRound(int redScoreParam, int blueScoreParam){
		redScore = redScoreParam;
		blueScore = blueScoreParam;

		ballController.reset ();
		updateScoreText ();
	}

	// По требованию сервера производим ресет игры
	[ClientRpc]
	void RpcResetGame(){
		RpcResetRound (0,0);
	}
}
