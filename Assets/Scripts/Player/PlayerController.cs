using System;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // ------------------------------------------------------------------
    [Header("Debug Only")]
    public TextMeshProUGUI DEBUG1;
    public TextMeshProUGUI DEBUG2;
    public TextMeshProUGUI DEBUG3;
    [Space]

    // ------------------------------------------------------------------
    [Header("Misc References")]
    [SerializeField] private CinemachinePositionComposer c_PositionComposer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerBoost playerBoost;
    private SoundManager soundManager;
    [Space]

    // ------------------------------------------------------------------
    [Header("Ground Check")]
    [SerializeField] private float     groundedDistance;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    private RaycastHit2D groundInfo;
    private float groundAngle;
    private bool isGrounded;
    private bool isWallRunning;
    private bool isUpsideDown;
    private bool isOnSlope;
    [Space]

    // ------------------------------------------------------------------
    [Header("Rotation")]
    [SerializeField] private float rotationSmooth;
    [SerializeField] private float fallRotationSmooth = 10.0f;
    private Quaternion orientation;
    private bool isFacingRight = true;
    [Space]

    // ------------------------------------------------------------------
    [Header("Gravity")]
    [SerializeField] private float gravity             = 98f;
    [SerializeField] private float fallGravityModifier = 2;
    [SerializeField] private float keepGroundedGravityAttenuation = 0.1f;
    private Vector2 appliedGravity;
    [Space]

    // ------------------------------------------------------------------
    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed = 10f;
    private float   moveSpeed;
    private Vector2 movementInput;
    private bool    isHoldingDownMove;
    private bool    isInControl;
    private Vector2 appliedMovement;
    [Space]

    [Header("Slide Inertia")]
    [SerializeField] private float slideInertiaStopAngle = 35;
    private bool isInSlideInertia = false;
    [Space]

    // ------------------------------------------------------------------
    [Header("Boost")]
    [SerializeField] private float boostSpeed;
    [SerializeField] private float boostOnlyAngleMin = 80;
    [SerializeField] private float boostOnlyAngleMax = 180;
    [SerializeField] private float boostGravityAttenuation = 0.5f;
    public UnityEvent<bool> BoostUpdate;
    private bool isBoosting => playerBoost.IsBoosting();
    [Space]

    // ------------------------------------------------------------------
    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float maxJump   = 100f;
    private bool canJump;
    private bool isJumping;
    private Vector2 appliedJump;
    [Space]

    // ------------------------------------------------------------------
    [Header("Sprite")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private Animator  spriteAnimator;
    [SerializeField] private Animator  trailAnimator;
    private Vector3 scale;
    [Space]

    // ------------------------------------------------------------------
    [Header("Respawn")]
    public UnityEvent RespawnEvent;
    [Space]

    // ------------------------------------------------------------------
    [Header("Level")]
    [SerializeField] private Transform levelStart;
    [SerializeField] private float     startDelay;
    //private bool isInLaunchingSequence = true;
    //private Prompt currentPrompt;

    private void OnValidate() { scale = transform.localScale; }

    // PLEASE READ (to the corrector):
    //
    // This script is a refactored version of "OldPlayerController.cs", which was rushed for a game jam
    // The goal was to make a modular, adaptive, (as glitchless as possible) physics based sonic-like player controller
    //      (Specifically based on Sonic Rush and its boost mechanic)
    //
    // The old script was locked to 8 jumping directions, and relied on switch cases on tilemap tags
    // Also, it only had three gravities: negative, 0, or positive, because it relied on Unity's built-in rigidbody gravity
    // This meant that the player was susceptible to slide off a wall if they had some residue force sending them away from it
    //
    // This new script should work regardless of tags, facillitating Level Design as there is no need to work on 10 tilemaps
    //      (the old 10 tilemaps were: Launching Up/UpRight/Right/DownRight/Down/DownLeft/Left/LeftUp, NoLaunch, and HiddenArea)
    //      There are now only 2 tilemaps: the level tilemap (Stage) and HiddenArea
    // This new script also added a gauged jump, which was absent before
    // It also added a feedback to better teach the player that they must boost to go up slopes (slideInertia)
    // Custom gravity was made instead of the built-in RigidBody gravity
    //
    // The player is still sometimes susceptible to randomly falling off of upside-down-surfaces or getting launched into a wall
    //      (although it is way less glitchy than the old spaghetti controller)
    // I would appreciate it if you could document these occurrences or offer potential causes or solutions
    // Additionally, since it relies heavily on surface normals and Unity's default mesh collider tends to be faulty, level element colliders must
    // be check manually to make sure there is no odd normal (such as a 90° angle on a rounded slope) which could make the player fall off

    public void Awake()
    {
        if (!rb)                   rb = GetComponent<Rigidbody2D>();
        if (!playerBoost) playerBoost = GetComponent<PlayerBoost>();

        playerBoost.OnGaugeDepleted.AddListener(EndBoost);

        //isInLaunchingSequence = true;
        //TriggerRunAnimation();

        //transform.position = levelStart.position;
        soundManager = SoundManager.instance;

        moveSpeed = baseMoveSpeed;
        rb.gravityScale = 0;

        DEBUG1.color = Color.red;
        DEBUG2.color = Color.green;
        DEBUG3.color = Color.blue;

        isInControl = true;
    }

    private void FixedUpdate()
    {
        GroundCheck();
        RotationCheck();
        GravityCheck();
        SlideInertiaCheck();
        MoveCheck();
        CameraCheck();

        ApplyMovement();

        DEBUG1.SetText($"");
        DEBUG2.SetText($"");
        DEBUG3.SetText($"");

        CheckAndFaceDirection();
        AnimationCheck();
    }

    private void GroundCheck()
    {
        groundInfo  = Physics2D.Raycast(transform.position, groundCheck.position - transform.position, groundedDistance, groundLayer);
        groundAngle = Vector3.Angle(groundInfo.normal, Vector3.up);

        isGrounded  = groundInfo.collider != null;
        canJump     = isGrounded;
        
        isWallRunning = (groundAngle == 90);
        isUpsideDown  = (groundAngle  > 90);
        isOnSlope     = (groundAngle > 20 && groundAngle < 90);

        if (isGrounded  && !canJump && isJumping) EndJump();
    }
    private void RotationCheck()
    {
        if (!isGrounded)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime * fallRotationSmooth);
            return;
        }

        if (!isOnSlope && groundAngle >= 90 && !isWallRunning && !isUpsideDown) return;

        orientation        = Quaternion.FromToRotation(transform.up, groundInfo.normal);
        transform.rotation = Quaternion.Slerp(transform.rotation, orientation * transform.rotation, Time.deltaTime * rotationSmooth);
    }

    private bool IsInBoostOnlyRange(float x) { return boostOnlyAngleMin <= x && x <= boostOnlyAngleMax; }

    private void GravityCheck()
    {
        if (isBoosting && isGrounded) // Stick to surface while boosting
        {
            appliedGravity = (appliedMovement.magnitude * boostGravityAttenuation) * -groundInfo.normal;
            return;
        }

        if (isInSlideInertia) // If should slide off a ramp
        {
            appliedGravity  += new Vector2(0, -gravity * Time.deltaTime * fallGravityModifier);
            return;
        }
        
        if (isGrounded && !isBoosting && (!isWallRunning && !isUpsideDown)) // Stay on the ground but don't be slowed down
        {                                                                   // during regular grounded (unboosted) movements
            appliedGravity = (-gravity * keepGroundedGravityAttenuation) * groundInfo.normal;
            return;
        }

        if (isWallRunning || isUpsideDown) // If boosted stopped while wall running/upside down
        {
            appliedGravity = new Vector2(0, -gravity * boostGravityAttenuation);
            return;
        }
         
        if (rb.linearVelocityY < 0) appliedGravity += new Vector2(0, -gravity * Time.deltaTime * fallGravityModifier); // Fall faster if not rising from a jump
        else                        appliedGravity += new Vector2(0, -gravity * Time.deltaTime);                       // Regular gravity 
    }
    private void SlideInertiaCheck()
    {
        if (groundAngle <= slideInertiaStopAngle)
        {
            isInSlideInertia = false;
            return;
        }

        if (!isBoosting && IsInBoostOnlyRange(groundAngle))
        {
            if (!isInSlideInertia && isWallRunning)
            {
                if (isFacingRight) SetIsFacingRight(rb.linearVelocity.y < 0);
                else               SetIsFacingRight(rb.linearVelocity.y > 0);
            }

            isInSlideInertia = true;
            return;
        }
    }

    #region Movement
    public void Move(InputAction.CallbackContext context)
    {
        if (!isInControl) return;

        if (context.started)
        {
            if ((isWallRunning || isUpsideDown) && isHoldingDownMove && !isJumping) return;

            isHoldingDownMove = true;
            movementInput     = context.ReadValue<Vector2>();
        }

        if (context.canceled)
        {
            isHoldingDownMove = false;
            if (isBoosting) return;

            movementInput = Vector2.zero;
        }
    }
    private void MoveCheck()
    {
        if (isInSlideInertia || movementInput.x == 0)
        {
            appliedMovement = Vector2.zero;
            return;
        }

        if (movementInput.x > 0) appliedMovement = transform.right  * moveSpeed;
        else                     appliedMovement = -transform.right * moveSpeed;
    }
    private void ApplyMovement()
    {
        Vector2 appliedForces = appliedMovement + appliedGravity;
        rb.AddForce(appliedForces);
        rb.linearVelocity += appliedJump;
    }
    #endregion

    #region FaceDirection
    private void CheckAndFaceDirection()
    {
        if (!isHoldingDownMove) return;

        if (isUpsideDown)
        {
            if (rb.linearVelocity.x < 0) SetIsFacingRight(true);
            else                         SetIsFacingRight(false); 
            return;
        }

        if (isWallRunning)
        {
            // Left wall
            if (groundInfo.normal.x == 1) 
            {
                if (rb.linearVelocity.y > 0) SetIsFacingRight(false);
                else                         SetIsFacingRight(true);
                return;
            }

            // Right wall
            if (rb.linearVelocity.y > 0) SetIsFacingRight(true);
            else                         SetIsFacingRight(false);
            return;
        }

        if (rb.linearVelocity.x < 0) SetIsFacingRight(false);
        else                         SetIsFacingRight(true);
    }

    private void SetIsFacingRight(bool faceRight)
    {
        isFacingRight = faceRight;

        if (isFacingRight)
        {
            transform.localScale = scale;
            return;
        }

        transform.localScale = new Vector3(-scale.x, scale.y, scale.z);
    }
    #endregion

    #region Animation 
    // Ideally, the animations would be in their own script
    private void AnimationCheck()
    {
        if (rb.linearVelocity == Vector2.zero) TriggerIdleAnimation();
        else TriggerRunAnimation();
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
    #endregion

    private void CameraCheck() { c_PositionComposer.Lookahead.IgnoreY = !isWallRunning; }

    #region Jump
    public void Jump(InputAction.CallbackContext context)
    {
        if (!isInControl) return;

        if (context.started)
        {
            if (!canJump || !isGrounded) return;
            StartCoroutine(JumpRoutine());
        }

        if (context.canceled) EndJump(); 
    }

    private IEnumerator JumpRoutine()
    {
        soundManager.PlayJumpSFX();

        isJumping = true;
        canJump   = false;

        float accumulatedJump = 0f;

        if (isWallRunning || isUpsideDown) 
             appliedJump = jumpForce * groundInfo.normal;
        else appliedJump = jumpForce * groundInfo.normal;

        while(isJumping)
        {
            accumulatedJump += jumpForce;

            if (accumulatedJump >= maxJump && isJumping) EndJump();

            yield return new WaitForFixedUpdate();
        }
    }

    private void EndJump()
    {
        isJumping = false;

        appliedJump = Vector2.zero;
        canJump     = isGrounded;

        StopCoroutine(JumpRoutine());
    }
    #endregion
    #region Boost
    public void Boost(InputAction.CallbackContext context)
    {
        if (!isInControl) return;

        if (context.performed)
        {
            TryBoost();

            if (!isBoosting) return;

            moveSpeed        = boostSpeed;
            isInSlideInertia = false;
            if (isHoldingDownMove) return;

            if (isFacingRight) movementInput = Vector2.right;
            else               movementInput = Vector2.left;

            return;
        }

        if (context.canceled) EndBoost();
    }
    public void TryBoost()
    {
        BoostUpdate.Invoke(true);
        playerBoost.StartBoost();
    }

    public void EndBoost()
    {
        moveSpeed = baseMoveSpeed;
        if (!isHoldingDownMove) movementInput = Vector2.zero;
        BoostUpdate.Invoke(false);
    }
    #endregion

    internal void SetCurrentPrompt(Prompt prompt)
    {
        throw new NotImplementedException();
    }
}
