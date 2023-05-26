using System;
using System.Linq;
using UnityEngine;

namespace HRCounter.Utils
{
    public static class RenderUtils
    {
        private static Lazy<Material> _uiNoGlow =
            new Lazy<Material>(() => Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(x => x.name == "UINoGlow"));

        private static Lazy<Shader> _textNoGlow = new Lazy<Shader>(() => Shader.Find("TextMeshPro/Mobile/Distance Field Zero Alpha Write"));

        public static Material UINoGlow => _uiNoGlow.Value;

        public static Shader TextNoGlow => _textNoGlow.Value;
    }
}