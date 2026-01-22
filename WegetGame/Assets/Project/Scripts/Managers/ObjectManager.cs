using System.Collections.Generic;
using UnityEngine;
using static Define;

public class ObjectManager
{
    public Dictionary<string, Sprite> DicSprite = new Dictionary<string, Sprite>();

    // 프리팹 원본 저장 (로딩 최적화)
    private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

    // ★ 1. 활성화된(Active) 오브젝트 관리 (기존 유지)
    public Dictionary<string, GameObject> DicGameObject = new Dictionary<string, GameObject>();

    // ★ 2. 비활성화된(Inactive) 오브젝트 대기소 (풀링용 큐)
    // string: 프리팹 이름 (Key), Queue: 대기줄
    private Dictionary<string, Queue<GameObject>> _pool = new Dictionary<string, Queue<GameObject>>();


    // ==========================================================
    // 일반 오브젝트 (Controller) 소환
    // ==========================================================
    public T Spawn<T>(Vector3 position, int templateID = 0, Transform parent = null) where T : BaseController
    {
        string prefabName = typeof(T).Name;

        // 1. 풀링 시스템을 통해 오브젝트 가져오기 (없으면 새로 만들고, 있으면 꺼내옴)
        GameObject go = SpawnGameObject(prefabName, position, parent);

        if (go == null) return null;

        // 2. 컴포넌트 가져오기
        T controller = go.GetComponent<T>();

        // 3. 초기화 (재사용된 객체일 수 있으므로 Init을 다시 호출해주는 것이 좋음)
        if (controller.Init() == false)
        {
            Debug.LogError($"[ObjectManager] {prefabName} 초기화(Init) 실패");
            // 초기화 실패 시 다시 반납(Despawn)하거나 파괴
            Despawn(go);
            return null;
        }

        // ★ 4. 활성 목록(DicGameObject)에 등록 (기존 유지)
        string key = go.GetInstanceID().ToString();
        if (!DicGameObject.ContainsKey(key))
            DicGameObject.Add(key, go);

        return controller;
    }

    // ==========================================================
    // 이펙트/UI 소환 (SpawnEffect)
    // ==========================================================
    // duration은 이제 여기서 쓰지 않습니다. (각 스크립트가 스스로 반납해야 함)
    public GameObject SpawnEffect(string prefabName, Vector3 position, Transform parent = null)
    {
        // 1. 풀링으로 가져오기
        GameObject go = SpawnGameObject(prefabName, position, parent,false);

        if (go == null)
        {
            Debug.LogError($"[ObjectManager] 이펙트 소환 실패: {prefabName}");
            return null;
        }

        // 이펙트는 보통 DicGameObject에 ID로 등록할 필요가 없어서 생략합니다.
        // (필요하다면 위 Spawn<T>처럼 등록해도 됩니다)

        return go;
    }

    // ==========================================================
    // ★ 핵심: 내부 소환 로직 (Create or Reuse)
    // ==========================================================
    private GameObject SpawnGameObject(string prefabName, Vector3 position, Transform parent,bool IsObject =true)
    {
        GameObject go = null;

        // 1. 창고(_pool)에 재고가 있는지 확인
        if (_pool.ContainsKey(prefabName) && _pool[prefabName].Count > 0)
        {
            // 재고가 있으면 꺼낸다 (Dequeue)
            go = _pool[prefabName].Dequeue();

            // 위치/부모 재설정
            go.transform.position = position;
            go.transform.SetParent(parent);

            // ★ 켜기 (On)
            go.SetActive(true);
        }
        else
        {
            // 2. 재고가 없으면 새로 만든다 (Instantiate)
            GameObject prefab = GetPrefab(prefabName, IsObject); // false: 전체 검색
            if (prefab == null) return null;

            go = Object.Instantiate(prefab, parent);
            go.transform.position = position;
            go.name = prefabName; // (Clone) 안 붙게 이름 깔끔하게
        }

        return go;
    }

    // ==========================================================
    // ★ 핵심: 반납 로직 (Despawn) - Destroy 대신 끄기
    // ==========================================================
    public void Despawn(GameObject go)
    {
        if (go == null) return;

        // 1. 활성 목록(DicGameObject)에 있다면 제거
        string idKey = go.GetInstanceID().ToString();
        if (DicGameObject.ContainsKey(idKey))
        {
            DicGameObject.Remove(idKey);
        }

        // 2. 풀링(창고)에 넣기 준비
        string poolKey = go.name; // 프리팹 이름을 키로 사용

        if (!_pool.ContainsKey(poolKey))
        {
            _pool.Add(poolKey, new Queue<GameObject>());
        }

        // 3. ★ 끄기 (Off)
        go.SetActive(false);

        // 4. 창고에 넣기 (Enqueue)
        _pool[poolKey].Enqueue(go);
    }

    // 편의성을 위한 오버로딩 (BaseController용)
    public void Despawn<T>(T obj) where T : BaseController
    {
        if (obj != null) Despawn(obj.gameObject);
    }

    // ==========================================================
    // 리소스 로드 (기존 유지)
    // ==========================================================
    private GameObject GetPrefab(string name, bool IsObject)
    {
        if (_prefabCache.TryGetValue(name, out GameObject prefab))
            return prefab;

        if (IsObject)
        {
            prefab = Resources.Load<GameObject>($"Prefabs/Objects/{name}");
        }
        else
        {
            prefab = Resources.Load<GameObject>($"Prefabs/Effects/{name}");
            if (prefab == null)
                prefab = Resources.Load<GameObject>($"Prefabs/{name}");
        }

        if (prefab == null)
            prefab = Resources.Load<GameObject>(name);

        if (prefab != null)
            _prefabCache.Add(name, prefab);

        return prefab;
    }

    public Sprite GetSprite(string name)
    {
        // (기존 코드와 동일)
        Sprite newSprite;
        if (!DicSprite.TryGetValue(name, out newSprite))
        {
            newSprite = Resources.Load<Sprite>(name);
            if (newSprite != null) DicSprite.Add(name, newSprite);
        }
        return newSprite;
    }
}