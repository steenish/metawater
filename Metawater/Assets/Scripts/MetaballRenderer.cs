using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

struct GridPoint {
  public GridPoint(Vector3 position, float value) {
    this.position = position;
    this.value = value;
  }

  public Vector3 position;
  public float value;
}

struct GridCell {
  public GridCell(Vector3[] points, float[] values) {
    this.points = points;
    this.values = values;
  }

  public Vector3[] points;
  public float[] values;
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(PhysicsController))]
public class MetaballRenderer : MonoBehaviour {

  [SerializeField]
  [Range(0.1f, 3.0f)]
  private float resolution = 10;
  [SerializeField]
  [Range(0.0f, 10.0f)]
  private float threshold = 2;
  [SerializeField]
  private Vector3 boundsOffset = Vector3.zero;
  [SerializeField]
  private float updateGridTimeThreshold = 1.0f;
  [SerializeField]
  private float renderMetaballsTimeThreshold = 0.5f;

  private Bounds boundingBox;
  private float updateGridTimer = Mathf.Infinity;
  private float renderMetaballsTimer = 0.0f;
  private GridPoint[,,] grid;
  private MeshFilter meshFilter;
  private PhysicsController physicsController;

  void Awake() {
    meshFilter = GetComponent<MeshFilter>();
    physicsController = GetComponent<PhysicsController>();
    // Set placeholder bounding box.
    boundingBox = new Bounds(transform.position, transform.localScale);
  }

  void Update() {
    // Check if grid should be updated.
    if (updateGridTimer > updateGridTimeThreshold) {
      updateGridTimer = 0.0f;
      UpdateBounds();
      ConstructGrid();
    } else {
      updateGridTimer += Time.deltaTime;
    }

    // Check if metaballs should be rerendered.
    if (renderMetaballsTimer > renderMetaballsTimeThreshold) {
      renderMetaballsTimer = 0.0f;
      UpdateGridValues();

      Dictionary<Vector3, int> vertices = new Dictionary<Vector3, int>();
      List<int> triangles = new List<int>();

      MarchingCubes(vertices, triangles);

      // Sort vertices by index.
      KeyValuePair<Vector3, int>[] vertexIndexPairs = vertices.OrderBy(x => x.Value).ToArray();
      Vector3[] orderedVertices = new Vector3[vertexIndexPairs.Length];
      for (int i = 0; i < orderedVertices.Length; ++i) {
        orderedVertices[i] = vertexIndexPairs[i].Key;
      }

      Mesh mesh = new Mesh();
      meshFilter.mesh = mesh;
      mesh.Clear();
      mesh.vertices = orderedVertices;
      mesh.triangles = triangles.ToArray();
      mesh.RecalculateNormals();
      mesh.RecalculateBounds();
    } else {
      renderMetaballsTimer += Time.deltaTime;
    }
  }

  // Constructs the uniform grid and assigns the 3D positions for each grid point.
  private void ConstructGrid() {
    // Have the same increment in all directions, so all grid cells are cubic.
    float increment = 1 / resolution;

    // Varying amount of grid points in all directions.
    int pointsX = (int) ((boundingBox.max.x - boundingBox.min.x) / increment);
    int pointsY = (int) ((boundingBox.max.y - boundingBox.min.y) / increment);
    int pointsZ = (int) ((boundingBox.max.z - boundingBox.min.z) / increment);

    grid = new GridPoint[pointsX, pointsY, pointsZ];

    for (int xIndex = 0; xIndex < pointsX; ++xIndex) {
      for (int yIndex = 0; yIndex < pointsY; ++yIndex) {
        for (int zIndex = 0; zIndex < pointsZ; ++zIndex) {
          Vector3 position = boundingBox.min +
                             new Vector3(xIndex*increment, yIndex*increment, zIndex*increment);
          grid[xIndex, yIndex, zIndex] = new GridPoint(position, 0.0f);
        }
      }
    }
  }

  private void UpdateBounds() {
    // Find min and max.
    Vector3 currentMin = Vector3.positiveInfinity;
    Vector3 currentMax = Vector3.negativeInfinity;
    Metaball[] metaballs = physicsController.metaballs;
    for (int i = 1; i < metaballs.Length; ++i) {
      Vector3 candidate = metaballs[i].transform.position;

      // Check if candidate position is lesser or greater than current min or max.
      if (candidate.x <= currentMin.x) currentMin.x = candidate.x;
      if (candidate.y <= currentMin.y) currentMin.y = candidate.y;
      if (candidate.z <= currentMin.z) currentMin.z = candidate.z;

      if (candidate.x >= currentMax.x) currentMax.x = candidate.x;
      if (candidate.y >= currentMax.y) currentMax.y = candidate.y;
      if (candidate.z >= currentMax.z) currentMax.z = candidate.z;
    }
    currentMin -= boundsOffset;
    currentMax += boundsOffset;

    // Create new zero-size bounds in the center of the balls.
    boundingBox = new Bounds(Vector3.Lerp(currentMin, currentMax, 0.5f), Vector3.zero);
    // Make bounds grow to encapsulate min and max.
    boundingBox.Encapsulate(currentMin);
    boundingBox.Encapsulate(currentMax);
  }

  // Updates the value in each grid point according to metaball positions.
  private void UpdateGridValues() {
    if (grid == null) return;

    for (int i = 0; i < grid.GetLength(0); ++i) {
      for (int j = 0; j < grid.GetLength(1); ++j) {
        for (int k = 0; k < grid.GetLength(2); ++k) {
          Vector3 position = grid[i,j,k].position;
          float pointValue = 0;
          Metaball[] metaballs = physicsController.metaballs;
          foreach (Metaball ball in metaballs) {
            pointValue += ball.Falloff(position);
          }
          grid[i,j,k].value = pointValue;
        }
      }
    }
  }

  // Begins the marching cubes algorithm.
  private void MarchingCubes(Dictionary<Vector3, int> vertices, List<int> triangles) {
    // Iterate through grid points, skip last one every time to account for grid cells.
    for (int i = 0; i < grid.GetLength(0)-1; ++i) {
      for (int j = 0; j < grid.GetLength(1)-1; ++j) {
        for (int k = 0; k < grid.GetLength(2)-1; ++k) {
          Polygonize(ConstructGridCell(i, j, k), vertices, triangles);
        }
      }
    }
  }

  private void Polygonize(GridCell cell, Dictionary<Vector3, int> vertices, List<int> triangles) {
    // Determine index into edge table.
    int cubeIndex = 0;
    for (int i = 0; i < cell.values.Length; i++) {
      if (cell.values[i] > threshold) cubeIndex |= (1 << i);
    }

    Vector3[] vertexList = new Vector3[12];

    // Find vertices where the metaball surface intersects the cube.
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 1) != 0)
    vertexList[0] =
    InterpolateVertex(cell.points[0], cell.points[1], cell.values[0], cell.values[1]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 2) != 0)
    vertexList[1] =
    InterpolateVertex(cell.points[1], cell.points[2], cell.values[1], cell.values[2]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 4) != 0)
    vertexList[2] =
    InterpolateVertex(cell.points[2], cell.points[3], cell.values[2], cell.values[3]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 8) != 0)
    vertexList[3] =
    InterpolateVertex(cell.points[3], cell.points[0], cell.values[3], cell.values[0]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 16) != 0)
    vertexList[4] =
    InterpolateVertex(cell.points[4], cell.points[5], cell.values[4], cell.values[5]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 32) != 0)
    vertexList[5] =
    InterpolateVertex(cell.points[5], cell.points[6], cell.values[5], cell.values[6]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 64) != 0)
    vertexList[6] =
    InterpolateVertex(cell.points[6], cell.points[7], cell.values[6], cell.values[7]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 128) != 0)
    vertexList[7] =
    InterpolateVertex(cell.points[7], cell.points[4], cell.values[7], cell.values[4]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 256) != 0)
    vertexList[8] =
    InterpolateVertex(cell.points[0], cell.points[4], cell.values[0], cell.values[4]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 512) != 0)
    vertexList[9] =
    InterpolateVertex(cell.points[1], cell.points[5], cell.values[1], cell.values[5]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 1024) != 0)
    vertexList[10] =
    InterpolateVertex(cell.points[2], cell.points[6], cell.values[2], cell.values[6]);
    if ((MarchingCubesTables.edgeTable[cubeIndex] & 2048) != 0)
    vertexList[11] =
    InterpolateVertex(cell.points[3], cell.points[7], cell.values[3], cell.values[7]);

    // Add vertices and triangles.
    int numVertices = vertices.Count; // The next vertex index.
    for (int i = 0; MarchingCubesTables.triTable[cubeIndex, i] != -1; i += 3) {
      int[] triangle = new int[3];
      // Create vertices and add to mesh vertex list.
      for (int j = 0; j < 3; ++j) {
        Vector3 vertex = vertexList[MarchingCubesTables.triTable[cubeIndex, i+j]];
        // Handle duplicate vertex.
        if (vertices.ContainsKey(vertex)) {
          // Add vertex index to triangle.
          triangle[j] = vertices[vertex];
          // No need to add duplicate vertex to vertices.
        } else { // Not a duplicate.
          // Add next vertex index to triangle.
          triangle[j] = vertices.Count;
          // Add new vertex to vertices.
          vertices.Add(vertex, vertices.Count);
        }
      }
      // Add the indices for the vertices to mesh triangles. The winding order in
      // the algorithm originally is counter-clockwise, but Unity requires clockwise.
      triangles.Add(triangle[0]);
      triangles.Add(triangle[2]);
      triangles.Add(triangle[1]);
    }
  }

  // Create a grid cell structure from the grid point at the given index.
  private GridCell ConstructGridCell(int xIndex, int yIndex, int zIndex) {
    // Get the point positions from the grid, indexed as in the paper.
    Vector3[] positions = {
      grid[xIndex, yIndex, zIndex].position,
      grid[xIndex, yIndex, zIndex+1].position,
      grid[xIndex+1, yIndex, zIndex+1].position,
      grid[xIndex+1, yIndex, zIndex].position,
      grid[xIndex, yIndex+1, zIndex].position,
      grid[xIndex, yIndex+1, zIndex+1].position,
      grid[xIndex+1, yIndex+1, zIndex+1].position,
      grid[xIndex+1, yIndex+1, zIndex].position
    };

    float[] values = {
      grid[xIndex, yIndex, zIndex].value,
      grid[xIndex, yIndex, zIndex+1].value,
      grid[xIndex+1, yIndex, zIndex+1].value,
      grid[xIndex+1, yIndex, zIndex].value,
      grid[xIndex, yIndex+1, zIndex].value,
      grid[xIndex, yIndex+1, zIndex+1].value,
      grid[xIndex+1, yIndex+1, zIndex+1].value,
      grid[xIndex+1, yIndex+1, zIndex].value
    };

    return new GridCell(positions, values);
  }

  private Vector3 InterpolateVertex(Vector3 point1, Vector3 point2, float value1, float value2) {
    Vector3 point;
    if(Mathf.Abs(value1 - value2) > 0.00001) {
      point = point1 + (point2 - point1) * ((threshold - value1) / (value2 - value1));
    } else {
      point = point1;
    }
    return point;
  }
}
