using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 牆面觸控 3D 特效
/// 在觸碰位置的 3D 空間產生粒子或其他效果
///
/// 使用步驟：
///   1. 製作一個粒子效果 Prefab（波紋/光暈等）
///   2. 建立 GameObject，掛此腳本
///   3. 設定 wallTransform = 牆面 Plane 的 Transform
/// </summary>
public class WallTouchEffect : MonoBehaviour
{
    [Header("─── 元件參考 ───")]
    public WallTouchDetector detector;
    [Tooltip("牆面 Plane 的 Transform")]
    public Transform wallTransform;

    [Header("─── 牆面 3D 尺寸 (Unity 單位) ───")]
    [Tooltip("與 wallWidth mm 對應的 Unity 實際寬度")]
    public float wall3DWidth  = 10f;
    [Tooltip("與 wallHeight mm 對應的 Unity 實際高度")]
    public float wall3DHeight = 5.625f;

    [Header("─── 觸碰特效 ───")]
    [Tooltip("觸碰時在牆面產生的粒子 Prefab（留 null 僅顯示 Log）")]
    public ParticleSystem ripplePrefab;
    [Tooltip("特效自動消失時間 (秒)")]
    public float effectLifetime = 1.5f;

    private List<ParticleSystem> _activeEffects = new List<ParticleSystem>();

    void Start()
    {
        if (detector == null) { Debug.LogError("[TEffect] 請指定 WallTouchDetector"); return; }
        detector.onTouchDown.AddListener(OnTouchDown);
        detector.onTouchMove.AddListener(OnTouchMove);
        detector.onTouchUp.AddListener(OnTouchUp);
    }

    private void OnTouchDown(List<Vector2> points)
    {
        _activeEffects.Clear();
        foreach (var p in points) SpawnEffect(p);
    }

    private void OnTouchMove(List<Vector2> points)
    {
        for (int i = 0; i < _activeEffects.Count && i < points.Count; i++)
            if (_activeEffects[i] != null)
                _activeEffects[i].transform.position = WallPosToWorld(points[i]);
    }

    private void OnTouchUp()
    {
        foreach (var fx in _activeEffects)
            if (fx != null) Destroy(fx.gameObject, effectLifetime);
        _activeEffects.Clear();
    }

    private void SpawnEffect(Vector2 wallPosMm)
    {
        Vector3 worldPos = WallPosToWorld(wallPosMm);
        if (ripplePrefab != null)
        {
            var fx = Instantiate(ripplePrefab, worldPos,
                wallTransform != null ? wallTransform.rotation : Quaternion.identity);
            fx.Play();
            _activeEffects.Add(fx);
        }
        else
        {
            Debug.Log($"[TEffect] Touch @ {worldPos} (Wall: {wallPosMm} mm)");
        }
    }

    // 牆面 mm 座標 → Unity 世界座標
    private Vector3 WallPosToWorld(Vector2 wallPosMm)
    {
        if (wallTransform == null) return Vector3.zero;
        float localX = (wallPosMm.x / detector.wallWidth  - 0.5f) * wall3DWidth;
        float localY = (wallPosMm.y / detector.wallHeight - 0.5f) * wall3DHeight;
        return wallTransform.TransformPoint(new Vector3(localX, localY, 0));
    }

    void OnDestroy()
    {
        if (detector == null) return;
        detector.onTouchDown.RemoveListener(OnTouchDown);
        detector.onTouchMove.RemoveListener(OnTouchMove);
        detector.onTouchUp.RemoveListener(OnTouchUp);
    }
}
