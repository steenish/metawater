using UnityEngine;

// Defines a 3D uniform grid.
public class UniformGrid2D<T>  {
	
	public int numPointsX { get; private set; }
	public int numPointsY { get; private set; }
	public Vector2 minPoint { get; private set; }
	public Vector2 maxPoint { get; private set; }
	
	private T[] grid;

	public UniformGrid2D(Vector2 minPoint, Vector2 maxPoint, int numPointsX, int numPointsY) {
		if (maxPoint.x < minPoint.x || maxPoint.y < minPoint.y) {
			throw new System.ArgumentException("minimum point greater than maximum point");
		}

		if (numPointsX < 1 || numPointsY < 1) {
			throw new System.ArgumentException("number of grid points below 1");
		}

		this.minPoint = minPoint;
		this.maxPoint = maxPoint;
		this.numPointsX = numPointsX;
		this.numPointsY = numPointsY;

		grid = new T[numPointsX * numPointsY];
	}

	public T this[int i, int j] {
		get {
			return grid[i + numPointsX * j];
		}

		set {
			grid[i + numPointsX * j] = value;
		}
	}

	public Vector2 GridPointCoordinates(int i, int j) {
		float xLength = (numPointsX > 1) ? (maxPoint.x - minPoint.x) / (numPointsX - 1) : 0.0f;
		float yLength = (numPointsY > 1) ? (maxPoint.y - minPoint.y) / (numPointsY - 1) : 0.0f;

		return minPoint + i * xLength * Vector2.right + j * yLength * Vector2.up;
	}
}
