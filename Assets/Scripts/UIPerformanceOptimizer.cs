using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;

public class UIPerformanceOptimizer : MonoBehaviour
{
    [Header("Performance Settings")]
    [Tooltip("是否启用批处理优化")]
    public bool enableBatching = true;
    
    [Tooltip("是否启用可见性优化")]
    public bool enableVisibilityOptimization = true;
    
    [Tooltip("是否启用事件系统优化")]
    public bool enableEventSystemOptimization = true;
    
    [Tooltip("是否启用对象池")]
    public bool enableObjectPooling = true;
    
    [Header("Batching Settings")]
    [Tooltip("批处理间隔时间")]
    public float batchingInterval = 0.5f;
    
    [Header("Object Pool Settings")]
    [Tooltip("最大对象池大小")]
    public int maxPoolSize = 100;
    
    [Header("References")]
    private Canvas canvas;
    private GraphicRaycaster graphicRaycaster;
    private EventSystem eventSystem;
    private Dictionary<GameObject, bool> visibilityCache = new Dictionary<GameObject, bool>();
    private Dictionary<Type, Queue<GameObject>> objectPools = new Dictionary<Type, Queue<GameObject>>();
    
    private float lastBatchingTime;
    
    private void Awake()
    {
        InitializeReferences();
        ApplyPerformanceSettings();
    }
    
    private void Start()
    {
        StartCoroutine(BatchingCoroutine());
    }
    
    private void InitializeReferences()
    {
        canvas = FindObjectOfType<Canvas>();
        graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
        eventSystem = FindObjectOfType<EventSystem>();
    }
    
    #region Performance Settings
    
    private void ApplyPerformanceSettings()
    {
        // 优化GraphicRaycaster
        if (graphicRaycaster != null)
        {
            graphicRaycaster.ignoreReversedGraphics = true;
            graphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }
        
        // 优化Canvas
        if (canvas != null)
        {
            canvas.planeDistance = 1000;
        }
        
        // 优化事件系统
        if (eventSystem != null && enableEventSystemOptimization)
        {
            eventSystem.sendNavigationEvents = false;
            eventSystem.pixelDragThreshold = 5;
        }
    }
    
    #endregion
    
    #region Batching Optimization
    
    private IEnumerator BatchingCoroutine()
    {
        while (true)
        {
            if (Time.time - lastBatchingTime >= batchingInterval)
            {
                lastBatchingTime = Time.time;
                PerformBatching();
            }
            yield return null;
        }
    }
    
    private void PerformBatching()
    {
        if (!enableBatching) return;
        
        // 收集所有需要批处理的UI元素
        List<Graphic> graphics = new List<Graphic>();
        Graphic[] allGraphics = FindObjectsOfType<Graphic>();
        
        foreach (Graphic graphic in allGraphics)
        {
            if (graphic != null && graphic.enabled)
            {
                graphics.Add(graphic);
            }
        }
        
        // 按材质和颜色分组进行批处理
        Dictionary<Material, List<Graphic>> materialGroups = new Dictionary<Material, List<Graphic>>();
        
        foreach (Graphic graphic in graphics)
        {
            if (graphic.material != null)
            {
                if (!materialGroups.ContainsKey(graphic.material))
                {
                    materialGroups[graphic.material] = new List<Graphic>();
                }
                materialGroups[graphic.material].Add(graphic);
            }
        }
        
        // 这里可以添加具体的批处理逻辑
        // 例如合并相同材质的渲染器等
    }
    
    #endregion
    
    #region Visibility Optimization
    
    public void UpdateVisibility()
    {
        if (!enableVisibilityOptimization) return;
        
        RectTransform[] allElements = FindObjectsOfType<RectTransform>();
        
        foreach (RectTransform element in allElements)
        {
            if (element.GetComponent<Canvas>() != null)
            {
                continue;
            }
            
            bool isVisible = IsElementVisible(element);
            
            if (visibilityCache.ContainsKey(element.gameObject))
            {
                if (visibilityCache[element.gameObject] != isVisible)
                {
                    visibilityCache[element.gameObject] = isVisible;
                    UpdateElementVisibility(element.gameObject, isVisible);
                }
            }
            else
            {
                visibilityCache[element.gameObject] = isVisible;
                UpdateElementVisibility(element.gameObject, isVisible);
            }
        }
    }
    
    private bool IsElementVisible(RectTransform element)
    {
        // 检查元素是否在相机视野内
        Vector3[] worldCorners = new Vector3[4];
        element.GetWorldCorners(worldCorners);
        
        Camera camera = canvas.worldCamera ?? Camera.main;
        if (camera == null) return true;
        
        foreach (Vector3 corner in worldCorners)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(corner);
            if (screenPoint.z > 0 && 
                screenPoint.x >= 0 && screenPoint.x <= Screen.width &&
                screenPoint.y >= 0 && screenPoint.y <= Screen.height)
            {
                return true;
            }
        }
        
        return false;
    }
    
    private void UpdateElementVisibility(GameObject element, bool isVisible)
    {
        // 启用/禁用渲染组件
        Graphic graphic = element.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.enabled = isVisible;
        }
        
        // 启用/禁用交互组件
        Selectable selectable = element.GetComponent<Selectable>();
        if (selectable != null)
        {
            selectable.interactable = isVisible;
        }
    }
    
    #endregion
    
    #region Object Pooling
    
    public GameObject GetPooledObject(Type objectType, GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!enableObjectPooling)
        {
            return Instantiate(prefab, position, rotation);
        }
        
        if (!objectPools.ContainsKey(objectType))
        {
            objectPools[objectType] = new Queue<GameObject>();
        }
        
        Queue<GameObject> pool = objectPools[objectType];
        
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }
        else if (CalculateSum(objectPools, p => p.Value.Count) < maxPoolSize)
        {
            GameObject obj = Instantiate(prefab, position, rotation);
            return obj;
        }
        else
        {
            Debug.LogWarning("Object pool is full");
            return Instantiate(prefab, position, rotation);
        }
    }
    
    public void ReturnObjectToPool(GameObject obj)
    {
        if (!enableObjectPooling)
        {
            Destroy(obj);
            return;
        }
        
        Type objectType = obj.GetType();
        
        if (!objectPools.ContainsKey(objectType))
        {
            objectPools[objectType] = new Queue<GameObject>();
        }
        
        obj.SetActive(false);
        objectPools[objectType].Enqueue(obj);
    }
    
    public void ClearPool(Type objectType)
    {
        if (objectPools.ContainsKey(objectType))
        {
            foreach (GameObject obj in objectPools[objectType])
            {
                Destroy(obj);
            }
            objectPools[objectType].Clear();
        }
    }
    
    public void ClearAllPools()
    {
        foreach (var pool in objectPools)
        {
            foreach (GameObject obj in pool.Value)
            {
                Destroy(obj);
            }
            pool.Value.Clear();
        }
        objectPools.Clear();
    }
    
    #endregion
    
    #region Event System Optimization
    
    public void OptimizeEventSystem()
    {
        if (!enableEventSystemOptimization) return;
        
        // 减少事件系统的开销
        if (eventSystem != null)
        {
            // 禁用不必要的导航事件
            eventSystem.sendNavigationEvents = false;
            
            // 调整拖拽阈值
            eventSystem.pixelDragThreshold = 5;
        }
        
        // 优化GraphicRaycaster
        if (graphicRaycaster != null)
        {
            // 忽略反向图形
            graphicRaycaster.ignoreReversedGraphics = true;
            
            // 设置合适的阻塞对象
            graphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }
    }
    
    #endregion
    
    #region Performance Monitoring
    
    public void LogPerformanceStats()
    {
        int activeGraphics = FindObjectsOfType<Graphic>().Length;
        int activeButtons = FindObjectsOfType<Button>().Length;
        int poolSize = CalculateSum(objectPools, p => p.Value.Count);
        
        Debug.Log($"UI Performance Stats:");
        Debug.Log($"  Active Graphics: {activeGraphics}");
        Debug.Log($"  Active Buttons: {activeButtons}");
        Debug.Log($"  Object Pool Size: {poolSize}/{maxPoolSize}");
    }
    
    #endregion
    
    #region Utility Methods
    
    [ContextMenu("Optimize Now")]
    public void OptimizeNow()
    {
        ApplyPerformanceSettings();
        OptimizeEventSystem();
        UpdateVisibility();
        PerformBatching();
        LogPerformanceStats();
    }
    
    [ContextMenu("Clear All Pools")]
    public void ClearAllPoolsContextMenu()
    {
        ClearAllPools();
    }
    
    #endregion
    
    // 辅助方法
    private int CalculateSum<T>(Dictionary<T, Queue<GameObject>> dict, Func<KeyValuePair<T, Queue<GameObject>>, int> selector)
    {
        int sum = 0;
        foreach (var pair in dict)
        {
            sum += selector(pair);
        }
        return sum;
    }
}