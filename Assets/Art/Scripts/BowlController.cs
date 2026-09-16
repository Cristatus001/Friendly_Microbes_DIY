using UnityEngine;
using System.Collections;

public class BowlController : MonoBehaviour
{
    [Header("Snap / MoveAway")]
    public Transform snapTarget;         // stove snap position
    public float snapDistance = 0.5f;
    public bool canSnap = false;         // enabled by MilkJarController after pour

    public bool canMoveAway = false;     // enabled by timer when finished (player must drag to moveAwayTarget)
    public Transform moveAwayTarget;     // where bowl should be placed after boil
    public float moveAwaySnapDistance = 0.5f;

    [Header("Pour Settings")]
    public float pourRotationAngleZ = 45f;  // tilt angle while pouring
    public float pourDuration = 1.5f;       // how long to stay tilted
    public float pourSpeed = 5f;            // how fast to rotate

    [Header("References")]
    public MilkJarController milkJarController;

    // internal
    private bool isDragging = false;
    private bool interactionsLocked = false; // when true, dragging disabled
    private Vector3 offset;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null) Debug.LogWarning("MainCamera not found. Make sure there is an active camera.");
    }

    void OnMouseDown()
    {
        // block pick-up while locked or timer running
        if (interactionsLocked || AdjustableTimer.TimerRunningGlobal) return;

        Vector3 mp = Input.mousePosition;
        float z = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
        mp.z = z;
        Vector3 world = mainCam.ScreenToWorldPoint(mp);
        offset = transform.position - world;
        isDragging = true;
    }

    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        // SNAP to stove (only allowed if canSnap AND timer NOT running)
        if (canSnap && !AdjustableTimer.TimerRunningGlobal && snapTarget != null &&
            Vector3.Distance(transform.position, snapTarget.position) <= snapDistance)
        {
            transform.position = snapTarget.position;
            if (milkJarController != null)
                milkJarController.OnBowlSnappedToStove();

            // lock the bowl immediately once snapped to stove until timer finishes
            interactionsLocked = true;
        }

        // Pour action: if canMoveAway and dropped onto moveAwayTarget and timer finished -> rotate bowl temporarily
        if (canMoveAway && !AdjustableTimer.TimerRunningGlobal && moveAwayTarget != null &&
            Vector3.Distance(transform.position, moveAwayTarget.position) <= moveAwaySnapDistance)
        {
            transform.position = moveAwayTarget.position;
            StopAllCoroutines();
            StartCoroutine(PourMilkRoutine());
        }
    }

    void Update()
    {
        // while timer is running, don’t drag at all
        if (AdjustableTimer.TimerRunningGlobal)
        {
            isDragging = false;
            return;
        }

        if (isDragging && !interactionsLocked)
        {
            Vector3 mp = Input.mousePosition;
            float z = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
            mp.z = z;
            Vector3 world = mainCam.ScreenToWorldPoint(mp);
            transform.position = world + offset;
        }
    }

    public void EnableSnapping()
    {
        canSnap = true;
    }

    public void EnableMoveAway()
    {
        // called when timer completes
        canMoveAway = true;
        // 🔴 disable snapping after boil so player cannot snap back onto stove
        canSnap = false;
        // unlock dragging so player can now move bowl to moveAwayTarget
        interactionsLocked = false;
    }

    public void SetTimerRunning(bool running)
    {
        interactionsLocked = running;
        if (running) isDragging = false;
    }

    public void LockDragging()
    {
        interactionsLocked = true;
    }

    public void ResetState()
    {
        canSnap = false;
        canMoveAway = false;
        interactionsLocked = false;
        isDragging = false;
        StopAllCoroutines();
    }

    IEnumerator PourMilkRoutine()
    {
        Vector3 startEuler = transform.localEulerAngles;
        Vector3 pourEuler = startEuler;
        pourEuler.z = pourRotationAngleZ;

        // rotate to pour
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * pourSpeed;
            transform.localEulerAngles = Vector3.Lerp(startEuler, pourEuler, t);
            yield return null;
        }

        yield return new WaitForSeconds(pourDuration);

        // rotate back upright
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * pourSpeed;
            transform.localEulerAngles = Vector3.Lerp(pourEuler, startEuler, t);
            yield return null;
        }
    }
}
