using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ¹èÆ² ¾À¿¡¼­ Ä«µå ÇÕ¼º(·¹½ÃÇÇ ±â¹Ý) ¹Ì¸®º¸±â/»ý¼º °ü¸®.
/// - Singleton
/// - Àç·á(Ä«µå ¿ÀºêÁ§Æ®) ¼±ÅÃ/Åä±Û
/// - º¸À¯ ·¹½ÃÇÇ(CombFormula) ±â¹Ý ÇÁ¸®ºä Ãâ·Â
/// - ÇÁ¸®ºä Å¬¸¯À¸·Î °á°ú ¼±ÅÃ -> ÇÕ¼º È®Á¤(µ¥ÀÌÅÍ+UI ¹Ý¿µ)
/// - ¿¬¼Ó ÇÕ¼º °¡´É
/// </summary>
public class CardFusionState : MonoBehaviour
{
    public static CardFusionState Instance { get; private set; }

    [Header("DB")]
    [SerializeField] private CardFusionDatabaseSO fusionDB;

    [Header("Fusion Result Preview")]
    [SerializeField] private Transform previewSpawnRoot;
    [SerializeField] private GameObject previewCardPrefab;
    [SerializeField] private bool clearPreviewOnNoResult = true;

    [Header("Card UI")]
    [Tooltip("Àç·á·Î ¾²ÀÏ Ä«µåµé ÇÁ¸®ÆÕ")]
    [SerializeField] public GameObject cardPrefab;
    [SerializeField] public RectTransform cardContentArea;
    public List<GameObject> spawnedCards = new List<GameObject>();

    [Header("Debug")]
    [SerializeField] private bool logSelection = true;

    // Àç·áµé(Ä«µå ÄÚµå) - °á°ú °è»ê¿ë(¿ÜºÎ ³ëÃâ¿ë À¯Áö)
    [SerializeField] private List<string> materials = new List<string>(3);

    // ÇöÀç ¶°ÀÖ´Â ÇÁ¸®ºä ÀÎ½ºÅÏ½ºµé
    private readonly List<GameObject> _previewInstances = new List<GameObject>(16);

    // ÇöÀç °è»êµÈ "°¡´ÉÇÑ °á°ú ÄÚµåµé"
    private readonly List<string> _currentResultCodes = new List<string>(8);

    // ¼±ÅÃ °ü¸®(°°Àº ¿ÀºêÁ§Æ® Åä±ÛÀ» À§ÇØ)
    private readonly Dictionary<FusionMaterialClickable, string> _selectedBySource = new();
    private readonly List<FusionMaterialClickable> _selectedOrder = new(3);

    // »óÅÂ
    private enum FusionPhase { SelectingMaterials, SelectingResult }
    private FusionPhase _phase = FusionPhase.SelectingMaterials;
    private bool IsSelectingResult => _phase == FusionPhase.SelectingResult;

    // loop
    public bool isActive = false;
    private Coroutine _fusionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Public control
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void SetActive(bool active)
    {
        if (active == isActive) return;
        isActive = active;

        if (isActive) StartFusionLoop();
        else StopFusionLoop();
    }

    private void StartFusionLoop()
    {
        if (_fusionCoroutine == null)
            _fusionCoroutine = StartCoroutine(FusionLoop());
    }

    private void StopFusionLoop()
    {
        if (_fusionCoroutine != null)
        {
            StopCoroutine(_fusionCoroutine);
            _fusionCoroutine = null;
        }

        isActive = false;
        StartCoroutine(CloseHandsThenReturn());
    }

    public void CancelAndReturn()
    {
        if (_fusionCoroutine != null)
        {
            StopCoroutine(_fusionCoroutine);
            _fusionCoroutine = null;
        }

        isActive = false;
        StartCoroutine(CloseHandsThenReturn());
    }

    private IEnumerator CloseHandsThenReturn()
    {
        // ¼±ÅÃ/ÇÁ¸®ºä/¹öÆÛ ¸ðµÎ Á¤¸®
        ClearPreview();
        ClearMaterials();
        ExitResultSelection();

        ClearAllCards();

        // ±âÁ¸ Èå¸§ À¯Áö
        yield return BattleCameraDirector.Instance.SetShotAndWait(BattleCameraDirector.ShotId.Back);
        BattleManager.Instance.AnotherModeDone();
        yield return null;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Main Loop
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private IEnumerator FusionLoop()
    {
        PopulateCards();
        ClearMaterials();
        ExitResultSelection();

        while (isActive)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelAndReturn();
                yield break;
            }

            // °á°ú ¼±ÅÃ ¸ðµå¸é ¹öÆÛ ¼Òºñ
            if (IsSelectingResult)
                TryConsumeSelectionBuffer();

            yield return null;
        }
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Card UI Spawn
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void ClearAllCards()
    {
        if (cardContentArea != null)
        {
            for (int i = cardContentArea.childCount - 1; i >= 0; i--)
                Destroy(cardContentArea.GetChild(i).gameObject);
        }
        spawnedCards.Clear();
    }

    private void PopulateCards()
    {
        spawnedCards.Clear();

        foreach (string code in BattleManager.Instance.apData.hands)
        {
            var cardGO = Instantiate(cardPrefab, cardContentArea);
            spawnedCards.Add(cardGO);

            if (BattleManager.Instance.apData.isBlinded)
            {
                Transform visualRoot = cardGO.transform.Find("VisualRoot");
                if (visualRoot != null)
                {
                    Transform blood = visualRoot.Find("BloodShed");
                    if (blood != null) blood.gameObject.SetActive(true);
                }
            }

            var info = cardGO.GetComponent<EachCardInfo>();
            if (info == null) { Debug.LogWarning("EachCardInfo ¾øÀ½"); continue; }

            info.cardData = CSVLoader.Instance.GetCardByCode(code);
            info.ApplyCardData();
        }

        Debug.Log("ÇÕ¼ºÀç·á »ý¼º¿Ï·á");
    }

    private void SpawnResultCardUI(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return;
        code = code.Trim();

        var cardGO = Instantiate(cardPrefab, cardContentArea);
        spawnedCards.Add(cardGO);

        if (BattleManager.Instance.apData.isBlinded)
        {
            Transform visualRoot = cardGO.transform.Find("VisualRoot");
            if (visualRoot != null)
            {
                Transform blood = visualRoot.Find("BloodShed");
                if (blood != null) blood.gameObject.SetActive(true);
            }
        }

        var info = cardGO.GetComponent<EachCardInfo>();
        if (info == null)
        {
            Debug.LogWarning("[Fusion] °á°ú Ä«µå¿¡ EachCardInfo ¾øÀ½");
            return;
        }

        var cardData = CSVLoader.Instance.GetCardByCode(code);
        if (cardData == null)
        {
            Debug.LogWarning($"[Fusion] °á°ú Ä«µå ÄÚµå ¸ø Ã£À½: {code}");
            return;
        }

        info.cardData = cardData;
        info.ApplyCardData();
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Material selection (object-based toggle)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public bool ToggleMaterial(FusionMaterialClickable source, string code)
    {
        if (source == null) return false;
        if (string.IsNullOrWhiteSpace(code)) return false;

        code = code.Trim();

        // ÀÌ¹Ì ¼±ÅÃµÈ ¿ÀºêÁ§Æ®¸é ÇØÁ¦
        if (_selectedBySource.ContainsKey(source))
        {
            _selectedBySource.Remove(source);
            _selectedOrder.Remove(source);

            RebuildMaterialsFromSelection();

            // ¼±ÅÃÀÌ ¹Ù²î¾úÀ¸´Ï ÇÁ¸®ºä/¸ðµå °»½Å
            RefreshPreview();

            // 1°³ ³²¾ÒÀ¸¸é ¡°°¡´É ¿©ºÎ¡± ·Î±×
            if (materials.Count == 1)
                LogIfNoCraftableFromSingle(materials[0]);

            // °á°ú ¼±ÅÃ ¸ðµå¿´´Âµ¥ Àç·á°¡ ¹Ù²î¸é ¹öÆÛ ²¿ÀÌ±â ½¬¿ì´Ï
            if (materials.Count < 2)
                ExitResultSelection();

            return false;
        }

        // »õ ¼±ÅÃ
        if (_selectedOrder.Count >= 3)
        {
            Debug.LogWarning("[Fusion] Àç·á´Â ÃÖ´ë 3°³ÀÔ´Ï´Ù.");
            return false;
        }

        _selectedBySource[source] = code;
        _selectedOrder.Add(source);

        RebuildMaterialsFromSelection();

        if (logSelection)
            Debug.Log($"[Fusion] SelectMaterial(Obj): {code} (count={materials.Count})");

        // 1°³ ¼±ÅÃÀÎµ¥ ¾Æ¹« ·¹½ÃÇÇµµ ¾øÀ¸¸é ·Î±×
        if (materials.Count == 1)
            LogIfNoCraftableFromSingle(materials[0]);

        RefreshPreview();
        return true;
    }

    private void RebuildMaterialsFromSelection()
    {
        materials.Clear();
        for (int i = 0; i < _selectedOrder.Count; i++)
        {
            var src = _selectedOrder[i];
            if (src == null) continue;

            if (_selectedBySource.TryGetValue(src, out var c) && !string.IsNullOrWhiteSpace(c))
                materials.Add(c);
        }
    }

    public void ClearMaterials()
    {
        // ÇÏÀÌ¶óÀÌÆ® OFF
        for (int i = 0; i < _selectedOrder.Count; i++)
        {
            var src = _selectedOrder[i];
            if (src != null) src.SetHighlighted(false);
        }

        _selectedBySource.Clear();
        _selectedOrder.Clear();
        materials.Clear();

        RefreshPreview();
    }

    public void UndoLastMaterial()
    {
        if (_selectedOrder.Count == 0) return;

        var last = _selectedOrder[_selectedOrder.Count - 1];
        _selectedOrder.RemoveAt(_selectedOrder.Count - 1);
        if (last != null)
        {
            _selectedBySource.Remove(last);
            last.SetHighlighted(false);
        }

        RebuildMaterialsFromSelection();
        RefreshPreview();
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Preview (owned recipe only)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void RefreshPreview()
    {
        if (fusionDB == null || previewSpawnRoot == null || previewCardPrefab == null)
        {
            ClearPreview();
            ExitResultSelection();
            return;
        }

        // Àç·á 2°³ ¹Ì¸¸ÀÌ¸é ÇÁ¸®ºä ¾øÀ½
        if (materials.Count < 2)
        {
            ClearPreview();
            ExitResultSelection();
            return;
        }

        _currentResultCodes.Clear();
        CollectPossibleOutputs(materials, _currentResultCodes);

        if (_currentResultCodes.Count == 0)
        {
            if (clearPreviewOnNoResult) ClearPreview();
            ExitResultSelection();
            return;
        }

        RebuildPreviewObjects(_currentResultCodes);
        EnterResultSelection();
    }

    private HashSet<string> BuildOwnedRecipeIdSet()
    {
        var set = new HashSet<string>(StringComparer.Ordinal);

        if (BattleManager.Instance == null) return set;

        // BattleManager¿¡ OwnedFusionRecipeIds °ÔÅÍ¸¦ ¸¸µé¾îµÐ »óÅÂ¸¦ °¡Á¤
        var owned = BattleManager.Instance.OwnedFusionRecipeIds;
        if (owned == null) return set;

        for (int i = 0; i < owned.Count; i++)
        {
            var id = owned[i];
            if (!string.IsNullOrWhiteSpace(id))
                set.Add(id.Trim());
        }

        return set;
    }

    private void CollectPossibleOutputs(List<string> currentMaterials, List<string> outputs)
    {
        var owned = BuildOwnedRecipeIdSet();

        // ÇöÀç Àç·á·Î ¿Ï¼ºµÇ´Â ·¹½ÃÇÇ¸¦ ¾ò°í, ±× ·¹½ÃÇÇID¸¦ ³»°¡ º¸À¯ÇßÀ» ¶§¸¸ Ãâ·Â
        if (fusionDB.TryGetRecipe(currentMaterials, out var recipe) && recipe != null)
        {
            if (!string.IsNullOrWhiteSpace(recipe.recipeId) && owned.Contains(recipe.recipeId.Trim()))
            {
                if (!string.IsNullOrWhiteSpace(recipe.outputCode))
                    outputs.Add(recipe.outputCode.Trim());
            }
        }
    }

    private void RebuildPreviewObjects(List<string> resultCodes)
    {
        ClearPreview();

        for (int i = 0; i < resultCodes.Count; i++)
        {
            string code = resultCodes[i];
            if (string.IsNullOrWhiteSpace(code)) continue;

            var cardGO = Instantiate(previewCardPrefab, previewSpawnRoot);

            // WhichCardSelected°¡ ÇÁ¸®ÆÕ¿¡ ¾ø´Ù¸é º¸Àå
            if (cardGO.GetComponent<WhichCardSelected>() == null)
                cardGO.AddComponent<WhichCardSelected>();

            var info = cardGO.GetComponent<EachCardInfo>();
            if (info == null)
            {
                Debug.LogWarning("[Fusion] Preview EachCardInfo ¾øÀ½");
                Destroy(cardGO);
                continue;
            }

            var cardData = CSVLoader.Instance.GetCardByCode(code);
            if (cardData == null)
            {
                Debug.LogWarning($"[Fusion] Preview Ä«µå ÄÚµå ¸ø Ã£À½: {code}");
                Destroy(cardGO);
                continue;
            }

            info.cardData = cardData;
            info.ApplyCardData();

            _previewInstances.Add(cardGO);
        }
    }

    private void ClearPreview()
    {
        for (int i = 0; i < _previewInstances.Count; i++)
        {
            if (_previewInstances[i] != null)
                Destroy(_previewInstances[i]);
        }
        _previewInstances.Clear();
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // SelectionBuffer phase handling
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void EnterResultSelection()
    {
        if (_phase == FusionPhase.SelectingResult) return;

        _phase = FusionPhase.SelectingResult;

        if (SelectionBuffer.Instance != null)
            SelectionBuffer.Instance.BeginSelection();
        else
            Debug.LogWarning("[Fusion] SelectionBuffer.Instance°¡ ¾øÀ½ (°á°ú ¼±ÅÃ ºÒ°¡)");
    }

    private void ExitResultSelection()
    {
        _phase = FusionPhase.SelectingMaterials;

        // ¹öÆÛ ²¿ÀÓ ¹æÁö¿ë ¸®¼Â
        if (SelectionBuffer.Instance != null)
            SelectionBuffer.Instance.BeginSelection();
    }

    private void TryConsumeSelectionBuffer()
    {
        var buf = SelectionBuffer.Instance;
        if (buf == null) return;

        if (!buf.isSelectingDone) return;

        string resultCode = buf.selectedResultCode;
        if (string.IsNullOrWhiteSpace(resultCode))
        {
            buf.BeginSelection();
            return;
        }

        resultCode = resultCode.Trim();

        // ¾ÈÀüÀåÄ¡: ÇöÀç ÈÄº¸ Áß ÇÏ³ª¸¸ Çã¿ë
        if (!_currentResultCodes.Contains(resultCode))
        {
            Debug.LogWarning($"[Fusion] ¼±ÅÃµÈ °á°ú°¡ ÇöÀç ÈÄº¸¿¡ ¾øÀ½: {resultCode}");
            buf.BeginSelection();
            return;
        }

        ExecuteFusion(resultCode);

        // ´ÙÀ½ ÇÕ¼ºÀ» À§ÇØ ¹öÆÛ ¸®¼Â
        buf.BeginSelection();
        ExitResultSelection();
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Execute Fusion (data + UI)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void ExecuteFusion(string resultCode)
    {
        // ½º³À¼¦
        var usedCodes = new List<string>(materials);
        var usedSources = new List<FusionMaterialClickable>(_selectedOrder);

        var apData = BattleManager.Instance.apData;

        // 1) apData ¾÷µ¥ÀÌÆ®
        for (int i = 0; i < usedCodes.Count; i++)
        {
            string c = usedCodes[i];
            if (string.IsNullOrWhiteSpace(c)) continue;

            apData.hands.Remove(c);

            //  ¿©±â ÇÊµå¸íÀº ³Ê ÇÁ·ÎÁ§Æ®¿¡ ¸ÂÃç¶ó (trash / trahs µî)
            apData.trash.Add(c);
        }

        apData.hands.Add(resultCode);

        // 2) UI¿¡¼­ Àç·á Ä«µå Á¦°Å
        for (int i = 0; i < usedSources.Count; i++)
        {
            var src = usedSources[i];
            if (src == null) continue;

            var go = src.gameObject;
            if (go != null)
            {
                spawnedCards.Remove(go);
                Destroy(go);
            }
        }

        // 3) ÇÁ¸®ºä Á¦°Å
        ClearPreview();
        _currentResultCodes.Clear();

        // 4) ¼±ÅÃ »óÅÂ ÃÊ±âÈ­
        ClearMaterials();

        // 5) °á°ú Ä«µå UI »ý¼º
        SpawnResultCardUI(resultCode);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // Single material log
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void LogIfNoCraftableFromSingle(string singleCode)
    {
        if (fusionDB == null) return;
        if (string.IsNullOrWhiteSpace(singleCode)) return;
        singleCode = singleCode.Trim();

        var owned = BuildOwnedRecipeIdSet();
        var recipes = fusionDB.Recipes;
        if (owned.Count == 0 || recipes == null || recipes.Count == 0)
        {
            Debug.Log("±× Ä«µå·Î´Â ¾Æ¹«°Íµµ ¸ø¸¸µéÁö·Õ~~");
            return;
        }

        for (int i = 0; i < recipes.Count; i++)
        {
            var r = recipes[i];
            if (r == null) continue;

            if (string.IsNullOrWhiteSpace(r.recipeId)) continue;
            if (!owned.Contains(r.recipeId.Trim())) continue;

            if (r.inputCodes == null) continue;

            for (int k = 0; k < r.inputCodes.Count; k++)
            {
                var inCode = r.inputCodes[k];
                if (string.IsNullOrWhiteSpace(inCode)) continue;

                if (string.Equals(inCode.Trim(), singleCode, StringComparison.Ordinal))
                    return; // ÇÏ³ª¶óµµ ÀÖÀ¸¸é ·Î±× X
            }
        }

        Debug.Log("±× Ä«µå·Î´Â ¾Æ¹«°Íµµ ¸ø¸¸µéÁö·Õ~~");
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // External (optional)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public IReadOnlyList<string> GetCurrentMaterials() => materials;
    public IReadOnlyList<string> GetCurrentResultCodes() => _currentResultCodes;
}
