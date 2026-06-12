#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class BoxGridEditorTools
{
    [MenuItem("Tools/BoxGrid/Generate Grid (Editor)")]
    public static void GenerateGrid()
    {
        BoxGrid bg = Object.FindObjectOfType<BoxGrid>();
        if (bg == null)
        {
            Debug.LogWarning("BoxGrid がシーンに存在しません。BoxGrid を追加してからこのメニューを実行してください。");
            return;
        }

        bg.GenerateGridEditor();
        Debug.Log("BoxGrid をエディタで生成しました。");
    }
}
#endif
