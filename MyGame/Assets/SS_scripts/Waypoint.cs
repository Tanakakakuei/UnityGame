using UnityEngine;
using System.Collections.Generic;

// 簡易ウェイポイントコンポーネント
public class Waypoint : MonoBehaviour
{
    public List<Waypoint> neighbors = new List<Waypoint>();

    // Gizmo 表示用
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.1f);
        Gizmos.color = Color.yellow;
        foreach (var n in neighbors)
        {
            if (n == null) continue;
            Gizmos.DrawLine(transform.position, n.transform.position);
        }
    }
}
