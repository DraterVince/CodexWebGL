using UnityEngine;
using UnityEngine.UI;

public class AnimatedPanel : MonoBehaviour
{
    public RectTransform panelRect;
    public Vector2 miniSize = new Vector2(640, 360);
    public Vector2 fullSize = new Vector2(1920, 1080);
    public Vector2 miniPos = new Vector2(684, 388);
    public Vector2 fullPos = new Vector2(0, 0);
    public float animationDuration = 0.3f;

    public bool isExpanded = true; // Start expanded (public for sync)
    private float animTime = 0f;
    private Vector2 startSize, startPos, targetSize, targetPos;
    private bool hasStartedTimer = false; // Track if timer has been started
    private bool isMultiplayerMode = false;

    void Start()
    {
        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        // Start large and centered
        panelRect.sizeDelta = fullSize;
        panelRect.anchoredPosition = fullPos;
        isExpanded = true;
        animTime = animationDuration; // No animation on start

        // Detect multiplayer mode
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
                        Debug.Log("[AnimatedPanel] Multiplayer mode detected - timer will sync");
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[AnimatedPanel] Could not detect Photon: {ex.Message}");
            isMultiplayerMode = false;
        }
    }

    public void OnPanelClicked()
    {
        isExpanded = !isExpanded;
        animTime = 0f;
        startSize = panelRect.sizeDelta;
        startPos = panelRect.anchoredPosition;
        targetSize = isExpanded ? fullSize : miniSize;
        targetPos = isExpanded ? fullPos : miniPos;

        // In multiplayer: Sync panel state across all players
        if (isMultiplayerMode)
        {
            SyncPanelState();
        }

        // When minimizing panel for the first time, start timer
        if (!isExpanded && !hasStartedTimer)
        {
            hasStartedTimer = true;

            if (isMultiplayerMode)
            {
                // In multiplayer: Sync timer across all players
                StartSyncedTimer();
            }
            else
            {
                // In single player: Start timer locally
                StartLocalTimer();
            }
        }
    }
    
    /// <summary>
    /// Sync panel state with SharedMultiplayerGameManager
    /// </summary>
    private void SyncPanelState()
    {
        try
        {
            var manager = FindObjectOfType<SharedMultiplayerGameManager>();
            if (manager != null)
            {
                manager.SyncExpectedOutputPanel(isExpanded);
                Debug.Log($"[AnimatedPanel] Synced panel state: {(isExpanded ? "Expanded" : "Minimized")}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[AnimatedPanel] Could not sync panel state: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Force panel to a specific state (called by RPC)
    /// </summary>
    public void SetPanelState(bool expanded)
    {
        if (isExpanded != expanded)
        {
            // Only toggle if state is different
            isExpanded = expanded;
            animTime = 0f;
            startSize = panelRect.sizeDelta;
            startPos = panelRect.anchoredPosition;
            targetSize = isExpanded ? fullSize : miniSize;
            targetPos = isExpanded ? fullPos : miniPos;
        }
    }

    private void StartSyncedTimer()
    {
        try
        {
            // Method 1: Direct find
            var manager = GameObject.FindObjectOfType<SharedMultiplayerGameManager>();
            if (manager != null)
            {
                manager.StartTimerForAllPlayers();
                Debug.Log("[AnimatedPanel] Started synced timer via direct reference");
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
                        Debug.Log("[AnimatedPanel] Started synced timer via reflection");
                        return;
                    }
                }
            }

            Debug.LogWarning("[AnimatedPanel] Could not start synced timer - SharedMultiplayerGameManager not found");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AnimatedPanel] Error starting synced timer: {ex.Message}");
        }
    }

    private void StartLocalTimer()
    {
        // Find Timer component and start it
        Timer timer = FindObjectOfType<Timer>();
        if (timer != null)
        {
            timer.StartTimer();
            Debug.Log("[AnimatedPanel] Started local timer");
        }
        else
        {
            Debug.LogWarning("[AnimatedPanel] Timer component not found in scene!");
        }
    }

    void Update()
    {
        if (animTime < animationDuration)
        {
            animTime += Time.deltaTime;
            float t = Mathf.Clamp01(animTime / animationDuration);
            panelRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
        }

        float scale = panelRect.sizeDelta.y / fullSize.y;
        foreach (RectTransform child in panelRect)
        {
            child.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
