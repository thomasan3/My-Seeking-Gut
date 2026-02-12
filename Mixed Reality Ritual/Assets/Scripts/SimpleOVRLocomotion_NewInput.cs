using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SimpleOVRLocomotion_NewInput : MonoBehaviour
{
    [Header("References")]
    public Transform rigRoot;          // [BuildingBlock] Camera Rig
    public Transform head;             // CenterEyeAnchor
    public CharacterController cc;     // Unity CharacterController

    [Header("Speeds")]
    public float moveSpeed = 2.0f;     // meters/sec
    public float turnSpeed = 60f;      // deg/sec (keyboard turn only)
    public float gravity = -9.81f;

    [Header("Keyboard Fallback (New Input System)")]
    public bool enableKeyboard = true;

    private float _yVel;

    void Reset()
    {
        rigRoot = transform;
        if (Camera.main) head = Camera.main.transform;
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (rigRoot == null || head == null || cc == null) return;

        // -------- 1) Thumbstick input (works in headset + Link) --------
        Vector2 moveInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        // -------- 2) Keyboard fallback using NEW Input System --------
#if ENABLE_INPUT_SYSTEM
        if (enableKeyboard && Keyboard.current != null)
        {
            float x = 0f, y = 0f;

            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.wKey.isPressed) y += 1f;
            if (Keyboard.current.sKey.isPressed) y -= 1f;

            Vector2 kb = new Vector2(x, y);
            if (kb.sqrMagnitude > 1f) kb.Normalize();

            // add keyboard to stick
            moveInput += kb;

            // Optional turning with Q/E
            float turn = 0f;
            if (Keyboard.current.qKey.isPressed) turn -= 1f;
            if (Keyboard.current.eKey.isPressed) turn += 1f;

            if (Mathf.Abs(turn) > 0.001f)
                rigRoot.Rotate(Vector3.up, turn * turnSpeed * Time.deltaTime, Space.World);
        }
#endif

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        // -------- 3) Move relative to head forward (flattened) --------
        Vector3 fwd = head.forward; fwd.y = 0; fwd.Normalize();
        Vector3 right = head.right; right.y = 0; right.Normalize();

        Vector3 planar = (fwd * moveInput.y + right * moveInput.x) * moveSpeed;

        // -------- 4) Gravity + move --------
        if (cc.isGrounded && _yVel < 0f) _yVel = -1f;
        _yVel += gravity * Time.deltaTime;

        Vector3 velocity = planar;
        velocity.y = _yVel;

        cc.Move(velocity * Time.deltaTime);
    }
}
