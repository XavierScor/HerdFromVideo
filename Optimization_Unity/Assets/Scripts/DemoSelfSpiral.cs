using PathCreation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoSelfSpiral : MonoBehaviour
{
    [SerializeField] private HerdController Herd;

    [Header("Parameter List")]
    [SerializeField] private Parameters[] ParameterSets;

    [Header("Navigation")]
    [SerializeField] private bool IsAuthored = false;
    [SerializeField] private PathCreator[] guidanceLines;
    [SerializeField] private Navigation navigation;

    [Header("Canvas")]
    [SerializeField] private GameObject Arrow;
    private List<GameObject> _arrowList;
    private GameObject _arrowObjectContainer;

    private int Resolution = 80;
    private float GroundSize = 80;
    private Vector3 GroundCenter = new(40, 0, 40);

    private void Awake()
    {
        Herd.InitializeHerdController();
        Herd.InitializeHerdAgent();

        _arrowList = new();
        _arrowObjectContainer = new("Arrow Container");
        _arrowObjectContainer.transform.parent = transform;
        _arrowObjectContainer.transform.position = Vector3.zero;

        navigation.Initialize(Resolution, GroundSize, GroundCenter);
        navigation.UpdateNavigationField(guidanceLines, new int[] { 5 }, Helper.GetAccessibility(Resolution, GroundSize, GroundCenter));
        navigation.TurnOnGuidanceLines(guidanceLines, 100, 10, 0.1f);
    }

    private void FixedUpdate()
    {
        //ShowVectorField(navigation.navigationField);

        //List<int> agentIndices = new();
        //for (int i = 0; i < Herd.AgentList.Count; i++)
        //{
        //    agentIndices.Add(i);
        //}
        //for (int agentIndex = 0; agentIndex < Herd.AgentList.Count; agentIndex++)
        //{
        //    Herd.AgentList[agentIndex].SetMaxLinearSpeed(3.0f);
        //}
        //Herd.UpdateAgentVisibility();
        //if (IsAuthored)
        //    Herd.Simulate(agentIndices, ParameterSets[0], navigation.navigationField, Resolution, GroundSize, GroundCenter);
        //else
        //    Herd.Simulate(agentIndices, ParameterSets[0]);


        List<int> agentIndicesGround = new();
        List<int> agentIndicesWater = new();

        for (int i = 0; i < Herd.AgentList.Count; i++)
        {
            if (Herd.AgentList[i].transform.position.x < 40f)
                agentIndicesWater.Add(i);
            else
                agentIndicesGround.Add(i);
        }
        for (int agentIndex = 0; agentIndex < Herd.AgentList.Count; agentIndex++)
        {
            Herd.AgentList[agentIndex].SetMaxLinearSpeed(3.0f);
        }
        Herd.UpdateAgentVisibility();
        if (IsAuthored)
        {
            Herd.Simulate(agentIndicesGround, ParameterSets[0], navigation.navigationField, Resolution, GroundSize, GroundCenter);
            Herd.Simulate(agentIndicesWater, ParameterSets[1], navigation.navigationField, Resolution, GroundSize, GroundCenter);
        }
        else
        {
            Herd.Simulate(agentIndicesGround, ParameterSets[0]);
            Herd.Simulate(agentIndicesWater, ParameterSets[1]);
        }
    }

    private void ShowVectorField(Vector3[] vectorField)
    {
        foreach (GameObject arrow in _arrowList)
        {
            Destroy(arrow);
        }
        _arrowList.Clear();

        for (int i = 0; i < Resolution * Resolution; i++)
        {
            Vector3 cellCenter = Helper.CellCenterFromIndex(i, Resolution, GroundSize, GroundCenter);
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
