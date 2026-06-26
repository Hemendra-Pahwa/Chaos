#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click setup: Chaos > Setup AKM Weapon in Scene
/// Creates the full weapon hierarchy under the Main Camera and wires all references.
///
/// Hierarchy created:
///   MainCamera
///     └── WeaponHolder         (auto-managed by FirstPersonController for position)
///          └── WeaponPivot     (Weapon.cs — bob, sway, recoil)
///               └── WPN_AKM   (FBX model — adjust local rotation/scale if needed)
///                    └── MuzzlePoint  (MuzzleFlash.cs + Point Light)
/// </summary>
public static class WeaponSceneSetup
{
    private const string AKM_PATH = "Assets/Weapons_ChamferZone/AKM/WPN_AKM.FBX";

    [MenuItem("Chaos/Setup AKM Weapon in Scene")]
    private static void SetupAKMWeapon()
    {
        // ── 1. Locate Main Camera ────────────────────────────────────────────────
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            var camObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (camObj != null) mainCamera = camObj.GetComponent<Camera>();
        }

        if (mainCamera == null)
        {
            EditorUtility.DisplayDialog("AKM Setup Failed",
                "No camera tagged 'MainCamera' found.\n\nEnsure your player camera has the 'MainCamera' tag.",
                "OK");
            return;
        }

        // ── 2. Load AKM model ────────────────────────────────────────────────────
        var akmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AKM_PATH);
        if (akmPrefab == null)
        {
            EditorUtility.DisplayDialog("AKM Setup Failed",
                $"AKM model not found at:\n{AKM_PATH}\n\nMake sure the ChamferZone AKM folder is in your Assets.",
                "OK");
            return;
        }

        // ── 3. Create WeaponHolder under MainCamera (FPS controller auto-manages its position) ──
        var weaponHolder = mainCamera.transform.Find("WeaponHolder");
        if (weaponHolder == null)
        {
            var holderObj = new GameObject("WeaponHolder");
            Undo.RegisterCreatedObjectUndo(holderObj, "Create WeaponHolder");
            weaponHolder = holderObj.transform;
            Undo.SetTransformParent(weaponHolder, mainCamera.transform, "Parent WeaponHolder");
            ResetLocalTransform(weaponHolder);
        }

        // ── 4. Replace WeaponPivot if it already exists ──────────────────────────
        var existingPivot = weaponHolder.Find("WeaponPivot");
        if (existingPivot != null)
        {
            if (!EditorUtility.DisplayDialog("AKM Setup",
                "A WeaponPivot already exists under WeaponHolder. Replace it?",
                "Replace", "Cancel"))
                return;

            Undo.DestroyObjectImmediate(existingPivot.gameObject);
        }

        // ── 5. Create WeaponPivot ─────────────────────────────────────────────────
        var pivotObj = new GameObject("WeaponPivot");
        Undo.RegisterCreatedObjectUndo(pivotObj, "Create WeaponPivot");
        var weaponPivot = pivotObj.transform;
        Undo.SetTransformParent(weaponPivot, weaponHolder, "Parent WeaponPivot");
        ResetLocalTransform(weaponPivot);

        // ── 6. Instantiate WPN_AKM model under WeaponPivot ───────────────────────
        var akmInstance = (GameObject)PrefabUtility.InstantiatePrefab(akmPrefab, weaponPivot);
        Undo.RegisterCreatedObjectUndo(akmInstance, "Instantiate WPN_AKM");
        akmInstance.name = "WPN_AKM";
        akmInstance.transform.localPosition = Vector3.zero;
        akmInstance.transform.localRotation = Quaternion.identity;
        akmInstance.transform.localScale = Vector3.one;

        // ── 7. Create MuzzlePoint at approximate barrel tip ──────────────────────
        var muzzlePointObj = new GameObject("MuzzlePoint");
        Undo.RegisterCreatedObjectUndo(muzzlePointObj, "Create MuzzlePoint");
        muzzlePointObj.transform.SetParent(akmInstance.transform, false);
        muzzlePointObj.transform.localPosition = new Vector3(0f, 0.04f, 0.55f); // adjust to barrel tip

        var muzzleLight = Undo.AddComponent<Light>(muzzlePointObj);
        muzzleLight.type = LightType.Point;
        muzzleLight.color = new Color(1f, 0.8f, 0.3f);
        muzzleLight.intensity = 4f;
        muzzleLight.range = 3f;
        muzzleLight.shadows = LightShadows.None;
        muzzleLight.enabled = false;

        var muzzleFlash = Undo.AddComponent<MuzzleFlash>(muzzlePointObj);
        SetSerializedField(muzzleFlash, "muzzleLight", muzzleLight);

        // ── 8. Add Weapon component to WeaponPivot and wire references ────────────
        var weaponComp = Undo.AddComponent<Weapon>(pivotObj);
        SetSerializedField(weaponComp, "fpsCam", mainCamera);
        SetSerializedField(weaponComp, "muzzleFlash", muzzleFlash);

        // Try to find AmmoText by name
        var allTmpTexts = Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None);
        foreach (var t in allTmpTexts)
        {
            if (t.name.ToLower().Contains("ammo"))
            {
                SetSerializedField(weaponComp, "ammoText", t);
                break;
            }
        }

        // Try to find HitMarkerUI
        var hitMarkers = Object.FindObjectsByType<HitMarkerUI>(FindObjectsSortMode.None);
        if (hitMarkers.Length > 0)
            SetSerializedField(weaponComp, "hitMarkerUI", hitMarkers[0]);

        // ── 9. Assign AKM as "rifle" in WeaponSwitcher ────────────────────────────
        var switcher = Object.FindFirstObjectByType<WeaponSwitcher>();
        if (switcher != null)
        {
            Undo.RecordObject(switcher, "Assign AKM to WeaponSwitcher");
            switcher.rifle = pivotObj;
            EditorUtility.SetDirty(switcher);
        }

        // ── 10. Finalise ──────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(mainCamera.gameObject.scene);
        Selection.activeGameObject = pivotObj;

        // Build a summary with any warnings
        var warnings = new StringBuilder();
        if (weaponComp.ammoText == null)
            warnings.AppendLine("• AmmoText UI not wired — assign it manually on the Weapon component.");
        if (weaponComp.hitMarkerUI == null)
            warnings.AppendLine("• HitMarkerUI not wired — assign it manually on the Weapon component.");
        if (switcher == null)
            warnings.AppendLine("• WeaponSwitcher not found — AKM won't respond to weapon switching yet.");

        var msg =
            "AKM weapon hierarchy created!\n\n" +
            "  MainCamera\n" +
            "    └── WeaponHolder\n" +
            "         └── WeaponPivot  (Weapon.cs)\n" +
            "              └── WPN_AKM  (model)\n" +
            "                   └── MuzzlePoint\n\n" +
            "Next steps:\n" +
            "1. Press Play and check the gun position. Adjust WPN_AKM local rotation/scale in the Inspector if it looks wrong.\n" +
            "2. Move MuzzlePoint to the tip of the barrel in the Scene view.\n" +
            "3. If the gun clips through walls, increase ViewmodelLocalPosition.z on the FirstPersonController.\n" +
            (warnings.Length > 0 ? "\nWarnings:\n" + warnings : "");

        EditorUtility.DisplayDialog("AKM Setup Complete", msg, "OK");
    }

    private static void ResetLocalTransform(Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    private static void SetSerializedField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
