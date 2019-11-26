using UnityEngine;

// 캐릭터와 몬스터에 TakeDamage라는 함수가 있습니다
// 공격할 오브젝트가 IDamage로 데미지를 전달하면, 상대방 오브젝트는 가지고 있는 TakeDamage함수로 데미지를 받습니다.

public interface IDamage {

    // _damage - 공격할 데미지 / _cause - 공격하는 오브젝트
    void TakeDamage(float _damage, GameObject _cause);
}
