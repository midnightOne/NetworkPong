using UnityEngine;
using System.Collections;
using System;
using System.ComponentModel;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
	public NetworkManager manager;

	[SerializeField] private Button HostButton;
	[SerializeField] private Button ConnectButton;
	[SerializeField] private InputField ipField;
	[SerializeField] private Text waitingText;
	[SerializeField] private Text connectingText;
	[SerializeField] private Camera gameCamera;
	[SerializeField] private Camera menuCamera;
	[SerializeField] private Camera orbitCamera;
	[SerializeField] private GameController game;
	[SerializeField] private float waitForConnectionTime = 15f; // Время на подключение второго игрока перед щапуском сервера с ботом
	[SerializeField] private Text statusText;

	private float serverCreateTime;


	const int STATE_IDLE = 0;
	const int STATE_WAITING_FOR_PLAYER_2 = 1;
	const int STATE_CONNECTING = 2;
	const int STATE_LOADING_MULTIPLAYER_GAME = 3;
	const int STATE_LOADING_PRACTICE_GAME = 4;
	const int STATE_MULTIPLAYER_GAME = 5;
	const int STATE_PRACTICE_GAME = 6;

	private int state = STATE_IDLE;

	private bool isHost = false;


	void Awake()
	{
		manager = GetComponent<NetworkManager>();

	}

	void Start(){
		if(HostButton == null){ 	throw new UnityException("MenuController: HostButton is not assigned!"); }
		if(ConnectButton == null){ 	throw new UnityException("MenuController: ConnectButton not assigned!"); }
		if(waitingText == null){ 	throw new UnityException("MenuController: waitingText is not assigned!"); }
		if(connectingText == null){ throw new UnityException("MenuController: connectingText is not assigned!"); }
		if(gameCamera == null){		throw new UnityException("MenuController: gameCamera is not assigned!"); }
		if(menuCamera == null){		throw new UnityException("MenuController: menuCamera is not assigned!"); }
		if(orbitCamera == null){	throw new UnityException("MenuController: orbitCamera is not assigned!"); }
		if(game == null){			throw new UnityException("MenuController: GameController is not assigned!"); }

		initMenu ();
	}

	void initMenu(){
		setButtonText(HostButton, "Создать игру");
		setButtonText(ConnectButton, "Присоединиться к игре");
		menuCamera.enabled = true;
		orbitCamera.enabled = true;
		gameCamera.enabled = false;
		HostButton.onClick.AddListener(clickHost);
		ConnectButton.onClick.AddListener(clickConnect);
		waitingText.enabled = false;
		connectingText.enabled = false;
		statusText.text = "";
	}

	void hideMenu(){
		// Выключаем ненужное при выходе из меню
		menuCamera.enabled = false;
		orbitCamera.enabled = false;
		gameCamera.enabled = true;
		HostButton.onClick.RemoveListener(clickHost);
		ConnectButton.onClick.RemoveListener(clickConnect);

	}

	void Update()
	{
		float timeLeft;
		if (state == STATE_WAITING_FOR_PLAYER_2){ // ждем второго игрока
			timeLeft = waitForConnectionTime - Time.time + serverCreateTime;
			waitingText.text = "Ожидаем соперника..." + timeLeft;

			if(game.playersConnected == 2){ 
				//Запускаем игру на двоих

				state = STATE_LOADING_MULTIPLAYER_GAME;

			} else if(timeLeft <= 0f){
				//Запускаем сервер с ботом
				game.startPracticeGame();
				hideMenu();
				state = STATE_LOADING_PRACTICE_GAME;
			} 

		} else if (state == STATE_CONNECTING){ // Подключаемся к серверу/хосту
			if(game.playersConnected == 2){ 
				//Запускаем игру на двоих
				state = STATE_LOADING_MULTIPLAYER_GAME;
			}
		} else if (state == STATE_LOADING_MULTIPLAYER_GAME){ // Сервер говорит, что подключилось 2 игрока, но мы еще не знаем, инициализировались-ли объекты игроков на клиентах
			if(game.checkReady()){
				game.startMultiplayerGame();
				hideMenu();
				state = STATE_MULTIPLAYER_GAME;
			}

		} else if(state == STATE_MULTIPLAYER_GAME  || state == STATE_PRACTICE_GAME){
			//checkDisconnect();

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				stopMultiplayerGame();

			}
		}



		
		/*if (!manager.IsClientConnected() && !NetworkServer.active && manager.matchMaker == null)
		{
			if (UnityEngine.Application.platform != RuntimePlatform.WebGLPlayer)
			{
				if (Input.GetKeyDown(KeyCode.S))
				{
					manager.StartServer();
				}
				if (Input.GetKeyDown(KeyCode.H))
				{
					manager.StartHost();
				}
			}
			if (Input.GetKeyDown(KeyCode.C))
			{
				manager.StartClient();
			}
		}
		if (NetworkServer.active && manager.IsClientConnected())
		{
			if (Input.GetKeyDown(KeyCode.X))
			{
				manager.StopHost();
			}
		}*/
	}

	void stopMultiplayerGame(){
		if(isHost){
			manager.StopHost();
		} else {
			manager.StopClient();
		}
		initMenu();
		state = STATE_IDLE;
	}

	void checkDisconnect(){
		if (!isHost && (manager.client == null || !manager.client.isConnected)){
			stopMultiplayerGame();
			statusText.text = "Соединение с сервером было потеряно";
		}

	}


	void clickHost(){
		if(state == STATE_IDLE){ // Запуск сервера
			waitingText.enabled = true;
			state = STATE_WAITING_FOR_PLAYER_2;
			setButtonText(HostButton, "Отмена");

			manager.StartHost ();
			serverCreateTime = Time.time;
			isHost = true;

		} else if(state == STATE_WAITING_FOR_PLAYER_2){ // Отмена
			setButtonText(HostButton, "Создать игру");
			waitingText.enabled = false;
			state = STATE_IDLE;
			manager.StopHost();
			isHost = false;
		}
	}

	void clickConnect(){
		if(state == STATE_IDLE){ // Подключение к серверу
			connectingText.enabled = true;
			state = STATE_CONNECTING;
			setButtonText(ConnectButton, "Отмена");

			manager.StartClient();
			isHost = false;
		} else if (state == STATE_CONNECTING){ // Отмена
			connectingText.enabled = false;
			setButtonText(ConnectButton, "Присоединиться к игре");

			state = STATE_IDLE;
			manager.StopClient();
		}

	}

	void setButtonText(Button btn, string text){
		btn.GetComponentsInChildren<Text>()[0].text = text;
	}
	
	void OnGUI()
	{
		
		bool noConnection = (manager.client == null || manager.client.connection == null ||
		                     manager.client.connection.connectionId == -1);
		
		if (!manager.IsClientConnected() && !NetworkServer.active && manager.matchMaker == null)
		{
			if (noConnection)
			{



				/*if (GUI.Button(new Rect(xpos, ypos, 200, 20), "LAN Host(H)"))
				{
					manager.StartHost();
				}

				
				if (GUI.Button(new Rect(xpos, ypos, 105, 20), "LAN Client(C)"))
				{
					manager.StartClient();
				}
				
				manager.networkAddress = GUI.TextField(new Rect(xpos + 100, ypos, 95, 20), manager.networkAddress);

				if (GUI.Button(new Rect(xpos, ypos, 200, 20), "LAN Server Only(S)"))
				{
					manager.StartServer();
				}*/

			}
			else
			{/*
				GUI.Label(new Rect(xpos, ypos, 200, 20), "Connecting to " + manager.networkAddress + ":" + manager.networkPort + "..");
				ypos += spacing;
				
				
				if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Cancel Connection Attempt"))
				{
					manager.StopClient();
				}*/
			}
		}
		else
		{/*

			if (manager.IsClientConnected())
			{
				GUI.Label(new Rect(xpos, ypos, 300, 20), "Client: address=" + manager.networkAddress + " port=" + manager.networkPort);
				ypos += spacing;
			}*/
		}
		
		if (manager.IsClientConnected() && !ClientScene.ready)
		{/*
			if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Client Ready"))
			{
				ClientScene.Ready(manager.client.connection);
				
				if (ClientScene.localPlayers.Count == 0)
				{
					ClientScene.AddPlayer(0);
				}
			}
			ypos += spacing;*/
		}
		
		if (NetworkServer.active || manager.IsClientConnected())
		{/*
			if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Stop (X)"))
			{
				manager.StopHost();
			}
			ypos += spacing;*/
		}

	}
}