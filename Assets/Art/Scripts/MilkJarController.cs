using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MilkJarController : MonoBehaviour
{
    [Header("Setup")]
    public Transform pivotPoint;           // empty child used for tilt (z axis)
    public Transform defaultPosition;      // jar returns here after pour
    public Transform milkFillTransform;    // sprite/transform representing milk fill inside bowl
    public Transform fillTarget;           // bowl target position for pouring detection
    public GameObject timerPanel;          // timer panel (assign)
    public Button enableTimerPanelButton;  // the external button that opens timer panel (hidden until both conditions met)
    public BowlController bowlController;

    [Header("Pour Settings")]
    public float pourDistance = 1.2f;      // detection radius to start pouring
    public float tiltSpeed = 8f;
    public float pourSpeed = 0.3f;         // how fast milk fill grows
    public float returnSpeed = 2f;         // jar returning speed (lerp factor)
    public float minTiltAngleZ = 35f;
    public float maxTiltAngleZ = 55f;
    public float fillScaleX = 0.3f;        // x and z scale for milkFill
    public float pourCompleteAmount = 0.5f;

    // internals
    private bool isPouring = false;
    private bool isReturning = false;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private bool isOverBowl = false;
    private float currentMilkFill = 0f;
    private bool placedOnStove = false;
    private bool pourComplete = false;

    void Start()
    {
        if (milkFillTransform != null)
            milkFillTransform.localScale = new Vector3(fillScaleX, 0f, fillScaleX);
        currentMilkFill = 0f;

        if (timerPanel != null)
            timerPanel.SetActive(false);

        if (enableTimerPanelButton != null)
        {
            enableTimerPanelButton.gameObject.SetActive(false);
            // DO NOT add a click handler here — AdjustableTimer.OpenTimerPanel is hooked to this button.
        }
    }

    void Update()
    {
        HandleDrag();

        if (isReturning) return;

        CheckPourProximity();

        if (isPouring)
            PourMilk();
        else
            TiltJar(false);
    }

    void HandleDrag()
    {
        if (isPouring || isReturning) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mp = Input.mousePosition;
            mp.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
            Vector3 world = Camera.main.ScreenToWorldPoint(mp);
            if (Vector2.Distance(new Vector2(world.x, world.y), new Vector2(transform.position.x, transform.position.y)) < 1f)
            {
                isDragging = true;
                dragOffset = transform.position - world;
            }
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 mp = Input.mousePosition;
            mp.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
            Vector3 world = Camera.main.ScreenToWorldPoint(mp);
            transform.position = world + dragOffset;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    void CheckPourProximity()
    {
        if (fillTarget == null) return;
        float dist = Vector2.Distance(new Vector2(transform.position.x, transform.position.y),
                                      new Vector2(fillTarget.position.x, fillTarget.position.y));
        isOverBowl = dist < pourDistance;

        if (isOverBowl && !isPouring && currentMilkFill < pourCompleteAmount)
            isPouring = true;
        else if ((!isOverBowl || currentMilkFill >= pourCompleteAmount) && isPouring)
            isPouring = false;
    }

    void PourMilk()
    {
        TiltJar(true);

        currentMilkFill += pourSpeed * Time.deltaTime;
        currentMilkFill = Mathf.Clamp01(currentMilkFill);

        if (milkFillTransform != null)
            milkFillTransform.localScale = new Vector3(fillScaleX, currentMilkFill, fillScaleX);

        if (currentMilkFill >= pourCompleteAmount)
        {
            isPouring = false;
            pourComplete = true;
            if (bowlController != null) bowlController.EnableSnapping();

            StartCoroutine(ReturnJar());

            // if bowl already snapped, enable the external timer button now (AdjustableTimer handles its own listener)
            TryEnableTimerButton();
        }
    }

    void TiltJar(bool tilt)
    {
        if (pivotPoint == null) return;

        float targetAngleZ = tilt ? (minTiltAngleZ + maxTiltAngleZ) / 2f : 0f;
        float currentAngleZ = pivotPoint.localEulerAngles.z;
        if (currentAngleZ > 180f) currentAngleZ -= 360f;

        float newAngleZ = Mathf.Lerp(currentAngleZ, targetAngleZ, Time.deltaTime * tiltSpeed);

        if (tilt)
            newAngleZ = Mathf.Clamp(newAngleZ, minTiltAngleZ, maxTiltAngleZ);
        else
            newAngleZ = Mathf.Lerp(newAngleZ, 0f, Time.deltaTime * tiltSpeed);

        pivotPoint.localEulerAngles = new Vector3(0f, 0f, newAngleZ);
    }

    IEnumerator ReturnJar()
    {
        isReturning = true;
        float t = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = defaultPosition != null ? defaultPosition.position : transform.position;

        while (t < 1f)
        {
            t += Time.deltaTime * returnSpeed;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        transform.position = endPos;
        isReturning = false;
    }

    // Called by BowlController when the bowl is snapped onto stove
    public void OnBowlSnappedToStove()
    {
        placedOnStove = true;
        TryEnableTimerButton();
    }

    void TryEnableTimerButton()
    {
        // only show the external enable button when BOTH pourComplete AND bowl is snapped on stove
        if (pourComplete && placedOnStove && enableTimerPanelButton != null)
        {
            enableTimerPanelButton.gameObject.SetActive(true);
        }
    }

    // Allow AdjustableTimer/GameManager to reset jar state
    public void ResetState()
    {
        StopAllCoroutines();
        isPouring = false;
        isReturning = false;
        isDragging = false;
        currentMilkFill = 0f;
        placedOnStove = false;
        pourComplete = false;

        if (milkFillTransform != null)
            milkFillTransform.localScale = new Vector3(fillScaleX, 0f, fillScaleX);

        if (enableTimerPanelButton != null)
            enableTimerPanelButton.gameObject.SetActive(false);

        if (timerPanel != null)
            //timerPanel.SetActive(false);

        if (pivotPoint != null)
            pivotPoint.localEulerAngles = Vector3.zero;
    }
}
