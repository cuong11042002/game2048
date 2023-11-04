using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public Vector2 Pos => transform.position;

    public Block OccupiedBlock;

    public Vector2 GetLeftNodePos()
    {
        return Pos + new Vector2(-1, 0);
    }
    public Vector2 GetRightNodePos()
    {
        return Pos + new Vector2(1, 0);
    }
    public Vector2 GetUpNodePos()
    {
        return Pos + new Vector2(0, 1);
    }
    public Vector2 GetDownNodePos()
    {
        return Pos + new Vector2(0, -1);
    }
}
