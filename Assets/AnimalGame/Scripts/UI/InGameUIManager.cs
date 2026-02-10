using UnityEngine;

public class InGameUIManager : MonoBehaviour
{
    [Header("UI Screens")]
    [SerializeField] private GameObject minionsUIObject;
    [SerializeField] private GameObject statsUIObject;
    [SerializeField] private Terresquall.VirtualJoystick virtualJoystick;

    private bool statCardsBound = false;

    private void Awake()
    {
        // Ensure clean start state
        CloseAll();
    }

    private void BindAllStatCards()
    {
        var player = FindObjectOfType<CCActor>();

        if(statsUIObject != null) 
        {
            var statCards = statsUIObject.GetComponentsInChildren<UIStatCard>();

            foreach(UIStatCard statCard in statCards)
            {
                statCard.Bind(player);
            }
        }
    }

    // ---------- Public API (Button Hooks) ----------

    public void OpenMinions()
    {
        CloseAll();
        minionsUIObject.SetActive(true);
        SetJoystick(false);
    }

    public void OpenStats()
    {
        CloseAll();
        statsUIObject.SetActive(true);
        if(!statCardsBound)
        {
            BindAllStatCards();
            statCardsBound = true;
        }
        SetJoystick(false);
    }

    public void CloseMinions()
    {
        minionsUIObject.SetActive(false);
        CheckJoystickState();
    }

    public void CloseStats()
    {
        statsUIObject.SetActive(false);
        CheckJoystickState();
    }

    public void CloseAll()
    {
        minionsUIObject.SetActive(false);
        statsUIObject.SetActive(false);
        SetJoystick(true);
    }

    private void CheckJoystickState()
    {
        // If no UI is open, re-enable joystick
        bool anyUIOpen = minionsUIObject.activeSelf || statsUIObject.activeSelf;
        SetJoystick(!anyUIOpen);
    }

    private void SetJoystick(bool active)
    {
        if (virtualJoystick != null)
            virtualJoystick.SetJoystickActive(active);
    }
}
