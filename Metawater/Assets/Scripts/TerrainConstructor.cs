using System;
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class TerrainConstructor : MonoBehaviour {

	[SerializeField]
	#pragma warning disable
	private Texture2D heightmap;
	#pragma warning restore
	[SerializeField]
	private float areaScale = 0.1f;
	[SerializeField]
	private float heightScale = 1.0f;
	
	private Mesh mesh;
	private UniformGrid<float> terrainGrid;
	private UniformGrid<Vector2> gradientGrid;

	void Awake() {
		// Initialize and set mesh.
		mesh = new Mesh();
		mesh.name = "TerrainMesh";
		GetComponent<MeshFilter>().sharedMesh = mesh;

		UpdateTerrain();
	}

	private void OnValidate() {
		UpdateTerrain();
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
		Vector3 minPoint = transform.position - Vector3.forward * (height - 1) * 0.5f * areaScale - Vector3.right * (width - 1) * 0.5f * areaScale;
		Vector3 maxPoint = transform.position + Vector3.forward * (height - 1) * 0.5f * areaScale + Vector3.right * (width - 1) * 0.5f * areaScale;
		terrainGrid = new UniformGrid<float>(minPoint, maxPoint, width, 1, height); // X is width, Z is height.

		for (int k = 0; k < terrainGrid.numPointsZ; ++k) { // Rows.
			for (int i = 0; i < terrainGrid.numPointsX; ++i) { // Columns.
				// Extract height data from the red channel. Red channel should be equal to
				// the others, and between 0 and 255, so divide to get value between 0 and 1.
				float color = pixelData[k * width + i].r / 255.0f;
				terrainGrid[i, 0, k] = color;
			}
		}
	}

	private void CalculateGradient() {
		// For each grid point in the terrain grid, do convolution using Sobel operator.
		// Values for points that are out of range are treated as the center value.

		// Initialize grid as a uniform grid of 2D vectors with the same size and number of points as the terrain grid.
		gradientGrid = new UniformGrid<Vector2>(terrainGrid.minPoint, terrainGrid.maxPoint, terrainGrid.numPointsX, terrainGrid.numPointsY, terrainGrid.numPointsZ);

		for (int k = 0; k < terrainGrid.numPointsZ; ++k) {
			for (int i = 0; i < terrainGrid.numPointsX; ++i) {
				// Extract the scalars.
				float[,] scalars = new float[3, 3];

				for (int a = 0; a < 3; ++a) {
					// Indicates whether to subtract 1, add 0 or add 1 to the i index.
					int iTerm = a - 1;
					for (int b = 0; b < 3; ++b) {
						// Indicates whether to subtract 1, add 0 or add 1 to the k index.
						int kTerm = b - 1;
						
						// Try to access the scalar at the index. If at an edge of the grid, exception is thrown and scalar set to NaN.
						try {
							scalars[a, b] = terrainGrid[i + iTerm, 0, k + kTerm];
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

				gradientGrid[i, 0, k] = new Vector2(partialX, partialZ);
			}
		}

		// Visualize the gradient field.
		for (int k = 0; k < gradientGrid.numPointsZ; ++k) {
			for (int i = 0; i < gradientGrid.numPointsX; ++i) {
				Vector2 gradient = gradientGrid[i, 0, k];
				Vector3 gradient3D = (new Vector3(gradient.x, 0.0f, gradient.y)).normalized * 0.2f;
				Vector3 position = gradientGrid.GridPointCoordinates(i, 0, k); // TODO: Fix bug where lines appear at different heights for some reason.
				Debug.Log(position);
				position.y = terrainGrid[i, 0, k];
				Debug.DrawLine(position, position + gradient3D, Color.white, Mathf.Infinity);
				Debug.DrawLine(position + gradient3D, position + gradient3D + Vector3.right * 0.05f, Color.white, Mathf.Infinity);
			}
		}
	}

	private void UpdateMesh() {
		// Initialize origin.
		Vector3 origin = terrainGrid.minPoint;

		// Construct vertices.
		Vector3[] vertices = new Vector3[terrainGrid.numPointsZ * terrainGrid.numPointsX];
		
		for (int k = 0; k < terrainGrid.numPointsZ; ++k) { // Rows.
			for (int i = 0; i < terrainGrid.numPointsX; ++i) { // Columns.
				// Set the vertex position as the offset from the origin, with y-value offset
				// according to the terrain grid value and the desired scale.
				vertices[k * terrainGrid.numPointsX + i] = new Vector3(origin.x + (i * areaScale), origin.y + (terrainGrid[i, 0, k] * heightScale), origin.z + (k * areaScale)); ;
			}
		}

		// Construct triangles.
		// Size of triangles array:
		// number of gridcells * 2 triangles per grid cell * 3 vertex indices per triangle
		int[] triangles = new int[6 * (terrainGrid.numPointsZ - 1) * (terrainGrid.numPointsX - 1)];
		int triangleOffset = 0;
		for (int k = 0; k < (terrainGrid.numPointsZ - 1); ++k) { // For each row of grid cells.
			for (int i = 0; i < (terrainGrid.numPointsX - 1); ++i) { // For each column of grid cells.
				int cellOffset = k * terrainGrid.numPointsX + i;
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

		// Update mesh.
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		meshFilter.sharedMesh.Clear();
		meshFilter.sharedMesh.vertices = vertices;
		meshFilter.sharedMesh.triangles = triangles;
		meshFilter.sharedMesh.RecalculateNormals();
	}
}
