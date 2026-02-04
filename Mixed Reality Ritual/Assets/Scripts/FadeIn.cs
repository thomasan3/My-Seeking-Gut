using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeIn : MonoBehaviour
{
    [Header("Object to Fade out")]
    [SerializeField] GameObject[] GObj;

    [Header("Fade Duration")]
    [SerializeField] float duration;

    [Header("Toggle for Fade-in")] //Otherwise it will fade out
    [SerializeField] bool fadeIn;

    void Start()
    {
        //Make everything transparent & set render mode to fade
        PrepareAllObjectsForFade();

        //fade-in
        StartCoroutine(FadeRoutine(GObj, true, duration));
    }

    void PrepareAllObjectsForFade()
    {
        foreach (GameObject obj in GObj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            MaskableGraphic[] graphics = obj.GetComponentsInChildren<MaskableGraphic>(true);

            foreach (Renderer r in renderers)
            {
                // Put material into fade mode
                r.material.SetFloat("_Mode", 2);
                r.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                r.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                r.material.SetInt("_ZWrite", 0);
                r.material.DisableKeyword("_ALPHATEST_ON");
                r.material.EnableKeyword("_BLEND_ON");
                r.material.DisableKeyword("_PREMULTIPLY_ON");
                r.material.renderQueue = 3000;

                // Set alpha to 0
                Color c = r.material.color;
                r.material.color = new Color(c.r, c.g, c.b, 0f);
            }

            foreach (MaskableGraphic g in graphics)
            {
                Color c = g.color;
                g.color = new Color(c.r, c.g, c.b, 0f);
            }
        }
    }


    public IEnumerator FadeRoutine(GameObject[] objects, bool fadeIn, float duration)
    {
        for (int i = 0; i < objects.Length; i++)
        {
            float counter = 0f;
            float startAlpha = fadeIn ? 0f : 1f;
            float endAlpha = fadeIn ? 1f : 0f;

            Renderer[] renderers = objects[i].GetComponentsInChildren<Renderer>(true);
            MaskableGraphic[] graphics = objects[i].GetComponentsInChildren<MaskableGraphic>(true);

            // Cache starting colors for ALL children
            Color[] rendererBase = new Color[renderers.Length];
            for (int r = 0; r < renderers.Length; r++)
                rendererBase[r] = renderers[r].material.color;

            Color[] graphicBase = new Color[graphics.Length];
            for (int g = 0; g < graphics.Length; g++)
                graphicBase[g] = graphics[g].color;

            while (counter < duration)
            {
                counter += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, endAlpha, counter / duration);

                for (int r = 0; r < renderers.Length; r++)
                {
                    Color c = rendererBase[r];
                    renderers[r].material.color = new Color(c.r, c.g, c.b, alpha);
                }

                for (int g = 0; g < graphics.Length; g++)
                {
                    Color c = graphicBase[g];
                    graphics[g].color = new Color(c.r, c.g, c.b, alpha);
                }

                yield return null;
            }
        }
    }

}
