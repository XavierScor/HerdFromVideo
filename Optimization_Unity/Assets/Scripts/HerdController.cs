using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;
using UnityEngine.Experimental.Rendering;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;
using System.Linq;
using UnityEditor.Build.Content;
using System.Runtime.InteropServices;
using UnityEngine.UI;

struct ModelParameter
{
    public float sensitivity;
    public float perceptionRadius;

    public float avoidanceWeight;
    public float avoidanceRadialRange;
    public float avoidanceAngularRange;

    public float cohesionWeight;
    public float cohesionRadialRange;
    public float cohesionAngularRange;

    public float alignmentWeight;
    public float alignmentRadialRange;
    public float alignmentAngularRange;

    public float obstacleAvoidanceWeight;
    public float obstacleRadialRange;
    public float obstacleAngularRange;
}

struct AgentGPU
{
    public Vector3 position;
    public Vector3 forward;
    public Vector3 up;

    public float perceptionRadius;
    public float capsuleHeight;
    public float capsuleRadius;
}

struct Cube
{
    public Matrix4x4 worldToLocal;
    public Matrix4x4 localToWorld;
    public Vector3 halfSize;
};

public class HerdController : MonoBehaviour
{
    [SerializeField] private float SimulationDeltaTime;
    [SerializeField] private HerdAgent AgentPrefab;
    [SerializeField] private GameObject Terrain;
    [SerializeField] private GameObject CubeObstacles;
    [SerializeField] private GameObject Arrow;
    [SerializeField] private GameObject FrameCanvas;

    [HideInInspector] public List<HerdAgent> AgentList = new();
    private List<Transform> _obstacleList = new();
    private Cube[] _obstaclesGPU;

    [Header("Random Initialization")]
    [SerializeField] private bool IsRandomInitialized;
    [SerializeField] private float RespawnWidth;
    [SerializeField] private float RespawnMinGap;

    [Header("Movement Area Limitation")]
    [SerializeField] private bool IsMovementAreaLimited;
    [SerializeField] private float MovableWidth;
    [SerializeField] private float MovableLength;

    [Header("Vision Parameters")]
    [SerializeField] private bool IsPerceptionOccluded;
    [SerializeField] private float PerceptionRadius = 4.0f;

    //[Header("Field Propagation")]
    //[Range(0, 10)]
    //[SerializeField] private int propagationRadius = 3;

    [Header("Density Field Matching")]
    [Range(0, 1.0f)]
    [SerializeField] private float densityMatchingFactor = 1.0f;

    [Header("Compute Shader")]
    [SerializeField] private ComputeShader AgentVisibilityShader;
    [SerializeField] private int NumRay;
    private float[] _agentToAgentVisibility;
    private float[] _agentToObstacleDistance;
    private Vector3[] _agentRayObstacleIntersectionPos;

    [DllImport("HerdInteractionOptimizationPlugin.dll")]
    private static extern void TotalForceOnAgent(
        int numAgents, float[] originVelocity, float[] totalForce_out,
        int[] numNeighbors, float[] neighborDistance, float[] neighborBearingAngle, float[] neighborDirection, float[] alignmentDirection,
        int[] numObstacles, float[] obstacleDistance, float[] obstacleBearingAngle, float[] obstacleAvoidanceForceDirection,
        float[] parameters, float perceptionRadius);

    [DllImport("HerdInteractionOptimizationPlugin.dll")]
    private static extern void VelocityMatchGradient(
        int numAgents, float[] originVelocity, float[] targetVelocity, float deltaTime,
        int[] numNeighbors, float[] neighborDistance, float[] neighborBearingAngle, float[] neighborDirection, float[] alignmentDirection,
        int[] numObstacles, float[] obstacleDistance, float[] obstacleBearingAngle, float[] obstacleAvoidanceForceDirection,
        float[] parameters, float perceptionRadius, float[] gradient, float[] errorResult);

    [DllImport("HerdInteractionOptimizationPlugin.dll")]
    private static extern void DirectionMatchGradient(
        int numAgents, float[] originVelocity, float targetAngleVariance, float deltaTime,
        int[] numNeighbors, float[] neighborDistance, float[] neighborBearingAngle, float[] neighborDirection, float[] alignmentDirection,
        int[] numObstacles, float[] obstacleDistance, float[] obstacleBearingAngle, float[] obstacleAvoidanceForceDirection,
        float[] parameters, float perceptionRadius, float[] gradient, float[] errorResult);

    public void InitializeHerdController()
    {
        // NOTE: Initialize obstacle list first because agents
        //       inside obstacles should not be generated.
        InitializeObstacleList();
        Time.fixedDeltaTime = SimulationDeltaTime;
    }

    public void InitializeHerdAgent(List<Vector3> agentPos = null, List<Vector3> agentDirection = null, List<float> agentSpeed = null)
    {
        // === Clear agent list === 
        if (AgentList.Count != 0)
        {
            foreach (HerdAgent agent in AgentList)
            {
                Destroy(agent.gameObject);
            }
            AgentList.Clear();
        }

        if (IsRandomInitialized)
        {
            InitializeRandomAgent();
        }
        else if (agentPos != null && agentDirection != null && agentPos.Count == agentDirection.Count)
        {
            for (int i = 0; i < agentPos.Count; i++)
            {
                Vector3 pos = agentPos[i];
                Vector3 dir = agentDirection[i];

                bool isInsideObstacle = false;
                foreach (Transform obstacle in _obstacleList)
                {
                    if (obstacle.gameObject.GetComponent<Collider>().bounds.Contains(new Vector3(pos.x, 0, pos.z)))
                    {
                        isInsideObstacle = true;
                    }
                }
                if (!isInsideObstacle)
                {
                    Quaternion quaternion = Quaternion.FromToRotation(Vector3.forward, dir.normalized);
                    HerdAgent newAgent = Instantiate(AgentPrefab, pos, quaternion);
                    newAgent.SetTerrain(Terrain);
                    if (agentSpeed != null)
                        newAgent.SetSpeed(agentSpeed[i]);
                    newAgent.name = "Sheep Agent " + (AgentList.Count + 1);
                    newAgent.transform.parent = transform;
                    AgentList.Add(newAgent);
                }
            }
        }
    }

    private void InitializeRandomAgent()
    {
        // === Generate agents positions by Poisson disk sampler ===
        List<Vector2> agentsPos = PoissonDiskSampler.GeneratePoints(RespawnMinGap, RespawnWidth, RespawnWidth);

        // === Instantiate prefab to the generated positions ===
        foreach (Vector2 pos in agentsPos)
        {
            Vector3 newAgentPos = new Vector3(pos.x, 0, pos.y) + transform.position - new Vector3(RespawnWidth / 2.0f, 0, RespawnWidth / 2.0f);
            bool isInsideObstacle = false;
            foreach (Transform obstacle in _obstacleList)
            {
                if (obstacle.gameObject.GetComponent<Collider>().bounds.Contains(new Vector3(newAgentPos.x, 0, newAgentPos.z)))
                {
                    isInsideObstacle = true;
                }
            }
            if (!isInsideObstacle)
            {
                HerdAgent newAgent = Instantiate(AgentPrefab, newAgentPos, Quaternion.identity);
                //HerdAgent newAgent = Instantiate(AgentPrefab, newAgentPos, Quaternion.FromToRotation(Vector3.forward, Vector3.left));
                newAgent.SetTerrain(Terrain);
                newAgent.name = "Sheep Agent " + (AgentList.Count + 1);
                newAgent.transform.parent = transform;
                AgentList.Add(newAgent);
            }
        }
    }

    private void TorusSimulation()
    {
        // === Connect left and up boundaries to the right and bottom ===
        foreach (HerdAgent agent in AgentList)
        {
            float agentX = agent.transform.position.x;
            float agentZ = agent.transform.position.z;
            if (agentX - transform.position.x < -MovableLength / 2.0f && agent.GetVelocity().x < 0)
            {
                agentX = transform.position.x + MovableLength / 2.0f;
            }
            if (agentX - transform.position.x > MovableLength / 2.0f && agent.GetVelocity().x > 0)
            {
                agentX = transform.position.x - MovableLength / 2.0f;
            }
            if (agentZ - transform.position.z < -MovableWidth / 2.0f && agent.GetVelocity().z < 0)
            {
                agentZ = transform.position.z + MovableWidth / 2.0f;
            }
            if (agentZ - transform.position.z > MovableWidth / 2.0f && agent.GetVelocity().z > 0)
            {
                agentZ = transform.position.z - MovableWidth / 2.0f;
            }
            agent.transform.position = new Vector3(agentX, agent.transform.position.y, agentZ);
        }
    }

    private void ShowDebugVisual()
    {
        // === Agent related visual ===
        for (int i = 0; i < AgentList.Count; i++)
        {
            // === Draw connection line between agent and visible neighbors ===
            if (AgentList[i].IsShowVisibleNeighbors == true)
            {
                for (int j = 0; j < AgentList.Count; j++)
                {
                    if (i != j && _agentToAgentVisibility[i * AgentList.Count + j] > 0.5)
                    {
                        Debug.DrawLine(
                            AgentList[i].transform.position,
                            AgentList[j].transform.position,
                            Color.yellow
                            );
                    }
                }
            }

            // === Draw connection line between agent and visible obstacles ===
            if (AgentList[i].IsShowVisibleObstacles == true)
            { 
                for (int a = 0; a < 2 * NumRay; a++)
                {
                    if (_agentToObstacleDistance[i * (2 * NumRay) + a] > 0)
                    {
                        Debug.DrawLine(
                        AgentList[i].transform.position,
                        _agentRayObstacleIntersectionPos[i * (2 * NumRay) + a],
                        Color.red);
                    }
                    else
                    {
                        Debug.DrawLine(
                        AgentList[i].transform.position,
                        _agentRayObstacleIntersectionPos[i * (2 * NumRay) + a],
                        Color.cyan);
                    }
                }
            }
        }
    }

    private void InitializeObstacleList()
    {
        foreach (Transform obstacle in CubeObstacles.transform)
        {
            _obstacleList.Add(obstacle);
        }

        _obstaclesGPU = new Cube[_obstacleList.Count];
        for (int i = 0; i < _obstacleList.Count; i++)
        {
            _obstaclesGPU[i].worldToLocal = _obstacleList[i].worldToLocalMatrix;
            _obstaclesGPU[i].localToWorld = _obstacleList[i].localToWorldMatrix;
            _obstaclesGPU[i].halfSize = _obstacleList[i].gameObject.GetComponent<BoxCollider>().size / 2.0f;
        }
    }

    public void Simulate(List<int> agentIndices, Parameters parameters)
    {
        // NOTE: This function should be called outside once if agents are divided into sub-groups
        //UpdateAgentVisibility();

        // === Force on agent calculated by DLL ===
        float[] forceOnAgentByDLL = CalculateForceOnAgent(agentIndices, parameters);
        int totalNumAgent = AgentList.Count;

        for (int j = 0; j < agentIndices.Count; j++)
        {
            AgentList[agentIndices[j]].MoveByForce(new Vector3(
                parameters.GetSensitivity() * forceOnAgentByDLL[j * 3 + 0],
                parameters.GetSensitivity() * forceOnAgentByDLL[j * 3 + 1],
                parameters.GetSensitivity() * forceOnAgentByDLL[j * 3 + 2]
            ));
        }

        // === Replace agent if out of boundary ===
        if (IsMovementAreaLimited)
        {
            TorusSimulation();
        }

        // === Show debug visual if neccessary ===
        ShowDebugVisual();
    }

    public void Simulate(List<int> agentIndices, Parameters parameters, Vector3[] authoringField, int resolution, float groundSize, Vector3 groundCenter)
    {
        // NOTE: This function should be called outside once if agents are divided into sub-groups
        //UpdateAgentVisibility();

        // === Force on agent calculated by DLL ===
        float[] forceOnAgentByDLL = CalculateForceOnAgent(agentIndices, parameters);
        int totalNumAgent = AgentList.Count;

        for (int j = 0; j < agentIndices.Count; j++)
        {
            int numNeighborFront = 0;
            for (int k = 0; k < totalNumAgent; k++)
            {
                if (agentIndices[j] != k && _agentToAgentVisibility[agentIndices[j] * totalNumAgent + k] > 0.5f)
                {
                    Vector3 agentToNeighborVector = AgentList[k].transform.position - AgentList[agentIndices[j]].transform.position;
                    float bearingAngle = Vector3.Angle(AgentList[agentIndices[j]].transform.forward, agentToNeighborVector);
                    if (bearingAngle < 60.0f)
                        numNeighborFront++;
                }
            }

            Vector3 newForce = new Vector3(
                    parameters.GetSensitivity() * forceOnAgentByDLL[j * 3 + 0],
                    parameters.GetSensitivity() * forceOnAgentByDLL[j * 3 + 1],
                    parameters.GetSensitivity() * forceOnAgentByDLL[j * 3 + 2]
                );

            if (numNeighborFront < 2)
            {
                int cellIndex = Helper.CellIndexFromPosition(AgentList[agentIndices[j]].transform.position, resolution, groundSize, groundCenter);
                if (authoringField[cellIndex].magnitude > 1e-3f)
                    AgentList[agentIndices[j]].MoveByForce(10 * authoringField[cellIndex].normalized * (1.0f - numNeighborFront / 5.0f) + newForce * (numNeighborFront / 5.0f) );
                else
                    AgentList[agentIndices[j]].MoveByForce(newForce);
                //AgentList[agentIndices[j]].GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.red;
            }
            else
            {
                AgentList[agentIndices[j]].MoveByForce(newForce);
                //AgentList[agentIndices[j]].GetComponentInChildren<SkinnedMeshRenderer>().material.color = Color.white;
            }
        }

        // === Replace agent if out of boundary ===
        if (IsMovementAreaLimited)
        {
            TorusSimulation();
        }

        // === Show debug visual if neccessary ===
        ShowDebugVisual();
    }

    public void UpdateAgentVisibility()
    {
        int numAgent = AgentList.Count;
        if (numAgent <= 0)
            return;

        // === Prepare agent data and compute buffer === 
        AgentGPU[] agentsGPU = new AgentGPU[numAgent];
        for (int i = 0; i < numAgent; i++)
        {
            agentsGPU[i].position = AgentList[i].transform.position;
            agentsGPU[i].forward = AgentList[i].transform.forward;
            agentsGPU[i].up = AgentList[i].transform.up;

            agentsGPU[i].perceptionRadius = PerceptionRadius;
            agentsGPU[i].capsuleHeight = AgentList[i].GetComponentInChildren<CapsuleCollider>().height;
            agentsGPU[i].capsuleRadius = AgentList[i].GetComponentInChildren<CapsuleCollider>().radius;
        }
        ComputeBuffer agentBuffer = new(numAgent, sizeof(float) * 12);
        agentBuffer.SetData(agentsGPU);

        // === Prepare visibility data and compute buffer ===
        _agentToAgentVisibility = new float[numAgent * numAgent];
        ComputeBuffer visibilityBuffer = new(_agentToAgentVisibility.Length, sizeof(float));
        visibilityBuffer.SetData(_agentToAgentVisibility);

        // === Prepare obstacle ray tracing result container and compute buffer ===
        _agentToObstacleDistance = new float[32 * numAgent];
        for (int i = 0; i < _agentToObstacleDistance.Length; i++) { _agentToObstacleDistance[i] = -1; }
        ComputeBuffer obstacleDistanceBuffer = new(_agentToObstacleDistance.Length, sizeof(float));
        obstacleDistanceBuffer.SetData(_agentToObstacleDistance);

        _agentRayObstacleIntersectionPos = new Vector3[32 * numAgent];
        ComputeBuffer obstacleIntersectionPosBuffer = new(_agentRayObstacleIntersectionPos.Length, sizeof(float) * 3);
        obstacleIntersectionPosBuffer.SetData(_agentRayObstacleIntersectionPos);

        // === Prepare obstacle data and compute buffer ===
        for (int i = 0; i < _obstacleList.Count; i++)
        {
            _obstaclesGPU[i].worldToLocal = _obstacleList[i].worldToLocalMatrix;
            _obstaclesGPU[i].localToWorld = _obstacleList[i].localToWorldMatrix;
            _obstaclesGPU[i].halfSize = _obstacleList[i].gameObject.GetComponent<BoxCollider>().size / 2.0f;
        }
        ComputeBuffer obstacleBuffer = new(_obstaclesGPU.Length, sizeof(float) * 35);
        obstacleBuffer.SetData(_obstaclesGPU);

        // === Send data to GPU ===
        AgentVisibilityShader.SetBool("_IsPerceptionOccluded", IsPerceptionOccluded);
        AgentVisibilityShader.SetInt("_NumRay", NumRay);
        AgentVisibilityShader.SetBuffer(0, "_Agents", agentBuffer);
        AgentVisibilityShader.SetBuffer(0, "_Visibility", visibilityBuffer);
        AgentVisibilityShader.SetBuffer(0, "_ObstacleDistance", obstacleDistanceBuffer);
        AgentVisibilityShader.SetBuffer(0, "_ObstacleIntersectionPos", obstacleIntersectionPosBuffer);
        AgentVisibilityShader.SetBuffer(0, "_Cubes", obstacleBuffer);

        // === Simulate by compute shaders ===
        int threadGroupsX = Mathf.CeilToInt(numAgent / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(numAgent / 8.0f);
        AgentVisibilityShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // === Retrive data back to CPU ===
        visibilityBuffer.GetData(_agentToAgentVisibility);
        obstacleDistanceBuffer.GetData(_agentToObstacleDistance);
        obstacleIntersectionPosBuffer.GetData(_agentRayObstacleIntersectionPos);

        // === Release compute buffers ===
        agentBuffer.Release();
        visibilityBuffer.Release();
        obstacleDistanceBuffer.Release();
        obstacleIntersectionPosBuffer.Release();
        obstacleBuffer.Release();
    }

    public float[] ExtractSimulatedDensity(int resolution, float groundSize, Vector3 groundCenter)
    {
        float[] density = new float[resolution * resolution];
        float cellSize = groundSize / resolution;

        for (int i = 0; i < AgentList.Count; i++)
        {
            int cellIndex = Helper.CellIndexFromPosition(AgentList[i].transform.position, resolution, groundSize, groundCenter);
            if (Helper.IsCoordWithinBoundary(Helper.CellCoordFromIndex(cellIndex, resolution), resolution))
                density[cellIndex] += 1f / (cellSize * cellSize);
        }

        return density;
    }

    public Vector3[] ExtractDensityDifferenceFieldGradient(
        int resolution, float groundSize, Vector3 groundCenter,
        float[] simulatedField, float[] targetField, bool[] cellAccessibility)
    {
        float cellSize = groundSize / resolution;
        float[] densityDifferenceField = new float[resolution * resolution];
        for (int i = 0; i < resolution * resolution; i++)
        {
            densityDifferenceField[i] = simulatedField[i] - targetField[i];
        }
        Vector3[] densityDifferenceFieldGradient = new Vector3[resolution * resolution];
        for (int i = 1; i < resolution - 1; i++)
        {
            for (int j = 1; j < resolution - 1; j++)
            {
                int index = Helper.CellIndexFromCoord(new Vector2Int(i, j), resolution);
                if (cellAccessibility[index])
                {
                    int indexLeft = Helper.CellIndexFromCoord(new Vector2Int(i, j) + Vector2Int.left, resolution);
                    int indexRight = Helper.CellIndexFromCoord(new Vector2Int(i, j) + Vector2Int.right, resolution);
                    densityDifferenceFieldGradient[index].x = (densityDifferenceField[indexRight] - densityDifferenceField[indexLeft]) / (2.0f * cellSize);
                    int indexDown = Helper.CellIndexFromCoord(new Vector2Int(i, j) + Vector2Int.down, resolution);
                    int indexUp = Helper.CellIndexFromCoord(new Vector2Int(i, j) + Vector2Int.up, resolution);
                    densityDifferenceFieldGradient[index].z = (densityDifferenceField[indexUp] - densityDifferenceField[indexDown]) / (2.0f * cellSize);
                }
                else
                {
                    densityDifferenceFieldGradient[index] = Vector3.zero;
                }
            }
        }
        return densityDifferenceFieldGradient;
    }

    private float[] CalculateForceOnAgent(List<int> localAgentIndices, Parameters parameters)
    {
        List<HerdAgent> agentsWithNeighbors = new();

        List<float> originVelocity = new();
        List<float> totalForce = new();

        List<int> numNeighbors = new();
        List<float> neighborDistance = new();
        List<float> neighborBearingAngle = new();
        List<float> neighborDirection = new();
        List<float> alignmentDirection = new();

        List<int> numObstacles = new();
        List<float> obstacleDistance = new();
        List<float> obstacleBearingAngle = new();
        List<float> obstacleAvoidanceForceDirection = new();

        int totalNumAgent = AgentList.Count;
        int localNumAgent = localAgentIndices.Count;

        for (int i = 0; i < localNumAgent; i++)
        {
            // === Build data container for current agents ===
            agentsWithNeighbors.Add(AgentList[localAgentIndices[i]]);
            for (int e = 0; e < 3; e++)
            {
                originVelocity.Add(AgentList[localAgentIndices[i]].GetVelocity().normalized[e]);
                totalForce.Add(0);
            }

            // === Build data container for neighbor visible agents ===
            int currentNumNeighbors = 0;
            for (int j = 0; j < totalNumAgent; j++)
            {
                if (localAgentIndices[i] != j && _agentToAgentVisibility[localAgentIndices[i] * totalNumAgent + j] > 0.5)
                {
                    currentNumNeighbors++;
                    Vector3 agentToNeighborVector = AgentList[j].transform.position - AgentList[localAgentIndices[i]].transform.position;
                    Vector3 neighborDirectionVector = agentToNeighborVector.normalized;
                    neighborDistance.Add(agentToNeighborVector.magnitude);
                    neighborBearingAngle.Add(Vector3.Angle(AgentList[localAgentIndices[i]].transform.forward, agentToNeighborVector) / 180.0f);
                    for (int e = 0; e < 3; e++)
                    {
                        neighborDirection.Add(neighborDirectionVector[e]);
                        alignmentDirection.Add((AgentList[j].transform.forward)[e]);
                    }
                }
            }
            numNeighbors.Add(currentNumNeighbors);

            // === Build data container for neighbor visible obstacles ===
            int currentNumObstacles = 0;
            for (int j = 0; j < NumRay; j++)
            {
                float angle = (float)j / (NumRay - 1.0f);
                float distanceLeft = _agentToObstacleDistance[localAgentIndices[i] * (2 * NumRay) + 2 * j];
                float distanceRight = _agentToObstacleDistance[localAgentIndices[i] * (2 * NumRay) + 2 * j + 1];
                if (distanceLeft > 0.0f)
                {
                    currentNumObstacles++;
                    obstacleDistance.Add(distanceLeft);
                    obstacleBearingAngle.Add(angle);
                    for (int e = 0; e < 3; e++)
                    {
                        obstacleAvoidanceForceDirection.Add(AgentList[localAgentIndices[i]].transform.right[e]);
                    }
                }
                if (distanceRight > 0.0f)
                {
                    currentNumObstacles++;
                    obstacleDistance.Add(distanceRight);
                    obstacleBearingAngle.Add(angle);
                    for (int e = 0; e < 3; e++)
                    {
                        obstacleAvoidanceForceDirection.Add((-AgentList[localAgentIndices[i]].transform.right)[e]);
                    }
                }
            }
            numObstacles.Add(currentNumObstacles);
        }

        float[] totalForceArray = totalForce.ToArray();

        TotalForceOnAgent(
            agentsWithNeighbors.Count, originVelocity.ToArray(), totalForceArray,
            numNeighbors.ToArray(), neighborDistance.ToArray(), neighborBearingAngle.ToArray(), neighborDirection.ToArray(), alignmentDirection.ToArray(),
            numObstacles.ToArray(), obstacleDistance.ToArray(), obstacleBearingAngle.ToArray(), obstacleAvoidanceForceDirection.ToArray(),
            parameters.GetParameterArray(), PerceptionRadius);

        return totalForceArray;
    }

    public float[] VelocityMatchParametersGradient(List<int> localAgentIndices, Parameters parameters, float[] targetDensity, Vector3[] targetCellVelocity, DataReader dataReader)
    {
        float[] gradientArray = new float[12];
        float[] errorResult = new float[1];

        if (localAgentIndices.Count > 0)
        {
            List<HerdAgent> agentsWithNeighbors = new();

            List<float> originVelocity = new();
            List<float> targetVelocity = new();

            List<int> numNeighbors = new();
            List<float> neighborDistance = new();
            List<float> neighborBearingAngle = new();
            List<float> neighborDirection = new();
            List<float> alignmentDirection = new();

            List<int> numObstacles = new();
            List<float> obstacleDistance = new();
            List<float> obstacleBearingAngle = new();
            List<float> obstacleAvoidanceForceDirection = new();

            bool[] cellAccessibility = Helper.GetAccessibility(dataReader.Resolution, dataReader.GroundSize, dataReader.GroundCenter);
            float[] simulatedDensity = ExtractSimulatedDensity(dataReader.Resolution, dataReader.GroundSize, dataReader.GroundCenter);

            Vector3[] densityDifferenceFieldGradient = ExtractDensityDifferenceFieldGradient(
                dataReader.Resolution, dataReader.GroundSize, dataReader.GroundCenter, simulatedDensity, targetDensity, cellAccessibility);

            int totalNumAgent = AgentList.Count;
            int localNumAgent = localAgentIndices.Count;

            for (int i = 0; i < localNumAgent; i++)
            {
                int cellIndex = Helper.CellIndexFromPosition(AgentList[localAgentIndices[i]].transform.position, dataReader.Resolution, dataReader.GroundSize, dataReader.GroundCenter);
                if (Helper.IsCoordWithinBoundary(Helper.CellCoordFromIndex(cellIndex, dataReader.Resolution), dataReader.Resolution))
                {
                    // === Build data container for current agents ===
                    agentsWithNeighbors.Add(AgentList[localAgentIndices[i]]);
                    for (int e = 0; e < 3; e++)
                    {
                        originVelocity.Add(AgentList[localAgentIndices[i]].GetVelocity().normalized[e]);
                        targetVelocity.Add(
                            -densityMatchingFactor * densityDifferenceFieldGradient[cellIndex].normalized[e] + 
                            (1.0f - densityMatchingFactor) * targetCellVelocity[cellIndex].normalized[e]);
                    }

                    // === Build data container for neighbor visible agents ===
                    int currentNumNeighbors = 0;
                    for (int j = 0; j < totalNumAgent; j++)
                    {
                        if (localAgentIndices[i] != j && _agentToAgentVisibility[localAgentIndices[i] * totalNumAgent + j] > 0.5)
                        {
                            currentNumNeighbors++;
                            Vector3 agentToNeighborVector = AgentList[j].transform.position - AgentList[localAgentIndices[i]].transform.position;
                            Vector3 neighborDirectionVector = agentToNeighborVector.normalized;
                            neighborDistance.Add(agentToNeighborVector.magnitude);
                            neighborBearingAngle.Add(Vector3.Angle(AgentList[localAgentIndices[i]].transform.forward, agentToNeighborVector) / 180.0f);
                            for (int e = 0; e < 3; e++)
                            {
                                neighborDirection.Add(neighborDirectionVector[e]);
                                alignmentDirection.Add((AgentList[j].transform.forward)[e]);
                            }
                        }
                    }
                    numNeighbors.Add(currentNumNeighbors);

                    // === Build data container for neighbor visible obstacles ===
                    int currentNumObstacles = 0;
                    for (int j = 0; j < NumRay; j++)
                    {
                        float angle = (float)j / (NumRay - 1.0f);
                        float distanceLeft = _agentToObstacleDistance[localAgentIndices[i] * (2 * NumRay) + 2 * j];
                        float distanceRight = _agentToObstacleDistance[localAgentIndices[i] * (2 * NumRay) + 2 * j + 1];
                        if (distanceLeft > 0.0f)
                        {
                            currentNumObstacles++;
                            obstacleDistance.Add(distanceLeft);
                            obstacleBearingAngle.Add(angle);
                            for (int e = 0; e < 3; e++)
                            {
                                obstacleAvoidanceForceDirection.Add(AgentList[localAgentIndices[i]].transform.right[e]);
                            }
                        }
                        if (distanceRight > 0.0f)
                        {
                            currentNumObstacles++;
                            obstacleDistance.Add(distanceRight);
                            obstacleBearingAngle.Add(angle);
                            for (int e = 0; e < 3; e++)
                            {
                                obstacleAvoidanceForceDirection.Add((-AgentList[localAgentIndices[i]].transform.right)[e]);
                            }
                        }
                    }
                    numObstacles.Add(currentNumObstacles);
                }
            }

            VelocityMatchGradient(
                agentsWithNeighbors.Count, originVelocity.ToArray(), targetVelocity.ToArray(), Time.fixedDeltaTime,
                numNeighbors.ToArray(), neighborDistance.ToArray(), neighborBearingAngle.ToArray(), neighborDirection.ToArray(), alignmentDirection.ToArray(),
                numObstacles.ToArray(), obstacleDistance.ToArray(), obstacleBearingAngle.ToArray(), obstacleAvoidanceForceDirection.ToArray(),
                parameters.GetParameterArray(), PerceptionRadius, gradientArray, errorResult);
        }

        return gradientArray;
    }

    public float[] DirectionMatchParametersGradient(int cellIndex, Parameters parameters, float targetAngleVar, DataReader dataReader)
    {
        float[] gradientArray = new float[12];
        float[] errorResult = new float[1];

        int totalNumAgent = AgentList.Count;
        List<int> localAgentIndices = new();
        for (int j = 0; j < totalNumAgent; j++)
        {
            if (Helper.CellIndexFromPosition(AgentList[j].transform.position, dataReader.Resolution, dataReader.GroundSize, dataReader.GroundCenter) == cellIndex)
            {
                localAgentIndices.Add(j);
            }
        }

        int localNumAgent = localAgentIndices.Count;
        if (localNumAgent > 0)
        {
            List<HerdAgent> agentsWithNeighbors = new();

            List<float> originVelocity = new();

            List<int> numNeighbors = new();
            List<float> neighborDistance = new();
            List<float> neighborBearingAngle = new();
            List<float> neighborDirection = new();
            List<float> alignmentDirection = new();

            List<int> numObstacles = new();
            List<float> obstacleDistance = new();
            List<float> obstacleBearingAngle = new();
            List<float> obstacleAvoidanceForceDirection = new();

            for (int i = 0; i < localNumAgent; i++)
            {
                // === Build data container for current agents ===
                agentsWithNeighbors.Add(AgentList[localAgentIndices[i]]);
                for (int e = 0; e < 3; e++)
                {
                    originVelocity.Add(AgentList[localAgentIndices[i]].GetVelocity().normalized[e]);
                }

                // === Build data container for neighbor visible agents ===
                int currentNumNeighbors = 0;
                for (int j = 0; j < totalNumAgent; j++)
                {
                    if (localAgentIndices[i] != j && _agentToAgentVisibility[localAgentIndices[i] * totalNumAgent + j] > 0.5)
                    {
                        currentNumNeighbors++;
                        Vector3 agentToNeighborVector = AgentList[j].transform.position - AgentList[localAgentIndices[i]].transform.position;
                        Vector3 neighborDirectionVector = agentToNeighborVector.normalized;
                        neighborDistance.Add(agentToNeighborVector.magnitude);
                        neighborBearingAngle.Add(Vector3.Angle(AgentList[localAgentIndices[i]].transform.forward, agentToNeighborVector) / 180.0f);
                        for (int e = 0; e < 3; e++)
                        {
                            neighborDirection.Add(neighborDirectionVector[e]);
                            alignmentDirection.Add((AgentList[j].transform.forward)[e]);
                        }
                    }
                }
                numNeighbors.Add(currentNumNeighbors);

                // === Build data container for neighbor visible obstacles ===
                int currentNumObstacles = 0;
                for (int j = 0; j < NumRay; j++)
                {
                    float angle = (float)j / (NumRay - 1.0f);
                    float distanceLeft = _agentToObstacleDistance[localAgentIndices[i] * (2 * NumRay) + 2 * j];
                    float distanceRight = _agentToObstacleDistance[localAgentIndices[i] * (2 * NumRay) + 2 * j + 1];
                    if (distanceLeft > 0.0f)
                    {
                        currentNumObstacles++;
                        obstacleDistance.Add(distanceLeft);
                        obstacleBearingAngle.Add(angle);
                        for (int e = 0; e < 3; e++)
                        {
                            obstacleAvoidanceForceDirection.Add(AgentList[localAgentIndices[i]].transform.right[e]);
                        }
                    }
                    if (distanceRight > 0.0f)
                    {
                        currentNumObstacles++;
                        obstacleDistance.Add(distanceRight);
                        obstacleBearingAngle.Add(angle);
                        for (int e = 0; e < 3; e++)
                        {
                            obstacleAvoidanceForceDirection.Add((-AgentList[localAgentIndices[i]].transform.right)[e]);
                        }
                    }
                }
                numObstacles.Add(currentNumObstacles);
            }

            DirectionMatchGradient(
                agentsWithNeighbors.Count, originVelocity.ToArray(), targetAngleVar, Time.fixedDeltaTime,
                numNeighbors.ToArray(), neighborDistance.ToArray(), neighborBearingAngle.ToArray(), neighborDirection.ToArray(), alignmentDirection.ToArray(),
                numObstacles.ToArray(), obstacleDistance.ToArray(), obstacleBearingAngle.ToArray(), obstacleAvoidanceForceDirection.ToArray(),
                parameters.GetParameterArray(), PerceptionRadius, gradientArray, errorResult);
        }

        return gradientArray;
    }

    public float ErrorDensity(DataReader dataReader)
    {
        float error = 0;
        float[] simulatedDensityField = ExtractSimulatedDensity(dataReader.Resolution, dataReader.GroundSize, dataReader.GroundCenter);

        int nonEmptyCellNum = 0;
        for (int i = 0; i < simulatedDensityField.Length; i++)
        {
            if (simulatedDensityField[i] * dataReader.CellSize * dataReader.CellSize > 0.5f)
            {
                nonEmptyCellNum++;
                float targetDensity = dataReader.AgentNum[i] / (dataReader.CellSize * dataReader.CellSize);
                error += Mathf.Abs(targetDensity - simulatedDensityField[i]) / (targetDensity + 0.1f);
            }
        }

        if (nonEmptyCellNum == 0)
            return float.MaxValue;
        else
            return error / (float)nonEmptyCellNum;
    }

    public float ErrorVelocity(DataReader dataReader, Vector3[] targetCellVelocity)
    {
        float error = 0;
        int validAgentNum = 0;

        for (int i = 0; i < AgentList.Count; i++)
        {
            int cellIndex = Helper.CellIndexFromPosition(AgentList[i].transform.position, dataReader.Resolution, dataReader.GroundSize, dataReader.GroundCenter);
            if (Helper.IsCoordWithinBoundary(Helper.CellCoordFromIndex(cellIndex, dataReader.Resolution), dataReader.Resolution))
            {
                if (targetCellVelocity[cellIndex].sqrMagnitude > 1e-3)
                {
                    error += (AgentList[i].GetVelocity().normalized - targetCellVelocity[cellIndex].normalized).magnitude;
                    validAgentNum++;
                }
            }
            //if (targetCellVelocity[cellIndex].sqrMagnitude > 1e-3)
            //{
            //    error += (AgentList[i].GetVelocity().normalized - targetCellVelocity[cellIndex].normalized).magnitude;
            //    validAgentNum++;
            //}
        }

        if (validAgentNum == 0)
            return float.MaxValue;
        else
            return error / (float)validAgentNum;
    }

    public float[] ErrorPolarization(DataReader dataReader)
    {
        Vector3 velocitySum = Vector3.zero;
        int validAgentNum = 0;
        for (int i = 0; i < AgentList.Count; i++)
        {
            velocitySum += AgentList[i].GetVelocity().normalized;
            validAgentNum++;
        }
        float simulatedPolarization = velocitySum.magnitude / validAgentNum;

        float targetPolarization = dataReader.FramePolarization;

        return new float[] { targetPolarization, simulatedPolarization, Mathf.Abs(simulatedPolarization - targetPolarization) };
    }

    public float[] ErrorAngularMomentum(DataReader dataReader)
    {
        Vector3 angularMomentumSum = Vector3.zero;
        int agentCount = AgentList.Count;
        float simulatedAngularMomentum = 0f;

        if (agentCount > 0)
        {
            Vector3 totalPosition = Vector3.zero;
            foreach (var agent in AgentList)
            {
                totalPosition += agent.transform.position;
            }
            Vector3 centerOfMass = totalPosition / agentCount;

            for (int i = 0; i < agentCount; i++)
            {
                Vector3 velocity = AgentList[i].GetVelocity();
                Vector3 relativePos = AgentList[i].transform.position - centerOfMass;

                float denominator = velocity.magnitude * relativePos.magnitude + 1e-5f;

                Vector3 individualAngularMomentum = Vector3.Cross(velocity, relativePos);
                angularMomentumSum += individualAngularMomentum / denominator;
            }
            simulatedAngularMomentum = angularMomentumSum.magnitude / agentCount;
        }

        float targetMomentum = dataReader.FrameAngularMomentum;

        return new float[] { targetMomentum, simulatedAngularMomentum, Mathf.Abs(simulatedAngularMomentum - targetMomentum) };
    }
    public float[] ErrorAspectRatio(DataReader dataReader)
    {
        float simulatedAspectRatio = 1.0f; // Default value
        float targetAspectRatio = 1.0f;  // Default value

        // --- Calculate Simulated Aspect Ratio ---
        if (AgentList != null && AgentList.Count >= 2) // Need at least 2 agents to define an aspect ratio
        {
            // 1. Calculate Centroid for Simulation Agents
            Vector3 simCentroid = Vector3.zero;
            foreach (var agent in AgentList)
            {
                simCentroid += agent.transform.position;
            }
            simCentroid /= AgentList.Count;

            // 2. Calculate Average Velocity (Forward Direction) for Simulation Agents
            Vector3 simForwardDir = Vector3.zero;
            foreach (var agent in AgentList)
            {
                // Use normalized velocity to focus on direction
                // Add check if GetVelocity() can return zero vector before normalizing if necessary
                Vector3 agentVel = agent.GetVelocity();
                if (agentVel.sqrMagnitude > float.Epsilon)
                {
                    simForwardDir += agentVel.normalized;
                }
            }

            // Normalize the average direction, handle zero velocity
            if (simForwardDir.sqrMagnitude > float.Epsilon)
            {
                simForwardDir = simForwardDir.normalized;
            }
            else
            {
                // Fallback: If no average velocity, try using orientation based on positions (simplified: use world forward)
                // A more robust fallback might involve PCA on positions, but that adds complexity.
                simForwardDir = Vector3.forward; // Or Vector3.right, or a direction based on agent positions relative to centroid
            }

            // 3. Define Orthogonal Axes for Simulation
            Vector3 simWorldUp = Vector3.up;
            // Handle alignment issues with the chosen world up direction
            if (Mathf.Abs(Vector3.Dot(simForwardDir, simWorldUp)) > 0.99f)
            {
                simWorldUp = Vector3.right; // Use a different axis if forward is aligned with up
                if (Mathf.Abs(Vector3.Dot(simForwardDir, simWorldUp)) > 0.99f) // Check alignment with the alternative axis too
                {
                    simWorldUp = Vector3.forward; // If aligned with both up and right, use forward (implies forwardDir is likely world up or right)
                }
            }
            Vector3 simRightDir = Vector3.Cross(simWorldUp, simForwardDir).normalized;
            // If simRightDir becomes zero (simForwardDir parallel to simWorldUp), Cross might return zero. Re-check normalization safety.
            if (simRightDir.sqrMagnitude < float.Epsilon)
            {
                // This case means forward was parallel to worldUp AND worldRight/worldForward. Highly unlikely unless forward is zero.
                // If forwardDir was calculated correctly, this shouldn't happen often. Defaulting right direction.
                simRightDir = Vector3.right; // Or find *any* perpendicular vector
                if (Mathf.Abs(Vector3.Dot(simForwardDir, simRightDir)) > 0.99f) simRightDir = Vector3.forward; // Try another if still parallel
            }


            // 4. Calculate Extents for Simulation Agents
            float simMinFwd = float.MaxValue;
            float simMaxFwd = float.MinValue;
            float simMinRight = float.MaxValue;
            float simMaxRight = float.MinValue;

            foreach (var agent in AgentList)
            {
                Vector3 relativePos = agent.transform.position - simCentroid;
                float forwardProj = Vector3.Dot(relativePos, simForwardDir);
                float rightProj = Vector3.Dot(relativePos, simRightDir);

                if (forwardProj < simMinFwd) simMinFwd = forwardProj;
                if (forwardProj > simMaxFwd) simMaxFwd = forwardProj;
                if (rightProj < simMinRight) simMinRight = rightProj;
                if (rightProj > simMaxRight) simMaxRight = rightProj;
            }

            // 5. Calculate Simulated Aspect Ratio
            float simForwardExtent = simMaxFwd - simMinFwd;
            float simRightExtent = simMaxRight - simMinRight;

            if (simForwardExtent > 1e-6f) // Avoid division by zero / very small numbers
            {
                simulatedAspectRatio = simRightExtent / simForwardExtent;
            }
            else
            {
                // Handle degenerate case: formation has no length along forward direction
                simulatedAspectRatio = (simRightExtent > 1e-6f) ? float.PositiveInfinity : 1.0f; // If it has width, infinite aspect ratio, else 1 (a point)
            }
        }
        // Else: Keep default simulatedAspectRatio = 1.0f if less than 2 agents

        // --- Calculate Target Aspect Ratio ---
        List<Vector3> occupiedCellCenters = new List<Vector3>();
        Vector3 targetVelocitySum = Vector3.zero;
        int validVelocityCount = 0;

        // Collect occupied cell centers and sum target velocities
        for (int i = 0; i < dataReader.Resolution * dataReader.Resolution; i++)
        {
            if (dataReader.AgentNum[i] > 0)
            {
                Vector3 cellCenter = Helper.CellCenterFromIndex(i, dataReader.Resolution, dataReader.GroundSize, Vector3.zero);
                occupiedCellCenters.Add(cellCenter);

                // Assuming AgentVelocityMean length matches Resolution*Resolution and contains meaningful data
                if (i < dataReader.AgentVelocityMean.Length)
                {
                    Vector3 cellVel = dataReader.AgentVelocityMean[i];
                    if (cellVel.sqrMagnitude > float.Epsilon)
                    {
                        targetVelocitySum += cellVel.normalized; // Use normalized velocity for direction average
                        validVelocityCount++;
                    }
                }
            }
        }

        if (occupiedCellCenters.Count >= 2) // Need at least 2 occupied cells
        {
            // 1. Calculate Centroid for Target Cells
            Vector3 targetCentroid = Vector3.zero;
            foreach (var cellCenter in occupiedCellCenters)
            {
                targetCentroid += cellCenter;
            }
            targetCentroid /= occupiedCellCenters.Count;

            // 2. Calculate Average Velocity (Forward Direction) for Target
            Vector3 targetForwardDir = Vector3.zero;
            if (validVelocityCount > 0 && targetVelocitySum.sqrMagnitude > float.Epsilon)
            {
                targetForwardDir = targetVelocitySum.normalized;
            }
            else
            {
                // Fallback: If no average velocity from dataReader, use default or calculate from cell positions
                targetForwardDir = Vector3.forward; // Simple fallback
                                                    // Alternative: calculate orientation from occupiedCellCenters relative to targetCentroid (more complex)
            }


            // 3. Define Orthogonal Axes for Target
            Vector3 targetWorldUp = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(targetForwardDir, targetWorldUp)) > 0.99f)
            {
                targetWorldUp = Vector3.right;
                if (Mathf.Abs(Vector3.Dot(targetForwardDir, targetWorldUp)) > 0.99f)
                {
                    targetWorldUp = Vector3.forward;
                }
            }
            Vector3 targetRightDir = Vector3.Cross(targetWorldUp, targetForwardDir).normalized;
            if (targetRightDir.sqrMagnitude < float.Epsilon)
            {
                targetRightDir = Vector3.right; // Default fallback
                if (Mathf.Abs(Vector3.Dot(targetForwardDir, targetRightDir)) > 0.99f) targetRightDir = Vector3.forward;
            }


            // 4. Calculate Extents for Target Cells
            float targetMinFwd = float.MaxValue;
            float targetMaxFwd = float.MinValue;
            float targetMinRight = float.MaxValue;
            float targetMaxRight = float.MinValue;

            foreach (var cellCenter in occupiedCellCenters)
            {
                Vector3 relativePos = cellCenter - targetCentroid;
                float forwardProj = Vector3.Dot(relativePos, targetForwardDir);
                float rightProj = Vector3.Dot(relativePos, targetRightDir);

                if (forwardProj < targetMinFwd) targetMinFwd = forwardProj;
                if (forwardProj > targetMaxFwd) targetMaxFwd = forwardProj;
                if (rightProj < targetMinRight) targetMinRight = rightProj;
                if (rightProj > targetMaxRight) targetMaxRight = rightProj;
            }

            // 5. Calculate Target Aspect Ratio
            float targetForwardExtent = targetMaxFwd - targetMinFwd;
            float targetRightExtent = targetMaxRight - targetMinRight;

            if (targetForwardExtent > 1e-6f)
            {
                targetAspectRatio = targetRightExtent / targetForwardExtent;
            }
            else
            {
                targetAspectRatio = (targetRightExtent > 1e-6f) ? float.PositiveInfinity : 1.0f;
            }
        }
        // Else: Keep default targetAspectRatio = 1.0f if less than 2 occupied cells

        //Debug.Log("Aspect Ratio: target = " + targetAspectRatio + "; simulated = " + simulatedAspectRatio);

        // Ensure neither aspect ratio is NaN or excessively large if that's undesirable
        if (float.IsNaN(targetAspectRatio) || float.IsInfinity(targetAspectRatio)) targetAspectRatio = 1.0f; // Or some large capped value
        if (float.IsNaN(simulatedAspectRatio) || float.IsInfinity(simulatedAspectRatio)) simulatedAspectRatio = 1.0f; // Or some large capped value


        return new float[] { targetAspectRatio, simulatedAspectRatio, Mathf.Abs(targetAspectRatio - simulatedAspectRatio) };
    }

    public void ReplaceAgentOutside(DataReader dataReader, float frameWidth, float frameHeight, bool isReplenished)
    {
        int resolution = dataReader.Resolution;
        float groundSize = dataReader.GroundSize;
        float cellSize = groundSize / (float)resolution;

        float scale = groundSize / Mathf.Max(frameWidth, frameHeight);

        int numAgentToAdd = 0;
        int numAgent = AgentList.Count;
        for (int i = 0; i < numAgent; i++)
        {
            if (frameWidth > frameHeight)
            {
                if (AgentList[i].transform.position.x < 0 || AgentList[i].transform.position.z < 0.5f * (groundSize - scale * frameHeight) ||
                AgentList[i].transform.position.x > scale * frameWidth || AgentList[i].transform.position.z > 0.5f * (groundSize + scale * frameHeight))
                {
                    Destroy(AgentList[i].gameObject);
                    AgentList.RemoveAt(i);
                    i--;
                    numAgent--;
                    numAgentToAdd++;
                }
            }
            else
            {
                if (AgentList[i].transform.position.x < 0.5f * (groundSize - scale * frameWidth) || AgentList[i].transform.position.z < 0 ||
                AgentList[i].transform.position.x > 0.5f * (groundSize + scale * frameWidth) || AgentList[i].transform.position.z > scale * frameHeight)
                {
                    Destroy(AgentList[i].gameObject);
                    AgentList.RemoveAt(i);
                    i--;
                    numAgent--;
                    numAgentToAdd++;
                }
            }
        }

        if (isReplenished)
        {
            float[] simulatedDensity = ExtractSimulatedDensity(dataReader.Resolution, dataReader.GroundSize, dataReader.GroundCenter);
            List<int> gridIndexToBeFilled = new();

            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    int gridIndex = Helper.CellIndexFromCoord(new Vector2Int(i, j), resolution);
                    Vector3 gridCenter = Helper.CellCenterFromIndex(gridIndex, resolution, groundSize, dataReader.GroundCenter);
                    int simulatedAgentNum = Mathf.FloorToInt(simulatedDensity[gridIndex] * dataReader.CellSize * dataReader.CellSize);
                    if (dataReader.AgentNum[gridIndex] > simulatedAgentNum)
                    {
                        gridIndexToBeFilled.Add(gridIndex);
                    }
                }
            }


            while (numAgentToAdd > 0 && gridIndexToBeFilled.Count > 0)
            {
                int randomIndex = Random.Range(0, gridIndexToBeFilled.Count - 1);
                int simulatedAgentNum = Mathf.FloorToInt(simulatedDensity[gridIndexToBeFilled[randomIndex]] * dataReader.CellSize * dataReader.CellSize);
                for (int i = 0; i < Mathf.Min(dataReader.AgentNum[gridIndexToBeFilled[randomIndex]] - simulatedAgentNum, numAgentToAdd); i++)
                {
                    numAgentToAdd--;
                    int cellIndex = gridIndexToBeFilled[randomIndex];
                    Vector3 cellCenter = Helper.CellCenterFromIndex(cellIndex, resolution, groundSize, dataReader.GroundCenter);
                    float velocity_x = Helper.GenerateNormalRandom(dataReader.AgentVelocityXMean[cellIndex], dataReader.AgentVelocityXVar[cellIndex]);
                    float velocity_y = Helper.GenerateNormalRandom(dataReader.AgentVelocityYMean[cellIndex], dataReader.AgentVelocityYVar[cellIndex]);
                    Vector3 agentVelocity = new(velocity_x, 0.0f, velocity_y);

                    Vector3 respawnGridPos = new(Random.Range(-0.5f, 0.5f) * cellSize, 0, Random.Range(-0.5f, 0.5f) * cellSize);
                    Quaternion quaternion = Quaternion.FromToRotation(Vector3.forward, agentVelocity.normalized);
                    HerdAgent newAgent = Instantiate(AgentPrefab, respawnGridPos + cellCenter, quaternion);
                    newAgent.SetTerrain(Terrain);
                    newAgent.name = "Sheep Agent " + (AgentList.Count + 1);
                    newAgent.transform.parent = transform;
                    AgentList.Add(newAgent);
                }
                gridIndexToBeFilled.RemoveAt(randomIndex);
            }
        }
    }

    public void ReplaceAgentOutside(DataReader dataReader, float frameWidth, float frameHeight, bool isReplenished, int replenishType)
    {
        int resolution = dataReader.Resolution;
        float groundSize = dataReader.GroundSize;
        float cellSize = groundSize / (float)resolution;

        float scale = groundSize / Mathf.Max(frameWidth, frameHeight);

        int numAgentToAdd = 0;
        int numAgent = AgentList.Count;
        for (int i = 0; i < numAgent; i++)
        {
            if (replenishType == 1)
            {
                if (frameWidth > frameHeight)
                {
                    if (AgentList[i].transform.position.x < 0 || AgentList[i].transform.position.z < 0.5f * (groundSize - scale * frameHeight) ||
                    AgentList[i].transform.position.z > 0.5f * (groundSize + scale * frameHeight))
                    {
                        Destroy(AgentList[i].gameObject);
                        AgentList.RemoveAt(i);
                        i--;
                        numAgent--;
                        numAgentToAdd++;
                    }
                }
                else
                {
                    if (AgentList[i].transform.position.x < 0.5f * (groundSize - scale * frameWidth) || AgentList[i].transform.position.z < 0 ||
                    AgentList[i].transform.position.z > scale * frameHeight)
                    {
                        Destroy(AgentList[i].gameObject);
                        AgentList.RemoveAt(i);
                        i--;
                        numAgent--;
                        numAgentToAdd++;
                    }
                }
            }
        }

        if (isReplenished)
        {
            if (replenishType == 1)
            {
                // === Replenish for sheep-narrow ===
                int maxNumAgentsInCell = 0;
                Vector3 averageVelocity = Vector3.zero;
                for (int i = 0; i < dataReader.Resolution * dataReader.Resolution; i++)
                {
                    if (dataReader.AgentNum[i] > maxNumAgentsInCell) { maxNumAgentsInCell = dataReader.AgentNum[i]; }
                    averageVelocity += dataReader.AgentVelocityMean[i];
                }
                averageVelocity = averageVelocity.normalized * dataReader.OverallAverageSpeed;

                int numAgentsOutsideRight = 0;
                for (int i = 0; i < AgentList.Count; i++)
                {
                    if (AgentList[i].transform.position.x > groundSize)
                    {
                        numAgentsOutsideRight++;
                    }
                }

                for (int i = 0; i < maxNumAgentsInCell * 4 - numAgentsOutsideRight; i++)
                {
                    float x = Random.Range(-0.5f * cellSize, 0.5f * cellSize);
                    float y = Random.Range(-1.5f * cellSize, 1.5f * cellSize);

                    int rightMiddleCellIndex = Helper.CellIndexFromCoord(new(resolution - 1, Mathf.FloorToInt(resolution * 0.5f) - 1), resolution);

                    float velocity_x = Helper.GenerateNormalRandom(averageVelocity.x, dataReader.AgentVelocityXVar[rightMiddleCellIndex]);
                    float velocity_y = Helper.GenerateNormalRandom(averageVelocity.y, dataReader.AgentVelocityYVar[rightMiddleCellIndex]);
                    Vector3 agentVelocity = new(velocity_x, 0.0f, velocity_y);

                    Vector3 rightMiddleCellCenter = Helper.CellCenterFromIndex(rightMiddleCellIndex, resolution, groundSize, dataReader.GroundCenter);
                    Vector3 respawnCenter = rightMiddleCellCenter + new Vector3(2 * dataReader.CellSize, 0, 0);
                    Quaternion quaternion = Quaternion.FromToRotation(Vector3.forward, agentVelocity.normalized);
                    HerdAgent newAgent = Instantiate(AgentPrefab, respawnCenter + new Vector3(x, 0, y), quaternion);
                    newAgent.SetTerrain(Terrain);
                    newAgent.name = "Sheep Agent " + (AgentList.Count + 1);
                    newAgent.transform.parent = transform;
                    AgentList.Add(newAgent);
                }
            }
        }
    }
}
