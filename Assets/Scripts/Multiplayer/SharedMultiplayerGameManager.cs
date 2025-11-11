using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

/// <summary>
/// Manages shared multiplayer gameplay with:
/// - Shared HP pool for all players
/// - Character switching with slide animations
/// - Turn-based card playing
/// - Automatic turn progression
/// </summary>
public class SharedMultiplayerGameManager : MonoBehaviourPunCallbacks
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true; // Toggle in Inspector to enable/disable console logs

    [Header("Shared Health System")]
    [SerializeField] private float sharedMaxHealth = 100f;
    [SerializeField] private Image sharedHealthBar;
    [SerializeField] private TextMeshProUGUI sharedHealthText;
    
    [Header("Character Display")]
    [SerializeField] private Transform characterDisplayPosition;
    [SerializeField] private float characterSlideSpeed = 5f;
    [SerializeField] private Vector3 offScreenLeft = new Vector3(-15f, 0f, 0f);
    [SerializeField] private Vector3 offScreenRight = new Vector3(15f, 0f, 0f);
  
    [Header("Turn Display")]
    [SerializeField] private TextMeshProUGUI currentTurnText;
    [SerializeField] private GameObject yourTurnIndicator;
    [SerializeField] private GameObject waitingIndicator;
    
    [Header("Player List UI")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerListItemPrefab;
    
    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    
    [Header("References")]
 [SerializeField] private PlayCardButton playCardButton;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private EnemyManager enemyManager;
    
    // Game state
    private float currentSharedHealth;
    private bool isGameOver = false;
    private PhotonView photonView;
private Dictionary<int, GameObject> playerCharacters = new Dictionary<int, GameObject>();
    private GameObject currentCharacterInstance;
    private bool isSwitchingCharacter = false;
    private CodexMultiplayerIntegration turnSystem;
    
    // Animation sync: delay health UI updates until attack animations complete
    private bool delayHealthUIUpdate = false;
    
    /// <summary>
    /// Conditional debug logging - only logs if enableDebugLogs is true
    /// </summary>
    private void Log(string message)
    {
        if (enableDebugLogs)
        {
 Debug.Log($"[SharedMultiplayerGameManager] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
        if (enableDebugLogs)
        {
  Debug.LogWarning($"[SharedMultiplayerGameManager] {message}");
        }
    }
    
    private void LogError(string message)
    {
        // Always log errors (critical issues)
        Debug.LogError($"[SharedMultiplayerGameManager] {message}");
    }
    
  private void Awake()
{
        photonView = GetComponent<PhotonView>();
        
        if (photonView == null)
        {
            LogError("CRITICAL: PhotonView component is missing! Add a PhotonView component to this GameObject.");
            enabled = false;
            return;
        }
        
   turnSystem = GetComponent<CodexMultiplayerIntegration>();
        
        if (turnSystem == null)
        {
    LogWarning("CodexMultiplayerIntegration not found. Add it to the same GameObject!");
     }
    }
    
    private void Start()
    {
        // Validate PhotonView exists
        if (photonView == null)
        {
            LogError("CRITICAL: PhotonView is null! Cannot use multiplayer features.");
            HideMultiplayerUI();
            enabled = false;
            return;
        }
        
      if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            LogWarning("Not in Photon room. Disabling.");
 HideMultiplayerUI();
enabled = false;
         return;
        }
     
        if (PhotonNetwork.IsMasterClient)
        {
  InitializeSharedHealth();
        StartCoroutine(InitializeTurnSystemDelayed());
   }
        
      if (turnSystem != null)
        {
            turnSystem.OnTurnChanged += OnTurnChanged;
  }
        else
    {
        LogWarning("Turn system is NULL!");
        }
        
   InitializeUI();
        UpdatePlayerListUI();
        StartCoroutine(LoadPlayerCharacters());
    }
    
    private System.Collections.IEnumerator InitializeTurnSystemDelayed()
 {
     yield return new WaitForSeconds(0.5f);
        
        if (turnSystem != null)
        {
 Log("Manually initializing turn system");
      turnSystem.InitializeTurnSystem();
        }
        else
        {
 LogError("Cannot initialize - turn system component is missing!");
    }
    }
    
    private void HideMultiplayerUI()
    {
        if (currentTurnText != null) currentTurnText.gameObject.SetActive(false);
        if (yourTurnIndicator != null) yourTurnIndicator.SetActive(false);
        if (waitingIndicator != null) waitingIndicator.SetActive(false);
        if (playerListContainer != null) playerListContainer.gameObject.SetActive(false);
    }
    
    private void InitializeUI()
    {
        if (currentTurnText != null)
        {
            currentTurnText.gameObject.SetActive(true);
      }
 else
        {
  LogWarning("Current turn text not assigned!");
        }
        
        if (yourTurnIndicator != null)
        {
 yourTurnIndicator.SetActive(false);
        }
        else
     {
          LogWarning("Your turn indicator not assigned!");
        }
        
        if (waitingIndicator != null)
        {
      waitingIndicator.SetActive(false);
        }
        else
        {
            LogWarning("Waiting indicator not assigned!");
        }
        
        if (playerListContainer != null)
  {
 playerListContainer.gameObject.SetActive(true);
        }
    else
        {
    LogWarning("Player list container not assigned!");
      }
        
        if (playerListItemPrefab == null)
   {
   LogWarning("Player list item prefab not assigned!");
    }
    }
    
    private void Update()
    {
     UpdateHealthUI();
        UpdateTurnUI();
    }
    
    #region Shared Health System
    
    private void InitializeSharedHealth()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        currentSharedHealth = sharedMaxHealth;
  
        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable
        {
   { "SharedHealth", currentSharedHealth },
         { "SharedMaxHealth", sharedMaxHealth }
 };
        
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }
    
    public void DamageSharedHealth(float damage)
    {
  if (!PhotonNetwork.IsMasterClient) return;

        currentSharedHealth -= damage;
    currentSharedHealth = Mathf.Max(0f, currentSharedHealth); // Fixed: Use Max to prevent negative health
        
        ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable
        {
            { "SharedHealth", currentSharedHealth }
        };
   
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        
        if (currentSharedHealth <= 0f && !isGameOver)
 {
            photonView.RPC("RPC_GameOver", RpcTarget.All, false);
        }
    }
    
    private float GetSharedHealth()
    {
        object healthObj;
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SharedHealth", out healthObj))
        {
     return (float)healthObj;
        }
        return sharedMaxHealth;
    }
    
    private void UpdateHealthUI()
    {
      if (!PhotonNetwork.InRoom) return;
        
        // Only update UI if not waiting for animation to complete
  if (delayHealthUIUpdate) return;
     
  float health = GetSharedHealth();
   float maxHealth = sharedMaxHealth;
        
    if (sharedHealthBar != null)
        {
  sharedHealthBar.fillAmount = health / maxHealth;
   }
  
      if (sharedHealthText != null)
{
      sharedHealthText.text = $"{health:F0} / {maxHealth:F0}";
    }
    }
    
    /// <summary>
    /// Force update health UI after attack animation completes
    /// </summary>
    private void ForceUpdateHealthUI()
    {
        if (!PhotonNetwork.InRoom) return;
        
        float health = GetSharedHealth();
   float maxHealth = sharedMaxHealth;
      
        if (sharedHealthBar != null)
        {
         sharedHealthBar.fillAmount = health / maxHealth;
        }
        
   if (sharedHealthText != null)
        {
       sharedHealthText.text = $"{health:F0} / {maxHealth:F0}";
        }
        
        Log($"Forced health UI update: {health}/{maxHealth}");
    }
    
    #endregion
    
    #region Character Switching System
    
    private IEnumerator LoadPlayerCharacters()
    {
     yield return new WaitForSeconds(1.5f);
        
        Log("===== LOADING CHARACTERS =====");
 Log($"Room: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.PlayerList.Length}");
        
      foreach (Player player in PhotonNetwork.PlayerList)
        {
            Log($"Processing player: {player.NickName}, ActorNumber: {player.ActorNumber}");
         
    object cosmeticObj;
            string cosmetic = "Default";
            
      if (player.CustomProperties.TryGetValue("multiplayer_cosmetic", out cosmeticObj))
          {
     cosmetic = cosmeticObj.ToString();
            }
         else if (player.CustomProperties.TryGetValue("cosmetic", out cosmeticObj))
            {
                cosmetic = cosmeticObj.ToString();
   }
            else if (player.CustomProperties.TryGetValue("current_cosmetic", out cosmeticObj))
      {
          cosmetic = cosmeticObj.ToString();
    }
            else
    {
        LogWarning($"Player {player.NickName} has no cosmetic property! Using Default");
            }
        
      if (string.IsNullOrWhiteSpace(cosmetic))
     {
       LogWarning($"Player {player.NickName} has empty cosmetic! Using Default");
          cosmetic = "Default";
    }
            
   LoadPlayerCharacter(player.ActorNumber, cosmetic);
        }
        
        yield return new WaitForSeconds(0.5f);
        
      Log("===== CHARACTER LOADING COMPLETE =====");
        Log($"Loaded {playerCharacters.Count} characters");
        
        if (PhotonNetwork.PlayerList.Length > 0)
        {
       int firstActorNumber = PhotonNetwork.PlayerList[0].ActorNumber;
    
            if (playerCharacters.ContainsKey(firstActorNumber))
            {
      ShowCharacter(firstActorNumber, false);
            }
   else
 {
  LogError($"CRITICAL: Character for actor {firstActorNumber} was not loaded!");
   }
     }
        else
    {
            LogError("No players in room after loading!");
        }
    }
    
    private void LoadPlayerCharacter(int actorNumber, string cosmeticData)
    {
        Log("===== LoadPlayerCharacter =====");
    Log($"Actor: {actorNumber}, Cosmetic: '{cosmeticData}'");
  
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (player == null)
        {
            LogError($"CRITICAL: Player with ActorNumber {actorNumber} not found in room!");
         return;
        }
        
 Log($"Found player: {player.NickName}");
        
        cosmeticData = cosmeticData?.Trim();
        if (string.IsNullOrEmpty(cosmeticData))
        {
            LogWarning("Empty cosmetic data, using 'Default'");
            cosmeticData = "Default";
        }
    
        GameObject characterPrefab = GetCharacterPrefab(cosmeticData);
        
      if (characterPrefab != null)
        {
            Log($"Creating character instance for {player.NickName}");
       
            try
   {
          GameObject character = Instantiate(characterPrefab, offScreenLeft, Quaternion.identity);
 character.name = $"Character_{player.NickName}_Actor{actorNumber}";
                character.SetActive(false);
       
          playerCharacters[actorNumber] = character;
       
           Log($"? Character created and stored for actor {actorNumber}");
        }
         catch (System.Exception ex)
         {
          LogError($"Exception creating character: {ex.Message}");
         }
        }
        else
        {
            LogError($"CRITICAL: Could not load character prefab for cosmetic: '{cosmeticData}'");
        }
    }
    
    private GameObject GetCharacterPrefab(string cosmeticData)
    {
 Log("===== GetCharacterPrefab =====");
   Log($"Searching for: '{cosmeticData}'");
        
   GameObject prefab = null;
    
        // Try exact match
      prefab = Resources.Load<GameObject>($"Characters/{cosmeticData}");
if (prefab != null)
        {
            Log($"? Found prefab with exact match: Characters/{cosmeticData}");
            return prefab;
        }
        
  // Try capitalized first letter
        if (!string.IsNullOrEmpty(cosmeticData) && cosmeticData.Length > 0)
        {
        string capitalizedPath = $"Characters/{char.ToUpper(cosmeticData[0])}{cosmeticData.Substring(1)}";
        prefab = Resources.Load<GameObject>(capitalizedPath);
     
   if (prefab != null)
          {
      Log($"? Found prefab with capitalization: {capitalizedPath}");
                return prefab;
            }
   }
        
      // Try all lowercase
        string lowercasePath = $"Characters/{cosmeticData.ToLower()}";
        prefab = Resources.Load<GameObject>(lowercasePath);
      
        if (prefab != null)
    {
        Log($"? Found prefab with lowercase: {lowercasePath}");
         return prefab;
        }
        
        // Try "Default" as ultimate fallback
     Log("Trying fallback: Characters/Default");
    prefab = Resources.Load<GameObject>("Characters/Default");
 
        if (prefab != null)
        {
            Log("? Using Default prefab as fallback");
    return prefab;
        }
        else
        {
   LogError("? CRITICAL: Even Default prefab not found!");
 LogError("Make sure you have a prefab at: Assets/Resources/Characters/Default.prefab");
  }
     
        return null;
    }
 
    private void ShowCharacter(int actorNumber, bool animated)
    {
        Log("===== ShowCharacter =====");
        Log($"Actor: {actorNumber}, Animated: {animated}");
      
        if (characterDisplayPosition == null)
     {
      LogError("Character Display Position is NULL! Assign it in Inspector.");
  return;
        }
   
        if (isSwitchingCharacter)
        {
        LogWarning("Already switching character, ignoring");
       return;
   }
        
        if (!playerCharacters.ContainsKey(actorNumber))
  {
          LogError($"? No character found for actor {actorNumber}!");
            
   Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
if (player != null)
   {
         LogError($"Missing character for player: {player.NickName}");
          }
            
            LogWarning("Continuing gameplay without character visual for this player.");
         return;
        }
        
        Log($"Character found for actor {actorNumber}: {playerCharacters[actorNumber].name}");
        
   if (animated)
        {
        Log($"Starting animated switch to actor {actorNumber}");
            StartCoroutine(SwitchCharacterAnimated(actorNumber));
        }
        else
        {
            Log($"Showing character instantly for actor {actorNumber}");
          
            if (currentCharacterInstance != null)
     {
              currentCharacterInstance.SetActive(false);
      }
 
     currentCharacterInstance = playerCharacters[actorNumber];
            currentCharacterInstance.transform.position = characterDisplayPosition.position;
        currentCharacterInstance.SetActive(true);
       
    Log($"? Character {actorNumber} shown at position {characterDisplayPosition.position}");
  }
    }
    
    private IEnumerator SwitchCharacterAnimated(int newActorNumber)
    {
        isSwitchingCharacter = true;
        
 if (currentCharacterInstance != null)
        {
            currentCharacterInstance.SetActive(true);
          Vector3 startPos = currentCharacterInstance.transform.position;
            Vector3 targetPos = offScreenLeft;
            
float elapsed = 0f;
            float duration = 1f / characterSlideSpeed;
            
        while (elapsed < duration)
      {
      elapsed += Time.deltaTime;
  float t = elapsed / duration;
        currentCharacterInstance.transform.position = Vector3.Lerp(startPos, targetPos, t);
      yield return null;
  }
      
 currentCharacterInstance.SetActive(false);
        }
      
        if (playerCharacters.ContainsKey(newActorNumber))
        {
            currentCharacterInstance = playerCharacters[newActorNumber];
            currentCharacterInstance.SetActive(true);
     currentCharacterInstance.transform.position = offScreenRight;
            
    Vector3 startPos = offScreenRight;
     Vector3 targetPos = characterDisplayPosition.position;
     
   float elapsed = 0f;
      float duration = 1f / characterSlideSpeed;
 
            while (elapsed < duration)
            {
  elapsed += Time.deltaTime;
     float t = elapsed / duration;
     currentCharacterInstance.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
    }
          
     currentCharacterInstance.transform.position = targetPos;
        }
        
    isSwitchingCharacter = false;
    }
    
    #endregion
    
    #region Turn Management
    
    private void OnTurnChanged(Player player)
    {
        Log($"========== TURN CHANGED TO: {player.NickName} ==========");
        
    // CRITICAL FIX: Check ActorNumber instead of relying on IsLocal
   // player.IsLocal can return false even for the local player in some cases
bool isMyTurn = (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber);
        
        Log($"Player ActorNumber: {player.ActorNumber}");
        Log($"Local Player ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
        Log($"Is my turn: {isMyTurn}");

        // Switch to new player's character
        ShowCharacter(player.ActorNumber, true);
  
        // Update UI
        UpdateTurnUI();
  
        // **CRITICAL FIX: Force card reset via RPC to ensure ALL clients reset**
        if (PhotonNetwork.IsMasterClient)
        {
            Log("Master Client - forcing card reset via RPC for all players");
            photonView.RPC("RPC_ForceCardReset", RpcTarget.All);
        }
     
        // Enable/Disable controls based on whose turn it is
        if (isMyTurn)  // ? FIXED: Use ActorNumber comparison, not player.IsLocal
        {
            Log("LOCAL PLAYER - Enabling controls");
     EnablePlayerControls();
     
     if (cardManager != null && cardManager.grid != null)
    {
  cardManager.grid.SetActive(true);
              Log("Card grid activated");
     }
            
            // Start timer only for the player whose turn it is
            StartTimerForCurrentPlayer();
        }
        else
        {
            Log($"REMOTE PLAYER ({player.NickName}) - Disabling controls");
         DisablePlayerControls();

            if (cardManager != null && cardManager.grid != null)
     {
   cardManager.grid.SetActive(false);
                Log("Card grid deactivated");
  }
            
            // Pause timer for players who are not on their turn
            Timer timer = FindObjectOfType<Timer>();
            if (timer != null)
            {
                timer.PauseTimer();
                Log("Timer paused - not my turn");
            }
        }
    
        Log($"========== TURN CHANGE COMPLETE ==========");
    }
    
  /// <summary>
    /// Force reset cards and timer for all players (called via RPC)
    /// </summary>
    [PunRPC]
    void RPC_ForceCardReset()
    {
        Log("RPC_ForceCardReset - Resetting cards and timer for ALL players");
   
     // **CRITICAL FIX: Set card counter to match current question (outputManager.counter)**
  if (cardManager != null)
        {
     // IMPORTANT: Card counter should match the current question being asked
            // outputManager.counter tells us which question we're on
 if (playCardButton != null && playCardButton.outputManager != null)
   {
        int correctCounter = playCardButton.outputManager.counter;
        
        // Validate the counter is in range
     if (correctCounter >= 0 && correctCounter < cardManager.cardContainer.Count)
           {
   cardManager.counter = correctCounter;
        Log($"Card counter set to {correctCounter} to match current question");
             }
    else
       {
      LogWarning($"Output counter {correctCounter} out of range! Resetting to 0");
 cardManager.counter = 0;
}
   }
         else
{
       LogWarning("PlayCardButton or OutputManager null - resetting counter to 0");
        cardManager.counter = 0;
   }
      
      Log($"Card counter is {cardManager.counter} (valid range: 0-{cardManager.cardContainer.Count - 1})");
            
  // Reset cards back to grid and randomize
cardManager.ResetCards();
  cardManager.StartCoroutine(cardManager.Randomize());
            Log("Cards reset and randomized for current question");
   }
  else
 {
      LogError("CardManager is NULL!");
        }
      
  // Reset and restart timer for all players
        Timer timer = FindObjectOfType<Timer>();
  if (timer != null)
   {
      timer.ResetTimer();
   timer.StartTimer();
   Log("Timer reset and restarted for all players");
        }
   else
{
      LogWarning("Timer not found in scene!");
    }
    }
    
    /// <summary>
    /// Sync card manager counter across all clients
    /// </summary>
    [PunRPC]
    void RPC_SyncCardCounter(int newCounter)
  {
Log($"RPC_SyncCardCounter - Setting counter to {newCounter}");
      
        if (cardManager != null)
        {
          cardManager.counter = newCounter;
            Log($"Card manager counter synced to {newCounter}");
        }
      
        if (playCardButton != null)
        {
            playCardButton.counter = 0; // Reset play button counter for new question
 Log("Play button counter reset to 0");
        }
    }
    
    private void UpdateTurnUI()
    {
    if (!PhotonNetwork.InRoom) return;
        
        if (isGameOver)
        {
            HideMultiplayerUI();
        return;
        }
        
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
      if (currentTurnText != null)
            {
   currentTurnText.text = "Solo Testing Mode";
                currentTurnText.gameObject.SetActive(true);
            }
            
      if (yourTurnIndicator != null) yourTurnIndicator.SetActive(true);
          if (waitingIndicator != null) waitingIndicator.SetActive(false);
        return;
        }
        
        if (turnSystem == null)
        {
            if (currentTurnText != null)
    {
    currentTurnText.text = "ERROR: Turn System Missing!\nAdd CodexMultiplayerIntegration component";
        currentTurnText.gameObject.SetActive(true);
     currentTurnText.color = Color.red;
      }
            
          if (yourTurnIndicator != null) yourTurnIndicator.SetActive(false);
      if (waitingIndicator != null) waitingIndicator.SetActive(true);
  return;
 }
     
        Player currentPlayer = turnSystem.GetCurrentTurnPlayer();
   
        if (currentPlayer != null)
        {
      if (currentTurnText != null)
            {
                currentTurnText.text = $"{currentPlayer.NickName}'s Turn";
 currentTurnText.gameObject.SetActive(true);
       currentTurnText.color = Color.white;
}
        
          if (yourTurnIndicator != null) yourTurnIndicator.SetActive(currentPlayer.IsLocal);
            if (waitingIndicator != null) waitingIndicator.SetActive(!currentPlayer.IsLocal);
        }
        else
        {
            if (currentTurnText != null)
            {
  currentTurnText.text = "Initializing turn system...\nPlease wait";
       currentTurnText.gameObject.SetActive(true);
       currentTurnText.color = Color.yellow;
     }
            
          if (yourTurnIndicator != null) yourTurnIndicator.SetActive(false);
         if (waitingIndicator != null) waitingIndicator.SetActive(true);
        }
    }
    
    private void EnablePlayerControls()
    {
        if (playCardButton != null) playCardButton.enabled = true;
        if (cardManager != null) cardManager.enabled = true;
    }
    
    private void DisablePlayerControls()
    {
        if (playCardButton != null) playCardButton.enabled = false;
    }
    
  #endregion
    
    #region Timer Synchronization
    
    /// <summary>
    /// Start timer only for the player whose turn it is
    /// </summary>
    public void StartTimerForCurrentPlayer()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Player currentPlayer = turnSystem?.GetCurrentTurnPlayer();
            if (currentPlayer != null)
            {
                Log($"Starting timer for {currentPlayer.NickName}'s turn");
                photonView.RPC("RPC_StartTimerForPlayer", RpcTarget.All, currentPlayer.ActorNumber);
            }
        }
    }
    
    /// <summary>
    /// Start timer for all players (used when starting new enemy)
    /// </summary>
    public void StartTimerForAllPlayers()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Log("Starting timer for all players (new enemy)");
            photonView.RPC("RPC_StartTimer", RpcTarget.All);
        }
    }
    
    /// <summary>
    /// Stop timer for all players
    /// </summary>
    public void StopTimerForAllPlayers()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Log("Stopping timer for all players");
            photonView.RPC("RPC_StopTimer", RpcTarget.All);
        }
    }
    
    [PunRPC]
    void RPC_StartTimer()
    {
        Timer timer = FindObjectOfType<Timer>();
        if (timer != null)
        {
          timer.ResetTimer();
  timer.StartTimer();
            Log("Timer started for all players");
        }
        else
 {
         LogWarning("Timer not found in scene!");
        }
    }
    
    [PunRPC]
    void RPC_StartTimerForPlayer(int playerActorNumber)
    {
        bool isMyTurn = (playerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber);
        
        Timer timer = FindObjectOfType<Timer>();
        if (timer != null)
        {
            if (isMyTurn)
            {
                timer.ResetTimer();
                timer.StartTimer();
                Log($"Timer started for my turn (ActorNumber: {playerActorNumber})");
            }
            else
            {
                timer.PauseTimer();
                Log($"Timer paused - not my turn (ActorNumber: {playerActorNumber})");
            }
        }
        else
        {
            LogWarning("Timer not found in scene!");
        }
    }
    
    [PunRPC]
    void RPC_StopTimer()
    {
        Timer timer = FindObjectOfType<Timer>();
        if (timer != null)
        {
            timer.PauseTimer();
            Log("Timer paused for all players");
        }
        else
        {
            LogWarning("Timer not found in scene!");
        }
    }
    
    #endregion
    
    #region Card Play Integration
    
    public void OnCardPlayed(bool wasCorrect)
    {
        Log($"OnCardPlayed called - Correct: {wasCorrect}");
     
        if (!turnSystem.IsMyTurn())
        {
   LogWarning("Not my turn, ignoring");
            return;
        }
        
        Log("Sending RPC to all players");
        photonView.RPC("RPC_OnCardPlayed", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, wasCorrect);
    }
    
    [PunRPC]
    void RPC_OnCardPlayed(int playerActorNumber, bool wasCorrect)
    {
 Log($"RPC_OnCardPlayed received - Player: {playerActorNumber}, Correct: {wasCorrect}");
        
   if (wasCorrect)
        {
          // CORRECT ANSWER - Trigger player attack animation on all clients
  bool hasAnimation = false;
   if (playCardButton != null && playCardButton.useJumpAttackAnimation && 
   playCardButton.playerCharacter != null && 
  playCardButton.playerJumpAttack != null &&
    playCardButton.enemyManager != null &&
   playCardButton.enemyManager.counter < playCardButton.enemyManager.enemies.Count)
       {
     GameObject currentEnemy = playCardButton.enemyManager.enemies[playCardButton.enemyManager.counter];
        
 if (currentEnemy != null)
        {
       hasAnimation = true;
           
    // Trigger player jump attack animation on ALL clients
           playCardButton.playerJumpAttack.PerformJumpAttack(currentEnemy.transform, () => {
         // Master Client applies damage
       if (PhotonNetwork.IsMasterClient)
       {
        playCardButton.EnemyTakeDamage(1f);
       
       // Sync enemy health to all clients
    int enemyIndex = playCardButton.enemyManager.counter;
           float newHealth = playCardButton.enemyHealthAmount[enemyIndex];
    photonView.RPC("RPC_SyncEnemyHealth", RpcTarget.All, enemyIndex, newHealth);
       
   Log("Player attack animation complete - enemy damaged");
     }
       else
 {
      Log("Non-master: Player attack animation complete (visual only)");
      }
  });
    }
     }
     
    if (!hasAnimation)
        {
          // No animation - apply damage immediately on Master Client
   if (PhotonNetwork.IsMasterClient)
  {
    playCardButton.EnemyTakeDamage(1f);
  
     // Sync enemy health to all clients
  int enemyIndex = playCardButton.enemyManager.counter;
  float newHealth = playCardButton.enemyHealthAmount[enemyIndex];
        photonView.RPC("RPC_SyncEnemyHealth", RpcTarget.All, enemyIndex, newHealth);
  
      Log("No animation - enemy damaged immediately");
    }
   }
        
        // Sync correct answer to all players
        // Get the answer index from the current counter before it was incremented
        if (playCardButton != null && playCardButton.outputManager != null)
        {
            int outputIndex = playCardButton.outputManager.counter;
            // The answer index should be passed from PlayCardButton, but we'll use counter as fallback
            // This will be synced via SyncCardState which is called from PlayCardButton
            Log($"Correct answer played - will sync answer list for output {outputIndex}");
        }
        
        // NOTE: Turn advancement for correct answers is handled in RPC_SyncEnemyHealth
        // This ensures the turn advances whether the enemy dies or survives
 }
  else
   {
       // WRONG ANSWER - Trigger enemy attack animation and damage shared health
          if (PhotonNetwork.IsMasterClient)
      {
           Log("Master Client damaging shared health");
          
           // Find the player character and enemy to trigger attack animation
       bool hasAnimation = false;
       if (playCardButton != null && playCardButton.playerCharacter != null && 
 playCardButton.enemyManager.counter < playCardButton.enemyManager.enemies.Count)
       {
GameObject currentEnemy = playCardButton.enemyManager.enemies[playCardButton.enemyManager.counter];
   EnemyJumpAttack enemyJumpAttack = currentEnemy.GetComponent<EnemyJumpAttack>();

        if (enemyJumpAttack != null)
    {
          hasAnimation = true;
 delayHealthUIUpdate = true;
     
  // Trigger enemy attack animation
    enemyJumpAttack.PerformJumpAttack(playCardButton.playerCharacter.transform, () => {
       // Damage applied when animation hits
        DamageSharedHealth(1f);
     
  // Force UI update immediately after damage
     ForceUpdateHealthUI();
     delayHealthUIUpdate = false;
    });
     }
     }
  
          if (!hasAnimation)
{
 // No animation - apply damage immediately
DamageSharedHealth(1f);
  }
    
   Log("Wrong answer - damaging shared health and advancing turn after delay");
         
         // Force card reset for all players before advancing turn
         photonView.RPC("RPC_ForceCardReset", RpcTarget.All);
         
         // Advance turn after delay to allow animation to complete
         StartCoroutine(AdvanceTurnAfterDelay(1.0f));
    }
  else
        {
    // Non-master clients just play the animation visually
     if (playCardButton != null && playCardButton.playerCharacter != null && 
         playCardButton.enemyManager.counter < playCardButton.enemyManager.enemies.Count)
      {
    GameObject currentEnemy = playCardButton.enemyManager.enemies[playCardButton.enemyManager.counter];
     EnemyJumpAttack enemyJumpAttack = currentEnemy.GetComponent<EnemyJumpAttack>();
         
     if (enemyJumpAttack != null)
  {
   enemyJumpAttack.PerformJumpAttack(playCardButton.playerCharacter.transform, () => {
         Log("Non-master: Enemy attack animation complete (visual only)");
     });
       }
     }
    
        Log("Non-master client, waiting for sync");
  }
        }
    }
    
  /// <summary>
    /// Sync enemy health across all clients and update UI
    /// </summary>
    [PunRPC]
  void RPC_SyncEnemyHealth(int enemyIndex, float newHealth)
    {
        Log($"RPC_SyncEnemyHealth - Enemy {enemyIndex}, Health: {newHealth}");
     
        if (playCardButton == null || enemyIndex >= playCardButton.enemyHealthAmount.Count)
 {
          LogWarning($"Cannot sync health - invalid enemy index {enemyIndex}");
   return;
        }
      
        // Update enemy health amount
playCardButton.enemyHealthAmount[enemyIndex] = newHealth;
        
      // Update enemy health UI
        playCardButton.UpdateEnemyHealthUI();
        
  // Master Client checks if enemy is defeated
    if (PhotonNetwork.IsMasterClient)
    {
    if (newHealth <= 0f)
       {
       Log($"Enemy {enemyIndex} defeated!");
          photonView.RPC("RPC_OnEnemyDefeated", RpcTarget.All, enemyIndex);
          StartCoroutine(AdvanceToNextEnemyOrEnd());
   }
        else
            {
          // Enemy still alive - advance turn so next player can try
      Log("Enemy still alive - forcing card reset then advancing turn");
            
       // **CRITICAL: Force card reset BEFORE advancing turn**
       // This ensures cards are reset for the next player's turn
 photonView.RPC("RPC_ForceCardReset", RpcTarget.All);
         
         // Advance turn after a short delay to allow animations/UI updates
         // This ensures the turn always advances after a correct answer, even if enemy survives
         StartCoroutine(AdvanceTurnAfterDelay(1.0f));
  }
        }
    }
    
    /// <summary>
    /// Handle enemy defeat across all clients
  /// </summary>
    [PunRPC]
    void RPC_OnEnemyDefeated(int enemyIndex)
    {
     Log($"RPC_OnEnemyDefeated - Enemy {enemyIndex}");
        
     if (playCardButton != null)
 {
     playCardButton.DeactivateEnemy();
   playCardButton.DeactivateOutput();
      playCardButton.DeactivateAnswer();
        }
    }
    
    /// <summary>
    /// Advance to next enemy or end the game
    /// </summary>
    private IEnumerator AdvanceToNextEnemyOrEnd()
    {
  yield return new WaitForSeconds(1.5f);
     
        if (enemyManager == null || playCardButton == null) yield break;
        
        // Increment counters
     enemyManager.counter++;
        playCardButton.outputManager.counter++;
    playCardButton.counter = 0;
        
        // Sync counters to all clients
        photonView.RPC("RPC_SyncCounters", RpcTarget.All, 
 enemyManager.counter, 
          playCardButton.outputManager.counter, 
   playCardButton.counter);
    
     // Check if all enemies defeated
        if (enemyManager.counter >= enemyManager.enemies.Count)
 {
            Log("All enemies defeated - Game Over (Victory)");
            photonView.RPC("RPC_GameOver", RpcTarget.All, true);
        }
        else
     {
    // Activate next enemy
     Log($"Activating next enemy: {enemyManager.counter}");
     photonView.RPC("RPC_ActivateNextEnemy", RpcTarget.All);
            
       // Start timer and advance turn
      StartTimerForAllPlayers();
            yield return new WaitForSeconds(0.5f);
            
         if (turnSystem != null)
       {
      turnSystem.EndTurn();
            }
        }
    }
    
    /// <summary>
    /// Sync counters across all clients
    /// </summary>
    [PunRPC]
    void RPC_SyncCounters(int enemyCounter, int outputCounter, int playButtonCounter)
    {
        Log($"RPC_SyncCounters - Enemy: {enemyCounter}, Output: {outputCounter}, PlayButton: {playButtonCounter}");
    
 if (enemyManager != null) enemyManager.counter = enemyCounter;
        if (playCardButton != null && playCardButton.outputManager != null) playCardButton.outputManager.counter = outputCounter;
 if (playCardButton != null) playCardButton.counter = playButtonCounter;
    }
  
    /// <summary>
    /// Activate next enemy on all clients
    /// </summary>
    [PunRPC]
  void RPC_ActivateNextEnemy()
    {
        Log($"RPC_ActivateNextEnemy - Activating enemy {enemyManager.counter}");
     
        if (playCardButton != null)
        {
    playCardButton.ActivateEnemy();
     playCardButton.ActivateOutput();
        playCardButton.ActivateAnswer();
      }
    }
    
    private void SyncCorrectAnswerUI(int playerActorNumber, int answerIndex)
    {
        if (playCardButton != null && playCardButton.outputManager != null)
        {
    int currentOutput = playCardButton.outputManager.counter;
          
         if (currentOutput < playCardButton.outputManager.answerListContainer.Count &&
          answerIndex < playCardButton.outputManager.answerListContainer[currentOutput].answers.Count)
            {
                var answerObject = playCardButton.outputManager.answerListContainer[currentOutput].answers[answerIndex];
          answerObject.SetActive(true);
       Log($"Synced correct answer UI for output {currentOutput}, answer {answerIndex}");
        }
        }
    }
    
    /// <summary>
    /// Sync correct answer to all players when a player gets it right
    /// </summary>
    public void SyncCorrectAnswerToAll(int outputIndex, int answerIndex)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Log($"Syncing correct answer to all players - Output: {outputIndex}, Answer: {answerIndex}");
            photonView.RPC("RPC_SyncCorrectAnswer", RpcTarget.All, outputIndex, answerIndex);
        }
    }
    
    [PunRPC]
    void RPC_SyncCorrectAnswer(int outputIndex, int answerIndex)
    {
        Log($"RPC_SyncCorrectAnswer received - Output: {outputIndex}, Answer: {answerIndex}");
        
        if (playCardButton != null && playCardButton.outputManager != null)
        {
            // Ensure output index is valid
            if (outputIndex >= 0 && outputIndex < playCardButton.outputManager.answerListContainer.Count)
            {
                var answerContainer = playCardButton.outputManager.answerListContainer[outputIndex];
                
                // Ensure answer index is valid
                if (answerIndex >= 0 && answerIndex < answerContainer.answers.Count)
                {
                    var answerObject = answerContainer.answers[answerIndex];
                    if (answerObject != null)
                    {
                        answerObject.SetActive(true);
                        Log($"Correct answer activated for all players - Output: {outputIndex}, Answer: {answerIndex}");
                    }
                    else
                    {
                        LogWarning($"Answer object is null at Output: {outputIndex}, Answer: {answerIndex}");
                    }
                }
                else
                {
                    LogWarning($"Answer index {answerIndex} out of range for output {outputIndex} (max: {answerContainer.answers.Count - 1})");
                }
            }
            else
            {
                LogWarning($"Output index {outputIndex} out of range (max: {playCardButton.outputManager.answerListContainer.Count - 1})");
            }
        }
        else
        {
            LogError("PlayCardButton or OutputManager is null - cannot sync answer!");
        }
    }
    
  public void SyncCardState(int cardCounter, int playButtonCounter, int outputCounter, int answerIndex)
    {
        photonView.RPC("RPC_SyncCardState", RpcTarget.All, cardCounter, playButtonCounter, outputCounter, answerIndex);
}
    
    [PunRPC]
    void RPC_SyncCardState(int cardCounter, int playButtonCounter, int outputCounter, int answerIndex)
    {
      Log($"Syncing card state - Card: {cardCounter}, PlayButton: {playButtonCounter}, Output: {outputCounter}, Answer: {answerIndex}");
     
   if (cardManager != null) cardManager.counter = cardCounter;
      if (playCardButton != null) playCardButton.counter = playButtonCounter;
        if (playCardButton != null && playCardButton.outputManager != null) playCardButton.outputManager.counter = outputCounter;
   
    // Sync correct answer to all players (answer index is already synced via RPC_SyncCorrectAnswer)
        // The answer list update is handled separately via RPC_SyncCorrectAnswer which is called from SyncCorrectAnswerToAll
        if (answerIndex >= 0 && playCardButton != null && playCardButton.outputManager != null)
        {
            // Directly update the answer UI for all clients
            if (outputCounter >= 0 && outputCounter < playCardButton.outputManager.answerListContainer.Count &&
                answerIndex >= 0 && answerIndex < playCardButton.outputManager.answerListContainer[outputCounter].answers.Count)
            {
                var answerObject = playCardButton.outputManager.answerListContainer[outputCounter].answers[answerIndex];
                if (answerObject != null)
                {
                    answerObject.SetActive(true);
                    Log($"Answer list updated - Output: {outputCounter}, Answer: {answerIndex}");
                }
            }
        }
    }
    
    public void OnEnemyDefeated()
    {
        if (PhotonNetwork.IsMasterClient)
  {
            StartCoroutine(NextTurnAfterDelay(1.5f));
     }
    }
    
    public void AdvanceTurn()
    {
        if (PhotonNetwork.IsMasterClient && turnSystem != null)
        {
            Log("Advancing turn manually");
         turnSystem.EndTurn();
        }
    }
    
    private IEnumerator NextTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (AreAllEnemiesDead())
  {
   photonView.RPC("RPC_GameOver", RpcTarget.All, true);
        }
        else
        {
 if (turnSystem != null)
         {
    Log("Moving to next turn after delay");
             turnSystem.EndTurn();
        }
        }
    }
    
    private IEnumerator AdvanceTurnAfterDelay(float delay)
    {
   Log($"===== ADVANCE TURN AFTER DELAY: {delay}s =====");
        Log($"Master Client: {PhotonNetwork.IsMasterClient}");
        Log($"Current turn player: {turnSystem?.GetCurrentTurnPlayer()?.NickName}");
  
        yield return new WaitForSeconds(delay);
     
        Log($"Delay complete - advancing turn now");
      
   if (turnSystem != null)
        {
      Log("Calling turnSystem.EndTurn()");
        turnSystem.EndTurn();
            Log("turnSystem.EndTurn() completed");
   }
        else
        {
LogError("CRITICAL: turnSystem is NULL! Cannot advance turn!");
        }
    }
  
    /// <summary>
    /// Handle turn timeout - treat like a wrong answer (damage shared health and advance turn)
    /// </summary>
    public void OnTurnTimedOut()
    {
   if (!PhotonNetwork.IsMasterClient)
        {
   LogWarning("OnTurnTimedOut called on non-Master Client - ignoring");
return;
  }
        
  Log("===== TURN TIMED OUT =====");
      
        // Damage shared health (timeout is like a wrong answer)
        DamageSharedHealth(1f);
  Log("Shared health damaged due to timeout");
    
        // Force card reset for all players
        photonView.RPC("RPC_ForceCardReset", RpcTarget.All);
        Log("Cards reset due to timeout");
        
  // Advance turn after delay
     StartCoroutine(AdvanceTurnAfterDelay(1.0f));
    }
    
    #endregion
    
    #region Enemy Management
    
    private bool AreAllEnemiesDead()
    {
        if (enemyManager == null) return false;
        
 foreach (var enemy in enemyManager.enemies)
    {
         if (enemy != null && enemy.activeSelf)
            {
  return false;
         }
        }
   
      return true;
    }
    
    #endregion
  
    #region Game Over
    
    [PunRPC]
    void RPC_GameOver(bool victory)
    {
    isGameOver = true;
     DisablePlayerControls();
        HideMultiplayerUI();
        
    if (victory)
        {
            if (victoryPanel != null) victoryPanel.SetActive(true);
            if (gameOverText != null) gameOverText.text = "VICTORY!\nAll enemies defeated!";
   }
        else
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
if (gameOverText != null) gameOverText.text = "GAME OVER\nShared health depleted!";
 }
    }
    
    public void RestartLevel()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    }
    
    public void ReturnToLobby()
    {
        PhotonNetwork.LoadLevel("MultiplayerLobby");
  }
    
    #endregion
    
    #region Player List UI
    
    private void UpdatePlayerListUI()
 {
   if (playerListContainer == null)
        {
            LogWarning("Player list container is NULL!");
            return;
        }
        
        if (playerListItemPrefab == null)
        {
            LogWarning("Player list item prefab is NULL!");
            return;
        }
        
   // Clear existing items
        foreach (Transform child in playerListContainer)
        {
       Destroy(child.gameObject);
        }
        
        Log($"Creating player list for {PhotonNetwork.PlayerList.Length} players");
        
        foreach (Player player in PhotonNetwork.PlayerList)
      {
        GameObject listItem = Instantiate(playerListItemPrefab, playerListContainer);
            listItem.SetActive(true);
            
            TextMeshProUGUI nameText = listItem.GetComponentInChildren<TextMeshProUGUI>();
         if (nameText != null)
    {
              nameText.text = player.NickName;
            
   if (player.IsLocal)
  {
         nameText.text += " (You)";
     nameText.color = Color.green;
            }
   
            Log($"Added player: {nameText.text}");
       }
         else
    {
       LogWarning("No TextMeshProUGUI found in player list item prefab!");
      }
        }
        
        playerListContainer.gameObject.SetActive(true);
    }
    
    #endregion
    
    #region Photon Callbacks
    
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
     if (propertiesThatChanged.ContainsKey("SharedHealth"))
        {
     UpdateHealthUI();
      }
    }
 
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
   if (playerCharacters.ContainsKey(otherPlayer.ActorNumber))
    {
        Destroy(playerCharacters[otherPlayer.ActorNumber]);
     playerCharacters.Remove(otherPlayer.ActorNumber);
        }
  
        UpdatePlayerListUI();
        
   // NEW: If a player leaves during an active game, return all players to lobby
 if (!isGameOver && PhotonNetwork.InRoom)
        {
    Log($"Player {otherPlayer.NickName} left during game - returning all players to lobby");
     
        // Show message to remaining players
            if (gameOverText != null)
            {
 gameOverText.text = $"Player {otherPlayer.NickName} left the game!\nReturning to lobby...";
}
        
            // Delay slightly to show message, then return to lobby
          StartCoroutine(ReturnToLobbyAfterDelay(2f));
   }
    }
  
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerListUI();
    }
    
    /// <summary>
    /// Return all players to lobby after a delay
    /// </summary>
    private IEnumerator ReturnToLobbyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (PhotonNetwork.InRoom)
        {
            Log("Leaving room and returning to lobby...");
    PhotonNetwork.LeaveRoom();
        }
    }
    
    /// <summary>
    /// Callback when we leave a room - return to lobby scene
  /// </summary>
    public override void OnLeftRoom()
    {
        Log("Successfully left room - loading lobby scene");
        PhotonNetwork.LoadLevel("MultiplayerLobby");
    }
    
    #endregion
}
