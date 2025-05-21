using PathCreation;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Navigation : MonoBehaviour
{
    // Navigation properties
    private int resolution;
    private float groundSize;
    private Vector3 groundCenter;

    // Navigation data
    private Vector3[] guidanceField;
    [SerializeField] bool isGuidanceAvoidObstacle = true;
    private float[] navigationCost;
    [HideInInspector] public Vector3[] navigationField;

    // Visualization related
    private bool isVisualInitialized = false;
    // Guidance line visualization
    [SerializeField] private GameObject GuidanceLineMeshPrefab;
    private List<string> guidanceLineNames = new();
    private float guidanceLineHeight = 0.5f;

    // C++ function to calculate navigation cost and direction by constrained optimization
    [DllImport("HerdNavigationPlugin.dll")]
    private static extern void NavigationDirectionAndCost(float rightCost, float upCost, float guidanceFieldX, float guidanceFieldY, ref NavigationResult result);

    // Struct container to collect result by C++ DLL
    struct NavigationResult
    {
        public float cost;
        public float alpha;
        public float navigationDirectionX;
        public float navigationDirectionY;
    }

    public void Initialize(int resolution, float groundWidth, Vector3 groundCenterPos)
    {
        // Initialize properties
        this.resolution = resolution;
        this.groundSize = groundWidth;
        this.groundCenter = groundCenterPos;

        navigationField = new Vector3[resolution * resolution];
    }

    // Update navigation field which is determined by a destination only
    // This method only updates the data, not the visual
    public void UpdateNavigationField(bool[] gridAccessibility, Vector3 destinationPos)
    {
        navigationField = new Vector3[resolution * resolution];
        navigationCost = new float[resolution * resolution];

        bool[] costChecked = new bool[resolution * resolution];
        for (int i = 0; i < navigationCost.Length; i++)
        {
            navigationCost[i] = resolution * resolution * 10.0f;
            costChecked[i] = false;
        }

        int destinationIndex = Helper.CellIndexFromPosition(destinationPos, resolution, groundSize, groundCenter);
        navigationCost[destinationIndex] = 0.0f;

        for (int i = 0; i < resolution * resolution - 1; i++)
        {
            int indexWithLowestCost = FindUncheckedIndexWithLowestCost(costChecked);
            if (indexWithLowestCost == -1) { Debug.LogError("Something is wrong with cost calculation at step: " + i + " !"); }

            costChecked[indexWithLowestCost] = true;
            if (gridAccessibility[indexWithLowestCost])
            {
                List<int> neighborIndices = FindAccessibleNeighbors(indexWithLowestCost, costChecked, gridAccessibility);
                foreach (int neighborIndex in neighborIndices)
                {
                    Vector2Int neighborCoord = Helper.CellCoordFromIndex(neighborIndex, resolution);
                    for (int k = 0; k < 4; k++)
                    {
                        Vector2Int rightCoord = neighborCoord + Helper.CellIterationSequence[k];
                        Vector2Int upCoord = neighborCoord + Helper.CellIterationSequence[k + 1];
                        if (Helper.IsCoordWithinBoundary(rightCoord, resolution) && Helper.IsCoordWithinBoundary(upCoord, resolution))
                        {
                            int rightIndex = Helper.CellIndexFromCoord(rightCoord, resolution);
                            int upIndex = Helper.CellIndexFromCoord(upCoord, resolution);

                            // DLL function for computing navigation direction and corresponding cost
                            NavigationResult result = new();
                            NavigationDirectionAndCost(navigationCost[rightIndex], navigationCost[upIndex], 0, 0, ref result);
                            if (result.cost <= navigationCost[neighborIndex])
                            {
                                navigationCost[neighborIndex] = result.cost;
                                navigationField[neighborIndex] =
                                    Quaternion.AngleAxis(-90.0f * k, Vector3.up) *
                                    new Vector3(result.navigationDirectionX, 0, result.navigationDirectionY);
                            }
                        }
                    }
                }
            }
        }
    }

    // Update navigation field which is determined by guidance lines only
    // This method only updates the data, not the visual
    public void UpdateNavigationField(PathCreator[] guidanceLines, int[] propagationRadius, bool[] gridAccessibility)
    {
        navigationField = new Vector3[resolution * resolution];
        ConvertGuidanceLineToField(guidanceLines, propagationRadius, gridAccessibility);
        for (int i = 0; i < resolution * resolution; i++)
        {
            if (gridAccessibility[i])
            {
                navigationField[i] = guidanceField[i];
            }
        }
    }

    // Update navigation field which is determined by (non-propagated) raw guidance field only
    // This method only updates the data, not the visual
    public void UpdateNavigationField(Vector3[] rawGuidanceField, int propagationRadius, bool[] gridAccessibility)
    {
        navigationField = new Vector3[resolution * resolution];
        guidanceField = new Vector3[resolution * resolution];
        int[] tileNumVisited = new int[resolution * resolution];
        List<int> gridsToPropagate = new();
        List<int> gridsChecked = new();
        List<int> correctionToPropagate = new();
        List<int> correctionGridsChecked = new();
        Vector3[] correctionField = new Vector3[resolution * resolution];
        int[] correctionTileNumVisited = new int[resolution * resolution];
        Vector3[] guidanceFieldBuffer = new Vector3[resolution * resolution];

        float guidanceMean = 0;
        int nonZeroGuidanceNum = 0;

        for (int i = 0; i < resolution * resolution; i++)
        {
            if (rawGuidanceField[i].sqrMagnitude > 1e-3)
            {
                guidanceFieldBuffer[i] = rawGuidanceField[i];
                tileNumVisited[i]++;
                if (!gridsChecked.Contains(i)) { gridsChecked.Add(i); }
                if (!gridsToPropagate.Contains(i)) { gridsToPropagate.Add(i); }

                guidanceMean += rawGuidanceField[i].magnitude;
                nonZeroGuidanceNum++;
            }
        }

        guidanceMean /= nonZeroGuidanceNum;

        PropagateGuidanceField(
            gridsToPropagate, gridsChecked, guidanceFieldBuffer, tileNumVisited, propagationRadius,
            correctionToPropagate, correctionGridsChecked, correctionField, correctionTileNumVisited, gridAccessibility);
        PropagateGuidanceField(correctionToPropagate, correctionGridsChecked, correctionField,
            correctionTileNumVisited, Mathf.FloorToInt(0.5f * propagationRadius), gridAccessibility);
        for (int i = 0; i < resolution * resolution; i++)
        {
            if (isGuidanceAvoidObstacle)
            {
                //guidanceField[i] += guidanceFieldBuffer[i].magnitude *
                //    (guidanceFieldBuffer[i] + correctionField[i]).normalized;

                guidanceField[i] += guidanceMean * (guidanceFieldBuffer[i] + correctionField[i]).normalized;
            }
            else
            {
                guidanceField[i] += guidanceFieldBuffer[i];
            }
        }
        for (int i = 0; i < resolution * resolution; i++)
        {
            if (gridAccessibility[i])
            {
                navigationField[i] = guidanceField[i];
            }
        }
    }

    // Update navigation field which is determined by guidance lines AND a destination
    // This method only updates the data, not the visual
    public void UpdateNavigationField(PathCreator[] guidanceLines, int[] propagationRadius, bool[] gridAccessibility, Vector3 destinationPos)
    {
        navigationField = new Vector3[resolution * resolution];
        navigationCost = new float[resolution * resolution];
        ConvertGuidanceLineToField(guidanceLines, propagationRadius, gridAccessibility);

        bool[] costChecked = new bool[resolution * resolution];
        for (int i = 0; i < navigationCost.Length; i++)
        {
            navigationCost[i] = resolution * resolution * 10.0f;
            costChecked[i] = false;
        }

        int destinationIndex = Helper.CellIndexFromPosition(destinationPos, resolution, groundSize, groundCenter);
        navigationCost[destinationIndex] = 0.0f;

        for (int i = 0; i < resolution * resolution - 1; i++)
        {
            int indexWithLowestCost = FindUncheckedIndexWithLowestCost(costChecked);
            if (indexWithLowestCost == -1) { Debug.LogError("Something is wrong with cost calculation at step: " + i + " !"); }

            costChecked[indexWithLowestCost] = true;
            if (gridAccessibility[indexWithLowestCost])
            {
                List<int> neighborIndices = FindAccessibleNeighbors(indexWithLowestCost, costChecked, gridAccessibility);
                foreach (int neighborIndex in neighborIndices)
                {
                    Vector2Int neighborCoord = Helper.CellCoordFromIndex(neighborIndex, resolution);
                    for (int k = 0; k < 4; k++)
                    {
                        Vector2Int rightCoord = neighborCoord + Helper.CellIterationSequence[k];
                        Vector2Int upCoord = neighborCoord + Helper.CellIterationSequence[k + 1];
                        if (Helper.IsCoordWithinBoundary(rightCoord, resolution) && Helper.IsCoordWithinBoundary(upCoord, resolution))
                        {
                            int rightIndex = Helper.CellIndexFromCoord(rightCoord, resolution);
                            int upIndex = Helper.CellIndexFromCoord(upCoord, resolution);

                            // DLL function for computing navigation direction and corresponding cost
                            NavigationResult result = new();
                            Vector3 guidanceFieldRotated = Quaternion.AngleAxis(90.0f * k, Vector3.up) * guidanceField[neighborIndex];
                            NavigationDirectionAndCost(navigationCost[rightIndex], navigationCost[upIndex],
                                guidanceFieldRotated.x, guidanceFieldRotated.z, ref result);
                            if (result.cost <= navigationCost[neighborIndex])
                            {
                                navigationCost[neighborIndex] = result.cost;
                                navigationField[neighborIndex] =
                                    Quaternion.AngleAxis(-90.0f * k, Vector3.up) *
                                    new Vector3(result.navigationDirectionX, 0, result.navigationDirectionY);
                            }
                        }
                    }
                }
            }
        }
    }

    // Convert guidance line to corresponding grid and propagate the field
    private void ConvertGuidanceLineToField(PathCreator[] guidanceLines, int[] propagationRadius, bool[] gridAccessibility)
    {
        float ratioStep = 0.01f;
        guidanceField = new Vector3[resolution * resolution];
        for (int l = 0; l < guidanceLines.Length; l++)
        {
            int[] tileNumVisited = new int[resolution * resolution];
            List<int> gridsToPropagate = new();
            List<int> gridsChecked = new();
            List<int> correctionToPropagate = new();
            List<int> correctionGridsChecked = new();
            Vector3[] correctionField = new Vector3[resolution * resolution];
            int[] correctionTileNumVisited = new int[resolution * resolution];
            Vector3[] guidanceFieldCurrentLine = new Vector3[resolution * resolution];
            for (int i = 0; i < Mathf.CeilToInt(1.0f / ratioStep); i++)
            {
                int gridIndex = Helper.CellIndexFromPosition(guidanceLines[l].path.GetPointAtTime(i * ratioStep), resolution, groundSize, groundCenter);
                tileNumVisited[gridIndex]++;
                guidanceFieldCurrentLine[gridIndex] = guidanceFieldCurrentLine[gridIndex] +
                    (guidanceLines[l].path.GetDirection(i * ratioStep).normalized - guidanceFieldCurrentLine[gridIndex]) / (float)tileNumVisited[gridIndex];
                if (!gridsChecked.Contains(gridIndex)) { gridsChecked.Add(gridIndex); }
                if (!gridsToPropagate.Contains(gridIndex)) { gridsToPropagate.Add(gridIndex); }
            }
            PropagateGuidanceField(
                gridsToPropagate, gridsChecked, guidanceFieldCurrentLine, tileNumVisited, propagationRadius[l],
                correctionToPropagate, correctionGridsChecked, correctionField, correctionTileNumVisited, gridAccessibility);
            PropagateGuidanceField(correctionToPropagate, correctionGridsChecked, correctionField,
                correctionTileNumVisited, Mathf.FloorToInt(0.5f * propagationRadius[l]), gridAccessibility);
            for (int i = 0; i < resolution * resolution; i++)
            {
                if (isGuidanceAvoidObstacle)
                {
                    guidanceField[i] += guidanceFieldCurrentLine[i].magnitude *
                        (guidanceFieldCurrentLine[i] + correctionField[i]).normalized;
                }
                else
                {
                    guidanceField[i] += guidanceFieldCurrentLine[i];
                }
            }
        }
        for (int i = 0; i < resolution * resolution; i++)
            guidanceField[i] = Vector3.ClampMagnitude(guidanceField[i], 1.0f - 1e-4f);
    }

    // Recursive function to propagate field
    private void PropagateGuidanceField(
        List<int> gridsToPropagate, List<int> gridsChecked, Vector3[] guidanceField,
        int[] tileNumVisited, int propagationRadius, bool[] gridAccessibility)
    {
        if (propagationRadius > 1)
        {
            List<int> newGridsChecked = new();
            List<int> newGridToPropagate = new();
            foreach (int centerGridIndex in gridsToPropagate)
            {
                Vector2Int centerGridCoord = Helper.CellCoordFromIndex(centerGridIndex, resolution);
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int neighborGridCoord = centerGridCoord + Helper.CellIterationSequence[i];
                    int neighborGridIndex = Helper.CellIndexFromCoord(neighborGridCoord, resolution);
                    if (Helper.IsCoordWithinBoundary(neighborGridCoord, resolution) &&
                        !gridsChecked.Contains(neighborGridIndex) &&
                        gridAccessibility[neighborGridIndex])
                    {
                        tileNumVisited[neighborGridIndex]++;
                        guidanceField[neighborGridIndex] = guidanceField[neighborGridIndex] +
                            (((float)propagationRadius - 1f) / (float)propagationRadius *
                            guidanceField[centerGridIndex] - guidanceField[neighborGridIndex]) / (float)tileNumVisited[neighborGridIndex];
                        if (!newGridsChecked.Contains(neighborGridIndex)) { newGridsChecked.Add(neighborGridIndex); }
                        if (!newGridToPropagate.Contains(neighborGridIndex)) { newGridToPropagate.Add(neighborGridIndex); }
                    }
                }
            }
            gridsChecked.AddRange(newGridsChecked);
            PropagateGuidanceField(newGridToPropagate, gridsChecked, guidanceField, tileNumVisited, propagationRadius - 1, gridAccessibility);
        }
    }

    private void PropagateGuidanceField(
        List<int> gridsToPropagate, List<int> gridsChecked, Vector3[] guidanceField, int[] tileNumVisited, int propagationRadius,
        List<int> correctionToPropagate, List<int> correctionGridsChecked, Vector3[] correctionField, int[] correctionTileNumVisited,
        bool[] gridAccessibility)
    {
        if (propagationRadius > 1)
        {
            List<int> newGridsChecked = new();
            List<int> newGridToPropagate = new();
            foreach (int centerGridIndex in gridsToPropagate)
            {
                Vector2Int centerGridCoord = Helper.CellCoordFromIndex(centerGridIndex, resolution);
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int neighborGridCoord = centerGridCoord + Helper.CellIterationSequence[i];
                    int neighborGridIndex = Helper.CellIndexFromCoord(neighborGridCoord, resolution);
                    if (Helper.IsCoordWithinBoundary(neighborGridCoord, resolution) && !gridsChecked.Contains(neighborGridIndex))
                    {
                        tileNumVisited[neighborGridIndex]++;
                        guidanceField[neighborGridIndex] = guidanceField[neighborGridIndex] +
                            (((float)propagationRadius - 1f) / (float)propagationRadius *
                            guidanceField[centerGridIndex] - guidanceField[neighborGridIndex]) / (float)tileNumVisited[neighborGridIndex];
                        if (!newGridsChecked.Contains(neighborGridIndex)) { newGridsChecked.Add(neighborGridIndex); }
                        if (!newGridToPropagate.Contains(neighborGridIndex)) { newGridToPropagate.Add(neighborGridIndex); }
                    }
                    if (Helper.IsCoordWithinBoundary(neighborGridCoord, resolution) && !gridAccessibility[neighborGridIndex])
                    {
                        Vector2 vectorFromCenterToNeighbor = neighborGridCoord - centerGridCoord;
                        Vector3 correctionDirection = new Vector3(vectorFromCenterToNeighbor.x, 0, vectorFromCenterToNeighbor.y).normalized;
                        if (Vector3.Angle(correctionDirection, guidanceField[centerGridIndex]) < 90.0f)
                        {
                            Vector3 correction = -correctionDirection * Vector3.Dot(correctionDirection, guidanceField[centerGridIndex]);
                            if (!correctionToPropagate.Contains(centerGridIndex)) { correctionToPropagate.Add(centerGridIndex); }
                            if (!correctionGridsChecked.Contains(centerGridIndex)) { correctionGridsChecked.Add(centerGridIndex); }
                            correctionField[centerGridIndex] += correction;
                            correctionTileNumVisited[centerGridIndex] = 1;
                        }
                    }
                }
            }
            gridsChecked.AddRange(newGridsChecked);
            PropagateGuidanceField(
                newGridToPropagate, gridsChecked, guidanceField, tileNumVisited, propagationRadius - 1,
                correctionToPropagate, correctionGridsChecked, correctionField, correctionTileNumVisited,
                gridAccessibility);
        }
    }

    // Find the grid which has the lowest cost among unchecked ones
    // Array.Min() does not work here because the grid must be unchecked
    private int FindUncheckedIndexWithLowestCost(bool[] costChecked)
    {
        float lowestCost = resolution * resolution * 10.0f;
        int indexWithLowestCost = -1;

        for (int i = 0; i < navigationCost.Length; i++)
        {
            if (navigationCost[i] <= lowestCost && costChecked[i] == false)
            {
                lowestCost = navigationCost[i];
                indexWithLowestCost = i;
            }
        }

        return indexWithLowestCost;
    }

    // Find unchecked and accessible neighbor to propagate cost
    private List<int> FindAccessibleNeighbors(int index, bool[] costChecked, bool[] gridAccessibility)
    {
        List<int> result = new();
        Vector2Int coord = Helper.CellCoordFromIndex(index, resolution);
        for (int i = 0; i < 4; i++)
        {
            Vector2Int neighborCoord = coord + Helper.CellIterationSequence[i];
            if (Helper.IsCoordWithinBoundary(neighborCoord, resolution))
            {
                int neighborIndex = Helper.CellIndexFromCoord(coord + Helper.CellIterationSequence[i], resolution);
                if (gridAccessibility[neighborIndex] && costChecked[neighborIndex] == false) { result.Add(neighborIndex); }
            }
        }
        return result;
    }

    // Initialize visuals for grids and guidance lines
    public void TurnOnVisual(PathCreator[] guidanceLines, int guidanceLineResolution, int numGuidanceLineArrows, float guidanceLineWidth)
    {
        if (!isVisualInitialized)
        {
            DrawGuidanceLines(guidanceLines, guidanceLineResolution, numGuidanceLineArrows, guidanceLineWidth);
            isVisualInitialized = true;
        }
    }

    public void TurnOnGuidanceLines(PathCreator[] guidanceLines, int guidanceLineResolution, int numGuidanceLineArrows, float guidanceLineWidth)
    {
        DrawGuidanceLines(guidanceLines, guidanceLineResolution, numGuidanceLineArrows, guidanceLineWidth);
    }

    // Update only visuals
    public void UpdateVisual(bool[] gridAccessibility)
    {
        if (!isVisualInitialized)
        {
            Debug.LogError("Call TurnOnVisual before update the visual of navigation field!");
        }
        else
        {
            BakeAccessibility(gridAccessibility);
        }
    }

    // Turn off visual and kill related game objects
    public void TurnOffVisual()
    {
        if (isVisualInitialized)
        {
            for (int i = 0; i < guidanceLineNames.Count; i++)
                Destroy(transform.Find(guidanceLineNames[i]).gameObject);
            guidanceLineNames.Clear();
            isVisualInitialized = false;
        }
    }

    private void BakeAccessibility(bool[] gridAccessibility)
    {
        float[] gridAccessibilityFloat = new float[resolution * resolution];
        for (int i = 0; i < resolution * resolution; i++)
        {
            if (gridAccessibility[i])
            {
                gridAccessibilityFloat[i] = 0f;
            }
            else
            {
                gridAccessibilityFloat[i] = 1f - 1e-5f;
            }
        }
    }

    private void DrawGuidanceLines(PathCreator[] guidanceLines, int guidanceLineResolution, int numGuidanceLineArrows, float guidanceLineWidth)
    {
        for (int l = 0; l < guidanceLines.Length; l++)
        {
            Color lineColor = Color.HSVToRGB(l * 1.0f / guidanceLines.Length, 1.0f, 1.0f);
            GameObject guidanceLineVisual = Instantiate(GuidanceLineMeshPrefab, transform.position + guidanceLineHeight * Vector3.up, Quaternion.identity, transform);
            guidanceLineVisual.name = "GuidanceLine" + (l + 1);
            guidanceLineNames.Add(guidanceLineVisual.name);

            List<Vector3> verticesList = new();
            List<int> trianglesList = new();

            Vector3 currentPosOnLine = guidanceLines[l].path.GetPointAtTime(0);
            Vector3 rightDirOnLine = Quaternion.AngleAxis(-90.0f, Vector3.up) * guidanceLines[l].path.GetDirection(0);
            verticesList.Add(currentPosOnLine - 0.5f * guidanceLineWidth * rightDirOnLine);
            verticesList.Add(currentPosOnLine + 0.5f * guidanceLineWidth * rightDirOnLine);

            for (int i = 1; i <= guidanceLineResolution; i++)
            {
                currentPosOnLine = guidanceLines[l].path.GetPointAtTime(i * 1.0f / guidanceLineResolution - 1e-5f);
                rightDirOnLine = Quaternion.AngleAxis(-90.0f, Vector3.up) * guidanceLines[l].path.GetDirection(i * 1.0f / guidanceLineResolution - 1e-5f);
                verticesList.Add(currentPosOnLine - 0.5f * guidanceLineWidth * rightDirOnLine);
                verticesList.Add(currentPosOnLine + 0.5f * guidanceLineWidth * rightDirOnLine);
                trianglesList.Add(verticesList.Count - 4); trianglesList.Add(verticesList.Count - 3); trianglesList.Add(verticesList.Count - 1);
                trianglesList.Add(verticesList.Count - 4); trianglesList.Add(verticesList.Count - 1); trianglesList.Add(verticesList.Count - 2);
            }

            for (int i = 1; i <= numGuidanceLineArrows; i++)
            {
                currentPosOnLine = guidanceLines[l].path.GetPointAtTime(i * 1.0f / (numGuidanceLineArrows + 1.0f));
                rightDirOnLine = Quaternion.AngleAxis(-90.0f, Vector3.up) * guidanceLines[l].path.GetDirection(i * 1.0f / (numGuidanceLineArrows + 1.0f));
                Vector3 currentDirOnLine = guidanceLines[l].path.GetDirection(i * 1.0f / (numGuidanceLineArrows + 1.0f)).normalized;

                verticesList.Add(currentPosOnLine - 2.0f * guidanceLineWidth * rightDirOnLine);
                verticesList.Add(currentPosOnLine + 2.0f * guidanceLineWidth * rightDirOnLine);
                verticesList.Add(currentPosOnLine + 1.73f * 2.0f * guidanceLineWidth * currentDirOnLine);
                trianglesList.Add(verticesList.Count - 3); trianglesList.Add(verticesList.Count - 2); trianglesList.Add(verticesList.Count - 1);
            }

            guidanceLineVisual.GetComponent<MeshFilter>().mesh.vertices = verticesList.ToArray();
            guidanceLineVisual.GetComponent<MeshFilter>().mesh.triangles = trianglesList.ToArray();
            guidanceLineVisual.GetComponent<MeshRenderer>().material.color = lineColor;
        }
    }

    public Vector3 GetNavigationByPos(Vector3 pos)
    {
        return navigationField[Helper.CellIndexFromPosition(pos, resolution, groundSize, groundCenter)];
    }
}
