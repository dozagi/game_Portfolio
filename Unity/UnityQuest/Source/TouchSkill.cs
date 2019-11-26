using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 이 스킬은 실행 후, 화면을 터치하면 할수록 많은 검이 나갑니다.

public class TouchSkill : SkillButton, IPointerDownHandler, IPointerUpHandler
{
    public GameObject Touching;           // 모션
    public GameObject Skill2;               // 스킬 오브젝트

    // 실행시간
    public float ExeTime;
    float timer;

    public bool bIsPlayingMotion;

    protected override void Awake()
    {
        base.Awake();

        if (Touching)
        {
            Touching.SetActive(false);
            bIsPlayingMotion = false;
        }
    }

    void Update()
    {
        // 스킬을 실행합니다.
        if (bIsPlayingMotion)
        {
            timer += Time.deltaTime;

            // 실행시간 초과 / 점프를 하면, 스킬중지->모든 조건 초기화
            if (timer >= ExeTime || UQGameManager.Instance.bStopSkill)
            {
                Skill2.SetActive(false);
                timer = 0.0f;
                UQGameManager.Instance.bIsUseExeSkill = false; 
                Touching.SetActive(false);
                bIsPlayingMotion = false;
                UQGameManager.Instance.TouchStack = 0.0f;
                UQGameManager.Instance.bIsUsingTouchSkill = false;

                // 강화 스킬을 사용할 때 생성되는 오라를 비활성화
                if (UQGameManager.Instance.bStopSkill)
                {
                    if (Player.transform.GetChild(2).gameObject.activeSelf)
                        Player.transform.GetChild(2).GetComponent<Skill2Aura>().SetInActivation();
                    UQGameManager.Instance.bStopSkill = false;
                }
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

    // 스킬버튼을 눌렀을 때
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);

        // 캔슬 조건
        if (!bCanUseSkill || UQGameManager.Instance.bIsPlayingSpeicalSkillAnimation || UQGameManager.Instance.bIsUseExeSkill || UQGameManager.Instance.bOnMenu
            || UQGameManager.Instance.bIsCastingSkill1 || UQGameManager.Instance.bIsDraggingSkill || UQGameManager.Instance.bPressingSpecialSkill)
        {
            bCancel = true;
            return;
        }

        UQGameManager.Instance.bPressingSkill2 = true;
        skillFilter.fillAmount = 0;
    }

    // 스킬 버튼을 뗐을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        UQGameManager.Instance.bPressingSkill2 = false;

        if (bCancel) return;

        if (bCanUseSkill && eventData.pointerCurrentRaycast.gameObject == gameObject && !UQGameManager.Instance.bIsJumping) 
        {
            // 스킬 실행
            ButtonImage.raycastTarget = false;
            UseSkill();
            Skill2.SetActive(true);
            UQGameManager.Instance.bIsUseExeSkill = true;

            if (Touching)
            {
                Touching.SetActive(true);
                bIsPlayingMotion = true;
            }
        }

    }

    protected override void OnDisable()
    {
        base.OnDisable();

        Skill2.SetActive(false);
        timer = 0.0f;
        UQGameManager.Instance.bIsUseExeSkill = false;
        Touching.SetActive(false);
        bIsPlayingMotion = false;
        UQGameManager.Instance.TouchStack = 0.0f;
        UQGameManager.Instance.bIsUsingTouchSkill = false;

        if (Player.transform.GetChild(2).gameObject.activeSelf)
            Player.transform.GetChild(2).GetComponent<Skill2Aura>().SetInActivation();
        UQGameManager.Instance.bStopSkill = false;
    }
}
