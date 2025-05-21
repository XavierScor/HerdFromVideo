using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DemoSheepNarrow : MonoBehaviour
{
    [SerializeField] private HerdController Herd;

    [Header("Parameter Container")]
    [SerializeField] private Parameters[] ParameterSets;

    [Header("Optimization")]
    [SerializeField] private bool IsOptimize = false;
    [SerializeField] private bool IsMatchDirectionSTD = false;
    [SerializeField] private float OptimizationStep = -0.01f;
    [SerializeField] private float DensityErrorThreshold = 0.10f;
    [SerializeField] private int MaxOptimizationSteps = 400;

    [Header("Log Error")]
    [SerializeField] private bool IsLogError = false;
    [SerializeField] private int MaxSimulationSteps = 100;
    private StreamWriter _errorLogWriter;

    [Header("Canvas")]
    [SerializeField] private GameObject FrameCanvas;
    [SerializeField] private int FrameWidth;
    [SerializeField] private int FrameHeight;
    [SerializeField] private GameObject Arrow;
    private List<GameObject> _arrowList;
    private GameObject _arrowObjectContainer;

    private DataReader _dataReader;

    private int _frameCountOverall = 0;
    private int _frameCountAfterReset = 0;

    private bool[] _accessibility;

    private void Awake()
    {
        // === Read meta data ===
        _dataReader = new();
        _dataReader.LoadVideoData("sheep_narrow_data/04_20_21_12"); // 14 body length 20x20
        Debug.Log("Resolution: " + _dataReader.Resolution + "; GroundSize: " + _dataReader.GroundSize);

        // === Get accessibility info ===
        _accessibility = new bool[_dataReader.Resolution * _dataReader.Resolution];
        _accessibility = Helper.GetAccessibility(_dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);

        _dataReader.LoadSpeedVSDensityData();
        //_dataReader.LoadCellData(false);      // Low guidance data not filtered (birds, noises by color change)
        _dataReader.LoadCellData(true, 0.2f);   // Low guidance data filtered
        _dataReader.PropagateGuidanceField(_accessibility, 10);

        // === Show current frame ===
        Texture2D frameTexture = _dataReader.LoadFrameTexture("sheep_narrow_frame", 1);
        float scale = _dataReader.GroundSize / Mathf.Max(FrameWidth, FrameHeight);
        // FrameCanvas.transform.localScale = new Vector3(FrameWidth * scale, FrameHeight * scale, 1f);
        // FrameCanvas.transform.position = new Vector3(_dataReader.GroundSize / 2.0f, 0.01f, _dataReader.GroundSize / 2.0f);
        // FrameCanvas.GetComponent<Renderer>().material.SetTexture("_MainTex", frameTexture);
        _arrowList = new();
        _arrowObjectContainer = new("Arrow Container");
        _arrowObjectContainer.transform.parent = transform;
        _arrowObjectContainer.transform.position = Vector3.zero;

        // === Show guidance field ===
        //ShowVectorField(_dataReader.GuidanceField);

        // === Seed the random generator ===
        Random.InitState(777);

        // === Initialize herd controller ===
        Herd.InitializeHerdController();

        if (IsOptimize)
        {
            // === Initialize parameter sets ===
            GameObject parameterSetsObject = new("Parameters Container");
            parameterSetsObject.transform.parent = transform;

            ParameterSets = new Parameters[1];

            GameObject parameterObject = new("Parameters");
            parameterObject.AddComponent<Parameters>();
            parameterObject.transform.parent = parameterSetsObject.transform;
            ParameterSets[0] = parameterObject.GetComponent<Parameters>();
            ParameterSets[0].SetParameterArray(new float[] { 1, 1, 1, 0.1f, 1, 1, 0.1f, 1, 1, 0.15f, 1, 0.5f });
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

        if (IsOptimize || IsLogError)
            Herd.ReplaceAgentOutside(_dataReader, FrameWidth, FrameHeight, true);
        else
            Herd.ReplaceAgentOutside(_dataReader, FrameWidth, FrameHeight, true, 1);

        float densityError = Herd.ErrorDensity(_dataReader);

        _dataReader.LoadFrameData(_frameCountAfterReset);
        Texture2D frameTexture = _dataReader.LoadFrameTexture("sheep_narrow_frame", _dataReader.VideoFrameIndex);
        // FrameCanvas.GetComponent<Renderer>().material.SetTexture("_MainTex", frameTexture);

        if (densityError > DensityErrorThreshold && IsOptimize || _frameCountAfterReset >= _dataReader.NumEntriesFrameData || _frameCountOverall == 0)
        {
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

            Debug.Log("Reset after " + _frameCountAfterReset + " frames");
            _frameCountAfterReset = 0;
        }
        else
        {
            _frameCountAfterReset++;
        }

        // === Group agents and simulate for one frame ===
        List<int> agentIndices = new();

        for (int agentIndex = 0; agentIndex < Herd.AgentList.Count; agentIndex++)
        {
            agentIndices.Add(agentIndex);
        }

        SetAgentMaxSpeedByDensity();

        if (!IsOptimize && _frameCountOverall <= MaxSimulationSteps || IsOptimize)
        {
            Herd.UpdateAgentVisibility();
            Herd.Simulate(agentIndices, ParameterSets[0]);
        }

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

        if (!IsOptimize && _frameCountOverall == MaxSimulationSteps + 1)
        {
            if (IsLogError)
            {
                _errorLogWriter.Close();
                Debug.Log("Error log finished");
                Debug.Break();
            }
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

    private void SetAgentMaxSpeedByDensity()
    {
        for (int agentIndex = 0; agentIndex < Herd.AgentList.Count; agentIndex++)
        {
            Herd.AgentList[agentIndex].SetMaxLinearSpeed(_dataReader.OverallAverageSpeed * _dataReader.FrameTime / Time.fixedDeltaTime);
        }
    }

    private void LoopResetAgentEachFrame()
    {
        if (_frameCountAfterReset < _dataReader.NumEntriesFrameData)
        {
            _dataReader.LoadFrameData(_frameCountAfterReset);

            Texture2D frameTexture = _dataReader.LoadFrameTexture("sheep_narrow_frame", _dataReader.VideoFrameIndex);
            // FrameCanvas.GetComponent<Renderer>().material.SetTexture("_MainTex", frameTexture);

            ShowVectorField(_dataReader.GetPropagatedVelocityField(_accessibility, 10));

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

            _frameCountAfterReset++;
        }
        else
        {
            _frameCountAfterReset = 0;
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
}
