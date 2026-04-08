    using UnityEngine;
    using UnityEngine.InputSystem;

    public class UITo3DSpawner : MonoBehaviour
    {
        [Header("UI 參考")]
        public RectTransform targetRectTransform; // 拖入你的 Canvas UI RectTransform
        public Canvas canvas;                     // 拖入 Canvas

        [Header("Cube 設定")]
        public GameObject cubePrefab;             // 拖入 Cube Prefab (或留空用 Primitive)
        public LayerMask raycastLayerMask = ~(1 << 6);   // 要碰撞的 Layer (預設全部)

    [Header("冷卻設定")]
        [Tooltip("每次生成之間的最短間隔時間（秒）")]
        public float cooldownDuration = 1.0f;

        // 記錄上一次成功執行的時間
        private float _lastSpawnTime = -Mathf.Infinity;

        [Header("偵錯")]
        public bool spawnOnClick = true;
    
        [Header("相機")]
        public Camera viewCam;
    private void Start()
    {
        raycastLayerMask = ~(1 << 6);
    }
    void Update()
        {
            if (spawnOnClick && Input.GetMouseButtonDown(0))
            {
                //Debug.Log(Input.mousePosition);
                SpawnCubeAtUIPosition(Input.mousePosition);
            }
        }

        public void SpawnCubeAtUIPosition(Vector2 TouchPos)
        {
            // ─── 冷卻檢查：距離上次呼叫是否已超過冷卻時間 ───────────────
            if (Time.time - _lastSpawnTime < cooldownDuration)
            {
                float remaining = cooldownDuration - (Time.time - _lastSpawnTime);
                Debug.Log($"冷卻中，還需等待 {remaining:F2} 秒");
                return; // 直接跳出，不執行後續
            }
            Ray ray = viewCam.ScreenPointToRay(TouchPos);

            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);

            // ─── Step 3: Raycast 找到 3D 物件 ──────────────────────────────
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, raycastLayerMask))
            {
                Debug.Log($"碰到物件: {hit.collider.name}，世界座標: {hit.point}");

                // ─── Step 4: 在 hit point 生成 Cube ────────────────────────
                SpawnCube(hit.point, hit.normal);

                // ✅ 只有成功生成才更新冷卻時間戳記
                _lastSpawnTime = Time.time;
            }
            else
            {
                Debug.LogWarning("Raycast 沒有碰到任何 3D 物件，請確認場景中有帶 Collider 的物件。");
            }
        }

        void SpawnCube(Vector3 position, Vector3 surfaceNormal)
        {
            GameObject cube;

            if (cubePrefab != null)
            {
                cube = Instantiate(cubePrefab, position, Quaternion.identity);
            }
            else
            {
                // 沒有 Prefab 時自動建立 Primitive Cube
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = position;
                cube.transform.localScale = Vector3.one * 0.3f;
            }

            // 讓 Cube 沿著碰撞面法線旋轉 (選用)
            cube.transform.up = surfaceNormal;

            Debug.Log($"Cube 生成於: {position}");
        }

        Camera GetCanvasCamera()
        {
            // Screen Space - Overlay → camera 為 null
            // Screen Space - Camera / World Space → 使用指定 camera
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return canvas.worldCamera;

            return null;
        }

        public float CooldownRemaining =>
           Mathf.Max(0f, cooldownDuration - (Time.time - _lastSpawnTime));

        public bool IsReady => CooldownRemaining <= 0f;
    }