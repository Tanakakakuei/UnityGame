#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AI;

public static class NavMeshAroundGrid
{
    [MenuItem("Tools/NavMesh/Add NavMesh Around Grid")]
    public static void AddNavMeshAroundGrid()
    {
        // Try to find the grid parent created by BoxGrid script
        GameObject gridParent = GameObject.Find("BoxGrid");
        if (gridParent == null)
        {
            // Fallback: find any object named BoxGrid (case-sensitive) or first component
            var bg = GameObject.FindObjectOfType<MonoBehaviour>();
            Debug.LogWarning("NavMeshAroundGrid: BoxGrid 親オブジェクトが見つかりませんでした。Scene に BoxGrid が存在するか確認してください。");
            return;
        }

        // Compute bounds that encapsulate all renderers under gridParent
        Renderer[] rends = gridParent.GetComponentsInChildren<Renderer>();
        if (rends == null || rends.Length == 0)
        {
            Debug.LogWarning("NavMeshAroundGrid: BoxGrid の子に Renderer が見つかりませんでした。生成が完了しているか確認してください。");
            return;
        }

        Bounds bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) bounds.Encapsulate(rends[i].bounds);

        // Determine margin: use one tile size if detectable
        float margin = 1f;
        // try to infer tile size from first renderer bounds
        if (rends.Length > 0)
        {
            Vector3 firstSize = rends[0].bounds.size;
            margin = Mathf.Max(firstSize.x, firstSize.z, 1f);
        }

        // Make nav floor slightly larger than grid
        float expand = margin;
        Vector3 desiredSize = new Vector3(bounds.size.x + expand, 1f, bounds.size.z + expand);
        Vector3 center = bounds.center;

        // Create or update plane
        GameObject floor = GameObject.Find("NavMeshFloor_Grid");
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "NavMeshFloor_Grid";
            // remove collider if undesired
            Collider col = floor.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            Undo.RegisterCreatedObjectUndo(floor, "Create NavMeshFloor_Grid");
        }

        floor.transform.position = new Vector3(center.x, center.y - 0.49f, center.z);
        // Unity Plane size is 10x10 units
        floor.transform.rotation = Quaternion.identity;
        floor.transform.localScale = new Vector3(desiredSize.x / 10f, 1f, desiredSize.z / 10f);

        // Mark Navigation static so it is considered in bake
        GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);

        Selection.activeGameObject = floor;

        // Bake NavMesh in editor
        NavMeshBuilder.BuildNavMesh();
        Debug.Log($"NavMeshFloor_Grid を作成/更新しました。center={center}, size={desiredSize}");
    }
}
#endif
