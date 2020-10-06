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

		float rayLimit = 2 * Mathf.Max(terrainMesh.bounds.size.x, terrainMesh.bounds.size.y);

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
				// Explore upwards to find where the lake should overflow.

				// Get the point on the river where the lake exploration starts.
				Vector3 startPoint = positions[positions.Count - 1];

				// Get maximum height as the first position height for bounding the lake exploration.
				float maximumHeight = positions[0].y;
				float totalSamplingRange = maximumHeight - startPoint.y;

				// Calculate the number of sampling steps.
				int samplingSteps = (int) Mathf.Ceil(totalSamplingRange / lakeSamplingDistance);

				// Create arrays for ray hits from the current step.
				RaycastHit[] hits = null;

				// Create list for rays that qualify for breaking the lake exploration.
				List<int> qualifiedRays = new List<int>();
				
				// Create flag for keeping track of early terminating of lake exploration.
				bool terminateEarly = false;

				for (int i = 0; i < samplingSteps && !terminateEarly; ++i) {
					// Move upwards a small distance and set the previous hits to the current hits.
					float sampledHeight = lakeSamplingDistance * (i + 1);
					sampledHeight = (sampledHeight > totalSamplingRange) ? totalSamplingRange : sampledHeight;
					Vector3 origin = startPoint + Vector3.up * sampledHeight;
					hits = new RaycastHit[numLakeRays];

					// Shoot rays outwards in the horizontal plane, record the hit positions for each ray.
					for (int j = 0; j < numLakeRays; ++j) {
						float samplingAngle = j * lakeRayAngle;

						// Cast the ray and save the ray in the array, otherwise leave as default.
						RaycastHit lakeHit;
						if (Physics.Raycast(origin, Quaternion.Euler(0.0f, samplingAngle, 0.0f) * Vector3.forward, out lakeHit, rayLimit, terrainLayerMask)) {
							hits[j] = lakeHit;
						}
					}

					// Compare the position for each ray with the position of the previous ray.
					// If the next position lies a lot further along the direction or if there was no new position,
					// the overflow point has been surpassed.
					for (int j = 0; j < hits.Length; ++j) {
						Vector3 previousHit = hits[(j == 0) ? hits.Length - 1 : j - 1].point;
						Vector3 thisHit = hits[j].point;

						if (previousHit == Vector3.zero) {
							continue; // We have already or will check this hit, just do not compare with it now.
						} else if (thisHit == Vector3.zero || rayRatioThreshold * (previousHit - origin).magnitude < (thisHit - origin).magnitude) {
							qualifiedRays.Add(j);
							terminateEarly = true;
						}
					}

					// TODOs
					// For each of the rays that qualified for this, do binary search on the uncertain interval
					// until the search interval is smaller than a threshold.
					// The lowest height that was found is the overflow point.
					// Do a RaycastAll on this ray to find the new start as the second intersection.
					// TODO TODO TODO!!! Create water surfaces for lakes.

					// NOTE: THIS METHOD WILL NOT WORK, IT SEEMS. EXPLORE OTHER OPTIONS.
					// PROPOSAL:
					// Explore terrain samples along rays that are at the height of the source.
					// Place origin at the source height above the last river position.
					// In a number of directions in the horizontal plane away from the origin, start exploration.
					// At a certain distance interval, shoot rays downwards to hit the terrain.
					// When one ray hits the terrain lower than the previous, do binary search between that ray
					// and the ray before the last (or at the origin, if fewer than three rays have been shot).
					// Binary search finds the local maximum point.
					// Select the lowest point found over all directions as the overflow point.

					// Draw debug lines.
					if (drawDebugLines) {
						for (int j = 0; j < hits.Length; ++j) {
							RaycastHit debugHit = hits[j];
							if (!debugHit.point.Equals(Vector3.zero)) {
								Debug.DrawLine(origin, debugHit.point, 
									qualifiedRays.Contains(j) ? Color.red : Color.white,
									Mathf.Infinity);
							}
						}
					}
				}
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
}
