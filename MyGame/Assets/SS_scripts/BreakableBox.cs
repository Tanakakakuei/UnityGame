using UnityEngine;

public class BreakableBox : MonoBehaviour
{
    [SerializeField] private int clicksToBreak = 3;
    private int clickCount = 0;
    private Material originalMaterial;
    private Renderer boxRenderer;
    [Header("Outline")]
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private float outlineWidth = 0.02f;
    [Header("Waypoint Check")]
    [Tooltip("ボックスのエッジからウェイポイントを探索する最大距離")]
    [SerializeField] private float waypointAdjacencyDistance = 0.5f;
    [Tooltip("到達判定を無視して常に壊せる（デバッグ用）")]
    [SerializeField] private bool ignoreWaypointCheck = false;
    [Header("Debug")]
    [SerializeField] private bool waypointDebug = true;
    [SerializeField] private Color waypointReachableColor = Color.green;
    [SerializeField] private Color waypointBlockedColor = Color.red;
    [SerializeField] private float debugDuration = 5f;

    void Start()
    {
        // Colliderがない場合は追加
        if (GetComponent<Collider>() == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            Debug.Log($"{gameObject.name}: BoxColliderを自動追加しました");
        }

        // レンダラーを取得
        boxRenderer = GetComponent<Renderer>();
        if (boxRenderer != null)
        {
            originalMaterial = boxRenderer.material;
            CreateOutline();
        }
    }

    void CreateOutline()
    {
        if (boxRenderer == null) return;

        // 世界座標でのバウンディングボックスを取得し、ローカル空間へ変換して線を描く
        Bounds b = boxRenderer.bounds;
        Vector3[] corners = new Vector3[8];
        Vector3 min = b.min;
        Vector3 max = b.max;

        // 8頂点
        corners[0] = new Vector3(min.x, min.y, min.z);
        corners[1] = new Vector3(max.x, min.y, min.z);
        corners[2] = new Vector3(max.x, max.y, min.z);
        corners[3] = new Vector3(min.x, max.y, min.z);
        corners[4] = new Vector3(min.x, min.y, max.z);
        corners[5] = new Vector3(max.x, min.y, max.z);
        corners[6] = new Vector3(max.x, max.y, max.z);
        corners[7] = new Vector3(min.x, max.y, max.z);

        GameObject outlineParent = new GameObject("Outline");
        outlineParent.transform.SetParent(transform, false);

        // エッジのペアを定義
        (int, int)[] edges = new (int, int)[] {
            (0,1),(1,5),(5,4),(4,0), // bottom
            (3,2),(2,6),(6,7),(7,3), // top
            (0,3),(1,2),(5,6),(4,7)  // verticals
        };

        Shader lineShader = Shader.Find("Sprites/Default");
        Material lineMat = new Material(lineShader);
        lineMat.color = outlineColor;

        for (int i = 0; i < edges.Length; i++)
        {
            var e = edges[i];
            GameObject lineObj = new GameObject($"Edge_{i}");
            lineObj.transform.SetParent(outlineParent.transform, false);
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = false;

            // ローカル座標へ変換
            Vector3 aLocal = transform.InverseTransformPoint(corners[e.Item1]);
            Vector3 bLocal = transform.InverseTransformPoint(corners[e.Item2]);
            lr.SetPosition(0, aLocal);
            lr.SetPosition(1, bLocal);
            lr.startWidth = outlineWidth;
            lr.endWidth = outlineWidth;
            lr.material = lineMat;
            lr.numCapVertices = 2;
        }
    }

    void OnMouseDown()
    {
        OnHit();
    }

    // プレイヤー（タグ: "Player"）との物理衝突を検出してダメージを与える
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            OnHit();
        }
    }

    public void OnHit()
    {
        // デバッグ用オーバーライド: ウェイポイント判定を無視する
        if (ignoreWaypointCheck)
        {
            if (waypointDebug) Debug.Log($"{gameObject.name}: ignoreWaypointCheck=true のためウェイポイント判定をスキップします");
        }
        else
        {
            if (!IsAdjacentToReachableWaypoint())
            {
                Debug.LogWarning($"{gameObject.name}: 隣接する到達可能なウェイポイントがないため破壊不可");
                return;
            }
        }

        clickCount++;
        Debug.Log($"{gameObject.name}: クリック数 {clickCount}/{clicksToBreak}");

        // ダメージ表現
        VisualFeedback();

        // クリック回数に達したら破壊
        if (clickCount >= clicksToBreak)
        {
            BreakBox();
        }
    }

    // ウェイポイントベースで到達可能か判定
    bool IsAdjacentToReachableWaypoint()
    {
        // 優先ルール: このボックスが親にウェイポイントを持っているなら、そのウェイポイントの隣接ウェイポイントに
        // ボックスを保有していないものがあれば破壊可能とする
        if (transform.parent != null)
        {
            Waypoint owner = transform.parent.GetComponent<Waypoint>();
            if (owner != null)
            {
                if (waypointDebug) Debug.Log($"WaypointDebug: box owner waypoint = {owner.name}");
                // 判定は単純: owner の neighbors の中に box を保有していない waypoint があれば壊せる
                foreach (var neighbor in owner.neighbors)
                {
                    if (neighbor == null) continue;
                    bool occupied = IsWaypointOccupied(neighbor);
                    if (waypointDebug) Debug.Log($"WaypointDebug: neighbor={neighbor.name} occupied={occupied}");
                    if (!occupied) return true;
                }
                return false;
            }
        }

        // フォールバック: 周辺チェック (以前の方法)
        float halfX = Mathf.Abs(transform.localScale.x) * 0.5f;
        float halfZ = Mathf.Abs(transform.localScale.z) * 0.5f;
        float outOffset = 0.05f;
        Vector3[] checkPoints = new Vector3[] {
            transform.position + transform.right * (halfX + outOffset),
            transform.position - transform.right * (halfX + outOffset),
            transform.position + transform.forward * (halfZ + outOffset),
            transform.position - transform.forward * (halfZ + outOffset)
        };

        foreach (var cp in checkPoints)
        {
            if (waypointDebug) Debug.DrawRay(cp, Vector3.up * 0.2f, Color.cyan, debugDuration);
            Waypoint[] allWps = UnityEngine.Object.FindObjectsOfType<Waypoint>();
            foreach (var wp in allWps)
            {
                float d = Vector3.Distance(cp, wp.transform.position);
                if (d <= waypointAdjacencyDistance)
                {
                    if (waypointDebug) Debug.Log($"WaypointDebug: found wp={wp.name} at dist={d:F2}");
                    bool occupied = IsWaypointOccupied(wp);
                    if (waypointDebug) Debug.Log($"WaypointDebug: wp={wp.name} occupied={occupied}");
                    if (!occupied) return true;
                }
            }
        }

        return false;
    }

    bool IsWaypointOccupied(Waypoint wp)
    {
        if (wp == null) return false;
        // Check for BreakableBox or objects tagged Minerals at waypoint position
        float chkRadius = 0.4f;
        Collider[] cols = Physics.OverlapSphere(wp.transform.position + Vector3.up * 0.1f, chkRadius);
        foreach (var c in cols)
        {
            if (c == null) continue;
            if (c.GetComponent<BreakableBox>() != null) return true;
            if (c.gameObject.CompareTag("Minerals")) return true;
        }
        return false;
    }

    Waypoint FindNearestWaypoint(Vector3 pos)
    {
        // ソートが不要なら FindObjectsSortMode.None を使う
        Waypoint[] all = UnityEngine.Object.FindObjectsByType<Waypoint>(FindObjectsSortMode.None);
        Waypoint best = null;
        float bestDist = float.MaxValue;
        foreach (var w in all)
        {
            float d = Vector3.Distance(w.transform.position, pos);
            if (d < bestDist)
            {
                best = w; bestDist = d;
            }
        }
        return best;
    }

    bool AreWaypointsConnected(Waypoint a, Waypoint b)
    {
        if (a == null || b == null) return false;
        var visited = new System.Collections.Generic.HashSet<Waypoint>();
        var q = new System.Collections.Generic.Queue<Waypoint>();
        q.Enqueue(a); visited.Add(a);
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == b) return true;
            foreach (var n in cur.neighbors)
            {
                if (n == null) continue;
                if (visited.Add(n)) q.Enqueue(n);
            }
        }
        return false;
    }

    bool CanDirectlyReach(Vector3 from, Vector3 to)
    {
        // キャプスルキャストで直線経路が塞がれていないか調べる
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        dir.Normalize();
        float radius = 0.3f; // プレイヤーの幅想定
        if (Physics.SphereCast(from, radius, dir, out RaycastHit hitInfo, dist))
        {
            if (waypointDebug) Debug.DrawLine(from, hitInfo.point, waypointBlockedColor, debugDuration);
            return false;
        }
        if (waypointDebug) Debug.DrawLine(from, to, waypointReachableColor, debugDuration);
        return true;
    }

    void VisualFeedback()
    {
        if (boxRenderer == null) return;

        // クリック数に応じて色を変更
        float damagePercent = (float)clickCount / clicksToBreak;
        Color damageColor = Color.Lerp(Color.white, Color.red, damagePercent);
        boxRenderer.material.color = damageColor;

        // スケール縮小アニメーション
        StartCoroutine(DamageAnimation());
    }

    System.Collections.IEnumerator DamageAnimation()
    {
        Vector3 originalScale = transform.localScale;
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0.95f, elapsed / duration);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    void BreakBox()
    {
        Debug.Log($"{gameObject.name}: 破壊されました！");
        
        // 破壊エフェクト
        BreakEffect();
        
        // ゲームオブジェクトを削除
        Destroy(gameObject);
    }

    void BreakEffect()
    {
        // 破壊時のパーティクルエフェクト（オプション）
        Vector3 breakPos = transform.position;
        
        // 簡易的な破壊演出
        float spreadDistance = 0.5f;
        for (int i = 0; i < 4; i++)
        {
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.transform.position = breakPos + Random.insideUnitSphere * spreadDistance;
            debris.transform.localScale = Vector3.one * 0.3f;
            
            Rigidbody rb = debris.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = debris.AddComponent<Rigidbody>();
            }
            rb.linearVelocity = Random.insideUnitSphere * 5f;

            // 1秒後に破壊
            Destroy(debris, 1f);
        }
    }

    public void ResetBox()
    {
        clickCount = 0;
        if (boxRenderer != null && originalMaterial != null)
        {
            boxRenderer.material.color = Color.white;
        }
    }
}
