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
    [SerializeField] private float jumpForce = 10f;
    float groundAngle;
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
    private bool isFallForgiven;
    private bool canJump;
    [SerializeField] private float groundedDistance;
    [SerializeField] private float forgiveFallDistance;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;

    public  UnityEvent RespawnEvent;
    private SoundManager soundManager;

    RaycastHit2D groundInfo;

    private bool IsWallRunning()
    {
        return groundAngle == 90;
    }

    private bool IsRunningUpsideDown()
    {
        return groundAngle >= 135 || groundAngle <= -135; //could be 0°? idk, to test
    }

    private void OnValidate()
    {
        scale = transform.localScale;
    }

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
        rb.gravityScale = gravity;

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

        ApplyMovement();

        DEBUG1.SetText($"Ground Normal: {groundInfo.normal}");
        DEBUG2.SetText($"Wall run: {IsWallRunning()}");
        DEBUG3.SetText($"Upside down: {IsRunningUpsideDown()}");

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
        if ((!isFallForgiven && !isGrounded) || movementInput == Vector2.zero)
        {
            rb.gravityScale = gravity;
            return;
        }

        if (IsWallRunning())
        {
            rb.gravityScale = 0;
            return;
        }

        if (IsRunningUpsideDown())
        {
            rb.gravityScale = -gravity;
            return;
        }

        rb.gravityScale = gravity;
    }

    private void ApplyMovement()
    {
        if (movementInput.x > 0) rb.AddForce(transform.right * moveSpeed, ForceMode2D.Force);
        else if (movementInput.x < 0) rb.AddForce(-transform.right * moveSpeed, ForceMode2D.Force);
    }

    private void CheckAndFaceDirection()
    {
        if (!isHoldingDownMove) return;

        if (IsRunningUpsideDown())
        {
            if (rb.linearVelocity.x < 0) FaceRightOrLeft(true);
            else                         FaceRightOrLeft(false); 

            return;
        }

        if (IsWallRunning())
        {
            // If left wall:
            if (groundInfo.normal.x == 1) 
            {
                if (rb.linearVelocity.y > 0) FaceRightOrLeft(false);
                else                         FaceRightOrLeft(true);

                return;
            }

            // If right wall:
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

            movementInput = Vector2.zero;

            if (!IsRunningUpsideDown() && !IsWallRunning()) return;
            rb.gravityScale = gravity;
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (!canJump || !isGrounded) return;

            soundManager.PlayJumpSFX();

            rb.AddForce(jumpForce * groundInfo.normal, ForceMode2D.Impulse);

            canJump = false;
        }
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
