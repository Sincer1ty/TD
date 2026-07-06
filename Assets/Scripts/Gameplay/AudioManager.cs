using UnityEngine;

namespace TD.Gameplay
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField] private bool persistAcrossScenes;
        [SerializeField] private bool debugLog;

        private static AudioManager instance;

        public static AudioManager Instance => instance != null ? instance : FindFirstObjectByType<AudioManager>();
        public float SfxVolume => sfxVolume;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                if (persistAcrossScenes)
                {
                    Destroy(gameObject);
                }

                return;
            }

            instance = this;

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning("AudioManager.PlaySfx called with a null AudioClip.", this);
                }

                return;
            }

            float finalVolume = Mathf.Clamp01(sfxVolume * Mathf.Clamp01(volume));
            if (finalVolume <= 0f)
            {
                return;
            }

            float safePitch = Mathf.Max(0.01f, pitch);
            GameObject audioObject = new GameObject($"SFX_{clip.name}");
            audioObject.transform.position = position;

            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = finalVolume;
            source.pitch = safePitch;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.Play();

            Destroy(audioObject, clip.length / safePitch + 0.1f);

            if (debugLog)
            {
                Debug.Log($"Playing SFX '{clip.name}' volume={finalVolume}, pitch={safePitch}.", this);
            }
        }
    }
}
