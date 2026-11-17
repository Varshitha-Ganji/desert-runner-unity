using UnityEngine;

/// <summary>
/// CharacterController-based runner: automatic forward movement, three smooth lanes,
/// jump + double jump (with coyote time and input buffering), gravity and fall detection.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Running")]
    public float startSpeed = 10f;
    public float maxSpeed = 14.5f;
    public float acceleration = 0.08f;       // m/s gained per second

    [Header("Lanes")]
    public float laneWidth = LevelBuilder.LaneWidth;
    public float laneSmoothTime = 0.07f;

    [Header("Jumping")]
    public float jumpHeight = 2.2f;
    public float gravity = -32f;
    public int maxJumps = 2;                 // 2 = double jump
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.14f;

    [Header("Death")]
    public float fallY = -3.6f;

    CharacterController cc;
    DesertFactory.ExplorerRig rig;

    float speed;
    float verticalVelocity;
    float laneVelocity;
    float targetX;
    int lane;
    int jumpsLeft;
    float coyote;
    float jumpBuffer;
    float animPhase;
    bool grounded;
    bool alive = true;
    bool finished;
    float lastX;

    public float Speed => speed;
    public bool Grounded => grounded;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cc.height = 1.9f;
        cc.radius = 0.38f;
        cc.center = new Vector3(0f, 0.95f, 0f);
        cc.stepOffset = 0.3f;
        cc.slopeLimit = 45f;
        cc.skinWidth = 0.03f;
        cc.minMoveDistance = 0f;
        rig = DesertFactory.BuildExplorer(transform);
        speed = startSpeed;
    }

    public void ResetTo(Vector3 position)
    {
        cc.enabled = false;
        transform.position = position;
        transform.rotation = Quaternion.identity;
        cc.enabled = true;

        speed = startSpeed;
        verticalVelocity = 0f;
        laneVelocity = 0f;
        lane = 0;
        targetX = 0f;
        lastX = position.x;
        jumpsLeft = maxJumps;
        coyote = 0f;
        jumpBuffer = 0f;
        animPhase = 0f;
        grounded = false;
        alive = true;
        finished = false;
        rig.root.localPosition = Vector3.zero;
        rig.root.localRotation = Quaternion.identity;
    }

    public void OnDeath() { alive = false; }
    public void OnWin() { alive = false; finished = true; }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || !alive) return;
        if (gm.StateFrame == Time.frameCount) return;   // don't treat the start/resume key press as a jump

        float dt = Time.deltaTime;

        // ---- Lane switching (A/D, arrows) ----
        if (GameInput.LeftPressed()) lane = Mathf.Max(-1, lane - 1);
        if (GameInput.RightPressed()) lane = Mathf.Min(1, lane + 1);
        targetX = lane * laneWidth;

        // ---- Ground detection (result of the previous Move) ----
        grounded = cc.isGrounded;
        jumpBuffer -= dt;
        if (GameInput.JumpPressed()) jumpBuffer = jumpBufferTime;

        if (grounded)
        {
            coyote = coyoteTime;
            jumpsLeft = maxJumps;
            if (verticalVelocity < 0f) verticalVelocity = -2f;
        }
        else
        {
            coyote -= dt;
            // Walked off an edge without jumping: the first mid-air jump is the "second" jump.
            if (coyote <= 0f) jumpsLeft = Mathf.Min(jumpsLeft, maxJumps - 1);
        }

        // ---- Jump / double jump ----
        if (jumpBuffer > 0f)
        {
            if (coyote > 0f)
            {
                verticalVelocity = JumpVelocity(1f);
                jumpsLeft = maxJumps - 1;
                coyote = 0f;
                jumpBuffer = 0f;
                Sfx.Jump();
            }
            else if (jumpsLeft > 0)
            {
                verticalVelocity = JumpVelocity(0.92f);
                jumpsLeft--;
                jumpBuffer = 0f;
                Sfx.DoubleJump();
            }
        }

        // ---- Gravity ----
        verticalVelocity = Mathf.Max(verticalVelocity + gravity * dt, -38f);

        // ---- Automatic forward run, gradually faster ----
        speed = Mathf.Min(maxSpeed, speed + acceleration * dt);

        // ---- Move ----
        float x = transform.position.x;
        lastX = x;
        float newX = Mathf.SmoothDamp(x, targetX, ref laneVelocity, laneSmoothTime);
        var delta = new Vector3(newX - x, verticalVelocity * dt, speed * dt);
        cc.Move(delta);

        if ((cc.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;

        // ---- Fall detection ----
        if (transform.position.y < fallY)
            gm.Die("You fell off the platform!");
    }

    float JumpVelocity(float scale)
    {
        return Mathf.Sqrt(-2f * gravity * jumpHeight) * scale;
    }

    // Running face-first into the side of a platform ends the run.
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        if (hit.normal.y < 0.2f && hit.normal.z < -0.5f && hit.moveDirection.z > 0.5f)
            gm.Die("You crashed into a wall!");
    }

    // ------------------------------------------------------------------ visuals
    void LateUpdate()
    {
        if (rig == null) return;
        var gm = GameManager.Instance;
        float dt = Time.unscaledDeltaTime;
        var state = gm != null ? gm.State : GameState.Menu;

        if (state == GameState.Playing && alive)
        {
            float lean = (targetX - transform.position.x) * -5f;
            rig.root.localRotation = Quaternion.Slerp(rig.root.localRotation, Quaternion.Euler(0f, 0f, lean), 12f * dt);

            if (grounded)
            {
                animPhase += Time.deltaTime * speed * 0.95f;
                float s = Mathf.Sin(animPhase);
                rig.legL.localRotation = Quaternion.Euler(s * 55f, 0f, 0f);
                rig.legR.localRotation = Quaternion.Euler(-s * 55f, 0f, 0f);
                rig.armL.localRotation = Quaternion.Euler(-s * 50f, 0f, 0f);
                rig.armR.localRotation = Quaternion.Euler(s * 50f, 0f, 0f);
                rig.root.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Cos(animPhase)) * 0.06f, 0f);
            }
            else
            {
                rig.legL.localRotation = Quaternion.Euler(-40f, 0f, 0f);
                rig.legR.localRotation = Quaternion.Euler(30f, 0f, 0f);
                rig.armL.localRotation = Quaternion.Euler(-150f, 0f, 10f);
                rig.armR.localRotation = Quaternion.Euler(-150f, 0f, -10f);
                rig.root.localPosition = Vector3.zero;
            }
        }
        else if (state == GameState.GameOver)
        {
            rig.root.localRotation = Quaternion.Slerp(rig.root.localRotation, Quaternion.Euler(80f, 0f, 0f), 6f * dt);
            rig.armL.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            rig.armR.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        }
        else if (state == GameState.Victory && finished)
        {
            rig.root.localRotation = Quaternion.identity;
            rig.root.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.5f, 0f);
            rig.armL.localRotation = Quaternion.Euler(-165f, 0f, 15f);
            rig.armR.localRotation = Quaternion.Euler(-165f, 0f, -15f);
            rig.legL.localRotation = Quaternion.identity;
            rig.legR.localRotation = Quaternion.identity;
        }
    }
}
