using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the locking/unlocking of chapter buttons based on player level progression.
/// Each button unlocks after completing 10 levels (button 1 = level 10, button 2 = level 20, etc.)
/// </summary>
public class ChapterButtonManager : MonoBehaviour
{
    [Header("Chapter Buttons (in order)")]
    [Tooltip("Assign 6 buttons in order. Button 1 unlocks at level 10, button 2 at level 20, etc.")]
    public Button[] chapterButtons = new Button[6];
    
    [Header("Visual Settings")]
    [Tooltip("Darkening factor for locked buttons (0-1, lower = darker)")]
    [Range(0f, 1f)]
    public float darkenFactor = 0.5f;
    
    [Header("Lock Overlay (Optional)")]
    [Tooltip("Optional: GameObjects to show/hide as lock icons for each button")]
    public GameObject[] lockOverlays = new GameObject[6];
    
    [Header("Level Display (Optional)")]
    [Tooltip("Optional: Text elements to display required level on each button")]
    public TextMeshProUGUI[] requiredLevelTexts = new TextMeshProUGUI[6];
    
    private int currentPlayerLevel = 0;
    private Color[] originalButtonColors;
    private Color[] originalTextColors;
    
    private void Start()
    {
        // Store original colors
        StoreOriginalColors();
        UpdateButtonStates();
    }
    
    private void OnEnable()
    {
        // Store original colors if not already stored
        if (originalButtonColors == null || originalButtonColors.Length == 0)
        {
            StoreOriginalColors();
        }
        UpdateButtonStates();
    }
    
    /// <summary>
    /// Store the original colors of all buttons
    /// </summary>
    private void StoreOriginalColors()
    {
        originalButtonColors = new Color[chapterButtons.Length];
        originalTextColors = new Color[chapterButtons.Length];
        
        for (int i = 0; i < chapterButtons.Length; i++)
        {
            if (chapterButtons[i] == null) continue;
            
            // Store button image color
            Image buttonImage = chapterButtons[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                originalButtonColors[i] = buttonImage.color;
            }
            
            // Store text color
            TextMeshProUGUI tmpText = chapterButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            Text legacyText = chapterButtons[i].GetComponentInChildren<Text>();
            
            if (tmpText != null)
            {
                originalTextColors[i] = tmpText.color;
            }
            else if (legacyText != null)
            {
                originalTextColors[i] = legacyText.color;
            }
        }
        
        Debug.Log($"[ChapterButtonManager] Stored original colors for {chapterButtons.Length} buttons");
    }
    
    /// <summary>
    /// Update all button states based on current player progress
    /// </summary>
    public void UpdateButtonStates()
    {
        // Get current level from PlayerDataManager first, fallback to PlayerPrefs
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.GetCurrentPlayerData() != null)
        {
            currentPlayerLevel = PlayerDataManager.Instance.GetCurrentPlayerData().levels_unlocked;
        }
        else
        {
            currentPlayerLevel = PlayerPrefs.GetInt("levelAt", 6); // Default to 6 (Level 1 unlocked)
        }
        
        Debug.Log($"[ChapterButtonManager] Current player level: {currentPlayerLevel}");
        
        // Update each button (1-6 correspond to levels 10, 20, 30, 40, 50, 60)
        for (int i = 0; i < chapterButtons.Length; i++)
        {
            if (chapterButtons[i] == null) continue;
            
            int requiredLevel = (i + 1) * 10; // Button 0 = level 10, Button 1 = level 20, etc.
            bool isUnlocked = currentPlayerLevel >= requiredLevel;
            
            UpdateButtonVisual(i, isUnlocked, requiredLevel);
        }
    }
    
    /// <summary>
    /// Update visual state of a specific button
    /// </summary>
    private void UpdateButtonVisual(int buttonIndex, bool isUnlocked, int requiredLevel)
    {
        Button button = chapterButtons[buttonIndex];
        
        // Set button interactability
        button.interactable = isUnlocked;
        
        // Update button color - darken if locked
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null && buttonIndex < originalButtonColors.Length)
        {
            if (isUnlocked)
            {
                // Restore original color
                buttonImage.color = originalButtonColors[buttonIndex];
            }
            else
            {
                // Darken the original color
                Color darkened = originalButtonColors[buttonIndex] * darkenFactor;
                darkened.a = originalButtonColors[buttonIndex].a; // Keep original alpha
                buttonImage.color = darkened;
            }
        }
        
        // Update lock overlay visibility
        if (buttonIndex < lockOverlays.Length && lockOverlays[buttonIndex] != null)
        {
            lockOverlays[buttonIndex].SetActive(!isUnlocked);
        }
        
        // Update required level text
        if (buttonIndex < requiredLevelTexts.Length && requiredLevelTexts[buttonIndex] != null)
        {
            if (isUnlocked)
            {
                requiredLevelTexts[buttonIndex].text = $"Chapter {buttonIndex + 1}";
            }
            else
            {
                requiredLevelTexts[buttonIndex].text = $"Locked\nLevel {requiredLevel} Required";
            }
        }
        
        // Update button text - darken if locked
        TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
        Text legacyText = button.GetComponentInChildren<Text>();
        
        if (buttonIndex < originalTextColors.Length)
        {
            if (tmpText != null)
            {
                if (isUnlocked)
                {
                    tmpText.color = originalTextColors[buttonIndex];
                }
                else
                {
                    Color darkened = originalTextColors[buttonIndex] * darkenFactor;
                    darkened.a = originalTextColors[buttonIndex].a;
                    tmpText.color = darkened;
                }
            }
            else if (legacyText != null)
            {
                if (isUnlocked)
                {
                    legacyText.color = originalTextColors[buttonIndex];
                }
                else
                {
                    Color darkened = originalTextColors[buttonIndex] * darkenFactor;
                    darkened.a = originalTextColors[buttonIndex].a;
                    legacyText.color = darkened;
                }
            }
        }
        
        Debug.Log($"[ChapterButtonManager] Button {buttonIndex + 1} - Required Level: {requiredLevel}, Unlocked: {isUnlocked}");
    }
    
    /// <summary>
    /// Call this when player levels up to refresh button states
    /// </summary>
    public void RefreshButtons()
    {
        UpdateButtonStates();
    }
    
    /// <summary>
    /// Check if a specific chapter is unlocked
    /// </summary>
    public bool IsChapterUnlocked(int chapterIndex)
    {
        int requiredLevel = (chapterIndex + 1) * 10;
        return currentPlayerLevel >= requiredLevel;
    }
}
