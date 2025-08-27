using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class TPSController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float walkSpeed = 3.8f;
    [SerializeField] float sprintSpeed = 6.5f;
    [SerializeField] float rotationSmooth = 12f;

    [Header("Jump & Gravity")]
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float coyoteTime = 0.1f; // 段差の端での猶予
    float lastGroundedTime;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;    // 足元
    [SerializeField] float groundCheckRadius = 0.25f;
    [SerializeField] LayerMask groundMask = ~0; // 何も指定しない場合は全部

    [Header("Slope")]
    [SerializeField] float slopeLimit = 55f;       // CCの設定と合わせる
    [SerializeField] float slideAccel = 6f;
    [SerializeField] float maxSlideSpeed = 10f;

    [Header("References")]
    [SerializeField] Transform cameraTransform; // Main Cameraをアサイン

    CharacterController controller;
    Animator animator; // 任意（人型に差し替えたとき）

    Vector2 moveInput;
    bool sprintHeld;
    bool jumpPressed;

    Vector3 velocity; // 垂直方向の速度を主に管理
    RaycastHit groundHit;
    bool isGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        CheckGround();
        HandleMovement();
        HandleGravityAndJump();
        UpdateAnimator();
    }

    void CheckGround()
    {
        Vector3 origin = groundCheck != null ? groundCheck.position : (transform.position + Vector3.up * 0.1f);
        float distance = 0.3f;

        isGrounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
                                        out groundHit, distance, groundMask, QueryTriggerInteraction.Ignore)
                     || controller.isGrounded;

        if (isGrounded) lastGroundedTime = Time.time;
    }

    void HandleMovement()
    {
        // カメラ基準の移動方向
        Vector3 camForward = cameraTransform.forward; camForward.y = 0; camForward.Normalize();
        Vector3 camRight = cameraTransform.right; camRight.y = 0; camRight.Normalize();

        Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y);
        Vector3 moveDir = (camForward * inputDir.z + camRight * inputDir.x);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        // 地面に沿わせる
        if (isGrounded && groundHit.collider != null)
        {
            moveDir = Vector3.ProjectOnPlane(moveDir, groundHit.normal).normalized;
        }

        float targetSpeed = (sprintHeld ? sprintSpeed : walkSpeed) * inputDir.magnitude;
        Vector3 horizVel = moveDir * targetSpeed;

        // 回転（移動方向へ向く）
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
        }

        // 斜面が急すぎる場合はスライド
        if (isGrounded && groundHit.collider != null)
        {
            float angle = Vector3.Angle(groundHit.normal, Vector3.up);
            float limit = Mathf.Min(slopeLimit, controller.slopeLimit);
            if (angle > limit + 0.5f)
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;
                horizVel += slideDir * Mathf.Min(maxSlideSpeed, slideAccel * (angle - limit));
            }
        }

        Vector3 finalMove = horizVel + new Vector3(0f, velocity.y, 0f);
        controller.Move(finalMove * Time.deltaTime);
    }

    void HandleGravityAndJump()
    {
        // 着地中は軽く張り付ける
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;

        bool canJump = (Time.time - lastGroundedTime) <= coyoteTime;

        if (jumpPressed && canJump)
        {
            // v = sqrt(h * -2g)
            float jumpVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
            velocity.y = jumpVel;
            jumpPressed = false;
        }

        // 重力
        velocity.y += gravity * Time.deltaTime;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        Vector3 v = controller.velocity; v.y = 0;
        float speed = v.magnitude;
        animator.SetFloat("Speed", speed);  // ブレンドツリー用
        animator.SetBool("Grounded", isGrounded);
        animator.SetFloat("VerticalVelocity", velocity.y);
    }

#if ENABLE_INPUT_SYSTEM
    // PlayerInput (Behavior: Send Messages) から呼ばれる
    public void OnMove(InputValue value)  { moveInput = value.Get<Vector2>(); }
    public void OnLook(InputValue value)  { /* マウス視点はCinemachineが処理。必要ならここで回転 */ }
    public void OnSprint(InputValue value){ sprintHeld = value.isPressed; }
    public void OnJump(InputValue value)  { if (value.isPressed) jumpPressed = true; }
#else
    // 古いInputのフォールバック（必要なら）
    void LateUpdate()
    {
        if (!Application.isPlaying) return;
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        sprintHeld = Input.GetKey(KeyCode.LeftShift);
        if (Input.GetKeyDown(KeyCode.Space)) jumpPressed = true;
    }
#endif
}
