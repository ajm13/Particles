using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selector : MonoBehaviour {

  public Grabbable target {
    get { return m_target; }
  }

  private Grabbable m_target;

  void OnTriggerEnter(Collider other) {
    if (m_target != null) return;
    Grabbable grabbable = other.GetComponent<Grabbable>();
    if (grabbable != null) {
      m_target = grabbable;
      m_target.HoverStart();
    }
  }

  void OnTriggerExit(Collider other) {
    Grabbable grabbable = other.GetComponent<Grabbable>();
    if (grabbable != null && m_target == grabbable) {
      m_target.HoverEnd();
      m_target = null;
    }
  }
}
