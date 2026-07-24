using UnityEngine;

/// <summary>
/// Gắn vào 1 GameObject trong scene Main Menu (ví dụ object "MenuManager").
/// Kéo các nút UI trong scene vào OnClick() của Button rồi gọi các hàm public bên dưới,
/// hoặc gọi trực tiếp từ code khác nếu cần.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Tên scene màn chơi chính, phải trùng tên trong Build Settings > Scenes In Build")]
    public string gameplaySceneName = "Gameplay";

    [Header("Panels")]
    [Tooltip("Panel chính của menu (chứa nút Start/Settings/Quit), sẽ ẩn đi khi mở Settings")]
    public GameObject mainPanel;
    [Tooltip("Panel cài đặt, để trống nếu không dùng SettingsMenu")]
    public GameObject settingsPanel;

    private void Start()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    /// <summary>Gắn vào nút "Start" / "Chơi mới" -> chuyển sang scene gameplay.</summary>
    public void OnClickStart()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(gameplaySceneName);
        }
        else
        {
            Debug.LogWarning("Chưa có SceneLoader trong scene này. " +
                              "Nhớ add component SceneLoader vào 1 GameObject rồi thử lại.");
        }
    }

    /// <summary>Gắn vào nút "Settings" -> mở panel cài đặt, ẩn menu chính.</summary>
    public void OnClickSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    /// <summary>Gắn vào nút "Back" trong panel Settings -> quay lại menu chính.</summary>
    public void OnClickBackFromSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    /// <summary>Gắn vào nút "Quit" -> thoát game.</summary>
    public void OnClickQuit()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}