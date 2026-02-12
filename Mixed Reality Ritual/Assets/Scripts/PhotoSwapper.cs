using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class PhotoSwapper : MonoBehaviour
{
    [Header("Assign the material you want to change")]
    [SerializeField] private Material materialAsset;

    [Header("Firebase Storage direct media URL")]
    [Tooltip("https://firebasestorage.googleapis.com/v0/b/seeking-gut.firebasestorage.app/o/latest.png?alt=media&token=71be01a7-f6fe-4a02-b82d-b36ab5c4cd34")] //put this url in the image url field
    [SerializeField] private string imageUrl;

    [Header("Refresh Settings")]
    [SerializeField] private bool keepRefreshing = true;
    [SerializeField] private float refreshSeconds = 10f;

    private void Start()
    {
        if (keepRefreshing)
            StartCoroutine(RefreshLoop());
        else
            StartCoroutine(DownloadImageCoroutine());
    }

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return DownloadImageCoroutine();
            yield return new WaitForSeconds(refreshSeconds);
        }
    }

    private IEnumerator DownloadImageCoroutine()
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogWarning("No image URL specified!");
            yield break;
        }

        string url = imageUrl.Contains("?")
            ? imageUrl + "&t=" + Time.time
            : imageUrl + "?t=" + Time.time;

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download image: " + request.error + " | URL: " + url);
            yield break;
        }

        Texture2D textureToApply = DownloadHandlerTexture.GetContent(request);

        if (materialAsset != null && textureToApply != null)
        {
            materialAsset.mainTexture = textureToApply;
        }
        else
        {
            Debug.LogWarning("Assign materialAsset in Inspector.");
        }
    }
}
