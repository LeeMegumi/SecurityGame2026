using System.Collections;
using UnityEngine;

public class FloorGenerator : MonoBehaviour
{
    [Header("地板 Prefab")]
    public GameObject floorPrefab;

    [Header("地板尺寸設定（單位：格）")]
    public int floorWidth = 5;
    public int floorDepth = 5;

    [Header("轉場動畫設定")]
    [Tooltip("每排開始翻轉的間隔時間（秒）")]
    public float rowDelay = 0.12f;
    [Tooltip("每塊地板翻轉動畫持續時間（秒）")]
    public float flipDuration = 0.55f;
    [Tooltip("翻轉目標角度（X 軸）")]
    public float targetXAngle = -90f;

    [Header("波浪位移設定")]
    [Tooltip("往下沉的最大距離（Unity 單位）")]
    public float waveDipDepth = 2.5f;
    [Tooltip("回彈超量比例（0 = 不超量，0.3 = 超量 30%）")]
    [Range(0f, 0.5f)]
    public float waveOvershoot = 0.2f;
    [Tooltip("下沉動畫佔整體的比例")]
    [Range(0.1f, 0.6f)]
    public float waveDipRatio = 0.35f;
    [Tooltip("回彈動畫佔整體的比例")]
    [Range(0.1f, 0.6f)]
    public float waveRiseRatio = 0.35f;

    [Header("材質切換設定")]
    [Tooltip("翻轉後要套用的目標材質球")]
    public Material[] targetMaterial;
    public int nextMatIndex;
    [Tooltip("換材質球的時機（動畫進度 0~1，建議 0.5）")]
    [Range(0f, 1f)]
    public float swapProgress = 0.5f;

    private const float TileSize = 10f;
    private const string AlphaProperty = "_Alpha";
    private GameObject[,] tiles;

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
        {
            PlayFlipAnimation();
        }
    }


    // ── 啟動 ─────────────────────────────────────────
    private void Start()
    {
        GenerateFloor();
    }

    // ── 生成地板 ──────────────────────────────────────
    [ContextMenu("Generate Floor")]
    public void GenerateFloor()
    {
        ClearFloor();

        if (floorPrefab == null)
        {
            Debug.LogError("[FloorGenerator] 請先指定 floorPrefab！");
            return;
        }

        tiles = new GameObject[floorWidth, floorDepth];

        float offsetX = (floorWidth - 1) * TileSize / 2f;
        float offsetZ = (floorDepth - 1) * TileSize / 2f;

        for (int z = 0; z < floorDepth; z++)
        {
            for (int x = 0; x < floorWidth; x++)
            {
                Vector3 localPos = new Vector3(
                    x * TileSize - offsetX,
                    0f,
                    z * TileSize - offsetZ
                );

                GameObject tile = Instantiate(
                    floorPrefab,
                    transform.TransformPoint(localPos),
                    transform.rotation,
                    transform
                );

                tile.name = $"Tile_{x}_{z}";
                tiles[x, z] = tile;
                SetTileAlpha(tile, 1f);
            }
        }

        Debug.Log($"[FloorGenerator] 生成完成：{floorWidth} x {floorDepth} 格，共 {floorWidth * floorDepth} 塊。");
    }

    // ── 播放翻轉轉場動畫 ──────────────────────────────
    [ContextMenu("Play Flip Animation")]
    public void PlayFlipAnimation()
    {
        targetXAngle -= 90;
        nextMatIndex += 1;

        if (tiles == null || tiles.Length == 0)
        {
            Debug.LogWarning("[FloorGenerator] 尚未生成地板，請先執行 Generate Floor。");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(FlipRowsCoroutine());
    }

    private IEnumerator FlipRowsCoroutine()
    {
        for (int z = floorDepth - 1; z >= 0; z--)
        {
            for (int x = 0; x < floorWidth; x++)
            {
                if (tiles[x, z] != null)
                    StartCoroutine(FlipTileCoroutine(tiles[x, z]));
            }

            yield return new WaitForSeconds(rowDelay);
        }
    }

    // ── 核心：翻轉 + 波浪 + 材質切換 ─────────────────
    private IEnumerator FlipTileCoroutine(GameObject tile)
    {
        Renderer rend = tile.GetComponent<Renderer>();
        Vector3 originPos = tile.transform.localPosition; // 記錄原始 Y 位置
        Quaternion startRot = tile.transform.localRotation;
        Quaternion endRot = Quaternion.Euler(targetXAngle, 0f, 0f);

        float elapsed = 0f;
        bool swapped = false;

        // 波浪三段時間節點（以 t 0~1 表示）
        float dipEnd = waveDipRatio;                    // 下沉結束點
        float riseEnd = waveDipRatio + waveRiseRatio;    // 回彈結束點
        // riseEnd ~ 1.0 之間：Y 穩定在原位，只剩旋轉與 Alpha 繼續

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flipDuration);

            // ════════════════════════════════════════
            //  1. 波浪 Y 位移
            // ════════════════════════════════════════
            float yOffset = 0f;

            if (t < dipEnd)
            {
                // 階段一：0 → dipEnd，往下沉
                float dipT = t / dipEnd;
                yOffset = Mathf.Lerp(0f, -waveDipDepth, EaseInCubic(dipT));
            }
            else if (t < riseEnd)
            {
                // 階段二：dipEnd → riseEnd，回彈（含超量）
                float riseT = (t - dipEnd) / (riseEnd - dipEnd);
                float overshoot = waveDipDepth * waveOvershoot; // 超量高度
                // 先彈過頭再收回
                yOffset = WaveOvershootCurve(riseT, -waveDipDepth, overshoot);
            }
            else
            {
                // 階段三：riseEnd ~ 1.0，Y 穩定在原位
                yOffset = 0f;
            }

            tile.transform.localPosition = originPos + new Vector3(0f, yOffset, 0f);

            // ════════════════════════════════════════
            //  2. Alpha 淡出 / 淡入
            // ════════════════════════════════════════
            if (t <= swapProgress)
            {
                float fadeOutT = t / swapProgress;
                SetTileAlpha(tile, Mathf.Lerp(1f, 0f, EaseInCubic(fadeOutT)));
            }

            if (!swapped && t >= swapProgress)
            {
                if (targetMaterial != null)
                    rend.material = targetMaterial[nextMatIndex % 3];  // 換材質球（此時 Alpha ≈ 0，視覺無感）
                SetTileAlpha(tile, 0f);
                swapped = true;
            }

            if (swapped && t >= swapProgress)
            {
                float fadeInT = (t - swapProgress) / (1f - swapProgress);
                SetTileAlpha(tile, Mathf.Lerp(0f, 1f, EaseOutCubic(fadeInT)));
            }

            // ════════════════════════════════════════
            //  3. 旋轉（全程執行）
            // ════════════════════════════════════════
            tile.transform.localRotation = Quaternion.Lerp(startRot, endRot, EaseOutCubic(t));

            yield return null;
        }

        // 確保最終狀態精確
        tile.transform.localPosition = originPos;
        tile.transform.localRotation = endRot;
        SetTileAlpha(tile, 1f);
    }

    // ── 回彈曲線：從 fromY 彈到超量高度再收回 0 ──────
    // t: 0~1，fromY: 起始Y（負值），overshoot: 超量正值
    private static float WaveOvershootCurve(float t, float fromY, float overshoot)
    {
        // 分兩小段：0~0.6 從 fromY 彈到 +overshoot，0.6~1 從 overshoot 收回 0
        if (t < 0.6f)
        {
            float t2 = t / 0.6f;
            return Mathf.Lerp(fromY, overshoot, EaseOutCubic(t2));
        }
        else
        {
            float t2 = (t - 0.6f) / 0.4f;
            return Mathf.Lerp(overshoot, 0f, EaseOutCubic(t2));
        }
    }

    // ── MaterialPropertyBlock 設定 Alpha ─────────────
    private void SetTileAlpha(GameObject tile, float alpha)
    {
        Renderer rend = tile.GetComponent<Renderer>();
        if (rend == null) return;

        var block = new MaterialPropertyBlock();
        rend.GetPropertyBlock(block);
        block.SetFloat(AlphaProperty, alpha);
        rend.SetPropertyBlock(block);
    }

    // ── 重置所有地板 ──────────────────────────────────
    [ContextMenu("Reset Floor Rotation")]
    public void ResetFloorRotation()
    {
        StopAllCoroutines();
        if (tiles == null) return;

        for (int z = 0; z < floorDepth; z++)
        {
            for (int x = 0; x < floorWidth; x++)
            {
                if (tiles[x, z] == null) continue;
                tiles[x, z].transform.localRotation = Quaternion.identity;
                // 位置也歸零（以防波浪動畫中途被打斷）
                Vector3 p = tiles[x, z].transform.localPosition;
                tiles[x, z].transform.localPosition = new Vector3(p.x, 0f, p.z);
                SetTileAlpha(tiles[x, z], 1f);
            }
        }
    }

    // ── 清空地板 ──────────────────────────────────────
    [ContextMenu("Clear Floor")]
    public void ClearFloor()
    {
        StopAllCoroutines();
        tiles = null;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);
#else
            Destroy(transform.GetChild(i).gameObject);
#endif
        }
    }

    // ── Easing ────────────────────────────────────────
    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseInCubic(float t) => t * t * t;
}