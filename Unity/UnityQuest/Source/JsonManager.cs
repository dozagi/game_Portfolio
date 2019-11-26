using UnityEngine;
using System.Collections;
using LitJson;
using System.IO;
using LitJson;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public delegate void NewSceneHandler();

// 저장된 정보로 몬스터를 배치합니다.  

public class CharactersInfo
{
    public int ID;
    public string Name;
    public double HP;
    public double Str;
    public double X;
    public double Y;

    public CharactersInfo(int _id, string _name, double _hp, double _str, double _x, double _y)
    {
        ID = _id;
        Name = _name;
        HP = _hp;
        Str = _str;
        X = _x;
        Y = _y;
    }
}

public class JsonManager : MonoBehaviour
{
    public event NewSceneHandler Handler;       // 새로운 스테이지가 시작되면 몬스터배치가 끝날 때, 스테이지의 몬스터의 수, 쓰러뜨릴 때의 델리게이트 적용을 담당합니다.

    public List<CharactersInfo> itemList = new List<CharactersInfo>();
    public List<CharactersInfo> MyInventory = new List<CharactersInfo>();

    public void LoadBtn()
    {
        StartCoroutine(LoadInfoData());
    }

    // 완료까지 실행합니다.
    IEnumerator LoadInfoData()
    {
            TextAsset tasset = Resources.Load("document") as TextAsset;
            JsonData itemData = JsonMapper.ToObject(tasset.ToString());
            GetItem(itemData);
            yield return null;
    }

    private void GetItem(JsonData name)
    {
        // 각 몬스터의 정보를 저장합니다.
        GameObject monster;
        for (int i = 0; i < name.Count; i++)
        {
            if (int.Parse(name[i]["Stage"].ToString()) == UQGameManager.Instance.Stage)
            {
                if (monster = ObjectPool.Instance.PopFromPool(name[i]["Name"].ToString()))
                {
                    monster.GetComponent<EnemyInfo>().SetInfo(float.Parse(name[i]["HP"].ToString()), float.Parse(name[i]["Str"].ToString()), float.Parse(name[i]["Speed"].ToString()));
                    monster.transform.position = new Vector3(float.Parse(name[i]["X"].ToString()), float.Parse(name[i]["Y"].ToString()), 0.0f);
                }
            }
        }

        Handler();
    }

    // 다음 씬으로 이동 후 실행합니다.
    private void OnLevelWasLoaded(int level)
    {
        if(UQGameManager.Instance.Stage != 0 && UQGameManager.Instance.Stage != -1)
            StartCoroutine(LoadInfoData());
    }

}


