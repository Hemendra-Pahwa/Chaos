using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// FPS weapon controller. Attach to WeaponPivot (child of WeaponHolder, parent of WPN_AKM).
/// Handles shooting, ammo, reload, muzzle flash, weapon bob, mouse sway, and recoil.
/// </summary>
public class Weapon : MonoBehaviour
{
    [Header("Camera")]
    public Camera fpsCam;

    [Header("Stats")]
    public float damage = 25f;
    public float range = 100f;
    public float fireRate = 10f;
    public int maxAmmo = 30;
    public float reloadTime = 2f;

    [Header("Effects")]
    public MuzzleFlash muzzleFlash;

    [Header("UI")]
    public TMP_Text ammoText;
    public HitMarkerUI hitMarkerUI;

    [Header("Weapon Feel — Bob")]
    public float bobFrequency = 5f;
    public float bobHorizontalAmplitude = 0.04f;
    public float bobVerticalAmplitude = 0.025f;

    [Header("Weapon Feel — Sway")]
    [Tooltip("Multiplier applied to raw mouse delta (screen px/frame) for position sway.")]
    public float swayAmount = 0.002f;
    public float maxSway = 0.06f;
    public float swaySmoothing = 8f;

    [Header("Weapon Feel — Recoil")]
    public float recoilAmount = 2f;
    public float recoilRecoverySpeed = 8f;

    private float _nextTimeToFire;
    private int _currentAmmo;
    private bool _isReloading;

    private float _bobTimer;
    private Vector3 _restLocalPosition;
    private Quaternion _restLocalRotation;
    private Vector3 _currentSwayOffset;
    private CharacterController _characterController;

    private void Start()
    {
        _currentAmmo = maxAmmo;
        UpdateAmmoUI();
        _restLocalPosition = transform.localPosition;
        _restLocalRotation = transform.localRotation;
        ResolveCharacterController();
    }

    private void ResolveCharacterController()
    {
        _characterController = GetComponentInParent<CharacterController>();
        if (_characterController != null) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _characterController = player.GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (_isReloading) return;

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartCoroutine(Reload());
            return;
        }

        if (_currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        if (Mouse.current.leftButton.isPressed && Time.time >= _nextTimeToFire)
        {
            _nextTimeToFire = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    private void LateUpdate()
    {
        UpdateSwayAndBob();
        RecoverRecoil();
    }

    private void UpdateSwayAndBob()
    {
        // Mouse sway — uses New Input System delta (screen px/frame)
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            Vector3 targetSway = Vector3.ClampMagnitude(
                new Vector3(-delta.y * swayAmount, delta.x * swayAmount, 0f), maxSway);
            _currentSwayOffset = Vector3.Lerp(_currentSwayOffset, targetSway, Time.deltaTime * swaySmoothing);
        }

        // Movement bob
        Vector3 bobOffset = Vector3.zero;
        if (_characterController != null)
        {
            float speed = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;
            if (speed > 0.2f && _characterController.isGrounded)
            {
                _bobTimer += Time.deltaTime * bobFrequency;
                bobOffset.x = Mathf.Sin(_bobTimer) * bobHorizontalAmplitude;
                bobOffset.y = Mathf.Sin(_bobTimer * 2f) * bobVerticalAmplitude;
            }
            else
            {
                // smoothly return bob timer to the nearest zero crossing to avoid a position snap
                _bobTimer = Mathf.Lerp(_bobTimer, Mathf.Round(_bobTimer / Mathf.PI) * Mathf.PI, Time.deltaTime * 5f);
            }
        }

        transform.localPosition = _restLocalPosition + _currentSwayOffset + bobOffset;
    }

    private void RecoverRecoil()
    {
        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            _restLocalRotation,
            Time.deltaTime * recoilRecoverySpeed);
    }

    private void Shoot()
    {
        _currentAmmo--;
        UpdateAmmoUI();

        if (muzzleFlash != null) muzzleFlash.Flash();

        // Visual recoil kick
        transform.Rotate(-recoilAmount, 0f, 0f);

        if (fpsCam == null) return;

        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, range))
        {
            EnemyHealth enemy = hit.transform.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                if (hitMarkerUI != null) hitMarkerUI.ShowHitMarker();
                enemy.TakeDamage((int)damage);
                return;
            }

            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                if (hitMarkerUI != null) hitMarkerUI.ShowHitMarker();
                target.TakeDamage(damage);
            }
        }
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
            ammoText.text = _currentAmmo + " / " + maxAmmo;
    }

    private IEnumerator Reload()
    {
        _isReloading = true;
        Debug.Log("Reloading...");
        yield return new WaitForSeconds(reloadTime);
        _currentAmmo = maxAmmo;
        UpdateAmmoUI();
        _isReloading = false;
        Debug.Log("Reload Complete");
    }
}