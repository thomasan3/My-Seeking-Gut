using System.Collections;
//using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRWhiteFadeSimple : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    private Coroutine fadeRoutine;

    void Start()
    {
    }

    public void StartFade(Color c, float fadeDuration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(c,fadeDuration));
    }

    IEnumerator FadeRoutine(Color c, float fadeDuration)
    {
        Color startColor = fadeImage.color;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            Color newColor = Color.Lerp(startColor, c, t / fadeDuration);
            fadeImage.color = newColor;
            yield return null;
        }
        fadeImage.color = c;
    }

}
