using UnityEngine;

// Defines a 3D uniform grid.
public class UniformGrid  {

	public Bounds bounds { get; private set; }
	public int numPointsX { get; private set; }
	public int numPointsY { get; private set; }
	public int numPointsZ { get; private set; }
	public Vector3 minPoint { get; private set; }
	public Vector3 maxPoint { get; private set; }
	
	
	private float[] grid;

	public UniformGrid(Vector3 minPoint, Vector3 maxPoint, int numPointsX, int numPointsY, int numPointsZ) {
		if (maxPoint.x < minPoint.x || maxPoint.y < minPoint.y || maxPoint.z < minPoint.z) {
			throw new System.ArgumentException("minimum point greater than maximum point");
		}

		if (numPointsX < 0 || numPointsY < 0 || numPointsZ < 0) {
			throw new System.ArgumentException("number of points below 0");
		}

		this.minPoint = minPoint;
		this.maxPoint = maxPoint;
		this.numPointsX = numPointsX;
		this.numPointsY = numPointsY;
		this.numPointsZ = numPointsZ;

		grid = new float[numPointsX * numPointsY * numPointsZ];

		bounds = new Bounds(minPoint + (maxPoint - minPoint) / 2,
							new Vector3(maxPoint.x - minPoint.x, maxPoint.y - minPoint.y, maxPoint.z - minPoint.z));
	}

	public float this[int i, int j, int k] {
		get {
			return grid[i + numPointsX * j + numPointsX * numPointsY * k];
		}

		set {
			grid[i + numPointsX * j + numPointsX * numPointsY * k] = value;
		}
	}
}
