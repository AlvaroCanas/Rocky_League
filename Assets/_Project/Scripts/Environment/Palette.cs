using System.Collections.Generic;
using UnityEngine;

namespace RockyLeague.Environment
{
    /// <summary>
    /// Gives one shared material per colour, made as a tinted copy of a base material the
    /// first time that colour is asked for (Flyweight: hundreds of blocks, a handful of
    /// materials, which also lets Unity batch them). Materials made by code are not freed
    /// with the scene, so the owner calls <see cref="Release"/> when it is destroyed.
    /// </summary>
    public sealed class Palette
    {
        private readonly Material _baseMaterial;
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();

        public Palette(Material baseMaterial)
        {
            _baseMaterial = baseMaterial;
        }

        public Material Paint(Color colour)
        {
            if (!_materials.TryGetValue(colour, out Material material))
            {
                material = new Material(_baseMaterial);
                material.color = colour;
                material.name = $"{_baseMaterial.name} {ColorUtility.ToHtmlStringRGB(colour)}";
                _materials.Add(colour, material);
            }

            return material;
        }

        public void Release()
        {
            foreach (Material material in _materials.Values)
            {
                Object.Destroy(material);
            }

            _materials.Clear();
        }
    }
}
