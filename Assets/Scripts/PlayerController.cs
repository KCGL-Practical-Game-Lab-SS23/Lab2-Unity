//Original Code Author: Aedan Graves

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


    [RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider))]
    [AddComponentMenu("SUPER Character/SUPER Character Controller")]
    public class PlayerController : MonoBehaviour
    {
        public Vector3 RespawnPoint = Vector3.zero;

        #region Camera Settings
        [Header("Camera Settings")]
        public Camera playerCamera;
        public bool enableCameraControl = true;

        public float Sensitivity = 16;
        public float rotationWeight = 4;
        public float verticalRotationRange = 170.0f;

        //Third Person
        public bool rotateCharacterToCameraForward = false;
        public float maxCameraDistance = 8;
        public LayerMask cameraObstructionIgnore = -1;
        public float cameraZoomSensitivity = 5;
        public float bodyCatchupSpeed = 2.5f;
        public float inputResponseFiltering = 2.5f;

        //Both
        Vector2 MouseXY;
        Vector3 initialRot;
        //float internalEyeHeight;

        //Third Person
        float mouseScrollWheel, maxCameraDistInternal, currentCameraZ, cameraZRef;
        Vector3 headPos, headRot, currentCameraPos, cameraPosVelRef;
        Quaternion quatHeadRot;
        Ray cameraObstCheck;
        RaycastHit cameraObstResult;
        [Space(20)]
        #endregion

        #region Movement
        [Header("Movement Settings")]

        //
        //Public
        //
        public bool enableMovementControl = true;

        //Walking/Sprinting/Crouching
        [Range(1.0f, 650.0f)]
        public float walkingSpeed = 140;

        [Range(1.0f, 400.0f)] 
        public float decelerationSpeed = 240;

        public bool isIdle;
        public LayerMask whatIsGround = -1;

        //Slope affectors
        public float hardSlopeLimit = 70;
        public float slopeInfluenceOnSpeed = 1;
        public float maxStairRise = 0.25f;
        public float stepUpSpeed = 0.2f;

        //Jumping
        public bool canJump = true;
        public bool holdJump = false;
        public bool jumpEnhancements = true;
        public bool Jumped;

        public KeyCode jumpKey_L = KeyCode.Space;

        [Range(1.0f, 65.0f)] public float jumpPower = 20;
        [Range(0.0f, 1.0f)] public float airControlFactor = 1;
        public float decentMultiplier = 2.5f, tapJumpMultiplier = 2.1f;

        //Walking/Sprinting/Crouching
        public GroundInfo currentGroundInfo = new GroundInfo();
        float currentGroundSpeed;
        Vector3 InputDir;
        float HeadRotDirForInput;
        Vector2 MovInput;
        Vector2 MovInput_Smoothed;
        Vector2 _2DVelocity;
        //PhysicMaterial _ZeroFriction, _MaxFriction;
        CapsuleCollider capsule;
        Rigidbody p_Rigidbody;

        //Jumping
        bool jumpInput_Momentary, jumpInput_FrameOf;
        #endregion


        #region Footstep System
        [Header("Footstep System")]
        public bool enableFootstepSounds = true;
        [Range(0.0f, 1.0f)] public float stepTiming = 0.15f;

        AudioSource playerAudioSource;
        public List<AudioClip> currentClipSet = new List<AudioClip>();
        [Space(18)]
        #endregion

        #region Animation
        public Animator _3rdPersonCharacterAnimator;
        public string a_2DVelocity, a_Grounded, a_Idle, a_Sliding;
        public bool stickRendererToCapsuleBottom = true;
        #endregion


        void Start()
        {
            RespawnPoint = transform.position;
            #region Camera
            maxCameraDistInternal = maxCameraDistance;
            
            //Lock and hide mouse
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            initialRot = transform.localEulerAngles;

            RotateView(initialRot);
            InputDir = transform.forward;
            #endregion

            #region Movement
            p_Rigidbody = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            currentGroundSpeed = walkingSpeed;
            #endregion

            #region Footstep
            playerAudioSource = GetComponent<AudioSource>();
            #endregion

        }
        void Update()
        {

            #region Input
            //camera
            MouseXY.x = Input.GetAxis("Mouse Y");
            MouseXY.y = Input.GetAxis("Mouse X");
            mouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");
            //movement

            jumpInput_Momentary = Input.GetKey(jumpKey_L);
            jumpInput_FrameOf = Input.GetKeyDown(jumpKey_L);
            MovInput = Vector2.up * Input.GetAxisRaw("Vertical") + Vector2.right * Input.GetAxisRaw("Horizontal");
            #endregion

            #region Camera
            if (enableCameraControl)
            {
                //  UpdateCameraPosition_3rdPerson();
                maxCameraDistInternal = Mathf.Clamp(maxCameraDistInternal - (mouseScrollWheel * (cameraZoomSensitivity * 2)), capsule.radius * 2, maxCameraDistance);
            }
            #endregion

            #region Movement

            HeadRotDirForInput = Mathf.MoveTowardsAngle(HeadRotDirForInput, headRot.y, bodyCatchupSpeed * (1 + Time.deltaTime));
            MovInput_Smoothed = Vector2.MoveTowards(MovInput_Smoothed, MovInput, inputResponseFiltering * (1 + Time.deltaTime));

            InputDir = Quaternion.AngleAxis(HeadRotDirForInput, Vector3.up) * (Vector3.ClampMagnitude((Vector3.forward * MovInput_Smoothed.y + Vector3.right * MovInput_Smoothed.x), 1));

            if (canJump && (holdJump ? jumpInput_Momentary : jumpInput_FrameOf))
            {
                Jump(jumpPower);
            }
            #endregion

            #region Animation
            UpdateAnimationTriggers();
            #endregion
        }
        void FixedUpdate()
        {
            #region Movement

            GetGroundInfo();
            MovePlayer(InputDir, currentGroundSpeed);
            #endregion

            #region Camera
            RotateView(MouseXY, Sensitivity, rotationWeight);
            UpdateBodyRotation_3rdPerson();
            UpdateCameraPosition_3rdPerson();
            #endregion

        }
        private void OnTriggerEnter(Collider other)
        {
            //@TODO : Collectibles
        }

        #region Camera Functions
        void RotateView(Vector2 yawPitchInput, float inputSensitivity, float cameraWeight)
        {

            yawPitchInput.x *= -1;
            yawPitchInput.y *= 1;
            float maxDelta = Mathf.Min(5, (26 - cameraWeight)) * 360;

            headPos = transform.position;
            quatHeadRot = Quaternion.Euler(headRot);
            headRot = Vector3.SmoothDamp(headRot, headRot + ((Vector3)yawPitchInput * (inputSensitivity * 5)), ref cameraPosVelRef, (Mathf.Pow(cameraWeight, 2)) * Time.fixedDeltaTime, maxDelta, Time.fixedDeltaTime);
            headRot.y += headRot.y > 180 ? -360 : headRot.y < -180 ? 360 : 0;
            headRot.x += headRot.x > 180 ? -360 : headRot.x < -180 ? 360 : 0;
            headRot.x = Mathf.Clamp(headRot.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
        }

        public void RotateView(Vector3 AbsoluteEulerAngles)
        {
            headRot = AbsoluteEulerAngles;
            headRot.y += headRot.y > 180 ? -360 : headRot.y < -180 ? 360 : 0;
            headRot.x += headRot.x > 180 ? -360 : headRot.x < -180 ? 360 : 0;
            headRot.x = Mathf.Clamp(headRot.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
            quatHeadRot = Quaternion.Euler(headRot);
        }

        void UpdateCameraPosition_3rdPerson()
        {

            //Camera Obstacle Check
            cameraObstCheck = new Ray(headPos + (quatHeadRot * (Vector3.forward * capsule.radius)), quatHeadRot * -Vector3.forward);
            if (Physics.SphereCast(cameraObstCheck, 0.5f, out cameraObstResult, maxCameraDistInternal, cameraObstructionIgnore, QueryTriggerInteraction.Ignore))
            {
                currentCameraZ = -(Vector3.Distance(headPos, cameraObstResult.point) * 0.9f);

            }
            else
            {
                currentCameraZ = Mathf.SmoothDamp(currentCameraZ, -(maxCameraDistInternal * 0.85f), ref cameraZRef, Time.deltaTime, 10, Time.fixedDeltaTime);
            }

            currentCameraPos = headPos + (quatHeadRot * (Vector3.forward * currentCameraZ));
            playerCamera.transform.position = currentCameraPos;
            playerCamera.transform.rotation = quatHeadRot;
        }

        void UpdateBodyRotation_3rdPerson()
        {
            //if is moving, rotate capsule to match camera forward   //change button down to bool of isFiring or isTargeting
            if (!isIdle && currentGroundInfo.isInContactWithGround)
            {
                transform.rotation = (Quaternion.Euler(0, Mathf.MoveTowardsAngle(p_Rigidbody.rotation.eulerAngles.y, (Mathf.Atan2(InputDir.x, InputDir.z) * Mathf.Rad2Deg), 10), 0));
                //transform.rotation = Quaternion.Euler(0,Mathf.MoveTowardsAngle(transform.eulerAngles.y,(Mathf.Atan2(InputDir.x,InputDir.z)*Mathf.Rad2Deg),2.5f), 0);
            }
            else if (!currentGroundInfo.isInContactWithGround && rotateCharacterToCameraForward)
            {
                transform.localRotation = (Quaternion.Euler(Vector3.up * Mathf.MoveTowardsAngle(p_Rigidbody.rotation.eulerAngles.y, headRot.y, 10)));
            }
        }
        #endregion

        #region Movement Functions
        void MovePlayer(Vector3 Direction, float Speed)
        {
            // GroundInfo gI = GetGroundInfo();
            isIdle = Direction.normalized.magnitude <= 0;
            _2DVelocity = Vector2.right * p_Rigidbody.velocity.x + Vector2.up * p_Rigidbody.velocity.z;

            //Movement
            if ((currentGroundInfo.isInContactWithGround) && !Jumped)
            {
                //Deceleration
                if (Direction.magnitude == 0 && p_Rigidbody.velocity.normalized.magnitude > 0.1f)
                {
                    p_Rigidbody.AddForce(-new Vector3(p_Rigidbody.velocity.x, currentGroundInfo.isInContactWithGround ? p_Rigidbody.velocity.y - Physics.gravity.y : 0, p_Rigidbody.velocity.z) * (decelerationSpeed * Time.fixedDeltaTime), ForceMode.Force);
                }
                //normal speed
                else if ((currentGroundInfo.isInContactWithGround) && currentGroundInfo.groundAngle < hardSlopeLimit)
                {
                    p_Rigidbody.velocity = (Vector3.MoveTowards(p_Rigidbody.velocity, Vector3.ClampMagnitude(((Direction) * ((Speed) * Time.fixedDeltaTime)) + (Vector3.down), Speed / 50), 1));
                }
            }

            //Air Control
            else if (!currentGroundInfo.isInContactWithGround)
            {
                p_Rigidbody.AddForce(Direction * walkingSpeed * Time.fixedDeltaTime * airControlFactor * 5 * currentGroundInfo.groundAngleMultiplier_Inverse, ForceMode.Acceleration);
                p_Rigidbody.velocity = Vector3.ClampMagnitude((Vector3.right * p_Rigidbody.velocity.x + Vector3.forward * p_Rigidbody.velocity.z), (walkingSpeed / 50)) + (Vector3.up * p_Rigidbody.velocity.y);

                if (p_Rigidbody.velocity.y < 0 && p_Rigidbody.velocity.y > Physics.gravity.y * 1.5f)
                {
                    p_Rigidbody.velocity += Vector3.up * (Physics.gravity.y * (decentMultiplier) * Time.fixedDeltaTime);
                }
                else if (p_Rigidbody.velocity.y > 0 && !jumpInput_Momentary)
                {
                    p_Rigidbody.velocity += Vector3.up * (Physics.gravity.y * (tapJumpMultiplier - 1) * Time.fixedDeltaTime);
                }

            }


        }
        void Jump(float Force)
        {
            if ((currentGroundInfo.isInContactWithGround) &&
                (currentGroundInfo.groundAngle < hardSlopeLimit) &&
                !Jumped)
            {
                Jumped = true;
                p_Rigidbody.velocity = (Vector3.right * p_Rigidbody.velocity.x) + (Vector3.forward * p_Rigidbody.velocity.z);
                p_Rigidbody.AddForce(Vector3.up * (Force), ForceMode.Impulse);
            }
        }

        void GetGroundInfo()
        {
            //to Get if we're actually touching ground.
            //to act as a normal and point buffer.
            currentGroundInfo.isInContactWithGround = Physics.Raycast(transform.position, Vector3.down, out currentGroundInfo.groundFromRay, (capsule.height / 2) + 0.25f, whatIsGround);

            if (Jumped && (Physics.Raycast(transform.position, Vector3.down, (capsule.height / 2) + 0.1f, whatIsGround) || Physics.CheckSphere(transform.position - (Vector3.up * ((capsule.height / 2) - (capsule.radius - 0.05f))), capsule.radius, whatIsGround)))
            {
                Jumped = false;
            }
        }
        #endregion

        public void CallFootstepClip()
        {
            if (playerAudioSource)
            {
                if (currentClipSet != null && currentClipSet.Any())
                {
                    playerAudioSource.PlayOneShot(currentClipSet[Random.Range(0, currentClipSet.Count())]);
                    Debug.Log("Sound!");
                }
            }
        }

        void UpdateAnimationTriggers()
        {
            if (_3rdPersonCharacterAnimator)
            {
                //update animation parameters
                _3rdPersonCharacterAnimator.SetFloat(a_2DVelocity, _2DVelocity.magnitude);
                _3rdPersonCharacterAnimator.SetBool(a_Idle, isIdle);
                _3rdPersonCharacterAnimator.SetBool(a_Grounded, currentGroundInfo.isInContactWithGround);
            }
        }

    }

    #region Classes and Enums
    [System.Serializable]
    public class GroundInfo
    {
        public bool isInContactWithGround;
        public float groundAngleMultiplier_Inverse = 1;
        public float groundAngle;
        public Vector3 groundInfluenceDirection;
        internal RaycastHit groundFromRay;
        internal RaycastHit[] groundFromSweep;
    }
#endregion