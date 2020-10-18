using System.Collections.Generic;
using UnityEngine;

using static HelperFunctions;

public class River : MonoBehaviour {

#pragma warning disable
	[SerializeField]
	private LineRenderer line;
    [SerializeField]
    private Transform source;
    [SerializeField]
    private bool drawDebugLines;
	[SerializeField]
    private float updateInterval;
	[SerializeField]
	private int maximumRiverIterations;
	[SerializeField]
	private float stepSize;
	[SerializeField]
	private TerrainConstructor terrainConstructor;
#pragma warning restore

	private Mesh terrainMesh;
    private List<Vector3> positions;

	private void Start() {
        terrainMesh = GameObject.Find("Terrain").GetComponent<MeshFilter>().sharedMesh;
        InvokeRepeating("CalculateRiver", 0.0f, updateInterval);

		maximumRiverIterations = 0;
	}

	private void CalculateRiver() {
		if (!terrainConstructor.gradientReady) return;

        positions = new List<Vector3>();
		positions.Add(source.position);
        int numRiverIterations = 0;
		
		while (numRiverIterations < maximumRiverIterations) {
			Vector3 nextPosition = IntegrateRK4(positions[positions.Count - 1], stepSize, terrainConstructor.gradientGrid);
			nextPosition.y = source.position.y;
			positions.Add(nextPosition);

			numRiverIterations++;
		}

		line.positionCount = positions.Count;
		line.SetPositions(positions.ToArray());

		maximumRiverIterations++;

        // Draw debug lines.
        if (drawDebugLines) {
            for (int i = 0; i < positions.Count - 1; ++i) {
                Debug.DrawLine(positions[i], positions[i + 1], Color.blue, updateInterval);
            }
        }
    }
}
