using UnityEngine;

public class PassthroughTesting : MonoBehaviour
{
    [SerializeField] private float fade_speed = 0.001f;
    private OVRPassthroughLayer pt;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pt = GetComponent<OVRPassthroughLayer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            pt.textureOpacity += fade_speed;
        }
        if (OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            pt.textureOpacity -= fade_speed;
        }
    }
}
