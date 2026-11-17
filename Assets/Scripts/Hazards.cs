using UnityEngine;

/// <summary>Spinning, bobbing golden coin. Collected on touch.</summary>
public class Coin : MonoBehaviour
{
    float baseY;
    float phase;

    void Start()
    {
        baseY = transform.localPosition.y;
        phase = Random.value * 6.28f;
    }

    void Update()
    {
        transform.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
        var p = transform.localPosition;
        p.y = baseY + Mathf.Sin(Time.time * 3f + phase) * 0.08f;
        transform.localPosition = p;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<PlayerController>()) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        gm.AddCoin();
        Destroy(gameObject);
    }

    public static GameObject Create(Transform parent, Vector3 pos)
    {
        var go = new GameObject("Coin");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var disc = DesertFactory.Prim(PrimitiveType.Cylinder, go.transform, "Disc", Vector3.zero,
            new Vector3(0.7f, 0.06f, 0.7f), DesertFactory.Mat(DesertFactory.Gold, 0.8f, 0.35f));
        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var rim = DesertFactory.Prim(PrimitiveType.Cylinder, go.transform, "Rim", Vector3.zero,
            new Vector3(0.52f, 0.075f, 0.52f), DesertFactory.Mat(new Color(1f, 0.92f, 0.45f), 0.9f, 0.5f));
        rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var sc = go.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.75f;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        go.AddComponent<Coin>();
        return go;
    }
}

/// <summary>Anything that ends the run when the player touches it.</summary>
public class KillZone : MonoBehaviour
{
    public string reason = "Ouch!";
    public bool active = true;

    void OnTriggerEnter(Collider other) { Check(other); }
    void OnTriggerStay(Collider other) { Check(other); }

    void Check(Collider other)
    {
        if (!active || !other.GetComponent<PlayerController>()) return;
        var gm = GameManager.Instance;
        if (gm != null) gm.Die(reason);
    }

    /// <summary>Adds a kinematic rigidbody (so moving triggers are detected reliably) and a trigger box.</summary>
    public static KillZone AddBox(GameObject go, Vector3 center, Vector3 size, string reason, bool kinematicBody = true)
    {
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.center = center;
        bc.size = size;
        EnsureBody(go);
        var kz = go.AddComponent<KillZone>();
        kz.reason = reason;
        return kz;
    }

    public static KillZone AddSphere(GameObject go, Vector3 center, float radius, string reason)
    {
        var sc = go.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.center = center;
        sc.radius = radius;
        EnsureBody(go);
        var kz = go.AddComponent<KillZone>();
        kz.reason = reason;
        return kz;
    }

    static void EnsureBody(GameObject go)
    {
        if (go.GetComponent<Rigidbody>()) return;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}

/// <summary>Continuously rotates an object (spinner bars, decorations).</summary>
public class Rotator : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float degreesPerSecond = 80f;

    void Update()
    {
        transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}

/// <summary>
/// A desert boulder that starts rolling toward the player once they get close.
/// It rolls along its lane (−Z direction) and is a kill zone while rolling.
/// </summary>
public class RollingRock : MonoBehaviour
{
    public float triggerDistance = 34f;
    public float rollSpeed = 7f;
    public float radius = 1f;

    bool rolling;
    Transform visual;

    void Awake()
    {
        visual = transform.GetChild(0);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        if (!rolling)
        {
            if (transform.position.z - gm.Player.transform.position.z < triggerDistance) rolling = true;
            else return;
        }

        transform.position += Vector3.back * (rollSpeed * Time.deltaTime);
        // roll about the X axis so the boulder tumbles toward the camera
        visual.Rotate(Vector3.right, -rollSpeed / radius * Mathf.Rad2Deg * Time.deltaTime, Space.World);

        // Fell off the end of the platform far behind the player — clean up.
        if (transform.position.z < gm.Player.transform.position.z - 25f) Destroy(gameObject);
    }

    public static GameObject Create(Transform parent, Vector3 pos, float radius = 1.05f)
    {
        var root = new GameObject("RollingRock");
        root.transform.SetParent(parent, false);
        root.transform.position = pos + Vector3.up * radius;

        var rock = DesertFactory.Prim(PrimitiveType.Sphere, root.transform, "Boulder", Vector3.zero,
            Vector3.one * radius * 2f, DesertFactory.Mat(new Color(0.62f, 0.45f, 0.30f), 0.05f));
        // lumps so the rotation reads clearly
        for (int i = 0; i < 6; i++)
        {
            var b = DesertFactory.Prim(PrimitiveType.Sphere, rock.transform, "Lump",
                Random.onUnitSphere * 0.45f, Vector3.one * Random.Range(0.25f, 0.4f),
                DesertFactory.Mat(DesertFactory.Brown));
        }

        KillZone.AddSphere(root, Vector3.zero, radius * 0.9f, "A rolling boulder flattened you!");
        var rr = root.AddComponent<RollingRock>();
        rr.radius = radius;
        return root;
    }
}

/// <summary>
/// A boulder that plunges from the sky when the player approaches. A dark warning disc marks the landing spot.
/// It lands just before the player arrives, then stays on the ground as an obstacle.
/// </summary>
public class FallingRock : MonoBehaviour
{
    public float startHeight = 16f;
    public float gravity = 30f;
    public float leadTime = 0.55f;   // seconds before the player arrives that the rock lands

    float groundY;
    float velocity;
    bool falling;
    bool landed;
    Transform shadow;

    void Start()
    {
        groundY = transform.position.y;
        transform.position += Vector3.up * startHeight;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        if (shadow != null && !landed)
        {
            // Warning marker grows as the rock approaches the ground.
            float h = Mathf.Max(0f, transform.position.y - groundY);
            float s = Mathf.Lerp(1.9f, 0.9f, Mathf.Clamp01(h / startHeight));
            shadow.localScale = new Vector3(s, 0.02f, s);
        }

        if (landed) return;

        if (!falling)
        {
            float fallTime = Mathf.Sqrt(2f * startHeight / gravity);
            float dist = gm.Player.Speed * (fallTime + leadTime);
            if (transform.position.z - gm.Player.transform.position.z < dist) falling = true;
            return;
        }

        velocity += gravity * Time.deltaTime;
        transform.position += Vector3.down * (velocity * Time.deltaTime);
        transform.Rotate(120f * Time.deltaTime, 60f * Time.deltaTime, 0f);

        if (transform.position.y <= groundY)
        {
            var p = transform.position; p.y = groundY; transform.position = p;
            landed = true;
            if (gm.CameraRig != null) gm.CameraRig.Shake(0.15f);
            if (shadow != null) Destroy(shadow.gameObject);
        }
    }

    public static GameObject Create(Transform parent, Vector3 groundPos, float radius = 1.0f)
    {
        var root = new GameObject("FallingRock");
        root.transform.SetParent(parent, false);
        root.transform.position = groundPos + Vector3.up * radius;

        DesertFactory.Prim(PrimitiveType.Sphere, root.transform, "Boulder", Vector3.zero,
            new Vector3(radius * 2f, radius * 1.8f, radius * 2f), DesertFactory.Mat(new Color(0.55f, 0.38f, 0.26f), 0.05f));
        for (int i = 0; i < 5; i++)
            DesertFactory.Prim(PrimitiveType.Sphere, root.transform, "Lump", Random.onUnitSphere * 0.5f,
                Vector3.one * Random.Range(0.3f, 0.5f), DesertFactory.Mat(DesertFactory.Brown));

        KillZone.AddSphere(root, Vector3.zero, radius * 0.95f, "A falling rock crushed you!");
        var fr = root.AddComponent<FallingRock>();

        var warn = DesertFactory.Prim(PrimitiveType.Cylinder, parent, "RockShadow", groundPos + Vector3.up * 0.03f,
            new Vector3(1.9f, 0.02f, 1.9f), DesertFactory.Mat(new Color(0.55f, 0.12f, 0.08f), 0.0f, 0.4f));
        fr.shadow = warn.transform;
        return root;
    }
}

/// <summary>Finish gate trigger: reaching it wins the level.</summary>
public class FinishGate : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<PlayerController>()) return;
        var gm = GameManager.Instance;
        if (gm != null) gm.Win();
    }
}
