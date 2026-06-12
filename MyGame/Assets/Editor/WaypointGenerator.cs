#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;

public static class WaypointGenerator
{
    [MenuItem("Tools/Waypoint/Generate From BoxGrid")]
    public static void GenerateWaypoints()
    {
        // Find grid parent named "BoxGrid" (created by BoxGrid script)
        GameObject gridParent = GameObject.Find("BoxGrid");
        if (gridParent == null)
        {
            // Fallback: find a parent that has children named like "Box_..."
            var allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>();
            foreach (var t in allTransforms)
            {
                if (t.childCount == 0) continue;
                var firstChild = t.GetChild(0);
                if (firstChild != null && firstChild.name.StartsWith("Box_"))
                {
                    gridParent = t.gameObject;
                    break;
                }
            }
        }

        if (gridParent == null)
        {
            Debug.LogError("WaypointGenerator: BoxGrid parent not found. Make sure BoxGrid has been generated.");
            return;
        }

        // Create or reuse Waypoints parent
        GameObject wpParent = GameObject.Find("Waypoints_Auto");
        if (wpParent == null)
        {
            wpParent = new GameObject("Waypoints_Auto");
            Undo.RegisterCreatedObjectUndo(wpParent, "Create Waypoints_Auto");
        }

        // Clear existing children
        for (int i = wpParent.transform.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(wpParent.transform.GetChild(i).gameObject);
        }

        // Create waypoints for each box child (collect children first to avoid modifying while iterating)
        var waypoints = new System.Collections.Generic.List<Waypoint>();
        var boxTransforms = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < gridParent.transform.childCount; i++) boxTransforms.Add(gridParent.transform.GetChild(i));
        foreach (var b in boxTransforms)
        {
            GameObject go = new GameObject($"WP_{b.localPosition.x}_{b.localPosition.z}");
            Undo.RegisterCreatedObjectUndo(go, "Create Waypoint");
            go.transform.SetParent(wpParent.transform, false);
            go.transform.position = b.position;
            var wp = go.AddComponent<Waypoint>();
            waypoints.Add(wp);

            // Reparent the existing box under this waypoint
            Undo.SetTransformParent(b, go.transform, "Parent box under waypoint");
            b.localPosition = Vector3.zero;
        }

        // Add the specific waypoint at Vector3(-1.5,0,-15.5)
        Vector3 special = new Vector3(-1.5f, 0.850f, -15.5f);
        GameObject specialGO = new GameObject("WP_Special");
        Undo.RegisterCreatedObjectUndo(specialGO, "Create Special Waypoint");
        specialGO.transform.SetParent(wpParent.transform, false);
        specialGO.transform.position = special;
        var specialWp = specialGO.AddComponent<Waypoint>();
        waypoints.Add(specialWp);

        // Determine neighbor threshold: median nearest-neighbor distance
        float threshold = ComputeNeighborThreshold(waypoints.Select(w => w.transform.position).ToArray());
        if (threshold <= 0f) threshold = 1f;
        threshold *= 1.5f; // allow some tolerance

        // Link neighbors (only orthogonal: up/down/left/right, no diagonals)
        float axisTolerance = threshold * 0.5f;
        for (int i = 0; i < waypoints.Count; i++)
        {
            var a = waypoints[i];
            // collect potential orthogonal neighbors
            var candidates = new System.Collections.Generic.List<Waypoint>();
            Vector3 apos = a.transform.position;
            for (int j = 0; j < waypoints.Count; j++)
            {
                if (i == j) continue;
                var b = waypoints[j];
                Vector3 bpos = b.transform.position;
                float dx = Mathf.Abs(apos.x - bpos.x);
                float dz = Mathf.Abs(apos.z - bpos.z);

                bool connect = false;
                // aligned on X (same column) and Z distance approx threshold
                if (dx <= axisTolerance && Mathf.Abs(dz - threshold) <= axisTolerance) connect = true;
                // aligned on Z (same row) and X distance approx threshold
                if (dz <= axisTolerance && Mathf.Abs(dx - threshold) <= axisTolerance) connect = true;

                if (connect) candidates.Add(b);
            }

            // ensure uniqueness and pick up to 4 nearest candidates
            var unique = candidates.Distinct().OrderBy(c => Vector3.Distance(apos, c.transform.position)).Take(4).ToList();
            a.neighbors.Clear();
            foreach (var u in unique) a.neighbors.Add(u);

            EditorUtility.SetDirty(a);
        }

        Selection.activeGameObject = wpParent;
        Debug.Log($"WaypointGenerator: Created {waypoints.Count} waypoints and linked neighbors (threshold={threshold:F2}).");
    }

    static float ComputeNeighborThreshold(Vector3[] positions)
    {
        if (positions == null || positions.Length < 2) return 0f;
        var nn = new System.Collections.Generic.List<float>();
        for (int i = 0; i < positions.Length; i++)
        {
            float best = float.MaxValue;
            for (int j = 0; j < positions.Length; j++)
            {
                if (i == j) continue;
                float d = Vector3.Distance(positions[i], positions[j]);
                if (d < best) best = d;
            }
            if (best < float.MaxValue) nn.Add(best);
        }
        if (nn.Count == 0) return 0f;
        nn.Sort();
        return nn[nn.Count / 2]; // median
    }

    [MenuItem("Tools/Waypoint/Clear Waypoints Except Special")]
    public static void ClearWaypointsExceptSpecial()
    {
        // Prefer clearing children under Waypoints_Auto
        GameObject wpParent = GameObject.Find("Waypoints_Auto");
        if (wpParent != null)
        {
            for (int i = wpParent.transform.childCount - 1; i >= 0; i--)
            {
                var child = wpParent.transform.GetChild(i).gameObject;
                if (child.name == "WP_Special") continue;
                Undo.DestroyObjectImmediate(child);
            }
            Debug.Log("WaypointGenerator: Cleared waypoints under Waypoints_Auto except WP_Special.");
            return;
        }

        // Fallback: remove all Waypoint components' GameObjects except the special one
        var all = UnityEngine.Object.FindObjectsOfType<Waypoint>();
        int removed = 0;
        foreach (var w in all)
        {
            if (w == null) continue;
            if (w.gameObject.name == "WP_Special") continue;
            Undo.DestroyObjectImmediate(w.gameObject);
            removed++;
        }
        Debug.Log($"WaypointGenerator: Removed {removed} waypoint GameObjects except WP_Special.");
    }
}
#endif
