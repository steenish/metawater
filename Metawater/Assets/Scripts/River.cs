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
	private float sqrVelocityThreshold;
	[SerializeField]
	private TerrainConstructor terrainConstructor;
	[SerializeField]
	private Bounds terrainBounds;
#pragma warning restore
	
    private List<Vector3> positions;

	private void Start() {
        InvokeRepeating("CalculateRiver", 0.0f, updateInterval);

		terrainBounds = terrainConstructor.GetComponent<MeshFilter>().sharedMesh.bounds;
	}

	private void CalculateRiver() {
		if (!terrainConstructor.gradientReady) return;

        positions = new List<Vector3>();
		positions.Add(source.position);
        int numRiverIterations = 0;
		float deltaSqrVelocity = Mathf.Infinity;
		
		while (numRiverIterations < maximumRiverIterations &&
			   deltaSqrVelocity > sqrVelocityThreshold &&
			   InHorizontalBounds(positions[positions.Count - 1], terrainBounds)) {
			Vector3 nextPosition = IntegrateRK4(positions[positions.Count - 1], stepSize, terrainConstructor.gradientGrid);
			nextPosition.y = source.position.y;
			positions.Add(nextPosition);

			deltaSqrVelocity = (positions[positions.Count - 1] - positions[positions.Count - 2]).sqrMagnitude;

			numRiverIterations++;
		}

		for (int i = 0; i < positions.Count; ++i) {
			Vector3 position = positions[i];
			float newHeight = terrainConstructor.terrainGrid.Interpolate(position);
			position.y = newHeight * terrainConstructor.transform.localScale.y;
			positions[i] = position;
		}

		line.positionCount = positions.Count;
		line.SetPositions(positions.ToArray());

        // Draw debug lines.
        if (drawDebugLines) {
            for (int i = 0; i < positions.Count - 1; ++i) {
                Debug.DrawLine(positions[i], positions[i + 1], Color.blue, updateInterval);
            }
        }

		Debug.Log(numRiverIterations);
    }
}
