using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the MuzzlePoint child of the weapon. Call Flash() when the weapon fires.
/// </summary>
public class MuzzleFlash : MonoBehaviour
{
    public float flashDuration = 0.05f;
    public Light muzzleLight;
    public ParticleSystem flashParticles;

    private void Awake()
    {
        if (muzzleLight != null)
            muzzleLight.enabled = false;
    }

    public void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(DoFlash());
    }

    private IEnumerator DoFlash()
    {
        if (muzzleLight != null) muzzleLight.enabled = true;
        if (flashParticles != null) flashParticles.Play();

        yield return new WaitForSeconds(flashDuration);

        if (muzzleLight != null) muzzleLight.enabled = false;
    }
}
