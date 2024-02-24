using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using JetBrains.Annotations;

namespace cdep
{
    public class cdepResources : MonoBehaviour
    {
        private static Texture2D ParseDepth(byte[] rawFile, int width, int height)
        {
            // Ensure the byte array length is a multiple of 4 (size of a float)
            if (rawFile.Length % 4 != 0)
            {
                throw new FormatException("Byte array length must be a multiple of 4");
            }

            // Initialize float array
            float[] floatArray = new float[rawFile.Length / 4];

            // Convert bytes to floats
            for (int j = 0; j < rawFile.Length; j += 4)
            {
                floatArray[j / 4] = BitConverter.ToSingle(rawFile, j);
            }

            Color[] colors = new Color[width * height];
            for (int j = 0; j < floatArray.Length; j++)
            {
                float val = floatArray[j];
                colors[floatArray.Length - j - 1] = new Color(val, val, val);
            }

            Texture2D depthLoadTexture = new Texture2D(width, height, TextureFormat.RFloat, false); //mock size 1x1
            depthLoadTexture.SetPixels(colors);
            depthLoadTexture.Apply();
            return depthLoadTexture;
        }

        public static Capture[] InitializeOdsTextures(string file_name, Vector3[] positions, int count)
        {
            Capture[] caps = new Capture[count];
            for (int i = 0; i < count; i++)
            {
                caps[i] = new Capture();
                Texture2D color = new Texture2D(1, 1); //mock size 
                // Load from file path and save as texture - color
                string textureImagePath = file_name + "_" + (i + 1) + ".png";
                byte[] bytes = File.ReadAllBytes(textureImagePath);
                color.LoadImage(bytes);
                caps[i].image = color;

                // Load from file path to texture asset - depth
                string depthImagePath = file_name + "_" + (i + 1) + ".depth";
                byte[] depthBytes = File.ReadAllBytes(depthImagePath);
                caps[i].depth = ParseDepth(depthBytes, color.width, color.height);
                caps[i].position = new Vector3(positions[i].x, -positions[i].y, positions[i].z);
            }
            return caps;
        }

        public static Capture[] InitializeOdsTextures(string folderPath, int imagesToLoad = -1)
        {
            CaptureData[] data;
            try
            {
                string file = File.ReadAllText(folderPath + "/captures.json");
                data = JsonConvert.DeserializeObject<CaptureData[]>(file);
            }catch (FileNotFoundException e) { 
                Debug.LogError(e.Message);
                return new Capture[0];
            }
            int len;
            if(imagesToLoad == -1)
            {
                len = data.Length;
            }
            else
            {
                len = Math.Min(data.Length, imagesToLoad);
            }
            Capture[] caps = new Capture[len];
            for (int i = 0; i < caps.Length; i++)
            {
                caps[i] = new Capture();
                Texture2D color = new Texture2D(1, 1); //mock size 1x1
                // Load from file path and save as texture - color
                string textureImagePath = folderPath + '/' + data[i].colorPath;
                byte[] bytes = File.ReadAllBytes(textureImagePath);
                color.LoadImage(bytes);
                caps[i].image = color;

                // Load from file path to texture asset - depth
                string depthImagePath = folderPath + '/' + data[i].depthPath;
                byte[] depthBytes = File.ReadAllBytes(depthImagePath);
                caps[i].depth = ParseDepth(depthBytes, color.width, color.height);
                data[i].position.y *= -1;
                caps[i].position = data[i].position;
            }
            return caps;
        }

        public static void PrintJson(string file_name, Vector3[] positions, int count)
        {
            CaptureData[] datas = new CaptureData[count];
            for (int i = 0; i < count; i++)
            {
                datas[i] = new CaptureData();
                // Load from file path and save as texture - color
                datas[i].colorPath = file_name + "_" + (i + 1) + ".png";
                // Load from file path to texture asset - depth
                datas[i].depthPath = file_name + "_" + (i + 1) + ".depth";
                datas[i].position = positions[i];
            }
            string debug = JsonConvert.SerializeObject(datas, Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }
            );
            Debug.Log(debug);
        }
    }
    public class Capture
    {
        public Texture2D image;
        public Texture2D depth;
        public Vector3 position;
        public MeshGeneration meshGenScript;
    }

    public class CaptureData
    {
        public SerializableVec3 position;
        public string colorPath;
        public string depthPath;
    }

    public class SerializableVec3
    {
        public float x, y, z;
        public static implicit operator Vector3(SerializableVec3 vec) => new Vector3(vec.x, vec.y, vec.z);
        public static implicit operator SerializableVec3(Vector3 vec) => new SerializableVec3() {x = vec.x, y = vec.y, z = vec.z};
    }
}
