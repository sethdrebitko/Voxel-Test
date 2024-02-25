﻿using System;
using UnityEngine;

namespace VoxelPlay {

	[HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000027332-detail-generators")]
	public abstract class VoxelPlayDetailGenerator : ScriptableObject {

		public bool enabled = true;

        [Tooltip("Set to true to allow multiple nested calls to AddDetail method. This can occur if the AddDetail method triggers the creation of nearby chunks which in turn invokes the detail generator.")]
		public bool allowNestedExecutions;

		[NonSerialized]
		public int detailGeneratorIndex;

		[NonSerialized]
		public bool busy;

		protected const int ONE_Y_ROW = VoxelPlayEnvironment.CHUNK_SIZE * VoxelPlayEnvironment.CHUNK_SIZE;
		protected const int ONE_Z_ROW = VoxelPlayEnvironment.CHUNK_SIZE;

		/// <summary>
		/// Initialization method. Called by Voxel Play at startup.
		/// </summary>
		public virtual void Init() { }


		/// <summary>
		/// Called by Voxel Play to inform that player has moved onto another chunk so new detail can start generating
		/// </summary>
		/// <param name="currentPosition">Current player position.</param>
		/// <param name="checkOnlyBorders">True means the player has moved to next chunk. False means player position is completely new and all chunks in range should be checked for detail in this call.</param>
		/// <param name="endTime">Provides a maximum time frame for execution this frame. Compare this with env.stopwatch milliseconds.</param>
		/// <returns><c>true</c>, if there's more work to be executed, <c>false</c> otherwise.</returns>
		public virtual bool ExploreArea(Vector3d currentPosition, bool checkOnlyBorders, long endTime) { return false; }

		/// <summary>
		/// Called by Voxel Play so detail can be computed incrementally so detail info is ready when needed (retrieved by GetDetail method)
		/// At runtime this method will be called in a specific thread so Unity API cannot be used.
		/// This method should not produce spikes nor heavy computation in a single frame.		
		/// </summary>
		/// <param name="endTime">Provides a maximum time frame for execution this frame. Compare this with env.stopwatch milliseconds.</param>
		/// <returns><c>true</c>, if there's more work to be executed, <c>false</c> otherwise.</returns>
		public virtual bool DoWork(long endTime) { return false; }

		/// <summary>
		/// Fills the given chunk with detail. Filled voxels won't be replaced by the terrain generator.
		/// Use Voxel.Empty to fill with void.
		/// </summary>
		public virtual void AddDetail(VoxelChunk chunk) { return; }


		/// <summary>
		/// Call this method from your DoWork() / AddDetail() code if you modify the chunk contents to ensure world is updated accordingly
		/// </summary>
		public void SetChunkIsDirty(VoxelChunk chunk) {
			if (chunk.isPopulated) {
				// if this detail generator has modified a fully generated chunk, we need to refresh it completely
				VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
				env.ChunkRedraw (chunk, includeNeighbours: true, refreshLightmap: true, refreshMesh: true);
			} else {
				// otherwise, just inform that this chunk has changes so the rest of pipeline (during CreateChunk) ensures the lightmap and mesh is rebuilt
				chunk.isDirty = true;
			}
		}

	}

}