using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity : MonoBehaviour {

  public int numParticles = 100000;
  public ComputeShader computeShader;
  public Shader particleShader;
  public Texture2D sprite;

  public float particleSize = 0.02f;
  public float handSize = 0.05f;

  public Transform leftHand;
  public Transform rightHand;

  private Vector3 olhv;
  private Vector3 orhv;

  private bool lff;
  private bool rff;

  const int GROUP_SIZE = 256;

  int kID, threadGroups;
  ComputeBuffer particles;
  Material material;

  void Start() {
    // twoHand = new GameObject().transform;

    threadGroups = Mathf.CeilToInt((float)numParticles / GROUP_SIZE);

    Particle[] particleArray = new Particle[numParticles];
    for (int i = 0; i < numParticles; i++) {
      particleArray[i].position = 10 * Random.insideUnitSphere;
      particleArray[i].velocity = Vector3.zero;
      particleArray[i].color = Vector3.one;
    }

    particles = new ComputeBuffer(numParticles, Particle.SIZE);
    particles.SetData(particleArray);

    material = new Material(particleShader);
    material.SetBuffer("particles", particles);
    material.SetTexture("_Sprite", sprite);

    kID = computeShader.FindKernel("Gravity");
    computeShader.SetBuffer(kID, "particles", particles);
    computeShader.SetInt("numParticles", numParticles);
  }

  void Update() {
    Vector3 lh = leftHand.position;
    Vector3 rh = rightHand.position;

    Vector3 lhv = transform.worldToLocalMatrix.MultiplyPoint(lh);
    Vector3 rhv = transform.worldToLocalMatrix.MultiplyPoint(rh);

    Vector3 v1 = lhv - olhv;
    Vector3 v2 = rhv - orhv;

    olhv = lhv;
    orhv = rhv;

    float lf = -OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
    float rf = -OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;

    lff = (OVRInput.Get(OVRInput.Button.Four));
    leftHand.GetComponentInChildren<OutlineMode>().SetColor(lff ? Color.cyan : Color.white);

    rff = (OVRInput.Get(OVRInput.Button.Two));
    rightHand.GetComponentInChildren<OutlineMode>().SetColor(rff ? Color.cyan : Color.white);

    leftHand.GetChild(0).transform.localScale = handSize * transform.lossyScale;
    rightHand.GetChild(0).transform.localScale = handSize * transform.lossyScale;

    float[] g1 = { lhv.x, lhv.y, lhv.z, lf };
    float[] g2 = { rhv.x, rhv.y, rhv.z, rf };

    computeShader.SetFloats("g1", g1);
    computeShader.SetFloats("g2", g2);
    computeShader.SetVector("v1", v1);
    computeShader.SetVector("v2", v2);
    computeShader.SetBool("rff", rff);
    computeShader.SetBool("lff", lff);
  }

  void FixedUpdate() {
    computeShader.SetFloat("deltaTime", Time.fixedDeltaTime);
    computeShader.Dispatch(kID, threadGroups, 1, 1);
  }

  void OnRenderObject() {
    material.SetPass(0);
    material.SetVector("_size", particleSize * transform.lossyScale);
    material.SetVector("_worldPos", transform.position);

    var m = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(transform.rotation), transform.lossyScale);
    material.SetMatrix("_worldRot", m);

    Graphics.DrawProcedural(MeshTopology.Points, numParticles);
  }

  void OnDestroy() {
    particles.Release();
  }
}
