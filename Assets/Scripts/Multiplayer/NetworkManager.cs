using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private byte maxPlayersPerRoom = 4;

    [Header("Multiplayer Scene Detection")]
    [Tooltip("Scenes that should use multiplayer (lobby, multiplayer level select, etc.)")]
    [SerializeField]
    private string[] multiplayerScenes = new string[]
    {
        "MultiplayerLobby",
        "MultiplayerLevelSelect",
        "MultiplayerGameplay",
        "Multiplayer" // Catch-all for any scene with "Multiplayer" in name
    };

    public bool IsConnectedToMaster = false;
    public bool IsInLobby = false;
    public string CurrentRoomName = "";

    private bool shouldBeConnected = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        PhotonNetwork.AutomaticallySyncScene = true;

        // Subscribe to scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Check if current scene is multiplayer
        CheckAndConnectIfNeeded();
    }

    /// <summary>
    /// Called when a new scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckAndConnectIfNeeded();

        // Ensure scene stays in sync with Master Client when already in a Photon room
        if (PhotonNetwork.InRoom && PhotonNetwork.AutomaticallySyncScene && !PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.CustomProperties != null &&
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("curScn", out object sceneId))
            {
                bool isMatchingScene = false;

                if (sceneId is string sceneName)
                {
                    isMatchingScene = string.Equals(sceneName, scene.name);
                }
                else if (sceneId is int sceneIndex)
                {
                    isMatchingScene = sceneIndex == scene.buildIndex;
                }

                if (!isMatchingScene)
                {
                    Debug.LogWarning($"[NetworkManager] Scene mismatch detected (local: {scene.name}). Resyncing to master scene: {sceneId}");

                    if (sceneId is string targetSceneName)
                    {
                        SceneManager.LoadScene(targetSceneName);
                    }
                    else if (sceneId is int targetSceneIndex)
                    {
                        SceneManager.LoadScene(targetSceneIndex);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Check if current scene needs multiplayer and connect/disconnect accordingly
    /// </summary>
    private void CheckAndConnectIfNeeded()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        bool isMultiplayerScene = IsMultiplayerScene(currentScene);

        Debug.Log($"[NetworkManager] Scene: {currentScene}, Is Multiplayer: {isMultiplayerScene}");

        if (isMultiplayerScene && !PhotonNetwork.IsConnected)
        {
            // We're in a multiplayer scene and not connected - connect now
            shouldBeConnected = true;
            ConnectToPhoton();
        }
        else if (!isMultiplayerScene && PhotonNetwork.IsConnected)
        {
            // We're in a single-player scene but still connected - disconnect
            shouldBeConnected = false;
            Disconnect();
        }
    }

    /// <summary>
    /// Check if a scene name is a multiplayer scene
    /// </summary>
    private bool IsMultiplayerScene(string sceneName)
    {
        foreach (string multiplayerScene in multiplayerScenes)
        {
            if (sceneName.Equals(multiplayerScene, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Also check if scene name contains "Multiplayer" or "Lobby"
        if (sceneName.Contains("Multiplayer") || sceneName.Contains("Lobby") || sceneName.Contains("PvP"))
        {
            return true;
        }

        return false;
    }

    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[NetworkManager] Already connected to Photon");
            return;
        }
        
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("[NetworkManager] Already connected and ready");
            return;
        }
        
        Debug.Log("[NetworkManager] Connecting to Photon...");
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public void Disconnect()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            Debug.Log("[NetworkManager] Disconnecting from Photon...");
        }
    }

    public void CreateRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("[NetworkManager] Cannot create room - not connected to Photon! Call ConnectToPhoton() first.");
            return;
        }
        
        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[NetworkManager] Already in a room. Leave current room first.");
            return;
        }
        
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = "Room_" + Random.Range(1000, 9999);
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
        Debug.Log($"Creating room: {roomName}");
    }

    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("[NetworkManager] Cannot join room - not connected to Photon! Call ConnectToPhoton() first.");
            return;
        }
        
        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[NetworkManager] Already in a room. Leave current room first.");
            return;
        }
        
        if (!string.IsNullOrEmpty(roomName))
        {
            PhotonNetwork.JoinRoom(roomName);
            Debug.Log($"Attempting to join room: {roomName}");
        }
        else
        {
            Debug.LogWarning("Room name is empty!");
        }
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            Debug.Log("Leaving room...");
        }
    }

    public void StartGame(string gameSceneName = "GameScene")
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"Starting game, loading scene: {gameSceneName}");
            PhotonNetwork.LoadLevel(gameSceneName);
        }
        else
        {
            Debug.LogWarning("Only the Master Client can start the game!");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("? Connected to Photon Master Server");
        IsConnectedToMaster = true;

        PhotonNetwork.JoinLobby();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"? Disconnected from Photon: {cause}");
        IsConnectedToMaster = false;
        IsInLobby = false;
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("? Joined Photon Lobby");
        IsInLobby = true;
    }

    public override void OnLeftLobby()
    {
        Debug.Log("Left Photon Lobby");
        IsInLobby = false;
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"? Room Created: {PhotonNetwork.CurrentRoom.Name}");
        CurrentRoomName = PhotonNetwork.CurrentRoom.Name;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"? Failed to create room: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"? Failed to join room: {message}");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left the room");
        CurrentRoomName = "";
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"New Master Client: {newMasterClient.NickName}");
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        if (PhotonNetwork.CurrentRoom != null)
        {
            CurrentRoomName = PhotonNetwork.CurrentRoom.Name;
            Debug.Log($"? Joined Room: {CurrentRoomName}");
            Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
        }

        // If Master Client, ensure scene is synced
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.AutomaticallySyncScene)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[NetworkManager] Master Client - syncing scene: {currentScene}");
            // Scene will be synced automatically via AutomaticallySyncScene
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Debug.Log($"Player joined: {newPlayer.NickName} (Total: {PhotonNetwork.CurrentRoom.PlayerCount})");

        // If we're Master Client and new player joins, ensure they get the current scene
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.AutomaticallySyncScene)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[NetworkManager] New player joined - current scene is: {currentScene}");
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Debug.Log($"Player left: {otherPlayer.NickName} (Total: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0})");
    }

    public void SetPlayerNickname(string nickname)
    {
        if (!string.IsNullOrEmpty(nickname))
        {
            PhotonNetwork.NickName = nickname;
            Debug.Log($"Nickname set to: {nickname}");
        }
    }

    public string GetConnectionStatus()
    {
        if (!PhotonNetwork.IsConnected)
            return "Disconnected";

        if (PhotonNetwork.InRoom)
            return $"In Room: {PhotonNetwork.CurrentRoom.Name}";

        if (PhotonNetwork.InLobby)
            return "In Lobby";

        if (PhotonNetwork.IsConnectedAndReady)
            return "Connected";

        return "Connecting...";
    }
}