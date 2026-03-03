using System;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class NewPlayerController : MonoBehaviour
{
    private bool isInControl;

    public TextMeshProUGUI DEBUG1;
    public TextMeshProUGUI DEBUG2;
    public TextMeshProUGUI DEBUG3;
    [SerializeField] private float homemadeGravity = 9.8f;
    [Space]

    [SerializeField] private CinemachinePositionComposer c_PositionComposer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerBoost playerBoost;
    [Space]

    [Header("Sprite")]
    [SerializeField] private Transform spriteTransform;
    [SerializeField] private Animator  spriteAnimator;
    [SerializeField] private Animator  trailAnimator;

    [Header("Movement")]
    // Movement Speed
    [SerializeField] private float baseMoveSpeed = 10f;
    private Vector2 movementInput;
    private float moveSpeed;
    private bool isHoldingDownMove;
    // Wall & Ceiling running

    // Rotation
    [SerializeField] private float rotationSmooth;
    private Quaternion orientation;
    // Skill
    private Vector3 scale;
    [Space]

    [Header("Boost")]
    [SerializeField] private float boostSpeed;
    public UnityEvent<bool> BoostUpdate;
    [Space]

    [Header("Jump")]
    [SerializeField] private float jumpDecceleration         = 1f;
    [SerializeField] private float startingJumpForce = 10f;
    [SerializeField] private float maxJumpForce      = 10f;
    private float                  currentJumpForce  = 0f;
    private bool                   isJumping;
    [Space]

    [Header("Gravity")]
    [SerializeField] private float gravity;
    [Space]

    [Header("Level")]
    [SerializeField] private Transform levelStart;
    [SerializeField] private float     startDelay;
    private bool isInLaunchingSequence = true;
    private Prompt currentPrompt;
    [Space]

    [Header("Groundcheck")]
    private bool isGrounded;
    private bool isWallRunning;
    private bool isUpsideDown;
    private bool isFallForgiven;
    private bool canJump;
    private float groundAngle;
    [SerializeField] private float groundedDistance;
    [SerializeField] private float forgiveFallDistance;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    public  UnityEvent   RespawnEvent;
    private SoundManager soundManager;

    private Vector2 appliedGravity;
    private Vector2 appliedMovement;
    private Vector2 appliedJump;
    private Vector2 currentJumpDir;

    RaycastHit2D groundInfo;

    private void OnValidate() { scale = transform.localScale; }

    public void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!playerBoost) playerBoost = GetComponent<PlayerBoost>();

        playerBoost.OnGaugeDepleted.AddListener(EndBoost);

        isInControl = false;
        isInLaunchingSequence = true;
        //TriggerRunAnimation();

        //transform.position = levelStart.position;
        soundManager = SoundManager.instance;

        moveSpeed = baseMoveSpeed;
        //rb.gravityScale = gravity;
        rb.gravityScale = 0;

        DEBUG1.color = Color.red;
        DEBUG2.color = Color.green;
        DEBUG3.color = Color.blue;
        DEBUG3.text  = "";
    }

    private void FixedUpdate()
    {
        GroundCheck();
        RotationCheck();
        GravityCheck();
        MoveCheck();
        JumpCheck();

        ApplyMovement();

        DEBUG1.SetText($"isJumping: {isJumping}");
        DEBUG2.SetText($"canJump: {canJump}");
        DEBUG3.SetText($"appliedJump: {appliedJump}");

        CheckAndFaceDirection();
    }

    private void GroundCheck()
    {
        groundInfo     = Physics2D.Raycast(transform.position, groundCheck.position - transform.position, groundedDistance, groundLayer);
        groundAngle    = Vector3.Angle(groundInfo.normal, Vector3.up);

        isGrounded     = groundInfo.collider != null;
        canJump        = isGrounded;

        isFallForgiven = Physics2D.Raycast(transform.position, groundCheck.position - transform.position, forgiveFallDistance, groundLayer);

        Debug.DrawLine(transform.position, (groundCheck.position), Color.black);
        Debug.DrawLine(transform.position, (groundCheck.position), Color.yellow);

        isWallRunning = (groundAngle == 90);
        isUpsideDown  = (groundAngle  > 90);
    }

    private void RotationCheck()
    {
        if (!isGrounded)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime * rotationSmooth);
            return;
        }

        orientation        = Quaternion.FromToRotation(transform.up, groundInfo.normal);

        if (orientation == transform.rotation) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, orientation * transform.rotation, Time.deltaTime * rotationSmooth);
    }
    private void GravityCheck()
    {
        if (isGrounded)
        {
            appliedGravity = Vector2.zero;
            return;
        }

        if (rb.linearVelocityY < 0 && !isWallRunning)
        {
            appliedGravity += new Vector2(0, -homemadeGravity * Time.deltaTime * 2);
            return;
        }

        appliedGravity += new Vector2(0, -homemadeGravity * Time.deltaTime);
    }

    private void MoveCheck()
    {
        if      (movementInput.x > 0) appliedMovement = transform.right  * moveSpeed;
        else if (movementInput.x < 0) appliedMovement = -transform.right * moveSpeed;

        else                          appliedMovement = Vector2.zero;
    }
    private void ApplyMovement()
    {
        Vector2 appliedForces = (appliedMovement /*+ appliedJump */+ appliedGravity);
        rb.AddForce(appliedForces);

        /*
        if      (movementInput.x > 0) rb.AddForce(transform.right  * moveSpeed, ForceMode2D.Force);
        else if (movementInput.x < 0) rb.AddForce(-transform.right * moveSpeed, ForceMode2D.Force);
        */
    }

    private void CheckAndFaceDirection()
    {
        if (!isHoldingDownMove) return;

        if (isUpsideDown)
        {
            if (rb.linearVelocity.x < 0) FaceRightOrLeft(true);
            else                         FaceRightOrLeft(false); 

            return;
        }

        if (isWallRunning)
        {
            if (groundInfo.normal.x == 1) // Left wall
            {
                if (rb.linearVelocity.y > 0) FaceRightOrLeft(false);
                else                         FaceRightOrLeft(true);

                return;
            }

            if (rb.linearVelocity.y > 0) FaceRightOrLeft(true);
            else                         FaceRightOrLeft(false);

            return;
        }

        if (rb.linearVelocity.x < 0) FaceRightOrLeft(false);
        else                         FaceRightOrLeft(true);
    }

    private void FaceRightOrLeft(bool faceRight)
    {
        if (faceRight)
        {
            transform.localScale = scale;
            return;
        }

        transform.localScale = new Vector3(-scale.x, scale.y, scale.z);
    }

    #region InputActions
    public void Move(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isHoldingDownMove = true;

            movementInput = context.ReadValue<Vector2>();
        }

        if (context.canceled)
        {
            isHoldingDownMove = false;

            movementInput     = Vector2.zero;

            if (!isUpsideDown && !isWallRunning) return;
            rb.gravityScale = gravity;
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (!canJump || !isGrounded) return;
            soundManager.PlayJumpSFX();
            currentJumpDir   = groundInfo.normal;

            isJumping = true;
            canJump   = false;

            currentJumpForce = startingJumpForce;
            //rb.AddForce(jumpSpeed * groundInfo.normal, ForceMode2D.Impulse);
        }

        if (context.canceled)
        {
            EndJump();

            return;
        }

        //appliedJump = startingJumpForce * currentJumpDir; //find a way to account for horizontal movement


        /*
        // Reached max jump:
        if (currentJumpForce >= maxJumpForce)
        {
            DEBUG1.SetText($"Reached max jump");
            ResetJump();
            return;
        }
        */
    }

    private void JumpCheck() //to coroutine
    {
        //if (!isJumping || currentJumpForce >= maxJumpForce) return;

        if (!isJumping) return;

        if (currentJumpForce >= maxJumpForce)
        {
            EndJump();
            return;
        }

        if (rb.linearVelocityY < 0) currentJumpForce  -= jumpDecceleration; //deccelerate if rising

        appliedJump        = currentJumpForce * currentJumpDir;
        rb.linearVelocity += appliedJump;
    }

    private void EndJump()
    {
        isJumping = false;

        appliedJump = Vector2.zero;
        canJump     = isGrounded;

        currentJumpForce = 0;
    }

    public void Boost(InputAction.CallbackContext context)
    {
        /*
        if (isInLaunchingSequence && currentPrompt)
        {
            currentPrompt.ReceiveInput(LaunchSequence.LaunchInputs.Shift);
        }

        if (!isInControl) return;
        */

        if (context.performed)
        {
            //if (isInLaunchingSequence) return;
            //if (hurtFlag) return;
            //if (!playerBoost.CanBoost()) return;
            //DoBoost();
            moveSpeed = boostSpeed;
            return;
        }

        if (context.canceled)
        {
            //if (isInLaunchingSequence) return;
            //EndBoost();
            moveSpeed = baseMoveSpeed;
        }
    }
    public void DoBoost()
    {
        moveSpeed = boostSpeed;

        //BoostUpdate.Invoke(true);
        //playerBoost.StartBoost();

        //SetHorizontalMovement(isRight);
    }

    public void EndBoost()
    {
        moveSpeed = baseMoveSpeed;
        //SetLaunchUp();

        //BoostUpdate.Invoke(false);

        //if (!isHoldingDownDirection) StopHorizontalMovement();
        //else TriggerIdleAnimation();
    }

    internal void SetCurrentPrompt(Prompt prompt)
    {
        throw new NotImplementedException();
    }
    #endregion
}
