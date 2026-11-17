using System;
using UnityEngine;

/// <summary>
/// Tiny procedural sound effects so the game has audio without any imported assets.
/// </summary>
public static class Sfx
{
    static AudioSource source;
    static AudioClip coin, jump, doubleJump, hit, win;

    public static void Init(GameObject host)
    {
        source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        coin = Make("coin", 0.20f, t =>
        {
            float f = t < 0.07f ? 988f : 1319f;
            return Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 12f);
        });

        jump = Make("jump", 0.18f, t =>
        {
            float phase = 2f * Mathf.PI * (280f * t + 0.5f * 1800f * t * t);
            return Mathf.Sin(phase) * (1f - t / 0.18f) * 0.8f;
        });

        doubleJump = Make("doubleJump", 0.18f, t =>
        {
            float phase = 2f * Mathf.PI * (420f * t + 0.5f * 2400f * t * t);
            return Mathf.Sin(phase) * (1f - t / 0.18f) * 0.8f;
        });

        hit = Make("hit", 0.45f, t =>
        {
            float noise = UnityEngine.Random.value * 2f - 1f;
            float thud = Mathf.Sin(2f * Mathf.PI * 70f * t);
            return (noise * 0.5f + thud) * Mathf.Exp(-t * 7f);
        });

        win = Make("win", 0.64f, t =>
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int i = Mathf.Min(3, (int)(t / 0.16f));
            float local = t - i * 0.16f;
            return Mathf.Sin(2f * Mathf.PI * notes[i] * t) * Mathf.Exp(-local * 5f) * 0.8f;
        });
    }

    static AudioClip Make(string name, float duration, Func<float, float> wave)
    {
        const int rate = 44100;
        int n = (int)(duration * rate);
        var data = new float[n];
        for (int i = 0; i < n; i++)
            data[i] = Mathf.Clamp(wave(i / (float)rate), -1f, 1f);
        var clip = AudioClip.Create(name, n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static void Play(AudioClip clip, float volume)
    {
        if (source != null && clip != null) source.PlayOneShot(clip, volume);
    }

    public static void Coin() => Play(coin, 0.55f);
    public static void Jump() => Play(jump, 0.45f);
    public static void DoubleJump() => Play(doubleJump, 0.45f);
    public static void Hit() => Play(hit, 0.8f);
    public static void Win() => Play(win, 0.7f);
}
