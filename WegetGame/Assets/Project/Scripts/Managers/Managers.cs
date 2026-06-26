using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Managers : MonoBehaviour
{
    private static Managers s_instance;

    private static bool s_isQuitting = false;

    private static ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

    // 외부에서 언제든 메인 스레드에 작업을 던질 수 있는 글로벌 함수
    public static void QueueOnMainThread(Action action)
    {
        if (action != null)
        {
            _mainThreadActions.Enqueue(action);
        }
    }
    public static Managers Instance
    {
        get
        {
            if (s_isQuitting)
            {
                return null;
            }
            if (s_instance == null)
                Init();
            return s_instance;
        }
    }


    #region Core
    private GameManager _game;
    private DataManager _data;
    private ResourceManager _resource;
    private SoundManager _sound;
    private UIManager _ui;
    private ObjectManager _object;
    private PoolManager _pool;
    private SceneManager _sceneManager;

    public static GameManager Game { get { return Instance?._game; } }
    public static DataManager Data { get { return Instance?._data; } }
    public static ResourceManager Resource { get { return Instance?._resource; } }
    public static SoundManager Sound { get { return Instance?._sound; } }
    public static UIManager UI { get { return Instance?._ui; } }

    public static ObjectManager Object { get { return Instance?._object; } }

    public static PoolManager Pool { get { return Instance?._pool; } }

    public static SceneManager Scene { get { return Instance?._sceneManager; } }

    #endregion

    public static void Init()
    {
        if (s_instance == null)
        {

            if (s_isQuitting) return;
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.GetOrAddComponent<Managers>();
            }

            DontDestroyOnLoad(go);

            // 초기화
            s_instance = go.GetComponent<Managers>();

            s_instance._game = new GameManager();
           // s_instance._game = go.GetOrAddComponent<GameManager>();
            s_instance._data = new DataManager();
            s_instance._resource = new ResourceManager();
            s_instance._sound = s_instance.AddComponent<SoundManager>();
            s_instance._ui = new UIManager();
            s_instance._object = new ObjectManager();
            s_instance._pool = new PoolManager();

            s_instance._data.Init();
            s_instance._game.Init();
        }
    }
    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Game.Save();
           
        }
    }

    private void OnApplicationQuit()
    {
        Game.Save();
        if (s_instance != null)
        {
            s_isQuitting = true;
            s_instance = null;
            Destroy(gameObject);
        }
       
    }

    private void Update()
    {
        _game?.OnUpdate();

        while (_mainThreadActions.TryDequeue(out Action action))
        {
            action?.Invoke();
        }
    }
}
