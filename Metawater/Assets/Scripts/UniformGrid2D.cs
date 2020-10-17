using System;
using UnityEngine;

using static HelperFunctions;

// Defines a 3D uniform grid for floats.
public class UniformGrid2DFloat {
	
	public int numPointsX { get; private set; }
	public int numPointsY { get; private set; }
	public Vector2 minPoint { get; private set; }
	public Vector2 maxPoint { get; private set; }
	
	private float[,] grid;
	private float xLength;
	private float yLength;
	private Vector3 minPointV3;
	private Vector3 maxPointV3;

	public UniformGrid2DFloat(Vector2 minPoint, Vector2 maxPoint, int numPointsX, int numPointsY) {
		if (maxPoint.x < minPoint.x || maxPoint.y < minPoint.y) {
			throw new System.ArgumentException("minimum point greater than maximum point");
		}

		if (numPointsX < 2 || numPointsY < 2) {
			throw new System.ArgumentException("number of grid points below 2");
		}

		this.minPoint = minPoint;
		this.maxPoint = maxPoint;
		this.numPointsX = numPointsX;
		this.numPointsY = numPointsY;

		xLength = (maxPoint.x - minPoint.x) / (numPointsX - 1);
		yLength = (maxPoint.y - minPoint.y) / (numPointsY - 1);
		minPointV3 = HV2ToV3(minPoint);
		maxPointV3 = HV2ToV3(maxPoint);

		grid = new float[numPointsX, numPointsY];
	}

	public float this[int i, int j] {
		get {
			return grid[i, j];
		}

		set {
			grid[i, j] = value;
		}
	}

	public Vector2 GridPointCoordinates(int i, int j) {
		return minPoint + i * xLength * Vector2.right + j * yLength * Vector2.up;
	}

	public float Interpolate(Vector2 position) {
		// Find indices of smallest point in interpolation cell.
		int i = (int) Mathf.Floor((position.x - minPoint.x) / xLength);
		int j = (int) Mathf.Floor((position.y - minPoint.y) / yLength);

		// Find coordinates of the smallest point.
		Vector2 leastPoint = GridPointCoordinates(i, j);

		// Find normalized coordinates in cell.
		Vector2 t = new Vector2((position.x - leastPoint.x) / xLength, (position.y - leastPoint.y) / yLength);

		// Perform interpolation.
		return InterpolateBilinear(grid[i, j], grid[i + j, j], grid[i, j + 1], grid[i + 1, j + 1], t);
	}

	public float Interpolate(Vector3 position) {
		return Interpolate(V3ToHV2(position));
	}

	public void Visualize() {
		Vector2 totalDisp = maxPoint - minPoint;

		for (int i = 0; i < numPointsX; ++i) {
			Vector3 firstPoint = minPointV3 + i * xLength * Vector3.right;
			Vector3 secondPoint = firstPoint + Vector3.forward * totalDisp.y;
			Debug.DrawLine(firstPoint, secondPoint, Color.green, Time.deltaTime);
		}

		for (int j = 0; j < numPointsY; ++j) {
			Vector3 firstPoint = minPointV3 + j * yLength * Vector3.forward;
			Vector3 secondPoint = firstPoint + Vector3.right * totalDisp.x;
			Debug.DrawLine(firstPoint, secondPoint, Color.green, Time.deltaTime);
		}
	}
}

// Defines a 3D uniform grid for floats.
public class UniformGrid2DVector2 {

	public int numPointsX { get; private set; }
	public int numPointsY { get; private set; }
	public Vector2 minPoint { get; private set; }
	public Vector2 maxPoint { get; private set; }

	private Vector2[,] grid;
	private float xLength;
	private float yLength;
	private Vector3 minPointV3;
	private Vector3 maxPointV3;

	public UniformGrid2DVector2(Vector2 minPoint, Vector2 maxPoint, int numPointsX, int numPointsY) {
		if (maxPoint.x < minPoint.x || maxPoint.y < minPoint.y) {
			throw new System.ArgumentException("minimum point greater than maximum point");
		}

		if (numPointsX < 2 || numPointsY < 2) {
			throw new System.ArgumentException("number of grid points below 2");
		}

		this.minPoint = minPoint;
		this.maxPoint = maxPoint;
		this.numPointsX = numPointsX;
		this.numPointsY = numPointsY;

		xLength = (maxPoint.x - minPoint.x) / (numPointsX - 1);
		yLength = (maxPoint.y - minPoint.y) / (numPointsY - 1);
		minPointV3 = HV2ToV3(minPoint);
		maxPointV3 = HV2ToV3(maxPoint);

		grid = new Vector2[numPointsX, numPointsY];
	}

	public Vector2 this[int i, int j] {
		get {
			return grid[i, j];
		}

		set {
			grid[i, j] = value;
		}
	}

	public Vector2 GridPointCoordinates(int i, int j) {
		return minPoint + i * xLength * Vector2.right + j * yLength * Vector2.up;
	}

	public Vector2 Interpolate(Vector2 position) {
		// Find indices of smallest point in interpolation cell.
		int i = (int) Mathf.Floor((position.x - minPoint.x) / xLength);
		int j = (int) Mathf.Floor((position.y - minPoint.y) / yLength);

		// Find coordinates of the smallest point.
		Vector2 leastPoint = GridPointCoordinates(i, j);

		// Find normalized coordinates in cell.
		Vector2 t = new Vector2((position.x - leastPoint.x) / xLength, (position.y - leastPoint.y) / yLength);

		// Perform interpolation.
		return InterpolateBilinear(grid[i, j], grid[i + j, j], grid[i, j + 1], grid[i + 1, j + 1], t);
	}

	public Vector2 Interpolate(Vector3 position) {
		return Interpolate(V3ToHV2(position));
	}

	public void Visualize() {
		Vector2 totalDisp = maxPoint - minPoint;

		for (int i = 0; i < numPointsX; ++i) {
			Vector3 firstPoint = minPointV3 + i * xLength * Vector3.right;
			Vector3 secondPoint = firstPoint + Vector3.forward * totalDisp.y;
			Debug.DrawLine(firstPoint, secondPoint, Color.green, Time.deltaTime);
		}

		for (int j = 0; j < numPointsY; ++j) {
			Vector3 firstPoint = minPointV3 + j * yLength * Vector3.forward;
			Vector3 secondPoint = firstPoint + Vector3.right * totalDisp.x;
			Debug.DrawLine(firstPoint, secondPoint, Color.green, Time.deltaTime);
		}
	}
}
