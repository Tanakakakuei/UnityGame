using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody rb;
    private Vector3 moveDirection = Vector3.zero;

    [Header("Raycast")]
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private LayerMask raycastLayer = -1;

    void Start()
    {
      

        // プレイヤー照明コントローラを追加（存在しない場合）
        if (GetComponent<PlayerLightController>() == null)
        {
            gameObject.AddComponent<PlayerLightController>();
        }
    }

    void Update()
    {
        HandleMovement();
        HandleRaycast();
    }

    void HandleMovement()
    {
        // 新しい Input System を使用して移動入力を取得する
        Vector2 moveInput = Vector2.zero;

        if (Gamepad.current != null)
        {
            moveInput += Gamepad.current.leftStick.ReadValue();
        }

        if (Keyboard.current != null)
        {
            float x = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
            float y = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
            moveInput += new Vector2(x, y);
        }
        // カメラ基準の進行方向に変換（WASD が画面に対して自然に動くようにする）
        Camera cam = Camera.main;
        Vector3 direction;
        if (cam != null)
        {
            Vector3 camForward = cam.transform.forward;
            Vector3 camRight = cam.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            direction = camRight * moveInput.x + camForward * moveInput.y;
        }
        else
        {
            direction = new Vector3(moveInput.x, 0f, moveInput.y);
        }

        if (direction.sqrMagnitude > 1f) direction.Normalize();

        if (direction.magnitude > 0f)
        {
            rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    void HandleRaycast()
    {
        // 新しい Input System のマウス入力を使用
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("Main Camera が見つかりません");
                return;
            }

            Vector2 mousePos = Mouse.current.position.ReadValue();
            // カメラからマウス位置を通るレイを作成（通常のスクリーン→ワールドのレイキャスト）
            Ray ray = cam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

            // デバッグ表示: レイの始点と方向をSceneビューで確認できるようにする
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 2f);
            Debug.Log($"Ray origin: {ray.origin}, direction: {ray.direction}, layerMask: {raycastLayer}");

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, raycastDistance, raycastLayer))
            {
                Debug.Log($"レイがヒット: {hit.collider.gameObject.name}");

                // BreakableBoxのコンポーネントを取得
                BreakableBox breakableBox = hit.collider.gameObject.GetComponent<BreakableBox>();
                if (breakableBox != null)
                {
                    breakableBox.OnHit();
                    Debug.Log("BreakableBoxにダメージを与えました");
                }
                else
                {
                    Debug.LogWarning("ヒットしたオブジェクトにBreakableBoxコンポーネントがありません");
                }
            }
            else
            {
                Debug.Log("レイが何もヒットしませんでした");
            }
        }
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }
}
