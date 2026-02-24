using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class NewPlayerController : MonoBehaviour
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

    // Vectors
    private Vector2 gravityVector;
    private Vector2 movementVector;
    private Vector2 jumpVector;

    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed = 10f;
    private float moveSpeed;
    private float horizontalMovement;     // 1f to go right, -1f to go left
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
    [SerializeField] private int maxJumps = 1;
    private int jumpsRemaining;
    //private LaunchDir currentDir;
    //private LaunchDir previousDir;
    [Space]

    [Header("Gravity")]
    [SerializeField] private float baseGravity = 2f;
    [SerializeField] private float maxFallSpeed = 18f;
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
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    //private bool hurtFlag;
    //private bool wallJumpFlag;

    public UnityEvent RespawnEvent;
    private SoundManager soundManager;

    RaycastHit2D groundInfo;

    private void Awake()
    {
        //baseXScale = transform.localScale.x;

        //if (!rb) rb = GetComponent<Rigidbody2D>();
        //if (!playerBoost) playerBoost = GetComponent<PlayerBoost>();

        //moveSpeed = baseMoveSpeed;
        playerBoost.OnGaugeDepleted.AddListener(EndBoost);

        isInControl = false;
        isInLaunchingSequence = true;
        //TriggerRunAnimation();
        //isRight = startIsRight;
        //FlipSprite(!isRight);

        //currentDir = LaunchDir.Up;
        //previousDir = LaunchDir.Up;

        //transform.position = levelStart.position;
        soundManager = SoundManager.instance;
    }

    private void FixedUpdate()
    {
        GroundCheck();

        ApplyGravity();
        MovePlayer(); // -> jump

        rb.linearVelocity = gravityVector + movementVector + jumpVector;
        //move & test collisions, save for next frame
        //slide if collisions


        // check ground
        // save ground infos

        // calculate gravity & apply rot
        // calculate movement
        // calculate jump

        // sum all vectors (gravity, movement, jump)
        // move & test collision

        // collision (save) => function to slide

        // jumpforce * groundnormal
    }

    private void MovePlayer()
    {
    }

    private void ApplyGravity()
    {
    }

    private void GroundCheck()
    {
        if (groundCheck == null)
        {
            Debug.Log("GroundCheck is null.");
        }

        //groundInfo.isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        Vector2 direction = transform.position - groundCheck.position;
        groundInfo = Physics2D.CircleCast(groundCheck.position, groundCheckRadius, direction, groundCheckRadius, groundLayer);

        Debug.Log($"ground normal: {groundInfo.normal}");
        Debug.DrawLine(transform.position, groundInfo.point);
    }

    #region InputActions
    public void Move(InputAction.CallbackContext context)
    {

    }

    public void Jump(InputAction.CallbackContext context)
    {

    }

    public void Boost(InputAction.CallbackContext context)
    {
        if (isInLaunchingSequence && currentPrompt)
        {
            currentPrompt.ReceiveInput(LaunchSequence.LaunchInputs.Shift);
        }

        if (!isInControl) return;


        if (context.performed)
        {
            //if (isInLaunchingSequence) return;
            //if (hurtFlag) return;
            //if (!playerBoost.CanBoost()) return;
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

        //SetHorizontalMovement(isRight);
    }

    public void EndBoost()
    {
        moveSpeed = baseMoveSpeed;
        //SetLaunchUp();

        BoostUpdate.Invoke(false);

        //if (!isHoldingDownDirection) StopHorizontalMovement();
        //else TriggerIdleAnimation();
    }

    internal void SetCurrentPrompt(Prompt prompt)
    {
        throw new NotImplementedException();
    }
    #endregion
}
