using System.Collections;
using System.Collections.Generic;
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
                foreach (Material m in r.materials)
                {
                    // Switch to Fade mode
                    m.SetFloat("_Mode", 2);
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    m.EnableKeyword("_BLEND_ON");
                    m.DisableKeyword("_PREMULTIPLY_ON");
                    m.renderQueue = 3000;

                    // Make fully transparent
                    Color c = m.color;
                    m.color = new Color(c.r, c.g, c.b, 0f);
                }
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

            // Cache base colors
            List<Material[]> allMaterials = new List<Material[]>();
            List<Color[]> baseColors = new List<Color[]>();

            foreach (Renderer r in renderers)
            {
                Material[] mats = r.materials;
                allMaterials.Add(mats);

                Color[] cols = new Color[mats.Length];
                for (int m = 0; m < mats.Length; m++)
                    cols[m] = mats[m].color;

                baseColors.Add(cols);
            }

            Color[] graphicBase = new Color[graphics.Length];
            for (int g = 0; g < graphics.Length; g++)
                graphicBase[g] = graphics[g].color;

            while (counter < duration)
            {
                counter += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, endAlpha, counter / duration);

                for (int r = 0; r < allMaterials.Count; r++)
                {
                    for (int m = 0; m < allMaterials[r].Length; m++)
                    {
                        Color c = baseColors[r][m];
                        allMaterials[r][m].color = new Color(c.r, c.g, c.b, alpha);
                    }
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
