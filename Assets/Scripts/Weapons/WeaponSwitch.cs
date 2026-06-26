using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSwitcher : MonoBehaviour
{
    public GameObject rifle;
    public GameObject pistol;

    private void Start()
    {
        // Default to rifle if available
        SetWeapon(rifle, pistol);
    }

    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            EquipRifle();

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            EquipPistol();
    }

    private void EquipRifle()
    {
        SetWeapon(rifle, pistol);
    }

    private void EquipPistol()
    {
        if (pistol == null)
        {
            Debug.Log("No pistol assigned.");
            return;
        }
        SetWeapon(pistol, rifle);
    }

    private static void SetWeapon(GameObject equipped, GameObject holstered)
    {
        if (equipped != null) equipped.SetActive(true);
        if (holstered != null) holstered.SetActive(false);
    }
}