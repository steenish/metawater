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
	private UniformGrid terrainGrid;

	void Awake() {
		// Initialize and set mesh.
		mesh = new Mesh();
		mesh.name = "TerrainMesh";
		GetComponent<MeshFilter>().sharedMesh = mesh;

		ExtractHeightMapData();
		UpdateMesh();
	}

	private void OnValidate() {
		ExtractHeightMapData();
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
		terrainGrid = new UniformGrid(minPoint, maxPoint, width, 1, height); // X is width, Z is height.

		for (int k = 0; k < terrainGrid.numPointsZ; ++k) { // Rows.
			for (int i = 0; i < terrainGrid.numPointsX; ++i) { // Columns.
				// Extract height data from the red channel. Red channel should be equal to
				// the others, and between 0 and 255, so divide to get value between 0 and 1.
				float color = pixelData[k * width + i].r / 255.0f;
				terrainGrid[i, 0, k] = color;
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
