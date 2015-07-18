﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genso.API {

    /// <summary>
    /// General character class for handling the physics and animations of individual characters
    /// </summary>
    /// Author: James Liu
    /// Authored on 07/01/2015
    [RequireComponent(typeof(CapsuleCollider))]
	public class Character : GensoBehaviour {

        private enum FacingMode { Rotation, Scale }

        private CapsuleCollider movementCollider;
        private CapsuleCollider triggerCollider;

        [SerializeField]
        private FacingMode _facingMode = FacingMode.Rotation;

        [SerializeField]
        private float triggerSizeRatio = 1.5f;

        // Private state variables
        private bool _grounded;
        private bool _helpless;
        private bool _facing;
        private bool _dashing;

        private Collider[] hurtboxes;

        public int PlayerNumber { get; set; }

        public Color PlayerColor {
            get { return GameSettings.GetPlayerColor(PlayerNumber); }
        }

        public ICharacterInput InputSource { get; set; }

        public Transform RespawnPosition { get; set; }

        public bool IsGrounded {
            get { return _grounded; }
            set {
                bool changed = _grounded == value;
                _grounded = value;
                if (value)
                    IsHelpless = false;
                if(changed)
                    OnGrounded.SafeInvoke();
            }
        }

        /// <summary>
        /// The direction the character is currently facing.
        /// If set to true, the character faces the right.
        /// If set to false, the character faces the left.
        /// 
        /// The method in which the character is flipped depends on what the Facing Mode parameter is set to.
        /// </summary>
        public bool Facing {
            get { return _facing; }
            set {
                if (_facing != value) {
                    if (_facingMode == FacingMode.Rotation)
                        transform.Rotate(0f, 180f, 0f);
                    else {
                        Vector3 temp = transform.localScale;
                        temp.x *= -1;
                        transform.localScale = temp;
                    }
                }
                _facing = value;
            }
        }

        public bool IsDashing {
            get {
                return IsGrounded && _dashing;
            }
            set {
                _dashing = value;
            }
        }

        public bool IsFastFalling {
            get {
                return !IsGrounded && InputSource != null && InputSource.Crouch;
            }
        }

        public bool IsCrouching {
            get {
                return IsGrounded && InputSource != null && InputSource.Crouch;
            }
        }

        public bool IsHelpless {
            get {
                return !IsGrounded && _helpless;
            }
            set {
                bool changed = _helpless == value;
                _helpless = value;
                if(changed)
                    OnHelpless.SafeInvoke();
            }
        }

        public event Action OnJump;
        public event Action OnHelpless;
        public event Action OnGrounded;
        public event Action<Vector2> OnMove;
        public event Action OnBlastZoneExit;

        public float Height {
            get { return movementCollider.height; }
        }

        #region Unity Callbacks

        protected virtual void Awake() {
            FindHurtboxes();

            // TODO: Find a better place to put this
            CameraController.AddTarget(this);

            movementCollider = GetComponent<CapsuleCollider>();
            movementCollider.isTrigger = false;

            triggerCollider = gameObject.AddComponent<CapsuleCollider>();
            triggerCollider.isTrigger = true;
        }

        protected virtual void Update() {
            // Sync Trigger and Movement Colliders
            triggerCollider.center = movementCollider.center;
            triggerCollider.direction = movementCollider.direction;
            triggerCollider.height = movementCollider.height * triggerSizeRatio;
            triggerCollider.radius = movementCollider.radius * triggerSizeRatio;

            if (InputSource.Jump)
                Jump();

        }


        protected virtual void OnDrawGizmos() {
            FindHurtboxes();
            GizmoUtil.DrawHitboxes(hurtboxes, HitboxType.Damageable, x => x.enabled);
        }
        #endregion

        void FindHurtboxes() {
            List<Collider> tempHurtboxes = new List<Collider>();
            foreach (Collider collider in GetComponentsInChildren<Collider>()) {
                if (!collider.CheckLayer(GameSettings.HurtboxLayers))
                    continue;
                Hurtbox.Register(this, collider);
                tempHurtboxes.Add(collider);
            }
            hurtboxes = tempHurtboxes.ToArray();
        }

        internal void AddCharacterComponent(CharacterComponent component) {
            if(component == null)
                throw new ArgumentNullException("component");

            OnJump += component.OnJump;
            OnGrounded += component.OnGrounded;
            OnMove += component.OnMove;
            OnBlastZoneExit += component.OnBlastZoneExit;
        }

        internal void RemoveCharacterComponent(CharacterComponent component) {
            if(component == null)
                throw new ArgumentNullException("component");

            OnJump -= component.OnJump;
            OnGrounded -= component.OnGrounded;
            OnMove -= component.OnMove;
            OnBlastZoneExit -= component.OnBlastZoneExit;
        }

        public virtual void Move(Vector2 direction) {
            OnMove.SafeInvoke(direction);
        }

        public virtual void Jump() {
            OnJump.SafeInvoke();
        }

        public void BlastZoneExit() {
            OnBlastZoneExit.SafeInvoke();
        }

    }

}