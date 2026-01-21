using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class PhotoSwapper : MonoBehaviour
{
    [SerializeField] private Material materialAsset; // The Material asset itself
    private string imageUrl = "https://github.com/Joseph-Rother/KI-image/blob/main/latest.png?raw=true";

    private void Start()
    {
        StartCoroutine(DownloadImageCoroutine());
    }

    private IEnumerator DownloadImageCoroutine()
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogWarning("No image URL specified!");
            yield break;
        }

        imageUrl = imageUrl + "?t=" + Time.time;

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download image: " + request.error);
        }
        else
        {
            Texture2D textureToApply = DownloadHandlerTexture.GetContent(request);
            
            if (materialAsset != null && textureToApply != null)
            {
                Debug.Log("Changing texture now");
                materialAsset.mainTexture = textureToApply;
            }
            else
            {
                Debug.LogWarning("Assign materialAsset and textureToApply!");
            }
        }



        



    }
}
