using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

public class GameController : NetworkBehaviour  {

	[SerializeField] private Text scoreText;
	[SerializeField] private GameObject ball;
	[SerializeField] private float noHitResetSeconds=20f; // через сколько времени после последнего касания шаром ракетки ресетнуть его
	[SerializeField] private MenuController menu;
	[SerializeField] private GameObject paddleRed;
	[SerializeField] private GameObject paddleBlue;

	private PaddleController paddleRedController;
	private PaddleController paddleBlueController;

	private int redPlayerIndex;
	private int bluePlayerIndex;

	public const int TEAM_RED = 0;
	public const int TEAM_BLUE = 1;

	private GameObject[] playerObjects;
	//private PaddleController[] paddleControllers; // RED,BLUE
	private PlayerController[] playerControllers; 

	[SyncVar]
	public int playersConnected = -1;

	public const int GOAL_RED = 1;
	public const int GOAL_BLUE = 2;

	private BallController ballController;
	private int redScore = 0;
	private int blueScore = 0;

	private bool practiceGame = false;
	private Bot bot;

	public bool isPaused = false;


	void Start () {
		if(scoreText == null){ 	throw new UnityException("GameController: Score text is not assigned!"); }
		if(ball == null){ 		throw new UnityException("GameController: Ball is not assigned!"); }
		if(paddleRed == null){ 	throw new UnityException("GameController: paddleRed is not assigned!"); }
		if(paddleBlue == null){ throw new UnityException("GameController: paddleBlue is not assigned!"); }

		ballController = ball.GetComponent<BallController> ();
		redScore = 0;
		blueScore = 0;

		paddleRedController = paddleRed.GetComponent<PaddleController> ();
		paddleBlueController = paddleBlue.GetComponent<PaddleController>();

		playerControllers = new PlayerController[2];
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

		updatePaddles ();

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

	void updatePaddles(){ // Авторитарно обновляем ввод игроков
		if(playerControllers[redPlayerIndex] != null){
			paddleRedController.up = playerControllers[redPlayerIndex].up;
			paddleRedController.down = playerControllers[redPlayerIndex].down;
		}

		if (playerControllers [bluePlayerIndex] != null) {
			paddleBlueController.up = playerControllers [bluePlayerIndex].up;
			paddleBlueController.down = playerControllers [bluePlayerIndex].down;
		}
	}



	void findPlayerObejects(){
		playerObjects = GameObject.FindGameObjectsWithTag ("Player");

		if(playerObjects.Length == 2){
			playerControllers[0] = playerObjects[0].GetComponent<PlayerController>();
			playerControllers[1] = playerObjects[1].GetComponent<PlayerController>();
			
			
		}

	}

	public bool checkReady(){
		findPlayerObejects ();
		// Если оба представлния игрока добавлены на сцену и netId каждого не 0, мы готовы начать.
		return (playerObjects.Length == 2 && playerControllers[0].netId.Value > 0 && playerControllers[1].netId.Value > 0 );
	}

	void setupPlayerObjects(){

		// Вычисляем кто первый игрок, кто второй, расставляем по местам и перекрашиваем второго в синий.
		// Стоит заметить, мы легко могли бы знать, что первый игрок тот, кто нажал в меню "создать", а второй - кто подключился, но это был бы костыль.

		findPlayerObejects ();



		if (playerObjects.Length == 2) {

			if( playerControllers[0].netId.Value < playerControllers[1].netId.Value){ // Объект 0 был создан раньше - > он красный
				redPlayerIndex = 0;
				bluePlayerIndex = 1;
			} else {
				redPlayerIndex = 1;
				bluePlayerIndex = 0;
			}

			playerControllers[redPlayerIndex].setTeam(TEAM_RED);
			playerControllers[bluePlayerIndex].setTeam(TEAM_BLUE);

		} else {
			Debug.LogError("Only one player object! Something went wrong.");
		}
	}

	void practiceGameQuickSetup(){
		playerControllers[0] = GameObject.FindGameObjectWithTag ("Player").GetComponent<PlayerController>();
		playerControllers[1] = bot;
		redPlayerIndex = 0;
		bluePlayerIndex = 1;
		playerControllers[redPlayerIndex].setTeam(TEAM_RED);
		playerControllers[bluePlayerIndex].setTeam(TEAM_BLUE);

		ballController.unfreeze();
		ballController.reset(); 
	}

	// Игра с ботом
	public void startPracticeGame(){
		bot = gameObject.AddComponent<Bot>();
		bot.ball = ball;
		bot.paddle = paddleBlue;
		practiceGame = true;

		practiceGameQuickSetup();
	}

	//Игра с человеком
	public void startMultiplayerGame(){
		//StartCoroutine("delayedGameStart");
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
		setupPlayerObjects ();
		
		ballController.unfreeze();
		ballController.reset();
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
