using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 오라와 스킬 컷씬을 보여줍니다.

public class SpecialSkill : SkillButton, IPointerDownHandler, IPointerUpHandler
{
    public GameObject SpecialSkillObj;         // 스킬 오브젝트
    private GameObject Aura;                   // 오라

    static int hashSpecialSkill = Animator.StringToHash("SpecialSkillAnimation2"); 

    // 실행 시간
    public float ExeTime;
    private float timer;
   
    private Camera cam;

    protected override void Awake()
    {
        base.Awake();
        
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>() as Camera;
    }

    void Update()
    {
        // 일정시간 실행합니다.
        if (UQGameManager.Instance.bUsingSpecialSkill)
        {
            timer += Time.deltaTime;

            if (timer >= ExeTime)
            {
                timer = 0.0f;
                SpecialSkillObj.SetActive(false);
                UQGameManager.Instance.bUsingSpecialSkill = false;
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
        if (UQGameManager.Instance.bIsDraggingSkill
            || UQGameManager.Instance.bIsCastingSkill1 || UQGameManager.Instance.bPressingSkill2)
        {
            bCancel = true;
            return;
        }

        UQGameManager.Instance.bPressingSpecialSkill = true;

        skillFilter.fillAmount = 0;
    }

    // 스킬 버튼을 뗐을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        UQGameManager.Instance.bPressingSpecialSkill = false; 

        if (bCancel) return;

        // 스킬 실행
        if (bCanUseSkill && eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            ButtonImage.raycastTarget = false;
            UseSkill();

            SpecialSkillObj.SetActive(true);

            // 선택한 컷씬을 활성화 합니다.
            if (!UQGameManager.Instance.Cutin2)
                cam.transform.GetChild(0).gameObject.SetActive(true);
            else
                cam.transform.GetChild(1).gameObject.SetActive(true);

            // 오라를 활성화합니다.
            Aura = ObjectPool.Instance.PopFromPool("SpecialSkillAura");
            Aura.transform.position = Player.transform.position;

            Player.GetComponent<Animator>().SetTrigger(hashSpecialSkill);         // アニメ
            UQGameManager.Instance.bIsUseExeSkill = true;
            UQGameManager.Instance.bIsPlayingSpeicalSkillAnimation = true;
            UQGameManager.Instance.bUsingSpecialSkill = true;
        }
    }
}
