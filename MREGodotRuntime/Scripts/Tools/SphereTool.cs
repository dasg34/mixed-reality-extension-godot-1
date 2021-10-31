using Assets.Scripts.User;
using Godot;
using Godot.Collections;
using MixedRealityExtension.Core;
using Microsoft.MixedReality.Toolkit.Input;

namespace Assets.Scripts.Tools
{
	public class SphereTool : Tool
	{
		readonly RID shape = PhysicsServer.ShapeCreate(PhysicsServer.ShapeType.Sphere);
		readonly PhysicsShapeQueryParameters shapeQueryParameters;
		PhysicsDirectSpaceState spaceState;

		public float querySmoothingFactor;
		public float queryRadius = 0.05f;
		private Spatial currentGrabbable;
		private Spatial currentGrabbableActor;
		private Vector3 hitPoint;
		private bool isSelectPressed = false;

		public SphereTool()
		{
			shapeQueryParameters = new PhysicsShapeQueryParameters()
			{
				CollideWithAreas = true,
				CollideWithBodies = true,
				ShapeRid = shape,
				Margin = 0.04f
			};
		}

		public Vector3 GetNearGraspPoint(InputSource inputSource)
		{
			Spatial indexTip = inputSource.Hand.FindNode("IndexTip") as Spatial;
			Spatial thumbTip = inputSource.Hand.FindNode("ThumbTip") as Spatial;

			return 0.5f * (indexTip.GlobalTransform.origin + thumbTip.GlobalTransform.origin);
		}

		internal Spatial FindTarget(InputSource inputSource, out Vector3? hitPoint)
		{
			Spatial actor = null;
			float radius;
			hitPoint = GetNearGraspPoint(inputSource);
			this.hitPoint = (Vector3)hitPoint;

			if (currentGrabbable != null)
			{
				radius = queryRadius * (1 + querySmoothingFactor);
			}
			else
			{
				radius = queryRadius;
			}
			currentGrabbable = null;

			PhysicsServer.ShapeSetData(shape, radius);
			shapeQueryParameters.Transform = new Transform(Basis.Identity, this.hitPoint);
			spaceState = inputSource.GetWorld().DirectSpaceState;
			var intersectShapes = spaceState.IntersectShape(shapeQueryParameters);

			foreach (Dictionary intersectResult in intersectShapes)
			{
				var collider = (Spatial)intersectResult["collider"];
				NearInteractionGrabbable grabbable;

				for (actor = collider; actor != null; actor = actor.GetParent<Spatial>())
					if (actor is Actor) break;

				grabbable = FindBaseNearInteractionGrabbable(actor);

				NearInteractionGrabbable FindBaseNearInteractionGrabbable(Node node)
				{
					if (node is NearInteractionGrabbable baseNearInteractionTouchable)
						return baseNearInteractionTouchable;

					foreach (Node child in node.GetChildren())
					{
						var grabbableChild = FindBaseNearInteractionGrabbable(child);
						if (grabbableChild != null) return grabbableChild;
					}
					return null;
				}

				if (grabbable != null)
				{
					currentGrabbable = grabbable;
					break;
				}
			}

			currentGrabbableActor = actor;

			return actor;
		}

		protected override void UpdateTool(InputSource inputSource)
		{
			var handler = IMixedRealityEventHandler.FindEventHandler<IMixedRealityPointerHandler>(currentGrabbableActor);
			if (!isSelectPressed)
			{
				if (inputSource.PinchChaged)
				{
					if (inputSource.IsPinching)
					{
						isSelectPressed = true;
						handler?.OnPointerDown(new MixedRealityPointerEventData(this, hitPoint));
					}
					else
					{
						isSelectPressed = false;
						handler?.OnPointerUp(new MixedRealityPointerEventData(this, hitPoint));
					}
				}
			}
			else
			{
				handler?.OnPointerDragged(new MixedRealityPointerEventData(this, hitPoint));
			}
		}

		public override void CleanUp() { }
	}
}