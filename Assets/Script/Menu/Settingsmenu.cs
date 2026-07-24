using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Gắn vào GameObject chứa panel Settings. Xử lý âm lượng (qua AudioMixer, khuyến khích
/// dùng AudioMixer thay vì AudioListener.volume để dễ mở rộng thêm Music/SFX riêng sau này)
/// và bật/tắt fullscreen. Các giá trị được lưu qua PlayerPrefs nên load lại vẫn giữ nguyên.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("AudioMixer của project, cần expose 1 param kiểu float tên đúng như masterVolumeParam " +
             "(chuột phải vào param trong Mixer -> Expose to script)")]
    public AudioMixer audioMixer;
    [Tooltip("Tên param đã expose trong AudioMixer dùng cho âm lượng tổng")]
    public string masterVolumeParam = "MasterVolume";
    [Tooltip("Slider kéo chỉnh âm lượng, giá trị 0.0001 - 1 (không để 0 tuyệt đối vì Log10(0) lỗi)")]
    public Slider volumeSlider;

    [Header("Display")]
    [Tooltip("Toggle bật/tắt chế độ toàn màn hình")]
    public Toggle fullscreenToggle;

    private const string VolumePrefKey = "settings_master_volume";
    private const string FullscreenPrefKey = "settings_fullscreen";

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
        bool savedFullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, Screen.fullScreen ? 1 : 0) == 1;

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(savedVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        SetVolume(savedVolume);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(savedFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
        SetFullscreen(savedFullscreen);
    }

    /// <summary>Gọi khi kéo volumeSlider. Nhận giá trị 0-1, convert sang dB cho AudioMixer.</summary>
    public void SetVolume(float linearValue)
    {
        linearValue = Mathf.Clamp(linearValue, 0.0001f, 1f);

        if (audioMixer != null)
        {
            // Convert thang tuyến tính 0-1 sang decibel (-80dB đến 0dB) cho AudioMixer
            float db = Mathf.Log10(linearValue) * 20f;
            audioMixer.SetFloat(masterVolumeParam, db);
        }

        PlayerPrefs.SetFloat(VolumePrefKey, linearValue);
    }

    /// <summary>Gọi khi bấm fullscreenToggle.</summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenPrefKey, isFullscreen ? 1 : 0);
    }
}