using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Zero-setup entry point. Runs automatically when ANY scene starts in Play mode (or a build),
/// so you only need to press Play - no GameObjects have to be placed by hand.
/// It creates the lighting, camera, player, level, HUD and game manager.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        if (Object.FindFirstObjectByType<GameManager>() != null) return;

        var host = new GameObject("DesertRunner");
        Sfx.Init(host);
        SetupLighting();
        var cam = SetupCamera();

        var playerGo = new GameObject("Player");
        playerGo.AddComponent<CharacterController>();
        var player = playerGo.AddComponent<PlayerController>();

        var follow = cam.gameObject.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player;

        var gm = host.AddComponent<GameManager>();
        host.AddComponent<GameHUD>();
        gm.Init(player, follow);
    }

    static void SetupLighting()
    {
        // Disable any lights already in the scene (e.g. the default sun) so ours is the only one.
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) l.gameObject.SetActive(false);

        var sunGo = new GameObject("Desert Sun");
        sunGo.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.93f, 0.78f);
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.8f;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.78f, 0.66f, 0.52f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = DesertFactory.Fog;
        RenderSettings.fogStartDistance = 90f;
        RenderSettings.fogEndDistance = 470f;
        QualitySettings.shadowDistance = 90f;

        // Procedural sky when the shader is available, otherwise a plain sky-blue background.
        var skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = new Material(skyShader);
            sky.SetColor("_SkyTint", new Color(0.75f, 0.62f, 0.5f));
            sky.SetColor("_GroundColor", DesertFactory.Fog);
            sky.SetFloat("_AtmosphereThickness", 1.15f);
            sky.SetFloat("_Exposure", 1.25f);
            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
        }
    }

    static Camera SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.nearClipPlane = 0.2f;
        cam.farClipPlane = 900f;
        cam.backgroundColor = Color.Lerp(DesertFactory.Sky, DesertFactory.Fog, 0.3f);
        cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        cam.allowHDR = true;
        return cam;
    }
}
