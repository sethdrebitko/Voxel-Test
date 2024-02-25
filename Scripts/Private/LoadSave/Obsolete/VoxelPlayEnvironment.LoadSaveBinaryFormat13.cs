using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

namespace VoxelPlay {

    public partial class VoxelPlayEnvironment : MonoBehaviour {

        void LoadGameBinaryFileFormat_13(BinaryReader br, bool preservePlayerPosition = false) {
            // Character controller transform position & rotation
            Vector3 pos = DecodeVector3Binary(br);
            Vector3 characterRotationAngles = DecodeVector3Binary(br);
            Vector3 cameraLocalRotationAngles = DecodeVector3Binary(br);
            if (!preservePlayerPosition) {
                if ((UnityEngine.Object)characterController != null) {
                    characterController.MoveTo(pos);
                    characterController.transform.rotation = Quaternion.Euler(characterRotationAngles);
                    cameraMain.transform.localRotation = Quaternion.Euler(cameraLocalRotationAngles);
                    characterController.UpdateLook();
                }
            }

            InitSaveGameStructs();
            // Read voxel definition table
            int vdCount = br.ReadInt16();
            for (int k = 0; k < vdCount; k++) {
                VoxelDefinition vd = GetVoxelDefinition(br.ReadString());
                saveVoxelDefinitionsList.Add(vd);
            }
            // Read item definition table
            int idCount = br.ReadInt16();
            for (int k = 0; k < idCount; k++) {
                saveItemDefinitionsList.Add(br.ReadString());
            }

            int numChunks = br.ReadInt32();
            VoxelDefinition voxelDefinition = defaultVoxel;
            int prevVdIndex = -1;
            Color32 voxelColor = Misc.color32White;

            for (int c = 0; c < numChunks; c++) {
                // Read chunks
                // Get chunk position
                Vector3d chunkPosition = DecodeVector3Binary(br).ToVector3d();
                VoxelChunk chunk = GetChunkUnpopulated(chunkPosition);
                byte isAboveSurface = br.ReadByte();
                chunk.isAboveSurface = isAboveSurface == 1;
                chunk.allowTrees = false;
                chunk.modified = true;
                chunk.isPopulated = true;
                chunk.voxelSignature = -1;
                chunk.renderState = ChunkRenderState.Pending;
                SetChunkOctreeIsDirty(chunkPosition, false);
                ChunkClearFast(chunk);
                // Read voxels
                int numWords = br.ReadInt16();
                for (int k = 0; k < numWords; k++) {
                    // Voxel definition
                    int vdIndex = br.ReadInt16();
                    if (prevVdIndex != vdIndex) {
                        if (vdIndex >= 0 && vdIndex < vdCount) {
                            voxelDefinition = saveVoxelDefinitionsList[vdIndex];
                            prevVdIndex = vdIndex;
                        }
                    }
                    // RGB
                    voxelColor.r = br.ReadByte();
                    voxelColor.g = br.ReadByte();
                    voxelColor.b = br.ReadByte();
                    // Voxel index
                    int voxelIndex = br.ReadInt16();
                    // Repetitions
                    int repetitions = br.ReadInt16();
                    // Flags (rotation and water level)
                    byte flags = br.ReadByte();

                    if (voxelDefinition == null) {
                        continue;
                    }

                    for (int i = 0; i < repetitions; i++) {
                        chunk.SetVoxel(voxelIndex + i, voxelDefinition, voxelColor);
                        chunk.voxels[voxelIndex + i].SetFlags(flags);
                    }
                }
                // Torches
                int lightCount = br.ReadInt16();
                VoxelHitInfo hitInfo = new VoxelHitInfo();
                for (int k = 0; k < lightCount; k++) {
                    // Voxel index
                    hitInfo.voxelIndex = br.ReadInt16();
                    // Voxel center
                    hitInfo.voxelCenter = GetVoxelPosition(chunkPosition, hitInfo.voxelIndex);
                    // Normal
                    hitInfo.normal = DecodeVector3Binary(br);
                    hitInfo.chunk = chunk;
                    // Item definition
                    int itemIndex = br.ReadInt16();
                    if (itemIndex < 0 || itemIndex >= idCount)
                        continue;
                    string itemDefinitionName = saveItemDefinitionsList[itemIndex];
                    ItemDefinition itemDefinition = GetItemDefinition(itemDefinitionName);
                    TorchAttach(hitInfo, itemDefinition);
                }
                // Read items
                int itemCount = br.ReadInt16();
                for (int k = 0; k < itemCount; k++) {
                    // Voxel index
                    int itemIndex = br.ReadInt16();
                    if (itemIndex < 0 || itemIndex >= idCount)
                        continue;
                    string itemDefinitionName = saveItemDefinitionsList[itemIndex];
                    Vector3d itemPosition = DecodeVector3Binary(br).ToVector3d();
                    int quantity = br.ReadInt16();
                    ItemSpawn(itemDefinitionName, itemPosition, quantity);
                }
                // Load custom voxel properties
                int voxelsPropertiesCount = br.ReadInt16();
                for (int k = 0; k < voxelsPropertiesCount; k++) {
                    chunk.voxelsProperties = new FastHashSet<FastHashSet<VoxelProperty>>();
                    int voxelIndex = br.ReadInt16();
                    int voxelPropertiesCount = br.ReadInt16();
                    FastHashSet<VoxelProperty> properties = new FastHashSet<VoxelProperty>();
                    chunk.voxelsProperties[voxelIndex] = properties;
                    for (int i = 0; i < voxelPropertiesCount; i++) {
                        int propId = br.ReadInt32();
                        VoxelProperty prop;
                        prop.floatValue = br.ReadSingle();
                        prop.stringValue = br.ReadString();
                        properties[propId] = prop;
                    }
                }
            }

            // Destroy any object with VoxelPlaySaveThis component to avoid repetitions
            VoxelPlaySaveThis[] gos = Misc.FindObjectsOfType<VoxelPlaySaveThis>();
            for (int k = 0; k < gos.Length; k++) {
                DestroyImmediate(gos[k].gameObject);
            }

            // Load gameobjects
            int goCount = br.ReadInt16();
            Dictionary<string, string> data = new Dictionary<string, string>();
            for (int k = 0; k < goCount; k++) {
                string prefabPath = br.ReadString();
                string goName = br.ReadString();
                Vector3 goPosition = DecodeVector3Binary(br);
                Vector3 goAngles = DecodeVector3Binary(br);
                Vector3 goScale = DecodeVector3Binary(br);
                data.Clear();
                Int16 dataCount = br.ReadInt16();
                for (int j = 0; j < dataCount; j++) {
                    string key = br.ReadString();
                    string value = br.ReadString();
                    data[key] = value;
                }

                GameObject o = Resources.Load<GameObject>(prefabPath);
                if (o != null) {
                    o = Instantiate(o);
                    o.name = goName;
                    VoxelPlaySaveThis go = o.GetComponent<VoxelPlaySaveThis>();
                    if (go == null) {
                        DestroyImmediate(o);
                        continue;
                    }
                    o.transform.position = goPosition;
                    o.transform.eulerAngles = goAngles;
                    o.transform.localScale = goScale;
                    go.SendMessage("OnLoadGame", data, SendMessageOptions.DontRequireReceiver);
                }
            }

            // Read number of custom sections
            int sectionsCount = br.ReadInt16();
            for (int k = 0; k < sectionsCount; k++) {
                string sectionName = br.ReadString();
                int length = br.ReadInt32();
                byte[] sectionData = br.ReadBytes(length);
                if (OnLoadCustomGameData != null) {
                    sectionData = SaveGameCustomDataWriter.Decompress(sectionData);
                    OnLoadCustomGameData(sectionName, sectionData);
                }
            }

        }

    }



}
