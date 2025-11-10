using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelection : MonoBehaviour
{
    public Button[] lvlButtons;
    public Button playButton;
    
    [Header("Username Display")]
    public TextMeshProUGUI usernameText;
    
    [Header("Character Selection")]
    [Tooltip("Enable character selection before starting level")]
    public bool enableCharacterSelection = true;
    [Tooltip("Reference to Character Selection Manager")]
    public CharacterSelectionManager characterSelectionManager;

    private int selectedLevel = -1;

    private void Start()
    {
        // Auto-find CharacterSelectionManager if not assigned
        if (characterSelectionManager == null)
        {
            characterSelectionManager = FindObjectOfType<CharacterSelectionManager>();
        }
   
        // Display username
        if (usernameText != null)
        {
            string username = PlayerPrefs.GetString("username", "Player");
            usernameText.text = "Welcome, " + username + "!";
        }
        
        // Tutorial level is at index 5, Level 1 is at index 6
        int levelAt = PlayerPrefs.GetInt("levelAt", 6);

        for (int i = 0; i < lvlButtons.Length; i++)
        {
            // Level button 0 = scene index 6 (Level 1)
            if (i + 6 > levelAt)
                lvlButtons[i].interactable = false;
            lvlButtons[i].onClick.AddListener(() => SelectLevel(System.Array.IndexOf(lvlButtons, UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<Button>())));
        }
        playButton.onClick.AddListener(PlaySelectedLevel);

        playButton.interactable = false;
    }
    
    void SelectLevel(int levelAt)
    {
        selectedLevel = levelAt;

        foreach (var btn in lvlButtons)
            btn.GetComponent<Image>().color = Color.white;
            lvlButtons[levelAt].GetComponent<Image>().color = Color.green;
            playButton.interactable = true;
    }
    
    void PlaySelectedLevel()
    {
        if (selectedLevel >= 0)
        {
            // Level button 0 = scene index 6 (Level 1)
            int sceneIndex = selectedLevel + 6;
       
            // Use character selection if enabled
            if (enableCharacterSelection && characterSelectionManager != null)
            {
                characterSelectionManager.PrepareToStartLevel(sceneIndex);
            }
            else
            {
                // Direct load without character selection
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
            }
        }
    }
}
