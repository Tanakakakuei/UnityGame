using UnityEngine;

public class BoxGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 30;
    [SerializeField] private int gridHeight = 30;
    
    [Header("Box Settings")]
    [SerializeField] private float boxSize = 1f;
    [SerializeField] private Material boxMaterial;
    
    [Header("Prefab (Optional)")]
    [SerializeField] private GameObject boxPrefab;

    void Start()
    {
        SpawnBoxGrid();
    }

    // Editor 用にインスペクタやメニューから呼べるように公開
    public void GenerateGridEditor()
    {
        // 既存の子をクリアして再生成
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            // 保護: BoxGrid 親である場合はスキップ
            if (c.name == "BoxGrid") continue;
            DestroyImmediate(c.gameObject);
        }
        SpawnBoxGrid();
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    void SpawnBoxGrid()
    {
        // 親オブジェクトを作成してグリッドを整理
        GameObject gridParent = new GameObject("BoxGrid");
        gridParent.transform.parent = transform;
        gridParent.transform.localPosition = Vector3.zero;

        // ボックスのサイズに合わせて間隔を調整（隙間なし）
        float usedSpacing = boxSize;

        // グリッドの中心を計算
        float totalWidth = (gridWidth - 1) * usedSpacing;
        float totalHeight = (gridHeight - 1) * usedSpacing;
        Vector3 startPos = new Vector3(-totalWidth / 2f, 0, -totalHeight / 2f);

        // 30×30 のボックスを生成
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 localPos = startPos + new Vector3(x * usedSpacing, 0, z * usedSpacing);
                CreateBox(localPos, gridParent.transform);
            }
        }

        Debug.Log($"BoxGrid生成完了: {gridWidth}×{gridHeight} = {gridWidth * gridHeight}個のボックスを配置しました");

        // 生成後にプレイヤーをグリッドの下中央に配置する
        // グリッドの下中央（ローカル座標）: x=0, z = -totalHeight/2
        Vector3 bottomCenterLocal = new Vector3(0f, 0f, -totalHeight / 2f);
        Vector3 bottomCenterWorld = gridParent.transform.TransformPoint(bottomCenterLocal);

        // Player タグのオブジェクトを探して位置を設定
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            // 現在の Y を保持して移動（階層によっては y を調整したい場合は値を変更）
            float playerY = playerObj.transform.position.y;
            playerObj.transform.position = new Vector3(bottomCenterWorld.x, playerY, bottomCenterWorld.z);
            Debug.Log($"Player をグリッド下中央に移動しました: {playerObj.transform.position}");
        }
        else
        {
            Debug.LogWarning("BoxGrid: Player タグのオブジェクトが見つかりませんでした。Player を手動で配置してください。");
        }
    }

    void CreateBox(Vector3 position, Transform parent)
    {
        GameObject box;

        if (boxPrefab != null)
        {
            // プリファブから生成（親下に生成してローカル位置を設定）
            box = Instantiate(boxPrefab, parent);
            box.transform.localPosition = position;
        }
        else
        {
            // キューブを作成
            box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = Vector3.one * boxSize;

            // コライダーは既に付与されるが設定を確認
            Collider collider = box.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = true;
            }

            // マテリアルを適用
            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer != null && boxMaterial != null)
            {
                renderer.material = boxMaterial;
            }
        }

        // BreakableBoxスクリプトをアタッチ
        BreakableBox breakableBox = box.AddComponent<BreakableBox>();

        // タグを設定（Inspectorで "Minerals" が存在すること）
        try {
            box.tag = "Minerals";
        } catch {
            // タグが存在しない場合は無視
        }

        box.name = $"Box_{position.x}_{position.z}";
    }
}
