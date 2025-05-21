using PathCreation;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DemoSheepWide : MonoBehaviour
{
    [SerializeField] private HerdController Herd;

    [Header("Parameter Container")]
    [SerializeField] private bool IsParametersGrided = false;
    [SerializeField] private GameObject ParameterContainer;
    [SerializeField] private Parameters[] ParameterSets;

    [Header("Optimization")]
    [SerializeField] private bool IsOptimize = false;
    [SerializeField] private bool IsMatchDirectionSTD = false;
    [SerializeField] private float OptimizationStep = -0.01f;
    [SerializeField] private float DensityErrorThreshold = 0.10f;
    [SerializeField] private int MaxOptimizationSteps = 400;
    [SerializeField] private int ParameterResX = 1;
    [SerializeField] private int ParameterResY = 1;

    [Header("Log Error")]
    [SerializeField] private bool IsLogError = false;
    [SerializeField] private int SimulationStartFrame = 100;
    [SerializeField] private int MaxSimulationSteps = 100;
    private StreamWriter _errorLogWriter;

    [Header("Canvas")]
    [SerializeField] private GameObject FrameCanvas;
    [SerializeField] private int FrameWidth;
    [SerializeField] private int FrameHeight;
    [SerializeField] private bool IsShowGuidanceField = false;
    [SerializeField] private GameObject Arrow;
    private List<GameObject> _arrowList;
    private GameObject _arrowObjectContainer;

    [Header("Moving Obstacles")]
    [SerializeField] private GameObject[] Obstacles;
    [SerializeField] private PathCreator[] ObstaclePaths;

    private DataReader _dataReader;

    private int _frameCountOverall = 0;
    private int _frameCountVideo = 0;
    private int _frameCountAfterReset = 0;

    private bool[] _accessibility;

    private void Awake()
    {
        // === Read meta data ===
        _dataReader = new();
        _dataReader.LoadVideoData("sheep_wide_data/04_21_10_20"); // 20 body length 80x80
        Debug.Log("Resolution: " + _dataReader.Resolution + "; GroundSize: " + _dataReader.GroundSize);

        // === Get accessibility info ===
        _accessibility = new bool[_dataReader.Resolution * _dataReader.Resolution];
        _accessibility = Helper.GetAccessibility(_dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);

        _dataReader.LoadSpeedVSDensityData();
        //_dataReader.LoadCellData(false);      // Low guidance data not filtered (birds, noises by color change)
        _dataReader.LoadCellData(true, 0.2f);   // Low guidance data filtered
        _dataReader.PropagateGuidanceField(_accessibility, 10);

        // === Show current frame ===
        Texture2D frameTexture = _dataReader.LoadFrameTexture("sheep_wide_frame", 1);
        float scale = _dataReader.GroundSize / Mathf.Max(FrameWidth, FrameHeight);
        FrameCanvas.transform.localScale = new Vector3(FrameWidth * scale, FrameHeight * scale, 1f);
        FrameCanvas.transform.position = new Vector3(_dataReader.GroundSize / 2.0f, 0.01f, _dataReader.GroundSize / 2.0f);
        FrameCanvas.GetComponent<Renderer>().material.SetTexture("_MainTex", frameTexture);
        _arrowList = new();
        _arrowObjectContainer = new("Arrow Container");
        _arrowObjectContainer.transform.parent = transform;
        _arrowObjectContainer.transform.position = Vector3.zero;

        // === Show guidance field ===
        if (IsShowGuidanceField)
        {
            ShowVectorField(_dataReader.GuidanceField);
            Debug.Break();
        }

        // === Seed the random generator ===
        Random.InitState(777);

        // === Initialize herd controller ===
        Herd.InitializeHerdController();

        if (IsOptimize)
        {
            // === Initialize parameter sets ===
            GameObject parameterSetsObject = new("Parameters Container");
            parameterSetsObject.transform.parent = transform;

            if (IsParametersGrided)
            {
                ParameterSets = new Parameters[ParameterResX * ParameterResY];
                for (int x = 0; x < ParameterResX; x++)
                {
                    for (int y = 0; y < ParameterResY; y++)
                    {
                        GameObject parameterObject = new("Parameters " + x + " " + y);
                        parameterObject.AddComponent<Parameters>();
                        parameterObject.transform.parent = parameterSetsObject.transform;
                        ParameterSets[y * ParameterResX + x] = parameterObject.GetComponent<Parameters>();
                    }
                }
            }
            else
            {
                ParameterSets = new Parameters[1];

                GameObject parameterObject = new("Parameters");
                parameterObject.AddComponent<Parameters>();
                parameterObject.transform.parent = parameterSetsObject.transform;
                ParameterSets[0] = parameterObject.GetComponent<Parameters>();
                ParameterSets[0].SetParameterArray(new float[] { 1, 1, 1, 0.1f, 1, 1, 0.1f, 1, 1, 0.15f, 1, 0.5f });
            }
        }
        else
        {
            if (IsParametersGrided)
            {
                ParameterSets = new Parameters[ParameterResX * ParameterResY];
                for (int i = 0; i < ParameterContainer.transform.childCount; i++)
                {
                    Parameters parameters = ParameterContainer.transform.GetChild(i).GetComponent<Parameters>();
                    if (parameters != null)
                    {
                        string[] objectNameEntries = ParameterContainer.transform.GetChild(i).name.Split(' ');
                        int x = int.Parse(objectNameEntries[1]);
                        int y = int.Parse(objectNameEntries[2]);
                        ParameterSets[y * ParameterResX + x] = parameters;
                    }
                }
            }
        }

        if (IsLogError)
        {
            System.DateTime fileCreateTime = System.DateTime.Now;
            string fileName = "Assets/Data/ErrorLog" + fileCreateTime.ToString("_HH_mm_ss") + ".txt";
            if (File.Exists(fileName))
            {
                Debug.Log(fileName + " already exists.");
                return;
            }
            _errorLogWriter = File.CreateText(fileName);
            _errorLogWriter.WriteLine(
                "Frame" + " " +
                "DensityError" + " " +
                "VelocityError" + " " +

                "TargetPolarization" + " " +
                "SimulatedPolarization" + " " +
                "PolarizationError" + " " +

                "TargetAngularMomentum" + " " +
                "SimulatedAngularMomentum" + " " +
                "AngularMomentumError" + " " +

                "TargetAspectRatio" + " " +
                "SimulatedAspectRatio" + " " +
                "AspectRatioError"
                );
        }
    }

    private void FixedUpdate()
    {
        //DrawGrid();
        //LoopResetAgentEachFrame();

        UpdateMovingObstacles(_frameCountVideo, _dataReader.NumEntriesFrameData);

        Herd.ReplaceAgentOutside(_dataReader, FrameWidth, FrameHeight, true);

        float densityError = Herd.ErrorDensity(_dataReader);
        Vector3[] targetCellVelocity = _dataReader.GetPropagatedVelocityField(_accessibility, 10);
        float velocityError = Herd.ErrorVelocity(_dataReader, targetCellVelocity);
        float[] polarizationError = Herd.ErrorPolarization(_dataReader);
        float[] angularMomentumError = Herd.ErrorAngularMomentum(_dataReader);
        float[] aspectRatioError = Herd.ErrorAspectRatio(_dataReader);
        if (!IsOptimize && _frameCountOverall <= MaxSimulationSteps && _frameCountOverall > 0)
        {
            Debug.Log("Frame: " + _frameCountOverall +
                "; Density Error: " + densityError.ToString("F4") +
                "; Velocity Error: " + velocityError.ToString("F4") +
                "; Polarization Error: " + polarizationError[2].ToString("F4") +
                "; Angular Momentum Error: " + angularMomentumError[2].ToString("F4") +
                "; Aspect Ratio Error: " + aspectRatioError[2].ToString("F4"));

            if (IsLogError)
                _errorLogWriter.WriteLine(
                    _frameCountOverall + " " +
                    densityError.ToString("F4") + " " +
                    velocityError.ToString("F4") + " " +

                    polarizationError[0].ToString("F4") + " " +
                    polarizationError[1].ToString("F4") + " " +
                    polarizationError[2].ToString("F4") + " " +

                    angularMomentumError[0].ToString("F4") + " " +
                    angularMomentumError[1].ToString("F4") + " " +
                    angularMomentumError[2].ToString("F4") + " " +

                    aspectRatioError[0].ToString("F4") + " " +
                    aspectRatioError[1].ToString("F4") + " " +
                    aspectRatioError[2].ToString("F4"));
        }

        if (_frameCountVideo >= _dataReader.NumEntriesFrameData)
        {
            _frameCountVideo = 0;
        }
        if (!IsOptimize && _frameCountOverall == 0)
            _frameCountVideo = SimulationStartFrame;

        _dataReader.LoadFrameData(_frameCountVideo);
        Texture2D frameTexture = _dataReader.LoadFrameTexture("sheep_wide_frame", _dataReader.VideoFrameIndex);
        FrameCanvas.GetComponent<Renderer>().material.SetTexture("_MainTex", frameTexture);

        if (densityError > DensityErrorThreshold && IsOptimize || _frameCountOverall == 0)
        {
            Debug.Log("Reset after " + _frameCountAfterReset + " frames");
            _frameCountAfterReset = 0;

            List<Vector3> pos = new();
            List<Vector3> dir = new();
            List<float> spd = new();
            float respawnSize = _dataReader.GroundSize / _dataReader.Resolution / 2.0f;
            for (int i = 0; i < _dataReader.Resolution * _dataReader.Resolution; i++)
            {
                Vector3 gridCenter = Helper.CellCenterFromIndex(i, _dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);
                for (int j = 0; j < _dataReader.AgentNum[i]; j++)
                {
                    pos.Add(gridCenter + new Vector3(Random.Range(-respawnSize, respawnSize), 0, Random.Range(-respawnSize, respawnSize)));
                    dir.Add(new Vector3(
                        Helper.GenerateNormalRandom(_dataReader.AgentVelocityXMean[i], _dataReader.AgentVelocityXVar[i]),
                        0,
                        Helper.GenerateNormalRandom(_dataReader.AgentVelocityYMean[i], _dataReader.AgentVelocityYVar[i])));
                    spd.Add(_dataReader.AgentVelocityMean[i].magnitude * _dataReader.FrameTime / Time.fixedDeltaTime);
                }
            }
            Herd.InitializeHerdAgent(pos, dir, spd);
        }
        else
        {
            _frameCountVideo++;
            _frameCountAfterReset++;
        }

        // === Group agents and simulate for one frame ===
        if (IsParametersGrided)
        {
            DrawGrid(ParameterResX, ParameterResY);
            List<int>[] agentIndicesArray = new List<int>[ParameterResX * ParameterResY];
            for (int i = 0; i < ParameterResX * ParameterResY; i++)
                agentIndicesArray[i] = new();

            for (int agentIndex = 0; agentIndex < Herd.AgentList.Count; agentIndex++)
            {
                float agentX = Herd.AgentList[agentIndex].transform.position.x;
                float agentY = Herd.AgentList[agentIndex].transform.position.z;
                float parameterCellWidth = _dataReader.GroundSize / ParameterResX;
                float parameterCellHeight = _dataReader.GroundSize / ParameterResY;

                agentIndicesArray[
                    ParameterResX * Mathf.FloorToInt(agentY / parameterCellHeight) +
                    Mathf.FloorToInt(agentX / parameterCellWidth)].Add(agentIndex);
            }

            SetAgentMaxSpeed();

            if (!IsOptimize && _frameCountOverall <= MaxSimulationSteps || IsOptimize)
            {
                Herd.UpdateAgentVisibility();
                for (int i = 0; i < ParameterResX * ParameterResY; i++)
                    Herd.Simulate(agentIndicesArray[i], ParameterSets[i], _dataReader.GuidanceField, _dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);
            }

            if (!IsOptimize && _frameCountOverall == MaxSimulationSteps + 1)
            {
                if (IsLogError)
                {
                    _errorLogWriter.Close();
                    Debug.Log("Error log finished");
                }
                Debug.Break();
            }

            if (IsOptimize && _frameCountOverall < MaxOptimizationSteps)
            {
                float cellArea = (_dataReader.GroundSize / _dataReader.Resolution) * (_dataReader.GroundSize / _dataReader.Resolution);
                float[] targetDensity = new float[_dataReader.AgentNum.Length];
                for (int i = 0; i < targetDensity.Length; i++)
                {
                    targetDensity[i] = _dataReader.AgentNum[i] / cellArea;
                }

                List<float[]> gradientList = new();
                for (int i = 0; i < ParameterResX * ParameterResY; i++)
                    gradientList.Add(
                        Helper.NormalizeArray(
                            Herd.VelocityMatchParametersGradient(
                                agentIndicesArray[i], ParameterSets[i], targetDensity, targetCellVelocity, _dataReader)
                            )
                        );

                float[] stepSize = new float[gradientList[0].Length];
                for (int i = 0; i < stepSize.Length; i++)
                    stepSize[i] = OptimizationStep;


                for (int i = 0; i < ParameterResX * ParameterResY; i++)
                    ParameterSets[i].UpdateParameters(stepSize, gradientList[i]);
            }
        }
        else
        {
            List<int> agentIndices = new();

            for (int agentIndex = 0; agentIndex < Herd.AgentList.Count; agentIndex++)
            {
                agentIndices.Add(agentIndex);
            }

            SetAgentMaxSpeed();

            if (!IsOptimize && _frameCountOverall <= MaxSimulationSteps || IsOptimize)
            {
                Herd.UpdateAgentVisibility();
                Herd.Simulate(agentIndices, ParameterSets[0], _dataReader.GuidanceField, _dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);
            }

            if (!IsOptimize && _frameCountOverall == MaxSimulationSteps + 1)
            {
                if (IsLogError)
                {
                    _errorLogWriter.Close();
                    Debug.Log("Error log finished");
                }
                Debug.Break();
            }

            if (IsOptimize && _frameCountOverall < MaxOptimizationSteps)
            {
                float cellArea = (_dataReader.GroundSize / _dataReader.Resolution) * (_dataReader.GroundSize / _dataReader.Resolution);
                float[] targetDensity = new float[_dataReader.AgentNum.Length];
                for (int i = 0; i < targetDensity.Length; i++)
                {
                    targetDensity[i] = _dataReader.AgentNum[i] / cellArea;
                }

                float[] gradient = Helper.NormalizeArray(Herd.VelocityMatchParametersGradient(agentIndices, ParameterSets[0], targetDensity, targetCellVelocity, _dataReader));

                if (IsMatchDirectionSTD)
                {
                    for (int i = 0; i < _dataReader.Resolution * _dataReader.Resolution; i++)
                    {
                        float[] varMatchGradient = Helper.NormalizeArray(Herd.DirectionMatchParametersGradient(i, ParameterSets[0], _dataReader.AgentAngleVar[i], _dataReader));
                        for (int g = 6; g < 9; g++)
                            gradient[g] += varMatchGradient[g];
                    }
                }

                float[] stepSize = new float[gradient.Length];
                for (int i = 0; i < stepSize.Length; i++)
                    stepSize[i] = OptimizationStep;

                ParameterSets[0].UpdateParameters(stepSize, gradient);
            }
        }

        if (IsOptimize && _frameCountOverall == MaxOptimizationSteps)
        {
            Debug.Log("Maximum optimization steps reached");
        }

        _frameCountOverall++;
    }

    private void DrawGrid()
    {
        // === Draw grid lines ===
        for (int x = 0; x < _dataReader.Resolution + 1; x++)
        {
            for (int y = 0; y < _dataReader.Resolution + 1; y++)
            {
                Debug.DrawLine(
                    new Vector3(x * _dataReader.CellSize, 1.0f, 0.0f),
                    new Vector3(x * _dataReader.CellSize, 1.0f, _dataReader.GroundSize),
                    new Color(1.0f, 0.4f, 0.2f));
                Debug.DrawLine(
                    new Vector3(0.0f, 1.0f, x * _dataReader.CellSize),
                    new Vector3(_dataReader.GroundSize, 1.0f, x * _dataReader.CellSize),
                    new Color(1.0f, 0.4f, 0.2f));
            }
        }
    }

    private void DrawGrid(int resX, int resY)
    {
        float width = _dataReader.GroundSize / resX;
        float height = _dataReader.GroundSize / resY;
        // === Draw grid lines ===
        for (int x = 0; x < resX + 1; x++)
        {
            for (int y = 0; y < resY + 1; y++)
            {
                Debug.DrawLine(
                    new Vector3(x * width, 1.0f, 0.0f),
                    new Vector3(x * width, 1.0f, _dataReader.GroundSize),
                    new Color(1.0f, 0.4f, 0.2f));
                Debug.DrawLine(
                    new Vector3(0.0f, 1.0f, y * height),
                    new Vector3(_dataReader.GroundSize, 1.0f, y * height),
                    new Color(1.0f, 0.4f, 0.2f));
            }
        }
    }

    private void SetAgentMaxSpeed()
    {
        //// === Set agent max speed according to local density of the cell ===
        //List<int>[] agentInCell = new List<int>[_dataReader.Resolution * _dataReader.Resolution];
        //for (int i = 0; i < agentInCell.Length; i++)
        //{
        //    agentInCell[i] = new List<int>();
        //}

        //for (int agentIndex = 0; agentIndex < Herd.AgentList.Count; agentIndex++)
        //{
        //    int cellIndex = Helper.CellIndexFromPosition(
        //        Herd.AgentList[agentIndex].transform.position, _dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);
        //    agentInCell[cellIndex].Add(agentIndex);
        //}

        //for (int cellIndex = 0; cellIndex < _dataReader.Resolution * _dataReader.Resolution; cellIndex++)
        //{
        //    float maxCellSpeed;
        //    if (agentInCell[cellIndex].Count > _dataReader.SpeedWhenAgentNum.Count - 1)
        //    {
        //        maxCellSpeed = _dataReader.SpeedWhenAgentNum[_dataReader.SpeedWhenAgentNum.Count - 1];
        //    }
        //    else
        //    {
        //        maxCellSpeed = _dataReader.SpeedWhenAgentNum[agentInCell[cellIndex].Count];
        //    }

        //    for (int agentIndexInCell = 0; agentIndexInCell < agentInCell[cellIndex].Count; agentIndexInCell++)
        //    {
        //        Herd.AgentList[agentInCell[cellIndex][agentIndexInCell]].SetMaxLinearSpeed(maxCellSpeed * _dataReader.FrameTime / Time.fixedDeltaTime);
        //        //Herd.AgentList[agentInCell[cellIndex][agentIndexInCell]].SetMaxLinearSpeed(_dataReader.OverallAverageSpeed * _dataReader.FrameTime / Time.fixedDeltaTime);
        //    }
        //}

        // === Set agent max speed to overall average speed of the clip ===
        for (int agentIndex = 0; agentIndex < Herd.AgentList.Count; agentIndex++)
        {
            Herd.AgentList[agentIndex].SetMaxLinearSpeed(_dataReader.OverallAverageSpeed * _dataReader.FrameTime / Time.fixedDeltaTime);
        }

        //// === Set agent max speed to the average speed extracted at this frame ===
        //for (int agentIndex = 0; agentIndex < Herd.AgentList.Count; agentIndex++)
        //{
        //    int cellIndex = Helper.CellIndexFromPosition(
        //        Herd.AgentList[agentIndex].transform.position, _dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);
        //    if (_dataReader.AgentNum[cellIndex] > 0)
        //        Herd.AgentList[agentIndex].SetMaxLinearSpeed(_dataReader.AgentVelocityMean[cellIndex].magnitude * _dataReader.FrameTime / Time.fixedDeltaTime);
        //    else
        //        Herd.AgentList[agentIndex].SetMaxLinearSpeed(_dataReader.OverallAverageSpeed * _dataReader.FrameTime / Time.fixedDeltaTime);
        //}
    }

    private void LoopResetAgentEachFrame()
    {
        if (_frameCountVideo < _dataReader.NumEntriesFrameData)
        {
            _dataReader.LoadFrameData(_frameCountVideo);

            Texture2D frameTexture = _dataReader.LoadFrameTexture("sheep_wide_frame", _dataReader.VideoFrameIndex);
            FrameCanvas.GetComponent<Renderer>().material.SetTexture("_MainTex", frameTexture);

            //ShowVectorField(_dataReader.GetPropagatedVelocityField(_accessibility, 10));

            List<Vector3> pos = new();
            List<Vector3> dir = new();
            float respawnSize = _dataReader.GroundSize / _dataReader.Resolution / 2.0f;
            for (int i = 0; i < _dataReader.Resolution * _dataReader.Resolution; i++)
            {
                Vector3 gridCenter = Helper.CellCenterFromIndex(i, _dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);
                for (int j = 0; j < _dataReader.AgentNum[i]; j++)
                {
                    pos.Add(gridCenter + new Vector3(Random.Range(-respawnSize, respawnSize), 0, Random.Range(-respawnSize, respawnSize)));
                    dir.Add(new Vector3(
                        Helper.GenerateNormalRandom(_dataReader.AgentVelocityXMean[i], _dataReader.AgentVelocityXVar[i]),
                        0,
                        Helper.GenerateNormalRandom(_dataReader.AgentVelocityYMean[i], _dataReader.AgentVelocityYVar[i])));
                }
            }
            Herd.InitializeHerdAgent(pos, dir);

            _frameCountVideo++;
        }
        else
        {
            _frameCountVideo = 0;
        }
    }

    private void ShowVectorField(Vector3[] vectorField)
    {
        foreach (GameObject arrow in _arrowList)
        {
            Destroy(arrow);
        }
        _arrowList.Clear();

        for (int i = 0; i < _dataReader.Resolution * _dataReader.Resolution; i++)
        {
            Vector3 cellCenter = Helper.CellCenterFromIndex(i, _dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);
            cellCenter += Vector3.up;
            Quaternion quaternion = Quaternion.FromToRotation(Vector3.forward, vectorField[i]);
            GameObject arrow = Instantiate(Arrow, cellCenter, quaternion);
            _arrowList.Add(arrow);
            arrow.transform.parent = _arrowObjectContainer.transform;
            if (vectorField[i].magnitude < 1e-3f)
                arrow.transform.localScale = Vector3.zero;
            else
                arrow.transform.localScale = Vector3.one;
        }
    }

    private void UpdateMovingObstacles(int currentFrame, int totalFrames)
    {
        for (int i = 0; i < Obstacles.Length; i++)
        {
            float framePerSegment = (float)totalFrames / ObstaclePaths[i].bezierPath.NumSegments;
            int numPassedSegments = Mathf.FloorToInt((float)currentFrame / framePerSegment);
            float ratioPassedInCurrent = (currentFrame - numPassedSegments * framePerSegment) / framePerSegment;

            Vector3[] pointsInCurrentSegment = ObstaclePaths[i].bezierPath.GetPointsInSegment(numPassedSegments);
            Vector3 obstaclePos = pointsInCurrentSegment[Mathf.FloorToInt(ratioPassedInCurrent * pointsInCurrentSegment.Length)];
            Obstacles[i].transform.position = new Vector3(obstaclePos.x, Obstacles[i].transform.position.y, obstaclePos.z);
        }
    }
}
