using Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;
using static Define;
using static Util;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

public interface ILoader<Value>
{
    void GetData();
}


public class DataManager
{
    public GameData CurrentData;

    private string _path => Path.Combine(Application.persistentDataPath, "SaveData.json");

    public void Init()
    {
        if(CurrentData==null)
        {
            LoadGame();
            Debug.Log("Managers.Data Init 완료");
        }
      
    }

    public void SaveGame()
    {
        CurrentData.MyCat.LastExitTime = System.DateTime.Now.ToString();

        string json = JsonUtility.ToJson(CurrentData, true);
        File.WriteAllText(_path, json);

        Debug.Log("[저장 완료]");
    }

    public void LoadGame()
    {
        if (File.Exists(_path))
        {
            string json = File.ReadAllText(_path);

            CurrentData = JsonUtility.FromJson<GameData>(json);

            Debug.Log($"[로드 성공] 경로: {_path}");
        }
        else
        {
            CurrentData = new GameData();
            CurrentData.MyCat.LastExitTime = System.DateTime.Now.ToString();
            Debug.Log("새로운 게임 데이터를 생성했습니다.");
        }
        CalculateOfflineStatus();
    }

    void CalculateOfflineStatus()
    {
        if (string.IsNullOrEmpty(CurrentData.MyCat.LastExitTime))
        {
            CurrentData.MyCat.LastExitTime = DateTime.Now.ToString();
            return;
        }

        DateTime lastTime = DateTime.Parse(CurrentData.MyCat.LastExitTime);
        DateTime nowTime = DateTime.Now;

        TimeSpan timeDiff = nowTime - lastTime;
        double secondsPassed = timeDiff.TotalSeconds;

        Debug.Log($"게임 꺼진 동안 {secondsPassed}초가 지났습니다.");

        float hungerDecrease = (float)(secondsPassed / 600.0) * 10.0f;

        CurrentData.MyCat.Hunger -= hungerDecrease;

        if (CurrentData.MyCat.Hunger < 0) CurrentData.MyCat.Hunger = 0;

        Debug.Log($"자리를 비운 동안 고양이가 {hungerDecrease}만큼 배고파졌습니다!");
    }

    public void ResetData()
    {
        CurrentData = new GameData();
        SaveGame();
        Debug.Log("[Debug] 데이터가 초기화되었습니다.");
    }


    public static List<T> ReadCsv<T>(string path) where T : new()
    {
        List<T> dataList = new List<T>();

        TextAsset textAsset = Managers.Resource.Load<TextAsset>("Data\\" + path);
        if (textAsset == null)
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {path}");
            return null;
        }

        using (StringReader reader = new StringReader(textAsset.text))
        {
            string line;
            bool isHeader = true;

            while ((line = reader.ReadLine()) != null)
            {
                if (isHeader)
                {
                    isHeader = false;
                    continue;
                }
                T data = ParseCsvLine<T>(line);
                dataList.Add(data);
            }
        }

        return dataList;
    }

    private static T ParseCsvLine<T>(string line) where T : new()
    {
        T data = new T();
        string[] values = line.Split(',');

        var properties = typeof(T).GetProperties();
        for (int i = 0; i < properties.Length && i < values.Length; i++)
        {
            var property = properties[i];
            if (property.CanWrite)
            {
                object value;

                if (property.PropertyType.IsEnum)
                {
                    value = Enum.Parse(property.PropertyType, values[i]);
                }
                else
                {
                    value = Convert.ChangeType(values[i], property.PropertyType);
                }

                property.SetValue(data, value);
            }
        }

        return data;
    }
}
