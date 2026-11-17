using UnityEngine;

/// <summary>
/// Third-person chase camera: locked behind the runner on Z, smoothed on Y so jumps feel gentle,
/// with a speed-based field-of-view kick.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public PlayerController target;
    public Vector3 offset = new Vector3(0f, 4.3f, -8f);
    public float lookAhead = 7f;
    public float baseFov = 62f;
    public float maxFovBoost = 10f;

    Camera cam;
    float smoothY;
    float shake;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void Snap()
    {
        if (target == null) return;
        smoothY = target.transform.position.y;
        Apply(1f, 1f);
    }

    public void Shake(float amount) { shake = amount; }

    void LateUpdate()
    {
        if (target == null) return;
        float dt = Time.unscaledDeltaTime;
        smoothY = Mathf.Lerp(smoothY, target.transform.position.y, 1f - Mathf.Exp(-4f * dt));
        Apply(1f - Mathf.Exp(-6f * dt), 1f - Mathf.Exp(-3f * dt));
    }

    void Apply(float posLerp, float fovLerp)
    {
        var p = target.transform.position;
        var desired = new Vector3(p.x * 0.65f + offset.x, smoothY + offset.y, p.z + offset.z);

        if (shake > 0.001f)
        {
            desired += Random.insideUnitSphere * shake;
            shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 1.5f);
        }

        transform.position = desired;
        var look = new Vector3(p.x * 0.35f, smoothY + 1.2f, p.z + lookAhead);
        transform.rotation = Quaternion.LookRotation(look - transform.position, Vector3.up);

        float t = Mathf.InverseLerp(target.startSpeed, target.maxSpeed, target.Speed);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov + t * maxFovBoost, fovLerp);
    }
}
