﻿using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    public class TextureArrayPacker {

        public TextureProviderSettings settings;
        public Texture2DArray textureArray;

        public struct WorldTexture {
            public Color32[] colorsAndEmission;
            public Color32[] normalsAndElevation;
        }

        /// <summary>
        /// List containing all textures added
        /// </summary>
        public List<WorldTexture> textures;

        /// <summary>
        /// Number of registered textures
        /// </summary>
        public int texturesCount => textures != null ? textures.Count : 0;

        /// <summary>
        /// Maximum textures allowed in a single texture array
        /// </summary>
        public int maxTextures => SystemInfo.maxTextureArraySlices;

        /// <summary>
        /// Returns true if this texture packer doesn't accept more textures
        /// </summary>
        public bool HasAvailableTextureSlices(int required) {
            return texturesCount + required <= maxTextures;
        }

        /// <summary>
        /// Dictionary for fast texture search
        /// </summary>
        Dictionary<int, int> texturesDict;

        Color32[] defaultMapColors, defaultPinkColors;
        Texture2D defaultTransparentTexture;


        public TextureArrayPacker(TextureProviderSettings settings) {
            this.settings = settings;
            Clear();
        }

        public void Clear() {
            if (textures == null) {
                textures = new List<WorldTexture>();
            } else {
                textures.Clear();
            }
            if (texturesDict == null) {
                texturesDict = new Dictionary<int, int>();
            } else {
                texturesDict.Clear();
            }
            if (textureArray != null) {
                Object.DestroyImmediate(textureArray);
            }
            if (defaultTransparentTexture != null) {
                Object.DestroyImmediate(defaultTransparentTexture);
            }
            textureArray = null;
        }

        public Texture2D GetDefaultTransparentTexture() {
            if (defaultTransparentTexture == null) {
                defaultTransparentTexture = new Texture2D(settings.textureSize, settings.textureSize, TextureFormat.ARGB32, false);
                defaultTransparentTexture.name = "DefaultTransparentTexture";
                defaultTransparentTexture.hideFlags = HideFlags.DontSave;
                Color32[] colors = new Color32[settings.textureSize * settings.textureSize];
                defaultTransparentTexture.SetPixels32(colors);
                defaultTransparentTexture.Apply();
            }
            return defaultTransparentTexture;
        }

        /// <summary>
        /// Returns the index in the texture list and the full index (index in the list + some flags specifying existence of normal/displacement maps)
        /// </summary>
        public int AddTexture(Texture2D texAlbedo, Texture2D texEmission, Texture2D texNRM, Texture2D texDISP, bool avoidRepetitions = true, bool ignoreAlpha = false) {

            if (texAlbedo == null) return 0;

            int index;
            if (avoidRepetitions) {
                int textureHash = texAlbedo.GetHashCode();
                if (texNRM != null) {
                    textureHash ^= (texNRM.GetHashCode() << 2);
                }
                if (texturesDict.TryGetValue(textureHash, out index)) {
                    return index;
                }
                index = textures.Count;
                texturesDict[textureHash] = index;
            } else {
                index = textures.Count;
            }

            // Add entry to dictionary

            // Albedo + Emission mask
            WorldTexture wt = new WorldTexture();
            wt.colorsAndEmission = CombineAlbedoAndEmission(texAlbedo, ignoreAlpha, texEmission);
            textures.Add(wt);

            // Normal + Elevation Map
            if (settings.enableNormalMap || settings.enableReliefMap) {
                WorldTexture wextra = new WorldTexture();
                wextra.normalsAndElevation = CombineNormalsAndElevation(texNRM, texDISP);
                textures.Add(wextra);
            }

            textureArray = null;

            return index;
        }


        Color32[] CombineAlbedoAndEmission(Texture2D albedoMap, bool ignoreAlpha, Texture2D emissionMap = null) {
            Color32[] mapColors;
            if (albedoMap == null) {
                return GetPinkColors();
            }
            if (albedoMap.width != settings.textureSize) {
                mapColors = TextureTools.ScaleTextureColors(albedoMap, settings.textureSize, settings.textureSize, FilterMode.Point);
            } else {
                mapColors = albedoMap.GetPixels32();
            }
            if (emissionMap == null) {
                if (ignoreAlpha) {
                    int len = mapColors.Length;
                    for (int k = 0; k < len; k++) {
                        mapColors[k].a = 255;
                    }
                }
                return mapColors;
            }
            Color32[] emissionColors;
            if (emissionMap.width != settings.textureSize) {
                emissionColors = TextureTools.ScaleTextureColors(emissionMap, settings.textureSize, settings.textureSize, FilterMode.Point);
            } else {
                emissionColors = emissionMap.GetPixels32();
            }
            for (int k = 0; k < mapColors.Length; k++) {
                mapColors[k].a = (byte)(255 - emissionColors[k].r);
            }
            return mapColors;
        }


        Color32[] CombineNormalsAndElevation(Texture2D normalMap, Texture2D elevationMap) {
            if (elevationMap == null && normalMap == null) {
                return GetDefaultMapColors();
            }
            Color32[] normalMapColors, elevationMapColors;
            if (normalMap == null) {
                normalMapColors = GetDefaultMapColors();
            } else if (normalMap.width != settings.textureSize) {
                normalMapColors = TextureTools.ScaleTextureColors(normalMap, settings.textureSize, settings.textureSize, FilterMode.Point);
            } else {
                normalMapColors = normalMap.GetPixels32();
            }
            if (elevationMap == null) {
                elevationMapColors = GetDefaultMapColors();
            } else if (elevationMap.width != settings.textureSize) {
                elevationMapColors = TextureTools.ScaleTextureColors(elevationMap, settings.textureSize, settings.textureSize, FilterMode.Point);
            } else {
                elevationMapColors = elevationMap.GetPixels32();
            }

            // detect dxt compression (has r = 255)
            int normalMapColorsLength = normalMapColors.Length;
            if (Application.isMobilePlatform || normalMapColors[0].r != 255) {
                // copy elevation into alpha channel of normal map to save 1 texture slot in texture array and optimize cache
                for (int k = 0; k < normalMapColorsLength; k++) {
                    normalMapColors[k].a = elevationMapColors[k].r;
                }
            } else {
                for (int k = 0; k < normalMapColorsLength; k++) {
                    // in dxt5nrm format, r is stored in the alpha channel so we move it back to r
                    normalMapColors[k].r = normalMapColors[k].a;
                    // reconstruct z (blue) from x & y
                    float x = (normalMapColors[k].r / 255f) * 2f - 1f;
                    float y = (normalMapColors[k].g / 255f) * 2f - 1f;
                    float t = x * x - y * y;
                    if (t < 0) t = 0; else if (t > 1f) t = 1f;
                    t = 1f - t;
                    t = (float)System.Math.Sqrt(t);
                    normalMapColors[k].b = (byte)((t * 0.5f + 0.5) * 255);
                    // copy elevation into alpha channel of normal map to save 1 texture slot in texture array and optimize cache
                    normalMapColors[k].a = elevationMapColors[k].r;   // copy elevation into alpha channel of normal map to save 1 texture slot in texture array and optimize cache
                }
            }
            return normalMapColors;
        }


        Color32[] GetPinkColors() {
            int len = settings.textureSize * settings.textureSize;
            if (defaultPinkColors != null && defaultPinkColors.Length == len) {
                return defaultPinkColors;
            }
            defaultPinkColors = new Color32[len];
            Color32 color = new Color32(255, 0, 0x80, 255);
            defaultPinkColors.Fill(color);
            return defaultPinkColors;
        }


        Color32[] GetDefaultMapColors() {
            int len = settings.textureSize * settings.textureSize;
            if (defaultMapColors != null && defaultMapColors.Length == len) {
                return defaultMapColors;
            }
            defaultMapColors = new Color32[len];
            Color32 color = new Color32(0, 0, 255, 255);
            defaultMapColors.Fill(color);
            return defaultMapColors;
        }


        public void CreateTextureArray() {
            if (textureArray != null) return;

            int textureCount = this.texturesCount;
            if (textureCount == 0) return;

            textureArray = new Texture2DArray(settings.textureSize, settings.textureSize, textureCount, TextureFormat.ARGB32, settings.useMipmapping);
            if (settings.enableReliefMap || !settings.enableSmoothLighting) {
                textureArray.wrapMode = TextureWrapMode.Repeat;
            } else {
                textureArray.wrapMode = TextureWrapMode.Clamp;
            }
            textureArray.filterMode = settings.useMipmapping ? FilterMode.Bilinear : FilterMode.Point;
            textureArray.mipMapBias = -settings.mipMapBias;

            for (int k = 0; k < textureCount; k++) {
                WorldTexture wt = textures[k];
                if (wt.colorsAndEmission != null) {
                    textureArray.SetPixels32(wt.colorsAndEmission, k);
                } else if (textures[k].normalsAndElevation != null) {
                    textureArray.SetPixels32(wt.normalsAndElevation, k);
                }
            }
            textureArray.Apply(settings.useMipmapping, true);

        }
    }
}