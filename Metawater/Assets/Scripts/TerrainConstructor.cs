using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static HelperFunctions;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class TerrainConstructor : MonoBehaviour {
	// TODO: Write custom inspector for terrain constructor, with hidden debug toggles and an update button.

	[SerializeField]
	#pragma warning disable
	private Texture2D heightmap;
	[SerializeField]
	private bool visualizeGrid;
	[SerializeField]
	private bool visualizeGradient;
	[SerializeField]
	private bool visualizeGradientMagnitude;
	[SerializeField]
	private Gradient gradientMagnitudeTransferFunction;
	[SerializeField]
	private bool update;
#pragma warning restore

	private Mesh mesh;
	private UniformGrid2DFloat terrainGrid;
	private UniformGrid2DVector2 gradientGrid;
	
	void Awake() {
		// Initialize and set mesh.
		mesh = new Mesh();
		mesh.name = "TerrainMesh";
		GetComponent<MeshFilter>().sharedMesh = mesh;

		update = false;

		UpdateTerrain();
	}

	private void OnValidate() {
		if (update) {
			UpdateTerrain();

			update = false;
		}
	}

	private void OnDrawGizmos() {
		if (visualizeGrid && terrainGrid != null) {
			// Visualize the terrain grid.
			terrainGrid.Visualize();

			Gizmos.DrawSphere(HV2ToV3(terrainGrid.minPoint), 2);
			Gizmos.DrawSphere(HV2ToV3(terrainGrid.maxPoint), 2);
		}

		if (visualizeGradient && gradientGrid != null) {
			// Visualize the gradient field.
			Vector3[] vertices = GetComponent<MeshFilter>().sharedMesh.vertices;
			float vizScale = new Vector2(transform.localScale.x, transform.localScale.z).magnitude;

			for (int j = 0; j < gradientGrid.numPointsY; ++j) {
				for (int i = 0; i < gradientGrid.numPointsX; ++i) {
					Vector3 gradient = HV2ToV3(gradientGrid[i, j]);

					Color color = Color.white;
					if (visualizeGradientMagnitude) {
						float t = gradient.sqrMagnitude;
						color = gradientMagnitudeTransferFunction.Evaluate(t);
					}

					gradient = gradient.normalized;
					Vector3 startPosition = Vector3.Scale(vertices[j * terrainGrid.numPointsX + i], transform.localScale);
					startPosition.y = terrainGrid[i, j] * transform.localScale.y;
					Vector3 offset = gradient * vizScale;
					Vector3 endPosition = startPosition + offset;
					Vector3 headOffset = new Vector3(offset.z, offset.y, -offset.x) * 0.3f;
					Debug.DrawLine(startPosition, endPosition, color, Time.deltaTime);
					Debug.DrawLine(endPosition, endPosition + (headOffset - offset) * 0.3f, color, Time.deltaTime);
					Debug.DrawLine(endPosition, endPosition + (-headOffset - offset) * 0.3f, color, Time.deltaTime);
				}
			}
		}
	}

	private void UpdateTerrain() {
		ExtractHeightMapData();
		CalculateGradient();
		UpdateMesh();
	}

	private void ExtractHeightMapData() {
		// Initialize height and width.
		int height = heightmap.height;
		int width = heightmap.width;

		// Extract colors from texture.
		Color32[] pixelData = heightmap.GetPixels32();

		// Initialize a new uniform grid.
		Vector3 minPoint = transform.position - Vector3.forward * (height - 1) * transform.localScale.z - Vector3.right * (width - 1) * transform.localScale.x;
		Vector3 maxPoint = transform.position + Vector3.forward * (height - 1) * transform.localScale.z + Vector3.right * (width - 1) * transform.localScale.x;
		//Vector3 minPoint = transform.position - Vector3.forward * (height - 1) - Vector3.right * (width - 1);
		//Vector3 maxPoint = transform.position + Vector3.forward * (height - 1) + Vector3.right * (width - 1);
		terrainGrid = new UniformGrid2DFloat(V3ToHV2(minPoint), V3ToHV2(maxPoint), width, height); // X is width, Z is height.

		for (int j = 0; j < terrainGrid.numPointsY; ++j) { // Rows.
			for (int i = 0; i < terrainGrid.numPointsX; ++i) { // Columns.
				// Extract height data from the red channel. Red channel should be equal to
				// the others, and between 0 and 255, so divide to get value between 0 and 1.
				float color = pixelData[j * width + i].r / 255.0f;
				terrainGrid[i, j] = color;
			}
		}
	}

	private void CalculateGradient() {
		// For each grid point in the terrain grid, do convolution using Sobel operator.
		// Values for points that are out of range are treated as the center value.

		// Initialize grid as a uniform grid of 2D vectors with the same size and number of points as the terrain grid.
		gradientGrid = new UniformGrid2DVector2(terrainGrid.minPoint, terrainGrid.maxPoint, terrainGrid.numPointsX, terrainGrid.numPointsY);

		for (int j = 0; j < terrainGrid.numPointsY; ++j) {
			for (int i = 0; i < terrainGrid.numPointsX; ++i) {
				// Extract the scalars.
				float[,] scalars = new float[3, 3];

				for (int b = 0; b < 3; ++b) {
					// Indicates whether to subtract 1, add 0 or add 1 to the j index.
					int jTerm = b - 1;
					for (int a = 0; a < 3; ++a) {
						// Indicates whether to subtract 1, add 0 or add 1 to the i index.
						int iTerm = a - 1;

						// Try to access the scalar at the index. If at an edge of the grid, exception is thrown and scalar set to NaN.
						try {
							scalars[a, b] = terrainGrid[i + iTerm, j + jTerm];
						} catch (IndexOutOfRangeException) {
							scalars[a, b] = float.NaN;
						}
					}
				}

				// Correct the NaN scalars.
				CorrectScalars(scalars);

				// The x-kernel takes the form:
				//  1, 0, -1
				//  2, 0, -2
				//  1, 0, -1

				// The z-kernel takes the form:
				//  -1, -2, -1
				//   0,  0,  0
				//   1,  2,  1

				float partialX = scalars[0, 0] - scalars[2, 0] + 2 * scalars[0, 1] - 2 * scalars[2, 1] + scalars[0, 2] - scalars[2, 2];
				float partialZ = scalars[0, 0] + 2 * scalars[1, 0] + scalars[2, 0] - scalars[0, 2] - 2 * scalars[1, 2] - scalars[2, 2];

				gradientGrid[i, j] = new Vector2(partialX, partialZ);
			}
		}
	}

	private void UpdateMesh() {
		// Construct vertices.
		// Calculate inverse scaling.
		Vector2 inverseScale = new Vector2(1 / transform.localScale.x, 1 / transform.localScale.z);
		Vector3[] vertices = new Vector3[terrainGrid.numPointsX * terrainGrid.numPointsY];
		for (int j = 0; j < terrainGrid.numPointsY; ++j) {
			for (int i = 0; i < terrainGrid.numPointsX; ++i) {
				// Set the vertex position as the offset from the origin, with y-value offset
				// according to the terrain grid value and the desired scale.
				vertices[j * terrainGrid.numPointsX + i] = HV2ToV3(Vector2.Scale(terrainGrid.GridPointCoordinates(i, j), inverseScale)) + transform.up * terrainGrid[i, j];
			}
		}

		// Construct triangles.
		// Size of triangles array:
		// number of gridcells * 2 triangles per grid cell * 3 vertex indices per triangle
		int[] triangles = new int[6 * (terrainGrid.numPointsY - 1) * (terrainGrid.numPointsX - 1)];
		int triangleOffset = 0;
		for (int j = 0; j < (terrainGrid.numPointsY - 1); ++j) { // For each row of grid cells.
			for (int i = 0; i < (terrainGrid.numPointsX - 1); ++i) { // For each column of grid cells.
				int cellOffset = j * terrainGrid.numPointsX + i;
				// Lower left triangle of grid cell (clockwise assigned).
				triangles[triangleOffset++] = cellOffset;
				triangles[triangleOffset++] = cellOffset + terrainGrid.numPointsX;
				triangles[triangleOffset++] = cellOffset + 1;

				// Upper right triangle of grid cell (clockwise assigned).
				triangles[triangleOffset++] = cellOffset + 1;
				triangles[triangleOffset++] = cellOffset + terrainGrid.numPointsX;
				triangles[triangleOffset++] = cellOffset + terrainGrid.numPointsX + 1;
			}
		}

		// Update mesh and mesh collider.
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		MeshCollider meshCollider = GetComponent<MeshCollider>();
		meshFilter.sharedMesh.Clear();
		meshFilter.sharedMesh.vertices = vertices;
		meshFilter.sharedMesh.triangles = triangles;
		meshFilter.sharedMesh.RecalculateNormals();
		meshCollider.sharedMesh = meshFilter.sharedMesh;
	}

	private void CorrectScalars(float[,] scalars) {
		// Find all the NaN scalars.
		// Construct a bit mask that has the layout (2,2)(2,1)(2,0)(1,2)(1,0)(0,2)(0,1)(0,0) where
		// the ith bit is set if the scalar at the ith index tuple is NaN.
		int bitMask = 0;
		if (float.IsNaN(scalars[0, 0])) bitMask |= 1;
		if (float.IsNaN(scalars[0, 1])) bitMask |= 1 << 1;
		if (float.IsNaN(scalars[0, 2])) bitMask |= 1 << 2;
		if (float.IsNaN(scalars[1, 0])) bitMask |= 1 << 3;
		if (float.IsNaN(scalars[1, 2])) bitMask |= 1 << 4;
		if (float.IsNaN(scalars[2, 0])) bitMask |= 1 << 5;
		if (float.IsNaN(scalars[2, 1])) bitMask |= 1 << 6;
		if (float.IsNaN(scalars[2, 2])) bitMask |= 1 << 7;

		// Perform correction depending on the case indicated by the bit mask.
		switch (bitMask) {
			case 0x2f: // Bottom left corner.
				scalars[0, 0] = scalars[1, 1];
				scalars[0, 1] = scalars[1, 1];
				scalars[0, 2] = scalars[1, 2];
				scalars[1, 0] = scalars[1, 1];
				scalars[2, 0] = scalars[2, 1];
				break;
			case 0x29: // Left edge.
				scalars[0, 0] = scalars[0, 1];
				scalars[1, 0] = scalars[1, 1];
				scalars[2, 0] = scalars[2, 1];
				break;
			case 0xe9: // Top left corner.
				scalars[0, 0] = scalars[0, 1];
				scalars[1, 0] = scalars[1, 1];
				scalars[2, 0] = scalars[1, 1];
				scalars[2, 1] = scalars[1, 1];
				scalars[2, 2] = scalars[1, 2];
				break;
			case 0xe0: // Top edge.
				scalars[2, 0] = scalars[1, 0];
				scalars[2, 1] = scalars[1, 1];
				scalars[2, 2] = scalars[1, 2];
				break;
			case 0xf4: // Top right corner.
				scalars[0, 2] = scalars[0, 1];
				scalars[1, 2] = scalars[1, 1];
				scalars[2, 0] = scalars[1, 0];
				scalars[2, 1] = scalars[1, 1];
				scalars[2, 2] = scalars[1, 1];
				break;
			case 0x94: // Right edge.
				scalars[0, 2] = scalars[0, 1];
				scalars[1, 2] = scalars[1, 1];
				scalars[2, 2] = scalars[2, 1];
				break;
			case 0x97: // Bottom right corner.
				scalars[0, 0] = scalars[1, 0];
				scalars[0, 1] = scalars[1, 1];
				scalars[0, 2] = scalars[1, 1];
				scalars[1, 2] = scalars[1, 1];
				scalars[2, 2] = scalars[2, 1];
				break;
			case 0x07: // Bottom edge.
				scalars[0, 0] = scalars[1, 0];
				scalars[0, 1] = scalars[1, 1];
				scalars[0, 2] = scalars[1, 2];
				break;
		}
	}
}
