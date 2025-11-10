using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevel : MonoBehaviour
{
  public int nextLevel;
    
    [Header("Save Settings")]
    [Tooltip("Auto-save game progress before loading next level")]
    public bool autoSaveBeforeNext = true;
    
    [Header("WebGL Save Settings")]
    [Tooltip("Wait time for IndexedDB to flush in WebGL (seconds)")]
 public float webglSaveDelay = 3.0f;

 private bool isTransitioning = false;

    private void Start()
    {
        nextLevel = SceneManager.GetActiveScene().buildIndex + 1;
 }

    public void ProgressLevel()
    {
  if (isTransitioning)
        {
            Debug.LogWarning("[NextLevel] Already transitioning, ignoring duplicate call");
            return;
     }
        
        isTransitioning = true;
      StartCoroutine(SaveAndLoadSceneCoroutine(nextLevel));
    }
    
 private IEnumerator SaveAndLoadSceneCoroutine(int sceneIndex)
  {
        // Auto-save before progressing
        if (autoSaveBeforeNext && NewAndLoadGameManager.Instance != null)
        {
            Debug.Log("[NextLevel] Auto-saving before progressing to next level...");
   NewAndLoadGameManager.Instance.AutoSave();
   
#if UNITY_WEBGL && !UNITY_EDITOR
          // WebGL: Wait for save to complete + extra buffer for IndexedDB
         Debug.Log("[NextLevel] WebGL: Waiting for save operation to complete...");
            yield return StartCoroutine(NewAndLoadGameManager.Instance.WaitForSaveCompletion());
            
 Debug.Log($"[NextLevel] WebGL: Adding {webglSaveDelay}s buffer for IndexedDB flush...");
        yield return new WaitForSecondsRealtime(webglSaveDelay);
     Debug.Log("[NextLevel] WebGL: Save should be fully persisted");
#else
            // Desktop: Small delay to ensure save completes
            yield return new WaitForSecondsRealtime(0.1f);
#endif
            
  Debug.Log("[NextLevel] Game progress saved!");
        }
      
        Debug.Log($"[NextLevel] Loading scene {sceneIndex}...");
     SceneManager.LoadScene(sceneIndex);
    }
}
