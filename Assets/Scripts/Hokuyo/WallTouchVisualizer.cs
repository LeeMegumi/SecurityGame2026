using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WallTouchVisualizer : MonoBehaviour
{
    [Header("─── 元件參考 ───")]
    public WallTouchDetector detector;

    [Header("視覺物件-")]
    public RectTransform touchPointPrefab;

    [Header("UI觸發物件-")]
    public RectTransform triggerPointPrefab;
    public RectTransform USTcanvasRect;
    public RectTransform resizeCanvasRect;

    [Header("─── Particle 設定 ───")]
    public Camera particleCamera;         // 拖入你專門渲染 Particle 的 Camera
    public string UIParticleLayer = "UIParticle"; // 你設定的 Layer 名稱
    public float particleDepth = 10f;  // ← 新增這行，方便調整

    [Header("─── 視覺設定 ───")]
    public Color USTtouchColor = new Color(0f, 0.8f, 1f, 0.85f);
    public Color triggerTouchColor = new Color(0f, 0f, 0f, 0.85f);
    public float touchDotSize = 60f;
    public float lerpSpeed = 15f;

    public UITo3DSpawner spawner;

    [Header("UI觸發物件可視化")]
    public bool triggerVisualDebug;

    private const float REF_W = 1920f;
    private const float REF_H = 1080f;

    private List<RectTransform> UST_dots = new List<RectTransform>();
    private List<RectTransform> resizeTrigger_dots = new List<RectTransform>();

    // ── 新增：Particle 物件池 ──────────────────────────
    private List<GameObject> activeParticles = new List<GameObject>();


    void Start()
    {
        if (detector == null) { Debug.LogError("[Visualizer] 請指定 WallTouchDetector"); return; }
        //detector.onTouchDown.AddListener(SyncDots);
        //detector.onTouchMove.AddListener(SyncDots);
        //detector.onTouchUp.AddListener(HideAllDots);
        detector.onTouchDown.AddListener(OnTouchDown);  // 改成各自的 handler
        detector.onTouchMove.AddListener(OnTouchMove);
        detector.onTouchUp.AddListener(HideAllDots);


    }

    private void SyncDots(List<Vector2> points)
    {
        // ── UST dots ────────────────────────────────────
        while (UST_dots.Count < points.Count)
        {
            var dot = Instantiate(touchPointPrefab, USTcanvasRect);
            dot.sizeDelta = Vector2.one * touchDotSize;
            var img = dot.GetComponent<Image>();
            if (img != null) img.color = USTtouchColor;
            UST_dots.Add(dot);
        }
        for (int i = 0; i < UST_dots.Count; i++)
        {
            if (i < points.Count)
            {
                if (!Main.instance.SpawnBulletAllow)
                {

                    Vector2 target = WallToCanvas(points[i]);
                    Vector2 screenTarget = target + new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    spawner.SpawnBulletAtUIPosition(screenTarget);

                    UST_dots[i].anchoredPosition = Vector2.Lerp(
                        UST_dots[i].anchoredPosition, target, Time.deltaTime * lerpSpeed);

                    UST_dots[i].gameObject.SetActive(true);
                }
            }
            else
            {
                UST_dots[i].gameObject.SetActive(false);
            }
        }

        // ── Resize Trigger dots ─────────────────────────
        while (resizeTrigger_dots.Count < points.Count)
        {
            var dot = Instantiate(triggerPointPrefab, resizeCanvasRect);
            dot.sizeDelta = Vector2.one * touchDotSize;
            var img = dot.GetComponent<Image>();
            var alphaGroup = dot.GetComponent<CanvasGroup>();
            if (img != null) img.color = triggerTouchColor;
            if (alphaGroup != null) alphaGroup.alpha = triggerVisualDebug ? 1 : 0;
            resizeTrigger_dots.Add(dot);
        }

        for (int i = 0; i < resizeTrigger_dots.Count; i++)
        {
            if (i < points.Count)
            {
                resizeTrigger_dots[i].gameObject.SetActive(true);
                var alphaGroup = resizeTrigger_dots[i].GetComponent<CanvasGroup>();
                if (alphaGroup != null) alphaGroup.alpha = triggerVisualDebug ? 1 : 0;

                Vector2 target = WallToCanvas(points[i]);
                Vector2 screenTarget = target + new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                spawner.SpawnBulletAtUIPosition(screenTarget);

                resizeTrigger_dots[i].anchoredPosition = Vector2.Lerp(
                    resizeTrigger_dots[i].anchoredPosition, target, Time.deltaTime * lerpSpeed);
            }
            else
            {
                resizeTrigger_dots[i].gameObject.SetActive(false);
            }
        }
    }

    private void HideAllDots()
    {
        foreach (var d in UST_dots) d.gameObject.SetActive(false);
        foreach (var d in resizeTrigger_dots) d.gameObject.SetActive(false);

        // Particle 歸還給 Pool
        /*foreach (var p in activeParticles)
            if (p != null) p.SetActive(false);
        activeParticles.Clear();*/
    }


    // ═══════════════════════════════════════════════════
    // anchoredPosition（Canvas 中心為原點）→ World Position
    // Screen Space Overlay Canvas 直接用 TransformPoint 轉換
    // ═══════════════════════════════════════════════════
    private Vector3 AnchoredToWorldPos(RectTransform canvasRect, Vector2 anchoredPos)
    {
        // anchoredPos 是以 Canvas 中心為 (0,0) 的座標
        // TransformPoint 將 Canvas Local Space → World Space
        return canvasRect.TransformPoint(new Vector3(anchoredPos.x, anchoredPos.y, 0f));
    }

    // ═══════════════════════════════════════════════════
    // 遞迴設定所有子物件的 Layer（確保 Particle 子節點也在正確 Layer）
    // ═══════════════════════════════════════════════════
    private void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private Vector2 WallToCanvas(Vector2 wallPosMm)
    {
        float cw = USTcanvasRect.rect.width;
        float ch = USTcanvasRect.rect.height;

        float nx = (wallPosMm.x - detector.rectX) / detector.rectWidth;
        float ny = (wallPosMm.y - detector.rectY) / detector.rectHeight;

        return new Vector2(
            ((nx - 0.5f) * cw),
            ((ny - 0.5f) * ch));
    }

    // 把 Canvas anchoredPosition 轉成 Particle Camera 可見的 World Position
    private Vector3 AnchoredToWorldPos(Vector2 anchoredPos)
    {
        // anchoredPosition 的範圍就是 Canvas 的一半寬高
        // 對應到 Camera 在某個 Z 深度下的視野範圍
        float z = 10f; // Particle 距離 Camera 的深度，依你場景調整

        // 把 anchoredPos 轉成 0~1 的 Viewport 座標
        float viewportX = (anchoredPos.x / USTcanvasRect.rect.width) + 0.5f;
        float viewportY = (anchoredPos.y / USTcanvasRect.rect.height) + 0.5f;

        // 用 Particle Camera 的 ViewportToWorldPoint 轉換
        Vector3 worldPos = particleCamera.ViewportToWorldPoint(
            new Vector3(viewportX, viewportY, particleDepth)
        );

        return worldPos;
    }

    void OnDestroy()
    {
        if (detector == null) return;
        detector.onTouchDown.RemoveListener(SyncDots);
        detector.onTouchMove.RemoveListener(SyncDots);
        detector.onTouchUp.RemoveListener(HideAllDots);

    }

    // ── TouchDown：只在這裡 Get Particle ──────────────
    private void OnTouchDown(List<Vector2> points)
    {
        SyncDots(points);

        if(Main.instance.SpawnBulletAllow) return;
        // 先清掉上次殘留的
        foreach (var p in activeParticles)
            if (p != null) p.SetActive(false);
        activeParticles.Clear();

        foreach (var point in points)
        {
            Vector2 canvasPos = WallToCanvas(point);
            Vector3 worldPos = AnchoredToWorldPos(canvasPos);

            GameObject particle = UIParticle_Pool.instance.Get(worldPos);
            activeParticles.Add(particle);
        }

        // Dot 同步也在這裡處理
       
    }

    // ── TouchMove：只更新位置，不產生新 Particle ──────
    private void OnTouchMove(List<Vector2> points)
    {
        // Dot 同步
        SyncDots(points);
        if (Main.instance.SpawnBulletAllow) return;
        for (int i = 0; i < activeParticles.Count; i++)
        {
            if (i < points.Count && activeParticles[i] != null && activeParticles[i].activeSelf)
            {
                Vector2 canvasPos = WallToCanvas(points[i]);
                Vector3 worldPos = AnchoredToWorldPos(canvasPos);
                activeParticles[i].transform.position = worldPos;
            }
        }

        
    }

    
}