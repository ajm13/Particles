using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtendoArm : MonoBehaviour
{
  public enum Hand { Left, Right };

  public Transform eyeAnchor;
  public Transform handAnchor;
  public Transform shoulderAnchor;

  public float extendStart = 0.4f;
  public float extendScale = 5;
  public float extendExponent = 3;
  public Vector3 shoulderOffset = new Vector3(0.17f, 0.19f, 0.08f);
  public Hand hand;

  // Use this for initialization
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {
    Vector3 offset = shoulderOffset;
    offset.x *= hand == Hand.Left ? 1 : -1;

    shoulderAnchor.position = eyeAnchor.position - offset;

    Vector3 dir = handAnchor.position - shoulderAnchor.position;
    float d = dir.magnitude < extendStart
      ? 0 : Mathf.Pow(extendScale * (dir.magnitude - extendStart), extendExponent);

    transform.localPosition = d * dir.normalized;
  }
}
