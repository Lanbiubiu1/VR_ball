using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatManager : MonoBehaviour
{
    [Header("球体碰撞设置")]
    [Tooltip("按下触发键时进行球体重叠检测的半径")]
    public float sphereRadius = 0.15f;

    [Tooltip("只检测这些图层上的碰撞体")]
    public LayerMask detectionLayers = ~0;

    [Tooltip("是否在场景视图绘制调试球体和命中线段")]
    public bool drawGizmos = true;

    [Header("输入设置（Meta/Oculus）")]
    [Tooltip("使用哪个手的触发键进行检测")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Tooltip("使用的触发键类型（IndexTrigger 或 HandTrigger）")]
    public OVRInput.Axis1D triggerAxis = OVRInput.Axis1D.PrimaryIndexTrigger;

    [Tooltip("触发阈值（0-1），超过该值视为按下")]
    [Range(0f, 1f)] public float triggerThreshold = 0.5f;

    [Header("可选：最大数量与回调")]
    [Tooltip("OverlapSphere 返回的最大结果数量（0 为不限制）")]
    public int maxResults = 16;

    [Tooltip("检测到的标签（留空不限制）")]
    public string requiredTag = string.Empty;

    [Header("命中后绑定设置")]
    [Tooltip("命中并需要附着的标签名称")] public string attachTag = "Bat";
    [Tooltip("附着后是否将局部位置归零")] public bool resetLocalPosition = true;
    [Tooltip("附着后是否将局部旋转归零")] public bool resetLocalRotation = true;
    [Tooltip("附着后是否将局部缩放设为1")] public bool resetLocalScale = false;
    [Tooltip("附着时如果存在刚体是否设为Kinematic")] public bool setRigidbodyKinematicOnAttach = true;

    // 最近一次的命中缓存
    private readonly List<Collider> _hits = new List<Collider>(32);

    // Start is called before the first frame update
    void Start()
    {
        // 可根据需要在此进行初始化
    }

    // Update is called once per frame
    void Update()
    {
        // 读取 Meta/Oculus 触发键的值
        float triggerValue = OVRInput.Get(triggerAxis, controller);
        if (triggerValue >= triggerThreshold)
        {
            DoSphereDetection();
        }
    }

    private void DoSphereDetection()
    {
        Vector3 origin = transform.position;
        int count;

        _hits.Clear();

        // 使用非分配版本以减少 GC
        Collider[] buffer = maxResults > 0 ? new Collider[maxResults] : new Collider[64];
        count = Physics.OverlapSphereNonAlloc(origin, sphereRadius, buffer, detectionLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider col = buffer[i];
            if (col == null) continue;

            if (!string.IsNullOrEmpty(requiredTag) && !col.CompareTag(requiredTag))
                continue;

            _hits.Add(col);
            HandleHit(col);
        }

        if (_hits.Count == 0)
        {
            Debug.Log("[BatManager] 触发检测：未命中任何碰撞体");
        }
    }

    private void HandleHit(Collider col)
    {
        // 在此实现命中逻辑，比如：
        // - 给球体施加力
        // - 发送事件或调用被命中对象上的脚本
        // 当前示例仅输出日志
        Debug.LogFormat("[BatManager] 命中对象：{0}（Layer: {1}）", col.name, LayerMask.LayerToName(col.gameObject.layer));

        // 示例：如果被命中物体带有刚体，则给一个朝向控制器前方的轻微力
        Rigidbody rb = col.attachedRigidbody;
        if (rb != null)
        {
            Vector3 direction = transform.forward;
            rb.AddForce(direction * 2f, ForceMode.Impulse);
        }

        // 如果标签匹配需要附着的标签，则进行父子关系绑定
        if (col.CompareTag(attachTag))
        {
            // 已经是子物体则跳过
            if (col.transform.parent == transform) return;

            // 如果需要停止物理影响
            if (rb != null && setRigidbodyKinematicOnAttach)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            col.transform.SetParent(transform);

            if (resetLocalPosition) col.transform.localPosition = Vector3.zero;
            if (resetLocalRotation) col.transform.localRotation = Quaternion.identity;
            if (resetLocalScale) col.transform.localScale = Vector3.one;

            Debug.LogFormat("[BatManager] 已附着对象 {0} 到控制器 {1}", col.name, name);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, sphereRadius);
    }
}
