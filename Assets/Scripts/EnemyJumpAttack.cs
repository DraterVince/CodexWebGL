using System.Collections;
using UnityEngine;

public class EnemyJumpAttack : MonoBehaviour
{
    public Vector3 characterScale = Vector3.one;
    public bool applyScaleOnStart = true;
    
    public float jumpToPlayerDuration = 0.4f;
    public float jumpBackDuration = 0.4f;
    public float jumpHeight = 1.5f;
public float attackDistance = 1.5f;
    public float attackPauseDuration = 0.15f;
    
    public AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public Animator characterAnimator;
    public bool useSpriteAnimation = true;
  public string idleAnimationName = "Idle";
    public string attackAnimationTrigger = "Attack";
    public bool useAttackTrigger = true;
    public string attackAnimationState = "Attack";
    
  public GameObject attackEffectPrefab;
    public Vector3 attackEffectOffset = Vector3.zero;
    
  private Vector3 originalPosition;
    private bool isAnimating = false;
    private bool hasShownIdleWarning = false;

    void Start()
    {
        if (applyScaleOnStart)
{
     transform.localScale = characterScale;
      }
        
        // Initialize original position - this will be updated before each attack
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
       Debug.LogWarning($"[EnemyJumpAttack] {gameObject.name}: No Animator found. Sprite animations will not play. Either add an Animator component or disable 'Use Sprite Animation'.");
       }
  }
        
if (useSpriteAnimation && characterAnimator != null)
        {
            if (characterAnimator.runtimeAnimatorController == null)
       {
        Debug.LogWarning($"[EnemyJumpAttack] {gameObject.name}: Animator found but no Animator Controller assigned! Please assign an Animator Controller or disable 'Use Sprite Animation'.");
                useSpriteAnimation = false;
         }
  else
            {
 characterAnimator.speed = 1f;
   PlayIdleAnimation();
   }
        }
    }
    
    void OnEnable()
    {
        // Update original position when enemy is enabled (in case it was repositioned)
        if (!isAnimating)
        {
            originalPosition = transform.position;
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
        Debug.LogWarning($"[EnemyJumpAttack] {gameObject.name}: Idle state '{idleAnimationName}' not found in Animator Controller. Disabling sprite animations. Please add the state or uncheck 'Use Sprite Animation'.");
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
 
        // Update original position before attack to ensure we return to the correct position
        originalPosition = transform.position;
        Vector3 startPosition = originalPosition;
        
        // Calculate direction from enemy to player
        Vector3 directionToTarget = (targetPosition - startPosition);
        
        // Check if distance is valid (avoid division by zero)
        float distanceToTarget = directionToTarget.magnitude;
        if (distanceToTarget < 0.01f)
        {
            Debug.LogWarning($"[EnemyJumpAttack] {gameObject.name}: Target position is too close to enemy position! Enemy: {startPosition}, Target: {targetPosition}");
            // Just play attack animation in place
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
            }
            
            yield return new WaitForSeconds(0.05f);
            if (onAttackHit != null) { onAttackHit.Invoke(); }
            yield return new WaitForSeconds(attackPauseDuration);
            isAnimating = false;
            yield break;
        }
        
        directionToTarget = directionToTarget.normalized;
        
        // Calculate attack position - enemy should jump TOWARDS player
        // The attack position should be a point between enemy and player, at attackDistance from player
        // If enemy is very close (< attackDistance), jump to 80% of the way to player
        // If enemy is far away, jump to a point that's attackDistance away from player
        float jumpDistance;
        if (distanceToTarget <= attackDistance)
        {
            // Enemy is already close - jump 80% of the way to player
            jumpDistance = distanceToTarget * 0.8f;
        }
        else
        {
            // Enemy is far - jump to a point that's attackDistance away from player
            jumpDistance = distanceToTarget - attackDistance;
        }
        
        // Ensure minimum jump distance
        jumpDistance = Mathf.Max(0.5f, jumpDistance);
        
        // Calculate attack position from enemy's starting position
        Vector3 attackPosition = startPosition + (directionToTarget * jumpDistance);
        
        // Make sure we don't overshoot the player
        float finalDistanceToPlayer = Vector3.Distance(attackPosition, targetPosition);
        if (finalDistanceToPlayer < attackDistance * 0.5f)
        {
            // If we're too close, adjust to be exactly attackDistance away
            Vector3 directionFromAttackToPlayer = (targetPosition - attackPosition).normalized;
            attackPosition = targetPosition - (directionFromAttackToPlayer * attackDistance);
        }
        
        Debug.Log($"[EnemyJumpAttack] {gameObject.name}: Enemy at {startPosition}, Player at {targetPosition}, Distance: {distanceToTarget:F2}");
        Debug.Log($"[EnemyJumpAttack] Jump distance: {jumpDistance:F2}, Attack position: {attackPosition}, Final distance to player: {Vector3.Distance(attackPosition, targetPosition):F2}");
        
        // Make enemy face the player before jumping (for 2D sprites, this might need sprite flipping instead)
        // Note: For 2D sprites, you might want to flip the sprite scale instead of rotating
        if (directionToTarget.magnitude > 0.01f)
        {
            // For 2D: Calculate which direction enemy should face
            // If moving right (positive X), keep default rotation
            // If moving left (negative X), flip sprite or rotate 180 degrees
            float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            
            // For 2D sprites, you might want to use this instead:
            // SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            // if (spriteRenderer != null && directionToTarget.x < 0)
            // {
            //     spriteRenderer.flipX = true;
            // }
            // else if (spriteRenderer != null && directionToTarget.x > 0)
            // {
            //     spriteRenderer.flipX = false;
            // }
            
            // For now, use rotation (this works for 3D or if sprite supports rotation)
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        yield return StartCoroutine(JumpToPosition(startPosition, attackPosition, jumpToPlayerDuration));
        
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
     
      // CRITICAL FIX: Return to originalPosition, not startPosition, to prevent jumping backwards
      yield return StartCoroutine(JumpToPosition(transform.position, originalPosition, jumpBackDuration));
        
        PlayIdleAnimation();
  
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
