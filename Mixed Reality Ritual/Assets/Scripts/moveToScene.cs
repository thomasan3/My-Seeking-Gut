using UnityEngine;
using UnityEngine.SceneManagement;

public class moveToScene : MonoBehaviour
{
    [SerializeField] string sceneName;
    void Update()
    {
        if (OVRInput.Get(OVRInput.Button.One)){
            SceneManager.LoadScene(sceneName);
        }
    }
}
