using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 2번의 생명이 있으며, 일반공격, 비석 낙하 스킬, 돌진스킬이 있습니다.

public class RealBoss : MonsterAI
{
    public delegate void Callback();
    private Callback callback = null;

    [SerializeField] private float Phase2HP;

    Vector3 bossDirection = Vector3.zero;       // 플레이어의 위치와 비교하여 왼쪽으로 갈것인지 오른쪽으로 갈것인지 결정합니다.
    [SerializeField] private Transform CollisionDetection;
    [SerializeField] private LayerMask ColliderMask;
    static int hashMove = Animator.StringToHash("speed");
    static int hashAttack = Animator.StringToHash("tAttack");
    static int hashSkill1 = Animator.StringToHash("bSkill1");
    static int hashSkill2 = Animator.StringToHash("tSkill2");
    static int hashStun = Animator.StringToHash("bStun");
    static int hashGetup = Animator.StringToHash("tGetup");

    private float MaxHP;
    private bool bSkill;                // 공격 시 스킬인지 판별
    private bool bCollision;          // 돌진하여 벽에 부딪히면, WaitUntil에서 다음 프레임으로 이동합니다.
    private bool bAttack;             // 공격 애니메이션이 끝나면 false로 하며, WaitUntil에서 다음 프레임으로 이동합니다 
    private bool bSkill2;              // 스킬 애니메이션이 끝나면 false로 하며, WaitUntil에서 다음 프레임으로 이동합니다 
    private bool bPase2;             // 쓰러진 후 두 번째 목숨이 되면 true
    private bool bIsPlayPase2;      // 2페이지 개시

    WaitForSeconds retrunDelay = new WaitForSeconds(0.55f);
    WaitForSeconds deathDelay = new WaitForSeconds(1.2f);
    WaitForSeconds stunDelay = new WaitForSeconds(2.5f);
    WaitUntil Attackwait;
    WaitUntil Skill2wait;
    WaitUntil Skill1wait;
    WaitUntil Phase2Wait;
    WaitUntil Phase2Play;

    [SerializeField] private GameObject[] RollPos;
    private CharacterPrologueScript dialogue;
    protected override void Awake()
    {
        base.Awake();

        // 정보 초기화
        enemyInfo.SetHP(enemyInfo.GetHP() + (enemyInfo.GetHP() * UQGameManager.Instance.ClearNum * 0.5f));

        // 특정 조건을 빠져나가기 위해 조건 정의 
        Attackwait = new WaitUntil(() => bAttack == false);
        Skill2wait = new WaitUntil(() => bSkill2 == false);
        Skill1wait = new WaitUntil(() => bCollision == true);
        Phase2Wait = new WaitUntil(() => bPase2 == true);
        Phase2Play = new WaitUntil(() => bIsPlayPase2 == true);

        MaxHP = enemyInfo.GetHP();
        enemyInfo.target = GameObject.FindGameObjectWithTag("Player");
        state = MonsterState.Chase;

        dialogue = GameObject.FindGameObjectWithTag("Dialogue").GetComponent<CharacterPrologueScript>();
        dialogue.SetCallback(StartMyCoroutin);          // 대화 창이 끝나면, callback함수를 불러 StartMyCoroutin함수를 실행합니다.
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        anim = GetComponent<Animator>();
        state = MonsterState.Chase;
        StartCoroutine(FSMMain());
    }

    // StartMyCoroutin함수가 실행되면 이 코루틴이 실행됩니다.
    protected override IEnumerator Idle()
    {
        // 2페이지 상태가 아니면 2페이지 상태로 돌입
        if (!bPase2)
        {
            dialogue.SetCallback(PlayPhase2);           // 호출 함수를 변경합니다. 대화 창이 끝나면 실행합니다. 
            bSkill2 = false;
            bAttack = false;
            bCollision = true;
            // 일정시간 대기 후 쓰러진 상태에서 일어납니다.
            yield return stunDelay;
            anim.enabled = true;
            anim.SetTrigger(hashGetup);
            if (!UQGameManager.Instance.Monsters[enemyInfo.GetMonsterNumber()])
            {
                // Phase2Wait는 쓰러진 상태에서 일어날 때까지 기다리며, 잠시 멈추고 대화를 시작합니다.
                yield return Phase2Wait;
                anim.enabled = false;
                dialogue.Az(4);
                // 대화가 끝날 때까지 기다립니다.
                yield return Phase2Play;
            }
            else
            {
                PlayPhase2();
                bPase2 = true;
            }
            // 보스 체력를 채우는 함수를 호출합니다.
            if (callback != null)
                callback();

        }
        else        // 2번째 부활에서 죽으면 끝
        {
            anim.SetTrigger("bIsDeath");
            yield return deathDelay;
            gameObject.SetActive(false);
        }
    }

    protected override IEnumerator Chase()
    {
        float time = 0.0f;                             // 한 방향으로 쫓을 시간
        bool bIsChasing = false;                   // 추격 시간이 완료하고 다음 추격으로 가는지?
        yield return null;

        do
        {
            // 한 방향으로 추격하는 시간을 설정
            if (!bIsChasing)
            {
                time = Random.Range(0.3f, 0.8f);
                bIsChasing = true;

                // 플레이어 위치를 비교하고 왼쪽으로 갈지 오른쪽으로 갈지 결정
                if (enemyInfo.target.transform.position.x > transform.position.x)
                {
                    bossDirection = Vector3.right;
                    transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
                }
                else
                {
                    bossDirection = Vector3.left;
                    transform.localScale = new Vector3(-0.005f, 0.005f, 0.005f);
                }
            }

            if (bIsChasing)
            {
                // 한 방향으로 추격시간이 끝나면 다시 랜덤 추격시간을 얻는다
                if (time <= 0.0f)
                    bIsChasing = false;
                else
                    time -= Time.deltaTime;

                RaycastHit2D groundInfoHorizontal = Physics2D.Raycast(CollisionDetection.position, bossDirection, 1.0f, ColliderMask);
                if (groundInfoHorizontal)
                {
                    if (groundInfoHorizontal.distance <= 0.3f)
                        SetState(MonsterState.Attack);
                }

                anim.SetFloat(hashMove, transform.position.magnitude);
                transform.Translate(new Vector3(bossDirection.x * Time.deltaTime * speed * 0.05f, 0, 0));
            }
            yield return null;
        } while (!bIsNewState);
    }

    protected override IEnumerator Attack()
    {
        int random;
        int index;
        float LeftDistance;
        float RightDistance;

        GameObject Stone;

        yield return null;

        LeftDistance = Vector2.Distance(RollPos[0].transform.position, transform.position);
        RightDistance = Vector2.Distance(RollPos[1].transform.position, transform.position);

        do
        {
            // 확률로 스킬공격인지 일반 공격인지 정한다
            random = Random.Range(0, 10);
            if (random < 3)
                bSkill = true;
            else
                bSkill = false;

            if (bSkill)
            {
                random = Random.Range(0, 2);
                if (random == 0)
                {
                    if (LeftDistance < RightDistance)
                        index = 0;
                    else
                        index = 1;
                    // 플레이어 위치를 비교해서 왼쪽으로 갈지 오른쪽으로 갈지 결정
                    SetDirection(RollPos[index].transform.position);
                    do
                    {
                        transform.Translate(new Vector3(bossDirection.x * Time.deltaTime * speed * 0.05f, 0, 0));
                        yield return null;
                    } while (Mathf.Abs(transform.position.x - RollPos[index].transform.position.x) > 0.2f);

                    bossDirection *= -1;
                    transform.localScale = new Vector3(transform.localScale.x * (-1), 0.005f, 0.005f);

                    anim.SetBool(hashSkill1, true);
                    GetComponent<Rigidbody2D>().AddForce(bossDirection * 1500.0f);
                    yield return Skill1wait;
                    anim.SetBool(hashStun, true);
                    yield return stunDelay;
                    anim.SetBool(hashStun, false);
                    yield return retrunDelay;
                }
                else
                {
                    // 비석 낙하 스킬
                    anim.SetTrigger(hashSkill2);
                    bSkill2 = true;
                    Stone = ObjectPool.Instance.PopFromPool("RealBossStone");
                    Stone.transform.position = new Vector3(enemyInfo.target.transform.position.x, 2.0f, 0.0f);
                    yield return Skill2wait;
                }
            }
            else
            {
                // 일반 공격
                anim.SetTrigger(hashAttack);
                bAttack = true;
                yield return Attackwait;
            }


            RaycastHit2D groundInfoHorizontal = Physics2D.Raycast(CollisionDetection.position, bossDirection, 1.0f, ColliderMask);
            if (!groundInfoHorizontal || groundInfoHorizontal.distance <= 0.2f)
            {
                SetState(MonsterState.Chase);
            }

            yield return null;
        } while (!bIsNewState);
    }

    // 돌진하는 스킬로 벽에 부딪히면 카메라 충돌효과 부여
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("LimitL") || collision.gameObject.CompareTag("LimitR"))
        {
            if (anim.GetBool(hashSkill1))
            {
                iTween.ShakePosition(GameObject.FindGameObjectWithTag("MainCamera"), iTween.Hash("x", 0.05, "y", 0.05, "time", 1));
                bCollision = true;
                anim.SetBool(hashSkill1, false);
            }
        }
    }

    public void SetNoneAttack()
    {
        bAttack = false;
    }

    public void SetNoneSkill2()
    {
        bSkill2 = false;
    }

    public void SetPhase2()
    {
        bPase2 = true;
    }

    public bool BIsPhase2()
    {
        return bPase2;
    }

    // 2페이즈 실행
    public void PlayPhase2()
    {
        anim.enabled = true;
        box2d.enabled = true;
        bIsPlayPase2 = true;
        enemyInfo.SetHP(Phase2HP + (UQGameManager.Instance.ClearNum*100));
        SetState(MonsterState.Chase);
    }

    public void FallDown()
    {
        anim.enabled = false;
    }

    public void SetCollback(Callback _call)
    {
        callback = _call;
    }

    // 방향 설정
    void SetDirection(Vector3 compare)
    {
        if (compare.x > transform.position.x)
        {
            bossDirection = Vector3.right;
            transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        }
        else
        {
            bossDirection = Vector3.left;
            transform.localScale = new Vector3(-0.005f, 0.005f, 0.005f);
        }
    }
}
