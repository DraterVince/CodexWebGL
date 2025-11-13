using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

/// <summary>
/// UI Panel for displaying multiplayer leaderboard in multiplayer lobby
/// </summary>
public class LeaderboardPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform leaderboardContainer;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Header("Leaderboard Entry UI (if using prefab)")]
    [SerializeField] private int rankTextIndex = 0; // Index of rank text in prefab
    [SerializeField] private int nameTextIndex = 1; // Index of name text in prefab
    [SerializeField] private int scoreTextIndex = 2; // Index of score text in prefab
    
    private MultiplayerLeaderboardManager leaderboardManager;
    
    private void Start()
    {
        // Find leaderboard manager (it persists across scenes with DontDestroyOnLoad)
        leaderboardManager = FindObjectOfType<MultiplayerLeaderboardManager>();
        
        // If not found, try to find it in the scene or create a reference
        if (leaderboardManager == null)
        {
            // Try to find it by name or tag
            GameObject managerObj = GameObject.Find("MultiplayerLeaderboardManager");
            if (managerObj != null)
            {
                leaderboardManager = managerObj.GetComponent<MultiplayerLeaderboardManager>();
            }
        }
        
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseLeaderboard);
        }
        
        // Hide panel initially
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // Refresh leaderboard manager reference when panel is enabled
        if (leaderboardManager == null)
        {
            leaderboardManager = FindObjectOfType<MultiplayerLeaderboardManager>();
        }
    }
    
    /// <summary>
    /// Open and display the leaderboard
    /// </summary>
    public void OpenLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
            RefreshLeaderboard();
        }
    }
    
    /// <summary>
    /// Close the leaderboard panel
    /// </summary>
    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Refresh the leaderboard display
    /// Shows top 10 best single-run scores from all players
    /// </summary>
    public void RefreshLeaderboard()
    {
        if (leaderboardContainer == null) return;
        
        // Clear existing entries
        foreach (Transform child in leaderboardContainer)
        {
            Destroy(child.gameObject);
        }
        
        if (titleText != null)
        {
            titleText.text = "Multiplayer Leaderboard\n(Top 10 Best Single-Run Scores)";
        }
        
        // Get leaderboard from manager (shows top 10 best scores)
        if (leaderboardManager != null)
        {
            DisplayTopScoresLeaderboard();
        }
        else
        {
            // Leaderboard manager not found
            GameObject messageObj = new GameObject("NoManagerMessage");
            messageObj.transform.SetParent(leaderboardContainer);
            TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
            messageText.text = "Leaderboard data not available.";
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.fontSize = 24;
        }
    }
    
    /// <summary>
    /// Display top 10 best single-run scores from all players
    /// </summary>
    private void DisplayTopScoresLeaderboard()
    {
        if (leaderboardManager == null || leaderboardContainer == null) return;
        
        // Get top 10 best scores (from Supabase)
        var leaderboard = leaderboardManager.GetLeaderboardData();
        
        if (leaderboard.Count == 0)
        {
            // No data yet
            GameObject messageObj = new GameObject("NoDataMessage");
            messageObj.transform.SetParent(leaderboardContainer);
            TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
            messageText.text = "No leaderboard entries yet.\nPlay multiplayer and beat levels to appear on the leaderboard!";
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.fontSize = 20;
            return;
        }
        
        // Already sorted by GetLeaderboardData(), just display
        int rank = 1;
        foreach (var entry in leaderboard)
        {
            if (leaderboardEntryPrefab != null)
            {
                GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
                
                // Try to find text components
                TextMeshProUGUI[] texts = entryObj.GetComponentsInChildren<TextMeshProUGUI>();
                
                if (texts.Length > rankTextIndex && texts[rankTextIndex] != null)
                {
                    texts[rankTextIndex].text = $"#{rank}";
                }
                
                if (texts.Length > nameTextIndex && texts[nameTextIndex] != null)
                {
                    texts[nameTextIndex].text = entry.Key;
                }
                
                if (texts.Length > scoreTextIndex && texts[scoreTextIndex] != null)
                {
                    texts[scoreTextIndex].text = $"{entry.Value} levels";
                }
            }
            else
            {
                // Fallback: create simple text entry
                GameObject entryObj = new GameObject($"LeaderboardEntry_{rank}");
                entryObj.transform.SetParent(leaderboardContainer);
                
                RectTransform rect = entryObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(400, 50);
                
                TextMeshProUGUI text = entryObj.AddComponent<TextMeshProUGUI>();
                text.text = $"#{rank}. {entry.Key}: {entry.Value} levels";
                text.fontSize = 20;
                text.alignment = TextAlignmentOptions.Left;
            }
            
            rank++;
        }
    }
    
    /// <summary>
    /// Toggle leaderboard panel visibility
    /// </summary>
    public void ToggleLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            if (leaderboardPanel.activeSelf)
            {
                CloseLeaderboard();
            }
            else
            {
                OpenLeaderboard();
            }
        }
    }
}

