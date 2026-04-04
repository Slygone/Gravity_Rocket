using UnityEngine;

namespace GravityRocket.VFX
{
    /// <summary>
    /// Static utility to control the chromatic aberration offset.
    /// The offset is set as a global shader property so any material using
    /// the GravityRocket/ChromaticAberration shader picks it up automatically.
    /// </summary>
    public static class ChromaticAberrationFeature
    {
        private static readonly int AberrationOffsetId = Shader.PropertyToID("_AberrationOffset");
        private static Vector2 _currentOffset;

        public static void SetOffset(Vector2 offset)
        {
            _currentOffset = offset;
            Shader.SetGlobalVector(AberrationOffsetId, offset);
        }

        public static Vector2 GetOffset()
        {
            return _currentOffset;
        }
    }
}
