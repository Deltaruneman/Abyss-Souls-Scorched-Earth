using UnityEngine;
using UnityEngine.SceneManagement;

// Gan script nay vao 1 object co BoxCollider2D (nho tick "Is Trigger").
// Khi Player dung trong vung collider va bam phim F, scene se duoc chuyen.
[RequireComponent(typeof(Collider2D))]
public class SceneSwapTrigger : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Ten scene can chuyen toi (phai duoc them vao Build Settings)")]
    public string sceneToLoad;

    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;

    [Header("Detect Player")]
    public string playerTag = "Player";

    private bool playerInRange;

    private void Reset()
    {
        // tu dong bat Is Trigger khi vua gan script, tranh quen tick
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            SwapScene();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) playerInRange = false;
    }

    private void SwapScene()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("SceneSwapTrigger: chua dien ten scene can chuyen toi.");
            return;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}