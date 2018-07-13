using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabManager : MonoBehaviour {

  public Selector rhand;
  public Selector lhand;
  public Transform eye;
  public Grabbable environment;

  public float smoothing = 0.2f;

  private Transform m_ranchor;
  private Transform m_lanchor;
  private Transform m_manchor;

  private Vector3 m_rpos;
  private Vector3 m_lpos;
  private Vector3 m_hint;

  private Quaternion m_rrot;
  private Quaternion m_lrot;
  private Quaternion m_mrot;

  private float m_scale;

  private bool m_lgrabbed = false;
  private bool m_rgrabbed = false;
  private bool m_twohand = false;

  private Grabbable m_rtarget;
  private Grabbable m_ltarget;

  // Use this for initialization
  void Start() {
    m_ranchor = new GameObject("ranchor").transform;
    m_lanchor = new GameObject("lanchor").transform;
    m_manchor = new GameObject("manchor").transform;
  }

  // Update is called once per frame
  void Update() {
    bool rgrab = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
    bool lgrab = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);

    bool rrelease = OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
    bool lrelease = OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);

    // position vars for simpler code
    Vector3 epos = eye.position;
    Vector3 rpos = rhand.transform.position;
    Vector3 lpos = lhand.transform.position;
    Vector3 mid = 0.5f * (rpos + lpos);

    Quaternion rrot = rhand.transform.rotation;
    Quaternion lrot = lhand.transform.rotation;

    // set anchor positions    
    m_ranchor.position = rpos;
    m_lanchor.position = lpos;
    m_manchor.position = mid;

    m_ranchor.rotation = Quaternion.Slerp(m_ranchor.rotation, rrot * Quaternion.Inverse(m_rrot), 0.2f);
    m_lanchor.rotation = Quaternion.Slerp(m_lanchor.rotation, lrot * Quaternion.Inverse(m_lrot), 0.2f);

    Grabbable rtarget = rhand.target ?? environment;
    Grabbable ltarget = lhand.target ?? environment;


    // start right grab
    if (rgrab) {
      // if same target as left hand, two hand grab
      if (m_lgrabbed && rtarget == ltarget) {
        ResetAnchor(m_manchor);
        rtarget.AttachTo(m_manchor);
        m_twohand = true;
      }

      // one hand grab
      else {
        ResetAnchor(m_ranchor);
        rtarget.AttachTo(m_ranchor);
        m_rrot = rrot;
      }

      m_rtarget = rtarget;
      m_rgrabbed = true;
    }

    // start left grab
    if (lgrab) {
      // if same target as right hand, two hand grab
      if (m_rgrabbed && ltarget == rtarget) {
        ResetAnchor(m_manchor);
        ltarget.AttachTo(m_manchor);
        m_twohand = true;
      }

      // one hand grab
      else {
        ResetAnchor(m_lanchor);
        ltarget.AttachTo(m_lanchor);
        m_lrot = lrot;
      }

      m_ltarget = ltarget;
      m_lgrabbed = true;
    }

    // start two hand grab
    if (m_twohand && (lgrab || rgrab)) {
      m_rpos = rpos;
      m_lpos = lpos;
      m_hint = (mid - epos).normalized;
      m_mrot = m_manchor.rotation;
      m_scale = (rpos - lpos).magnitude;
    }

    // right hand release
    if (rrelease) {
      if (m_twohand) {
        m_twohand = false;
        m_rtarget.AttachTo(m_lanchor);
      } else m_rtarget.Detach();

      m_rgrabbed = false;
    }

    // left hand release
    if (lrelease) {
      if (m_twohand) {
        m_twohand = false;
        m_ltarget.AttachTo(m_ranchor);
      } else m_ltarget.Detach();

      m_lgrabbed = false;
    }

    // two hand scale and rotation
    if (m_twohand) {
      float scale = (rpos - lpos).magnitude / m_scale;
      m_manchor.localScale = scale * Vector3.one;

      Vector3 hint = (mid - epos).normalized;
      Vector3 dir0 = (m_rpos - m_lpos).normalized;
      Vector3 dir1 = (rpos - lpos).normalized;

      m_mrot = Quaternion.FromToRotation(dir0, dir1)
             * Quaternion.FromToRotation(m_hint, hint)
             * m_mrot;

      m_manchor.rotation = m_mrot;

      m_hint = hint;
      m_rpos = rpos;
      m_lpos = lpos;
    }
  }

  void ResetAnchor(Transform anchor) {
    anchor.localScale = Vector3.one;
    anchor.rotation = Quaternion.identity;
  }
}
