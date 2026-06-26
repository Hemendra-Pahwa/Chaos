using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class FirstPersonController : MonoBehaviour
	{
		private static readonly int SpeedHash = Animator.StringToHash("Speed");
		private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");

		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 4.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 6.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		[Header("Presentation")]
		[Tooltip("Optional animator for the player model. Auto-found if left empty.")]
		public Animator CharacterAnimator;
		[Tooltip("Optional weapon holder transform. Auto-found on the main camera if left empty.")]
		public Transform WeaponHolder;
		[Tooltip("Socket names tried in order when attaching the weapon holder to the animated rig.")]
		public string[] WeaponSocketNames = { "ik_hand_gun", "ik_hand_r", "hand_r", "weapon_socket", "right_hand_weapon", "righthand" };
		[Tooltip("When enabled, the weapon is mounted to the animated character rig. Disable for an FPS-style camera viewmodel.")]
		public bool MountWeaponToCharacter = false;
		[Tooltip("Camera-relative position used for the FPS weapon viewmodel.")]
		public Vector3 ViewmodelLocalPosition = new Vector3(0.11f, -0.12f, 0.28f);
		[Tooltip("Camera-relative rotation used for the FPS weapon viewmodel.")]
		public Vector3 ViewmodelLocalEulerAngles = new Vector3(2.0f, -8.0f, 0.0f);
		[Tooltip("Optional first-person arms/viewmodel prefab. When assigned, it is instantiated under the main camera.")]
		public GameObject FirstPersonArmsPrefab;
		[Tooltip("Layer used by the separate first-person viewmodel camera.")]
		public string ViewmodelLayerName = "ViewModel";
		[Tooltip("Socket names tried on the first-person arms prefab for weapon mounting.")]
		public string[] ViewmodelSocketNames = { "WeaponSocket", "weapon_socket", "ik_hand_gun", "ik_hand_r", "hand_r", "righthand" };
		[Tooltip("Local position of the instantiated first-person arms prefab under the camera.")]
		public Vector3 ViewmodelArmsLocalPosition = Vector3.zero;
		[Tooltip("Local rotation of the instantiated first-person arms prefab under the camera.")]
		public Vector3 ViewmodelArmsLocalEulerAngles = Vector3.zero;
		[Tooltip("Local scale of the instantiated first-person arms prefab.")]
		public Vector3 ViewmodelArmsLocalScale = Vector3.one;
		[Tooltip("Local position of the shared WeaponHolder relative to the first-person arms weapon socket.")]
		public Vector3 ViewmodelWeaponHolderLocalPosition = Vector3.zero;
		[Tooltip("Local rotation of the shared WeaponHolder relative to the first-person arms weapon socket.")]
		public Vector3 ViewmodelWeaponHolderLocalEulerAngles = Vector3.zero;
		[Tooltip("Hide the world-body player mesh while using a separate first-person viewmodel.")]
		public bool HideWorldBodyInFirstPerson = true;
		[Tooltip("Keep the hidden world body casting shadows.")]
		public bool KeepWorldBodyShadows = true;
		[Tooltip("Local position applied to the holder after it is attached to the hand socket.")]
		public Vector3 WeaponHolderLocalPosition = Vector3.zero;
		[Tooltip("Local rotation applied to the holder after it is attached to the hand socket.")]
		public Vector3 WeaponHolderLocalEulerAngles = Vector3.zero;
		[Tooltip("Mounted weapons keep their authored local offsets so the rifle and pistol stay aligned to the hands.")]
		public bool ResetMountedWeaponChildOffsets = false;
		[Tooltip("First-person field of view.")]
		public float FirstPersonFieldOfView = 75.0f;
		[Tooltip("Near clip used for first-person view.")]
		public float FirstPersonNearClip = 0.05f;
		[Tooltip("Animator damping time for speed transitions.")]
		public float AnimationBlendTime = 0.1f;
		public bool reverseControls = false;
		public bool jumpsDisabled = false;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

	
#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;
		private Camera _mainCameraComponent;
		private bool _weaponHolderAttached;
		private bool _weaponHolderManagedByViewmodel;
		private bool _loggedMissingWeaponSocket;
		private bool _loggedMissingAnimator;
		private bool _loggedMissingViewmodelSocket;
		private bool _loggedMissingViewmodelLayer;
		private Vector3 _originalWeaponHolderLocalPosition;
		private Quaternion _originalWeaponHolderLocalRotation;
		private bool _weaponHolderPoseCaptured;
		private Transform _mountedWeapon;
		private bool _mountedWeaponPoseCaptured;
		private Vector3 _mountedWeaponOriginalLocalPosition;
		private Quaternion _mountedWeaponOriginalLocalRotation;
		private GameObject _viewmodelInstance;
		private Camera _viewmodelCamera;
		private int _viewmodelLayer = -1;
		private bool _worldBodyHidden;

		private const float _threshold = 0.01f;

		private bool IsCurrentDeviceMouse
		{
			get
			{
				#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
				#else
				return false;
				#endif
			}
		}

		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start()
		{
			
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
			RefreshPresentation();
		}

		private void Update()
		{
			JumpAndGravity();
			GroundedCheck();
			Move();
			UpdateCharacterAnimation();
		}

		private void LateUpdate()
		{
			CameraRotation();
			RefreshPresentation();
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				
				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// Update Cinemachine camera target pitch
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void Move()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			// normalise input direction
			
			float moveX = _input.move.x;
			float moveY = _input.move.y;

			if (reverseControls)
			{
				moveX *= -1;
				moveY *= -1;
			}

			Vector3 inputDirection = new Vector3(moveX, 0.0f, moveY).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * moveX + transform.forward * moveY;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
		}

		private void UpdateCharacterAnimation()
		{
			CharacterAnimator = ResolveCharacterAnimator();
			if (CharacterAnimator != null)
			{
				CharacterAnimator.applyRootMotion = false;
			}

			if (CharacterAnimator == null)
			{
				if (!_loggedMissingAnimator)
				{
					Debug.LogWarning("Player animation was not updated because no child Animator with a Speed parameter was found.");
					_loggedMissingAnimator = true;
				}

				return;
			}

			var animationSpeed = Grounded ? _speed : 0.0f;
			CharacterAnimator.SetFloat(SpeedHash, animationSpeed, AnimationBlendTime, Time.deltaTime);
			CharacterAnimator.SetBool(IsSprintingHash, Grounded && _input.sprint && _input.move.sqrMagnitude > _threshold);
		}

		private void RefreshPresentation()
		{
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}

			if (_mainCamera != null && _mainCameraComponent == null)
			{
				_mainCameraComponent = _mainCamera.GetComponent<Camera>();
			}

			CharacterAnimator = ResolveCharacterAnimator();

			if (_mainCamera != null && WeaponHolder == null)
			{
				WeaponHolder = _mainCamera.transform.Find("WeaponHolder");
			}

			if (_mainCameraComponent != null)
			{
				_mainCameraComponent.fieldOfView = FirstPersonFieldOfView;
				_mainCameraComponent.nearClipPlane = FirstPersonNearClip;
			}

			if (_mainCamera != null && CinemachineCameraTarget != null)
			{
				var cameraTargetTransform = CinemachineCameraTarget.transform;
				if (_mainCamera.transform.parent != cameraTargetTransform)
				{
					_mainCamera.transform.SetParent(cameraTargetTransform, true);
				}

				_mainCamera.transform.localPosition = Vector3.zero;
				_mainCamera.transform.localRotation = Quaternion.identity;
			}

			EnsureFirstPersonViewmodel();

			if (WeaponHolder == null)
			{
				return;
			}

			if (_weaponHolderManagedByViewmodel)
			{
				return;
			}

			if (!MountWeaponToCharacter)
			{
				if (_mainCamera != null && WeaponHolder.parent != _mainCamera.transform)
				{
					WeaponHolder.SetParent(_mainCamera.transform, false);
				}

				_weaponHolderAttached = false;
				WeaponHolder.localPosition = ViewmodelLocalPosition;
				WeaponHolder.localRotation = Quaternion.Euler(ViewmodelLocalEulerAngles);
				return;
			}

			if (CharacterAnimator == null)
			{
				return;
			}

			if (_weaponHolderAttached)
			{
				ApplyMountedWeaponAlignment();
				return;
			}

			var socket = FindWeaponSocket(CharacterAnimator.transform);
			if (socket == null)
			{
				if (!_loggedMissingWeaponSocket)
				{
					Debug.LogWarning("WeaponHolder attachment failed: no hand or gun socket was found on the player rig.");
					_loggedMissingWeaponSocket = true;
				}

				return;
			}

			_weaponHolderAttached = true;
			WeaponHolder.SetParent(socket, false);
			ApplyMountedWeaponAlignment();
		}

		private Transform FindWeaponSocket(Transform root)
		{
			return FindSocket(root, WeaponSocketNames, CharacterAnimator);
		}

		private void EnsureFirstPersonViewmodel()
		{
			if (_mainCamera == null || _mainCameraComponent == null || FirstPersonArmsPrefab == null)
			{
				_weaponHolderManagedByViewmodel = false;
				return;
			}

			if (_viewmodelLayer < 0)
			{
				_viewmodelLayer = LayerMask.NameToLayer(ViewmodelLayerName);
				if (_viewmodelLayer < 0 && !_loggedMissingViewmodelLayer)
				{
					Debug.LogWarning($"Viewmodel setup could not find the '{ViewmodelLayerName}' layer. Create it in Project Settings > Tags and Layers.");
					_loggedMissingViewmodelLayer = true;
				}
			}

			if (_viewmodelInstance == null)
			{
				_viewmodelInstance = Instantiate(FirstPersonArmsPrefab, _mainCamera.transform);
				_viewmodelInstance.name = FirstPersonArmsPrefab.name + " (Viewmodel)";
			}

			_viewmodelInstance.transform.localPosition = ViewmodelArmsLocalPosition;
			_viewmodelInstance.transform.localRotation = Quaternion.Euler(ViewmodelArmsLocalEulerAngles);
			_viewmodelInstance.transform.localScale = ViewmodelArmsLocalScale;

			if (_viewmodelLayer >= 0)
			{
				SetLayerRecursively(_viewmodelInstance, _viewmodelLayer);
				EnsureViewmodelCamera();
			}

			if (HideWorldBodyInFirstPerson)
			{
				HideWorldBodyRenderers();
			}

			if (WeaponHolder == null)
			{
				return;
			}

			var socket = FindSocket(_viewmodelInstance.transform, ViewmodelSocketNames, null);
			if (socket == null)
			{
				if (!_loggedMissingViewmodelSocket)
				{
					Debug.LogWarning("First-person arms prefab was found, but no weapon socket was found on it.");
					_loggedMissingViewmodelSocket = true;
				}

				_weaponHolderManagedByViewmodel = false;
				return;
			}

			_loggedMissingViewmodelSocket = false;
			_weaponHolderManagedByViewmodel = true;
			_weaponHolderAttached = false;

			if (WeaponHolder.parent != socket)
			{
				WeaponHolder.SetParent(socket, false);
			}

			WeaponHolder.localPosition = ViewmodelWeaponHolderLocalPosition;
			WeaponHolder.localRotation = Quaternion.Euler(ViewmodelWeaponHolderLocalEulerAngles);

			if (_viewmodelLayer >= 0)
			{
				SetLayerRecursively(WeaponHolder.gameObject, _viewmodelLayer);
			}
		}

		private void EnsureViewmodelCamera()
		{
			if (_viewmodelLayer < 0 || _mainCameraComponent == null)
			{
				return;
			}

			if (_viewmodelCamera == null)
			{
				var existing = _mainCamera.transform.Find("ViewmodelCamera");
				if (existing != null)
				{
					_viewmodelCamera = existing.GetComponent<Camera>();
				}

				if (_viewmodelCamera == null)
				{
					var cameraObject = new GameObject("ViewmodelCamera");
					cameraObject.transform.SetParent(_mainCamera.transform, false);
					_viewmodelCamera = cameraObject.AddComponent<Camera>();
				}
			}

			_viewmodelCamera.transform.localPosition = Vector3.zero;
			_viewmodelCamera.transform.localRotation = Quaternion.identity;
			_viewmodelCamera.clearFlags = CameraClearFlags.Depth;
			_viewmodelCamera.cullingMask = 1 << _viewmodelLayer;
			_viewmodelCamera.fieldOfView = _mainCameraComponent.fieldOfView;
			_viewmodelCamera.nearClipPlane = 0.01f;
			_viewmodelCamera.farClipPlane = _mainCameraComponent.farClipPlane;
			_viewmodelCamera.depth = _mainCameraComponent.depth + 1f;

			var audioListener = _viewmodelCamera.GetComponent<AudioListener>();
			if (audioListener != null)
			{
				audioListener.enabled = false;
			}

			_mainCameraComponent.cullingMask &= ~(1 << _viewmodelLayer);

			var mainCameraData = _mainCameraComponent.GetComponent<UniversalAdditionalCameraData>();
			var viewmodelCameraData = _viewmodelCamera.GetComponent<UniversalAdditionalCameraData>();
			if (viewmodelCameraData == null)
			{
				viewmodelCameraData = _viewmodelCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
			}
			if (mainCameraData != null && viewmodelCameraData != null)
			{
				viewmodelCameraData.renderType = CameraRenderType.Overlay;

				if (!mainCameraData.cameraStack.Contains(_viewmodelCamera))
				{
					mainCameraData.cameraStack.Add(_viewmodelCamera);
				}
			}
		}

		private void HideWorldBodyRenderers()
		{
			if (_worldBodyHidden || CharacterAnimator == null)
			{
				return;
			}

			var renderers = CharacterAnimator.GetComponentsInChildren<Renderer>(true);
			for (var i = 0; i < renderers.Length; i++)
			{
				if (KeepWorldBodyShadows)
				{
					renderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
				}
				else
				{
					renderers[i].enabled = false;
				}
			}

			_worldBodyHidden = true;
		}

		private static Transform FindSocket(Transform root, string[] socketNames, Animator humanoidFallbackAnimator)
		{
			for (var i = 0; i < socketNames.Length; i++)
			{
				var exactMatch = FindChildByName(root, socketNames[i], true);
				if (exactMatch != null)
				{
					return exactMatch;
				}
			}

			for (var i = 0; i < socketNames.Length; i++)
			{
				var partialMatch = FindChildByName(root, socketNames[i], false);
				if (partialMatch != null)
				{
					return partialMatch;
				}
			}

			return humanoidFallbackAnimator != null
				&& humanoidFallbackAnimator.avatar != null
				&& humanoidFallbackAnimator.avatar.isValid
				&& humanoidFallbackAnimator.avatar.isHuman
				? humanoidFallbackAnimator.GetBoneTransform(HumanBodyBones.RightHand)
				: null;
		}

		private Animator ResolveCharacterAnimator()
		{
			if (IsUsableAnimator(CharacterAnimator))
			{
				return CharacterAnimator;
			}

			var animators = GetComponentsInChildren<Animator>(true);
			for (var i = 0; i < animators.Length; i++)
			{
				if (IsUsableAnimator(animators[i]))
				{
					_loggedMissingAnimator = false;
					return animators[i];
				}
			}

			return null;
		}

		private static bool IsUsableAnimator(Animator animator)
		{
			if (animator == null || animator.runtimeAnimatorController == null)
			{
				return false;
			}

			var parameters = animator.parameters;
			for (var i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].type == AnimatorControllerParameterType.Float && parameters[i].nameHash == SpeedHash)
				{
					return true;
				}
			}

			return false;
		}

		private static Transform FindChildByName(Transform root, string targetName, bool exactMatch)
		{
			if (root == null || string.IsNullOrWhiteSpace(targetName))
			{
				return null;
			}

			var normalizedTargetName = NormalizeName(targetName);
			var transforms = root.GetComponentsInChildren<Transform>(true);
			for (var i = 0; i < transforms.Length; i++)
			{
				var normalizedCurrentName = NormalizeName(transforms[i].name);
				if (exactMatch ? normalizedCurrentName == normalizedTargetName : normalizedCurrentName.Contains(normalizedTargetName))
				{
					return transforms[i];
				}
			}

			return null;
		}

		private static string NormalizeName(string value)
		{
			return value.Replace("_", "").Replace(" ", "").ToLowerInvariant();
		}

		private static void SetLayerRecursively(GameObject root, int layer)
		{
			if (root == null)
			{
				return;
			}

			var transforms = root.GetComponentsInChildren<Transform>(true);
			for (var i = 0; i < transforms.Length; i++)
			{
				transforms[i].gameObject.layer = layer;
			}
		}

		private void CaptureWeaponHolderPose()
		{
			if (WeaponHolder == null || _weaponHolderPoseCaptured)
			{
				return;
			}

			_originalWeaponHolderLocalPosition = WeaponHolder.localPosition;
			_originalWeaponHolderLocalRotation = WeaponHolder.localRotation;
			_weaponHolderPoseCaptured = true;
		}

		private void ApplyMountedWeaponAlignment()
		{
			if (WeaponHolder == null)
			{
				return;
			}

			WeaponHolder.localPosition = WeaponHolderLocalPosition;
			WeaponHolder.localRotation = Quaternion.Euler(WeaponHolderLocalEulerAngles);
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (!jumpsDisabled && _input.jump && _jumpTimeoutDelta <= 0.0f)
				{
						_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}