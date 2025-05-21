using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DemoSeals : MonoBehaviour
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
    [SerializeField] private bool IsShowGrid = false;

    private DataReader _dataReader;

    private int _frameCountOverall = 0;
    private int _frameCountAfterReset = 0;

    private void Awake()
    {
        // === Read meta data ===
        _dataReader = new();
        _dataReader.LoadStaticData("seal_data/04_20_19_45"); // 20x20
        Debug.Log("Resolution: " + _dataReader.Resolution + "; GroundSize: " + _dataReader.GroundSize);

        // === Show current frame ===
        Texture2D frameTexture = _dataReader.LoadFrameTexture("seal_frame", 1);
        float scale = _dataReader.GroundSize / Mathf.Max(FrameWidth, FrameHeight);
        FrameCanvas.transform.localScale = new Vector3(FrameWidth * scale, FrameHeight * scale, 1f);
        FrameCanvas.transform.position = new Vector3(_dataReader.GroundSize / 2.0f, 0.01f, _dataReader.GroundSize / 2.0f);
        FrameCanvas.GetComponent<Renderer>().material.SetTexture("_MainTex", frameTexture);

        // === Read first frame ===
        _dataReader.LoadFrameData(0);
        List<Vector3> pos = new();
        List<Vector3> dir = new();
        Random.InitState(777);
        float respawnSize = _dataReader.GroundSize / _dataReader.Resolution / 2.0f;
        for (int i = 0; i < _dataReader.Resolution * _dataReader.Resolution; i++)
        {
            Vector3 gridCenter = Helper.CellCenterFromIndex(i, _dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);
            for (int j = 0; j < _dataReader.AgentNum[i]; j++)
            {
                pos.Add(gridCenter + new Vector3(Random.Range(-respawnSize, respawnSize), 0, Random.Range(-respawnSize, respawnSize)));
                Vector2 buffer = Random.insideUnitCircle;
                dir.Add(new Vector3(buffer.x, 0, buffer.y));
            }
        }

        //// === Read first frame ===
        //_dataReader.LoadFrameData(0);
        //List<Vector3> pos = new();
        //List<Vector3> dir = new();
        //Random.InitState(777);

        //int totalNumAgent = 0;
        //for (int i = 0; i < _dataReader.Resolution * _dataReader.Resolution; i++)
        //{
        //    totalNumAgent += _dataReader.AgentNum[i];
        //}

        //float respawnSize = _dataReader.GroundSize / _dataReader.Resolution / 2.0f;
        //for (int i = 0; i < totalNumAgent; i++)
        //{
        //    int randomGrid = Random.Range(0, _dataReader.Resolution * _dataReader.Resolution);
        //    Vector3 gridCenter = Helper.CellCenterFromIndex(randomGrid, _dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);
        //    pos.Add(gridCenter + new Vector3(Random.Range(-respawnSize, respawnSize), 0, Random.Range(-respawnSize, respawnSize)));
        //    Vector2 buffer = Random.insideUnitCircle;
        //    dir.Add(new Vector3(buffer.x, 0, buffer.y));
        //}

        // === Initialize herd controller ===
        Herd.InitializeHerdController();
        Herd.InitializeHerdAgent(pos, dir);

        for (int agentIndex = 0; agentIndex < Herd.AgentList.Count; agentIndex++)
        {
            Herd.AgentList[agentIndex].SetMaxLinearSpeed(1.0f);
            Herd.AgentList[agentIndex].SetDrag(1.2f);
        }

        if (IsOptimize)
        {
            // === Initialize parameter sets ===
            GameObject parameterSetsObject = new("Parameters Container");
            parameterSetsObject.transform.parent = transform;

            ParameterSets = new Parameters[3];

            GameObject leftParameterObject = new("Left Parameters");
            leftParameterObject.AddComponent<Parameters>();
            leftParameterObject.transform.parent = parameterSetsObject.transform;
            ParameterSets[0] = leftParameterObject.GetComponent<Parameters>();
            ParameterSets[0].SetParameterArray(new float[] { 0.25f, 0.7f, 1, 0.15f, 1, 0.5f, 0.15f, 1, 0.5f, 0.3f, 0.3f, 0.5f });
            ParameterSets[0].SetSensitivity(0.3f);

            GameObject middleParameterObject = new("Middle Parameters");
            middleParameterObject.AddComponent<Parameters>();
            middleParameterObject.transform.parent = parameterSetsObject.transform;
            ParameterSets[1] = middleParameterObject.GetComponent<Parameters>();
            ParameterSets[1].SetParameterArray(new float[] { 0.25f, 0.7f, 1, 0.15f, 1, 0.5f, 0.15f, 1, 0.5f, 0.3f, 0.3f, 0.5f });
            ParameterSets[1].SetSensitivity(0.3f);

            GameObject rightParameterObject = new("Right Parameters");
            rightParameterObject.AddComponent<Parameters>();
            rightParameterObject.transform.parent = parameterSetsObject.transform;
            ParameterSets[2] = rightParameterObject.GetComponent<Parameters>();
            ParameterSets[2].SetParameterArray(new float[] { 0.25f, 0.7f, 1, 0.15f, 1, 0.5f, 0.15f, 1, 0.5f, 0.3f, 0.3f, 0.5f });
            ParameterSets[2].SetSensitivity(0.3f);
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
            _errorLogWriter.WriteLine("Frame" + " " + "DensityError");
        }
    }

    private void FixedUpdate()
    {
        if (IsShowGrid)
        {
            // === Draw grid lines ===
            for (int x = 0; x < _dataReader.Resolution + 1; x++)
            {
                for (int y = 0; y < _dataReader.Resolution + 1; y++)
                {
                    Debug.DrawLine(new Vector3(x * _dataReader.CellSize, 1.0f, 0.0f), new Vector3(x * _dataReader.CellSize, 1.0f, _dataReader.GroundSize));
                    Debug.DrawLine(new Vector3(0.0f, 1.0f, x * _dataReader.CellSize), new Vector3(_dataReader.GroundSize, 1.0f, x * _dataReader.CellSize));
                }
            }
        }

        float densityError = Herd.ErrorDensity(_dataReader);
        _frameCountOverall++;

        // === If density error is too high, re-initialize agent positions === 
        if (densityError > DensityErrorThreshold && IsOptimize)
        {
            List<Vector3> pos = new();
            List<Vector3> dir = new();
            float respawnSize = _dataReader.GroundSize / _dataReader.Resolution / 2.0f;
            for (int i = 0; i < _dataReader.Resolution * _dataReader.Resolution; i++)
            {
                Vector3 gridCenter = Helper.CellCenterFromIndex(i, _dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);
                for (int j = 0; j < _dataReader.AgentNum[i]; j++)
                {
                    pos.Add(gridCenter + new Vector3(Random.Range(-respawnSize, respawnSize), 0, Random.Range(-respawnSize, respawnSize)));
                    Vector2 buffer = Random.insideUnitCircle;
                    dir.Add(new Vector3(buffer.x, 0, buffer.y));
                }
            }

            // === Initialize herd controller ===
            Herd.InitializeHerdAgent(pos, dir);

            Debug.Log("Reset after " + _frameCountAfterReset + " frames");
            _frameCountAfterReset = 0;
        }
        else
        {
            _frameCountAfterReset++;
        }

        // === Group agents and simulate for one frame ===
        for (int agentIndex = 0; agentIndex < Herd.AgentList.Count; agentIndex++)
        {
            if (Herd.AgentList[agentIndex].transform.position.x < 27)
            {
                Herd.AgentList[agentIndex].SetMaxLinearSpeed(3.0f);
                Herd.AgentList[agentIndex].SetDrag(0.0f);
            }
            else
            {
                Herd.AgentList[agentIndex].SetMaxLinearSpeed(0.5f);
                Herd.AgentList[agentIndex].SetDrag(1.0f);
            }
        }

        List<int> leftAgentIndices = new();
        List<int> middleAgentIndices = new();
        List<int> rightAgentIndices = new();

        for (int i = 0; i < Herd.AgentList.Count; i++)
        {
            if (Herd.AgentList[i].transform.position.x < 27)
            {
                leftAgentIndices.Add(i);
            }
            else if (Herd.AgentList[i].transform.position.x < 45)
            {
                middleAgentIndices.Add(i);
            }
            else
            {
                rightAgentIndices.Add(i);
            }
        }

        Herd.UpdateAgentVisibility();

        if (!IsOptimize && _frameCountOverall <= MaxSimulationSteps || IsOptimize)
        {
            Herd.Simulate(leftAgentIndices, ParameterSets[0]);
            Herd.Simulate(middleAgentIndices, ParameterSets[1]);
            Herd.Simulate(rightAgentIndices, ParameterSets[2]);
            //Debug.Log("Frame: " + _frameCountOverall + "; Density Error: " + densityError.ToString("F4"));
            if (IsLogError)
                _errorLogWriter.WriteLine(_frameCountOverall + " " + densityError.ToString("F4"));
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

        // === Optimize agent parameters based on density difference ===
        if (IsOptimize && _frameCountOverall < MaxOptimizationSteps)
        {
            float cellArea = (_dataReader.GroundSize / _dataReader.Resolution) * (_dataReader.GroundSize / _dataReader.Resolution);
            float[] targetDensity = new float[_dataReader.AgentNum.Length];
            Vector3[] targetCellVelocity = new Vector3[_dataReader.AgentNum.Length];

            for (int i = 0; i < targetDensity.Length; i++)
            {
                targetDensity[i] = _dataReader.AgentNum[i] / cellArea;
            }

            float[] leftGradient = Helper.NormalizeArray(Herd.VelocityMatchParametersGradient(leftAgentIndices, ParameterSets[0], targetDensity, targetCellVelocity, _dataReader));
            float[] middleGradient = Helper.NormalizeArray(Herd.VelocityMatchParametersGradient(middleAgentIndices, ParameterSets[1], targetDensity, targetCellVelocity, _dataReader));
            float[] rightGradient = Helper.NormalizeArray(Herd.VelocityMatchParametersGradient(rightAgentIndices, ParameterSets[2], targetDensity, targetCellVelocity, _dataReader));

            if (IsMatchDirectionSTD)
            {
                for (int i = 0; i < _dataReader.Resolution * _dataReader.Resolution; i++)
                {
                    Vector3 cellCenter = Helper.CellCenterFromIndex(i, _dataReader.Resolution, _dataReader.GroundSize, _dataReader.GroundCenter);
                    if (cellCenter.x < 27)
                    {
                        float[] varGradient = Helper.NormalizeArray(Herd.DirectionMatchParametersGradient(i, ParameterSets[0], 3000, _dataReader));
                        for (int g = 6; g < 9; g++)
                            leftGradient[g] += varGradient[g];
                    }
                    else if (cellCenter.x < 45)
                    {
                        float[] varGradient = Helper.NormalizeArray(Herd.DirectionMatchParametersGradient(i, ParameterSets[1], 3000, _dataReader));
                        for (int g = 6; g < 9; g++)
                            middleGradient[g] += varGradient[g];
                    }
                    else
                    {
                        float[] varGradient = Helper.NormalizeArray(Herd.DirectionMatchParametersGradient(i, ParameterSets[2], 3000, _dataReader));
                        for (int g = 6; g < 9; g++)
                            rightGradient[g] += varGradient[g];
                    }
                }
            }

            float[] stepSize = new float[leftGradient.Length];
            for (int i = 0; i < stepSize.Length; i++)
                stepSize[i] = OptimizationStep;

            for (int i = 9; i < 12; i++)
            {
                leftGradient[i] = 0;
                middleGradient[i] = 0;
                rightGradient[i] = 0;
            }

            ParameterSets[0].UpdateParameters(stepSize, leftGradient);
            ParameterSets[1].UpdateParameters(stepSize, middleGradient);
            ParameterSets[2].UpdateParameters(stepSize, rightGradient);
        }
        
        if (IsOptimize && _frameCountOverall == MaxOptimizationSteps)
        {
            Debug.Log("Maximum optimization steps reached");
        }
    }
}
