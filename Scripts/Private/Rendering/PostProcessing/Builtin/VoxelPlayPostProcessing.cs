using UnityEngine;

namespace VoxelPlay {

    [ExecuteAlways]
    public class VoxelPlayPostProcessing : MonoBehaviour {

        Material mat;

        const string m_ShaderName = "Hidden/VoxelPlay/VoxelPlayPostProcessingBuiltin";

        public static bool isActive;

        private void OnEnable() {
            if (mat == null) {
                mat = new Material(Shader.Find(m_ShaderName));
            }
            isActive = true;
        }

        private void OnDisable() {
            isActive = false;
        }

        private void OnDestroy() {
            if (mat != null) DestroyImmediate(mat);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination) {
            Graphics.Blit(source, destination, mat);
            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env != null && !env.usePostProcessing) enabled = false;
        }

    }


}