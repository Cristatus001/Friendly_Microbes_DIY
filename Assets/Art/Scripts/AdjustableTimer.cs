using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class AdjustableTimer : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI daysText;
    public TextMeshProUGUI hoursText;
    public TextMeshProUGUI minutesText;
    public TextMeshProUGUI secondsText;

    public Button daysPlus;
    public Button daysMinus;
    public Button hoursPlus;
    public Button hoursMinus;
    public Button minutesPlus;
    public Button minutesMinus;
    public Button secondsPlus;
    public Button secondsMinus;

    public Button startButton;                 // Start INSIDE panel
    public Button enableTimerPanelButton;      // External button that opens panel (heartbeat while timer runs)
    public Button specialButton;               // optional: unlocked after first run

    public TextMeshProUGUI countdownText;      // MUST be outside timerPanel

    public GameObject timerPanel;              // whole panel GameObject (assign)
    public CanvasGroup timerPanelCanvasGroup;  // canvas group of the panel (fade control)

    [Header("Sound Effects")]
    public AudioSource audioSource;
    public AudioClip clickSound;
    public AudioClip finishSound;

    [Header("Timer Settings")]
    [SerializeField] private float countdownSpeed = 3f; // how many realtime seconds equal 1 timer second * speed

    [Header("References")]
    public BowlController bowlController; // assign bowl controller so timer can lock/unlock it

    // internals
    private int days = 0, hours = 0, minutes = 0, seconds = 0;
    private float remainingSeconds = 0f;
    private bool timerRunning = false;
    private bool startUsed = false; // once used, Start stays locked (until ResetAll)
    private Dictionary<TextMeshProUGUI, Coroutine> pulseCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();
    private Coroutine heartbeatCoroutine = null;

    // global flag others read
    public static bool TimerRunningGlobal = false;

    void Start()
    {
        // Hook +/- listeners
        AddButtonListeners(daysPlus, () => ChangeValue(ref days, 1, 0, 9999, daysText));
        AddButtonListeners(daysMinus, () => ChangeValue(ref days, -1, 0, 9999, daysText));

        AddButtonListeners(hoursPlus, () => ChangeValue(ref hours, 1, 0, 23, hoursText));
        AddButtonListeners(hoursMinus, () => ChangeValue(ref hours, -1, 0, 23, hoursText));

        AddButtonListeners(minutesPlus, () => ChangeValue(ref minutes, 1, 0, 60, minutesText));
        AddButtonListeners(minutesMinus, () => ChangeValue(ref minutes, -1, 0, 60, minutesText));

        AddButtonListeners(secondsPlus, () => ChangeValue(ref seconds, 1, 0, 59, secondsText));
        AddButtonListeners(secondsMinus, () => ChangeValue(ref seconds, -1, 0, 59, secondsText));
        
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartTimerFromPanel);
        }

        // open panel via external button
        if (enableTimerPanelButton != null)
        {
            enableTimerPanelButton.onClick.RemoveAllListeners();
            enableTimerPanelButton.onClick.AddListener(OpenTimerPanel);
            enableTimerPanelButton.interactable = true;
        }

        UpdateUI();
        UpdateCountdownText(0);
        UpdateStartButtonState();

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    void AddButtonListeners(Button btn, Action onClick)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            PlayClickSound();
            onClick?.Invoke();
        });
    }

    void ChangeValue(ref int val, int delta, int min, int max, TextMeshProUGUI textToAnimate)
    {
        if (timerRunning) return;
        val += delta;
        val = Mathf.Clamp(val, min, max);
        UpdateUI();

        if (startUsed)
        {
            if (startButton != null) startButton.interactable = false;
        }
        else
        {
            UpdateStartButtonState();
        }

        if (textToAnimate != null)
        {
            if (pulseCoroutines.TryGetValue(textToAnimate, out Coroutine existing) && existing != null)
            {
                StopCoroutine(existing);
                ResetTextTransform(textToAnimate);
            }
            pulseCoroutines[textToAnimate] = StartCoroutine(AnimateTextPulse(textToAnimate));
        }
    }

    void ResetTextTransform(TextMeshProUGUI txt)
    {
        if (txt == null) return;
        txt.transform.localScale = Vector3.one;
        txt.color = Color.white;
    }

    void UpdateUI()
    {
        if (daysText) daysText.text = days.ToString("00");
        if (hoursText) hoursText.text = hours.ToString("00");
        if (minutesText) minutesText.text = minutes.ToString("00");
        if (secondsText) secondsText.text = seconds.ToString("00");
    }

    void UpdateStartButtonState()
    {
        bool isSevenMinutes = days == 0 && hours == 0 && minutes == 7 && seconds == 0;
        if (startButton != null)
            startButton.interactable = isSevenMinutes && !timerRunning && !startUsed;
    }

    // Called by the Start button inside the timer panel
    public void StartTimerFromPanel()
    {
        PlayClickSound();

        remainingSeconds = seconds + minutes * 60 + hours * 3600 + days * 86400;
        if (remainingSeconds <= 0)
        {
            if (countdownText) { countdownText.text = "Set a valid time!"; countdownText.gameObject.SetActive(true); }
            return;
        }

        if (specialButton != null)
            specialButton.interactable = false;  // lock special button once timer starts

        // lock the bowl while timer runs
        if (bowlController != null) bowlController.SetTimerRunning(true);

        timerRunning = true;
        TimerRunningGlobal = true;
        startUsed = true;
        DisableButtons(true);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            UpdateCountdownText(remainingSeconds);
        }

        // heartbeat effect on enableTimerPanelButton, and make it non-interactable during countdown
        if (heartbeatCoroutine != null) StopCoroutine(heartbeatCoroutine);
        if (enableTimerPanelButton != null)
        {
            enableTimerPanelButton.interactable = false;
            heartbeatCoroutine = StartCoroutine(HeartbeatPulse(enableTimerPanelButton.transform));
        }

        // fade out panel after 2 seconds
        if (timerPanelCanvasGroup != null)
            StartCoroutine(FadeOutPanel(2f, 1f));
    }

    public void OpenTimerPanel()
    {
        if (specialButton != null)
            specialButton.interactable = true;   // allow special button before start

        if (timerPanel == null) return;
        timerPanel.SetActive(true);
        if (timerPanelCanvasGroup != null)
        {
            timerPanelCanvasGroup.alpha = 1f;
            timerPanelCanvasGroup.interactable = true;
            timerPanelCanvasGroup.blocksRaycasts = true;
        }
        UpdateUI();
        UpdateStartButtonState();
    }

    IEnumerator FadeOutPanel(float waitBeforeFade, float fadeDuration)
    {
        yield return new WaitForSeconds(waitBeforeFade);
        if (timerPanelCanvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = timerPanelCanvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            timerPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        timerPanelCanvasGroup.alpha = 0f;
        timerPanelCanvasGroup.interactable = false;
        timerPanelCanvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        if (!timerRunning) return;

        if (remainingSeconds > 0f)
        {
            remainingSeconds -= Time.deltaTime * countdownSpeed;
            if (remainingSeconds < 0f) remainingSeconds = 0f;
            UpdateCountdownText(remainingSeconds);
        }
        else
        {
            // finished
            timerRunning = false;
            TimerRunningGlobal = false;

            if (countdownText != null)
                countdownText.text = "Time's Up!";

            PlayFinishSound();
                 
            // stop heartbeat + restore enable button
            if (heartbeatCoroutine != null) { StopCoroutine(heartbeatCoroutine); heartbeatCoroutine = null; }
            if (enableTimerPanelButton != null)
            {
                enableTimerPanelButton.transform.localScale = Vector3.one;
                enableTimerPanelButton.interactable = true;
            }

            // handle text hiding and bowl unlock
            StartCoroutine(HandleTimerFinished());

            DisableButtons(false);
            if (startButton != null) startButton.interactable = false;
        }
    }

    IEnumerator HandleTimerFinished()
    {
        yield return new WaitForSeconds(2f); // keep "Time's Up!" visible
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
        if (specialButton != null)
            specialButton.interactable = true;   // re-enable after timer completes

        // Unlock bowl after timer
        if (bowlController != null)
        {
            bowlController.SetTimerRunning(false);
            bowlController.EnableMoveAway();
        }
    }

    IEnumerator HideCountdownTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    void UpdateCountdownText(float totalSeconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(Mathf.Max(0f, totalSeconds));
        if (countdownText != null)
            countdownText.text = $"{t.Days:D2}/{t.Hours:D2}/{t.Minutes:D2}/{t.Seconds:D2}";
    }

    IEnumerator AnimateTextPulse(TextMeshProUGUI txt)
    {
        if (txt == null) yield break;
        Color originalColor = txt.color;
        Vector3 originalScale = txt.transform.localScale;
        float pulseDuration = 0.3f;
        float elapsed = 0f;
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float scale = 1f + Mathf.Sin(elapsed * Mathf.PI * 4f) * 0.2f;
            txt.transform.localScale = originalScale * scale;
            txt.color = Color.Lerp(originalColor, Color.white, Mathf.PingPong(elapsed * 8f, 1f));
            yield return null;
        }
        txt.transform.localScale = originalScale;
        txt.color = originalColor;
    }

    void DisableButtons(bool disable)
    {
        if (daysPlus) daysPlus.interactable = !disable;
        if (daysMinus) daysMinus.interactable = !disable;
        if (hoursPlus) hoursPlus.interactable = !disable;
        if (hoursMinus) hoursMinus.interactable = !disable;
        if (minutesPlus) minutesPlus.interactable = !disable;
        if (minutesMinus) minutesMinus.interactable = !disable;
        if (secondsPlus) secondsPlus.interactable = !disable;
        if (secondsMinus) secondsMinus.interactable = !disable;

        if (!disable) UpdateStartButtonState();
        else if (startButton) startButton.interactable = false;
    }

    IEnumerator HeartbeatPulse(Transform target)
    {
        if (target == null) yield break;
        float pulseSpeed = 4f;
        float pulseAmount = 0.12f;
        Vector3 baseScale = Vector3.one;
        float ticker = 0f;
        while (true)
        {
            ticker += Time.deltaTime * pulseSpeed;
            float scale = 1f + Mathf.Sin(ticker) * pulseAmount;
            target.localScale = baseScale * scale;
            yield return null;
        }
    }

    void PlayClickSound()
    {
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound);
    }

    void PlayFinishSound()
    {
        if (audioSource != null && finishSound != null) audioSource.PlayOneShot(finishSound);
    }

    // Reset everything for fresh run (GameManager will call this)
    public void ResetAll()
    {
        StopAllCoroutines();
        timerRunning = false;
        TimerRunningGlobal = false;
        startUsed = false;
        remainingSeconds = 0f;
        UpdateCountdownText(0f);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        UpdateUI();
        UpdateStartButtonState();
        if (timerPanelCanvasGroup != null) timerPanelCanvasGroup.alpha = 1f;
       
        if (enableTimerPanelButton != null) enableTimerPanelButton.interactable = true;
    }
}
