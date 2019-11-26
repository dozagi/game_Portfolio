using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

// 데이터 세이브, 로드입니다

public class DataSaveLoad : MonoBehaviour {

    // 저장할 캐릭터 정보
    [Serializable]
    public class UQData
    {
        public int clearNum;
        public float StrBuff;
        public float CriticalBuff;
        public float SkillBuff;
        public bool[] Monsters;
    }

    private void Awake()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        LoadData();
    }

    // 저장할 테이터를 특정 경로에 저장합니다.
    public void SaveData()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream fs = File.Create(Application.persistentDataPath + "/UQData.dat");

        // UQData에 저장할 데이터를 넣습니다. 
        UQData data = new UQData();
        data.Monsters = new bool[UQGameManager.Instance.Monsters.Length];

        data.clearNum = UQGameManager.Instance.ClearNum;
        data.StrBuff = UQGameManager.Instance.STRBuff;
        data.CriticalBuff = UQGameManager.Instance.CriticalBuff;
        data.SkillBuff = UQGameManager.Instance.SkillBuff;
        
        for (int i = 0; i < UQGameManager.Instance.Monsters.Length; i++)
        {
            data.Monsters[i] = UQGameManager.Instance.Monsters[i];
        }
        bf.Serialize(fs, data);
        
        fs.Close();
    }

    // 저장된 데이터를 읽습니다.
    public void LoadData()
    {
        // 파일을 호출합니다.
        BinaryFormatter bf = new BinaryFormatter();
        FileStream fs;

        // 파일이 존재하면 열고, 없으면 생성합니다.
        if (File.Exists(Application.persistentDataPath + "/UQData.dat"))
        {
            fs =File.Open(Application.persistentDataPath + "/UQData.dat", FileMode.Open);
        }
        else
        {
            fs = File.Create(Application.persistentDataPath + "/UQData.dat");
        }

        if (fs != null && fs.Length > 0)
        {
            // 읽어 들인 데이터의 정보를 저장합니다.
            UQData data = (UQData)bf.Deserialize(fs);

            UQGameManager.Instance.ClearNum = data.clearNum;
            UQGameManager.Instance.STRBuff = data.StrBuff;
            UQGameManager.Instance.CriticalBuff = data.CriticalBuff;
            UQGameManager.Instance.SkillBuff = data.SkillBuff;
            UQGameManager.Instance.Monsters = data.Monsters;
            for (int i = 0; i < UQGameManager.Instance.Monsters.Length; i++)
            {
                UQGameManager.Instance.Monsters[i] = data.Monsters[i];
            }
        }
        fs.Close();
    }
}
