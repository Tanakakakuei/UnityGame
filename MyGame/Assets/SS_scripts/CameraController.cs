using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 15, 10);
    [SerializeField] private float followSpeed = 5f;
    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 30f;
    [SerializeField] private float startDistance = 5f;
    [Tooltip("初期のカメラ距離に追加で離す量（Inspectorで微調整）")]
    [SerializeField] private float extraDistance = 2f;

    private float currentDistance;

    void Start()
    {
        if (playerTransform == null)
        {
            playerTransform = FindFirstObjectByType<Player>()?.transform;
            if (playerTransform == null)
            {
                Debug.LogError("CameraController: Playerが見つかりません");
                return;
            }
        }

        // 初期ズーム距離を適用して初期位置を設定する
        currentDistance = Mathf.Clamp(startDistance + extraDistance, minDistance, maxDistance);
        if (cameraOffset != Vector3.zero)
        {
            cameraOffset = cameraOffset.normalized * currentDistance;
        }
        UpdateCameraPosition();
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        HandleZoom();
        UpdateCameraPosition();
    }

    void HandleZoom()
    {
        // Input System のマウススクロールを使う（古い Input にも対応）
        float scroll = 0f;
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y;
        }
        #else
        scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
        #endif

        if (Mathf.Abs(scroll) > 0.0001f)
        {
            currentDistance -= scroll * zoomSpeed * Time.deltaTime * 100f;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            // cameraOffset の方向を保ったまま距離だけ調整
            cameraOffset = cameraOffset.normalized * currentDistance;
        }
    }

    void UpdateCameraPosition()
    {
        Vector3 targetPosition = playerTransform.position + cameraOffset;
        // 基本の追従位置へ補間
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // プレイヤーが画面中央に来るようにカメラ位置を微調整
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 playerPos = playerTransform.position;
            // カメラからプレイヤーへの距離をカメラ前方向の成分で取得
            float depth = Vector3.Dot(playerPos - transform.position, cam.transform.forward);
            // ビューポート中央が depth の世界座標
            Vector3 centerWorld = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
            Vector3 correction = playerPos - centerWorld;
            // 補正を適用してプレイヤーを中央に揃える
            transform.position += correction;
        }
    }
}
