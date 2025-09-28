using UnityEngine;
using TMPro; // 推荐用TextMeshPro，比UGUI Text性能更好
using System.Collections;
using UnityEngine.UI;

[System.Serializable]
public class EndlessVictoryUIConfig
{
    public GameObject victoryUIPrefab; // 通用胜利UI预制体（含背景图+文本）
    public float showDuration = 2f; // 显示时长
    public float fadeInTime = 0.3f; // 淡入时间
    public float fadeOutTime = 0.3f; // 淡出时间
    public string commonBgSpritePath = "VictorySprites/EndlessCommonBg"; // 通用背景图路径
    public string levelTextFormat = "第{0}关完成！"; // 文本格式（{0}替换为关卡数）
}

public class EndlessVictoryUIManager : MonoBehaviour
{
    public static EndlessVictoryUIManager Instance;

    [SerializeField] private EndlessVictoryUIConfig config;
    private GameObject _currentUIInstance;
    private Image _bgImage; // 通用背景图
    private TMP_Text _levelText; // 动态显示关卡数的文本
    private Sprite _commonBgSprite; // 预加载的通用背景图（全程复用）
    private bool _isShowing = false;

    // 对象池（仅需1个实例）
    private ObjectPool<GameObject> _uiPool;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitObjectPool();
            PreloadCommonResources(); // 预加载通用资源（仅一次）
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 预加载通用背景图（游戏启动时加载，全程复用）
    private void PreloadCommonResources()
    {
        // 同步加载（通用图体积小，一次加载无压力）
        _commonBgSprite = Resources.Load<Sprite>(config.commonBgSpritePath);
        if (_commonBgSprite == null)
        {
            Debug.LogError("未找到通用胜利背景图：" + config.commonBgSpritePath);
        }
    }

    private void InitObjectPool()
    {
        _uiPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(config.victoryUIPrefab, transform),
            actionOnGet: (obj) => 
            { 
                obj.SetActive(true);
                // 缓存组件引用（避免重复GetComponent，提升性能）
                _bgImage = obj.GetComponent<Image>();
                _levelText = obj.GetComponentInChildren<TMP_Text>();
                // 初始化状态：透明
                _bgImage.canvasRenderer.SetAlpha(0);
                _levelText.canvasRenderer.SetAlpha(0);
            },
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            defaultCapacity: 1
        );
    }

    /// <summary>
    /// 显示无尽模式下的关卡胜利UI
    /// </summary>
    /// <param name="currentLevel">当前关卡数（如1,2,3...1000...）</param>
    public void ShowEndlessVictoryUI(int currentLevel)
    {
        if (_isShowing || currentLevel < 1) return;

        StartCoroutine(CoShowVictoryUI(currentLevel));
    }

    private IEnumerator CoShowVictoryUI(int currentLevel)
    {
        _isShowing = true;
        _currentUIInstance = _uiPool.Get();

        // 1. 设置通用背景图（复用预加载的Sprite，无IO操作）
        if (_commonBgSprite != null)
        {
            _bgImage.sprite = _commonBgSprite;
        }

        // 2. 动态更新关卡文本（仅修改文本内容，无资源加载）
        _levelText.text = string.Format(config.levelTextFormat, currentLevel);

        // 3. 淡入动画（同时激活背景和文本）
        _bgImage.CrossFadeAlpha(1, config.fadeInTime, false);
        _levelText.CrossFadeAlpha(1, config.fadeInTime, false);
        yield return new WaitForSeconds(config.fadeInTime + config.showDuration);

        // 4. 淡出动画
        _bgImage.CrossFadeAlpha(0, config.fadeOutTime, false);
        _levelText.CrossFadeAlpha(0, config.fadeOutTime, false);
        yield return new WaitForSeconds(config.fadeOutTime);

        // 5. 回收UI实例（不释放通用资源，因为还要复用）
        _uiPool.Release(_currentUIInstance);
        _isShowing = false;
    }

    // 复用之前的对象池实现（完全通用，无需修改）
    public class ObjectPool<T> where T : class
    {
        // （代码同之前的ObjectPool，省略重复内容）
        private readonly System.Func<T> _createFunc;
        private readonly System.Action<T> _actionOnGet;
        private readonly System.Action<T> _actionOnRelease;
        private readonly System.Action<T> _actionOnDestroy;
        private readonly System.Collections.Generic.Queue<T> _pool = new System.Collections.Generic.Queue<T>();

        public int Count { get; private set; }

        public ObjectPool(System.Func<T> createFunc, System.Action<T> actionOnGet, System.Action<T> actionOnRelease, System.Action<T> actionOnDestroy, int defaultCapacity = 0)
        {
            _createFunc = createFunc ?? throw new System.ArgumentNullException(nameof(createFunc));
            _actionOnGet = actionOnGet;
            _actionOnRelease = actionOnRelease;
            _actionOnDestroy = actionOnDestroy;

            for (int i = 0; i < defaultCapacity; i++)
            {
                T item = _createFunc();
                _actionOnRelease?.Invoke(item);
                _pool.Enqueue(item);
            }
            Count = _pool.Count;
        }

        public T Get()
        {
            T item;
            if (_pool.Count == 0)
            {
                item = _createFunc();
            }
            else
            {
                item = _pool.Dequeue();
            }
            _actionOnGet?.Invoke(item);
            Count--;
            return item;
        }

        public void Release(T item)
        {
            if (_pool.Contains(item))
            {
                Debug.LogWarning("尝试释放已在池中的对象");
                return;
            }
            _actionOnRelease?.Invoke(item);
            _pool.Enqueue(item);
            Count++;
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T item = _pool.Dequeue();
                _actionOnDestroy?.Invoke(item);
            }
            Count = 0;
        }
    }
}