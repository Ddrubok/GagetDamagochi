using Data;
//using Newtonsoft.Json;
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

[Serializable]
public class GameData
{
    public string catName = "나비";
    public int level = 1;
    public int loveScore = 0;
    public int hunger = 50;
    public CatBreed myBreed = CatBreed.Cheese;
    public CatPersonality myPersonality = CatPersonality.Normal;
    public string lastExitTime; // 오프라인 시간 계산용
    public string evolutionStage;
}

public class DataManager
{

    public Dictionary<int, Data.TestData> TestDic { get; private set; } = new Dictionary<int, Data.TestData>();

    public int GotchaSize { get; private set; }

    public GameData CurrentData { get; private set; } = new GameData();

    private string _savePath => Path.Combine(Application.persistentDataPath, "SaveData.json");

    public void Init()
    {

        LoadGame();

        Debug.Log("Managers.Data Init 완료");
    }

    public void SaveGame()
    {
        CurrentData.lastExitTime = DateTime.Now.ToString();

        string json = JsonUtility.ToJson(CurrentData, true);

        File.WriteAllText(_savePath, json);
        Debug.Log($"게임 저장됨: {_savePath}");
    }

    public void LoadGame()
    {
        if (!File.Exists(_savePath))
        {
            CurrentData = new GameData();
            CurrentData.lastExitTime = DateTime.Now.ToString();
            SaveGame();
            Debug.Log("세이브 파일이 없어 새로 생성했습니다.");
            return;
        }

        string json = File.ReadAllText(_savePath);

        CurrentData = JsonUtility.FromJson<GameData>(json);
        Debug.Log("게임 불러오기 성공!");
    }

    //private Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    //{
    //    TextAsset textAsset = Managers.Resource.Load<TextAsset>("Data\\" + path);
    //    return JsonConvert.DeserializeObject<Loader>(textAsset.text);
    //}

    //private Loader LoadJson<Loader, Value>(string path) where Loader : ILoader<Value>
    //{
    //    TextAsset textAsset = Managers.Resource.Load<TextAsset>("Data\\" + path);
    //    return JsonConvert.DeserializeObject<Loader>(textAsset.text);
    //}

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
