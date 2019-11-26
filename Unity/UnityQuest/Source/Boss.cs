using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 인간형 보스로 점프와 일반공격, 스킬을 사용합니다.

public class Boss : MonsterAI
{
    [SerializeField] private string ObjectName;
    private GameObject HitTarget;
    private IDamage idamage;

    static int hashMove = Animator.StringToHash("speed");
    public Transform CollisionDetection;
    public Transform CollisionDetectionBack;
    [SerializeField] private LayerMask ColliderMask;
    [SerializeField] private LayerMask GroundMask;

    WaitForSeconds delay = new WaitForSeconds(0.5f);        // 죽을 때 기다리는 시간
    Vector3 bossDirection = Vector3.zero;       // 플레이어 위치를 비교하고 왼쪽으로 갈지 오른쪽으로 갈지 결정
    GameObject skillSeed;

    protected override void Awake()
    {
        base.Awake();
        enemyInfo.SetHP(enemyInfo.GetHP() + (enemyInfo.GetHP() * UQGameManager.Instance.ClearNum * 0.5f));

        anim.SetLayerWeight(1, 1.0f);
        anim.SetLayerWeight(2, 0.0f);

        enemyInfo.target = GameObject.FindGameObjectWithTag("Player");
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        anim = GetComponent<Animator>();
        state = MonsterState.Chase;
        anim.SetLayerWeight(1, 1.0f);
        anim.SetLayerWeight(2, 0.0f);
        StartCoroutine(FSMMain());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    // 몬스터 비활성화
    void InactivationMonster()
    {
        gameObject.SetActive(false);
        StopCoroutine(state.ToString());
    }

    protected override IEnumerator Idle()
    {
        yield return null ;
    }

    // 플레이어와 멀리 떨어지면 일정 확률로 점프를 하고 점프 딜레이를 줍니다
    // 플레이어와 일정 거리 내에 있다면 공격으로, 떨어지면 추적을 합니다.
    protected override IEnumerator Chase()
    {
        int jumpRandom = 2;							// 점프 확률
        float jumpDelay = 5.0f;
        float CurrentjumpDelay = 0.0f;
        float time = 0.0f;                             // 한 방향으로 쫓을 시간
        bool bIsChasing = false;                   // 추격시간이 완료하면 다음 추격으로 갈지?

        yield return null;
        do
        {
            // 한 방향으로 추격할 시간을 설정
            if (!bIsChasing)
            {
                time = Random.Range(0.3f, 0.8f);
                bIsChasing = true;

                // 플레이어 위치를 비교하고 왼쪽으로 갈지 오른쪽으로 갈지 결정
                if (enemyInfo.target.transform.position.x > transform.position.x)
                {
                    bossDirection = Vector3.right;
                    transform.localScale = new Vector3(1f, 1f, 1f);
                }
                else
                {
                    bossDirection = Vector3.left;
                    transform.localScale = new Vector3(-1f, 1f, 1f);
                }
            }

            if (bIsChasing)
            {
                // 한 방향으로 추격이 끝나면 다시 랜덤 추격시간을 받습니다.
                if (time <= 0.0f)
                    bIsChasing = false;
                else
                    time -= Time.deltaTime;

                if (CurrentjumpDelay > 0.0f)
                    CurrentjumpDelay -= Time.deltaTime;

                // 떨어져있으면 일정 확률로 점프를 하면서 접근하며, 일정 거리 안에 있으면 공격으로 전환합니다.
                RaycastHit2D groundInfoHorizontal = Physics2D.Raycast(CollisionDetection.position, bossDirection, 2.0f, ColliderMask);
                if (groundInfoHorizontal)
                {
                    if (groundInfoHorizontal.distance >= 1.0f && groundInfoHorizontal.distance <= 1.5f)
                    {
                        if (jumpRandom > Random.Range(1, 101))
                        {
                            if (CurrentjumpDelay <= 0.0f)
                            {
                                CurrentjumpDelay = jumpDelay;
                                anim.SetTrigger("bIsJump");
                                rig2d.velocity = new Vector2(bossDirection.x * 2.0f, 4.0f);
                            }
                        }
                    }
                    else if (groundInfoHorizontal.distance <= 0.1f)
                        SetState(MonsterState.Attack);
                }

                transform.Translate(new Vector3(bossDirection.x * Time.deltaTime * speed * 0.05f, 0, 0));
                anim.SetFloat(hashMove, transform.position.magnitude);

            }
            yield return null;
        } while (!bIsNewState);
    }

    // 일반 공격, 스킬 공격
    protected IEnumerator Attack()
    {
        float skill_ = 20.0f;                         // 스킬 확률
        float skillCoolTime = 5.0f;
        float skillTime = skillCoolTime;  

        yield return null;

        do
        {
            RaycastHit2D HorizontalCheck = Physics2D.Raycast(CollisionDetection.position, bossDirection, 1.0f, ColliderMask);
            if (!HorizontalCheck)
            {
                if (skill_ > Random.Range(0, 101))       // 보스의 뒤로 이동하면 보스가 스킬을 사용할 확률
                {
                    // 스킬 실행
                    anim.SetBool("bIsCrouch", true);
                    do
                    {
                        if (skillTime < 0.0f)
                        {
                            anim.SetBool("bIsCrouch", false);
                            skillTime = skillCoolTime;
                            break;
                        }
                        else
                            skillTime -= Time.deltaTime;

                        yield return null;
                    } while (true);
                }
                else
                    SetState(MonsterState.Chase);
            }
            else
            {
                // 일반 공격
                if (HorizontalCheck.distance < 0.1f)
                {
                    anim.SetTrigger("bIsAttack");
                    yield return delay;
                }
                else if (HorizontalCheck.distance > 0.1f)
                    SetState(MonsterState.Chase);
            }
            yield return null;

        } while (!bIsNewState);
    }

    // 땅에서 랜덤 위치에 물체가 발사됩니다. 
    public void BossSkill()
    {
        float forward, back;

        // 벽이 있을 때를 고려해 물체가 맵 안에 생성되도록 합니다. 
        RaycastHit2D SkillHorizontal = Physics2D.Raycast(CollisionDetection.position, bossDirection, 2.0f, GroundMask);
        RaycastHit2D SkillHorizontalBack = Physics2D.Raycast(CollisionDetectionBack.position, bossDirection * (-1), 2.0f, GroundMask);
        if (SkillHorizontal)
        {
            if (SkillHorizontal.distance > 0.3f)
                forward = SkillHorizontal.distance;
            else
                forward = 0.0f;
        }
        else
            forward = 2.0f;

        if (SkillHorizontalBack)
        {
            if (SkillHorizontalBack.distance > 0.3f)
                back = SkillHorizontalBack.distance;
            else
                back = 0.0f;
        }
        else
            back = 2.0f;

        // 5개 물체를 발사합니다.
        float ran;
        for (int i = 0; i < 5; i++)
        {
            if (transform.localScale.x > 0)
                ran = Random.Range(-back, forward);
            else
                ran = Random.Range(-forward, back);

            // 오브젝트 풀링으로 랜덤 위치에 생성합니다.
            skillSeed = ObjectPool.Instance.PopFromPool("BossSeed");
            skillSeed.transform.position = new Vector3(transform.position.x + ran, -0.4f, 0.0f);
            skillSeed.GetComponent<Rigidbody2D>().AddForce(Vector2.up * 150.0f);
        }
    }

    public void EndSkill()
    {
        anim.SetBool("bIsCrouch", false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HitTarget = collision.gameObject;
            idamage = HitTarget.GetComponent<IDamage>();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HitTarget = null;
            idamage = null;
        }
    }
}



