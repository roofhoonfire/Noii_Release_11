using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StatWindowUIPage1 : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("스탯 윈도우 전체 패널(Active On/Off 대상)")]
    [SerializeField] private GameObject panel;

    [Tooltip("페이드용 CanvasGroup (없으면 자동 추가)")]
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.20f;

    [Header("Owner Info")]
    [Tooltip("이 UI가 참조할 캐릭터 ID (예: \"player\")")]
    [SerializeField] private string ownerCharacterId = "player";

    [Header("Stats Text (TMP)")]
    [SerializeField] private TMP_Text strText;
    [SerializeField] private TMP_Text dexText;
    [SerializeField] private TMP_Text defText;
    [SerializeField] private TMP_Text intText;

    [Header("Items UI")]
    [Tooltip("아이템 프리팹(EachItemInfo 포함)")]
    [SerializeField] private GameObject itemPrefab;

    [Tooltip("아이템들이 배치될 영역 (GridLayoutGroup 추천)")]
    [SerializeField] private RectTransform pageArea;

    [Header("Grid Settings (Paged)")]
    [SerializeField] private Vector2 cellSize = new Vector2(300, 50);
    [SerializeField] private Vector2 spacing = new Vector2(40, 40);
    [SerializeField] private int columns = 2;
    private const int PAGE_CAPACITY = 8;

    [Header("Navigation UI")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text pageLabel;

    private readonly List<Item> _items = new();
    private int _currentPage = 0;
    private int _totalPages = 0;

    private GridLayoutGroup _grid;
    private Tween _fadeTween;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        // Panel 기본 비활성
        if (panel != null)
            panel.SetActive(false);

        // CanvasGroup 준비
        if (panel != null && canvasGroup == null)
            canvasGroup = panel.GetComponent<CanvasGroup>();
        if (panel != null && canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // Grid 설정
        if (pageArea != null)
        {
            _grid = pageArea.GetComponent<GridLayoutGroup>();
            if (_grid == null)
                _grid = pageArea.gameObject.AddComponent<GridLayoutGroup>();

            _grid.cellSize = cellSize;
            _grid.spacing = spacing;
            _grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = columns;
        }

        // 네비 버튼 바인딩
        if (prevButton) prevButton.onClick.AddListener(PrevPage);
        if (nextButton) nextButton.onClick.AddListener(NextPage);

        UpdateNav(0, 0);
    }

    private void Update()
    {
        // 열기/닫기: Tab 토글
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!IsOpen) Open();
            else Close();
        }

        // 닫기: Esc
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Open()
    {
        if (panel == null)
        {
            Debug.LogWarning("[StatWindowUIPage1] panel이 설정되어 있지 않습니다.");
            return;
        }

        panel.SetActive(true);

        // 페이드 인
        StartFade(1f, fadeInDuration);

        // 열릴 때 "페이지1" 렌더링: 스탯 + 아이템
        RenderPage1();
    }

    public void Close()
    {
        if (panel == null)
            return;

        // 페이드 아웃 후 비활성 + 아이템 제거
        StartFade(0f, fadeOutDuration, onComplete: () =>
        {
            ClearPage();
            panel.SetActive(false);
        });
    }

    private void RenderPage1()
    {
        // 1) 상태 로드
        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning("[StatWindowUIPage1] GameStateManager.Instance가 없습니다.");
            UpdateStatsText(null);
            _items.Clear();
            ClearPage();
            UpdateNav(0, 0);
            return;
        }

        var state = GameStateManager.Instance.GetCharacterState(ownerCharacterId);
        if (state == null)
        {
            Debug.LogWarning($"[StatWindowUIPage1] CharacterState를 찾을 수 없습니다. ID: {ownerCharacterId}");
            UpdateStatsText(null);
            _items.Clear();
            ClearPage();
            UpdateNav(0, 0);
            return;
        }

        // 2) 스탯 텍스트 갱신
        UpdateStatsText(state);

        // 3) 아이템 리스트 로드(기존 방식 동일)
        LoadOwnedItemsFromSave(state);

        // 4) 페이지 계산 + 첫 페이지 렌더
        _totalPages = (_items.Count == 0) ? 0 : Mathf.CeilToInt(_items.Count / (float)PAGE_CAPACITY);
        _currentPage = 0;
        RenderItemsPage(_currentPage);
    }

    private void UpdateStatsText(CharacterState state)
    {
        if (state == null)
        {
            if (strText) strText.text = "-";
            if (dexText) dexText.text = "-";
            if (defText) defText.text = "-";
            if (intText) intText.text = "-";
            return;
        }

        if (strText) strText.text = state.str.ToString();
        if (dexText) dexText.text = state.dex.ToString();
        if (defText) defText.text = state.def.ToString();
        if (intText) intText.text = state.intel.ToString();
    }

    private void LoadOwnedItemsFromSave(CharacterState ownerState)
    {
        _items.Clear();

        if (ownerState == null)
            return;

        if (CSVLoader.Instance == null || CSVLoader.Instance.runtimeItemSO == null ||
            CSVLoader.Instance.runtimeItemSO.items == null ||
            CSVLoader.Instance.runtimeItemSO.items.Length == 0)
        {
            Debug.LogWarning("[StatWindowUIPage1] item 메타 데이터(runtimeItemSO)가 비어있습니다.");
            return;
        }

        foreach (var code in ownerState.itemIds)
        {
            if (string.IsNullOrWhiteSpace(code))
                continue;

            Item item = CSVLoader.Instance.GetItemByCode(code);
            if (item != null)
            {
                _items.Add(item);
            }
            else
            {
                Debug.LogWarning($"[StatWindowUIPage1] itemIds에 '{code}'가 있지만 아이템 메타에서 찾지 못했습니다.");
            }
        }

        Debug.Log($"[StatWindowUIPage1] 소지 아이템 로드 완료: {_items.Count}개 (owner={ownerCharacterId})");
    }

    private void RenderItemsPage(int pageIndex)
    {
        ClearPage();

        if (pageArea == null)
        {
            Debug.LogWarning("[StatWindowUIPage1] pageArea가 비어있습니다.");
            UpdateNav(0, 0);
            return;
        }

        if (_items.Count == 0)
        {
            UpdateNav(0, 0);
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogWarning("[StatWindowUIPage1] itemPrefab이 설정되어 있지 않습니다.");
            UpdateNav(pageIndex, _totalPages);
            return;
        }

        int start = pageIndex * PAGE_CAPACITY;
        int end = Mathf.Min(start + PAGE_CAPACITY, _items.Count);

        for (int i = start; i < end; i++)
        {
            var item = _items[i];
            if (item == null) continue;

            var go = Instantiate(itemPrefab, pageArea);
            go.transform.localScale = Vector3.one;

            var info = go.GetComponent<EachItemInfo>();
            if (info != null)
            {
                info.itemData = item;
                info.ApplyItemData();
            }
            else
            {
                Debug.LogWarning("[StatWindowUIPage1] EachItemInfo 컴포넌트가 프리팹에 없습니다.");
            }
        }

        UpdateNav(pageIndex, _totalPages);
    }

    private void ClearPage()
    {
        if (pageArea == null) return;

        for (int i = pageArea.childCount - 1; i >= 0; i--)
            Destroy(pageArea.GetChild(i).gameObject);
    }

    private void UpdateNav(int pageIndex, int total)
    {
        if (prevButton) prevButton.interactable = (total > 0) && pageIndex > 0;
        if (nextButton) nextButton.interactable = (total > 0) && pageIndex < total - 1;
        if (pageLabel) pageLabel.text = (total == 0) ? "0 / 0" : $"{pageIndex + 1} / {total}";
    }

    public void NextPage()
    {
        if (!IsOpen) return;

        if (_currentPage < _totalPages - 1)
        {
            _currentPage++;
            RenderItemsPage(_currentPage);
        }
    }

    public void PrevPage()
    {
        if (!IsOpen) return;

        if (_currentPage > 0)
        {
            _currentPage--;
            RenderItemsPage(_currentPage);
        }
    }

    private void StartFade(float targetAlpha, float duration, System.Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        _fadeTween?.Kill();

        // 켜질 때는 먼저 레이캐스트 켜고, 꺼질 때는 끝나고 끈다
        if (targetAlpha > 0.01f)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        _fadeTween = canvasGroup.DOFade(targetAlpha, duration)
            .SetEase(targetAlpha > 0.01f ? Ease.OutQuad : Ease.InQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                bool visible = targetAlpha > 0.01f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
                _fadeTween = null;
                onComplete?.Invoke();
            });
    }
}
