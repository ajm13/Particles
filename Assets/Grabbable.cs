using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grabbable : MonoBehaviour {

  public Material highlight;

  private Transform m_parent;
  private Rigidbody m_rigidbody;
  private bool m_kinematic;
  private Material m_material;
  private int m_hover = 0;

  public void HoverStart() {
    m_hover++;

    if (highlight)
      GetComponent<Renderer>().material = highlight;
  }

  public void HoverEnd() {
    m_hover--;

    if (highlight && m_hover == 0)
      GetComponent<Renderer>().material = m_material;
  }

  public void AttachTo(Transform anchor) {
    if (m_rigidbody)
      m_rigidbody.isKinematic = true;

    transform.parent = anchor;
  }

  public void Detach() {
    if (m_rigidbody)
      m_rigidbody.isKinematic = m_kinematic;

    transform.parent = m_parent;
  }

  void Start() {
    m_parent = transform.parent;
    m_rigidbody = GetComponent<Rigidbody>() ?? null;

    if (m_rigidbody)
      m_kinematic = m_rigidbody.isKinematic;

    if (highlight)
      m_material = GetComponent<Renderer>().material;
  }
}
