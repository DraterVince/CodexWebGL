using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Shows a tutorial prompt dialog when a new save is created
/// Attach this to a Canvas in the SaveLoadMenu scene
/// </summary>
public class TutorialPromptDialog : MonoBehaviour
{
    public static TutorialPromptDialog Instance;
    
    [Header("UI References")]
    [Tooltip("The tutorial prompt panel")]
    public GameObject tutorialPromptPanel;
    
    [Tooltip("Yes button - plays tutorial")]
    public Button yesButton;
    
    [Tooltip("No button - skips to level select")]
    public Button noButton;
    
    [Header("Optional Text")]
    [Tooltip("Message text (optional)")]
    public TextMeshProUGUI messageText;
    
    private const string TUTORIAL_SCENE = "Tutorial"; // Scene name or use index 5
    private const string LEVEL_SELECT_SCENE = "LevelSelect";
    
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
    
        // Hide panel initially
   if (tutorialPromptPanel != null)
        {
            tutorialPromptPanel.SetActive(false);
        }
    
   // Setup button listeners
    if (yesButton != null)
  {
            yesButton.onClick.AddListener(OnYesClicked);
        }
      
  if (noButton != null)
   {
      noButton.onClick.AddListener(OnNoClicked);
        }
    }
    
  /// <summary>
    /// Show the tutorial prompt dialog
    /// Call this when a new save is created
    /// </summary>
    public void ShowTutorialPrompt()
    {
  Debug.Log("[TutorialPromptDialog] Showing tutorial prompt");
        
if (tutorialPromptPanel != null)
        {
     tutorialPromptPanel.SetActive(true);
      
 // Update message if text component exists
            if (messageText != null)
     {
  messageText.text = "Would you like to play the tutorial?\n\n" +
      "The tutorial will teach you the basics of the game.";
   }
    }
        else
        {
            Debug.LogWarning("[TutorialPromptDialog] Tutorial prompt panel is not assigned!");
        }
    }
    
    /// <summary>
    /// Hide the tutorial prompt without loading any scene
    /// </summary>
  public void HidePrompt()
    {
     if (tutorialPromptPanel != null)
        {
  tutorialPromptPanel.SetActive(false);
     }
    }
    
    /// <summary>
    /// User clicked "Yes" - Load tutorial level
    /// </summary>
    private void OnYesClicked()
    {
      Debug.Log("[TutorialPromptDialog] User chose to play tutorial");
     
        HidePrompt();
      
    // Mark that tutorial has been seen
   PlayerPrefs.SetInt("HasSeenTutorial", 1);
        PlayerPrefs.Save();

  // Load tutorial level (scene index 5)
        // The lore dialogue will play automatically when the scene loads
 Debug.Log("[TutorialPromptDialog] Loading tutorial level with lore intro...");
        SceneManager.LoadScene(5); // Tutorial scene index
    }
    
    /// <summary>
    /// User clicked "No" - Skip to level select
 /// </summary>
  private void OnNoClicked()
    {
   Debug.Log("[TutorialPromptDialog] User chose to skip tutorial");
        
      HidePrompt();
        
        // Mark that tutorial has been seen (even if skipped)
        PlayerPrefs.SetInt("HasSeenTutorial", 1);
        PlayerPrefs.Save();
        
      // Load level select
     Debug.Log("[TutorialPromptDialog] Loading level select...");
  SceneManager.LoadScene(LEVEL_SELECT_SCENE);
 }
    
    /// <summary>
    /// Check if player has already seen tutorial prompt
    /// </summary>
    public static bool HasSeenTutorial()
    {
        return PlayerPrefs.GetInt("HasSeenTutorial", 0) == 1;
    }
    
    /// <summary>
    /// Reset tutorial flag (for testing)
    /// </summary>
    public static void ResetTutorialFlag()
    {
        PlayerPrefs.DeleteKey("HasSeenTutorial");
PlayerPrefs.Save();
   Debug.Log("[TutorialPromptDialog] Tutorial flag reset");
    }
}
