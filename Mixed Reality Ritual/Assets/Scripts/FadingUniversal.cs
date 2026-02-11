using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FadingUniversal : MonoBehaviour
{
     private static readonly int TransparencyProp = Shader.PropertyToID("_Transparency");

    public void StartFadeRenderer(GameObject targetObject, float duration, float targetTransparency, float startVal = -1)
    {
        Renderer renderer = targetObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            StartCoroutine(FadeRoutineRenderer(renderer, duration, targetTransparency,startVal));
        }
    }

    private IEnumerator FadeRoutineRenderer(Renderer renderer, float duration, float targetTransparency,float startVal = -1)
    {
        
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        
        renderer.GetPropertyBlock(propBlock);

        float startTransparency = startVal >= 0 ? startVal : propBlock.GetFloat("_Transparency");
        
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float currentVal = Mathf.Lerp(startTransparency, targetTransparency, t);
            
            propBlock.SetFloat(TransparencyProp, currentVal);
            renderer.SetPropertyBlock(propBlock);

            yield return null;
        }

        propBlock.SetFloat(TransparencyProp, targetTransparency);
        renderer.SetPropertyBlock(propBlock);
    }

    public void StartFadeLighting(GameObject targetObject, float duration, float targetBrightRatio,float startVal = -1)
    {
        Light light = targetObject.GetComponent<Light>();
        if (light != null)
        {
            StartCoroutine(FadeRoutineLighting(light, duration, targetBrightRatio,startVal));
        }
    }

    private IEnumerator FadeRoutineLighting(Light light, float duration, float targetBrightRatio,float startVal = -1)
    {
        
       // float startIntensity = startOn ? light.intensity : 0.0f;
        float startIntensity = startVal >= 0 ? startVal : light.intensity;
        float targetIntensity = light.intensity * targetBrightRatio;
        
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float currentVal = Mathf.Lerp(startIntensity,targetIntensity, t);
            
            light.intensity = currentVal;

            yield return null;
        }
        light.intensity = targetIntensity;
    }

    public void StartFadeVolume(GameObject targetObject, float duration, float targetBrightRatio, float startVal = -1)
    {
        Bloom bloom;
        targetObject.GetComponent<Volume>().profile.TryGet<Bloom>(out bloom);
        
        if (bloom != null)
        {
            StartCoroutine(FadeRoutineBloom(bloom, duration, targetBrightRatio,startVal));
        }
    }

    private IEnumerator FadeRoutineBloom(Bloom bloom, float duration, float targetBrightRatio, float startVal = -1)
    {
        
        float startIntensity = startVal >=0 ? startVal : bloom.intensity.value;

        float targetIntensity = bloom.intensity.value * targetBrightRatio;
        
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float currentVal = Mathf.Lerp(startIntensity,targetIntensity, t);
            
            bloom.intensity.value = currentVal;

            yield return null;
        }
        bloom.intensity.value = targetIntensity;
    }

    public void StartFadePassthrough(GameObject targetObject, float duration, float targetTransparency, float startVal = -1)
    {
        OVRPassthroughLayer passthrough = targetObject.GetComponent<OVRPassthroughLayer>();
        
        if (passthrough != null)
        {
            StartCoroutine(FadeRoutinePassthrough(passthrough, duration, targetTransparency,startVal));
        }
    }

    private IEnumerator FadeRoutinePassthrough(OVRPassthroughLayer passthrough, float duration, float targetTransparency, float startVal = -1)
    {
        
        float startTransparency = startVal >= 0 ? startVal : passthrough.textureOpacity;
        
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float currentVal = Mathf.Lerp(startTransparency,targetTransparency, t);
            
            passthrough.textureOpacity = currentVal;

            yield return null;
        }
        passthrough.textureOpacity = targetTransparency;
    }
}
