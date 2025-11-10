using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Tutorial system for scene index 5 (Tutorial Level)
/// Pauses timer for THE ENTIRE tutorial duration - no timer countdown during tutorial
/// Shows correct/wrong panels after player plays a card
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial Panel")]
    public GameObject tutorialPanel;
    
    [Tooltip("Optional: Reference to lore dialogue system")]
    public TutorialLoreDialogue loreDialogue;
    
    [Header("Instruction Panels")]
    [Tooltip("Panel prompting player to play a card")]
    public GameObject playCardPromptPanel;
    
    [Header("Feedback Panels")]
    [Tooltip("Panel to show when player plays WRONG card")]
    public GameObject wrongAnswerPanel;
  
    [Tooltip("Panel to show when player plays CORRECT card")]
    public GameObject correctAnswerPanel;
    
    private MonoBehaviour timerComponent;
    private MonoBehaviour outputComponent;
    private MonoBehaviour playCardButtonComponent;
    
    private bool isTutorial = false;
  private bool hasTriggered = false;
    private bool isWaitingForCardPlay = false; // Track if we're waiting for player to play a card
    private bool timerPausedForTutorial = false; // Track if timer is paused for tutorial
    
    private void Start()
    {
  // Only work in tutorial level (scene 5)
   isTutorial = (SceneManager.GetActiveScene().buildIndex == 5);
      
    if (!isTutorial)
        {
            enabled = false;
         return;
        }
        
        // Find timer component
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
 {
          if (timerComponent == null)
         {
             timerComponent = obj.GetComponent("Timer") as MonoBehaviour;
            }
            if (outputComponent == null)
            {
      outputComponent = obj.GetComponent("OutputManager") as MonoBehaviour;
   }
      if (playCardButtonComponent == null)
    {
    playCardButtonComponent = obj.GetComponent("PlayCardButton") as MonoBehaviour;
            }
if (timerComponent != null && outputComponent != null && playCardButtonComponent != null)
                break;
    }
        
      // Hide panel initially
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
   }
  
        // If lore dialogue exists, let it handle showing the tutorial panel
        // Otherwise, show tutorial panel immediately
   if (loreDialogue == null && tutorialPanel != null)
        {
            // No lore dialogue, show tutorial panel immediately
      // (Keep hidden for now until output is clicked)
   }
        
      // Hide prompt panel initially
        if (playCardPromptPanel != null)
   {
    playCardPromptPanel.SetActive(false);
  }
        
        // Hide feedback panels initially
        if (wrongAnswerPanel != null)
        {
            wrongAnswerPanel.SetActive(false);
        }
    
   if (correctAnswerPanel != null)
        {
            correctAnswerPanel.SetActive(false);
        }
  
        // Setup click handlers
        SetupClicks();
    }
  
    private void SetupClicks()
    {
        if (outputComponent == null) return;
     
        // Get the outputs list using reflection
        var outputsField = outputComponent.GetType().GetField("outputs");
        if (outputsField == null) return;
        
        var outputsList = outputsField.GetValue(outputComponent) as List<GameObject>;
        if (outputsList == null) return;
        
        foreach (GameObject output in outputsList)
    {
        if (output == null) continue;
         
  EventTrigger trigger = output.GetComponent<EventTrigger>();
            if (trigger == null)
 {
   trigger = output.AddComponent<EventTrigger>();
}
      
 EventTrigger.Entry entry = new EventTrigger.Entry();
      entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { OnOutputClick(); });
         trigger.triggers.Add(entry);
   }
    }
    
    private void OnOutputClick()
    {
        if (hasTriggered) return;
        
        hasTriggered = true;
        
        // Pause timer for the ENTIRE tutorial duration
        if (timerComponent != null)
        {
            timerComponent.SendMessage("PauseTimer");
            timerPausedForTutorial = true;
      Debug.Log("[TutorialManager] ?? Timer PAUSED for entire tutorial duration");
        }
        
        // Show panel
        if (tutorialPanel != null)
    {
            tutorialPanel.SetActive(true);
        }
    }
  
    /// <summary>
    /// Call from button to close panel
    /// Timer stays PAUSED - does NOT resume
    /// </summary>
    public void ClosePanel()
    {
        if (tutorialPanel != null)
        {
       tutorialPanel.SetActive(false);
     }
        
        // ? CHANGED: Timer stays paused - do NOT resume
        Debug.Log("[TutorialManager] Panel closed, timer remains PAUSED");
        
   // Show the prompt panel
        if (playCardPromptPanel != null)
      {
            playCardPromptPanel.SetActive(true);
            Debug.Log("[TutorialManager] Showing play card prompt panel");
        }
  else
        {
   // If no prompt panel, start waiting for card play
   // Timer is still paused!
            isWaitingForCardPlay = true;
   Debug.Log("[TutorialManager] Waiting for player to play a card (timer still paused)...");
        }
    }
    
    /// <summary>
    /// Close the play card prompt panel
    /// Timer stays PAUSED - does NOT resume
    /// Call this from the "Got it" or "OK" button on the prompt panel
    /// </summary>
    public void ClosePlayCardPrompt()
    {
        Debug.Log("[TutorialManager] Closing play card prompt");
   
        if (playCardPromptPanel != null)
{
   playCardPromptPanel.SetActive(false);
        }
        
        // ? CHANGED: Timer stays paused - do NOT resume
    Debug.Log("[TutorialManager] Prompt closed, timer remains PAUSED");
 
  // Start waiting for player to play a card
   isWaitingForCardPlay = true;
        Debug.Log("[TutorialManager] Waiting for player to play a card (timer still paused)...");
    }
    
    /// <summary>
    /// DEPRECATED: This method no longer resumes the timer
    /// Timer stays paused for entire tutorial
    /// </summary>
    public void ResumeTimer()
    {
        // ? CHANGED: Do nothing - timer stays paused
        Debug.Log("[TutorialManager] ?? ResumeTimer called but timer stays PAUSED for tutorial");
        // Timer will only resume after tutorial completes (correct answer)
    }
    
    /// <summary>
    /// Resume timer after tutorial is complete (correct answer shown)
    /// </summary>
    private void ResumeTutorialTimer()
    {
        if (timerComponent != null && timerPausedForTutorial)
        {
          timerComponent.SendMessage("StartTimer");
          timerPausedForTutorial = false;
            Debug.Log("[TutorialManager] ?? Timer RESUMED - tutorial complete!");
        }
    }
    
    /// <summary>
 /// Called every frame to check if player played a card
    /// </summary>
    private void Update()
    {
        if (!isTutorial || !isWaitingForCardPlay) return;
   
        // Check if PlayCardButton has been played using reflection
      if (playCardButtonComponent == null) return;
        
        // Get the PlayedCard holder to see if a card was played
     GameObject playedCardHolder = GameObject.Find("PlayedCard");
        if (playedCardHolder == null) return;
   
      // If there's a card in the PlayedCard holder, the player just played
        if (playedCardHolder.transform.childCount > 0)
    {
            // Card was played - check if it's correct or wrong
        CheckCardAnswer();
        }
    }
    
    /// <summary>
 /// Check if the played card was correct or wrong
    /// </summary>
    private void CheckCardAnswer()
    {
      isWaitingForCardPlay = false; // Stop checking
   
      // Get the PlayedCard
        GameObject playedCardHolder = GameObject.Find("PlayedCard");
        if (playedCardHolder == null || playedCardHolder.transform.childCount == 0)
  {
        Debug.LogWarning("[TutorialManager] No card found in PlayedCard holder!");
            return;
        }
    
      // Get the card from PlayedCard holder
        GameObject playedCard = playedCardHolder.transform.GetChild(0).gameObject;
        
        // Get the expected output to find the correct card number
        GameObject expectedOutput = GameObject.Find("ExpectedOutput");
        if (expectedOutput == null)
        {
  Debug.LogWarning("[TutorialManager] ExpectedOutput not found!");
    return;
     }
        
        // Get expected card number from ExpectedOutput's text (e.g., "Play Card 3")
   TMPro.TextMeshProUGUI expectedText = expectedOutput.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (expectedText == null)
        {
        Debug.LogWarning("[TutorialManager] ExpectedOutput text not found!");
        return;
        }
      
    // Extract number from "Play Card X"
        string expectedString = expectedText.text;
        int expectedCardNumber = -1;
  
        if (expectedString.Contains("Play Card "))
        {
            string numberStr = expectedString.Replace("Play Card ", "").Trim();
      int.TryParse(numberStr, out expectedCardNumber);
      }
  
        // Get played card number from its name (e.g., "Card3(Clone)")
        string playedCardName = playedCard.name.Replace("(Clone)", "").Replace("Card", "");
        int playedCardNumber = -1;
        int.TryParse(playedCardName, out playedCardNumber);
        
    Debug.Log($"[TutorialManager] Expected: {expectedCardNumber}, Played: {playedCardNumber}");
        
        // Check if correct
   bool isCorrect = (playedCardNumber == expectedCardNumber);
 
        if (isCorrect)
 {
Debug.Log("[TutorialManager] ? Correct card played!");
 
  // Show correct panel
   if (correctAnswerPanel != null)
  {
          correctAnswerPanel.SetActive(true);
            }
  
      // ? CHANGED: Resume timer now that tutorial is complete
            ResumeTutorialTimer();
    }
        else
        {
    Debug.Log("[TutorialManager] ? Wrong card played!");
            
         // Show wrong panel
          if (wrongAnswerPanel != null)
 {
            wrongAnswerPanel.SetActive(true);
            }
        
   // ? Timer stays paused - player needs to try again
          Debug.Log("[TutorialManager] Timer remains paused - player should try again");

            // Allow player to try again
     isWaitingForCardPlay = true;
        }
    }
    
    /// <summary>
    /// Close the wrong answer panel
    /// Timer stays paused - player should try again
    /// </summary>
    public void CloseWrongAnswerPanel()
{
        if (wrongAnswerPanel != null)
        {
         wrongAnswerPanel.SetActive(false);
        }
      
        // Timer stays paused - player gets to try again
   Debug.Log("[TutorialManager] Wrong answer panel closed, timer remains paused, waiting for retry");
    }
    
    /// <summary>
    /// Close the correct answer panel
    /// Timer should already be running at this point
 /// </summary>
    public void CloseCorrectAnswerPanel()
    {
if (correctAnswerPanel != null)
  {
            correctAnswerPanel.SetActive(false);
        }
        
Debug.Log("[TutorialManager] Correct answer panel closed, timer should be running");
        // Tutorial complete!
    }
}
