using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.XR.WSA;

public class River : MonoBehaviour {

#pragma warning disable
    [SerializeField]
    private Transform source;
    [SerializeField]
    private bool drawDebugLines;
    [SerializeField]
    private float riverSamplingDistance;
	[SerializeField]
	private float lakeSamplingDistance;
	[SerializeField]
	private float lakeExplorationThreshold;
	[SerializeField]
	private int numLakeRays;
    [SerializeField]
    private float maximumRiverIterations;
	[SerializeField]
	private float rayRatioThreshold; // = nextRayMagnitude / previousRayMagnitude. Maximum such ratio allowed without terminating lake exploration.
    [SerializeField]
    private float updateInterval;
#pragma warning restore

    private int terrainLayerMask;
	private float lakeRayAngle;
    private Mesh terrainMesh;
    private List<Vector3> positions;

    private void Start() {
        terrainLayerMask = 1 << 8;
        terrainMesh = GameObject.Find("Terrain").GetComponent<MeshFilter>().sharedMesh;
        InvokeRepeating("CalculateRiver", 0.0f, updateInterval);

		lakeRayAngle = 360.0f / numLakeRays;

		maximumRiverIterations = 0;
	}

    private void CalculateRiver() {
        bool finished = false;
        positions = new List<Vector3>();
        Vector3 tentativePosition = source.position;
        int numRiverIterations = 0;

		float rayLimit = 2 * Mathf.Max(terrainMesh.bounds.size.z, Mathf.Max(terrainMesh.bounds.size.x, terrainMesh.bounds.size.y));

		Physics.queriesHitBackfaces = true;

		while (!finished && numRiverIterations < maximumRiverIterations) {
            RaycastHit hit;
            // Perform raycast from tentative position downwards to find new position, or upwards to check local minimum.
            if (Physics.Raycast(tentativePosition, Vector3.down, out hit, rayLimit, terrainLayerMask)) {
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
                tentativePosition = positions[positions.Count - 1] + gradient * riverSamplingDistance;
            } else if (Physics.Raycast(tentativePosition, Vector3.up, out hit, rayLimit, terrainLayerMask)) {
				// The current tentative position is below the terrain, so a local minimum was found.
				// This means there should be a lake here, up to the point where the lake would overflow.
				// Explore to find where the lake should overflow.

				// Get the point on the river where the lake exploration starts.
				Vector3 startPoint = positions[positions.Count - 1];

				// Get maximum height as the source height for bounding the lake exploration, and use for origin.
				float maximumHeight = positions[0].y;

				// Place origin at the source height above the last river position.
				Vector3 lakeOrigin = startPoint + Vector3.up;

				// A list for saving the found candidates for the overflow point.
				List<Vector3> overflowPointCandidates = new List<Vector3>();
				Vector3 overflowPoint = Vector3.up * Mathf.Infinity;

				// In a number of directions in the horizontal plane away from the origin, start exploration.
				for (int i = 0; i < numLakeRays; ++i) {
					float samplingAngle = i * lakeRayAngle;

					Vector3 directionVector = Quaternion.Euler(0.0f, samplingAngle, 0.0f) * Vector3.forward;

					// Cast a ray to find out maximum distance to explore to.
					RaycastHit distanceRayHit;
					Physics.Raycast(lakeOrigin, directionVector, out distanceRayHit, rayLimit, terrainLayerMask);

					Vector3 overflowPointCandidate = RecursiveLakeExploration(0.0f, distanceRayHit.distance, lakeSamplingDistance, lakeOrigin, directionVector, rayLimit);
				}

				// Select the lowest point found over all directions as the overflow point.
				foreach (Vector3 candidate in overflowPointCandidates) {
					if (candidate.y < overflowPoint.y) {
						overflowPoint = candidate;
					}
				}

				positions.Add(overflowPoint);

				// TODO TODO TODO!!! Create water surfaces for lakes.
			} else {
                finished = true;
            }
            numRiverIterations++;
        }

		Physics.queriesHitBackfaces = false;

		maximumRiverIterations++;

        // Draw debug lines.
        if (drawDebugLines) {
            for (int i = 0; i < positions.Count - 1; ++i) {
                Debug.DrawLine(positions[i], positions[i + 1], Color.blue, updateInterval);
            }
        }
    }

	private Vector3 RecursiveLakeExploration(float minimumDistance, float maximumDistance, float samplingDistance, Vector3 lakeOrigin, Vector3 directionVector, float rayLimit) {
		float distanceAlongDirection = minimumDistance;

		// Base case: the interval is below the threshold.
		if (maximumDistance - minimumDistance <= lakeExplorationThreshold) {
			// Return the terrain hit point at the minimum distance.
			Vector3 rayOrigin = lakeOrigin + directionVector * distanceAlongDirection;
			RaycastHit terrainHit;
			Physics.Raycast(rayOrigin, Vector3.down, out terrainHit, rayLimit, terrainLayerMask);
			return terrainHit.point;
		}

		// Set up vectors for the three last hits with the terrain.
		Vector3 previousHit = Vector3.zero; // The hit in the iteration before the last.
		Vector3 lastHit = Vector3.zero;     // The hit in the last iteration.
		Vector3 currentHit = Vector3.zero;  // The hit in the current iteration.

		// Explore terrain samples along rays that are at the height of the source.
		while (distanceAlongDirection > maximumDistance) {
			distanceAlongDirection += samplingDistance;

			// At a certain distance interval, shoot rays downwards to hit the terrain.
			Vector3 rayOrigin = lakeOrigin + directionVector * distanceAlongDirection;
			RaycastHit terrainHit;
			if (Physics.Raycast(rayOrigin, Vector3.down, out terrainHit, rayLimit, terrainLayerMask)) {
				// Update the terrain hits.
				previousHit = lastHit;
				lastHit = currentHit;
				currentHit = terrainHit.point;

				// Check if it is possible that an overflow point candidate is in the interval.
				if (OverflowPointPossible(lastHit, currentHit)) {
					// Recurse on the new interval.
					float newMinDistance = Vector3.Project(previousHit - lakeOrigin, directionVector).magnitude;
					float newMaxDistance = Vector3.Project(currentHit - lakeOrigin, directionVector).magnitude;
					float newSamplingDistance = samplingDistance * ((newMaxDistance - newMinDistance) / (maximumDistance - minimumDistance));
					return RecursiveLakeExploration(newMinDistance, newMaxDistance, samplingDistance, lakeOrigin, directionVector, rayLimit);
				}
			} else {
				// If there was no hit, terminate the search along the direction.
				break;
			}
		}
		// Nothing found, retur the vector infinitely far up.
		return Vector3.up * Mathf.Infinity;
	}

	private bool OverflowPointPossible(Vector3 secondHit, Vector3 thirdHit) {
		// It is only possible that there is an overflow point if the second hit is higher than the third hit.
		return secondHit.y > thirdHit.y;
	}
}
