﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Lemma.Components;
using Lemma.Util;
using BEPUphysics.Collidables;
using BEPUphysics.CollisionTests;

namespace Lemma.Factories
{
	public class EffectBlockFactory : Factory
	{
		public EffectBlockFactory()
		{
			this.Color = new Vector3(1.0f, 0.25f, 0.25f);
		}

		public override Entity Create(Main main)
		{
			Entity result = new Entity(main, "EffectBlock");

			Transform transform = new Transform();
			result.Add("Transform", transform);

			ModelInstance model = new ModelInstance();
			result.Add("Model", model);

			result.Add("Offset", new Property<Vector3> { Editable = false });
			result.Add("Lifetime", new Property<float> { Editable = false });
			result.Add("TotalLifetime", new Property<float> { Editable = true });
			result.Add("StartPosition", new Property<Vector3> { Editable = true });
			result.Add("StartOrientation", new Property<Matrix> { Editable = false });
			result.Add("TargetMap", new Property<Entity.Handle> { Editable = true });
			result.Add("TargetCoord", new Property<Map.Coordinate> { Editable = false });
			result.Add("TargetCellStateID", new Property<int> { Editable = true });
			result.Add("Scale", new Property<bool> { Editable = true, Value = true });

			return result;
		}

		public override void Bind(Entity result, Main main, bool creating = false)
		{
			result.CannotSuspend = true;
			Transform transform = result.Get<Transform>();
			ModelInstance model = result.Get<ModelInstance>();

			model.Add(new Binding<Matrix>(model.Transform, transform.Matrix));

			Property<bool> scale = result.GetProperty<bool>("Scale");
			Property<Vector3> start = result.GetProperty<Vector3>("StartPosition");
			start.Set = delegate(Vector3 value)
			{
				start.InternalValue = value;
				transform.Position.Value = value;
			};
			Property<Matrix> startOrientation = result.GetProperty<Matrix>("StartOrientation");
			Vector3 startEuler = Vector3.Zero;
			startOrientation.Set = delegate(Matrix value)
			{
				startOrientation.InternalValue = value;
				startEuler = Quaternion.CreateFromRotationMatrix(startOrientation).ToEuler();
				transform.Orientation.Value = value;
			};

			Property<Entity.Handle> map = result.GetProperty<Entity.Handle>("TargetMap");
			Property<Map.Coordinate> coord = result.GetProperty<Map.Coordinate>("TargetCoord");
			Property<int> stateId = result.GetProperty<int>("TargetCellStateID");

			Property<float> totalLifetime = result.GetProperty<float>("TotalLifetime");
			Property<float> lifetime = result.GetProperty<float>("Lifetime");
			
			result.Add(new Updater
			{
				delegate(float dt)
				{
					lifetime.Value += dt;

					float blend = lifetime / totalLifetime;

					if (map.Value.Target == null || !map.Value.Target.Active)
					{
						result.Delete.Execute();
						return;
					}

					Map m = map.Value.Target.Get<Map>();

					if (blend > 1.0f)
					{
						result.Delete.Execute();

						if (stateId != 0)
						{
							m.Fill(coord, WorldFactory.States[stateId]);
							m.Regenerate();
						}
						Sound.PlayCue(main, "BuildBlock", 1.0f, 0.06f);
					}
					else
					{
						if (scale)
							model.Scale.Value = new Vector3(blend);
						else
							model.Scale.Value = new Vector3(1.0f);
						Matrix finalOrientation = m.Transform;
						finalOrientation.Translation = Vector3.Zero;
						Vector3 finalEuler = Quaternion.CreateFromRotationMatrix(finalOrientation).ToEuler();
						finalEuler = Vector3.Lerp(startEuler, finalEuler, blend);
						transform.Orientation.Value = Matrix.CreateFromYawPitchRoll(finalEuler.X, finalEuler.Y, finalEuler.Z);

						Vector3 finalPosition = m.GetAbsolutePosition(coord);
						float distance = (finalPosition - start).Length() * 0.1f * Math.Max(0.0f, 0.5f - Math.Abs(blend - 0.5f));

						transform.Position.Value = Vector3.Lerp(start, finalPosition, blend) + new Vector3((float)Math.Sin(blend * Math.PI) * distance);
					}
				},
			});

			this.SetMain(result, main);
			IBinding offsetBinding = null;
			model.Add(new NotifyBinding(delegate()
			{
				if (offsetBinding != null)
					model.Remove(offsetBinding);
				offsetBinding = new Binding<Vector3>(model.GetVector3Parameter("Offset"), result.GetProperty<Vector3>("Offset"));
				model.Add(offsetBinding);
			}, model.FullInstanceKey));
		}
	}
}