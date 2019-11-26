using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 한 글자 씩 출력합니다.

public class PrologueText : MonoBehaviour {

    [SerializeField] private Text txt;
    static private string dialogText;
    [SerializeField] private GameObject Prologue;

    // 대화 창 애니메이션
    private Animator anim;
    static int hashOpen = Animator.StringToHash("bIsOpen");

    WaitForSeconds waitSeconds = new WaitForSeconds(0.02f);
    WaitForSeconds NextDialogueWaitSeconds = new WaitForSeconds(1.0f);

    private void Awake()
    {
        anim = GetComponent<Animator>();
        anim.enabled = false;
    }

    // CharacterPrologueScript부터 대화를 받습니다
    public void GetText(string _prologue)
    {
        // 애니메이터 실행, 대화를 저장합니다.
        anim.enabled = true;
        dialogText = _prologue;

        // 대화 창이 없으면 애니메이션으로 대화창을 열고, 대화창이 있으면 바로 대화를 시작합니다.
        if (!anim.GetBool(hashOpen))
            anim.SetBool(hashOpen, true);
        else
            ProloguePlay();
    }

    public void ProloguePlay()
    {
        StartCoroutine(NextPrologue());
    }

    // 대화 시작
    IEnumerator NextPrologue()
    {
        // bIsCompleteDialogue는 아직 대화가 끝나지 않았다는 의미로 flase로 합니다
        UQGameManager.Instance.bIsCompleteDialogue = false;
        txt.text = "";          // 공백에서 한 글자씩 출력합니다.

        // 한 글자씩 써 내려갑니다
        for (int i = 0; i < dialogText.Length; i++)
        {
            // 화면을 터치하면 이 효과가 없고 바로 모든 대사가 써집니다.
            if (UQGameManager.Instance.bIsDialogueStop)
            {
                txt.text = dialogText;
                break;
            }
            else        // 입력이 없으면 한 글자씩 출력합니다.
            {
                txt.text += dialogText[i];
                yield return waitSeconds;
            }
        }

        // 대사 끝
        UQGameManager.Instance.bIsCompleteDialogue = true;

        // 대사가 끝난 상태에서 터치하면 다음 대사가 시작하며 Auto버튼을 누르면 자동으로 대화가 진행됩니다.
        do
        {
            if(UQGameManager.Instance.bIsAuto)
            {
                yield return NextDialogueWaitSeconds;
                Prologue.GetComponent<CharacterPrologueScript>().Bz();
                break;
            }
            if(UQGameManager.Instance.bIsNextDialogue)
            {
                Prologue.GetComponent<CharacterPrologueScript>().Bz();
                break;
            }
            yield return null;
        } while (true);

    }

    // 대화 창을 닫습니다.
    public void ClosePrologue()
    {
        anim.SetBool(hashOpen, false);
    }

    // 대화창을 초기화 합니다.
    public void ClearDialogue()
    {
        txt.text = "";
        anim.enabled = false;
    }
}
