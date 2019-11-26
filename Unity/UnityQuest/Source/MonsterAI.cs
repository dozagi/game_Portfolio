using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// FSM으로 AI를 제작했습니다
// Coroutin으로 상태에 대응하는 행동을 보여줄 수 있습니다.

public enum MonsterState { Idle, Move, Chase, Attack, Hunt }

public class MonsterAI : MonoBehaviour
{
    // 각 몬스터가 공통으로 필요한 변수
    protected static int hashDeath = Animator.StringToHash("bIsDeath");
    protected WaitForSeconds delay = new WaitForSeconds(0.5f);
    public float speed;                  // 이동속도
    protected int direction;            // 방향

    // 상태
    protected MonsterState state;
    protected MonsterState InitialState;
    protected bool bIsNewState = false;

    // 참조 컴포넌트 및 스크립트
    protected Rigidbody2D rig2d;
    public Animator anim;
    protected EnemyInfo enemyInfo;
    protected BoxCollider2D box2d;

    protected virtual void Awake()
    {
        // 초기화
        anim = GetComponent<Animator>();
        box2d = GetComponent<BoxCollider2D>();
        rig2d = GetComponent<Rigidbody2D>();
        enemyInfo = GetComponent<EnemyInfo>();

        InitialState = state;
    }

    protected virtual void OnDisable()
    {
        // 몬스터 제거
        enemyInfo.SetHP(enemyInfo.GetInitialHP());
        enemyInfo.target = null;
        enemyInfo.DeathHandler -= SetDeathState;
        state = InitialState;
    }

    // 델리게이트 이벤트 적용
    protected virtual void OnEnable()
    {
        box2d.enabled = true;

        if (!enemyInfo.BIsBoss())
            enemyInfo.life = 1;
        else
            enemyInfo.life = 2;

        enemyInfo.DeathHandler += SetDeathState;
    }

    public void SetIdleCoroutine()
    {
        state = MonsterState.Idle;
        StartMyCoroutin();
    }

    public void StartMyCoroutin()
    {
        StartCoroutine(FSMMain());
    }

    public void SetDeathState()
    {
        box2d.enabled = false;
        anim.SetTrigger("bIsDeath");
        StopAllCoroutines();
    }

    protected IEnumerator FSMMain()
    {
        while (true)
        {
            bIsNewState = false;
            yield return StartCoroutine(state.ToString());
        }
    }

    public void SetState(MonsterState _state)
    {
        state = _state;
        bIsNewState = true;
    }

    // Idle
    protected virtual IEnumerator Idle()
    {
        yield return null;
    }

    // Move
    protected virtual IEnumerator Move()
    {
        yield return null;
    }

    // Chase
    protected virtual IEnumerator Chase()
    {
        yield return null;
    }

    // Attack
    protected virtual IEnumerator Attack()
    {
        yield return null;
    }

    // Hunt (Eagle몬스터의 고유한 행동)
    protected virtual IEnumerator Hunt()
    {
        yield return null;
    }
}
