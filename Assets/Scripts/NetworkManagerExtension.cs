using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class NetworkManagerExtension : NetworkManager {

	// Обновляется только на сервере
	public static int playersConnected = 0;
	// Use this for initialization
	void Start () {
	}

	public override void OnClientError (NetworkConnection conn, int errorCode)
	{
		//StopHost ();
		base.OnClientError (conn, errorCode);
	}

	public override void OnServerConnect (NetworkConnection conn)
	{
		playersConnected++;
		base.OnServerConnect (conn);
	}

	public override void OnServerDisconnect (NetworkConnection conn)
	{
		playersConnected--;
		base.OnServerDisconnect (conn);
	}

	
	// Update is called once per frame
	void Update () {
	
	}
}
