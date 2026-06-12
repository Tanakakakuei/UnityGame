#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AI;

public static class NavMeshAdder
{
    [MenuItem("Tools/NavMesh/Add Floor At Specified Position")]
    public static void AddFloorAndBake()
    {
        Vector3 pos = new Vector3(-0.5f, 0f, -15.5200005f);

        // 既に同じ名前の自動生成オブジェクトがあれば再利用
        GameObject existing = GameObject.Find("NavMeshFloor_Auto");
        if (existing != null)
        {
            existing.transform.position = pos;
            Selection.activeGameObject = existing;
            Debug.Log("NavMeshFloor_Auto が既に存在するため位置を更新しました。");
        }
        else
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "NavMeshFloor_Auto";
            // Unity の Plane はデフォルトで 10x10 なのでスケールを調整して 1x1 にする
            floor.transform.localScale = Vector3.one * 0.1f;
            floor.transform.position = pos;
            floor.transform.rotation = Quaternion.identity;

            // Undo を登録
            Undo.RegisterCreatedObjectUndo(floor, "Create NavMesh Floor");

            // NavMesh 用に NavigationStatic を設定
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);

            Selection.activeGameObject = floor;
            Debug.Log("NavMeshFloor_Auto を作成しました: " + pos);
        }

        // エディタ上で NavMesh をビルド
        NavMeshBuilder.BuildNavMesh();
        Debug.Log("NavMesh をビルドしました。");
    }
}
#endif
