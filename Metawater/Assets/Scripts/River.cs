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
	private int numLakeDirections;
	[SerializeField]
	private float lakeExplorationLimit;
#pragma warning restore
	
    private List<Vector3> positions;
	private Bounds terrainBounds;
	private float lakeAngle;
	private float lakeStepSize;

	private void Start() {
        InvokeRepeating("CalculateRiver", 0.0f, updateInterval);

		terrainBounds = terrainConstructor.GetComponent<MeshFilter>().sharedMesh.bounds;
		lakeAngle = 360.0f / numLakeDirections;

		// The lake step size is set to the minimum meaningful distance in the terrain grid.
		lakeStepSize = Mathf.Min(terrainConstructor.terrainGrid.xLength, terrainConstructor.terrainGrid.yLength);
	}

	// TODO: Instead of using the current sampling point for lake overflow, find the actual point for the maximum.
	// TODO: Prevent river hill climbing during lake exploration.
	// TODO: Test this a lot.
	// TEST NOTES:
	// - the river always seems to head straight in positive z direction, sometimes turning to positive x direction.

	private void CalculateRiver() {
		if (!terrainConstructor.gradientReady) return;

        positions = new List<Vector3>();
		positions.Add(source.position);
        int numRiverIterations = 0;
		float deltaSqrVelocity = Mathf.Infinity;
		
		// While the number of iterations have not been exceeded and the last position was inside the horizontal
		// terrain bounds, continue exploring the river and potential lakes.
		while (numRiverIterations < maximumRiverIterations &&
			   InHorizontalBounds(positions[positions.Count - 1], terrainBounds)) {

			// The river has not found a local minimum, perform river exploration.
			if (deltaSqrVelocity > sqrVelocityThreshold) {
				// Integrate in the gradient field from the current position to find the next position.
				Vector3 nextPosition = IntegrateRK4(positions[positions.Count - 1], stepSize, terrainConstructor.gradientGrid);
				nextPosition.y = source.position.y; // MARKER 1
				positions.Add(nextPosition);

				deltaSqrVelocity = (positions[positions.Count - 1] - positions[positions.Count - 2]).sqrMagnitude;
			} else {
				// The river found a local minimum, perform lake exploration.

				// Get the point on the river where the lake exploration starts.
				Vector3 startPoint = positions[positions.Count - 1];

				// Draw line from bottom of lake to possible top.
				if (drawDebugLines) {
					Debug.DrawLine(startPoint, startPoint, Color.white, updateInterval);
				}

				// A list for saving the found candidates for the overflow point.
				List<Vector3> overflowPointCandidates = new List<Vector3>();

				// In a number of directions in the horizontal plane away from the origin, start exploration.
				for (int i = 0; i < numLakeDirections; ++i) {
					float samplingAngle = i * lakeAngle;

					// Calculate the direction in which to explore.
					Vector3 directionVector = Quaternion.Euler(0.0f, samplingAngle, 0.0f) * Vector3.forward;

					// Draw radial lines.
					if (drawDebugLines) {
						Debug.DrawLine(startPoint, startPoint + directionVector * lakeExplorationLimit, Color.white, updateInterval);
					}
					
					// Set up vectors for the two last samples of the terrain grid.
					float previousHeight = 0.0f; // The sample in the iteration before the current.
					float currentHeight = 0.0f;  // The sample in the current iteration.

					float distanceAlongDirection = 0.0f;
					// Explore terrain samples along the exploration directions.
					while (distanceAlongDirection < lakeExplorationLimit) {
						distanceAlongDirection += lakeStepSize;

						// At each step size along the direction, sample the height in the terrain grid.
						Vector3 samplingPoint = startPoint + directionVector * distanceAlongDirection;

						// Update the heights.
						previousHeight = currentHeight;
						currentHeight = terrainConstructor.terrainGrid.Interpolate(samplingPoint);
							
						// Draw line for a ray down.
						if (drawDebugLines) {
							Debug.DrawLine(samplingPoint, new Vector3(samplingPoint.x, currentHeight, samplingPoint.z), Color.white, updateInterval);
						}

						// Check if it is possible that an overflow point is in the interval.
						if (previousHeight > currentHeight) {
							// If possible, add it and break the exploration.
							overflowPointCandidates.Add(samplingPoint);
							break;
						}
					} // If the while loop terminates without finding anything, just move on.
				} // The for loop terminating is not a guarantee for having found a single overflow point.
				
				// Find the lowest point in the list.
				Vector3 overflowPoint = Vector3.up * Mathf.Infinity;

				// Select the lowest point found over all directions as the overflow point.
				foreach (Vector3 candidate in overflowPointCandidates) {
					if (candidate.y < overflowPoint.y) {
						overflowPoint = candidate;
					}
				}

				// If an overflow point was found, add it, otherwise terminate river exploration.
				if (overflowPoint.y < Mathf.Infinity) {
					overflowPoint.y = source.position.y;
					positions.Add(overflowPoint);
				} else {
					break;
				}
			}

			numRiverIterations++;
		}

		// Project river points down onto the terrain.
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
