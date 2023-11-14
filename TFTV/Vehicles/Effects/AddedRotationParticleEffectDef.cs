using System;
using Base.Defs;
using Base.Entities.Effects;
using UnityEngine;

namespace TFTVVehicleRework.Effects
{
    [CreateAssetMenu(fileName = "AddedRotationParticleEffectDef", menuName = "Defs/Effects/AddedRotationParticleEffect")]
    public class AddedRotationParticleEffectDef : ParticleEffectDef
    {
        public Vector3 RotationAxis;
        public float RotationDegrees;
        public bool UseTransformOrientation;
        public Direction RotationDirection;
        public enum Direction
        {
            [Header("Right")]
            X,
            [Header("Up")]
            Y,
            [Header("Forward")]
            Z
        }
    }
}