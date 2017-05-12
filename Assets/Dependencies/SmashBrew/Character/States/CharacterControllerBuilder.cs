using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HouraiTeahouse.SmashBrew.States;

namespace HouraiTeahouse.SmashBrew.Characters {

    [CreateAssetMenu]
    public partial class CharacterControllerBuilder : ScriptableObject, ISerializationCallbackReceiver {

        [Serializable]
        public class StateData {
            public string Name;
            public CharacterStateData Data;
        }

        public StateData[] _data;
        Dictionary<string, CharacterStateData> _dataMap;

        protected internal StateControllerBuilder<CharacterState, CharacterStateContext> Builder { get; internal set; }

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        protected CharacterState State(string name, CharacterStateData data) {
            var state = new CharacterState(name, data);
            if (Builder != null)
                Builder.AddState(state);
            return state;
        }

        void InjectState(object obj, string path = "", int depth = 0) {
            var type = typeof(CharacterState);
            foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties(flags)) {
                string propertyName = propertyInfo.Name;
                if (!string.IsNullOrEmpty(path))
                    propertyName = path + "." + propertyName;
                if (propertyName == "name" || propertyName == "hideFlags" || propertyName == "Builder")
                    continue;
                var propertyType = propertyInfo.PropertyType;
                object instance;
                if (propertyType == type) {
                    if (_dataMap == null)
                        _dataMap = new Dictionary<string, CharacterStateData>();
                    if (!_dataMap.ContainsKey(propertyName))
                        _dataMap.Add(propertyName, new CharacterStateData());
                    var state = new CharacterState(propertyName, _dataMap[propertyName]);
                    Builder.AddState(state);
                    instance = state;
                } else {
                    instance = Activator.CreateInstance(propertyType);
                    if (depth < 7)
                        InjectState(instance, propertyName, depth + 1);
                }
                propertyInfo.SetValue(obj, instance, null);
            }
        }

        protected Func<CharacterStateContext, bool> Input(Func<InputContext, bool> input) {
            Argument.NotNull(input);
            return ctx => input(ctx.Input);
        }

        protected Func<CharacterStateContext, bool> Attack(Func<InputContext, bool> inputFunc = null) {
            if (inputFunc == null)
                return ctx => ctx.Input.Attack.WasPressed;
            return ctx => ctx.Input.Attack.WasPressed && inputFunc(ctx.Input);
        }

        protected Func<CharacterStateContext, bool> Special(Func<InputContext, bool> inputFunc = null) {
            if (inputFunc == null)
                return ctx => ctx.Input.Special.WasPressed;
            return ctx => ctx.Input.Special.WasPressed && inputFunc(ctx.Input);
        }

        public StateController<CharacterState, CharacterStateContext> BuildCharacterControllerImpl(StateControllerBuilder<CharacterState, CharacterStateContext> builder) {
            Builder = builder;
            InjectState(this);

            //TODO(james7132): Make this configurable
            const float inputThreshold = 0.1f;

            // Ground Attacks
            Idle
                // Tilt Attacks
                .AddTransition(TiltUp, Attack(input => input.Movement.y > inputThreshold))
                .AddTransition(TiltDown, Attack(input => input.Movement.y < -inputThreshold))
                .AddTransition(TiltDown, Attack(input => Math.Abs(input.Movement.y) > inputThreshold))
                // Smash Attacks
                .AddTransitionTo(SmashUp.Charge)                            // May require additional conditional
                .AddTransitionTo(SmashSide.Charge)                          // May require additional conditional
                .AddTransitionTo(SmashDown.Charge)                          // May require additional conditional
                // Neutral Combo
                .AddTransition(Neutral, Attack());

            // Normal 
            Idle.AddTransition(Walk, ctx => Math.Abs(ctx.Input.Movement.x) > inputThreshold)
                //TODO(james7132): Figure out how to do proper smash input detection
                .AddTransitionTo(Dash)
                .AddTransition(Crouch, Input(input => input.Movement.y < -inputThreshold))
                .AddTransition(Fall, ctx => !ctx.IsGrounded);

            // Crouching States
            CrouchStart.AddTransitionTo(Crouch);
            Crouch.AddTransition(TiltDown, Attack())
                .AddTransition(CrouchEnd, Input(input => input.Movement.y >= -inputThreshold));
            CrouchEnd.AddTransitionTo(Idle);

            // Ledge States
            Idle.AddTransition(LedgeGrab, ctx => ctx.IsGrabbingLedge);
            LedgeGrab.AddTransitionTo(LedgeIdle);
            LedgeIdle.AddTransition(LedgeRelease, Input(input => input.Movement.y < -inputThreshold))
                .AddTransition(LedgeClimb, Input(input => input.Movement.y > inputThreshold))
                .AddTransition(LedgeJump, Input(input => input.Jump.WasPressed));
            LedgeJump.AddTransitionTo(Jump);
            new[] {LedgeRelease, LedgeClimb, LedgeEscape}
                .AddTransitions(Idle, ctx => ctx.NormalizedAnimationTime >= 1.0f && ctx.IsGrounded)
                .AddTransitions(Fall, ctx => ctx.NormalizedAnimationTime >= 1.0f && !ctx.IsGrounded);

            Run.AddTransitionTo(RunBrake);                                  // May require additional conditional

            // Aerial Attacks
            Fall.AddTransition(AerialUp, Attack(input => input.Movement.y > inputThreshold))
                .AddTransition(AerialDown, Attack(input => input.Movement.y < -inputThreshold))
                // TODO(james7132): Make these face in the right direction
                .AddTransition(AerialForward, Attack(input => input.Movement.y > inputThreshold))
                .AddTransition(AerialBackward, Attack(input => input.Movement.y < -inputThreshold))
                .AddTransition(AerialNeutral, Attack());
            new[] {AerialForward, AerialBackward, AerialDown, AerialUp, AerialNeutral}
                .AddTransitionTo(Fall);

            // Shielding
            Idle.AddTransition(Shield.On, Input(input => input.Shield.WasPressed));
            Shield.On.AddTransition(Shield.Perfect, ctx => ctx.IsHit)
                .AddTransitionTo(Shield.Main);
            Shield.Main.AddTransition(Shield.Broken, ctx => ctx.ShieldHP < 0);
            Shield.Broken.AddTransitionTo(Shield.Stunned);
            new[] {Shield.Broken, Shield.Stunned, Idle}.Chain();
            
            // Rolls/Sidesteps
            Shield.Main.AddTransition(EscapeForward, Input(input => input.Movement.x > inputThreshold))
                .AddTransition(EscapeBackward, Input(input => input.Movement.x < -inputThreshold))
                .AddTransition(Escape, Input(input => input.Movement.y < -inputThreshold));
            new[] {Escape, EscapeForward, EscapeBackward}.AddTransitionTo(Shield.Main);

            // Returning to Ground
            new[] {Fall, FallHelpless}.AddTransitions(Land, ctx => ctx.IsGrounded);

            new[] {Land,
                   Neutral,
                   TiltUp, TiltDown, TiltSide,
                   SmashUp.Attack, SmashDown.Attack, SmashSide.Attack,
                   RunBrake,
                   Shield.Off, Shield.Stunned}
                .AddTransitionTo(Idle);

            new[] {Dash, RunTurn}.AddTransitionTo(Run);


            JumpStart.AddTransitionTo(Jump);
            EscapeAir.AddTransitionTo(FallHelpless);

            Builder.WithDefaultState(Idle);
            BuildCharacterController();
            return Builder.Build();
        }

        protected virtual void BuildCharacterController() {
            Crouch.AddTransitionTo(TiltDown);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {
            if (_dataMap == null)
                return;
            _data = _dataMap.Select(kvp => new StateData {Name = kvp.Key, Data = kvp.Value}).ToArray();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            if (_data == null)
                return;
            _dataMap = _data.ToDictionary(s => s.Name, s => s.Data);
        }

    }

}
