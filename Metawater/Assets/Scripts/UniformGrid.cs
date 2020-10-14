using UnityEngine;

// Defines a 3D uniform grid.
public class UniformGrid<T>  {

	public Bounds bounds { get; private set; }
	public int numPointsX { get; private set; }
	public int numPointsY { get; private set; }
	public int numPointsZ { get; private set; }
	public Vector3 minPoint { get; private set; }
	public Vector3 maxPoint { get; private set; }
	
	private T[] grid;

	public UniformGrid(Vector3 minPoint, Vector3 maxPoint, int numPointsX, int numPointsY, int numPointsZ) {
		if (maxPoint.x < minPoint.x || maxPoint.y < minPoint.y || maxPoint.z < minPoint.z) {
			throw new System.ArgumentException("minimum point greater than maximum point");
		}

		if (numPointsX < 1 || numPointsY < 1 || numPointsZ < 1) {
			throw new System.ArgumentException("number of grid points below 1");
		}

		this.minPoint = minPoint;
		this.maxPoint = maxPoint;
		this.numPointsX = numPointsX;
		this.numPointsY = numPointsY;
		this.numPointsZ = numPointsZ;

		grid = new T[numPointsX * numPointsY * numPointsZ];

		bounds = new Bounds(minPoint + (maxPoint - minPoint) / 2,
							new Vector3(maxPoint.x - minPoint.x, maxPoint.y - minPoint.y, maxPoint.z - minPoint.z));
	}

	public T this[int i, int j, int k] {
		get {
			return grid[i + numPointsX * j + numPointsX * numPointsY * k];
		}

		set {
			grid[i + numPointsX * j + numPointsX * numPointsY * k] = value;
		}
	}

	public Vector3 GridPointCoordinates(int i, int j, int k) {
		float xLength = (numPointsX > 1) ? (maxPoint.x - minPoint.x) / (numPointsX - 1) : 0.0f;
		float yLength = (numPointsY > 1) ? (maxPoint.y - minPoint.y) / (numPointsY - 1) : 0.0f;
		float zLength = (numPointsZ > 1) ? (maxPoint.z - minPoint.z) / (numPointsZ - 1) : 0.0f;

		return minPoint + i * xLength * Vector3.right + j * yLength * Vector3.up + k * zLength * Vector3.forward;
	}
}
