using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class OpenCardUI : MonoBehaviour
{
    [Header("Prefabs & Areas")]
    [Tooltip("액션 카드용 프리팹")]
    public GameObject card_action;           // 카드 프리팹
    [Tooltip("서포트 카드용 프리팹")]
    public GameObject card_support;
    [Tooltip("카드들이 배치될 영역 (GridLayoutGroup 필요)")]
    public RectTransform pageArea;          // PageArea (GridLayoutGroup 부착)
    [Tooltip("전체 카드 UI 패널 (CanvasGroup 필수)")]
    public GameObject Panel;

    [Header("Navigation UI")]
    public Button prevButton;
    public Button nextButton;
    public TMP_Text pageLabel;              // 현재 페이지 표시 텍스트

    [Header("Grid Settings (2×3 페이지)")]
    public Vector2 cellSize = new Vector2(300, 400);
    public Vector2 spacing = new Vector2(40, 40);
    public int columns = 3;                 // 3 열 고정
    private const int PAGE_CAPACITY = 6;    // 2×3

    private readonly List<Card> _cards = new(); // 현재 표시 대상 카드들
    private int _currentPage = 0;
    private int _totalPages = 0;

    private GridLayoutGroup _grid;

    [Header("개발자 모드")]
    [Tooltip("서포트 카드(cardType != 0)를 표시할지 여부")]
    public bool dev = true;

    [Header("Owner Info")]
    [Tooltip("이 카드창이 참조할 캐릭터 ID (예: \"player\")")]
    public string ownerCharacterId = "player";

    private void Awake()
    {
        // Grid 설정
        _grid = pageArea.GetComponent<GridLayoutGroup>();
        if (_grid == null)
            _grid = pageArea.gameObject.AddComponent<GridLayoutGroup>();

        _grid.cellSize = cellSize;
        _grid.spacing = spacing;
        _grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        _grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _grid.constraintCount = columns;

        // 네비 버튼 바인딩
        if (prevButton) prevButton.onClick.AddListener(PrevPage);
        if (nextButton) nextButton.onClick.AddListener(NextPage);

        // 시작 시 패널 비활성(원하면)
        if (Panel != null)
            Panel.SetActive(false);
    }

    private void Update()
    {
        // 카드창 열기: Panel이 꺼져 있을 때
        if (!Panel.activeSelf && (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)))
        {
            CardLoad();
        }

        // 카드창 닫기: Panel이 켜져 있을 때
        if (Panel.activeSelf &&
            (Input.GetKeyDown(KeyCode.Escape) ))
        {
            CloseCardUI();
        }
    }

    /// <summary>
    /// 카드 UI를 열고, 현재 캐릭터가 보유한 카드 목록을 기준으로 페이지 렌더
    /// </summary>
    public void CardLoad()
    {
        if (Panel == null)
        {
            Debug.LogWarning("[OpenCardUI] Panel이 설정되어 있지 않습니다.");
            return;
        }

        // 패널 활성화
        Panel.SetActive(true);

        // CanvasGroup 페이드 인
        var cg = Panel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = Panel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        cg.DOFade(1f, 0.25f).SetEase(Ease.OutQuad);

        // 현재 세이브 데이터에서 카드 목록 로드
        LoadOwnedCardsFromSave();

        // 페이지 정보 갱신
        _totalPages = (_cards.Count == 0)
            ? 0
            : Mathf.CeilToInt(_cards.Count / (float)PAGE_CAPACITY);

        _currentPage = 0; // 열 때는 항상 첫 페이지부터

        RenderPage(_currentPage);
    }

    /// <summary>
    /// GameStateManager → CharacterState.cardIds 를 기반으로
    /// 실제 Card 메타데이터 목록(_cards)을 채운다.
    /// </summary>
    private void LoadOwnedCardsFromSave()
    {
        _cards.Clear();

        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning("[OpenCardUI] GameStateManager.Instance가 없습니다.");
            return;
        }

        // 캐릭터 상태 가져오기
        CharacterState ownerState = GameStateManager.Instance.GetCharacterState(ownerCharacterId);
        if (ownerState == null)
        {
            Debug.LogWarning($"[OpenCardUI] CharacterState를 찾을 수 없습니다. ID: {ownerCharacterId}");
            return;
        }

        // 카드 메타 DB(CSVLoader) 확인
        if (CSVLoader.Instance == null || CSVLoader.Instance.runtimeCardSO == null ||
            CSVLoader.Instance.runtimeCardSO.cards == null ||
            CSVLoader.Instance.runtimeCardSO.cards.Length == 0)
        {
            Debug.LogWarning("[OpenCardUI] 카드 메타 데이터(runtimeCardSO)가 비어있습니다.");
            return;
        }

        // 보유 카드 ID 목록을 순회하며, 매칭되는 Card 메타를 찾아 리스트에 추가
        foreach (var code in ownerState.cardIds)
        {
            if (string.IsNullOrWhiteSpace(code))
                continue;

            Card card = CSVLoader.Instance.GetCardByCode(code);
            if (card != null)
            {
                _cards.Add(card); // 중복 코드가 있으면 여러 장으로 취급
            }
            else
            {
                Debug.LogWarning($"[OpenCardUI] cardIds에 '{code}'가 있지만 카드 메타에서 찾지 못했습니다.");
            }
        }

        Debug.Log($"[OpenCardUI] 소지 카드 로드 완료: {_cards.Count}장 (owner={ownerCharacterId})");
    }

    /// <summary>
    /// 특정 페이지 인덱스의 카드들을 Grid에 인스턴스
    /// </summary>
    private void RenderPage(int pageIndex)
    {
        ClearPage();

        if (_cards.Count == 0)
        {
            UpdateNav(0, 0);
            Debug.Log("[OpenCardUI] 소지 카드가 없습니다.");
            return;
        }

        int start = pageIndex * PAGE_CAPACITY;
        int end = Mathf.Min(start + PAGE_CAPACITY, _cards.Count);

        for (int i = start; i < end; i++)
        {
            Card card = _cards[i];
            if (card == null) continue;

            GameObject go = null;

            // cardType 0 → 액션 카드, 그 외 → 서포트 카드(단 dev 옵션에 따라)
            if (card.cardType == 0)
            {
                if (card_action == null)
                {
                    Debug.LogWarning("[OpenCardUI] card_action 프리팹이 설정되어 있지 않습니다.");
                    continue;
                }
                go = Instantiate(card_action, pageArea);
            }
            else
            {
                if (!dev)     // 개발자 모드가 아니면 서포트 카드 패스
                    continue;

                if (card_support == null)
                {
                    Debug.LogWarning("[OpenCardUI] card_support 프리팹이 설정되어 있지 않습니다.");
                    continue;
                }
                go = Instantiate(card_support, pageArea);
            }

            go.transform.localScale = Vector3.one; // 안전 보정

            var info = go.GetComponent<EachCardInfo>();
            if (info != null)
            {
                info.cardData = card;
                info.ApplyCardData();
            }
            else
            {
                Debug.LogWarning("[OpenCardUI] EachCardInfo 컴포넌트가 프리팹에 없습니다.");
            }
        }

        UpdateNav(pageIndex, _totalPages);
        Debug.Log($"[OpenCardUI] 페이지 {pageIndex + 1}/{_totalPages} 렌더 (카드 {end - start}장).");
    }

    /// <summary>
    /// Grid 내 기존 카드 오브젝트 삭제
    /// </summary>
    private void ClearPage()
    {
        for (int i = pageArea.childCount - 1; i >= 0; i--)
        {
            Destroy(pageArea.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 페이지 네비게이션 UI 상태 갱신
    /// </summary>
    private void UpdateNav(int pageIndex, int total)
    {
        if (prevButton) prevButton.interactable = (total > 0) && pageIndex > 0;
        if (nextButton) nextButton.interactable = (total > 0) && pageIndex < total - 1;

        if (pageLabel)
            pageLabel.text = (total == 0) ? "0 / 0" : $"{pageIndex + 1} / {total}";
    }

    public void NextPage()
    {
        if (_currentPage < _totalPages - 1)
        {
            _currentPage++;
            RenderPage(_currentPage);
        }
    }

    public void PrevPage()
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            RenderPage(_currentPage);
        }
    }

    private void CloseCardUI()
    {
        var cg = Panel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = Panel.AddComponent<CanvasGroup>();

        // 페이드아웃
        cg.DOFade(0f, 0.2f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            // 카드 오브젝트 전부 삭제
            ClearPage();

            // 패널 비활성화
            Panel.SetActive(false);
        });
    }

}
