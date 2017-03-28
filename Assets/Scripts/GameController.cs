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
	private PlayerController[] playerControllers; 

	[SyncVar]
	public int playersConnected = -1;

	public const int GOAL_RED = 1;
	public const int GOAL_BLUE = 2;


	public bool allPlayersConnected = false; 	// Подключены оба игрока
	public bool allPlayersReady = false;		// У обоих игроков загрузились объекты (до этого игру начинать нельзя)

	private BallController ballController;
	private int redScore = 0;
	private int blueScore = 0;
	[SyncVar] public int victory = -1;

	private bool practiceGame = false;
	private Bot bot;

	private bool isPaused = false;


	public void resetGame(){
		playersConnected = -1;
		allPlayersConnected = false;
		allPlayersReady = false;
		redScore = 0;
		blueScore = 0;
		updateScoreText ();
		victory = -1;
		practiceGame = false;
		isPaused = false;
		if (playerObjects != null) {
			playerObjects [0] = null;
			playerObjects [1] = null;
		}
		playerControllers [1] = null;
		playerControllers [0] = null;

		NetworkManagerExtension.playersConnected = 0;
		ballController.reset ();

	}

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
		allPlayersConnected = (playersConnected == 2); // Ставим флаг готовности по количеству игроков

		if(!isServer){
			return;
		}

		if(allPlayersReady || practiceGame){
			checkPauseRequests ();
		}

		updatePlayersConnected ();

		if(isPaused){ return; }

		updatePaddles ();
		updateScore();
	}



	void updatePlayersConnected(){
		//Поскольку счет количества игроков идет только на сервере, записываем результат в синхронизируемую переменную
		if(playersConnected != NetworkManagerExtension.playersConnected){
			playersConnected = NetworkManagerExtension.playersConnected;
			Debug.Log("PLAYERS: " + playersConnected);
		}
	}

	void updateScore(){
		if(ballController.goalHit == GOAL_RED){					//Красный гол (Считаем только один раз)
			blueScore++;
			resetRound();
		} else if(ballController.goalHit == GOAL_BLUE){ 						//Синий гол (Считаем только один раз)
			redScore++;
			resetRound();
		} else if(noHitResetSeconds < Time.time - ballController.lastHitTime){	//Мяч долго не касался ракеток и голов, скорее всего застрял -> ресетим
			RpcResetRound(redScore, blueScore);
		}

		if(redScore >= 10){
			victory = 0; // Красная победа
		} else if(blueScore >= 10){
			victory = 1; // Синяя победа
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
		// Если оба объекты обоих игроков добавлены на сцену и netId каждого не 0, мы готовы начать.
		allPlayersReady = (playerObjects.Length == 2 && playerControllers [0].netId.Value > 0 && playerControllers [1].netId.Value > 0);
		return allPlayersReady;
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
		setupPlayerObjects ();

		ballController.unfreeze();
		ballController.reset(); 
		//Сообщаем клиентам, что пора начинать игру
		//RpcGameStart ();
	}

	//--------------------------------------------------------------------------------------------------------
	// ПАУЗА 

	void checkPauseRequests(){
		if(playerControllers[0].wantsToTogglePause || playerControllers[1].wantsToTogglePause){

			playerControllers[0].wantsToTogglePause = false;
			playerControllers[1].wantsToTogglePause = false;
			togglePause();
		}
		
	}

	//Работает только на сервере
	[Server]
	void togglePause(){
		if (isPaused) {
			isPaused = false;
			ballController.unfreeze ();
			RpcUnPause();
		} else {
			isPaused = true;
			ballController.freeze ();
			RpcPause ();
		}

	}
	
	//Работает только на клиенте, вызывается с сервера (нажна чтобы сервер поставил на паузу остальных игроков в сети)
	[ClientRpc]
	private void RpcPause(){
		isPaused = true;
	}
	
	[ClientRpc]
	private void RpcUnPause(){
		isPaused = false;
	}

	//--------------------------------------------------------------------------------------------------------
	// /ПАУЗА 


	void updateScoreText(){
		scoreText.text = redScore + " | " + blueScore;
	}

	void resetRound(){
		ballController.reset ();
		updateScoreText ();
		RpcResetRound(redScore, blueScore);
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

	public bool IsPaused {
		get {
			return isPaused;
		}
	}
}
