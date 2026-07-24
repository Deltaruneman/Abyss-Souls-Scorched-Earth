using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton dùng để chuyển scene ở bất kỳ đâu trong game (menu, gameplay, pause menu, ...).
/// Đặt trên 1 GameObject riêng trong scene đầu tiên (ví dụ Main Menu), có DontDestroyOnLoad
/// nên chỉ cần add 1 lần, các scene sau vẫn gọi được qua SceneLoader.Instance.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Loading Screen (tuỳ chọn)")]
    [Tooltip("Panel hiện khi đang load scene, để trống nếu không cần loading screen " +
             "(khi đó chuyển scene sẽ diễn ra ngay lập tức, có thể giật hình nếu scene nặng)")]
    public GameObject loadingScreen;
    [Tooltip("Thanh progress bar hiển thị % load xong, để trống nếu không cần")]
    public Slider progressBar;
    [Tooltip("Text hiển thị % load xong dạng chữ, để trống nếu không cần")]
    public TMP_Text progressText;
    [Tooltip("Thời gian chờ tối thiểu (giây) khi hiện loading screen, tránh trường hợp " +
             "scene nhẹ load xong quá nhanh khiến loading screen chớp 1 cái rồi biến mất")]
    public float minLoadingTime = 0.5f;

    private bool isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingScreen != null) loadingScreen.SetActive(false);
    }

    /// <summary>Load scene theo tên (tên phải có trong Build Settings > Scenes In Build).</summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading || string.IsNullOrEmpty(sceneName)) return;
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    /// <summary>Load lại scene hiện tại (dùng cho nút "Chơi lại" khi thua/thắng).</summary>
    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Thoát game. Trong Editor chỉ log ra Console vì Application.Quit không có tác dụng.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("QuitGame() được gọi (không thoát trong Editor).");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        if (loadingScreen != null) loadingScreen.SetActive(true);
        SetProgress(0f);

        // Time.timeScale có thể đang = 0 nếu chuyển scene từ trong lúc dialogue/pause đang bật
        Time.timeScale = 1f;

        float elapsed = 0f;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // op.progress chạy từ 0 -> 0.9 trong lúc load, giữ ở 0.9 chờ allowSceneActivation
        while (op.progress < 0.9f || elapsed < minLoadingTime)
        {
            elapsed += Time.unscaledDeltaTime;

            float displayProgress = Mathf.Clamp01(op.progress / 0.9f);
            SetProgress(displayProgress);

            yield return null;
        }

        SetProgress(1f);
        op.allowSceneActivation = true;

        // Chờ scene active hẳn rồi mới tắt loading screen
        while (!op.isDone)
        {
            yield return null;
        }

        if (loadingScreen != null) loadingScreen.SetActive(false);
        isLoading = false;
    }

    private void SetProgress(float value01)
    {
        if (progressBar != null) progressBar.value = value01;
        if (progressText != null) progressText.text = Mathf.RoundToInt(value01 * 100f) + "%";
    }
}