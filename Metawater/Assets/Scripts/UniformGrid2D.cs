using UnityEngine;

using static HelperFunctions;

// Defines a 3D uniform grid.
public class UniformGrid2D<T>  {
	
	public int numPointsX { get; private set; }
	public int numPointsY { get; private set; }
	public Vector2 minPoint { get; private set; }
	public Vector2 maxPoint { get; private set; }
	
	private T[,] grid;

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

		grid = new T[numPointsX, numPointsY];
	}

	public T this[int i, int j] {
		get {
			return grid[i, j];
		}

		set {
			grid[i, j] = value;
		}
	}

	public Vector2 GridPointCoordinates(int i, int j) {
		float xLength = (numPointsX > 1) ? (maxPoint.x - minPoint.x) / (numPointsX - 1) : 0.0f;
		float yLength = (numPointsY > 1) ? (maxPoint.y - minPoint.y) / (numPointsY - 1) : 0.0f;

		return minPoint + i * xLength * Vector2.right + j * yLength * Vector2.up;
	}

	public void Visualize() {
		float xLength = (numPointsX > 1) ? (maxPoint.x - minPoint.x) / (numPointsX - 1) : 0.0f;
		float yLength = (numPointsY > 1) ? (maxPoint.y - minPoint.y) / (numPointsY - 1) : 0.0f;
		Vector3 minPointV3 = HV2ToV3(minPoint);
		Vector3 maxPointV3 = HV2ToV3(maxPoint);
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
