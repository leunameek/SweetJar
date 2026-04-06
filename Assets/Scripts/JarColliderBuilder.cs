using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct JarRing
{
    [Tooltip("Height from the jar's local origin.")]
    public float height;
    [Tooltip("Inner radius at this height.")]
    public float radius;
}

/// <summary>
/// Generates BoxColliders that follow the jar's actual silhouette.
/// Define the inner profile as a list of rings (height + radius), then
/// right-click the component header → "Build Colliders".
/// Gizmos show the preview live as you edit values.
/// </summary>
public class JarColliderBuilder : MonoBehaviour
{
    [Header("Wall Profile (bottom → top)")]
    [Tooltip("At least 2 rings required. Match these to your mesh in the Scene view using the gizmos.")]
    public JarRing[] profile = new JarRing[]
    {
        new JarRing { height = 0.0f, radius = 0.85f },  // narrow bottom
        new JarRing { height = 1.0f, radius = 1.40f },  // lower body widens
        new JarRing { height = 3.0f, radius = 1.65f },  // belly — widest
        new JarRing { height = 5.5f, radius = 1.40f },  // upper body narrows
        new JarRing { height = 7.0f, radius = 0.90f },  // neck
    };

    [Header("Wall Settings")]
    [Tooltip("Thickness of each wall panel. Keep thin to avoid eating into the interior.")]
    public float wallThickness = 0.2f;

    [Range(6, 20)]
    [Tooltip("Number of box panels per band around the ring. 10–12 is plenty.")]
    public int wallSegments = 10;

    [Header("Floor")]
    public float floorThickness = 0.15f;

    [Header("Physics Material (optional)")]
    public PhysicsMaterial physicsMaterial;

    [Header("Editor")]
    public bool showGizmos = true;

    // ---------------------------------------------------------------
    //  Context-menu actions
    // ---------------------------------------------------------------

    [ContextMenu("Build Colliders")]
    public void Build()
    {
        if (profile == null || profile.Length < 2)
        {
            Debug.LogError("[JarColliderBuilder] Profile needs at least 2 rings.");
            return;
        }
        ClearGenerated();
        BuildWalls();
        BuildFloor();
        int panelCount = (profile.Length - 1) * wallSegments;
        Debug.Log($"[JarColliderBuilder] Built {panelCount} wall panels + 1 floor on '{gameObject.name}'.");
    }

    [ContextMenu("Clear Generated Colliders")]
    public void ClearGenerated()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("JarWall_") || child.name == "JarFloor")
                DestroyImmediate(child.gameObject);
        }
    }

    // ---------------------------------------------------------------
    //  Internal builders
    // ---------------------------------------------------------------

    void BuildWalls()
    {
        float angleStep = 360f / wallSegments;

        for (int band = 0; band < profile.Length - 1; band++)
        {
            float y0 = profile[band].height;
            float y1 = profile[band + 1].height;
            float r0 = profile[band].radius;
            float r1 = profile[band + 1].radius;

            float dy  = y1 - y0;
            float dr  = r1 - r0;

            // Average radius for the panel ring center
            float avgRadius         = (r0 + r1) * 0.5f;
            float panelCenterRadius = avgRadius + wallThickness * 0.5f;

            // Width: chord between adjacent panel centers + tiny overlap to close gaps
            float panelWidth  = 2f * panelCenterRadius * Mathf.Sin(Mathf.PI / wallSegments) + 0.02f;

            // Height: actual length along the sloped surface
            float panelHeight = Mathf.Sqrt(dy * dy + dr * dr);

            // Tilt the panel around local X to match the jar surface slope.
            // atan2(dr, dy): positive = flares out, negative = narrows.
            float slopeDeg = Mathf.Atan2(dr, dy) * Mathf.Rad2Deg;

            for (int i = 0; i < wallSegments; i++)
            {
                float angleDeg = i * angleStep;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                // Panel center: midpoint between the two rings, offset outward
                Vector3 center = new Vector3(
                    Mathf.Sin(angleRad) * panelCenterRadius,
                    (y0 + y1) * 0.5f,
                    Mathf.Cos(angleRad) * panelCenterRadius
                );

                // 1. Rotate around Y to face radially outward.
                // 2. Tilt around local X to follow the slope.
                Quaternion rot = Quaternion.Euler(0f, angleDeg, 0f)
                               * Quaternion.Euler(slopeDeg, 0f, 0f);

                GameObject wall = new GameObject($"JarWall_{band:D2}_{i:D2}");
                wall.transform.SetParent(transform, false);
                wall.transform.localPosition = center;
                wall.transform.localRotation = rot;

                BoxCollider bc = wall.AddComponent<BoxCollider>();
                bc.size   = new Vector3(panelWidth, panelHeight, wallThickness);
                bc.center = Vector3.zero;

                if (physicsMaterial != null)
                    bc.sharedMaterial = physicsMaterial;
            }
        }
    }

    void BuildFloor()
    {
        float bottomRadius  = profile[0].radius;
        float floorDiameter = (bottomRadius + wallThickness) * 2f;
        float bottomY       = profile[0].height;

        GameObject floor = new GameObject("JarFloor");
        floor.transform.SetParent(transform, false);
        floor.transform.localPosition = new Vector3(0f, bottomY + floorThickness * 0.5f, 0f);
        floor.transform.localRotation = Quaternion.identity;

        BoxCollider bc = floor.AddComponent<BoxCollider>();
        bc.size   = new Vector3(floorDiameter, floorThickness, floorDiameter);
        bc.center = Vector3.zero;

        if (physicsMaterial != null)
            bc.sharedMaterial = physicsMaterial;
    }

    // ---------------------------------------------------------------
    //  Gizmos — live preview of profile + panels
    // ---------------------------------------------------------------

    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        if (!showGizmos) return;
        if (profile == null || profile.Length < 2) return;

        Matrix4x4 prevMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        // --- Profile rings ---
        for (int r = 0; r < profile.Length; r++)
        {
            // Top ring orange (marks the opening), rest yellow
            Gizmos.color = (r == profile.Length - 1)
                ? new Color(1f, 0.5f, 0f, 0.9f)
                : new Color(1f, 1f, 0f, 0.9f);
            DrawWireCircle(new Vector3(0f, profile[r].height, 0f), profile[r].radius, 48);
        }

        // --- Silhouette lines connecting rings ---
        Gizmos.color = new Color(1f, 1f, 0f, 0.45f);
        int guideLines = 8;
        for (int seg = 0; seg < profile.Length - 1; seg++)
        {
            for (int l = 0; l < guideLines; l++)
            {
                float a    = l * (360f / guideLines) * Mathf.Deg2Rad;
                float sinA = Mathf.Sin(a), cosA = Mathf.Cos(a);
                Vector3 bot = new Vector3(sinA * profile[seg].radius,     profile[seg].height,     cosA * profile[seg].radius);
                Vector3 top = new Vector3(sinA * profile[seg + 1].radius, profile[seg + 1].height, cosA * profile[seg + 1].radius);
                Gizmos.DrawLine(bot, top);
            }
        }

        // --- Wall panels (per band) ---
        Color panelFill = new Color(0f, 1f, 1f, 0.06f);
        Color panelWire = new Color(0f, 1f, 1f, 0.70f);
        float angleStep = 360f / wallSegments;

        for (int band = 0; band < profile.Length - 1; band++)
        {
            float y0 = profile[band].height;
            float y1 = profile[band + 1].height;
            float r0 = profile[band].radius;
            float r1 = profile[band + 1].radius;

            float avgRadius         = (r0 + r1) * 0.5f;
            float panelCenterRadius = avgRadius + wallThickness * 0.5f;
            float panelWidth        = 2f * panelCenterRadius * Mathf.Sin(Mathf.PI / wallSegments) + 0.02f;
            float panelHeight       = Mathf.Sqrt((y1 - y0) * (y1 - y0) + (r1 - r0) * (r1 - r0));
            float slopeDeg          = Mathf.Atan2(r1 - r0, y1 - y0) * Mathf.Rad2Deg;
            Vector3 panelSize       = new Vector3(panelWidth, panelHeight, wallThickness);

            for (int i = 0; i < wallSegments; i++)
            {
                float angleDeg = i * angleStep;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                Vector3    center = new Vector3(
                    Mathf.Sin(angleRad) * panelCenterRadius,
                    (y0 + y1) * 0.5f,
                    Mathf.Cos(angleRad) * panelCenterRadius
                );
                Quaternion rot = Quaternion.Euler(0f, angleDeg, 0f)
                               * Quaternion.Euler(slopeDeg, 0f, 0f);

                Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(center, rot, Vector3.one);

                Gizmos.color = panelFill;
                Gizmos.DrawCube(Vector3.zero, panelSize);
                Gizmos.color = panelWire;
                Gizmos.DrawWireCube(Vector3.zero, panelSize);
            }
        }

        // --- Floor ---
        Gizmos.matrix = transform.localToWorldMatrix;
        float floorDiameter = (profile[0].radius + wallThickness) * 2f;
        Vector3 floorCenter = new Vector3(0f, profile[0].height + floorThickness * 0.5f, 0f);
        Vector3 floorSize   = new Vector3(floorDiameter, floorThickness, floorDiameter);

        Gizmos.color = panelFill;
        Gizmos.DrawCube(floorCenter, floorSize);
        Gizmos.color = panelWire;
        Gizmos.DrawWireCube(floorCenter, floorSize);

        Gizmos.matrix = prevMatrix;
#endif
    }

#if UNITY_EDITOR
    static void DrawWireCircle(Vector3 center, float radius, int steps)
    {
        float stepAngle = 360f / steps;
        Vector3 prev = center + new Vector3(0f, 0f, radius);
        for (int i = 1; i <= steps; i++)
        {
            float rad  = i * stepAngle * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(JarColliderBuilder))]
public class JarColliderBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        JarColliderBuilder builder = (JarColliderBuilder)target;

        EditorGUILayout.Space(6);

        // Gizmos toggle button
        string gizmoLabel = builder.showGizmos ? "Gizmos ON" : "Gizmos OFF";
        Color  prevColor  = GUI.backgroundColor;
        GUI.backgroundColor = builder.showGizmos ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);

        if (GUILayout.Button(gizmoLabel, GUILayout.Height(28)))
        {
            Undo.RecordObject(builder, "Toggle Jar Gizmos");
            builder.showGizmos = !builder.showGizmos;
            EditorUtility.SetDirty(builder);
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = prevColor;

        EditorGUILayout.Space(4);

        // Build / Clear buttons
        if (GUILayout.Button("Build Colliders", GUILayout.Height(28)))
            builder.Build();

        if (GUILayout.Button("Clear Generated Colliders", GUILayout.Height(22)))
            builder.ClearGenerated();
    }
}
#endif
