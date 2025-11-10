using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] public float remainingTime;
    [SerializeField] GameObject GameOverScreen;
    [SerializeField] GameObject ExpectedOutput;

    private bool isTimerActive = false;

    private void Start()
    {
        isTimerActive = false;
    }

    void Update()
    {
        if (isTimerActive && remainingTime > 0)
        {
            // Use unscaledDeltaTime so timer continues even when game is paused or player tabs out
            remainingTime -= Time.unscaledDeltaTime;
        }
        else if (isTimerActive && remainingTime < 0)
        {
            remainingTime = 0;
            timerText.color = Color.red;
            GameOverScreen.SetActive(true);
        }

        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void ResetTimer()
    {
        float resetTime = 20;
        remainingTime = resetTime;
    }

    public void StartTimer()
    {
        isTimerActive = true;
    }

    public void PauseTimer()
    {
        isTimerActive = false;
    }

    public void DisableScreen()
    {
        if (GameOverScreen.activeInHierarchy)
            ExpectedOutput.SetActive(false);
    }
}