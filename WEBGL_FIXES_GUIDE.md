# WebGL Build Fixes Guide

## Issues Fixed:
1. ? "Made with Unity" watermark at bottom of game
2. ? "Development Build" text at bottom-right
3. ? Development console appearing in-game
4. ? Google login causing full page reload

---

## Quick Fix Instructions

### Step 1: Remove Development Build Elements
**In Unity Editor:**
1. Go to menu: **Tools ? Configure Production Build Settings**
2. This automatically disables:
   - Development Build text
   - Development console
- Profiler
   - Splash screen (if you have Unity Pro)

**OR Manual Method:**
1. **File ? Build Settings**
2. ? **Uncheck "Development Build"**
3. ? Uncheck "Autoconnect Profiler"

---

### Step 2: Hide Unity Watermark (Bottom Footer)
**In Unity Editor:**
1. Go to menu: **Tools ? Remove 'Made with Unity' Watermark (WebGL CSS Fix)**
2. This creates CSS that hides the bottom footer

This hides the "Made with Unity" text and logo at the bottom of your WebGL game.

**Note:** The splash screen on game load requires Unity Pro/Plus to fully remove.

---

### Step 3: Fix Google Login Popup Issue
**In Unity Editor:**
1. Go to menu: **Tools ? Fix WebGL Template for Google OAuth Popup**
2. Go to menu: **Tools ? Configure WebGL Template with Google Auth Fix**

This fixes the issue where Google login reloads your entire game by:
- Opening Google OAuth in a popup window instead of redirecting
- Keeping your Unity game running in the background
- Properly handling the auth callback

---

### Step 4: Rebuild Your WebGL Game
1. **File ? Build Settings**
2. Select **WebGL** platform
3. Click **Build** or **Build and Run**

---

## What Each Setting Does

### Development Build (DISABLED)
- ? Removes "Development Build" watermark text
- ? Disables in-game console (press ~ key)
- ? Disables debug logging to console
- ? Improves performance
- ? Smaller build size

### CSS Watermark Fix
- ? Hides "Made with Unity" logo at bottom
- ? Hides WebGL footer bar
- ? Game takes up more screen space

### Google OAuth Popup Fix
- ? Opens Google login in popup window
- ? Game stays loaded in background
- ? No page reload = no data loss
- ? Better user experience

---

## Checking Your Settings

**In Unity Editor:**
- Menu: **Tools ? Show Current Build Settings**

This displays:
- Development Build status
- Profiler status
- Splash screen configuration
- WebGL template being used

---

## Troubleshooting

### "Made with Unity" still shows on game startup
- This is the **splash screen** (not the bottom watermark)
- Requires **Unity Pro** or **Unity Plus** subscription to remove
- You can minimize it in: **Edit ? Project Settings ? Player ? Splash Image**

### Google login still redirects instead of popup
- Make sure you ran: **Tools ? Fix WebGL Template for Google OAuth Popup**
- Rebuild your WebGL game after applying the fix
- Check browser console for errors

### Development Build text still appears
- Make sure **Development Build** is unchecked in Build Settings
- Rebuild your game - settings don't apply to existing builds

### Bottom footer/watermark still shows
- Make sure you ran: **Tools ? Remove 'Made with Unity' Watermark**
- Check that `TemplateData/style.css` exists in your template folder
- Rebuild your WebGL game

### Popup blocked message
- Users need to allow popups for your game's website
- Add instructions to your game: "Please allow popups for Google sign-in"

---

## Files Modified

1. **Assets/Editor/BuildSettingsHelper.cs**
   - Menu tools for configuring build settings
   - One-click production build configuration

2. **Assets/WebGLTemplates/SupabaseTemplate/google-auth-fix.js**
   - Popup-based Google OAuth implementation
   - Prevents full page redirects

3. **Assets/WebGLTemplates/SupabaseTemplate/TemplateData/style.css**
   - CSS to hide Unity watermark and footer

---

## For Itch.io Deployment

After building, make sure to:
1. ? Development Build: **DISABLED**
2. ? WebGL Template: **SupabaseTemplate**
3. ? Upload entire Build folder to itch.io
4. ? Enable "This file will be played in the browser" for index.html
5. ? Set viewport dimensions (e.g., 960x600)

---

## Unity Pro Alternative

If you upgrade to Unity Pro/Plus:
- Full splash screen removal
- Custom splash screens
- No watermarks at all
- Professional branding

**Free Version Limitations:**
- Unity splash screen on startup (can't be removed)
- "Made with Unity" branding (can be hidden with CSS)

---

## Questions?

Run any of these Unity menu commands:
- **Tools ? Show Current Build Settings** - Check your configuration
- **Tools ? Configure Production Build Settings** - Quick setup
- **Tools ? Fix WebGL Template for Google OAuth Popup** - Fix Google login

**All fixes are now one-click away!**
