﻿using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Aim section of the component pipeline.
    /// Its job is to aim the camera in response to the user's mouse or joystick input.
    ///
    /// The composer does not change the camera's position.  It will only pan and tilt the
    /// camera where it is, in order to get the desired framing.  To move the camera, you have
    /// to use the virtual camera's Body section.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    public class CinemachinePOV : CinemachineComponentBase
    {
        /// <summary>The Vertical axis.  Value is -90..90. Controls the vertical orientation</summary>
        [Tooltip("The Vertical axis.  Value is -90..90. Controls the vertical orientation")]
        [AxisStateProperty]
        public AxisState m_VerticalAxis = new AxisState(-70, 70, false, false, 300f, 0.1f, 0.1f, "Mouse Y", true);

        /// <summary>Controls how automatic recentering of the Vertical axis is accomplished</summary>
        [Tooltip("Controls how automatic recentering of the Vertical axis is accomplished")]
        public AxisState.Recentering m_VerticalRecentering = new AxisState.Recentering(false, 1, 2);

        /// <summary>The Horizontal axis.  Value is -180..180.  Controls the horizontal orientation</summary>
        [Tooltip("The Horizontal axis.  Value is -180..180.  Controls the horizontal orientation")]
        [AxisStateProperty]
        public AxisState m_HorizontalAxis = new AxisState(-180, 180, true, false, 300f, 0.1f, 0.1f, "Mouse X", false);

        /// <summary>Controls how automatic recentering of the Horizontal axis is accomplished</summary>
        [Tooltip("Controls how automatic recentering of the Horizontal axis is accomplished")]
        public AxisState.Recentering m_HorizontalRecentering = new AxisState.Recentering(false, 1, 2);

        /// <summary>True if component is enabled and has a LookAt defined</summary>
        public override bool IsValid { get { return enabled; } }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Aim stage</summary>
        public override CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Aim; } }

        private void OnValidate()
        {
            m_VerticalAxis.Validate();
            m_VerticalRecentering.Validate();
            m_HorizontalAxis.Validate();
            m_HorizontalRecentering.Validate();
        }

        /// <summary>Applies the axis values and orients the camera accordingly</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for calculating damping.  Not used.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid)
                return;

            // Only read joystick when game is playing
            if (deltaTime >= 0 && CinemachineCore.Instance.IsLive(VirtualCamera))
            {
                if (m_HorizontalAxis.Update(deltaTime))
                    m_HorizontalRecentering.CancelRecentering();
                if (m_VerticalAxis.Update(deltaTime))
                    m_VerticalRecentering.CancelRecentering();
            }
            m_HorizontalAxis.Value = m_HorizontalRecentering.DoRecentering(m_HorizontalAxis.Value, deltaTime, 0);
            m_VerticalAxis.Value = m_VerticalRecentering.DoRecentering(m_VerticalAxis.Value, deltaTime, 0);

            // If we have a transform parent, then apply POV in the local space of the parent
            Quaternion rot = Quaternion.Euler(m_VerticalAxis.Value, m_HorizontalAxis.Value, 0);
            Transform parent = VirtualCamera.transform.parent;
            if (parent != null)
                rot = parent.rotation * rot;
            else
                rot = rot * Quaternion.FromToRotation(Vector3.up, curState.ReferenceUp);
            curState.RawOrientation = rot;
        }

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation does nothing.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>True if the vcam should do an internal update as a result of this call</returns>
        public override bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime,
            ref CinemachineVirtualCameraBase.TransitionParams transitionParams)
        {
            m_HorizontalAxis.Value = m_HorizontalRecentering.DoRecentering(m_HorizontalAxis.Value, -1, 0);
            m_VerticalAxis.Value = m_VerticalRecentering.DoRecentering(m_VerticalAxis.Value, -1, 0);
            m_HorizontalRecentering.CancelRecentering();
            m_VerticalRecentering.CancelRecentering();
            if (fromCam != null && transitionParams.m_InheritPosition)
            {
                Vector3 up = VcamState.ReferenceUp;
                Quaternion targetRot = fromCam.State.RawOrientation;
                Vector3 fwd = Vector3.forward;
                Transform parent = VirtualCamera.transform.parent;
                if (parent != null)
                    fwd = parent.rotation * fwd;

                m_HorizontalAxis.Value = 0;
                m_HorizontalAxis.Reset();
                Vector3 targetFwd = targetRot * Vector3.forward;
                Vector3 a = fwd.ProjectOntoPlane(up);
                Vector3 b = targetFwd.ProjectOntoPlane(up);
                if (!a.AlmostZero() && !b.AlmostZero())
                    m_HorizontalAxis.Value = Vector3.SignedAngle(a, b, up);

                m_VerticalAxis.Value = 0;
                m_VerticalAxis.Reset();
                fwd = Quaternion.AngleAxis(m_HorizontalAxis.Value, up) * fwd;
                Vector3 right = Vector3.Cross(up, fwd);
                if (!right.AlmostZero())
                    m_VerticalAxis.Value = Vector3.SignedAngle(fwd, targetFwd, right);
                return true;
            }
            return false;
        }
    }
}
