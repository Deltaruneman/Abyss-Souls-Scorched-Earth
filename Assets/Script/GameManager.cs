using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý vòng đời của Player: spawn/respawn từ prefab, lưu checkpoint,
/// và hiện menu restart khi Player chết.
/// Đặt script này trên 1 GameObject rỗng trong scene (ví dụ "GameManager"),
/// đảm bảo scene chỉ có duy nhất 1 GameManager.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player")]
    [Tooltip("Prefab của Player (kéo prefab có sẵn component PlayerController vào đây)")]
    public GameObject playerPrefab;
    [Tooltip("Điểm spawn ban đầu, dùng khi chưa có checkpoint nào được kích hoạt")]
    public Transform initialSpawnPoint;
    [Tooltip("Tự động spawn Player ngay khi scene bắt đầu (Start)")]
    public bool spawnOnStart = true;

    [Header("Checkpoint")]
    [Tooltip("Dùng initialSpawnPoint làm checkpoint đầu tiên, tránh trường hợp Player chết trước khi chạm checkpoint nào")]
    public bool useInitialSpawnAsFirstCheckpoint = true;

    [Header("Camera")]
    [Tooltip("CameraFollow trong scene, dùng để tự động gán target là Player mỗi khi spawn/respawn. Để trống nếu không cần.")]
    public CameraFollow cameraFollow;

    [Header("Death & Restart UI")]
    [Tooltip("Panel UI hiện ra khi Player chết (chứa nút Restart), để trống nếu không cần UI")]
    public GameObject restartMenu;
    [Tooltip("Thời gian (giây) trễ trước khi hiện menu restart, để kịp xem animation/hiệu ứng chết")]
    public float restartMenuDelay = 1f;
    [Tooltip("Có dừng game (Time.timeScale = 0) khi hiện menu restart hay không")]
    public bool pauseOnRestartMenu = true;

    private Transform currentCheckpoint;
    private GameObject currentPlayerInstance;
    private PlayerController currentPlayerController;

    // static -> KHÔNG bị mất khi LoadScene() huỷ hết object trong scene cũ (kể cả GameManager cũ).
    // Lưu Vector3 (toạ độ) thay vì Transform, vì Transform của checkpoint object cũ sẽ bị Destroy
    // và trở thành null ngay khi scene được load lại.
    private static Vector3? persistedCheckpointPosition;

    /// <summary>Instance Player hiện tại trong scene (null nếu chưa spawn hoặc đã chết chờ restart).</summary>
    public GameObject CurrentPlayerInstance => currentPlayerInstance;
    public PlayerController CurrentPlayerController => currentPlayerController;

    private void Awake()
    {
        // Đảm bảo chỉ có 1 GameManager tồn tại
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Chỉ dùng initialSpawnPoint làm checkpoint đầu nếu CHƯA có checkpoint nào được lưu từ trước
        // (persistedCheckpointPosition còn giá trị nghĩa là đây là lần Start() sau khi RestartScene() reload lại scene)
        if (!persistedCheckpointPosition.HasValue && useInitialSpawnAsFirstCheckpoint && initialSpawnPoint != null)
        {
            currentCheckpoint = initialSpawnPoint;
        }

        if (restartMenu != null)
        {
            restartMenu.SetActive(false);
        }

        if (spawnOnStart)
        {
            SpawnPlayer();
        }
    }

    /// <summary>
    /// Spawn (hoặc respawn) Player từ playerPrefab tại checkpoint hiện tại
    /// (hoặc initialSpawnPoint nếu chưa có checkpoint nào). Nếu đã có 1 Player
    /// đang tồn tại trong scene, instance cũ sẽ bị huỷ trước khi spawn cái mới.
    /// </summary>
    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("GameManager: chưa gán playerPrefab, không thể spawn Player.");
            return;
        }

        Vector3 spawnPos;
        if (persistedCheckpointPosition.HasValue)
        {
            // Vừa reload scene xong -> checkpoint Transform cũ đã mất, dùng toạ độ đã lưu
            spawnPos = persistedCheckpointPosition.Value;
        }
        else
        {
            Transform spawnPoint = currentCheckpoint != null ? currentCheckpoint : initialSpawnPoint;
            spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        }

        if (currentPlayerInstance != null)
        {
            Destroy(currentPlayerInstance);
        }

        currentPlayerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        currentPlayerController = currentPlayerInstance.GetComponent<PlayerController>();

        if (currentPlayerController != null)
        {
            // Player được spawn lúc runtime nên không thể kéo-thả gán sẵn listener
            // cho onDeath trong Inspector -> đăng ký bằng code ở đây.
            currentPlayerController.onDeath.AddListener(HandlePlayerDeath);
        }
        else
        {
            Debug.LogWarning("GameManager: playerPrefab không có component PlayerController.");
        }

        // Gán lại target cho CameraFollow mỗi lần spawn/respawn, vì Player cũ đã bị Destroy
        // và CameraFollow đang giữ tham chiếu tới object cũ (sẽ thành null/MissingReference).
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(currentPlayerInstance.transform);
        }

        if (restartMenu != null)
        {
            restartMenu.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    /// <summary>
    /// Gọi hàm này từ script Checkpoint khi Player đi qua 1 điểm lưu, để lần
    /// respawn tiếp theo bắt đầu từ đây thay vì initialSpawnPoint.
    /// </summary>
    public void SetCheckpoint(Transform checkpoint)
    {
        if (checkpoint == null) return;
        currentCheckpoint = checkpoint;
        persistedCheckpointPosition = checkpoint.position;
    }

    /// <summary>Được gọi tự động qua PlayerController.onDeath khi Player chết.</summary>
    private void HandlePlayerDeath()
    {
        StartCoroutine(ShowRestartMenuAfterDelay());
    }

    private IEnumerator ShowRestartMenuAfterDelay()
    {
        if (restartMenuDelay > 0f)
        {
            // Dùng WaitForSecondsRealtime để không bị ảnh hưởng nếu có chỗ khác lỡ set Time.timeScale = 0 sớm
            yield return new WaitForSecondsRealtime(restartMenuDelay);
        }

        if (restartMenu != null)
        {
            restartMenu.SetActive(true);
        }

        if (pauseOnRestartMenu)
        {
            Time.timeScale = 0f;
        }
    }

    /// <summary>
    /// Hàm respawn Player: gán cho sự kiện OnClick của nút "Restart" trên UI
    /// (UI này tự hiện ra sau khi Player chết, xem HandlePlayerDeath/ShowRestartMenuAfterDelay).
    /// Reload lại toàn bộ scene (reset địch, item, v.v. về trạng thái ban đầu) nhưng vẫn
    /// respawn Player tại checkpoint gần nhất nhờ persistedCheckpointPosition (static,
    /// không bị mất khi LoadScene huỷ object của scene cũ).
    /// </summary>
    public void RestartFromCheckpoint()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Gán hàm này cho nút "Chơi lại từ đầu" (New Game) nếu muốn xoá hẳn checkpoint
    /// đã lưu và load lại scene từ vị trí initialSpawnPoint.
    /// </summary>
    public void RestartScene()
    {
        persistedCheckpointPosition = null;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}