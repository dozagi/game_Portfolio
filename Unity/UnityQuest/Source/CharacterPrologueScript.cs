using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 특정 지점이나 스테이지에서 Az함수가 호출되어 인덱스에 맞는 대화를 시작합니다.
public delegate void ProgressHandler();

[System.Serializable]
public class Dialogue
{
    [TextArea(3,10)]
    public string dialogue;

    public bool bIsUnity;
}

[System.Serializable]  //  MonoBehaviour가 아닌 스크립트를 인스펙터 상에 표시
public class StringItem
{
    // 이미지 및 나타낼 대화를 인스펙터 상에서 입력
    public Sprite UnityImage;
    public Sprite PartnerImage;
    public string UnityName;
    public string PartnerName;
    public Dialogue[] variable;
}

public class CharacterPrologueScript : MonoBehaviour {

    public event ProgressHandler Handler;

    public delegate void Callback();
    private Callback callback = null;

    [SerializeField] private GameObject[] InactivationUI;     // 활성화, 비활성화 할 모든 UI
    [SerializeField] private GameObject UnityProlog;         // 좌측 대화창 Window
    [SerializeField] private GameObject BossProlog;          // 우측 대화창 Window

    [SerializeField] private StringItem[] dialogue;

    static int CurrentDialogueIdex = -1;
    static int CurrentDialogue = 0;

    Image Unity, Boss;
    Text UnityName, PartnerName;

    GameObject Button;          // Auto Button

    private void Awake()
    {
        Unity = UnityProlog.transform.GetChild(1).GetComponent<Image>();
        UnityName = UnityProlog.transform.GetChild(2).GetComponent<Text>();
        Boss = BossProlog.transform.GetChild(1).GetComponent<Image>();
        PartnerName = BossProlog.transform.GetChild(2).GetComponent<Text>();

        Button = transform.GetChild(2).gameObject;
        if (Button.activeSelf)
            Button.SetActive(false);
    }

    // 대화 신호를 받습니다.
    public void Az(int stringIndex)
    {
        // 신호가 들어오면 bIsAuto가 false로 , 대화가 진행되는 의미에서 bIsDialogue가 true로 합니다.
        UQGameManager.Instance.bIsAuto = false;
        UQGameManager.Instance.bIsDialogue = true;

        // 대화 인덱스를 받아 주고 받을 인덱스를 0으로 초기화합니다. 
        CurrentDialogue = stringIndex;
        CurrentDialogueIdex = 0;

        // Auto 버튼을 활성화 합니다.
        if (!Button.activeSelf)
            Button.SetActive(true);

        // 주고 받을 함수를 실행하고 대화 창을 제외한 나머지 UI를 비활성화 합니다.
        Bz();
        InactivationUI = GameObject.FindGameObjectsWithTag("PlayUI");
        SetInactivationUI(false);        // 특정 UI를 숨깁니다

        #region 대화 이미지를 설정합니다. 이미지가 없으면 없는대로 나옵니다.
        if (dialogue[CurrentDialogue].UnityImage)
        {
            Unity.enabled = true;
            Unity.sprite = dialogue[CurrentDialogue].UnityImage;
            UnityName.enabled = true;
            UnityName.text = dialogue[CurrentDialogue].UnityName;
        }
        if (dialogue[CurrentDialogue].PartnerImage)
        {
            Boss.enabled = true;
            Boss.sprite = dialogue[CurrentDialogue].PartnerImage;
            PartnerName.enabled = true;
            PartnerName.text = dialogue[CurrentDialogue].PartnerName;
        }
        #endregion
    }

    // 대화 창을 제외한 모든 UI를 숨깁니다.
    public void SetInactivationUI(bool _active)
    {
        for (int i = 0; i < InactivationUI.Length; i++)
        {
            for(int j=0;j < InactivationUI[i].transform.childCount;j++)
                InactivationUI[i].transform.GetChild(j).gameObject.SetActive(_active);
        }
    }

    // 대화 주고받음
    public void Bz()
    {
        // bIsNextDialogue는 모든 글자가 출력될 동안 기다리는데, 이때 터치를 하면 다음 대사로 보내주는 역할을 한다.
        // bIsDialogueStop는 대사가 한 글자씩 출력 중에 터치를 하면 true로 변하고, 한번에 대사를 출력하는 역할을 한다は
        UQGameManager.Instance.bIsNextDialogue = false;
        UQGameManager.Instance.bIsDialogueStop = false;

        #region 대화의 길이만큼 실행합니다. 
        if (dialogue[CurrentDialogue].variable.Length > CurrentDialogueIdex)
        {
            #region bool 값에 따라 어느 쪽으로 출력할지를 결정합니다.
            if (dialogue[CurrentDialogue].variable[CurrentDialogueIdex].bIsUnity)
            {
                UnityProlog.GetComponent<PrologueText>().GetText(dialogue[CurrentDialogue].variable[CurrentDialogueIdex].dialogue);
                
            }
            else
            {
                BossProlog.GetComponent<PrologueText>().GetText(dialogue[CurrentDialogue].variable[CurrentDialogueIdex].dialogue);
            }
            CurrentDialogueIdex++;
            #endregion
        }
        else        // 대화가 끝나면 양쪽의 대화 창을 닫는 애니메이션을 실행합니다.
        {
            UnityProlog.GetComponent<PrologueText>().ClosePrologue();
            BossProlog.GetComponent<PrologueText>().ClosePrologue();
            StartCoroutine(WaitPrologue());         // 原状復旧
        }
        #endregion
    }

    // UI 창이 복구 되면 움직일 수 있도록 합니다.
    IEnumerator WaitPrologue()
    {
        // 일정 시간 후, UI복구, 코하쿠와 상대측의 이미지 비활성화, 대화창 비활성화
        yield return new WaitForSeconds(0.8f);
        SetInactivationUI(true);
        Boss.enabled = false;
        Unity.enabled = false;
        UQGameManager.Instance.bIsDialogue = false;
        if (callback != null)
            callback();

        if (Button.activeSelf)
            Button.SetActive(false);
    }

    // Auto버튼을 누르면 자동으로 대화가 실행됩니다.
    public void PlayAuto()
    {
        UQGameManager.Instance.bIsAuto = true;
    }

    public void SetCallback(Callback cal)
    {
        callback = cal;
    }
}
