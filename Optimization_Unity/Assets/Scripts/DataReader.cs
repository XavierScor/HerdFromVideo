using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DataReader
{
    // === Meta data overall ===
    private string[] _metaTextLineArray;

    public int Resolution;
    public float GroundSize;
    public float CellSize;
    public Vector3 GroundCenter;

    public float FrameTime;
    public int TotalVideoFrameCount;

    public float OverallAverageSpeed;
    public float OverallAverageSpeedSTD;

    private int _speedVSDensityStartLineIndex;
    private int _speedVSDensityEndLineIndex;
    public List<float> SpeedWhenAgentNum;
    public List<float> SpeedSTDWhenAgentNum;

    // === Cell data at current frame ===
    private string[] _frameTextLineArray;
    private List<int> _frameStartLineIndices;
    public int NumEntriesFrameData;

    public int VideoFrameIndex;
    public float FramePolarization;
    public float FrameAngularMomentum;
    public int[] AgentNum;
    public float[] AgentVelocityXMean, AgentVelocityXVar;
    public float[] AgentVelocityYMean, AgentVelocityYVar;
    public Vector3[] AgentVelocityMean;
    public float[] AgentAngleVar;

    // === Cell data overall (accessibility, general flow) ===
    private string[] _cellTextLineArray;

    private int _accessibilityStartLineIndex;
    private int _accessibilityEndLineIndex;
    public bool[] Accessibility;

    private int _cellGuidanceStartLineIndex;
    private int _cellGuidanceEndLineIndex;
    public Vector3[] GuidanceField;
    public float MaxGuidanceMagnitude = 0;

    public DataReader()
    {
        
    }

    private void ParseTextLine(string textLine, int textLineIndex)
    {
        // === Process meta data: size info ===
        string[] entries = textLine.Split(' ');

        if (entries[0].Length >= 10 && entries[0][..10] == "Resolution")
        {
            Resolution = int.Parse(entries[1]);
        }
        if (entries[0].Length >= 10 && entries[0][..10] == "GroundSize")
        {
            GroundSize = float.Parse(entries[1]);
        }

        // === Process meta data: video info ===
        if (entries[0].Length >= 9 && entries[0][..9] == "FrameTime")
        {
            FrameTime = float.Parse(entries[1]);
        }
        if (entries[0].Length >= 15 && entries[0][..15] == "TotalFrameCount")
        {
            TotalVideoFrameCount = int.Parse(entries[1]);
        }
        if (entries[0].Length >= 19 && entries[0][..19] == "OverallAverageSpeed" && 
            entries[2].Length >= 22 && entries[2][..22] == "OverallAverageSpeedSTD")
        {
            OverallAverageSpeed = float.Parse(entries[1]);
            OverallAverageSpeedSTD = float.Parse(entries[3]);
        }

        // === Process meta data: speed v.s. density ===
        if (entries[0].Length >= 14 && entries[0][..14] == "SpeedVSDensity")
        {
            _speedVSDensityStartLineIndex = textLineIndex;
        }
        if (entries[0].Length >= 5 && entries[0][..5] == "SDEND")
        {
            _speedVSDensityEndLineIndex = textLineIndex;
        }

        // === Process frame data ===
        if (entries[0].Length >= 5 && entries[0].Length < 9 && entries[0][..5] == "Frame")
        {
            _frameStartLineIndices.Add(textLineIndex);
        }

        // === Process cell data ===
        if (entries[0].Length >= 17 && entries[0][..17] == "NonAccessibleGrid")
        {
            _accessibilityStartLineIndex = textLineIndex;
        }
        if (entries[0].Length >= 6 && entries[0][..6] == "NAGEND")
        {
            _accessibilityEndLineIndex = textLineIndex;
        }

        if (entries[0].Length >= 13&& entries[0][..13] == "GuidanceField")
        {
            _cellGuidanceStartLineIndex = textLineIndex;
        }
        if (entries[0].Length >= 5 && entries[0][..5] == "GFEND")
        {
            _cellGuidanceEndLineIndex = textLineIndex;
        }
    }

    public void LoadStaticData(string filename)
    {
        // === Load and create TextAsset for meta data ===
        string metaDataPath = filename + "_data_meta";
        TextAsset metaData = Resources.Load<TextAsset>(metaDataPath);
        if (metaData == null)
            Debug.Log("Meta data not load: " + metaDataPath);

        _metaTextLineArray = metaData.text.Split('\n');
        for (int i = 0; i < _metaTextLineArray.Length; i++)
        {
            ParseTextLine(_metaTextLineArray[i], i);
        }
        CellSize = GroundSize / (float)Resolution;
        GroundCenter = new(GroundSize / 2.0f, 0.0f, GroundSize / 2.0f);
        AgentNum = new int[Resolution * Resolution];

        // === Load and create TextAsset for frame data ===
        string frameDataPath = filename + "_data_frame";
        TextAsset frameData = Resources.Load<TextAsset>(frameDataPath);
        if (frameData == null)
            Debug.Log("Frame data not load: " + frameDataPath);

        _frameStartLineIndices = new();
        _frameTextLineArray = frameData.text.Split('\n');
        for (int i = 0; i < _frameTextLineArray.Length; i++)
        {
            ParseTextLine(_frameTextLineArray[i], i);
        }
        NumEntriesFrameData = _frameStartLineIndices.Count;
        _frameStartLineIndices.Add(_frameTextLineArray.Length - 1);
    }

    public void LoadVideoData(string filename)
    {
        // === Load and create TextAsset for meta data ===
        string metaDataPath = filename + "_data_meta";
        TextAsset metaData = Resources.Load<TextAsset>(metaDataPath);
        if (metaData == null)
            Debug.Log("Meta data not load: " + metaDataPath);

        _metaTextLineArray = metaData.text.Split('\n');
        for (int i = 0; i < _metaTextLineArray.Length; i++)
        {
            ParseTextLine(_metaTextLineArray[i], i);
        }
        CellSize = GroundSize / (float)Resolution;
        GroundCenter = new(GroundSize / 2.0f, 0.0f, GroundSize / 2.0f);
        AgentNum = new int[Resolution * Resolution];
        AgentVelocityXMean = new float[Resolution * Resolution];
        AgentVelocityXVar = new float[Resolution * Resolution];
        AgentVelocityYMean = new float[Resolution * Resolution];
        AgentVelocityYVar = new float[Resolution * Resolution];
        AgentVelocityMean = new Vector3[Resolution * Resolution];
        AgentAngleVar = new float[Resolution * Resolution];

        // === Load and create TextAsset for frame data ===
        string frameDataPath = filename + "_data_frame";
        TextAsset frameData = Resources.Load<TextAsset>(frameDataPath);
        if (frameData == null)
            Debug.Log("Frame data not load: " + frameDataPath);

        _frameStartLineIndices = new();
        _frameTextLineArray = frameData.text.Split('\n');
        for (int i = 0; i < _frameTextLineArray.Length; i++)
        {
            ParseTextLine(_frameTextLineArray[i], i);
        }
        NumEntriesFrameData = _frameStartLineIndices.Count;
        _frameStartLineIndices.Add(_frameTextLineArray.Length - 1);

        // === Load and create TextAsset for cell data ===
        string cellDataPath = filename + "_data_cell";
        TextAsset cellData = Resources.Load<TextAsset>(cellDataPath);
        if (cellData == null)
            Debug.Log("Cell data not load: " + cellDataPath);
        _cellTextLineArray = cellData.text.Split("\n");
        for (int i = 0; i < _cellTextLineArray.Length; i++)
        {
            ParseTextLine(_cellTextLineArray[i], i);
        }
        Accessibility = new bool[Resolution * Resolution];
        GuidanceField = new Vector3[Resolution * Resolution];
    }

    public Texture2D LoadFrameTexture(string foldername, int frameIndex)
    {
        string framePath = foldername + '/' + frameIndex.ToString("D4");
        Texture2D frameTexture = Resources.Load<Texture2D>(framePath);
        if (frameTexture == null)
            Debug.Log("Texture not load: " + framePath);
        return frameTexture;
    }

    // NOTE: Frame index starts from 0 for this function
    public bool LoadFrameData(int frameIndex)
    {
        bool isLoadSuccess = true;
        int gridFlattenLength = Resolution * Resolution;

        Array.Clear(AgentNum, 0, gridFlattenLength);
        if (AgentVelocityXMean != null)
        {
            Array.Clear(AgentVelocityXMean, 0, gridFlattenLength);
            Array.Clear(AgentVelocityXVar, 0, gridFlattenLength);
            Array.Clear(AgentVelocityYMean, 0, gridFlattenLength);
            Array.Clear(AgentVelocityYVar, 0, gridFlattenLength);
            Array.Clear(AgentVelocityMean, 0, gridFlattenLength);
            Array.Clear(AgentAngleVar, 0, gridFlattenLength);
        }

        if (frameIndex < _frameStartLineIndices.Count - 1)
        {
            int startIndex = _frameStartLineIndices[frameIndex];
            int endIndex = _frameStartLineIndices[frameIndex + 1];

            if (_frameTextLineArray[startIndex][..5] == "Frame")
            {
                string[] headEntries = _frameTextLineArray[startIndex].Split(' ');
                VideoFrameIndex = int.Parse(headEntries[1]);
                if (headEntries.Length >= 8)
                {
                    FramePolarization = float.Parse(headEntries[5]);
                    FrameAngularMomentum = float.Parse(headEntries[7]);
                }

                for (int i = startIndex + 1; i < endIndex; i++)
                {
                    string[] dataEntries = _frameTextLineArray[i].Split(' ');
                    if (dataEntries[0] == "Data")
                    {
                        Vector2Int coord = new(int.Parse(dataEntries[1]), int.Parse(dataEntries[2]));
                        int opencvIndex = Helper.CellIndexFromCoord(coord, Resolution);
                        int unityIndex = Helper.OpenCVIndexToUnityIndex(opencvIndex, Resolution);
                        AgentNum[unityIndex] = (int)float.Parse(dataEntries[3]);

                        if (dataEntries.Length >= 10)
                        {
                            AgentVelocityXMean[unityIndex] = float.Parse(dataEntries[4]);
                            AgentVelocityXVar[unityIndex] = float.Parse(dataEntries[5]);
                            AgentVelocityYMean[unityIndex] = -float.Parse(dataEntries[6]);
                            AgentVelocityYVar[unityIndex] = float.Parse(dataEntries[7]);
                            AgentVelocityMean[unityIndex] = new Vector3(AgentVelocityXMean[unityIndex], 0.0f, AgentVelocityYMean[unityIndex]);
                            AgentAngleVar[unityIndex] = float.Parse(dataEntries[9]);
                        }
                    }
                    else
                    {
                        Debug.LogError("Frame " + frameIndex + ", Data " + i + ": data not found.");
                        isLoadSuccess = false;
                    }
                }
            }
            else
            {
                Debug.LogError("Frame " + frameIndex + ": header not found.");
                isLoadSuccess = false;
            }
        }
        else
        {
            Debug.LogError("Frame index " + frameIndex + " too large.");
            isLoadSuccess = false;
        }

        return isLoadSuccess;
    }

    public bool LoadSpeedVSDensityData()
    {
        bool isLoadSuccess = true;
        SpeedWhenAgentNum = new();
        SpeedSTDWhenAgentNum = new();

        SpeedWhenAgentNum.Add(0.0f);
        SpeedSTDWhenAgentNum.Add(0.0f);

        int maxAgentNumRecorded = _speedVSDensityEndLineIndex - _speedVSDensityStartLineIndex - 1;
        for (int i = 1; i <= maxAgentNumRecorded; i++)
        {
            string[] dataEntries = _metaTextLineArray[i + _speedVSDensityStartLineIndex].Split(' ');
            if (int.Parse(dataEntries[0]) == i)
            {
                SpeedWhenAgentNum.Add(float.Parse(dataEntries[1]));
                SpeedSTDWhenAgentNum.Add(float.Parse(dataEntries[2]));
            }
            else
            {
                Debug.LogError("Speed when agent num = " + i + " is not found.");
                isLoadSuccess = false;
            }
        }

        return isLoadSuccess;
    }

    public bool LoadCellData(bool isFilterLowGuidance, float guidanceFilterThreshold = 0.0f)
    {
        bool isLoadSuccess = true;
        int gridFlattenLength = Resolution * Resolution;

        Array.Clear(Accessibility, 0, gridFlattenLength);
        Array.Clear(GuidanceField, 0, gridFlattenLength);

        // === Read and load cell accessibility ===
        int numAccessibilityTextLines = _accessibilityEndLineIndex - _accessibilityStartLineIndex - 1;
        for (int i = 1; i <= numAccessibilityTextLines; i++)
        {
            string[] dataEntries = _cellTextLineArray[i + _accessibilityStartLineIndex].Split(' ');
            if (dataEntries[0] == "NAG")
            {
                Vector2Int coord = new(int.Parse(dataEntries[1]), int.Parse(dataEntries[2]));
                int opencvIndex = Helper.CellIndexFromCoord(coord, Resolution);
                int unityIndex = Helper.OpenCVIndexToUnityIndex(opencvIndex, Resolution);
                Accessibility[unityIndex] = false;
            }
            else
            {
                Debug.LogError("Non-accessible cell format error: " + dataEntries[0]);
                isLoadSuccess = false;
            }
        }

        // === Read and load guidance field ===
        int numGuidanceFieldTextLines = _cellGuidanceEndLineIndex - _cellGuidanceStartLineIndex - 1;
        for (int i = 1; i <= numGuidanceFieldTextLines; i++)
        {
            string[] dataEntries = _cellTextLineArray[i + _cellGuidanceStartLineIndex].Split(' ');
            if (dataEntries[0] == "GF")
            {
                Vector2Int coord = new(int.Parse(dataEntries[1]), int.Parse(dataEntries[2]));
                int opencvIndex = Helper.CellIndexFromCoord(coord, Resolution);
                int unityIndex = Helper.OpenCVIndexToUnityIndex(opencvIndex, Resolution);
                GuidanceField[unityIndex].x = float.Parse(dataEntries[3]);
                GuidanceField[unityIndex].z = -float.Parse(dataEntries[4]);

                if (GuidanceField[unityIndex].magnitude > MaxGuidanceMagnitude)
                {
                    MaxGuidanceMagnitude = GuidanceField[unityIndex].magnitude;
                }
            }
            else
            {
                Debug.LogError("Guidance field format error: " + dataEntries[0]);
                isLoadSuccess = false;
            }
        }
        if (isLoadSuccess && isFilterLowGuidance)
        {
            for (int i = 0; i < GuidanceField.Length; i++)
            {
                if (GuidanceField[i].magnitude < guidanceFilterThreshold * MaxGuidanceMagnitude)
                {
                    GuidanceField[i] = Vector3.zero;
                }
            }
        }

        return isLoadSuccess;
    }

    public void SetAccessibility(bool[] accessibility)
    {
        for (int i = 0; i < Resolution * Resolution; i++)
        {
            Accessibility[i] = accessibility[i];
        }
    }

    public void PropagateGuidanceField(bool[] accessibility, int propagationRadius)
    {
        int[] tileNumVisited = new int[Resolution * Resolution];
        List<int> gridsToPropagate = new();
        List<int> gridsChecked = new();
        List<int> correctionToPropagate = new();
        List<int> correctionGridsChecked = new();
        Vector3[] correctionField = new Vector3[Resolution * Resolution];
        int[] correctionTileNumVisited = new int[Resolution * Resolution];
        Vector3[] guidanceFieldBuffer = new Vector3[Resolution * Resolution];

        for (int i = 0; i < Resolution * Resolution; i++)
        {
            if (GuidanceField[i].sqrMagnitude > 1e-3)
            {
                guidanceFieldBuffer[i] = GuidanceField[i];
                tileNumVisited[i]++;
                if (!gridsChecked.Contains(i)) { gridsChecked.Add(i); }
                if (!gridsToPropagate.Contains(i)) { gridsToPropagate.Add(i); }
            }
        }

        PropagateVectorField(
            gridsToPropagate, gridsChecked, guidanceFieldBuffer, tileNumVisited, propagationRadius,
            correctionToPropagate, correctionGridsChecked, correctionField, correctionTileNumVisited, accessibility);
        PropagateVectorField(correctionToPropagate, correctionGridsChecked, correctionField,
                correctionTileNumVisited, Mathf.FloorToInt(0.5f * propagationRadius), accessibility);
        for (int i = 0; i < Resolution * Resolution; i++)
        {
            GuidanceField[i] = guidanceFieldBuffer[i] + correctionField[i];
        }
    }

    public Vector3[] GetPropagatedVelocityField(bool[] accessibility, int propagationRadius)
    {
        Vector3[] velocityField = new Vector3[Resolution * Resolution];

        int[] tileNumVisited = new int[Resolution * Resolution];
        List<int> gridsToPropagate = new();
        List<int> gridsChecked = new();
        List<int> correctionToPropagate = new();
        List<int> correctionGridsChecked = new();
        Vector3[] correctionField = new Vector3[Resolution * Resolution];
        int[] correctionTileNumVisited = new int[Resolution * Resolution];
        Vector3[] velocityFieldBuffer = new Vector3[Resolution * Resolution];

        for (int i = 0; i < Resolution * Resolution; i++)
        {
            if (AgentVelocityMean[i].sqrMagnitude > 1e-3)
            {
                velocityFieldBuffer[i] = AgentVelocityMean[i];
                tileNumVisited[i]++;
                if (!gridsChecked.Contains(i)) { gridsChecked.Add(i); }
                if (!gridsToPropagate.Contains(i)) { gridsToPropagate.Add(i); }
            }
        }

        PropagateVectorField(
            gridsToPropagate, gridsChecked, velocityFieldBuffer, tileNumVisited, propagationRadius,
            correctionToPropagate, correctionGridsChecked, correctionField, correctionTileNumVisited, accessibility);
        PropagateVectorField(correctionToPropagate, correctionGridsChecked, correctionField,
                correctionTileNumVisited, Mathf.FloorToInt(0.5f * propagationRadius), accessibility);
        for (int i = 0; i < Resolution * Resolution; i++)
        {
            velocityField[i] = velocityFieldBuffer[i] + correctionField[i];
        }

        return velocityField;
    }

    private void PropagateVectorField(
        List<int> gridsToPropagate, List<int> gridsChecked, Vector3[] guidanceField,
        int[] tileNumVisited, int propagationRadius, bool[] gridAccessibility)
    {
        if (propagationRadius > 1)
        {
            List<int> newGridsChecked = new();
            List<int> newGridToPropagate = new();
            foreach (int centerGridIndex in gridsToPropagate)
            {
                Vector2Int centerGridCoord = Helper.CellCoordFromIndex(centerGridIndex, Resolution);
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int neighborGridCoord = centerGridCoord + Helper.CellIterationSequence[i];
                    int neighborGridIndex = Helper.CellIndexFromCoord(neighborGridCoord, Resolution);
                    if (Helper.IsCoordWithinBoundary(neighborGridCoord, Resolution) &&
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
            PropagateVectorField(newGridToPropagate, gridsChecked, guidanceField, tileNumVisited, propagationRadius - 1, gridAccessibility);
        }
    }

    private void PropagateVectorField(
        List<int> gridsToPropagate, List<int> gridsChecked, Vector3[] guidanceField, int[] tileNumVisited, int propagationRadius,
        List<int> correctionToPropagate, List<int> correctionGridsChecked, Vector3[] correctionField, int[] correctionTileNumVisited,
        bool[] accessibility)
    {
        if (propagationRadius > 1)
        {
            List<int> newGridsChecked = new();
            List<int> newGridToPropagate = new();
            foreach (int centerGridIndex in gridsToPropagate)
            {
                Vector2Int centerGridCoord = Helper.CellCoordFromIndex(centerGridIndex, Resolution);
                for (int i = 0; i < 4; i++)
                {
                    Vector2Int neighborGridCoord = centerGridCoord + Helper.CellIterationSequence[i];
                    int neighborGridIndex = Helper.CellIndexFromCoord(neighborGridCoord, Resolution);
                    if (Helper.IsCoordWithinBoundary(neighborGridCoord, Resolution) && !gridsChecked.Contains(neighborGridIndex))
                    {
                        tileNumVisited[neighborGridIndex]++;
                        guidanceField[neighborGridIndex] = guidanceField[neighborGridIndex] +
                            (((float)propagationRadius - 1f) / (float)propagationRadius *
                            guidanceField[centerGridIndex] - guidanceField[neighborGridIndex]) / (float)tileNumVisited[neighborGridIndex];
                        if (!newGridsChecked.Contains(neighborGridIndex)) { newGridsChecked.Add(neighborGridIndex); }
                        if (!newGridToPropagate.Contains(neighborGridIndex)) { newGridToPropagate.Add(neighborGridIndex); }
                    }
                    if (Helper.IsCoordWithinBoundary(neighborGridCoord, Resolution) && !accessibility[neighborGridIndex])
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
            PropagateVectorField(
                newGridToPropagate, gridsChecked, guidanceField, tileNumVisited, propagationRadius - 1,
                correctionToPropagate, correctionGridsChecked, correctionField, correctionTileNumVisited,
                accessibility);
        }
    }
}