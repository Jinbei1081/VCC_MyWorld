// Fake DynamicBone Components
// https://gist.github.com/anatawa12/4476430cfcc2ccef4bc40341d20001cf
// anatawa12 did't buy the DynamicBone asset and I made this class based on actual usage assets of DynamicBones.
// 
// MIT License
// 
// Copyright (c) 2023 anatawa12
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

#if UNITY_EDITOR && (!ANATAWA12_GISTS_VPM_PACKAGE || GIST_4476430cfcc2ccef4bc40341d20001cf)

using UnityEngine;
using System.Collections.Generic;

public class DynamicBone : MonoBehaviour
{
  public Transform m_Root;
  public float m_UpdateRate;

  public float m_UpdateMode;

  public float m_Damping;

  public AnimationCurve m_DampingDistrib;
  public float m_Elasticity;
  public AnimationCurve m_ElasticityDistrib;
  public float m_Stiffness;
  public AnimationCurve m_StiffnessDistrib;
  public float m_Inert;
  public AnimationCurve m_InertDistrib;
  public float m_Friction;
  public AnimationCurve m_FrictionDistrib;
  public float m_Radius;
  public AnimationCurve m_RadiusDistrib;

  public float m_EndLength;
  public Vector3 m_EndOffset;
  public Vector3 m_Gravity;
  public Vector3 m_Force;

  public List<DynamicBoneCollider> m_Colliders; // DynamicBoneCollider[] -> List<DynamicBoneCollider>: performance rank calucation
  public List<Transform> m_Exclusions; // Transform[] -> List<Transform>: error in DB -> PB converter
  public int m_FreezeAxis;
  public float m_DistantDisable;
  public Object m_ReferenceObject;

  public float m_DistanceToObject;
}

#endif
