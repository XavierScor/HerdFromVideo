using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helper
{
    public const int OBSTACLE_LAYER_MASK = 1 << 6;
    public const string OBSTACLE_LAYER_NAME = "Obstacle";
    public const int AGENT_LAYER_MASK = 1 << 7;
    public const string AGENT_LAYER_NAME = "Agent";

    public static Vector2Int[] CellIterationSequence = {
        Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down, Vector2Int.right };

    public static bool IsCoordWithinBoundary(Vector2Int coord, int resolution)
    {
        return (coord.x >= 0) && (coord.x <= resolution - 1) && (coord.y >= 0) && (coord.y <= resolution - 1);
    }

    public static Vector3 CellCenterFromIndex(int index, int resolution, float groundSize, Vector3 groundCenter)
    {
        int x_coord = (int)Mathf.Floor(index % resolution);
        int y_coord = (int)Mathf.Floor(index / resolution);
        float gridWidth = groundSize / (float)resolution;
        Vector3 groundBottomLeft = groundCenter - new Vector3(groundSize / 2.0f, 0.0f, groundSize / 2.0f);
        Vector3 gridCenter = groundBottomLeft + new Vector3((x_coord + 0.5f) * gridWidth, 0.0f, (y_coord + 0.5f) * gridWidth);
        return gridCenter;
    }

    public static int CellIndexFromPosition(Vector3 position, int resolution, float groundSize, Vector3 groundCenter)
    {
        float gridWidth = groundSize / (float)resolution;
        Vector3 groundBottomLeft = groundCenter - new Vector3(groundSize / 2.0f, 0, groundSize / 2.0f);
        Vector3 relativePos = position - groundBottomLeft;
        int x_coord = (int)Mathf.Floor(relativePos.x / gridWidth);
        int y_coord = (int)Mathf.Floor(relativePos.z / gridWidth);
        return x_coord + y_coord * resolution;
    }

    public static Vector2Int CellCoordFromIndex(int index, int resolution)
    {
        int x_coord = (int)Mathf.Floor(index % resolution);
        int y_coord = (int)Mathf.Floor(index / resolution);
        return new Vector2Int(x_coord, y_coord);
    }

    public static int CellIndexFromCoord(Vector2Int coord, int resolution)
    {
        return coord.x + coord.y * resolution;
    }

    public static bool[] GetAccessibility(int resolution, float groundSize, Vector3 groundCenter)
    {
        bool[] gridAccessibility = new bool[resolution * resolution];
        float gridWidth = groundSize / (float)resolution;
        for (int j = 0; j < resolution; j++)
        {
            for (int i = 0; i < resolution; i++)
            {
                int gridIndex = Helper.CellIndexFromCoord(new Vector2Int(i, j), resolution);
                Vector3 gridCenter = Helper.CellCenterFromIndex(gridIndex, resolution, groundSize, groundCenter);
                Collider[] colliders = Physics.OverlapBox(gridCenter, Vector3.one * gridWidth / 2.0f, Quaternion.identity, OBSTACLE_LAYER_MASK);
                if (colliders.Length > 0) { gridAccessibility[gridIndex] = false; }
                else { gridAccessibility[gridIndex] = true; }
            }
        }
        return gridAccessibility;
    }

    // NOTE: OpenCV's Y coordinate starts from top to bottom
    //       Unity's Y coordinate starts from bottom to top
    public static int OpenCVIndexToUnityIndex(int opencvIndex, int resolution)
    {
        Vector2Int opencvCoord = CellCoordFromIndex(opencvIndex, resolution);
        Vector2Int unityCoord = new(opencvCoord[0], resolution - 1 - opencvCoord[1]);
        return CellIndexFromCoord(unityCoord, resolution);
    }

    public static float[] NormalizeArray(float[] array)
    {
        float norm = 0;
        for (int i = 0; i < array.Length; i++)
        {
            norm += array[i] * array[i];
        }
        norm = Mathf.Sqrt(norm);

        float[] result = new float[array.Length];

        if (norm > 1e-3f)
        {
            for (int i = 0; i < array.Length; i++)
            {
                result[i] = array[i] / norm;
            }
        }

        return result;
    }

    public static float GenerateNormalRandom(float mu, float var)
    {
        float sigma = Mathf.Sqrt(var);
        float rand1 = Random.Range(0.0f, 1.0f);
        float rand2 = Random.Range(0.0f, 1.0f);

        float n = Mathf.Sqrt(-2.0f * Mathf.Log(rand1)) * Mathf.Cos((2.0f * Mathf.PI) * rand2);

        return (mu + sigma * n);
    }
}
