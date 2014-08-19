﻿using System; using ComponentBind;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Lemma.Components;
using System.IO;
using System.Xml.Serialization;

namespace Lemma.Factories
{
	public class ParticleEmitterFactory : Factory<Main>
	{
		public ParticleEmitterFactory()
		{
			this.Color = new Vector3(0.4f, 1.0f, 0.4f);
		}

		public override Entity Create(Main main)
		{
			return new Entity(main, "ParticleEmitter");
		}

		public override void Bind(Entity entity, Main main, bool creating = false)
		{
			Transform transform = entity.GetOrCreate<Transform>("Transform");

			ParticleEmitter emitter = entity.GetOrCreate<ParticleEmitter>("ParticleEmitter");

			VoxelAttachable attachable = VoxelAttachable.MakeAttachable(entity, main);

			this.SetMain(entity, main);

			attachable.EditorProperties();

			emitter.Add(new Binding<Vector3>(emitter.Position, transform.Position));
			emitter.EditorProperties();
		}

		public override void AttachEditorComponents(Entity entity, Main main)
		{
			base.AttachEditorComponents(entity, main);
			Model editorModel = entity.Get<Model>("EditorModel");
			ParticleEmitter emitter = entity.Get<ParticleEmitter>();
			editorModel.Add(new Binding<bool>(editorModel.Enabled, () => !entity.EditorSelected || emitter.ParticleType.Value == null, entity.EditorSelected, emitter.ParticleType));

			VoxelAttachable.AttachEditorComponents(entity, main, editorModel.Color);
		}
	}
}