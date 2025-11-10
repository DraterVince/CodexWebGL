#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Helper script to configure build settings for production builds
/// Removes development build text, console, and configures splash screen
/// </summary>
public class BuildSettingsHelper : EditorWindow
{
[MenuItem("Tools/Configure Production Build Settings")]
    public static void ConfigureProductionSettings()
    {
        // Disable development build (removes "Development Build" text and console)
        EditorUserBuildSettings.development = false;
      
        // Disable autoconnect profiler
    EditorUserBuildSettings.connectProfiler = false;
        
     // Configure Player Settings for WebGL
        PlayerSettings.SplashScreen.show = false; // Requires Unity Pro/Plus
      PlayerSettings.SplashScreen.showUnityLogo = false; // Requires Unity Pro/Plus
        
        // Alternative: Minimize splash screen if you don't have Pro
        // PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Static;
        
        Debug.Log("? Production build settings applied:");
        Debug.Log("  - Development Build: DISABLED");
        Debug.Log("  - Development Console: DISABLED");
        Debug.Log("  - Profiler Auto-connect: DISABLED");
        Debug.Log("  - Splash Screen: DISABLED (requires Unity Pro/Plus)");
        Debug.Log("\nNote: If you don't have Unity Pro/Plus, the splash screen will still show.");
      Debug.Log("Consider using File ? Build Settings ? Player Settings to adjust splash screen manually.");
    }
    
    [MenuItem("Tools/Configure Development Build Settings")]
    public static void ConfigureDevelopmentSettings()
    {
   // Enable development build
     EditorUserBuildSettings.development = true;
        
        // Enable autoconnect profiler
 EditorUserBuildSettings.connectProfiler = true;
        
        Debug.Log("? Development build settings applied:");
        Debug.Log("  - Development Build: ENABLED");
   Debug.Log("  - Development Console: ENABLED");
        Debug.Log("  - Profiler Auto-connect: ENABLED");
    }
    
    [MenuItem("Tools/Show Current Build Settings")]
    public static void ShowCurrentSettings()
    {
        Debug.Log("=== CURRENT BUILD SETTINGS ===");
    Debug.Log($"Development Build: {(EditorUserBuildSettings.development ? "ENABLED" : "DISABLED")}");
        Debug.Log($"Autoconnect Profiler: {(EditorUserBuildSettings.connectProfiler ? "ENABLED" : "DISABLED")}");
        Debug.Log($"Splash Screen Show: {PlayerSettings.SplashScreen.show}");
   Debug.Log($"Show Unity Logo: {PlayerSettings.SplashScreen.showUnityLogo}");
        Debug.Log($"Active Build Target: {EditorUserBuildSettings.activeBuildTarget}");
    }
}
#endif
