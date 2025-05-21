using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public static class PoissonDiskSampler
{
    public static List<Vector2> GeneratePoints(float minGap, float width, float height, int k = 30)
    {
        Random.InitState(7);

        float cellSize = minGap / (float)Mathf.Sqrt(2);
        int cols = (int)Mathf.CeilToInt(width / cellSize);
        int rows = (int)Mathf.CeilToInt(height / cellSize);
        int[,] grid = new int[cols, rows];
        for (int i = 0; i < cols; i++)
            for (int j = 0; j < rows; j++)
                grid[i, j] = -1;

        List<Vector2> Points = new List<Vector2>();
        List<int> activeList = new List<int>();
        Random.InitState(7);

        Vector2 firstVector2 = new Vector2(
            (float)Random.value * width,
            (float)Random.value * height
        );
        Points.Add(firstVector2);
        activeList.Add(0);
        int col = (int)(firstVector2.x / cellSize);
        int row = (int)(firstVector2.y / cellSize);
        grid[col, row] = 0;

        while (activeList.Count > 0)
        {
            int activeIndex = Random.Range(0, activeList.Count);
            int currentIndex = activeList[activeIndex];
            Vector2 current = Points[currentIndex];
            bool foundValid = false;

            for (int i = 0; i < k; i++)
            {
                float angle = Random.value * 2 * Mathf.PI;
                float newRadius = minGap + minGap * Random.value;
                float newX = current.x + (float)(Mathf.Cos(angle) * newRadius);
                float newY = current.y + (float)(Mathf.Sin(angle) * newRadius);

                if (newX < 0 || newX >= width || newY < 0 || newY >= height)
                    continue;

                int candidateCol = (int)(newX / cellSize);
                int candidateRow = (int)(newY / cellSize);

                bool isValid = true;
                for (int x = Mathf.Max(0, candidateCol - 2); x <= Mathf.Min(cols - 1, candidateCol + 2); x++)
                {
                    for (int y = Mathf.Max(0, candidateRow - 2); y <= Mathf.Min(rows - 1, candidateRow + 2); y++)
                    {
                        int neighborIndex = grid[x, y];
                        if (neighborIndex != -1)
                        {
                            Vector2 neighbor = Points[neighborIndex];
                            float dx = newX - neighbor.x;
                            float dy = newY - neighbor.y;
                            if (dx * dx + dy * dy < minGap * minGap)
                            {
                                isValid = false;
                                break;
                            }
                        }
                    }
                    if (!isValid) break;
                }

                if (isValid)
                {
                    Vector2 candidate = new Vector2(newX, newY);
                    Points.Add(candidate);
                    activeList.Add(Points.Count - 1);
                    grid[candidateCol, candidateRow] = Points.Count - 1;
                    foundValid = true;
                    break;
                }
            }

            if (!foundValid)
                activeList.RemoveAt(activeIndex);
        }

        return Points;
    }
}
