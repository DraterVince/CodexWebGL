/**
 * Google OAuth Popup Fix for WebGL Unity Games - OPTIMIZED FOR SPEED
 * 
 * This script fixes the issue where Google login causes a full page redirect,
 * which reloads the entire Unity WebGL game.
 * 
 * Solution: Open Google OAuth in a popup window and quickly detect completion
 */

console.log('[Google Auth Fix] Loading FAST popup-based Google authentication...');

// CRITICAL: Detect if we're running inside the OAuth popup
if (window.opener && window.opener !== window) {
    console.log('[Google Auth Fix] Running inside popup - checking for auth tokens...');
    
    // Check if URL contains OAuth tokens (from Google redirect)
    const urlParams = new URLSearchParams(window.location.search);
    const hashParams = new URLSearchParams(window.location.hash.substring(1));
    
    const hasCode = urlParams.has('code');
    const hasAccessToken = hashParams.has('access_token');
    
    if (hasCode || hasAccessToken) {
        console.log('[Google Auth Fix] ✅ OAuth tokens detected in popup! Closing...');
        
        // Give Supabase a moment to process the tokens, then close
        setTimeout(() => {
            console.log('[Google Auth Fix] Closing popup now');
            window.close();
        }, 1000);
        
        // Stop loading the rest of the page
        throw new Error('OAuth popup - stopping page load');
    }
}

// Store the original function if it exists
if (typeof window.SupabaseGoogleSignIn !== 'undefined') {
    window.OriginalSupabaseGoogleSignIn = window.SupabaseGoogleSignIn;
}

/**
 * Override SupabaseGoogleSignIn with FAST popup version
 */
window.SupabaseGoogleSignIn = async function() {
    try {
      console.log('[Google Auth] ===== STARTING FAST GOOGLE SIGN-IN =====');
        
  if (!window.supabase) {
         console.error('[Google Auth] Supabase client not initialized!');
SendMessageToUnity('AuthManager', 'OnGoogleSignInError', 'Supabase not initialized');
      return;
 }

        // Get current page URL for redirect
        const redirectUrl = window.location.href;
    console.log('[Google Auth] Redirect URL:', redirectUrl);

        // Request OAuth URL from Supabase WITHOUT redirecting
   console.log('[Google Auth] Requesting OAuth URL...');
        const { data, error } = await window.supabase.auth.signInWithOAuth({
            provider: 'google',
            options: {
       redirectTo: redirectUrl,
                skipBrowserRedirect: true // KEY: Don't redirect main window!
  }
        });

        if (error) {
    console.error('[Google Auth] Error:', error);
   SendMessageToUnity('AuthManager', 'OnGoogleSignInError', error.message);
        return;
        }

        if (!data || !data.url) {
      console.error('[Google Auth] No OAuth URL returned');
    SendMessageToUnity('AuthManager', 'OnGoogleSignInError', 'No OAuth URL');
  return;
        }

        console.log('[Google Auth] 🚀 Opening popup...');
 
        // Calculate popup window position (centered)
        const width = 500;
        const height = 600;
        const left = (screen.width / 2) - (width / 2);
const top = (screen.height / 2) - (height / 2);
        
        // Open OAuth in popup window
      const popup = window.open(
   data.url,
        'GoogleSignIn',
            `width=${width},height=${height},left=${left},top=${top},toolbar=no,menubar=no,scrollbars=yes`
        );

      if (!popup || popup.closed || typeof popup.closed === 'undefined') {
            console.error('[Google Auth] Popup blocked!');
          SendMessageToUnity('AuthManager', 'OnGoogleSignInError', 'Popup blocked - please allow popups');
            return;
        }

        console.log('[Google Auth] ? Popup opened - waiting for auth...');
        
        // FAST polling for auth success
        let authSuccessful = false;
  let checkCount = 0;
        const maxChecks = 60; // 30 seconds max (check every 500ms)
      
   const authCheckInterval = setInterval(async () => {
  checkCount++;
            
            // Check if popup is still open
  if (popup.closed) {
                clearInterval(authCheckInterval);
    
          if (!authSuccessful) {
          console.log('[Google Auth] Popup closed - checking for session...');
     
   // Quick final check
  setTimeout(async () => {
             const { data: { session } } = await window.supabase.auth.getSession();
             if (session) {
           await handleGoogleAuthSuccess(session);
                 } else {
                SendMessageToUnity('AuthManager', 'OnGoogleSignInError', 'Sign-in cancelled');
             }
        }, 500);
 }
       return;
        }
   
        // Check for auth success
     try {
           const { data: { session }, error: sessionError } = await window.supabase.auth.getSession();
    
     if (session && session.user) {
          console.log('[Google Auth] ??? AUTH SUCCESS! ???');
             console.log('[Google Auth] User:', session.user.email);
        
   authSuccessful = true;
  clearInterval(authCheckInterval);
              
    // Close popup
        try {
            if (popup && !popup.closed) {
   popup.close();
          }
 } catch (e) {
         // Ignore close errors
   }
           
      // Process auth immediately
         await handleGoogleAuthSuccess(session);
            return;
        }
            } catch (e) {
  // Ignore check errors, keep trying
        }
            
 // Timeout
          if (checkCount >= maxChecks) {
    clearInterval(authCheckInterval);
     if (!authSuccessful) {
           console.error('[Google Auth] Timeout waiting for auth');
     try { popup.close(); } catch (e) {}
       SendMessageToUnity('AuthManager', 'OnGoogleSignInError', 'Sign-in timeout');
         }
            }
      }, 500); // Check every 500ms for FAST response

    } catch (error) {
    console.error('[Google Auth] Exception:', error);
        SendMessageToUnity('AuthManager', 'OnGoogleSignInError', error.message || 'Unknown error');
    }
};

/**
 * Handle successful Google authentication - FAST VERSION
 */
async function handleGoogleAuthSuccess(session) {
    console.log('[Google Auth] Processing auth SUCCESS...');

    const authResponse = {
        user: {
            id: session.user.id,
            email: session.user.email
        },
        session: {
     access_token: session.access_token,
            refresh_token: session.refresh_token
   }
    };

    try {
     // Quick check for player data (don't wait long)
     console.log('[Google Auth] Checking player data...');
    
        const playerDataPromise = window.supabase
    .from('player_data')
            .select('*')
         .eq('user_id', session.user.id)
     .single();
  
 // Race: either data loads in 2 seconds, or we timeout
      const timeoutPromise = new Promise(resolve => setTimeout(() => resolve(null), 2000));
 
        const result = await Promise.race([playerDataPromise, timeoutPromise]);
        
 if (result && result.data) {
          console.log('[Google Auth] ? Player data found:', result.data.username);
   // Send to Unity quickly
   await waitForUnityInstance(3000); // Max 3 second wait
            SendMessageToUnity('PlayerDataManager', 'OnPlayerDataLoaded', JSON.stringify(result.data));
        } else {
        console.log('[Google Auth] No player data or timeout - Unity will create it');
     }
    } catch (error) {
      console.log('[Google Auth] Player data check error (will be created in Unity):', error.message);
    }

    // Send auth success to Unity immediately
    console.log('[Google Auth] Sending auth success to Unity NOW...');
    await waitForUnityInstance(3000); // Max 3 second wait
    SendMessageToUnity('AuthManager', 'OnGoogleSignInSuccess', JSON.stringify(authResponse));
    console.log('[Google Auth] ??? COMPLETE - proceeding to next scene! ???');
}

/**
 * Helper: Wait for Unity instance with timeout
 */
async function waitForUnityInstance(maxWaitMs = 5000) {
    const startTime = Date.now();
    
    while (!window.unityInstance && (Date.now() - startTime) < maxWaitMs) {
  await new Promise(resolve => setTimeout(resolve, 50));
  }
 
  if (!window.unityInstance) {
console.warn('[Google Auth] Unity instance not ready after timeout');
    } else {
        console.log('[Google Auth] ? Unity ready');
    }
}

/**
 * Helper: Send message to Unity
 */
function SendMessageToUnity(gameObject, method, message) {
    try {
        if (typeof unityInstance !== 'undefined' && unityInstance !== null) {
      unityInstance.SendMessage(gameObject, method, message || '');
    console.log(`[Google Auth] ? Sent: ${gameObject}.${method}`);
        } else {
    console.error('[Google Auth] Unity instance not available!');
     }
    } catch (e) {
        console.error('[Google Auth] Error sending to Unity:', e);
    }
}

console.log('[Google Auth Fix] ? FAST popup authentication ready!');
