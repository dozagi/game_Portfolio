using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 하늘에서 검이 떨어지는 스킬입니다.
// 강화 스킬로 특정 지점에 큰 데미지를 줄 수 있습니다.
public class DragSkill : SkillButton, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Camera cam;

    // 스킬 범위
    public GameObject Skill3RageParticle;           // 범위 파티클
    public GameObject[] Range;
    public int RangeCount = 40;                       // 범위 파티클 수
    public LayerMask groundMask;                   // 파티클의 레이어

    // 모션, 스킬 발생 오브젝트
    public GameObject Dragging;
    public GameObject FallingSword;

    // 실행 시간
    public float ExeTime;               
    private float timer;

    private Vector3 SkillStartPosition;              // 스킬 버튼 아이콘 초기화 위치

    private bool bReturn;                             // 드래그 이미지의 위치를 초기화 -> 대화할 때
    public bool bIsPlayingMotion;
    private bool bIsOnUI;

    protected override void Awake()
    {
        base.Awake();
        
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;

        bIsOnUI = true;

        if (Dragging)
        {
            Dragging.SetActive(false);
            bIsPlayingMotion = false;
        }

        // 파티클 배열 생성
        Range = new GameObject[RangeCount];
        for (int i = 0; i < RangeCount; i++)
        {
            if (Skill3RageParticle)
            {
                Range[i] = Instantiate(Skill3RageParticle, Vector3.zero, Skill3RageParticle.transform.rotation) as GameObject;
                Range[i].transform.parent = transform;
                Range[i].SetActive(false);
            }
        }
    }

    void Update()
    {
        if (bIsPlayingMotion)
        {
            timer += Time.deltaTime;

            // 실행 시간 이 지나거나 점프를 했을 때 중지 -> 조건 초기화
            if (timer >= ExeTime || UQGameManager.Instance.bStopSkill)
            {
                timer = 0.0f;
                UQGameManager.Instance.bIsUseExeSkill = false;                      // 스킬 중지
                Dragging.SetActive(false);                                                      // 모션 중지
                bIsPlayingMotion = false;
                UQGameManager.Instance.DragStack = 0.0f;                            // 스택 초기화
                UQGameManager.Instance.bIsUsingDragSkill = false;                   // 드래그 스킬 중지
                UQGameManager.Instance.FinalDragPos = Vector2.zero;              // 스킬 이미지 위치 초기화
                Player.GetComponent<BattleSpriteAction>().bskill3Sound = false;     // 스킬 사운드 초기화
                UQGameManager.Instance.bStopSkill = false;
                FallingSword.SetActive(false);                                                  // 스킬 실행 오브젝트 초기화
            }
        }
    }

    // 쿨타임 코루틴
    IEnumerator Cooltime()
    {
        bCoroutine = true;
        while (skillFilter.fillAmount > 0)
        {
            skillFilter.fillAmount -= 1 * Time.smoothDeltaTime / coolTime;

            yield return null;
        }
        bCanUseSkill = true;
        ButtonImage.raycastTarget = true;
        bCoroutine = false;
        yield break;
    }

    // 눌렀을 때
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);

        // 캔슬 조건
        if (UQGameManager.Instance.bIsPlayingSpeicalSkillAnimation
            || UQGameManager.Instance.bIsCastingSkill1 || UQGameManager.Instance.bOnMenu
            || UQGameManager.Instance.bPressingSpecialSkill || UQGameManager.Instance.bPressingSkill2)
        {
            bCancel = true;         // 캔슬
            return;
        }

        bReturn = false;
        skillFilter.fillAmount = 0;
        SkillStartPosition = transform.position;
    }

    // 드래그 중일 때
    public void OnDrag(PointerEventData eventData)
    {
        // 드래그 중, 대화를 하면 캔슬
        if (bCancel || bReturn) return;

        ButtonImage.raycastTarget = false;

        if (!bCanUseSkill)  return;

        // 이미지 드래그 위치
        transform.position = eventData.position;
        UQGameManager.Instance.bIsDraggingSkill = true;

        // UI를 포인팅 하고 있는지 확인
        bIsOnUI = GetBoolTouchUI(eventData);

        #region 범위 파티클 표시
        int filp = 1;
        int filpValue = 0;
        if (!bIsOnUI)
        {
            for (int i = 0; i < RangeCount; i++)
            {
                if (i >= (int)(RangeCount * 0.5f))
                {
                    filp = -1;
                    filpValue = (int)(RangeCount * 0.5f);
                }

                Vector3 SkillStandardPosition = new Vector3(transform.position.x + (i - filpValue) * 20.0f * filp, 750.0f, transform.position.z);
                RaycastHit2D distanceFromGround = Physics2D.Raycast(cam.ScreenToWorldPoint(SkillStandardPosition), Vector2.down, 10, groundMask);

                Range[i].SetActive(true);
                Vector3 CreatePos = distanceFromGround.point;
                CreatePos.z = 0.0f;
                Range[i].transform.position = CreatePos;
            }
        }
        else
        {
            for (int i = 0; i < RangeCount; i++)
            {
                Range[i].SetActive(false);
            }
        }
        #endregion
    }

    // 버튼을 뗐을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        // 드래그 중지
        UQGameManager.Instance.bIsDraggingSkill = false;

        if (bCancel) return;

        // 다른 스킬 실행 or 대화 중일 때는 뗐을 때 스킬실행을 막습니다. 
        if (UQGameManager.Instance.bIsUseExeSkill || bReturn)
        {
            transform.position = SkillStartPosition;
            ButtonImage.raycastTarget = true;
            return;
        }

        UQGameManager.Instance.FinalDragPos = transform.position;
        transform.position = SkillStartPosition;

        // UI를 포인팅 하고 있는지 확인
        bIsOnUI = GetBoolTouchUI(eventData);

        if (!bIsOnUI && !UQGameManager.Instance.bIsJumping)
        {
            // 파티클 비활성화
            for (int i = 0; i < RangeCount; i++)
                Range[i].SetActive(false);

            // 스킬실행
            if (Dragging)
            {
                Dragging.SetActive(true);
                bIsPlayingMotion = true;
            }
            ButtonImage.raycastTarget = false;
            UseSkill();
            FallingSword.SetActive(true);
            UQGameManager.Instance.bIsUseExeSkill = true;  

        }
        else
            ButtonImage.raycastTarget = true;
    }

    bool GetBoolTouchUI(PointerEventData eventData)
    {
        // UI를 포인팅하면 실행할 수 없도록 합니다.
        if (EventSystem.current.IsPointerOverGameObject(eventData.pointerId) == false)
            return false;
        else
            return true;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        bReturn = true;
        UQGameManager.Instance.bIsDraggingSkill = false;
        transform.position = SkillStartPosition;                // 이미지를 원위치로 
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        timer = 0.0f;
        ButtonImage.raycastTarget = true;
        UQGameManager.Instance.bIsUseExeSkill = false;
        Dragging.SetActive(false);
        bIsPlayingMotion = false;
        UQGameManager.Instance.DragStack = 0.0f;
        UQGameManager.Instance.bIsUsingDragSkill = false;
        UQGameManager.Instance.FinalDragPos = Vector2.zero;
        UQGameManager.Instance.bStopSkill = false;
        FallingSword.SetActive(false);
    }
}
