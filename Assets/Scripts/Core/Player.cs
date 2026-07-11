using EasyButtons;
using System.Linq;
using UnityEngine;
using Zodiac.Util;
using Zodiac.UI;
using UnityEngine.InputSystem;
using System;

namespace Zodiac.Core
{
    [RequireComponent(typeof(CharacterController))]
    public class Player : MonoBehaviour, InputSystem_Actions.IPlayerActions
    {

        [Header("References")]
        [SerializeField] private Transform head;
        public GunController gun;
        [SerializeField] private MenuLayer pauseMenu;

        [Header("Constants")]
        [SerializeField]
        private float
            walkSpeed = 4;

        [SerializeField]
        private float
            runMult = 1.5f,
            jumpForce = 2,
            lookSpeed = 1,
            maxCoyoteTime = 0.1f;
        [SerializeField, Range(-100, 0)]
            private float gravity = -9.8f;

        private CharacterController characterController;

        private float coyoteTime;
        private float yVelocity;

        [HideInInspector]
        public InputSystem_Actions.PlayerActions Input { get; private set; }

        void Awake()
        {
            Input = new InputSystem_Actions().Player;
        }

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            pauseMenu.onClose.AddListener(UnPause); // replace with separate script
            UnPause();
        }

        void OnEnable()
        {
            Input.AddCallbacks(this);
        }

        void OnDisable()
        {
            Input.RemoveCallbacks(this);
        }

        public void OnPause(InputAction.CallbackContext ctx){
            Input.Disable();
            pauseMenu.Open();
        }

        private void Update()
        {
            Vector3 movement = Vector3.zero;

            // coyote time
            if (characterController.isGrounded)
            {
                coyoteTime = 0;
                yVelocity = Mathf.Clamp(yVelocity, gravity, float.MaxValue);
            }
            else coyoteTime += Time.deltaTime;

            // gravity
            yVelocity += gravity * Time.deltaTime;

            movement += yVelocity * Time.deltaTime * Vector3.up;

            Vector3 walk = ((Vector3)Input.Move.ReadValue<Vector2>()).xzy();
            walk = Vector3.ClampMagnitude(walk, 1);

            //Vector3 move = transform.TransformDirection(move);
            walk = transform.forward * walk.z + transform.right * walk.x;

            //sprinting &= Vector3.Dot(transform.forward, inputWorld) > 0.5f;
            movement += (Input.Sprint.IsPressed() ? runMult : 1) * Time.deltaTime * walkSpeed * walk;

            characterController.Move(movement);
        }

        public void UnPause()
        {
            Input.Enable();
        }

        public void OnLook(InputAction.CallbackContext ctx)
        {
            var raw = ctx.ReadValue<Vector2>() * lookSpeed;
            transform.Rotate(0, raw.x, 0);
            //Past me was fucking cooking wtf
            var pitch = head.localEulerAngles.x - raw.y;
            if (pitch is > 90 and < 270) pitch = pitch < 180 ? 90 : 270;

            head.localEulerAngles = new Vector3(pitch, 0, 0);
        }


        public void OnJump(InputAction.CallbackContext context)
        {
            if (coyoteTime > maxCoyoteTime) return;

            // kinematic equation
            yVelocity = Mathf.Sqrt(jumpForce * 2 * -gravity);
        }

        public void OnInteract(InputAction.CallbackContext context) { } //TODO:
        public void OnCrouch(InputAction.CallbackContext context) { } //TODO:

        public void OnMove(InputAction.CallbackContext context) { } //no-op
        public void OnSprint(InputAction.CallbackContext context) { } //no-op
        public void OnShoot(InputAction.CallbackContext context) => CursorHelper.IsCursorLocked = true;
        public void OnAim(InputAction.CallbackContext context) => CursorHelper.IsCursorLocked = true;
    }
}