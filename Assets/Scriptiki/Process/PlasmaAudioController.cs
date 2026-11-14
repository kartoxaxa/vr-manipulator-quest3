using UnityEngine;

public class PlasmaAudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource humSource;
    public AudioSource hissSource;
    public AudioSource crackleSource;

    [Header("Process State")]
    public ProcessState state;

    private bool isActive = false;

    void Start()
    {
        InitSource(humSource);
        InitSource(hissSource);
        InitSource(crackleSource);

        humSource.Play();
        hissSource.Play();
        crackleSource.Play();
    }

    void InitSource(AudioSource src)
    {
        if (src == null) return;

        src.volume = 0f;
        src.loop = true;
        src.spatialBlend = 1f;   // ← 3D звук
        src.minDistance = 0.5f;
        src.maxDistance = 4f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.mute = false;
    }

    void Update()
    {
        if (!isActive)
        {
            FadeOutAll();
            return;
        }

        if (state == null)
        {
            humSource.volume = Mathf.Lerp(humSource.volume, 0.3f, Time.deltaTime * 2f);
            hissSource.volume = Mathf.Lerp(hissSource.volume, 0.2f, Time.deltaTime * 2f);
            crackleSource.volume = Mathf.Lerp(crackleSource.volume, 0.1f, Time.deltaTime * 2f);
            return;
        }

        float humTarget = Mathf.Lerp(0.2f, 1f, state.currentA / 10f);
        humSource.volume = Mathf.Lerp(humSource.volume, humTarget, Time.deltaTime * 2f);

        float hissTarget = Mathf.Lerp(0.1f, 0.8f, state.pressure / 5000f);
        hissSource.volume = Mathf.Lerp(hissSource.volume, hissTarget, Time.deltaTime * 2f);

        float instability = 1f - Mathf.Clamp01(state.pressure / 5000f);
        float crackleTarget = Mathf.Lerp(0f, 0.7f, state.gasH2 / 5f) * instability;
        crackleSource.volume = Mathf.Lerp(crackleSource.volume, crackleTarget, Time.deltaTime * 3f);
    }

    private void FadeOutAll()
    {
        humSource.volume = Mathf.MoveTowards(humSource.volume, 0f, Time.deltaTime * 2f);
        hissSource.volume = Mathf.MoveTowards(hissSource.volume, 0f, Time.deltaTime * 2f);
        crackleSource.volume = Mathf.MoveTowards(crackleSource.volume, 0f, Time.deltaTime * 2f);
    }

    public void IgniteAudio()
    {
        isActive = true;
    }

    public void ExtinguishAudio()
    {
        isActive = false;
    }

    public void ApplyMode(ProcessMode mode)
    {
        // режимы НЕ мьютят звук
        // только задают стартовые уровни
        switch (mode)
        {
            case ProcessMode.Mode1:
                hissSource.volume = 0.2f;
                crackleSource.volume = 0.05f;
                break;

            case ProcessMode.Mode2:
                hissSource.volume = 0f;
                crackleSource.volume = 0f;
                break;

            case ProcessMode.Mode3:
                hissSource.volume = 0.2f;
                crackleSource.volume = 0.2f;
                break;

            case ProcessMode.Mode4:
                hissSource.volume = 0.3f;
                crackleSource.volume = 0.3f;
                break;

            case ProcessMode.Mode5:
                hissSource.volume = 0.1f;
                crackleSource.volume = 0f;
                break;

            case ProcessMode.Mode6:
                hissSource.volume = 0f;
                crackleSource.volume = 0.2f;
                break;
        }
    }
}
