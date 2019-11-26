using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 보이지 않는 3개의 UI와 접촉하면 스택이 증가합니다

public class DragSkillStack : MonoBehaviour, IDragHandler
{
    public GameObject Motion;
    public Transform Touch1;
    public Transform Touch2;
    public Transform MiddleTouch;

    public bool bIsTouch1;
    public bool bIsTouch2;

    private Camera cam;

    public float dragStack;

    DragSkill dragSkill;
    // Use this for initialization
    private void OnEnable()
    {
        dragStack = 0.008f;
        UQGameManager.Instance.DragStack = 0.0f;
    }

    // 3개의 블록을 전부 지나면 스택이 증가하고, 이것을 반복하여 많은 스택을 쌓을 수가 있습니다
    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.position.x < Touch1.position.x)
        {
            bIsTouch1 = true;
        }
        else if (eventData.position.x > Touch2.position.x)
        {
            bIsTouch2 = true;
        }
        else if(Vector2.Distance(eventData.position, MiddleTouch.position) < 100.0f )
        {
            Dragging();
        }
    }

    // 스택이 쌓입니다.
    void Dragging()
    {
        if (bIsTouch1 && bIsTouch2)
        {
            bIsTouch1 = false;
            bIsTouch2 = false;
            if (UQGameManager.Instance.DragStack < 0.05)
                UQGameManager.Instance.DragStack += dragStack;
        }
    }
}
