using System.Collections;
using UnityEngine;

public class VRWhiteFadeSimple : MonoBehaviour
{
    public MeshRenderer fadeSphere;
    public float fadeDuration = 1f;

    private Material mat;
    private float alpha = 0f;
    private Coroutine fadeRoutine;

    void Start()
    {
        mat = fadeSphere.material;
        SetAlpha(0f); // start invisible
    }

    public void FadeToWhite()
    {
        StartFade(1f);
    }

    public void FadeFromWhite()
    {
        StartFade(0f);
    }

    void StartFade(float target)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    IEnumerator FadeRoutine(float target)
    {
        float start = alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            alpha = Mathf.Lerp(start, target, t / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(target);
    }

    void SetAlpha(float a)
    {
        Color c = mat.color;
        c.a = a;
        mat.color = c;
    }
}
