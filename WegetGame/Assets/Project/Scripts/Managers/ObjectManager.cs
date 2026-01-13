using System.Collections.Generic;
using UnityEngine;
using static Define;

public class ObjectManager
{
    public Dictionary<string, Sprite> DicSprite = new Dictionary<string, Sprite>();

    private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

    public Dictionary<string, GameObject> DicGameObject = new Dictionary<string, GameObject>();

    public T Spawn<T>(Vector3 position, int templateID = 0, Transform parent = null) where T : BaseController
    {
        string prefabName = typeof(T).Name;


        GameObject prefab = GetPrefab(prefabName);
        if (prefab == null)
        {
            Debug.LogError($"[ObjectManager] 프리팹을 찾을 수 없습니다: {prefabName}. Resources/Prefabs 폴더에 파일이 있는지 확인하세요.");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, parent);
        go.transform.position = position;
        go.name = $"{prefabName}_{go.GetInstanceID()}";
        T controller = go.GetComponent<T>();
        if (controller == null)
        {
            controller = go.AddComponent<T>();
        }

        if (controller.Init() == false)
        {
            Debug.LogError($"[ObjectManager] {prefabName} 초기화(Init) 실패");
            Object.Destroy(go);
            return null;
        }

        DicGameObject.Add(go.GetInstanceID().ToString(), go);

        return controller;
    }

    public void Despawn<T>(T obj) where T : BaseController
    {
        if (obj == null) return;

        string key = obj.gameObject.GetInstanceID().ToString();
        if (DicGameObject.ContainsKey(key))
        {
            DicGameObject.Remove(key);
        }

        Object.Destroy(obj.gameObject);
    }

    private GameObject GetPrefab(string name)
    {
        if (_prefabCache.TryGetValue(name, out GameObject prefab))
            return prefab;

        prefab = Resources.Load<GameObject>($"Prefabs/{name}");

        if (prefab == null)
            prefab = Resources.Load<GameObject>(name);

        if (prefab != null)
            _prefabCache.Add(name, prefab);

        return prefab;
    }

    public Sprite GetSprite(string name)
    {
        Sprite newSprite;
        if (!DicSprite.TryGetValue(name, out newSprite))
        {
            newSprite = Resources.Load<Sprite>(name);
            if (newSprite != null)
                DicSprite.Add(name, newSprite);
        }
        if (newSprite == null)
            Debug.Log($"스프라이트 로드 실패: {name}");

        return newSprite;
    }
}