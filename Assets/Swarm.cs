using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class Swarm : MonoBehaviour
{

    public int numParticles = 10000;
    public ComputeShader computeShader;
    public Shader particleShader;
    public Texture2D sprite;

    public float particleSize = 0.02f;
    public float handSize = 0.05f;

    public bool useOpt = false;

    public float fov = 270;
    public float radius = 1.5f;
    public float speed = 0.02f;
    public float maxForce = 0.001f;
    public float randomness = 0.001f;

    public float aSight = 0.1f;
    public float cSight = 0.1f;
    public float sSight = 0.03f;

    public float aWeight = 2.0f;
    public float cWeight = 1.0f;
    public float sWeight = 1.0f;

    private bool paused = false;
    private float time = 0;

    public GameObject leftHand;
    public GameObject rightHand;

    private Vector3 olhv;
    private Vector3 orhv;

    private float lr = 0.2f;
    private float rr = 0.2f;

    private bool lff = true;
    private bool rff = true;

    const int GROUP_SIZE = 256;

    int zeroBuffers, countBins, copyCounts, scanIter, swapBuffers, sortBins,
        swarm, bGroups, pGroups, numBins;
    ComputeBuffer particles;
    ComputeBuffer temp, binCounts, binIds, nnsBins;
    Material material;

    bool scaling = false;
    float scaleStart;
    float scalePrev;

    Vector3 scalePivot;
    Vector3 scaleOrigin;

    void Start()
    {
        pGroups = Mathf.CeilToInt((float)numParticles / GROUP_SIZE);

        Particle[] particleArray = new Particle[numParticles];
        for (int i = 0; i < numParticles; i++)
        {
            particleArray[i].position = radius * Random.insideUnitSphere;
            particleArray[i].velocity = Random.insideUnitSphere;
            particleArray[i].color = Vector3.one;
        }

        particles = new ComputeBuffer(numParticles, Particle.SIZE);
        particles.SetData(particleArray);

        material = new Material(particleShader);
        material.SetBuffer("particles", particles);
        material.SetTexture("_Sprite", sprite);

        swarm = computeShader.FindKernel("Swarm");
        zeroBuffers = computeShader.FindKernel("ZeroBuffers");
        countBins = computeShader.FindKernel("CountBins");
        copyCounts = computeShader.FindKernel("CopyCounts");
        scanIter = computeShader.FindKernel("ScanIter");
        swapBuffers = computeShader.FindKernel("SwapBuffers");
        sortBins = computeShader.FindKernel("SortBins");

        InitializeBins();

        computeShader.SetBuffer(swarm, "particles", particles);
        computeShader.SetBuffer(countBins, "particles", particles);
        computeShader.SetBuffer(sortBins, "particles", particles);

        computeShader.SetInt("numParticles", numParticles);
        computeShader.SetFloat("time", time);
        computeShader.SetFloat("speed", speed);
        computeShader.SetFloat("maxForce", maxForce);
        computeShader.SetFloat("radius", radius);
        computeShader.SetFloat("fov", fov);
        computeShader.SetFloat("randomness", randomness);

        computeShader.SetFloat("aSight", aSight);
        computeShader.SetFloat("cSight", cSight);
        computeShader.SetFloat("sSight", sSight);
        computeShader.SetFloat("aWeight", aWeight);
        computeShader.SetFloat("cWeight", cWeight);
        computeShader.SetFloat("sWeight", sWeight);

        computeShader.SetFloat("lr", lr);
        computeShader.SetFloat("rr", rr);
        computeShader.SetBool("lff", lff);
        computeShader.SetBool("rff", rff);
    }

    void InitializeBins()
    {
        float maxSight = Mathf.Max(aSight, cSight, sSight);
        float binWidth = maxSight;
        int gridWidth = Mathf.CeilToInt(2 * radius / binWidth);
        numBins = gridWidth * gridWidth * gridWidth;
        bGroups = Mathf.CeilToInt((float)numBins / GROUP_SIZE);

        print(numBins + " bins");

        temp = new ComputeBuffer(numBins, sizeof(int));
        binIds = new ComputeBuffer(numBins, sizeof(int));
        binCounts = new ComputeBuffer(numBins, sizeof(int));
        nnsBins = new ComputeBuffer(numParticles, Particle.SIZE);

        computeShader.SetBuffer(zeroBuffers, "temp", temp);
        computeShader.SetBuffer(copyCounts, "temp", temp);
        computeShader.SetBuffer(scanIter, "temp", temp);
        computeShader.SetBuffer(swapBuffers, "temp", temp);

        computeShader.SetBuffer(swarm, "binCounts", binCounts);
        computeShader.SetBuffer(zeroBuffers, "binCounts", binCounts);
        computeShader.SetBuffer(countBins, "binCounts", binCounts);
        computeShader.SetBuffer(copyCounts, "binCounts", binCounts);

        computeShader.SetBuffer(swarm, "nnsBins", nnsBins);
        computeShader.SetBuffer(sortBins, "nnsBins", nnsBins);

        computeShader.SetBuffer(swarm, "binIds", binIds);
        computeShader.SetBuffer(scanIter, "binIds", binIds);
        computeShader.SetBuffer(swapBuffers, "binIds", binIds);
        computeShader.SetBuffer(sortBins, "binIds", binIds);

        computeShader.SetInt("numBins", numBins);
        computeShader.SetInt("gridWidth", gridWidth);
        computeShader.SetFloat("binWidth", binWidth);
    }

    void Update()
    {
        if (!paused) time += Time.deltaTime;
        if (OVRInput.GetDown(OVRInput.Button.One)) paused = !paused;

        Vector3 lh = leftHand.transform.position;
        Vector3 rh = rightHand.transform.position;

        Vector3 lhv = transform.worldToLocalMatrix.MultiplyPoint(lh);
        Vector3 rhv = transform.worldToLocalMatrix.MultiplyPoint(rh);

        Vector3 v1 = lhv - olhv;
        Vector3 v2 = rhv - orhv;

        olhv = lhv;
        orhv = rhv;

        //Vector3 v1 = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTrackedRemote);
        //Vector3 v2 = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTrackedRemote);

        //Matrix4x4 trs = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(transform.rotation), transform.localScale);
        //v1 = transform.worldToLocalMatrix.MultiplyVector(v1);//trs.MultiplyVector(v1);
        //v2 = transform.worldToLocalMatrix.MultiplyVector(v2);//trs.MultiplyVector(v2);

        float lf = -OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
        float rf = -OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;

        float lt = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);
        float rt = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);

        bool lhg = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
        bool rhg = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);

        if (OVRInput.GetDown(OVRInput.Button.Two)) rff = !rff;
        if (OVRInput.GetDown(OVRInput.Button.Four)) lff = !lff;

        if (lt > 0 && Mathf.Abs(lf) > 0)
            lr = Mathf.Clamp(lr - lt * 0.02f * Mathf.Sign(lf), 0.05f, 0.95f);

        if (rt > 0 && Mathf.Abs(rf) > 0)
            rr = Mathf.Clamp(rr - rt * 0.02f * Mathf.Sign(rf), 0.05f, 0.95f);

        if (rhg && !lhg)
            transform.parent = rightHand.transform;
        else if (lhg && !rhg)
            transform.parent = leftHand.transform;
        else
            transform.parent = null;

        // start scaling
        if (!scaling && lhg && rhg)
        {
            scaleStart = (lh - rh).magnitude;
            scalePrev = transform.localScale.x;
            scalePivot = 0.5f * (lh + rh);
            scaleOrigin = transform.position;
            scaling = true;
        }

        // stop scaling
        if (scaling && (!lhg || !rhg))
            scaling = false;

        // apply scale
        if (scaling)
        {
            float scale = (lh - rh).magnitude / scaleStart;
            scale = Mathf.Clamp(scale * scalePrev, 0.01f, 8f) / scalePrev;
            Vector3 scaleV = (scale * scalePrev) * Vector3.one;

            transform.position = (scaleOrigin - scalePivot) * scale + scalePivot;
            transform.localScale = scaleV;
            leftHand.transform.GetChild(0).transform.localScale = handSize * scaleV;
            rightHand.transform.GetChild(0).transform.localScale = handSize * scaleV;
        }

        float[] g1 = { lhv.x, lhv.y, lhv.z, lf };
        float[] g2 = { rhv.x, rhv.y, rhv.z, rf };

        computeShader.SetBool("paused", paused);
        computeShader.SetBool("useOpt", useOpt);

        computeShader.SetFloat("time", time);
        computeShader.SetFloat("deltaTime", Time.fixedDeltaTime);

        computeShader.SetFloats("g1", g1);
        computeShader.SetFloats("g2", g2);

        computeShader.SetVector("v1", v1);
        computeShader.SetVector("v2", v2);

        computeShader.SetFloat("speed", speed);
        computeShader.SetFloat("maxForce", maxForce);
        computeShader.SetFloat("radius", radius);
        computeShader.SetFloat("fov", fov);
        computeShader.SetFloat("randomness", randomness);

        computeShader.SetFloat("aSight", aSight);
        computeShader.SetFloat("cSight", cSight);
        computeShader.SetFloat("sSight", sSight);
        computeShader.SetFloat("aWeight", aWeight);
        computeShader.SetFloat("cWeight", cWeight);
        computeShader.SetFloat("sWeight", sWeight);

        computeShader.SetFloat("lr", lr);
        computeShader.SetFloat("rr", rr);
        computeShader.SetBool("rff", rff);
        computeShader.SetBool("lff", lff);

        computeShader.Dispatch(zeroBuffers, bGroups, 1, 1);
        computeShader.Dispatch(countBins, pGroups, 1, 1);
        // computeShader.Dispatch(calcBinIds, bGroups, 1, 1);
        computeShader.Dispatch(copyCounts, bGroups, 1, 1);

        for (int o = 1; o < numBins; o *= 2)
        {
            computeShader.SetInt("o", o);
            computeShader.Dispatch(scanIter, bGroups, 1, 1);
            computeShader.Dispatch(swapBuffers, bGroups, 1, 1);
        }

        computeShader.Dispatch(sortBins, pGroups, 1, 1);
        computeShader.Dispatch(swarm, pGroups, 1, 1);

        // int[] bleh = new int[numBins];
        // int[] bleh2 = new int[numBins];
        // int[] bleh3 = new int[2 * numBins];
        // binCounts.GetData(bleh);
        // temp.GetData(bleh3);
        // binIds.GetData(bleh2);

        // string str = "cnt: ";
        // string str3 = "tmp: ";
        // string str2 = "ids: ";
        // for (int q = 0; q < 2 * numBins; q++)
        // {
        //     if (q < numBins)
        //     {
        //         str += bleh[q] + ", ";
        //         str2 += bleh2[q] + ", ";
        //     }
        //     str3 += bleh3[q] + ", ";
        // }
        // print(str);
        // print(str3);
        // print(str2);
        // print("");
    }

    void OnRenderObject()
    {
        material.SetPass(0);
        material.SetVector("_size", particleSize * transform.localScale);
        material.SetVector("_worldPos", transform.position);

        var m = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(transform.rotation), transform.localScale);
        material.SetMatrix("_worldRot", m);

        Graphics.DrawProcedural(MeshTopology.Points, numParticles);
    }

    void OnDestroy()
    {
        particles.Release();
        temp.Release();
        binIds.Release();
        binCounts.Release();
        nnsBins.Release();
    }
}
