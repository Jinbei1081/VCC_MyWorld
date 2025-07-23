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

public class DynamicBoneCollider : MonoBehaviour
{
    // it seem a enum but I cannot know actual value names so I keep this as int
    public int m_Direction;
    public Vector3 m_Center;
    public int m_Bound;
    public float m_Radius;
    public float m_Height;
    public float m_Radius2;
}

#endif
