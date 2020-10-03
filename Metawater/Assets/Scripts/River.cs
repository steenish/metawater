using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.XR.WSA;

[ExecuteAlways]
public class River : MonoBehaviour {

#pragma warning disable
    [SerializeField]
    private Transform source;
    [SerializeField]
    private bool drawDebugLines;
    [SerializeField]
    private float samplingDistance;
    [SerializeField]
    private int maximumIterations;
    [SerializeField]
    private float updateInterval;
#pragma warning restore

    private int terrainLayerMask;
    private Mesh terrainMesh;
    private List<Vector3> positions;

    private void Start() {
        terrainLayerMask = 1 << 8;
        terrainMesh = GameObject.Find("Terrain").GetComponent<MeshFilter>().mesh;
        InvokeRepeating("CalculateRiver", 0.0f, updateInterval);
    }

    private void CalculateRiver() {
        bool finished = false;
        positions = new List<Vector3>();
        Vector3 tentativePosition = source.position;
        int numIterations = 0;

        while (!finished && numIterations < maximumIterations) {
            RaycastHit hit;
            // Perform raycast from tentative position downwards to find new position.
            if (Physics.Raycast(tentativePosition, Vector3.down, out hit, Mathf.Infinity, terrainLayerMask)) {
                // The current tentative position was above the mesh, add projected point to positions.
                positions.Add(hit.point);

                // Now explore along gradient for a new tentative position.
                // Extract triangle information from hit and terrain.
                int triangle = hit.triangleIndex;
                Vector3[] vertices = terrainMesh.vertices;
                int[] triangles = terrainMesh.triangles;
                Vector3 v0 = vertices[triangles[triangle * 3]];
                Vector3 v1 = vertices[triangles[triangle * 3 + 1]];
                Vector3 v2 = vertices[triangles[triangle * 3 + 2]];

                // Use triangle information to calculate the gradient from the barycentric interpolation function.
                float A = 0.5f * (v0.x * (v1.z - v2.z) + v1.x * (v2.z - v0.z) + v2.x * (v0.z - v1.z));
                Vector3 gradient = 0.5f * (1 / A) * new Vector3(v0.y * (v1.z - v2.z) + v1.y * (v2.z - v0.z) + v2.y * (v0.z - v1.z),
                                                              0.0f,
                                                              v0.y * (v2.x - v1.x) + v1.y * (v0.x - v2.x) + v2.y * (v1.x - v0.x));
                // Normalize and invert gradient to get direction of steepest descent.
                gradient = -gradient.normalized;

                // Find new tentative position using the last position
                tentativePosition = positions[positions.Count - 1] + gradient * samplingDistance;
            } else {
                finished = true;
            }
            numIterations++;
        }

        // Draw debug lines.
        if (drawDebugLines) {
            for (int i = 0; i < positions.Count - 1; ++i) {
                Debug.DrawLine(positions[i], positions[i + 1], Color.blue, updateInterval);
            }
        }
    }
}
