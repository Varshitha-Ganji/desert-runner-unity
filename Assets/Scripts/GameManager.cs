using System;
using UnityEngine;

public enum GameState { Menu, Playing, Paused, GameOver, Victory }

/// <summary>
/// Owns the game state machine, scoring and the persistent best score.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const string BestKey = "DesertRunner_BestScore";
    public const int CoinValue = 10;       // points per coin
    public const int FinishBonus = 1000;   // bonus for reaching the temple

    public GameState State { get; private set; } = GameState.Menu;
    public PlayerController Player { get; private set; }
    public CameraFollow CameraRig { get; private set; }
    public GameObject LevelRoot { get; private set; }

    public float FinishZ;
    public int Coins { get; private set; }
    public float Distance { get; private set; }
    public int Bonus { get; private set; }
    public int BestScore { get; private set; }
    public bool NewBest { get; private set; }
    public string DeathReason { get; private set; } = "";

    /// <summary>Distance-based score: one point per metre run.</summary>
    public int Score => Coins * CoinValue + Mathf.FloorToInt(Distance) + Bonus;
    public float Progress => TotalLength > 0f ? Mathf.Clamp01(Distance / TotalLength) : 0f;
    public float TotalLength { get; private set; }

    public event Action StateChanged;
    /// <summary>Frame of the last state change; lets the player ignore the key press that caused it.</summary>
    public int StateFrame { get; private set; } = -1;

    float startZ;

    void Awake()
    {
        Instance = this;
        BestScore = PlayerPrefs.GetInt(BestKey, 0);
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    public void Init(PlayerController player, CameraFollow cameraRig)
    {
        Player = player;
        CameraRig = cameraRig;
        BuildRun();
        SetState(GameState.Menu);
    }

    void BuildRun()
    {
        if (LevelRoot != null)
        {
            LevelRoot.SetActive(false);
            Destroy(LevelRoot);
        }

        LevelRoot = LevelBuilder.Build(out FinishZ);
        Player.ResetTo(LevelBuilder.PlayerStart);
        if (CameraRig != null) CameraRig.Snap();

        Coins = 0;
        Distance = 0f;
        Bonus = 0;
        NewBest = false;
        DeathReason = "";
        startZ = Player.transform.position.z;
        TotalLength = FinishZ - startZ;
    }

    void Update()
    {
        if (Player == null) return;

        switch (State)
        {
            case GameState.Menu:
                if (GameInput.ConfirmPressed()) SetState(GameState.Playing);
                break;

            case GameState.Playing:
                Distance = Mathf.Clamp(Player.transform.position.z - startZ, Distance, TotalLength);
                if (GameInput.PausePressed()) SetState(GameState.Paused);
                break;

            case GameState.Paused:
                if (GameInput.PausePressed() || GameInput.ConfirmPressed()) SetState(GameState.Playing);
                else if (GameInput.RestartPressed()) Restart();
                break;

            case GameState.GameOver:
            case GameState.Victory:
                if (GameInput.RestartPressed() || GameInput.ConfirmPressed()) Restart();
                break;
        }
    }

    public void StartRun() { if (State == GameState.Menu) SetState(GameState.Playing); }
    public void Resume() { if (State == GameState.Paused) SetState(GameState.Playing); }
    public void Pause() { if (State == GameState.Playing) SetState(GameState.Paused); }

    public void Restart()
    {
        BuildRun();
        SetState(GameState.Playing);
    }

    public void AddCoin()
    {
        if (State != GameState.Playing) return;
        Coins++;
        Sfx.Coin();
    }

    public void Die(string reason)
    {
        if (State != GameState.Playing) return;
        DeathReason = reason;
        Player.OnDeath();
        Sfx.Hit();
        SaveBest();
        SetState(GameState.GameOver);
    }

    public void Win()
    {
        if (State != GameState.Playing) return;
        Distance = TotalLength;
        Bonus = FinishBonus;
        Player.OnWin();
        Sfx.Win();
        SaveBest();
        SetState(GameState.Victory);
    }

    void SaveBest()
    {
        if (Score > BestScore)
        {
            BestScore = Score;
            NewBest = true;
            PlayerPrefs.SetInt(BestKey, BestScore);
            PlayerPrefs.Save();
        }
    }

    void SetState(GameState next)
    {
        State = next;
        StateFrame = Time.frameCount;
        Time.timeScale = next == GameState.Paused ? 0f : 1f;
        StateChanged?.Invoke();
    }
}
