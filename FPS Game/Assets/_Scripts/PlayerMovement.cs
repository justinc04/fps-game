using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviourPun
{ 
    [Header("Movement")]
    public float runSpeed;
    public float walkSpeed;
    public float groundAcceleration;
    public float airAcceleration;
    [SerializeField] float landingSpeedMultiplier;
    public float landingDuration;
    [HideInInspector] public float speed;
    [HideInInspector] public Vector3 direction;
    [HideInInspector] public bool speedControlled;
    [HideInInspector] public bool directionControlled;
    [HideInInspector] public float abilitySpeedAffector = 1;
    [HideInInspector] public float weaponSpeedAffector = 1;
    private float landingSpeedAffector = 1;
    [HideInInspector] public bool silentSteps;
    public float mouseSensitivityMultiplier = 1;

    [Header("Gravity")]
    [SerializeField] float gravityScale;
    [SerializeField] float fallGravityScale;
    [HideInInspector] public bool gravityControlled;
    private const float gravityConstant = -9.81f;
    private float gravity;

    [Header("Jumping")]
    [HideInInspector] public Vector3 velocity;
    public float jumpHeight;
    [SerializeField] float groundDistance;
    [SerializeField] float hangTime;
    [SerializeField] float jumpBuffer;
    [HideInInspector] public bool jumpControlled;
    [HideInInspector] public bool isGrounded;
    private float hangTimeCounter;
    private float jumpBufferCounter;
    private bool isJumping;
    private bool hasLanded = true;

    [Header("Components")]
    [SerializeField] GameObject cameraHolder;
    [SerializeField] LayerMask groundMask;
    [SerializeField] PlayerAudio playerAudio;
    public Transform groundCheck;
    private PhotonView pv;
    private CharacterController controller;

    private float verticalLookRot;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        gravity = gravityConstant * gravityScale;
    }

    private void Update()
    {
        if (!pv.IsMine)
        {
            return;
        }

        Look();
        Move();
        CheckGrounded();
        CheckJumpAllowed();
        Jump();
        ApplyGravity();
    }

    void Look()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Settings.Instance.mouseSensitivity * mouseSensitivityMultiplier;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Settings.Instance.mouseSensitivity * mouseSensitivityMultiplier;

        transform.Rotate(Vector3.up * mouseX);

        verticalLookRot += mouseY;
        verticalLookRot = Mathf.Clamp(verticalLookRot, -90f, 90f);

        cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRot;
    }

    void Move()
    {
        float horizontal = PlayerInput.Instance.GetAxis().x;
        float vertical = PlayerInput.Instance.GetAxis().y;
        float acceleration = hangTimeCounter > 0f ? groundAcceleration : airAcceleration;

        if (!directionControlled)
        {
            Vector3 targetDirection = (transform.right * horizontal + transform.forward * vertical).normalized;
            direction = Vector3.Lerp(direction, targetDirection, acceleration * Time.deltaTime);
        }

        if (!speedControlled)
        {
            float targetSpeed = (PlayerInput.Instance.GetWalkButton() ? walkSpeed : runSpeed) * weaponSpeedAffector * landingSpeedAffector * abilitySpeedAffector;
            speed = Mathf.Lerp(speed, targetSpeed, acceleration * Time.deltaTime);
        }

        controller.Move(direction * speed * Time.deltaTime);

        if (!silentSteps && direction != Vector3.zero && controller.velocity.magnitude > walkSpeed && !PlayerInput.Instance.GetWalkButton() && isGrounded)
        {
            if (!playerAudio.CheckSound("Footsteps"))
            {
                playerAudio.Play("Footsteps");
            }
        }
        else if (playerAudio.CheckSound("Footsteps"))
        {
            playerAudio.Stop("Footsteps");
        }
    }

    void CheckGrounded()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (!isGrounded)
        {
            hasLanded = false;
        }

        if (isGrounded && !hasLanded)
        {
            hasLanded = true;

            if (velocity.y < -7f)
            {
                StartCoroutine(Landed());
                playerAudio.Play("Jump Land");
            }
        }

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -5f;
        }
    }

    IEnumerator Landed()
    {
        landingSpeedAffector = landingSpeedMultiplier;
        yield return new WaitForSeconds(landingDuration);
        landingSpeedAffector = 1;
    }

    void CheckJumpAllowed()
    {
        if (isGrounded)
        {
            hangTimeCounter = hangTime;
        }
        else
        {
            hangTimeCounter -= Time.deltaTime;
        }

        if (PlayerInput.Instance.GetJumpButtonDown())
        {
            jumpBufferCounter = jumpBuffer;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    void Jump()
    {
        if (jumpControlled)
        {
            return;
        }

        if (!isJumping && jumpBufferCounter > 0f && hangTimeCounter > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
            StartCoroutine(JumpCooldown());

            playerAudio.Play("Jump");
        }

        if (velocity.y < 0)
        {
            gravity = gravityConstant * fallGravityScale;
        }
        else
        {
            gravity = gravityConstant * gravityScale;
        }

        if (PlayerInput.Instance.GetJumpButtonUp() && velocity.y > 0f)
        {
            velocity.y *= .8f;
            hangTimeCounter = 0f;
        }
    }

    IEnumerator JumpCooldown()
    {
        isJumping = true;
        yield return new WaitForSeconds(.4f);
        isJumping = false;
    }

    void ApplyGravity()
    {
        if (gravityControlled)
        {
            return;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
