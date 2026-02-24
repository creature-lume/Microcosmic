using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private bool isInControl;

    [SerializeField] private CinemachinePositionComposer c_PositionComposer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerBoost playerBoost;
    [Space]

    [Header("Sprite")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private Animator spriteAnimator;
    [SerializeField] private Animator trailAnimator;
 
    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed = 10f;
    private float moveSpeed;
    private float horizontalMovement;    // 1f to go right, -1f to go left
    private bool isRight;                // if the last input given went right or not
    private bool isHoldingDownDirection; // whether the player is holding down any arrow key
    private bool isWallRunning;
    private bool rampJumpFlag;
    private float baseXScale;
    [SerializeField] private float rotationSmooth;
    private Quaternion orientation;
    [Space]

    [Header("Boost")]
    [SerializeField] private float boostSpeed;
    public UnityEvent<bool> BoostUpdate;
    [Space]

    [Header("Jump")]
    [SerializeField] private float jumpPower = 10f;
    [SerializeField] private int   maxJumps  = 1;
    private int jumpsRemaining;
    private LaunchDir currentDir;
    private LaunchDir previousDir;
    [Space]

    [Header("Gravity")]
    [SerializeField] private float baseGravity         = 2f;
    [SerializeField] private float maxFallSpeed        = 18f;
    [SerializeField] private float fallSpeedMultiplier = 2f;
    private float gravityModifier = 1f;
    [Space]

    [Header("Level")]
    [SerializeField] private bool startIsRight;
    [SerializeField] private Transform levelStart;
    [SerializeField] private float startDelay;
    private bool isInLaunchingSequence = true;
    private Prompt currentPrompt;
    [Space]

    [Header("Groundcheck")]
    [SerializeField] private float forgiveNotGrounded;
    [SerializeField] private Transform _groundCheckPos;
    [SerializeField] private Vector2   _groundCheckSize = new Vector2(0.5f, 0.5f); 
    [SerializeField] private LayerMask _groundLayer;

    private bool hurtFlag;
    private bool wallJumpFlag;

    public UnityEvent RespawnEvent;
    private SoundManager soundManager;

    public enum LaunchDir
    {
        Down,
        DownRight,
        Right,
        UpRight,
        Up,
        UpLeft,
        Left,
        DownLeft
    }

    #region Setters/Getters
    public void SetCurrentPrompt(Prompt prompt) { currentPrompt = prompt; }
    public bool IsFalling() 
    {
        if (gravityModifier == 1f) return rb.gravityScale == gravityModifier * baseGravity * fallSpeedMultiplier;
        else                       return rb.gravityScale == gravityModifier * baseGravity * (fallSpeedMultiplier * 0.5f);
    }

    public bool IsInControl() { return isInControl; }
    private bool InputMatchesDirection()
    {
        return (isRight && horizontalMovement == 1f) || (!isRight && horizontalMovement == -1f);
    }
    private void FlipSprite(bool isFlipped)
    {
        if (isFlipped) transform.localScale = new Vector3(-baseXScale, transform.localScale.y, transform.localScale.z);
        else           transform.localScale = new Vector3(baseXScale, transform.localScale.y, transform.localScale.z);
    }

    private void TriggerRunAnimation()
    {
        spriteAnimator.SetBool("isRunning", true);
        trailAnimator.SetBool("isRunning", true);
    }

    private void TriggerIdleAnimation()
    {
        spriteAnimator.SetBool("isRunning", false);
        trailAnimator.SetBool("isRunning", false);
    }

    private void SetHorizontalMovement(bool goRight)
    {
        TriggerRunAnimation();

        rampJumpFlag = false;
        wallJumpFlag = false;

        if (goRight)
        {
            horizontalMovement = 1f;
            return;
        }

        horizontalMovement = -1f;
        return;
    }

    private void SetHorizontalMovement(float value)
    {
        TriggerRunAnimation();
        rampJumpFlag       = false;
        wallJumpFlag       = false;
        horizontalMovement = value;
    }

    private void StopHorizontalMovement()
    {
        TriggerIdleAnimation();
        rampJumpFlag       = false;
        wallJumpFlag       = false;
        horizontalMovement = 0f;
    }

    public void SetLaunchUp()
    {
        currentDir      = LaunchDir.Up;
        if (previousDir == LaunchDir.Down)
        {
            if (rb.linearVelocity.x < 0) isRight = false;
            if (rb.linearVelocity.x > 0) isRight = true;
        }
        orientation     = Quaternion.Euler(0, spriteTransform.rotation.y, 0);
        isWallRunning   = false;
        gravityModifier = 1f;
        previousDir = currentDir;
        transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
    }
    #endregion

    // --------------------------------------- START ------------------------------------------
    private void Start()
    {
        Debug.LogWarning($"PlayerController is deprecated. Use NewPlayerController instead.");

        baseXScale = transform.localScale.x;

        if (!rb)                   rb = GetComponent<Rigidbody2D>();
        if (!playerBoost) playerBoost = GetComponent<PlayerBoost>();

        moveSpeed = baseMoveSpeed;
        playerBoost.OnGaugeDepleted.AddListener(EndBoost);

        isInControl = false;
        isInLaunchingSequence = true;
        TriggerRunAnimation();
        isRight = startIsRight;
        FlipSprite(!isRight);

        currentDir  = LaunchDir.Up;
        previousDir = LaunchDir.Up;

        transform.position = levelStart.position;
        soundManager = SoundManager.instance;
    }

    // --------------------------------------- UPDATE ------------------------------------------
    private void FixedUpdate()
    {
        if (isInLaunchingSequence)
        {
            rb.linearVelocity = new Vector2(1f * moveSpeed, rb.linearVelocity.y);
            return;
        }

        if      (rampJumpFlag && currentDir == LaunchDir.UpLeft)  FlipSprite(true);
        else if (rampJumpFlag && currentDir == LaunchDir.UpRight) FlipSprite(false);
        else                                                      FlipSprite(!isRight);

        if (isWallRunning)
        {
            if (currentDir == LaunchDir.Right) rb.linearVelocity = new Vector2(-1f, horizontalMovement * moveSpeed);
            if (currentDir == LaunchDir.Left)  rb.linearVelocity = new Vector2(1f, horizontalMovement * moveSpeed);
        }
        else                                   rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y);

        c_PositionComposer.Lookahead.IgnoreY = !isWallRunning;

        GravityCheck();
        GroundCheck();
    }

    // -----------------------------------------------------------------------------------------
    private void GravityCheck()
    {
        if (isGrounded())
        {
            rb.gravityScale = gravityModifier * baseGravity;
            return;
        }

        if (rb.linearVelocity.y < 0 && gravityModifier == 1f)
        {
            rb.gravityScale   = gravityModifier * baseGravity * fallSpeedMultiplier;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
        }
        else if (gravityModifier == -1f)
        {
            rb.gravityScale = gravityModifier * baseGravity * (fallSpeedMultiplier * 0.5f);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, maxFallSpeed));
        }
        else rb.gravityScale = gravityModifier * baseGravity;
    }

    public void Move(InputAction.CallbackContext context)
    {
        if (isInLaunchingSequence && currentPrompt)
        {
            if (context.ReadValue<Vector2>().x > 0) currentPrompt.ReceiveInput(LaunchSequence.LaunchInputs.Right);
            else currentPrompt.ReceiveInput(LaunchSequence.LaunchInputs.Left);
        }

        if (!isInControl) return;

        if (context.performed)
        {
            if (isInLaunchingSequence) return;
            SetHorizontalMovement(context.ReadValue<Vector2>().x);
            if (horizontalMovement > 0) isRight = true;
            if (horizontalMovement < 0) isRight = false;

            if (horizontalMovement == 0f) TriggerIdleAnimation();

            isHoldingDownDirection = true;
        }

        else if (context.canceled)
        {
            if (isInLaunchingSequence) return;
            if (!playerBoost.IsBoosting()) StopHorizontalMovement();
            isHoldingDownDirection = false;
        }
    }

    public void InputUp(InputAction.CallbackContext context)
    {
        if (!isInLaunchingSequence || !currentPrompt) return;
            currentPrompt.ReceiveInput(LaunchSequence.LaunchInputs.Up);
    }

    public void InputDown(InputAction.CallbackContext context)
    {
        if (!isInLaunchingSequence || !currentPrompt) return;
            currentPrompt.ReceiveInput(LaunchSequence.LaunchInputs.Down);
    }

    #region Boost
    public void Boost(InputAction.CallbackContext context)
    {
        if (isInLaunchingSequence && currentPrompt)
        {
            currentPrompt.ReceiveInput(LaunchSequence.LaunchInputs.Shift);
        }

        if (!isInControl) return;


        if (context.performed)
        {
            if (isInLaunchingSequence) return;
            if (hurtFlag) return;
            if (!playerBoost.CanBoost()) return;
            DoBoost();
        }
        
        else if (context.canceled)
        {
            if (isInLaunchingSequence) return;
            EndBoost();
        }
    }

    public void DoBoost()
    {
        moveSpeed = boostSpeed;

        BoostUpdate.Invoke(true);
        playerBoost.StartBoost();

        SetHorizontalMovement(isRight);
    }

    public void EndBoost()
    {
        moveSpeed = baseMoveSpeed;
        SetLaunchUp();

        BoostUpdate.Invoke(false);

        if (!isHoldingDownDirection) StopHorizontalMovement();
        else                         TriggerIdleAnimation();
    }
    #endregion
    #region Jump
    public void Jump(InputAction.CallbackContext context)
    {
        if (isInLaunchingSequence && currentPrompt) currentPrompt.ReceiveInput(LaunchSequence.LaunchInputs.Space);

        if (jumpsRemaining == 0 || !isInControl) return;

        if (context.performed)
        {
            DoJump(currentDir, jumpPower);
        }
        else if (context.canceled)
        {
            if (isInLaunchingSequence) return;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            jumpsRemaining--;
        }
    }

    private void DoJump(LaunchDir jumpDir, float power)
    {
        if (isInLaunchingSequence) return;
        isWallRunning = false;
        switch (jumpDir) //I am so sorry for this ugly ass code but this is a jam we gotta ball
        {
            case LaunchDir.Up:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, power);
                soundManager.PlayJumpSFX();
                break;
            case LaunchDir.Left:
                if (previousDir == LaunchDir.Up)
                {
                    Debug.Log("Nope.");
                    break;
                }
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, power);
                SetHorizontalMovement(false);
                wallJumpFlag = true;
                orientation = Quaternion.Euler(0, spriteTransform.rotation.y, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
                soundManager.PlayJumpSFX();
                break;
            case LaunchDir.UpLeft:
                if (horizontalMovement > 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, power);
                    break;
                }
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, power);
                SetHorizontalMovement(false);
                if (!isHoldingDownDirection) rampJumpFlag = true;
                soundManager.PlayJumpSFX();
                break;
            case LaunchDir.DownLeft:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -power);
                soundManager.PlayJumpSFX();
                break;
            case LaunchDir.Down:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -power);
                soundManager.PlayJumpSFX();
                break;
            case LaunchDir.DownRight:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -power);
                soundManager.PlayJumpSFX();
                break;
            case LaunchDir.Right:
                if (previousDir == LaunchDir.Up)
                {
                    Debug.Log("Nope.");
                    break;
                }
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, power);
                SetHorizontalMovement(true);
                wallJumpFlag = true;
                orientation = Quaternion.Euler(0, spriteTransform.rotation.y, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
                soundManager.PlayJumpSFX();
                break;
            case LaunchDir.UpRight:
                if (horizontalMovement < 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, power);
                    break;
                }
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, power);
                SetHorizontalMovement(true);
                if (!isHoldingDownDirection) rampJumpFlag = true;
                soundManager.PlayJumpSFX();
                break;
        }

        SetLaunchUp();

        if (playerBoost.IsBoosting()) rampJumpFlag = false;

        gravityModifier = 1f;
        jumpsRemaining--;
    }

    #endregion
    private void GroundCheck()
    {
        if (!isGrounded())
        {
            if (isWallRunning && IsFalling()) {SetLaunchUp(); return;}

            if (rampJumpFlag || wallJumpFlag || !IsFalling()) return;
            gravityModifier = 1f;
            orientation = Quaternion.Euler(0, spriteTransform.rotation.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, orientation, forgiveNotGrounded * Time.deltaTime);
            return;
        }

        jumpsRemaining = maxJumps;

        //Returns control of the player once they touch the ground after getting hurt
        if (hurtFlag || IsFalling())
        {
            if (!isHoldingDownDirection) StopHorizontalMovement();
            isInControl = true;
            hurtFlag = false;
        }

        switch (Physics2D.OverlapBox(_groundCheckPos.position, _groundCheckSize, 0, _groundLayer).tag)
        {
            case "LaunchUp":
                SetLaunchUp();
                if (rampJumpFlag) StopHorizontalMovement();
                if (wallJumpFlag)                                                     //Prevents the player from continuously sliding to the opposite
                {                                                                     //direction they're inputing after a wall jump
                    if (isHoldingDownDirection) StopHorizontalMovement();

                    else if (!InputMatchesDirection()) SetHorizontalMovement(isRight);
                }
                break;

            case "LaunchUpLeft":
                if (rampJumpFlag && IsFalling()) StopHorizontalMovement();
                currentDir = LaunchDir.UpLeft;
                orientation = Quaternion.Euler(0, spriteTransform.rotation.y, 45);
                transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
                isWallRunning = false;
                gravityModifier = 1f;

                if (!playerBoost.IsBoosting()) break;
                else if (previousDir == LaunchDir.Down)  SetHorizontalMovement(true); 
                previousDir = currentDir;
                break;

            case "LaunchLeft":
                if (!playerBoost.IsBoosting()) break;
                currentDir = LaunchDir.Left;
                orientation = Quaternion.Euler(0, spriteTransform.rotation.y, 90);
                transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
                gravityModifier = 1f;

                if      (previousDir == LaunchDir.UpLeft)   SetHorizontalMovement(true);
                else if (previousDir == LaunchDir.DownLeft) SetHorizontalMovement(false);
                isWallRunning = true;
                previousDir = currentDir;
                break;

            case "LaunchDownLeft":
                if (!playerBoost.IsBoosting()) break;
                currentDir = LaunchDir.DownLeft;
                orientation = Quaternion.Euler(0, spriteTransform.rotation.y, 135);
                transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
                isWallRunning = false;
                gravityModifier = -1f;

                if      (previousDir == LaunchDir.Left) SetHorizontalMovement(false);
                else if (previousDir == LaunchDir.Down) SetHorizontalMovement(true); 
                previousDir = currentDir;
                break;

            case "LaunchDown":
                if (!playerBoost.IsBoosting()) break;
                currentDir = LaunchDir.Down;
                orientation = Quaternion.Euler(0, spriteTransform.rotation.y, 180);
                transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
                isWallRunning = false;
                gravityModifier = -1f;

                if      (previousDir == LaunchDir.DownLeft)  SetHorizontalMovement(false);
                else if (previousDir == LaunchDir.DownRight) SetHorizontalMovement(true); 
                previousDir = currentDir;
                break;

            case "LaunchDownRight":
                if (!playerBoost.IsBoosting()) break;
                currentDir = LaunchDir.DownRight;
                orientation = Quaternion.Euler(0, spriteTransform.rotation.y, -135);
                transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
                isWallRunning = false;
                gravityModifier = -1f;

                if (previousDir == LaunchDir.Right) SetHorizontalMovement(true); 
                previousDir = currentDir;
                break;

            case "LaunchRight":
                if (!playerBoost.IsBoosting()) break;
                currentDir = LaunchDir.Right;
                orientation = Quaternion.Euler(0, spriteTransform.rotation.y, -90);
                transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
                isWallRunning = true;
                gravityModifier = 1f;

                if (previousDir == LaunchDir.DownRight) SetHorizontalMovement(false);
                if (previousDir == LaunchDir.UpRight)   SetHorizontalMovement(true); 
                previousDir = currentDir;
                break;

            case "LaunchUpRight":
                if (rampJumpFlag && IsFalling()) StopHorizontalMovement();
                currentDir = LaunchDir.UpRight;
                orientation = Quaternion.Euler(0, spriteTransform.rotation.y, -45);
                transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
                isWallRunning = false;
                gravityModifier = 1f;

                if (!playerBoost.IsBoosting()) break;
                if (previousDir == LaunchDir.Right) SetHorizontalMovement(false); //true for loop de loops
                //if (previousDir == LaunchDir.Up)    SetHorizontalMovement(false); 
                //if (previousDir == LaunchDir.Left)  SetHorizontalMovement(false); 
                previousDir = currentDir;
                break;
        }
    }

    public void OnStartLevel(Prompt.PromptQuality quality)
    {
        switch (quality)
        {
            default:
            case Prompt.PromptQuality.Failed:
                playerBoost.AddGauge(50);
                break;
            case Prompt.PromptQuality.Ok:
                playerBoost.AddGauge(100);
                break;
            case Prompt.PromptQuality.Good:
                playerBoost.AddGauge(200);
                break;
            case Prompt.PromptQuality.Perfect:
                playerBoost.AddGauge(300);
                break;
        }

        StartCoroutine(StartLevelDelay());
    }

    private IEnumerator StartLevelDelay()
    {
        isInLaunchingSequence = false;
        isInControl = true;
        if (!isHoldingDownDirection) TriggerIdleAnimation();
        yield return new WaitForSeconds(startDelay);
        if (!isHoldingDownDirection)
        {
            StopHorizontalMovement();
            TriggerIdleAnimation();
        }
    }

    public void OnGetHurt()
    {
        isInControl = false;
        
        if (playerBoost.IsBoosting()) EndBoost();

        hurtFlag      = true;
        isWallRunning = false;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
        SetHorizontalMovement(!isRight);
        rampJumpFlag = true;
        soundManager.PlaySFX(SoundManager.SFX.Damage);
    }

    private bool isGrounded()
    {
        if (Physics2D.OverlapBox(_groundCheckPos.position, _groundCheckSize, 0, _groundLayer)) return true;

        else return false;
    }

    public void ReturnToSpawn()
    {
        SetLaunchUp();
        StopHorizontalMovement();
        transform.position = levelStart.position;
    }

    public void OnLoseLife(Vector3 checkpoint)
    {
        SetHorizontalMovement(false);
        rb.linearVelocity  = Vector2.zero;
        transform.position = checkpoint;
        RespawnEvent.Invoke();
        soundManager.PlaySFX(SoundManager.SFX.Death);
        playerBoost.AddGauge(100);
    }

    public void Bump(LaunchDir bumpDir, float bumpPower)
    {
        isWallRunning = false;
        rampJumpFlag  = false;
        wallJumpFlag  = false;

        soundManager.PlaySFX(SoundManager.SFX.Bumper);

        if (jumpsRemaining >= 0) jumpsRemaining = 1;

        if (isInLaunchingSequence) return;
        isWallRunning = false;
        switch (bumpDir) //I am so sorry for this ugly ass code but this is a jam we gotta ball
        {
            case LaunchDir.Up:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bumpPower);
                break;
            case LaunchDir.Left:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bumpPower);
                SetHorizontalMovement(false);
                isRight = false;
                wallJumpFlag = true;
                orientation = Quaternion.Euler(0, spriteTransform.rotation.y, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
                break;
            case LaunchDir.UpLeft:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bumpPower);
                SetHorizontalMovement(false);
                isRight = false;
                if (!isHoldingDownDirection) rampJumpFlag = true;
                break;
            case LaunchDir.DownLeft:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -bumpPower);
                isRight = false;
                break;
            case LaunchDir.Down:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -bumpPower);
                break;
            case LaunchDir.DownRight:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -bumpPower);
                isRight = true;
                break;
            case LaunchDir.Right:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bumpPower);
                SetHorizontalMovement(true);
                isRight = true;
                wallJumpFlag = true;
                orientation = Quaternion.Euler(0, spriteTransform.rotation.y, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, orientation, Time.deltaTime * rotationSmooth);
                break;
            case LaunchDir.UpRight:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, bumpPower);
                SetHorizontalMovement(true);
                isRight = true;
                if (!isHoldingDownDirection) rampJumpFlag = true;
                break;
        }

        SetLaunchUp();

        if (playerBoost.IsBoosting()) rampJumpFlag = false;

        gravityModifier = 1f;
        jumpsRemaining--;
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed) GUIManager.instance.TogglePause();
    }

    #region UnityTool
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_groundCheckPos.position, _groundCheckSize);
    }
    #endregion
}
