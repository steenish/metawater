using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static HelperFunctions;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class TerrainConstructor : MonoBehaviour {
	// TODO: Figure out how to scale the grids and not affect the vertices, kind of reverse-scale the vertices maybe?

	[SerializeField]
	#pragma warning disable
	private Texture2D heightmap;
	[SerializeField]
	private bool visualizeGradient;
	[SerializeField]
	private bool update;
#pragma warning restore

	private Mesh mesh;
	private UniformGrid2D<float> terrainGrid;
	private UniformGrid2D<Vector2> gradientGrid;
	
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
		if (!visualizeGradient || gradientGrid == null) return;

		// Visualize the gradient field.
		Vector3[] vertices = GetComponent<MeshFilter>().sharedMesh.vertices;
		float vizScale = new Vector2(transform.localScale.x, transform.localScale.z).magnitude;

		for (int j = 0; j < gradientGrid.numPointsY; ++j) {
			for (int i = 0; i < gradientGrid.numPointsX; ++i) {
				Vector3 gradient = HV2ToV3(gradientGrid[i, j]).normalized;
				Vector3 position = Vector3.Scale(vertices[j * terrainGrid.numPointsX + i], transform.localScale);
				position.y = terrainGrid[i, j] * transform.localScale.y;
				Debug.DrawLine(position, position + gradient * vizScale, Color.white, Time.deltaTime);
				Gizmos.DrawSphere(position + gradient * vizScale, vizScale * 0.3f);
			}
		}

		//Vector3 minPoint = transform.position - Vector3.forward * (heightmap.height - 1) * transform.localScale.z - Vector3.right * (heightmap.width - 1) * transform.localScale.x;
		//Vector3 maxPoint = transform.position + Vector3.forward * (heightmap.height - 1) * transform.localScale.z + Vector3.right * (heightmap.width - 1) * transform.localScale.x;
		Vector3 minPoint = transform.position - Vector3.forward * (heightmap.height - 1) - Vector3.right * (heightmap.width - 1);
		Vector3 maxPoint = transform.position + Vector3.forward * (heightmap.height - 1) + Vector3.right * (heightmap.width - 1);

		Gizmos.DrawSphere(minPoint, 2);
		Gizmos.DrawSphere(maxPoint, 2);
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
		//Vector3 minPoint = transform.position - Vector3.forward * (height - 1) * transform.localScale.z - Vector3.right * (width - 1) * transform.localScale.x;
		//Vector3 maxPoint = transform.position + Vector3.forward * (height - 1) * transform.localScale.z + Vector3.right * (width - 1) * transform.localScale.x;
		Vector3 minPoint = transform.position - Vector3.forward * (height - 1) - Vector3.right * (width - 1);
		Vector3 maxPoint = transform.position + Vector3.forward * (height - 1) + Vector3.right * (width - 1);
		terrainGrid = new UniformGrid2D<float>(V3ToHV2(minPoint), V3ToHV2(maxPoint), width, height); // X is width, Z is height.

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
		gradientGrid = new UniformGrid2D<Vector2>(terrainGrid.minPoint, terrainGrid.maxPoint, terrainGrid.numPointsX, terrainGrid.numPointsY);

		for (int j = 0; j < terrainGrid.numPointsY; ++j) {
			for (int i = 0; i < terrainGrid.numPointsX; ++i) {
				// Extract the scalars.
				float[,] scalars = new float[3, 3];

				for (int a = 0; a < 3; ++a) {
					// Indicates whether to subtract 1, add 0 or add 1 to the i index.
					int iTerm = a - 1;
					for (int b = 0; b < 3; ++b) {
						// Indicates whether to subtract 1, add 0 or add 1 to the k index.
						int jTerm = b - 1;
						
						// Try to access the scalar at the index. If at an edge of the grid, exception is thrown and scalar set to NaN.
						try {
							scalars[a, b] = terrainGrid[i + iTerm, j + jTerm];
						} catch (IndexOutOfRangeException) {
							scalars[a, b] = float.NaN;
						}
					}
				}

				// The x-kernel takes the form:
				//  1, 0, -1]
				//  2, 0, -2
				// [1, 0, -1
				int[,] xKernel = new int[,] { { 1, 0, -1 }, { 2, 0, -2 }, { 1, 0, -1 } };

				// The z-kernel takes the form:
				//  -1, -2, -1]
				//   0,  0,  0
				// [ 1,  2,  1
				int[,] zKernel = new int[,] { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } };

				float partialX = 0.0f;
				float partialZ = 0.0f;

				for (int a = 0; a < 3; ++a) {
					for (int b = 0; b < 3; ++b) {
						float scalar = scalars[a, b];

						// If scalar was not defined because it was out of range, set it to the scalar at the center of the kernel.
						if (scalar == float.NaN) {
							scalar = scalars[1, 1];
						}
						
						partialX += xKernel[a, b] * scalar;
						partialZ += zKernel[a, b] * scalar;
					}
				}

				gradientGrid[i, j] = new Vector2(partialX, partialZ);
			}
		}
	}

	private void UpdateMesh() {
		// Construct vertices.
		Vector3[] vertices = new Vector3[terrainGrid.numPointsX * terrainGrid.numPointsY];
		for (int j = 0; j < terrainGrid.numPointsY; ++j) {
			for (int i = 0; i < terrainGrid.numPointsX; ++i) {
				// Set the vertex position as the offset from the origin, with y-value offset
				// according to the terrain grid value and the desired scale.
				vertices[j * terrainGrid.numPointsX + i] = HV2ToV3(terrainGrid.GridPointCoordinates(i, j)) + transform.up * terrainGrid[i, j];

				//Debug.Log("(i,j) = (" + i + "," + j + "), vertices[j * terrainGrid.numPointsX + i] = " + vertices[j * terrainGrid.numPointsX + i]);
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
}
