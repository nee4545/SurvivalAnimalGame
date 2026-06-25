using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(GridLayoutGroup))]
public class GridScrollContentFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private GridLayoutGroup grid;

    [SerializeField] private bool updateEveryFrameInPlayMode = true;

    private void Awake()
    {
        Cache();
    }

    private void OnEnable()
    {
        Cache();
        UpdateContentHeight();
    }

    private void OnTransformChildrenChanged()
    {
        Cache();
        UpdateContentHeight();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || updateEveryFrameInPlayMode)
            UpdateContentHeight();
    }

    private void Cache()
    {
        if (!rectTransform)
            rectTransform = GetComponent<RectTransform>();

        if (!grid)
            grid = GetComponent<GridLayoutGroup>();
    }

    public void UpdateContentHeight()
    {
        Cache();

        if (!rectTransform || !grid)
            return;

        int childCount = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf)
                childCount++;
        }

        int columns = Mathf.Max(1, grid.constraintCount);
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)childCount / columns));

        float height =
            grid.padding.top +
            grid.padding.bottom +
            rows * grid.cellSize.y +
            Mathf.Max(0, rows - 1) * grid.spacing.y;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
}
