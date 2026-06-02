using UnityEngine;
using System.Collections;

public class RitualSpawner : MonoBehaviour
{
    [SerializeField] float rotationSeconds;
    [SerializeField] float rotationDegrees;

    [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField] private Transform viewAngle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {

        Vector3 playerPos = viewAngle.position;
        transform.position = new Vector3(playerPos.x, 0f, playerPos.z);

        Vector3 playerEuler = viewAngle.rotation.eulerAngles;
        Quaternion playerOffset = Quaternion.Euler(0f, playerEuler.y, 0f);
        Quaternion yOffset = Quaternion.AngleAxis(30f, Vector3.up);
        transform.rotation = playerOffset * yOffset;

        StartCoroutine(RotateCoroutine(rotationSeconds));
    }

    private IEnumerator RotateCoroutine(float duration)
    {
        float t = 0f;

        Quaternion startRot = transform.rotation;
        
        Quaternion target = startRot * Quaternion.AngleAxis(-rotationDegrees, Vector3.up);

        while (t < duration)
        {
            t += Time.deltaTime;
            
            float ratio = t / duration;

            float curveRatio = rotationCurve.Evaluate(ratio);
            transform.localRotation = Quaternion.Slerp(startRot, target, curveRatio);

            yield return null;
        }
        transform.localRotation = target;
    }
}
