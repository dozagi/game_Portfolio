using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 길게 누르면, 강한 스킬이 발동됩니다.

public class StandardSkill : SkillButton, IPointerDownHandler, IPointerUpHandler
{
    public GameObject CastingEffect;        // 충전 이펙트
    public GameObject Skill1;                   // 스킬 오브젝트

    private int stack;                               // 스택

    public bool bIsPress;
    public bool bCompleteCasting;           // 풀 충전인지 확인

    protected override void Awake()
    {
        base.Awake();

        bIsPress = false;
        stack = 0;
        bCompleteCasting = false;
    }

    void Update()
    {
        // 스킬 버튼을 눌렀을 때 실행합니다.
        if (bIsPress)
        {
            stack++;
            if(stack >= 60)
            {
                UQGameManager.Instance.bIsFullCastingSkill = true;       // 풀 충전 완료  

                if (!bCompleteCasting && (CastingEffect = ObjectPool.Instance.PopFromPool("CastingEffect")))
                {
                    CastingEffect.transform.position = Player.transform.position;
                    bCompleteCasting = true;                // 충전 완료
                }
            }
        }

        // 스킬 실행 후, 스킬오브젝트를 비활성화
        if (Skill1.activeSelf)
        {
            if (!UQGameManager.Instance.bIsUsingCastingSkill)
                Skill1.SetActive(false);
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

    // 스킬버튼을 눌렀을 때.
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);

        // 캔슬 조건
        if (UQGameManager.Instance.bIsPlayingSpeicalSkillAnimation || UQGameManager.Instance.bIsDraggingSkill || UQGameManager.Instance.bIsUsingTouchSkill || UQGameManager.Instance.bIsUsingDragSkill
            || UQGameManager.Instance.bIsJumping || UQGameManager.Instance.bPressingSpecialSkill || UQGameManager.Instance.bPressingSkill2)
        {
            bCancel = true;
            return;
        }

        // 캔슬이 아닌상태면 실행
        if (!bCancel)
        {
            bIsPress = true;
            UQGameManager.Instance.bIsCastingSkill1 = true;
            skillFilter.fillAmount = 0;
        }
    }

    // 스킬버튼을 뗐을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        UQGameManager.Instance.bIsCastingSkill1 = false;        // 풀 충전 초기화

        if (bCancel) return; 

        bIsPress = false;

        // 점프 상태가 아니면 실행합니다.
        if (!UQGameManager.Instance.bIsJumping)
        {
            // 스킬 실행
            ButtonImage.raycastTarget = false;
            UseSkill();
            Skill1.SetActive(true);
            bCompleteCasting = false;                                           
            UQGameManager.Instance.bIsUsingCastingSkill = true;       
            UQGameManager.Instance.bIsUseExeSkill = true;            
            UQGameManager.Instance.bIsFullCastingSkill = false;
            stack = 0;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (UQGameManager.Instance.bIsCastingSkill1)
        {
            UQGameManager.Instance.bIsCastingSkill1 = false;
            bIsPress = false;
            stack = 0;
            ButtonImage.raycastTarget = true;
            bCompleteCasting = false;
            UQGameManager.Instance.bIsUsingCastingSkill = false;
            UQGameManager.Instance.bIsUseExeSkill = false;
            UQGameManager.Instance.bIsFullCastingSkill = false;
        }
    }

   
}
