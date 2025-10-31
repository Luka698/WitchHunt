using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseUIManager : MonoBehaviour
{
    [Header("UI Buttons")]
    public Button unpauseButton;
    public Button mainMenuButton;

    [Header("Fade Background (Optional)")]
    public CanvasGroup backgroundFade;

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu"; // Tên scene Main Menu

    private bool isPaused = false;

    void Start()
    {
        // Gắn sự kiện cho nút
        if (unpauseButton != null)
            unpauseButton.onClick.AddListener(UnpauseGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (backgroundFade != null)
        {
            backgroundFade.alpha = 0f;
            backgroundFade.blocksRaycasts = false;
        }

        // Đảm bảo UI tắt khi bắt đầu
        gameObject.SetActive(false);
    }

    void Update()
    {
        // Nhấn ESC để bật/tắt pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused)
                PauseGame();
            else
                UnpauseGame();
        }
    }

    /// <summary>
    /// Dừng game và hiển thị giao diện Pause
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;

        Time.timeScale = 0f;
        gameObject.SetActive(true);

        if (backgroundFade != null)
        {
            backgroundFade.alpha = 0.7f;
            backgroundFade.blocksRaycasts = true;
        }

        Debug.Log("[PauseUI] Game paused.");
    }

    /// <summary>
    /// Tiếp tục chơi game
    /// </summary>
    public void UnpauseGame()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = 1f;
        gameObject.SetActive(false);

        if (backgroundFade != null)
        {
            backgroundFade.alpha = 0f;
            backgroundFade.blocksRaycasts = false;
        }

        Debug.Log("[PauseUI] Game resumed.");
    }

    /// <summary>
    /// Quay lại menu chính
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("[PauseUI] Returning to Main Menu...");
        Time.timeScale = 1f; // Trả lại tốc độ bình thường
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
