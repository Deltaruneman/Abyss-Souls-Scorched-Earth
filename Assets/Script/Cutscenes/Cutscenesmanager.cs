using System;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

/// <summary>
/// Singleton quản lý việc phát cutscene dạng video toàn màn hình.
/// Bất kỳ script nào (ví dụ NPCDialogue) cũng có thể gọi CutsceneManager.Instance.PlayCutscene(clip)
/// để phát 1 đoạn video, tạm dừng game, và tự động dọn dẹp khi video kết thúc.
///
/// === CÁCH SETUP TRONG SCENE ===
/// 1. Tạo 1 GameObject rỗng, đặt tên "CutsceneManager".
/// 2. Add component "Video Player" (UnityEngine.Video.VideoPlayer) và script CutsceneManager này vào đó.
/// 3. Tạo 1 Canvas mới (Screen Space - Overlay), đặt Sort Order cao (ví dụ 100) để luôn nằm trên cùng.
/// 4. Trong Canvas, tạo 1 RawImage, kéo dãn (stretch) full 4 cạnh màn hình (Anchor = stretch-stretch).
/// 5. Kéo GameObject chứa Canvas vào ô "Cutscene Canvas" bên dưới, kéo RawImage vào ô "Display Image".
/// 6. Để trống "Render Texture" - script sẽ tự tạo 1 RenderTexture lúc chạy game và tự gán cho cả
///    VideoPlayer lẫn RawImage. (Nếu muốn tự set độ phân giải riêng thì tạo RenderTexture asset và kéo vào).
/// 7. Trên GameObject của NPC (NPCDialogue), bật "Play Cutscene After Final Node" và kéo file video
///    (VideoClip - kéo thẳng file .mp4/.mov đã import vào project) vào ô "End Cutscene Clip".
///
/// Video import: chỉ cần kéo file .mp4/.mov vào thư mục Assets, Unity sẽ tự nhận diện là VideoClip.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    [Header("References (xem hướng dẫn setup trong comment ở đầu file)")]
    [Tooltip("GameObject chứa Canvas hiển thị video. Sẽ tự SetActive(true) khi cutscene bắt đầu, " +
             "SetActive(false) khi kết thúc.")]
    public GameObject cutsceneCanvas;
    [Tooltip("RawImage full màn hình dùng để hiển thị hình ảnh video")]
    public RawImage displayImage;
    [Tooltip("RenderTexture dùng chung cho VideoPlayer và RawImage. Để trống thì script tự tạo lúc runtime.")]
    public RenderTexture renderTexture;

    [Header("Tuỳ chọn")]
    [Tooltip("Dừng hẳn thời gian game (Time.timeScale = 0) trong lúc phát cutscene, tránh Player/Enemy " +
             "vẫn di chuyển ở phía sau")]
    public bool pauseGameDuringCutscene = true;
    [Tooltip("Cho phép bấm phím này để bỏ qua (skip) cutscene")]
    public bool allowSkip = true;
    public KeyCode skipKey = KeyCode.Escape;

    private VideoPlayer videoPlayer;
    private Action onCutsceneEndedCallback;
    private bool isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;

        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(1920, 1080, 0);
        }
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;

        if (displayImage != null) displayImage.texture = renderTexture;
        if (cutsceneCanvas != null) cutsceneCanvas.SetActive(false);

        videoPlayer.loopPointReached += OnVideoFinished;
    }

    // Input.GetKeyDown vẫn hoạt động bình thường dù Time.timeScale = 0,
    // vì Update() chạy theo thời gian thực (unscaled), không bị pause.
    private void Update()
    {
        if (!isPlaying || !allowSkip) return;
        if (Input.GetKeyDown(skipKey))
        {
            SkipCutscene();
        }
    }

    /// <summary>
    /// Phát 1 đoạn video cutscene toàn màn hình.
    /// </summary>
    /// <param name="clip">VideoClip cần phát</param>
    /// <param name="onEnded">(Tuỳ chọn) callback được gọi khi cutscene kết thúc, dù là phát hết hay bị skip</param>
    public void PlayCutscene(VideoClip clip, Action onEnded = null)
    {
        if (clip == null)
        {
            Debug.LogWarning("[CutsceneManager] VideoClip null, bỏ qua yêu cầu phát cutscene.");
            onEnded?.Invoke();
            return;
        }

        onCutsceneEndedCallback = onEnded;
        isPlaying = true;

        if (cutsceneCanvas != null) cutsceneCanvas.SetActive(true);
        if (pauseGameDuringCutscene) Time.timeScale = 0f;

        videoPlayer.clip = clip;
        videoPlayer.Stop();
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnPrepared;
        vp.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        EndCutscene();
    }

    private void SkipCutscene()
    {
        videoPlayer.Stop();
        EndCutscene();
    }

    private void EndCutscene()
    {
        isPlaying = false;
        if (cutsceneCanvas != null) cutsceneCanvas.SetActive(false);
        if (pauseGameDuringCutscene) Time.timeScale = 1f;

        Action callback = onCutsceneEndedCallback;
        onCutsceneEndedCallback = null;
        callback?.Invoke();
    }
}