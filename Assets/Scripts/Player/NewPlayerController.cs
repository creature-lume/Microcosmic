using System;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class NewPlayerController : MonoBehaviour
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
    [SerializeField] private float     forgiveFallDistance;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    private RaycastHit2D groundInfo;
    private float groundAngle;
    private bool isGrounded;
    private bool isWallRunning;
    private bool isUpsideDown;
    private bool isFallForgiven;
    [Space]

    // ------------------------------------------------------------------
    [Header("Rotation")]
    [SerializeField] private float rotationSmooth;
    private Quaternion orientation;
    private bool isFacingRight = true;
    [Space]

    // ------------------------------------------------------------------
    [Header("Gravity")]
    [SerializeField] private float gravity             = 98f;
    [SerializeField] private float fallGravityModifier = 2;
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

    // ------------------------------------------------------------------
    [Header("Boost")]
    [SerializeField] private float boostSpeed;
    public UnityEvent<bool> BoostUpdate;
    private bool isBoosting => playerBoost.IsBoosting();

    [Space]

    // ------------------------------------------------------------------
    [Header("Jump")]
    [SerializeField] private float jumpForce   = 7f;
    [SerializeField] private float maxJumpTime = 1f;
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

    public void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!playerBoost) playerBoost = GetComponent<PlayerBoost>();
        if (!playerBoost)
        {
            Debug.LogError($"Player boost is null.");
            Debug.DebugBreak();
        }

        playerBoost.OnGaugeDepleted.AddListener(EndBoost);

        //isInControl = false;
        //isInLaunchingSequence = true;
        //TriggerRunAnimation();

        //transform.position = levelStart.position;
        soundManager = SoundManager.instance;

        moveSpeed = baseMoveSpeed;
        //rb.gravityScale = gravity;
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
        MoveCheck();
        CameraCheck();

        ApplyMovement();

        DEBUG1.SetText($"isHoldingDownMove: {isHoldingDownMove}");
        DEBUG2.SetText($"isFacingRight: {isFacingRight}");
        DEBUG3.SetText($"isBoosting: {isBoosting}");

        CheckAndFaceDirection();
    }

    private void GroundCheck()
    {
        groundInfo  = Physics2D.Raycast(transform.position, groundCheck.position - transform.position, groundedDistance, groundLayer);
        groundAngle = Vector3.Angle(groundInfo.normal, Vector3.up);

        isGrounded  = groundInfo.collider != null;
        canJump     = isGrounded;
        
        isFallForgiven = Physics2D.Raycast(transform.position, groundCheck.position - transform.position, forgiveFallDistance, groundLayer);

        Debug.DrawLine(transform.position, (groundCheck.position), Color.black);
        Debug.DrawLine(transform.position, (groundCheck.position), Color.yellow);

        isWallRunning = (groundAngle == 90);
        isUpsideDown  = (groundAngle  > 90);

        if (isGrounded && !canJump && isJumping) EndJump();
    }
    private void RotationCheck()
    {
        if (!isGrounded)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime * rotationSmooth);
            return;
        }

        orientation        = Quaternion.FromToRotation(transform.up, groundInfo.normal);
        transform.rotation = Quaternion.Slerp(transform.rotation, orientation * transform.rotation, Time.deltaTime * rotationSmooth);
    }
    private void GravityCheck()
    {
        //if (isWallRunning)
        //{
        //    appliedGravity = -gravity * groundInfo.normal;
        //    return;
        //}

        if (isBoosting && isGrounded /*&& isFallForgiven*/)
        {
            appliedGravity = (appliedMovement.magnitude * 0.5f) * -groundInfo.normal;
            return;
        }

        if (isGrounded)
        {
            appliedGravity = (-gravity * 0.1f) * groundInfo.normal;
            return;
        }

        if (rb.linearVelocityY < 0 && !isWallRunning)
        {
            appliedGravity += new Vector2(0, -gravity * Time.deltaTime * fallGravityModifier);
            return;
        }

        appliedGravity += new Vector2(0, -gravity * Time.deltaTime);
    }

    #region Movement
    public void Move(InputAction.CallbackContext context)
    {
        if (!isInControl) return;

        if (context.started)
        {
            if ((isWallRunning || isUpsideDown) && isHoldingDownMove && !isJumping) return;

            movementInput     = context.ReadValue<Vector2>();
            isHoldingDownMove = true;
        }

        if (context.canceled)
        {
            isHoldingDownMove = false;
            if (isBoosting) return;

            movementInput     = Vector2.zero;

            //if (!isUpsideDown && !isWallRunning) return;
            //appliedGravity += new Vector2(0, -gravity * Time.deltaTime * fallGravityModifier);
        }
    }
    private void MoveCheck()
    {
        if      (movementInput.x > 0) appliedMovement = transform.right  * moveSpeed;
        else if (movementInput.x < 0) appliedMovement = -transform.right * moveSpeed;

        else appliedMovement = Vector2.zero;
    }
    private void ApplyMovement()
    {
        Vector2 appliedForces = (appliedMovement + appliedGravity);
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

    private void CameraCheck()
    {
        c_PositionComposer.Lookahead.IgnoreY = !isWallRunning;
    }

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

        if (isWallRunning || isUpsideDown)
        {
            appliedJump = jumpForce * groundInfo.normal;
            //appliedGravity = appliedJump;
        }
        else appliedJump = jumpForce * groundInfo.normal;

        yield return new WaitForSeconds(maxJumpTime);
        
        if (isJumping) EndJump();
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

            moveSpeed = boostSpeed;
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
