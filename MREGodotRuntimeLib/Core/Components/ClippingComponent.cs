// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Godot;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.GodotHelper;

namespace MixedRealityExtension.Core.Components
{
	internal class ClippingComponent : ActorComponentBase
	{
		private List<MeshInstance> meshInstances = new List<MeshInstance>();
		private static Vector3 Vector3Half = Vector3.One * 0.5f;
		public AABB Bounds =>new AABB(GlobalTransform.origin - GlobalTransform.basis.Scale / 2, GlobalTransform.basis.Scale);

		internal void ApplyPatch(ClippingPatch patch)
		{
			ClearMeshInstances();
			if (patch.ClippingObjects != null)
			{
				foreach (var clippingObjectId in patch.ClippingObjects)
				{
					Spatial targetActor = AttachedActor.App.FindActor(clippingObjectId) as Spatial;
					AddMeshInstance(targetActor);
				}
			}
		}

		private IEnumerable<ShaderMaterial> ShaderMaterials()
		{
			foreach (var meshInstance in meshInstances)
			{
				if (!meshInstance.IsVisibleInTree()) continue;
				if (meshInstance.MaterialOverride is ShaderMaterial shaderMaterial)
					yield return shaderMaterial;
				else if (meshInstance.Mesh != null)
				{
					var materialCount = meshInstance.Mesh.GetSurfaceCount();
					for (int i = 0; i < materialCount; i++)
					{
						var material = meshInstance.Mesh.SurfaceGetMaterial(i);
						if (material is ShaderMaterial meshMaterial)
							yield return meshMaterial;
					}
				}
				else
				{
					var materialCount = meshInstance.GetSurfaceMaterialCount();
					for (int i = 0; i < materialCount; i++)
					{
						var material = meshInstance.GetSurfaceMaterial(i);
						if (material is ShaderMaterial meshInstanceMaterial)
							yield return meshInstanceMaterial;
					}
				}
			}
		}

		public override void _Process(float delta)
		{
			var globalTransform = GlobalTransform;
			globalTransform.basis = globalTransform.basis.Scaled(Vector3Half);
			var affineInverse = globalTransform.AffineInverse();
			foreach (var shaderMaterial in ShaderMaterials())
			{
				shaderMaterial.SetShaderParam("clipBoxInverseTransform", affineInverse);
			}
		}

		public void ClearMeshInstances()
		{
			foreach (var shaderMaterial in ShaderMaterials())
			{
				// clear inverse transform matrix
				shaderMaterial.SetShaderParam("clipBoxInverseTransform", null);
			}
			meshInstances.Clear();
		}

		public void AddMeshInstance(Spatial root)
		{
			MWGOTreeWalker.VisitTree(root, node =>
			{
				if (node is MeshInstance meshInstance)
				{
					meshInstances.Add(meshInstance);
				}
			});
		}

		public void RemoveMeshInstance(Spatial root)
		{
			MWGOTreeWalker.VisitTree(root, node =>
			{
				if (node is MeshInstance meshInstance)
				{
					meshInstances.Remove(meshInstance);
				}
			});
		}

		public IEnumerable<MeshInstance> GetNodesCopy()
		{
			return new List<MeshInstance>(meshInstances);
		}
	}
}