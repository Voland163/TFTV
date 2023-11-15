using System;
using Base.Defs;
using UnityEngine;
using Base.Entities.Effects;

namespace TFTVVehicleRework.Effects
{
    public class AddedRotationParticleEffect : ParticleEffect
    {
        public AddedRotationParticleEffectDef AddedRotationParticleEffectDef
        {
            get
            {
                return this.Def<AddedRotationParticleEffectDef>();
            }
        }
        protected override void OnApply(EffectTarget target)
        {
            base.OnApply(target);
            Transform transform = this.ParticleSystem.transform;
            transform.RotateAround(transform.position, GetRotationAxis(transform), this.AddedRotationParticleEffectDef.RotationDegrees);
        }

        private Vector3 GetRotationAxis(Transform transform)
        {
            if(this.AddedRotationParticleEffectDef.UseTransformOrientation)
            {
                switch(this.AddedRotationParticleEffectDef.RotationDirection)
                {
                    case AddedRotationParticleEffectDef.Direction.X:
                        return transform.right;
                    case AddedRotationParticleEffectDef.Direction.Y:
                        return transform.up;
                    case AddedRotationParticleEffectDef.Direction.Z:
                        return transform.forward;
                }
            }
            return this.AddedRotationParticleEffectDef.RotationAxis;
        }
    }
}