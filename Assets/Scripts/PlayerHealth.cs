using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float respawnDelay = 2f;
    public Transform respawnPoint;

    private float currentHealth;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private CharacterController characterController;
    private StarterAssets.FirstPersonController firstPersonController;
    private bool isRespawning;

    public TMP_Text healthText;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        firstPersonController = GetComponent<StarterAssets.FirstPersonController>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        currentHealth = maxHealth;

        UpdateHealthUI();
    }

    public void TakeDamage(float damage)
    {
        if (isRespawning)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            StartCoroutine(Respawn());
        }
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = "Health: " + Mathf.CeilToInt(currentHealth);
        }
    }

    void Die()
    {
        Debug.Log("Player Died");
    }

    IEnumerator Respawn()
    {
        isRespawning = true;
        Die();

        if (firstPersonController != null)
        {
            firstPersonController.enabled = false;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        yield return new WaitForSeconds(respawnDelay);

        var targetPosition = respawnPoint != null ? respawnPoint.position : spawnPosition;
        var targetRotation = respawnPoint != null ? respawnPoint.rotation : spawnRotation;

        transform.SetPositionAndRotation(targetPosition, targetRotation);
        currentHealth = maxHealth;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (firstPersonController != null)
        {
            firstPersonController.enabled = true;
        }

        UpdateHealthUI();
        isRespawning = false;
    }
}