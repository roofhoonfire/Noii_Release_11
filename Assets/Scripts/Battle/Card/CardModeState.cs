// CardModeState.cs (원파일 통짜 교체본)
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using System.Security.Policy;

static class TmpParseUtil
{
    static readonly Regex RichTag = new Regex("<.*?>", RegexOptions.Compiled);

    public static bool TryParseInt(TMP_Text t, out int value)
    {
        value = 0;
        if (t == null || string.IsNullOrEmpty(t.text)) return false;
        var raw = RichTag.Replace(t.text, "");   // <color=...>12</color> → 12
        return int.TryParse(raw, out value);
    }
}


public enum Elem
{

    elem_fire, elem_ice, elem_wind, elem_earth
}
public class InGameAPData
{
    //인게임에서만 영향을 주는 값들을 적어 둔다


    public int permDef =0;
    public int tempDef = 0;
    public int permCast = 0;
    public int tempCast = 0;
    public int permRum = 0;
    public int tempRum = 0;
    public int permDam = 0;
    public int tempDam = 0;
    public bool isBlinded =false;
    public int CastingMinumum = 1;
    public List<string> deckList = new List<string>();
    public List<string> hands = new List<string>();
    public List<string> trash = new List<string>();

    public List<Elem> elements = new List<Elem>();
    public int currentChakra=0; //나
    public int maxChakraPoint=3;

}



public class CardModeState : MonoBehaviour
{
    public static CardModeState Instance;

    [Header("Card UI")]
    public GameObject cardPrefab;
    public RectTransform cardContentArea;
    [Header("Input Locks")]
    [SerializeField] private bool cancelLocked = false;  // ← 필드(Inspector 노출)
    public bool CancelLocked => cancelLocked;
    // Buffer
    public ActionData curAction;
    public int actionClockBuffer = 0;
    public List<GameObject> spawnedCards = new List<GameObject>();

    public bool isActive = false;

    public bool isInteractiveSession = false; // 추가: 인터랙티브(ChooseLoop) 진입 여부


    private Coroutine _selectCardCoroutine;
    private int _populateVersion = 0;


    [Header("Clear Hand Anim")]
    [SerializeField] private CurvedHandLayoutTween handLayout; // Hands에 붙인 컴포넌트 연결

    [SerializeField] private float gatherDuration = 0.12f;     // 펼쳐진 손패 -> 한 점으로 모으기
    [SerializeField] private float toSpawnDuration = 0.12f;    // 모인 후 -> 스폰 위치로 이동
    [SerializeField] private Ease gatherEase = Ease.InQuad;
    [SerializeField] private Ease toSpawnEase = Ease.InQuad;

    [SerializeField] private Vector2 gatherPoint = Vector2.zero;
   
    private Coroutine _clearRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        //나중에 바꿔줘라 ㅎ

      /*  var sourceDeck = new List<string> { "c1", "c2", "c2" };
        if (sourceDeck == null)
        {
            Debug.LogError("CardModeState - source deckList is NULL");
            return;
        }
        apData.deckList = new List<string>(sourceDeck);

        // 3) 핸드 초기화
        apData.hands = new List<string>();

        // 뽑을 카드 수 (덱이 5장 미만이면 가능한 만큼)
        int drawCount = Mathf.Min(5, apData.deckList.Count);

        System.Random rng = new System.Random();

        for (int i = 0; i < drawCount; i++)
        {
            int idx = rng.Next(0, apData.deckList.Count);

            string pickedCard = apData.deckList[idx];

            // 핸드에 추가
            apData.hands.Add(pickedCard);

            // 덱에서 제거
            apData.deckList.RemoveAt(idx);
        }

        Debug.Log($"[CardModeState] 최초 핸드 초기화 완료: {apData.hands.Count}장 뽑음");
      */
        // 손패 셋팅 
    }

    public void SetActive(bool active)
    {
        // 기존 호출 호환: ChooseLoop 경로는 인터랙티브로 취급
        SetActive(active,  interactive: true);
    }

    // ★ 추가 오버로드: 진입 성격 명시
    public void SetActive(bool active, bool interactive)
    {
        if (active == isActive) return;
        isActive = active;
        isInteractiveSession = interactive;

        if (isActive)
        {
            cancelLocked = false;

         
            StartSelectCardLoop();
        }
        else StopSelectCardLoop(null);
    }
    private void StartSelectCardLoop()
    {
        // ★ 인터랙티브일 때만 생각 모션 트리거
     
          //    var anim = meGo.GetComponentInChildren<Animator>();
            //    if (anim) anim.SetTrigger("Trig_Think");
       

        if (_selectCardCoroutine == null)
            _selectCardCoroutine = StartCoroutine(SelectCardFlow());
    }

    private IEnumerator SelectCardFlow()
    {
        // 1) 손 등장 애니 (handCount = 손패 장수)
      //  yield return BothArmsAnimator.Instance.PlayEnter(handCount);

        // 2) 카드 생성 (등장 완료 후 실행)
        PopulateCards();
        InitBuffer();
        // ActionPacketUpgrade(apdata);

        // 3) 선택 루프
        yield return StartCoroutine(SelectCardLoop());

        _selectCardCoroutine = null;
    }

    public void InitBuffer()
    {
        curAction = new ActionData();
      //  curAction.effects = new List<CardEffect>();
    }

    private IEnumerator SelectCardLoop()
    {
        while (isActive)
        {
            // 손/전환 애니 중에는 입력 무시
        //   if (UIInputGate.IsLocked)
       //     {
           //     yield return null;
        //        continue;
      //      }

            // ESC 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!cancelLocked) CancelAndReturn();     // ← 필드로 체크
                else Debug.Log("[CardMode] Cancel locked after using support/bless.");
                yield break;
            }

            SelectCard();
            yield return null;
        }
    }

    public void CancelAndReturn()
    {
        if (cancelLocked) { Debug.Log("[CardMode] Cancel locked; ignoring."); return; }

        if (_selectCardCoroutine != null)
        {
            StopCoroutine(_selectCardCoroutine);
            _selectCardCoroutine = null;
        }
        isActive = false;

        // 카드 제거 → 손 사라짐 애니 → ChooseLoop 복귀
        StartCoroutine(CloseHandsThenReturn());
    }
    private IEnumerator CloseHandsThenReturn()
    {
        //  BattleCameraDirector.Instance.EnterSelectShot();
        ClearAllCardsandAnotherModeDone();
        //  yield return BothArmsAnimator.Instance.PlayExit();

        //if (LocalState.Instance?.alim) LocalState.Instance.alim.SetActive(false);

        // ★ 인터랙티브일 때만 Think_Done 트리거 + ChooseLoop 복귀
        /*  if (isInteractiveSession)
          {
              var me = PhotonNetwork.LocalPlayer.ActorNumber;
              if (LocalState.Instance?.PlayerObDic != null &&
                  LocalState.Instance.PlayerObDic.TryGetValue(me, out var meGo) &&
                  meGo != null)
              {
                  var anim = meGo.GetComponentInChildren<Animator>();
                  if (anim) anim.SetTrigger("Trig_Think_Done");
              }

              LocalState.Instance.ReturnToChooseLoop();
          }*/

        yield return null;
    }

    public void StopSelectCardLoop(ActionData action)
    {
        if (_selectCardCoroutine != null)
        {
            StopCoroutine(_selectCardCoroutine);
            _selectCardCoroutine = null;
        }
        isActive = false;

        // ★ 인터랙티브일 때만 Think_Done 트리거
        if (isInteractiveSession)
        {
          //  var me = PhotonNetwork.LocalPlayer.ActorNumber;
        //    if (LocalState.Instance?.PlayerObDic != null &&
        //        LocalState.Instance.PlayerObDic.TryGetValue(me, out var meGo) &&
        //        meGo != null)
            {
            //    var anim = meGo.GetComponentInChildren<Animator>();
          //      if (anim) anim.SetTrigger("Trig_Think_Done");
            }
        }

        StartCoroutine(CloseHandsThenSubmit(action));
    }


    private IEnumerator CloseHandsThenSubmit(ActionData action)
    {
        ClearAllCardsandAnotherModeDone();
        BattleManager.Instance.SubmitPlayerAction(action, action.actionClock);
        yield return null;

    }



    public void PopulateCards()
    {
        spawnedCards.Clear();

        foreach (string code in BattleManager.Instance.apData.hands)
        {
            var cardGO = Instantiate(cardPrefab, cardContentArea);
            spawnedCards.Add(cardGO);

            var info = cardGO.GetComponent<EachCardInfo>();
            if (info == null) { Debug.LogWarning("EachCardInfo 없음"); continue; }

            info.cardData = CSVLoader.Instance.GetCardByCode(code);
            info.ApplyCardData();
        }

        //  생성 위치(덱) -> 손패(부채꼴)로 펼치기
        handLayout.SpawnThenTweenToHand(spawnedCards);

        Debug.Log("손패 생성완료");
    }

    public void ClearAllCardsandAnotherModeDone()
    {
        BattleManager.Instance.AnotherModeDone();

        if (_clearRoutine != null) StopCoroutine(_clearRoutine);
        _clearRoutine = StartCoroutine(ClearWithGatherAnimRoutine());
    }


    private IEnumerator ClearWithGatherAnimRoutine()
    {
        // 안전장치: 이미 없으면 그냥 정리
        if (spawnedCards == null || spawnedCards.Count == 0)
        {
            HardClear();
            yield break;
        }

        // (1) 펼쳐진 손패 -> 한 점으로 "쇽" 모으기
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            var go = spawnedCards[i];
            if (!go) continue;

            var rt = go.GetComponent<RectTransform>();
            if (!rt) continue;

            rt.DOKill();

            rt.DOAnchorPos(gatherPoint, gatherDuration).SetEase(gatherEase).SetUpdate(true);
            rt.DOLocalRotate(Vector3.zero, gatherDuration).SetEase(gatherEase).SetUpdate(true);
            rt.DOScale(0.98f, gatherDuration).SetEase(gatherEase).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(gatherDuration);

        // (2) 모인 점 -> handLayout의 "생성 위치(spawn)"로 이동
        Vector2 spawnPos = handLayout != null ? handLayout.GetSpawnAnchoredPos() : gatherPoint;

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            var go = spawnedCards[i];
            if (!go) continue;

            var rt = go.GetComponent<RectTransform>();
            if (!rt) continue;

            rt.DOKill();

            rt.DOAnchorPos(spawnPos, toSpawnDuration).SetEase(toSpawnEase).SetUpdate(true);
            rt.DOLocalRotate(Vector3.zero, toSpawnDuration).SetEase(toSpawnEase).SetUpdate(true);
            rt.DOScale(0.95f, toSpawnDuration).SetEase(toSpawnEase).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(toSpawnDuration);

        // (3) 이제 삭제/클리어
        HardClear();
    }

    private void HardClear()
    {
        for (int i = cardContentArea.childCount - 1; i >= 0; i--)
            Destroy(cardContentArea.GetChild(i).gameObject);

        spawnedCards.Clear();
        _clearRoutine = null;
    }



    private void SelectCard()
    {
        // 드래그핸들러에서 StopSelectCardLoop 호출하는 기존 구조 사용
    }
    public void ActivatePachingOnCodeZero()
    {
        foreach (var card in spawnedCards)
        {
            if (card == null) continue;

            var info = card.GetComponentInChildren<EachCardInfo>(true);
            if (info == null || info.cardData == null) continue;

            // code가 "0"인 카드만 대상
            if (info.cardData.cardType == 0)
            {
                // 직계에서 먼저 찾고, 없으면 모든 하위에서 이름으로 탐색
                Transform paching = card.transform.Find("Paching");
                if (paching == null)
                {
                    foreach (var t in card.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "Paching") { paching = t; break; }
                    }
                }

                if (paching != null)
                {
                    Debug.Log("파칭 찾음");

                    paching.gameObject.SetActive(true);
                }
                // else: 못 찾으면 아무 것도 안 함 (요청대로 다른 동작 불필요)
            }
        }
    }
    public void ActionPacketUpgrade()
    {
        
    }
    public void ActionPacketUpgrade_Bless( int delta_rumb, int delta_cast, int delta_def, int thisBlessDam)
    {

    }


    public void LockCancel(string why = null) { cancelLocked = true; if (!string.IsNullOrEmpty(why)) Debug.Log($"[CardMode] Cancel locked: {why}"); }
    public void UnlockCancel() { cancelLocked = false; Debug.Log("[CardMode] Cancel unlocked."); }


    private IEnumerator WaitCardsAppliedThenUpgrade( int version)
    {
        yield return null;
    }
}

// ===== 여기부터 같은 파일 바깥(전역)에 두는 확장 메서드 유틸 =====
public static class TransformUtil
{
    /// <summary>
    /// 이름이 targetName인 자손 노드 어디에서든 컴포넌트 T를 찾아 반환.
    /// 존재하지 않으면 null. 비활성 포함(true).
    /// </summary>
    public static T FindComponentByNameDeep<T>(this Transform root, string targetName) where T : Component
    {
        if (root == null || string.IsNullOrEmpty(targetName)) return null;
        foreach (var c in root.GetComponentsInChildren<T>(true))
            if (c != null && c.name == targetName) return c;
        return null;
    }

}
