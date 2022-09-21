﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.GodotHelper;
using Godot;

namespace MixedRealityExtension.Animation
{
	internal partial class NativeAnimation : BaseAnimation
	{
		private AnimationPlayer animationPlayer;
		private Godot.Animation animation;
		private NativeAnimationHelper helper;

		public override string Name
		{
			get => animation.ResourceName.Replace("0x3A", ":");
			protected set { animation.ResourceName = value; }
		}
		public override bool IsPlaying
		{
			protected set
			{
				base.IsPlaying = value;
				if (WrapMode == MWAnimationWrapMode.Once && Time == 0)
				{
					helper.EmitSignal("animation_finished", animation.ResourceName);
				}
			}
		}
		public override long BasisTime
		{
			get
			{
				if (IsPlaying && Speed != 0)
					return manager.ServerNow() - (long)Mathf.Floor(Time * 1000 / Speed);
				else
					return 0;
			}
			protected set
			{
				Time = (manager.ServerNow() - value) * Speed / 1000.0f;
			}
		}

		public override float Time
		{
			get => animationPlayer.IsPlaying() ? animationPlayer.CurrentAnimationPosition : 0.0f;
			protected set
			{
				animationPlayer.Seek(value, true);
			}
		}

		public override float Speed
		{
			get => animationPlayer.PlaybackSpeed;
			protected set
			{
				animationPlayer.PlaybackSpeed = value;
			}
		}

		public override float Weight
		{
			get => animationPlayer.IsPlaying() ? 1f : 0f;
			protected set
			{
				if (value > 0)
				{
					if (WrapMode == MWAnimationWrapMode.Once &&
						(Speed < 0 && Time <= 0 || Speed > 0 && Time == animation.Length))
					{
						animationPlayer.Seek(Time, true);
						MarkFinished(Time);
					}
					else
						animationPlayer.Play();
				}
				else
					animationPlayer.Stop();
			}
		}

		public override MWAnimationWrapMode WrapMode
		{
			get
			{
				switch (animation.LoopMode)
				{
					case Godot.Animation.LoopModeEnum.None:
						return MWAnimationWrapMode.Once;
					case Godot.Animation.LoopModeEnum.Linear:
						return MWAnimationWrapMode.Loop;
					case Godot.Animation.LoopModeEnum.Pingpong:
						return MWAnimationWrapMode.PingPong;
				}
				return MWAnimationWrapMode.Once;
			}
			protected set
			{
				switch (value)
				{
					case MWAnimationWrapMode.Loop:
						animation.LoopMode = Godot.Animation.LoopModeEnum.Linear;
						break;
					case MWAnimationWrapMode.PingPong:
						animation.LoopMode = Godot.Animation.LoopModeEnum.Pingpong;
						break;
					default:
						animation.LoopMode = Godot.Animation.LoopModeEnum.None;
						break;
				}
			}
		}

		internal NativeAnimation(AnimationManager manager, Guid id, Godot.AnimationPlayer animationPlayer, Godot.Animation animation) : base(manager, id)
		{
			this.animationPlayer = animationPlayer;
			this.animation = animation;


			helper = new NativeAnimationHelper();
			helper.Animation = this;
			animationPlayer.AddChild(helper);
		}

		internal override void OnDestroy()
		{
			helper.QueueFree();
		}

		public override AnimationPatch GeneratePatch()
		{
			var patch = base.GeneratePatch();
			patch.Duration = animation.Length;
			return patch;
		}

		private partial class NativeAnimationHelper : Node3D
		{
			public NativeAnimation Animation;

			public override void _Ready()
			{
				Animation.animationPlayer.Connect("animation_finished", new Callable(this, nameof(AnimationEndReached)));
			}

			private void AnimationEndReached(string animationString)
			{
				var animation = Animation.animationPlayer.GetAnimation(animationString);
				var time = Animation.animationPlayer.CurrentAnimationPosition;
				if (Animation.WrapMode == MWAnimationWrapMode.Once &&
					(Animation.Speed < 0 && time <= 0 || Animation.Speed > 0 && time == animation.Length))
				{
					Animation.MarkFinished(time);
				}
			}
		}
	}
}
