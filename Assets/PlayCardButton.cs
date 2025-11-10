using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class PlayCardButton : MonoBehaviour
{
    public int counter = 0;

    public int newMoney;
    public int nextLevel;

    public Timer timer;
    public CardManager cardManager;
    public EnemyManager enemyManager;
    public OutputManager outputManager;
    public MoneyManager moneyManager;

    [SerializeField] GameObject GameOverScreen;
    [SerializeField] GameObject YouWinScreen;

    [SerializeField] GameObject Reward;

    [SerializeField] TextMeshProUGUI playerHP;
    [SerializeField] TextMeshProUGUI enemyHP;

    public Item card;

    [Header("Attack Animation")]
    [Tooltip("Reference to the player character GameObject with CharacterJumpAttack component")]
    public GameObject playerCharacter;

    [Tooltip("Enable/disable jump attack animation")]
    public bool useJumpAttackAnimation = true;

    public CharacterJumpAttack playerJumpAttack;

    public List<CorrectAnswerContainer> correctAnswersContainer = new List<CorrectAnswerContainer>();
    [System.Serializable]
    public class CorrectAnswerContainer
    {
        public List<string> correctAnswers = new List<string>();
    }

    public Image playerHealthBar;
    public List<GameObject> enemyHealthBarObject = new List<GameObject>();
    public List<Image> enemyHealthBar = new List<Image>();

    public List<float> enemyHealthAmount = new List<float>();
    public List<float> enemyHealthTotal = new List<float>();

    [SerializeField] public float playerHealthAmount;
    [SerializeField] public float playerHealthTotal;

    Image cardDesign;

    Transform parent;
    [SerializeField] Transform playedCard;
    
    // Button reference to enable/disable based on card presence
    private Button playButton;

    // Multiplayer support (uses reflection to avoid compile errors)
    private bool isMultiplayerMode = false;
    
    // Animation sync: delay UI updates until attack animations complete
    private bool delayHealthUIUpdate = false;
    private float pendingEnemyHealth = -1f;
    private float pendingPlayerHealth = -1f;

    private void Start()
    {
        cardManager = FindAnyObjectByType<CardManager>();
        
 // Get Button component
   playButton = GetComponent<Button>();
        if (playButton == null)
      {
            Debug.LogWarning("[PlayCardButton] No Button component found on this GameObject!");
        }

        if (playerCharacter != null)
        {
          playerJumpAttack = playerCharacter.GetComponent<CharacterJumpAttack>();
            if (playerJumpAttack == null && useJumpAttackAnimation)
  {
        Debug.LogWarning("CharacterJumpAttack component not found on player character. Add it to enable jump attack animation.");
 }
        }

     // Check if in multiplayer mode using reflection
        DetectMultiplayerMode();
    }

    private void DetectMultiplayerMode()
    {
        try
        {
            // Try to find PhotonNetwork class using reflection
            System.Type photonNetworkType = System.Type.GetType("Photon.Pun.PhotonNetwork, PhotonUnityNetworking");

            if (photonNetworkType != null)
            {
                // Get IsConnected property
                var isConnectedProp = photonNetworkType.GetProperty("IsConnected");
                var inRoomProp = photonNetworkType.GetProperty("InRoom");

                if (isConnectedProp != null && inRoomProp != null)
                {
                    bool isConnected = (bool)isConnectedProp.GetValue(null);
                    bool inRoom = (bool)inRoomProp.GetValue(null);

                    isMultiplayerMode = isConnected && inRoom;

                    if (isMultiplayerMode)
                    {
                        Debug.Log("[PlayCardButton] Multiplayer mode detected");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PlayCardButton] Could not detect Photon: {ex.Message}");
            isMultiplayerMode = false;
        }
    }

    private void Update()
    {
        // Enable/disable button based on whether there's a card in PlayedCard holder
 if (playButton != null)
        {
        parent = transform.Find("PlayedCard");
            bool hasCard = parent != null && parent.childCount > 0;
          playButton.interactable = hasCard;
        }
        
    // Health and UI updates
        if (!isMultiplayerMode)
 {
        // Single player mode
  if (playerHealthAmount <= 0f)
            {
     GameOverScreen.SetActive(true);
  }

   playerHP.text = playerHealthAmount.ToString() + " / " + playerHealthTotal.ToString();
            playerHealthBar.fillAmount = playerHealthAmount / playerHealthTotal;
     }
        else
   {
     // Multiplayer mode: manage card visibility based on turn
  UpdateCardVisibility();
        }

   // Enemy health display - only update if not waiting for animation
  if (!delayHealthUIUpdate && enemyManager.counter < enemyHealthAmount.Count)
   {
      enemyHP.text = enemyHealthAmount[enemyManager.counter].ToString() + " / " + enemyHealthTotal[enemyManager.counter].ToString();
         enemyHealthBar[enemyManager.counter].fillAmount = enemyHealthAmount[enemyManager.counter] / enemyHealthTotal[enemyManager.counter];
  }

     // Victory check (moved here to work in both modes)
  if (enemyManager.counter >= enemyManager.enemies.Count)
{
     if (!isMultiplayerMode)
          {
         nextLevel = SceneManager.GetActiveScene().buildIndex + 1;
       if (nextLevel > PlayerPrefs.GetInt("levelAt"))
     {
       PlayerPrefs.SetInt("levelAt", nextLevel);
      }
    }
   YouWinScreen.SetActive(true);
 }
    }
    /// <summary>
    /// Update card visibility based on whose turn it is
    /// ALL players see the SAME cards, just hidden when not their turn
    /// </summary>
    private void UpdateCardVisibility()
    {
        // Only manage card visibility in multiplayer mode
        if (!isMultiplayerMode)
        {
            return; // In single player, cards should always be visible
        }

        bool isMyTurn = IsMyTurn();

        // Hide/show the entire card grid when it's not your turn
        if (cardManager != null && cardManager.grid != null)
        {
            cardManager.grid.SetActive(isMyTurn);
        }
    }

    public void PlayButton()
    {
    // Check if it's player's turn in multiplayer using reflection
        if (isMultiplayerMode && !IsMyTurn())
        {
            Debug.Log("[PlayCardButton] Not your turn!");
     return;
        }

        // SAFETY CHECK: Validate PlayedCard parent exists
    parent = transform.Find("PlayedCard");
   if (parent == null)
        {
   // Silently do nothing - no error, no log spam
      return;
        }
        
        // SAFETY CHECK: Validate parent has a child card
    if (parent.childCount == 0)
   {
 // Silently do nothing - no card to play
  return;
        }
        
      playedCard = parent.GetChild(0);
     
        // SAFETY CHECK: Validate we got the card
  if (playedCard == null)
 {
        // Silently do nothing
       return;
     }
        
        // Store the played card name for checking
        string playedCardName = playedCard.name;

  for (int i = 0; i < correctAnswersContainer[outputManager.counter].correctAnswers.Count; i++)
        {
            if (counter == i)
            {
                if (playedCard.name == correctAnswersContainer[outputManager.counter].correctAnswers[counter])
                {
   // CORRECT ANSWER
  // Store the current answer index BEFORE incrementing
        int currentAnswerIndex = counter;

     // Increment counters BEFORE notifying multiplayer so sync gets correct values
      cardManager.counter++;
     counter++;

      // Notify multiplayer manager with updated counters AND the answer index before increment
      if (isMultiplayerMode)
       {
   NotifyMultiplayerCardPlayed(true, currentAnswerIndex);
         }

       if (useJumpAttackAnimation && playerJumpAttack != null && enemyManager.counter < enemyManager.enemies.Count)
     {
       GameObject currentEnemy = enemyManager.enemies[enemyManager.counter];
       
    // Mark that we're delaying UI updates until animation hits
       delayHealthUIUpdate = true;

    playerJumpAttack.PerformJumpAttack(currentEnemy.transform, () => {
     // This callback fires when the attack actually hits
        EnemyTakeDamage(1);
       
      // Show the correct answer marker when attack hits
     outputManager.answerListContainer[outputManager.counter].answers[currentAnswerIndex].SetActive(true);
   
      // Update UI immediately after damage is applied
   UpdateEnemyHealthUI();
     delayHealthUIUpdate = false;
     
  CheckEnemyDefeat(currentAnswerIndex);
       });
    }
        else
    {
         EnemyTakeDamage(1);
     // Show the correct answer marker immediately (no animation)
       outputManager.answerListContainer[outputManager.counter].answers[currentAnswerIndex].SetActive(true);
           UpdateEnemyHealthUI();
        CheckEnemyDefeat(currentAnswerIndex);
   }
   }
         else
                {
 // WRONG ANSWER
                // Notify multiplayer manager (counters don't change)
         if (isMultiplayerMode)
      {
         NotifyMultiplayerCardPlayed(false, -1);
         // Turn advancement and card reset will be handled by SharedMultiplayerGameManager
            }
          else
                 {
  // Single player: apply damage locally
          if (useJumpAttackAnimation && playerCharacter != null && enemyManager.counter < enemyManager.enemies.Count)
     {
     GameObject currentEnemy = enemyManager.enemies[enemyManager.counter];
EnemyJumpAttack enemyJumpAttack = currentEnemy.GetComponent<EnemyJumpAttack>();

               if (enemyJumpAttack != null)
        {
           // Mark that we're delaying UI updates until animation hits
         delayHealthUIUpdate = true;
    
        enemyJumpAttack.PerformJumpAttack(playerCharacter.transform, () => {
          // This callback fires when the attack actually hits
     PlayerTakeDamage(1);
             
    // Update UI immediately after damage is applied
  UpdatePlayerHealthUI();
       delayHealthUIUpdate = false;
     });
          }
         else
  {
      PlayerTakeDamage(1);
      UpdatePlayerHealthUI();
  }
   }
  else
          {
PlayerTakeDamage(1);
         UpdatePlayerHealthUI();
         }
     }

         if (!isMultiplayerMode)
               {
   cardManager.ResetCards();
              cardManager.StartCoroutine(cardManager.Randomize());
        }
     }
         
         // CRITICAL FIX: Clear the played card IMMEDIATELY after playing in multiplayer
      // This prevents it from getting stuck when turn changes
             if (isMultiplayerMode && parent != null && parent.childCount > 0)
                {
          Debug.Log($"[PlayCardButton] Immediately clearing played card: {playedCardName}");
    Transform cardToMove = parent.GetChild(0);
   cardToMove.SetParent(cardManager.grid.transform);
        cardToMove.gameObject.SetActive(false);
    cardToMove.localScale = Vector3.one;
    Debug.Log($"[PlayCardButton] Card cleared from PlayedCard holder");
      }
      
    break;
            }
        }
    }

    /// <summary>
    /// Notify SharedMultiplayerGameManager that a card was played
    /// </summary>
    private void NotifyMultiplayerCardPlayed(bool wasCorrect, int answerIndex)
    {
        Debug.Log($"[PlayCardButton] Attempting to notify multiplayer manager: {(wasCorrect ? "Correct" : "Wrong")}");

        try
        {
            // Method 1: Direct find (faster, preferred)
            var manager = GameObject.FindObjectOfType<SharedMultiplayerGameManager>();
            if (manager != null)
            {
                manager.OnCardPlayed(wasCorrect);

                // Sync card manager state
                if (wasCorrect)
                {
                    // Sync the counter advancement with the answer index BEFORE increment
                    manager.SyncCardState(cardManager.counter, counter, outputManager.counter, answerIndex);
                }

                Debug.Log($"[PlayCardButton] SUCCESS - Notified via direct reference");
                return;
            }

            // Method 2: Reflection fallback
            var managerType = System.Type.GetType("SharedMultiplayerGameManager");
            if (managerType != null)
            {
                var managerObj = GameObject.FindObjectOfType(managerType);
                if (managerObj != null)
                {
                    // Call OnCardPlayed method
                    var method = managerType.GetMethod("OnCardPlayed");
                    if (method != null)
                    {
                        method.Invoke(managerObj, new object[] { wasCorrect });

                        // Sync card state if correct
                        if (wasCorrect)
                        {
                            var syncMethod = managerType.GetMethod("SyncCardState");
                            if (syncMethod != null)
                            {
                                syncMethod.Invoke(managerObj, new object[] { cardManager.counter, counter, outputManager.counter, answerIndex });
                            }
                        }

                        Debug.Log($"[PlayCardButton] SUCCESS - Notified via reflection");
                        return;
                    }
                }
            }

            Debug.LogError("[PlayCardButton] FAILED - SharedMultiplayerGameManager not found in scene!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayCardButton] ERROR notifying multiplayer manager: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Check if it's the local player's turn using reflection
    /// </summary>
    private bool IsMyTurn()
    {
        // If not in multiplayer mode, always return true (single player)
        if (!isMultiplayerMode)
        {
            return true;
        }

        try
        {
            // Check if we're alone in the room (for testing)
            System.Type photonNetworkType = System.Type.GetType("Photon.Pun.PhotonNetwork, PhotonUnityNetworking");
            if (photonNetworkType != null)
            {
                var currentRoomProp = photonNetworkType.GetProperty("CurrentRoom");
                if (currentRoomProp != null)
                {
                    var currentRoom = currentRoomProp.GetValue(null);
                    if (currentRoom != null)
                    {
                        var playerCountProp = currentRoom.GetType().GetProperty("PlayerCount");
                        if (playerCountProp != null)
                        {
                            int playerCount = (int)playerCountProp.GetValue(currentRoom);
                            if (playerCount == 1)
                            {
                                // Solo in multiplayer room - allow playing for testing
                                Debug.Log("[PlayCardButton] Solo in multiplayer room - allowing turn");
                                return true;
                            }
                        }
                    }
                }
            }

            // Find CodexMultiplayerIntegration using reflection
            var integration = GameObject.FindObjectOfType(System.Type.GetType("CodexMultiplayerIntegration"));
            if (integration != null)
            {
                // Call IsMyTurn() method
                var method = integration.GetType().GetMethod("IsMyTurn");
                if (method != null)
                {
                    return (bool)method.Invoke(integration, null);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[PlayCardButton] Could not check turn: {ex.Message}");
        }

        // If can't determine or multiplayer not fully set up, allow the action
        return true;
    }

    public void PlayerTakeDamage(float damage)
    {
        playerHealthAmount -= damage;
        playerHealthBar.fillAmount = playerHealthAmount / playerHealthTotal;
    }

    public void EnemyTakeDamage(float damage)
    {
        enemyHealthAmount[enemyManager.counter] -= damage;
        // Don't update UI here - it will be updated after animation
    }
    
    /// <summary>
    /// Update enemy health UI after animation completes
    /// </summary>
    public void UpdateEnemyHealthUI()
    {
        if (enemyManager.counter < enemyHealthAmount.Count)
 {
enemyHP.text = enemyHealthAmount[enemyManager.counter].ToString() + " / " + enemyHealthTotal[enemyManager.counter].ToString();
     enemyHealthBar[enemyManager.counter].fillAmount = enemyHealthAmount[enemyManager.counter] / enemyHealthTotal[enemyManager.counter];
      }
    }
    
    /// <summary>
    /// Update player health UI after animation completes
    /// </summary>
    private void UpdatePlayerHealthUI()
    {
   if (!isMultiplayerMode)
        {
       playerHP.text = playerHealthAmount.ToString() + " / " + playerHealthTotal.ToString();
         playerHealthBar.fillAmount = playerHealthAmount / playerHealthTotal;
        }
    }

    public void DeactivateEnemy()
    {
        enemyManager.enemies[enemyManager.counter].SetActive(false);
        enemyHealthBarObject[enemyManager.counter].SetActive(false);
    }

    public void ActivateEnemy()
    {
        enemyManager.enemies[enemyManager.counter].SetActive(true);
        enemyHealthBarObject[enemyManager.counter].SetActive(true);
    }

    public void DeactivateOutput()
    {
        outputManager.codes[outputManager.counter].SetActive(false);
        outputManager.outputs[outputManager.counter].SetActive(false);
    }

    public void ActivateOutput()
    {
        outputManager.codes[outputManager.counter].SetActive(true);
        outputManager.outputs[outputManager.counter].SetActive(true);
    }

    public void DeactivateAnswer()
    {
        if (enemyManager.counter < enemyHealthAmount.Count)
        {
            outputManager.answerList[outputManager.counter].SetActive(false);
        }
    }

    public void ActivateAnswer()
    {
        outputManager.answerList[outputManager.counter].SetActive(true);
    }

    private string GetLevelRewardLockKey()
    {
        int currentSlot = 0;
        if (NewAndLoadGameManager.Instance != null)
        {
            currentSlot = NewAndLoadGameManager.Instance.CurrentSlot;
            string userId = "";

            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.GetCurrentPlayerData() != null)
            {
                userId = PlayerDataManager.Instance.GetCurrentPlayerData().user_id;
            }

            if (!string.IsNullOrEmpty(userId))
            {
                return $"{userId}_rewardLock_Slot{currentSlot}_Level_" + SceneManager.GetActiveScene().buildIndex;
            }
        }
        return $"rewardLock_Slot{currentSlot}_Level_" + SceneManager.GetActiveScene().buildIndex;
    }

    private void CheckEnemyDefeat(int currentAnswerIndex)
    {
        if (enemyHealthAmount[enemyManager.counter] <= 0f)
        {
         DeactivateEnemy();
     DeactivateOutput();
            DeactivateAnswer();
            enemyManager.counter++;
    outputManager.counter++;
     counter = 0;

      if (enemyManager.counter >= enemyManager.enemies.Count)
  {
         // All enemies defeated
       Invoke("ShowWinScreen", 1.0f);
     }
    else if (enemyManager.counter < enemyHealthAmount.Count)
 {
          ActivateEnemy();
      ActivateOutput();
        ActivateAnswer();

     if (isMultiplayerMode)
    {
              // Multiplayer: OnTurnChanged will handle card reset/randomize
    // Just start timer and advance turn
         NotifyStartTimer();
     NotifyAdvanceTurn();
                }
 else
    {
         // Single player: reset cards and start timer
           cardManager.ResetCards();
              cardManager.StartCoroutine(cardManager.Randomize());
          timer.ResetTimer();
          timer.StartTimer();
       }
            }
        }
        else
        {
        // Enemy not defeated - question answered correctly but enemy still alive
            // Cards already advanced (counter++) in PlayButton
  // Just advance turn so next player can try next question
   
            if (isMultiplayerMode)
   {
          // Multiplayer: OnTurnChanged will handle card reset/randomize
     // Just start timer and advance turn
     NotifyStartTimer();
      NotifyAdvanceTurn();
  }
            else
      {
    // Single player: reset cards for next question and start timer
                cardManager.ResetCards();
     cardManager.StartCoroutine(cardManager.Randomize());
    timer.ResetTimer();
      timer.StartTimer();
            }
        }
    }

    /// <summary>
    /// Notify SharedMultiplayerGameManager to start timer for all players
    /// </summary>
    private void NotifyStartTimer()
    {
        try
        {
            // Method 1: Direct find
            var manager = GameObject.FindObjectOfType<SharedMultiplayerGameManager>();
            if (manager != null)
            {
                manager.StartTimerForAllPlayers();
                Debug.Log("[PlayCardButton] Notified timer start via direct reference");

                // Cards will be reset by OnTurnChanged in SharedMultiplayerGameManager
                return;
            }

            // Method 2: Reflection fallback
            var managerType = System.Type.GetType("SharedMultiplayerGameManager");
            if (managerType != null)
            {
                var managerObj = GameObject.FindObjectOfType(managerType);
                if (managerObj != null)
                {
                    var method = managerType.GetMethod("StartTimerForAllPlayers");
                    if (method != null)
                    {
                        method.Invoke(managerObj, null);
                        Debug.Log("[PlayCardButton] Notified timer start via reflection");

                        // Cards will be reset by OnTurnChanged in SharedMultiplayerGameManager
                        return;
                    }
                }
            }

            Debug.LogWarning("[PlayCardButton] Could not notify timer start - manager not found");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayCardButton] Error starting synced timer: {ex.Message}");
        }
    }

    /// <summary>
    /// Notify SharedMultiplayerGameManager to advance to next turn
    /// </summary>
    private void NotifyAdvanceTurn()
    {
        try
        {
            // Method 1: Direct find
            var manager = GameObject.FindObjectOfType<SharedMultiplayerGameManager>();
            if (manager != null)
            {
                manager.AdvanceTurn();
                Debug.Log("[PlayCardButton] Notified turn advancement via direct reference");
                return;
            }

            // Method 2: Reflection fallback
            var managerType = System.Type.GetType("SharedMultiplayerGameManager");
            if (managerType != null)
            {
                var managerObj = GameObject.FindObjectOfType(managerType);
                if (managerObj != null)
                {
                    var method = managerType.GetMethod("AdvanceTurn");
                    if (method != null)
                    {
                        method.Invoke(managerObj, null);
                        Debug.Log("[PlayCardButton] Notified turn advancement via reflection");
                        return;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayCardButton] Error advancing turn: {ex.Message}");
        }

        Debug.LogWarning("[PlayCardButton] Could not notify turn advancement - manager not found");
    }

    private void ShowWinScreen()
    {
        // Only save progress if NOT in multiplayer mode
        if (!isMultiplayerMode)
        {
       nextLevel = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextLevel > PlayerPrefs.GetInt("levelAt"))
            {
 PlayerPrefs.SetInt("levelAt", nextLevel);

           if (PlayerDataManager.Instance != null)
           {
          PlayerDataManager.Instance.UpdateLevelsUnlocked(nextLevel);
                }
  }

            // CRITICAL: Don't award money for tutorial level (scene index 5)
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
 bool isTutorialLevel = (currentSceneIndex == 5);
       
   if (!isTutorialLevel)
            {
   string lockKey = GetLevelRewardLockKey();
     if (PlayerPrefs.GetInt(lockKey, 0) == 0)
            {
    newMoney = moneyManager.moneyCount + moneyManager.rewardAmount;
        PlayerPrefs.SetInt("moneyCount", newMoney);
    PlayerPrefs.SetInt(lockKey, 1);
            Reward.SetActive(true);

        if (PlayerDataManager.Instance != null)
   {
   PlayerDataManager.Instance.UpdateMoney(newMoney);
         }
                }
            }
    else
  {
             Debug.Log("[PlayCardButton] Tutorial level - no money reward");
     }

  if (NewAndLoadGameManager.Instance != null)
            {
  NewAndLoadGameManager.Instance.AutoSave();
            }

      if (CardCollectionManager.Instance != null)
            {
    int currentLevel = SceneManager.GetActiveScene().buildIndex;
                CardCollectionManager.Instance.UnlockCardsForLevel(currentLevel);
            }
 }
        else
        {
            Debug.Log("[PlayCardButton] Multiplayer mode - progress NOT saved to single-player");
        }

        // Always show win screen
        if (YouWinScreen != null)
        {
            YouWinScreen.SetActive(true);
        }
    }
}