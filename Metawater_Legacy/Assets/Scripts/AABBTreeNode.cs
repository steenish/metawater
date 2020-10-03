using UnityEngine;

public class AABBTreeNode {

  public Bounds bounds { get; private set; }
  public AABBTreeNode leftChild { get; set; }
  public AABBTreeNode rightChild { get; set; }
  public int[] triangles { get; set; }

  public AABBTreeNode(Bounds bounds) {
    this.bounds = bounds;
  }
}
