using System.Collections.Generic;
using UnityEngine;

struct Capsule
{
    public Vector3 centerA;
    public Vector3 centerB;
    public float radius;
}

public class VisionTracing : MonoBehaviour
{
    //public ComputeShader RayTracingShader;

    //[SerializeField] private GameObject CubeObstacles;
    //private List<Transform> _obstacleList = new();

    //private RenderTexture _target;
    //private Camera _camera;

    //[SerializeField]
    //[Range(0.0f, 1.0f)]
    //private float BlendRatio = 1.0f;

    //private Material _blendMaterial;

    //private ComputeBuffer _capsuleBuffer;
    //private ComputeBuffer _cubeBuffer;

    //[SerializeField]
    //private HerdController Herd;

    //private void Awake()
    //{
    //    _camera = GetComponent<Camera>();
    //}

    //private void SetShaderParameters()
    //{
    //    RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
    //    RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
    //    RayTracingShader.SetBuffer(0, "_Capsules", _capsuleBuffer);
    //    RayTracingShader.SetBuffer(0, "_Cubes", _cubeBuffer);
    //}

    //private void OnEnable()
    //{
    //    _blendMaterial = new(Shader.Find("Hidden/ImageBlender"));
    //    SetUpScene();
    //}

    //private void OnDisable()
    //{
    //    if (_blendMaterial != null)
    //    {
    //        DestroyImmediate(_blendMaterial);
    //    }
    //    if (_capsuleBuffer != null)
    //    {
    //        _capsuleBuffer.Release();
    //    }
    //    if (_cubeBuffer != null)
    //    {
    //        _cubeBuffer.Release();
    //    }
    //}

    //private void SetUpScene()
    //{
    //    List<Capsule> capsules = new();

    //    for (int i = 0; i < Herd._agentList.Count; i++)
    //    {
    //        Vector3 pos = Herd._agentList[i].transform.position + new Vector3(0.0f, 0.2f, 0.0f);
    //        Vector3 dir = Herd._agentList[i].transform.forward;
    //        Capsule agentCapsule = new();
    //        agentCapsule.centerA = pos + 0.3f * dir;
    //        agentCapsule.centerB = pos - 0.3f * dir;
    //        agentCapsule.radius = 0.2f;
    //        capsules.Add(agentCapsule);
    //    }

    //    _capsuleBuffer = new ComputeBuffer(capsules.Count, 28);
    //    _capsuleBuffer.SetData(capsules);

    //    foreach (Transform obstacle in CubeObstacles.transform)
    //    {
    //        _obstacleList.Add(obstacle);
    //    }

    //    Cube[] obstacleDataToGPU = new Cube[_obstacleList.Count];
    //    for (int i = 0; i < _obstacleList.Count; i++)
    //    {
    //        obstacleDataToGPU[i].worldToLocal = _obstacleList[i].worldToLocalMatrix;
    //        obstacleDataToGPU[i].localToWorld = _obstacleList[i].localToWorldMatrix;
    //        obstacleDataToGPU[i].halfSize = _obstacleList[i].gameObject.GetComponent<BoxCollider>().size / 2.0f;
    //    }

    //    _cubeBuffer = new(obstacleDataToGPU.Length, sizeof(float) * 35);
    //    _cubeBuffer.SetData(obstacleDataToGPU);
    //}

    //private void InitRenderTexture()
    //{
    //    if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
    //    {
    //        if (_target != null)
    //            _target.Release();

    //        _target = new(
    //            Screen.width, Screen.height, 1,
    //            RenderTextureFormat.ARGBFloat,
    //            RenderTextureReadWrite.Linear);
    //        _target.enableRandomWrite = true;
    //        _target.Create();
    //    }
    //}

    //private void Render(RenderTexture source, RenderTexture destination)
    //{
    //    InitRenderTexture();

    //    RayTracingShader.SetTexture(0, "Result", _target);
    //    int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
    //    int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
    //    RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

    //    _blendMaterial.SetFloat("_Ratio", BlendRatio);
    //    _blendMaterial.SetTexture("_Source", source);
    //    Graphics.Blit(_target, destination, _blendMaterial);
    //}

    //private void OnRenderImage(RenderTexture source, RenderTexture destination)
    //{
    //    SetShaderParameters();
    //    Render(source, destination);
    //}
}
