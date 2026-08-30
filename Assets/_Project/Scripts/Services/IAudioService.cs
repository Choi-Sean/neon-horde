namespace NeonHorde
{
    public interface IAudioService
    {
        void PlaySfx(string key, float volumeScale = 1f);
        void PlayMusic(string key);
        void StopMusic();
        void SetVolumes(float bgm, float sfx);
    }

    /// <summary>
    /// No-op audio until clips are added. Real impl (pooled AudioSources + an
    /// addressable/Resources clip map) is content work for the polish pass.
    /// </summary>
    public sealed class NullAudioService : IAudioService
    {
        public void PlaySfx(string key, float volumeScale = 1f) { }
        public void PlayMusic(string key) { }
        public void StopMusic() { }
        public void SetVolumes(float bgm, float sfx) { }
    }
}
