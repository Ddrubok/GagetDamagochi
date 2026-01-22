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
    public GameObject SpawnEffect(string prefabName, Vector3 position,Transform Parent = null, float duration = 2.0f)
    {
        // 1. 프리팹 찾기 (false를 넘겨서 Objects 폴더 말고 다른 곳도 찾게 함)
        GameObject prefab = GetPrefab(prefabName, false);

        if (prefab == null)
        {
            Debug.LogError($"[ObjectManager] 이펙트 프리팹을 찾을 수 없습니다: {prefabName}");
            return null;
        }

        // 2. 생성
        GameObject go = Object.Instantiate(prefab);
        if(Parent!=null)
            go.GetComponent<RectTransform>().parent = Parent;
        go.transform.position = position;
        go.name = $"{prefabName}_Effect";

        // 3. 파티클 재생 후 자동 삭제 예약
        // (파티클은 굳이 DicGameObject에 넣어 관리할 필요가 보통 없습니다)
        Object.Destroy(go, duration);

        return go;
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

    private GameObject GetPrefab(string name, bool IsObject = true)
    {
        if (_prefabCache.TryGetValue(name, out GameObject prefab))
            return prefab;

        // 1. Objects 폴더 검색 (주로 캐릭터, 몬스터 등)
        if (IsObject)
        {
            prefab = Resources.Load<GameObject>($"Prefabs/Objects/{name}");
        }
        else
        {
            // 2. ★ Effects 폴더 우선 검색 (파티클)
            prefab = Resources.Load<GameObject>($"Prefabs/Effects/{name}");

            // 3. 없으면 그냥 Prefabs 폴더 검색 (기타)
            if (prefab == null)
                prefab = Resources.Load<GameObject>($"Prefabs/{name}");
        }

        // 최종적으로 없으면 루트 검색
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