using UnityEngine;
using System.Collections;

public class HitMarkerUI : MonoBehaviour
{
    public GameObject hitMarker;

    public void ShowHitMarker()
    {
        StartCoroutine(HitMarkerFlash());
    }

    IEnumerator HitMarkerFlash()
    {
        hitMarker.SetActive(true);

        yield return new WaitForSeconds(0.1f);

        hitMarker.SetActive(false);
    }
}