using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

[NetworkSettings(sendInterval=0.05f)]
public class PlayerController : NetworkBehaviour {

	public NetworkInstanceId netId;

	[SyncVar] public bool up = false;
	[SyncVar] public bool down = false;

	public int team = -1;
	
	void Start ()
	{
		netId = GetComponent<NetworkIdentity>().netId;
		
	}

	public void setTeam(int newTeam){
		//paddle = newPaddle;
		team = newTeam;
	}
	/*
	public void assignRed(){
		
		paddle = GameObject.Find ("Paddle_Red").GetComponent<PaddleController>();
		team = TEAM_RED;
	}
	
	public void assignBlue(){
		paddle = GameObject.Find ("Paddle_Blue").GetComponent<PaddleController>();
		team = TEAM_BLUE;

	}*/

	//Отправляем ввод игрока на сервер, а тот уже принимает решения.
	[Command]
	void CmdUpdateInputs(bool upInput, bool downInput){
		up = upInput;
		down = downInput;
	}


	void Update () {
		if(isLocalPlayer){ // выполняем только для игрока
			CmdUpdateInputs(Input.GetKey(KeyCode.UpArrow), Input.GetKey(KeyCode.DownArrow));
		} 
	}
}
