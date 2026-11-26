using UnityEngine;

namespace RockyLeague.Core.Building
{
    /// <summary>
    /// Creates simple shapes (cubes, spheres, cylinders) as children of one parent.
    /// Both the arena and the scenery are built by code from these pieces, so the
    /// "create a primitive, place it, paint it" steps live in a single place (DRY).
    /// Plain C# class: whoever builds owns one and calls it while building.
    /// </summary>
    public sealed class BlockBuilder
    {
        private readonly Transform _parent;

        public BlockBuilder(Transform parent)
        {
            _parent = parent;
        }

        /// <summary>A solid box that things can collide with (walls, floor).</summary>
        public GameObject Solid(string name, Vector3 localCenter, Vector3 size, Material material, PhysicsMaterial physics)
        {
            GameObject block = Create(PrimitiveType.Cube, name, localCenter, size, Quaternion.identity, material);
            block.GetComponent<Collider>().sharedMaterial = physics;
            return block;
        }

        /// <summary>A box that is only seen, never touched (lines, trims, scenery).</summary>
        public GameObject Decoration(string name, Vector3 localCenter, Vector3 size, Material material)
        {
            return Decoration(PrimitiveType.Cube, name, localCenter, size, Quaternion.identity, material);
        }

        /// <summary>Any primitive shape that is only seen, never touched.</summary>
        public GameObject Decoration(PrimitiveType shape, string name, Vector3 localCenter, Vector3 size, Quaternion localRotation, Material material)
        {
            GameObject block = Create(shape, name, localCenter, size, localRotation, material);
            Object.Destroy(block.GetComponent<Collider>());
            return block;
        }

        private GameObject Create(PrimitiveType shape, string name, Vector3 localCenter, Vector3 size, Quaternion localRotation, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(shape);
            block.name = name;
            block.transform.SetParent(_parent, false);
            block.transform.localPosition = localCenter;
            block.transform.localRotation = localRotation;
            block.transform.localScale = size;

            if (material != null)
            {
                block.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            return block;
        }
    }
}
