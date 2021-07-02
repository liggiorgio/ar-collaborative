using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace ARC
{
	[HelpURL("https://www.example.com")]
	[AddComponentMenu("ARC/ARC Session Listener")]
	[DisallowMultipleComponent]
	public class ARCSessionListener : CaptainsMessListener
	{
		public GameObject sharedSessionPrefab;
		public Text networkStateField;

		public UnityEvent startConnecting;
		public UnityEvent stopConnecting;
		public UnityEvent serverCreated;
		public UnityEvent joinedLobby;
		public UnityEvent leftLobby;
		public UnityEvent countdownStarted;
		public UnityEvent countdownCancelled;
		public ARCStartGameEvent startGame;
		public UnityEvent abortGame;

		[HideInInspector]
		public ARCSession sharedSession;
		ARCNetworkState _networkState = ARCNetworkState.Init;

		void Start()
		{
			_networkState = ARCNetworkState.Offline;
			ClientScene.RegisterPrefab(sharedSessionPrefab);
		}

		void Update()
		{
			networkStateField.text = _networkState.ToString();	
		}

		public override void OnStartConnecting()
		{
			_networkState = ARCNetworkState.Connecting;
			startConnecting.Invoke();
		}

		public override void OnStopConnecting()
		{
			_networkState = ARCNetworkState.Offline;
			stopConnecting.Invoke();
		}

		public override void OnServerCreated()
		{
			// Create game session
			ARCSession oldSession = FindObjectOfType<ARCSession>();
			if (oldSession == null)
			{
				GameObject serverSession = Instantiate(sharedSessionPrefab);
				NetworkServer.Spawn(serverSession);
				serverCreated.Invoke();
			}
			else
			{
				Debug.LogError("ARSharedSession already exists!");
			}
		}

		public override void OnJoinedLobby()
		{
			_networkState = ARCNetworkState.Connected;

			sharedSession = FindObjectOfType<ARCSession>();
			if (sharedSession)
			{
				sharedSession.OnJoinedLobby();
			}
			else
			{
				Debug.Log("No gamesession found!");
			}
			joinedLobby.Invoke();
		}

		public override void OnLeftLobby()
		{
			_networkState = ARCNetworkState.Offline;
			sharedSession.OnLeftLobby();
			leftLobby.Invoke();
		}

		public override void OnCountdownStarted()
		{
			sharedSession.OnCountdownStarted();
			countdownStarted.Invoke();
		}

		public override void OnCountdownCancelled()
		{
			sharedSession.OnCountdownCancelled();
			countdownCancelled.Invoke();
		}

		public override void OnStartGame(List<CaptainsMessPlayer> aStartingPlayers)
		{
			sharedSession.OnStartGame(aStartingPlayers);
			ARCUser[] users = aStartingPlayers.Select(p => p as ARCUser).ToArray();
			startGame.Invoke(users);
		}

		public override void OnAbortGame()
		{
			sharedSession.OnAbortGame();
			abortGame.Invoke();
		}
	}
}
