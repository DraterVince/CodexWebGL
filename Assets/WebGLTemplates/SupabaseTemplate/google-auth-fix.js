/**
 * Google OAuth Popup Fix for WebGL Unity Games
 * 
 * This script fixes the issue where Google login causes a full page redirect,
 * which reloads the entire Unity WebGL game.
 * 
 * Solution: Open Google OAuth in a popup window instead of redirecting the main page
 */

// Store the original SupabaseGoogleSignIn function
window.OriginalSupabaseGoogleSignIn = window.SupabaseGoogleSignIn;

// Override with popup version
window.SupabaseGoogleSignIn = async function() {
    try {
        console.log('[Google Auth] Starting Google Sign-In with POPUP...');
        
        if (!window.supabase) {
            console.error('[Google Auth] Supabase client not initialized!');
         SendMessageToUnity('AuthManager', 'OnGoogleSignInError', 'Supabase not initialized');
       return;
        }

        // Get the OAuth URL from Supabase
        const redirectUrl = window.location.origin + window.location.pathname;
        console.log('[Google Auth] Redirect URL:', redirectUrl);

        const { data, error } = await window.supabase.auth.signInWithOAuth({
       provider: 'google',
 options: {
           redirectTo: redirectUrl,
        skipBrowserRedirect: true // KEY FIX: Prevents full page redirect
   }
 });

        if (error) {
         console.error('[Google Auth] Error getting OAuth URL:', error);
         SendMessageToUnity('AuthManager', 'OnGoogleSignInError', error.message);
 return;
    }

        if (!data || !data.url) {
     console.error('[Google Auth] No OAuth URL returned');
            SendMessageToUnity('AuthManager', 'OnGoogleSignInError', 'No OAuth URL');
            return;
      }

        console.log('[Google Auth] Opening popup window...');
        
        // Open popup window for OAuth
        const width = 600;
      const height = 700;
        const left = (screen.width / 2) - (width / 2);
        const top = (screen.height / 2) - (height / 2);
        
        const popup = window.open(
       data.url,
            'Google Sign-In',
   `width=${width},height=${height},left=${left},top=${top},toolbar=no,menubar=no,location=no,status=no`
        );

   if (!popup) {
    console.error('[Google Auth] Popup blocked!');
      SendMessageToUnity('AuthManager', 'OnGoogleSignInError', 'Popup blocked - please allow popups for this site');
   return;
        }

  console.log('[Google Auth] Popup opened successfully');
        
        // Monitor popup closure
        const popupCheckInterval = setInterval(() => {
   if (popup.closed) {
   clearInterval(popupCheckInterval);
       console.log('[Google Auth] Popup closed by user');
        
     // Check if auth succeeded after popup closed
     setTimeout(async () => {
        const { data: { session } } = await window.supabase.auth.getSession();
              if (!session) {
         console.log('[Google Auth] No session found after popup closed');
         SendMessageToUnity('AuthManager', 'OnGoogleSignInError', 'Sign-in cancelled');
          }
        }, 500);
  }
        }, 500);

    } catch (error) {
        console.error('[Google Auth] Exception:', error);
        SendMessageToUnity('AuthManager', 'OnGoogleSignInError', error.message);
    }
};

// Enhanced auth state listener to detect Google OAuth completion
window.setupGoogleAuthListener = function() {
    if (!window.supabase) {
        console.error('[Google Auth] Cannot setup listener - Supabase not initialized');
        return;
    }

    console.log('[Google Auth] Setting up auth state change listener...');

    window.supabase.auth.onAuthStateChange(async (event, session) => {
        console.log('[Google Auth] Auth state changed:', event);
  
        if (event === 'SIGNED_IN' && session) {
 console.log('[Google Auth] User signed in via Google!');
    console.log('[Google Auth] User ID:', session.user.id);
            console.log('[Google Auth] Email:', session.user.email);
       console.log('[Google Auth] Provider:', session.user.app_metadata.provider);

            // Check if this is a Google sign-in
            if (session.user.app_metadata.provider === 'google') {
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

   // Load or create player data
     try {
    const { data: playerData, error: playerError } = await window.supabase
      .from('player_data')
     .select('*')
        .eq('user_id', session.user.id)
           .single();

      if (playerError && playerError.code !== 'PGRST116') {
                  console.error('[Google Auth] Error checking player data:', playerError);
       }

              if (!playerData) {
        console.log('[Google Auth] No player data found - Unity will create it');
   } else {
     console.log('[Google Auth] Existing player data found');
    }
     } catch (playerLoadError) {
        console.error('[Google Auth] Error loading player data:', playerLoadError);
     }

        console.log('[Google Auth] Waiting for Unity before sending success...');
        await waitForUnityInstance();
  
     SendMessageToUnity('AuthManager', 'OnGoogleSignInSuccess', JSON.stringify(authResponse));
     console.log('[Google Auth] Google Sign-In SUCCESS sent to Unity!');
   }
        }
    });
};

console.log('[Google Auth Fix] Popup-based Google authentication loaded!');
