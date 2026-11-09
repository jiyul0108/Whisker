using UnityEngine;

// 효과음 한 번 재생 (오브젝트에 붙이지 않음)
// 화면 어디서든 같은 크기로 들리는 2D 소리. 팝업으로 게임이 멈춰 있어도 들린다
// 높이를 바꾸는 소리(울음)는 따로 재생해서 다른 소리의 높이에 영향을 주지 않음
public static class Sfx
{
    private static AudioSource normal, pitched;

    public static void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        bool shift = !Mathf.Approximately(pitch, 1f);
        AudioSource s = shift ? Get(ref pitched, "Sfx_Pitched") : Get(ref normal, "Sfx");
        s.pitch = pitch;
        s.PlayOneShot(clip, volume);
    }

    private static AudioSource Get(ref AudioSource s, string name)
    {
        if (s != null) return s;   // 씬을 다시 불러오면 사라지므로 그때 다시 만듦
        var go = new GameObject(name);
        s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        return s;
    }
}
