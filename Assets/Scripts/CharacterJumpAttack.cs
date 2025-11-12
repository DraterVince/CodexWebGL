using System.Collections;
using UnityEngine;

public class CharacterJumpAttack : MonoBehaviour
{
public Vector3 characterScale = Vector3.one;
    public bool applyScaleOnStart = true;
    
    public float jumpToEnemyDuration = 0.5f;
    public float jumpBackDuration = 0.5f;
    public float jumpHeight = 2f;
    public float attackDistance = 1.5f;
    public float attackPauseDuration = 0.2f;
    
    public AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    public Animator characterAnimator;
    public bool useSpriteAnimation = true;
    public string idleAnimationName = "Idle";
    public string attackAnimationTrigger = "Attack";
    public bool useAttackTrigger = true;
    public string attackAnimationState = "Attack";
    
    public GameObject attackEffectPrefab;
    public Vector3 attackEffectOffset = Vector3.zero;
    
    // CRITICAL: In multiplayer, we don't want to return to original position after attack
    // The character should slide off screen instead
    public bool returnToStartPositionAfterAttack = true;
    
    private Vector3 originalPosition;
    private bool isAnimating = false;
    private bool hasShownIdleWarning = false;

    void Start()
    {
        if (applyScaleOnStart)
   {
            transform.localScale = characterScale;
        }
        
        originalPosition = transform.position;
        
        if (characterAnimator == null && useSpriteAnimation)
        {
    characterAnimator = GetComponent<Animator>();
        
            if (characterAnimator == null)
  {
      characterAnimator = GetComponentInChildren<Animator>();
            }

            if (characterAnimator == null)
   {
 Debug.LogWarning($"[CharacterJumpAttack] {gameObject.name}: No Animator found. Sprite animations will not play. Either add an Animator component or disable 'Use Sprite Animation'.");
            }
 }
        
        if (useSpriteAnimation && characterAnimator != null)
     {
        if (characterAnimator.runtimeAnimatorController == null)
     {
    Debug.LogWarning($"[CharacterJumpAttack] {gameObject.name}: Animator found but no Animator Controller assigned! Please assign an Animator Controller or disable 'Use Sprite Animation'.");
                useSpriteAnimation = false;
            }
            else
         {
      characterAnimator.speed = 1f;
           PlayIdleAnimation();
  }
        }
    }

    void Update()
    {
        if (!isAnimating && useSpriteAnimation && characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
        {
      if (!string.IsNullOrEmpty(idleAnimationName))
      {
    try
       {
          var currentState = characterAnimator.GetCurrentAnimatorStateInfo(0);
          if (!currentState.IsName(idleAnimationName) && currentState.normalizedTime >= 1f)
     {
         PlayIdleAnimation();
      }
        }
    catch
        {
       }
  }
        }
    }

    public void SetCharacterAnimator(Animator newAnimator)
    {
        characterAnimator = newAnimator;
        if (characterAnimator != null)
        {
     characterAnimator.speed = 1f;
        }
        PlayIdleAnimation();
    }
    
    public void SetCharacterScale(Vector3 scale)
    {
      characterScale = scale;
        transform.localScale = characterScale;
    }
    
    public void SetCharacterScale(float uniformScale)
    {
        SetCharacterScale(new Vector3(uniformScale, uniformScale, uniformScale));
    }
    
    public Vector3 GetCharacterScale()
    {
return characterScale;
    }
    
    public void PlayIdleAnimation()
    {
    if (useSpriteAnimation && characterAnimator != null && !string.IsNullOrEmpty(idleAnimationName))
  {
 if (characterAnimator.runtimeAnimatorController == null)
  {
       return;
   }
         
        try
    {
              characterAnimator.Play(idleAnimationName, 0, 0f);
            }
            catch (System.Exception)
        {
         if (useSpriteAnimation && !hasShownIdleWarning)
      {
     Debug.LogWarning($"[CharacterJumpAttack] {gameObject.name}: Idle state '{idleAnimationName}' not found in Animator Controller. Disabling sprite animations. Please add the state or uncheck 'Use Sprite Animation'.");
   hasShownIdleWarning = true;
           useSpriteAnimation = false;
       }
            }
}
    }
    
    public void PlayAttackAnimation()
    {
        if (useSpriteAnimation && characterAnimator != null)
        {
         if (characterAnimator.runtimeAnimatorController == null)
   {
     return;
            }
  
   if (useAttackTrigger && !string.IsNullOrEmpty(attackAnimationTrigger))
   {
       if (HasParameter(characterAnimator, attackAnimationTrigger))
       {
              characterAnimator.Update(0f);
      characterAnimator.SetTrigger(attackAnimationTrigger);
             characterAnimator.Update(0f);
          }
     }
   else if (!string.IsNullOrEmpty(attackAnimationState))
    {
try
        {
        characterAnimator.Play(attackAnimationState, 0, 0f);
 characterAnimator.Update(0f);
            }
   catch (System.Exception)
          {
           }
            }
   }
  }
    
    public void PerformJumpAttack(Vector3 targetPosition, System.Action onAttackHit = null)
    {
   if (!isAnimating)
 {
      StartCoroutine(JumpAttackCoroutine(targetPosition, onAttackHit));
     }
    }

    public void PerformJumpAttack(Transform target, System.Action onAttackHit = null)
    {
        if (target != null)
        {
            PerformJumpAttack(target.position, onAttackHit);
        }
    }

    private IEnumerator JumpAttackCoroutine(Vector3 targetPosition, System.Action onAttackHit)
    {
        isAnimating = true;
     
        Vector3 startPosition = transform.position;
        Vector3 directionToTarget = (targetPosition - startPosition).normalized;
        Vector3 attackPosition = targetPosition - (directionToTarget * attackDistance);
        
    yield return StartCoroutine(JumpToPosition(startPosition, attackPosition, jumpToEnemyDuration));
        
        if (useSpriteAnimation && characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
        {
            if (useAttackTrigger && !string.IsNullOrEmpty(attackAnimationTrigger))
        {
       if (HasParameter(characterAnimator, attackAnimationTrigger))
 {
      characterAnimator.Update(0f);
          characterAnimator.ResetTrigger(attackAnimationTrigger);
        characterAnimator.SetTrigger(attackAnimationTrigger);
              characterAnimator.Update(0f);
              characterAnimator.Update(0.001f);
                }
            }
  else if (!string.IsNullOrEmpty(attackAnimationState))
   {
    characterAnimator.Play(attackAnimationState, 0, 0f);
 characterAnimator.Update(0f);
   }
            
            yield return null;
        }
        
  yield return new WaitForSeconds(0.05f);
 
    if (onAttackHit != null)
        {
  onAttackHit.Invoke();
        }
    
        if (attackEffectPrefab != null)
        {
     Vector3 effectPosition = transform.position + (directionToTarget * (attackDistance * 0.5f)) + attackEffectOffset;
     GameObject effect = Instantiate(attackEffectPrefab, effectPosition, Quaternion.identity);
            Destroy(effect, 2f);
     }
        
     yield return new WaitForSeconds(attackPauseDuration);
        
        // CRITICAL: In multiplayer, don't return to start position - let the character slide off screen instead
        // Check if we're in multiplayer mode
        bool isMultiplayer = false;
        try
        {
            isMultiplayer = Photon.Pun.PhotonNetwork.IsConnected && Photon.Pun.PhotonNetwork.InRoom;
        }
        catch
        {
            // Photon not available - assume singleplayer
            isMultiplayer = false;
        }
        
        // Only return to start position if explicitly enabled AND not in multiplayer
        if (returnToStartPositionAfterAttack && !isMultiplayer)
        {
            yield return StartCoroutine(JumpToPosition(transform.position, startPosition, jumpBackDuration));
            PlayIdleAnimation();
        }
        else
        {
            // Multiplayer mode - stay at attack position (will slide off screen via SharedMultiplayerGameManager)
            Debug.Log($"[CharacterJumpAttack] {gameObject.name}: Multiplayer mode - staying at attack position (will slide off screen)");
            // Don't play idle animation - character will be deactivated when sliding off screen
        }
        
        isAnimating = false;
    }

    private IEnumerator JumpToPosition(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
     elapsed += Time.deltaTime;
          float t = Mathf.Clamp01(elapsed / duration);
        
 float curvedT = jumpCurve.Evaluate(t);
    
 Vector3 currentPos = Vector3.Lerp(from, to, curvedT);
    
      float heightOffset = jumpHeight * Mathf.Sin(t * Mathf.PI);
        currentPos.y += heightOffset;
            
            transform.position = currentPos;
        
            yield return null;
        }
        
        transform.position = to;
    }

    public bool IsAnimating()
    {
   return isAnimating;
    }

    public void UpdateOriginalPosition()
    {
        if (!isAnimating)
        {
            originalPosition = transform.position;
     }
    }

    public void StopAnimation()
    {
  StopAllCoroutines();
      transform.position = originalPosition;
        isAnimating = false;
        PlayIdleAnimation();
    }
    
  private bool HasParameter(Animator animator, string paramName)
 {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
    if (param.name == paramName)
      return true;
        }
  return false;
    }
}
