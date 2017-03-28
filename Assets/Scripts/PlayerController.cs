using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

[NetworkSettings(sendInterval=0.05f)]
public class PlayerController : NetworkBehaviour {

	public NetworkInstanceId netId;

	/*	На клиенте только один gameObject и его скрипты могут отправлять команды на сервер (и это объект игрока),
	 * поэтому алгоритм такой: Пользователь нажимает Esc -> экземпляр этого компонента на объекте игрока на его клиенте
	 * отправляет команду такому же экземпляру этого компонента на объекте игрока, но на сервере, а тот записывает желание в эту переменную.
	 * Потом серверный геймконтроллер читает ее и уже тогда отправляет всем клиентам команду о паузе.
	 * */
	[SyncVar]
	public bool wantsToTogglePause = false;


	[SyncVar] public bool up = false;
	[SyncVar] public bool down = false;

	[SyncVar]
	public int team = -1;
	
	void Start ()
	{
		netId = GetComponent<NetworkIdentity>().netId;
		
	}

	public void setTeam(int newTeam){
		team = newTeam;
	}


	//Отправляем ввод игрока на сервер, а тот уже принимает решения.
	[Command]
	void CmdUpdateInputs(bool upInput, bool downInput){
		up = upInput;
		down = downInput;
	}

	//Отправляем ввод игрока на сервер, а тот уже принимает решения.
	[Command]
	void CmdTogglePause(){
		wantsToTogglePause = true;
	}


	void Update () {

		if(isLocalPlayer){ // выполняем только для игрока
			bool currentUp = Input.GetKey (KeyCode.UpArrow);
			bool currentDown = Input.GetKey (KeyCode.DownArrow);
			if(up != currentUp || down != currentDown){ // чтобы не тратить зря трафик, отправляем только если что-то изменилось
				CmdUpdateInputs(currentUp, currentDown);
			}

			if (Input.GetKeyDown(KeyCode.Escape) && !wantsToTogglePause)
			{
				CmdTogglePause();
			}
		} 
	}
}
