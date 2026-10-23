using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KatarinaMod.Modules.LeagueSkins
{
    internal static class LeagueResources
    {
        private static string[] resourceNames;

        internal static byte[] Load(string folder, string file)
        {
            Assembly assembly = typeof(LeagueResources).Assembly;
            if (resourceNames == null) resourceNames = assembly.GetManifestResourceNames();

            string suffix = ".LeagueSkins." + folder + "." + file;
            string name = resourceNames.FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (name == null) return null;

            using (Stream stream = assembly.GetManifestResourceStream(name))
            using (MemoryStream memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        }
    }

    internal static class LeagueBytes
    {
        internal static Vector3 ReadVector3(byte[] d, int o)
        {
            return new Vector3(BitConverter.ToSingle(d, o), BitConverter.ToSingle(d, o + 4), BitConverter.ToSingle(d, o + 8));
        }

        internal static Quaternion ReadQuaternion(byte[] d, int o)
        {
            return new Quaternion(BitConverter.ToSingle(d, o), BitConverter.ToSingle(d, o + 4), BitConverter.ToSingle(d, o + 8), BitConverter.ToSingle(d, o + 12));
        }

        internal static string ReadString(byte[] d, int o, int max)
        {
            int end = o;
            while (end < d.Length && end < o + max && d[end] != 0) end++;
            return System.Text.Encoding.ASCII.GetString(d, o, end - o);
        }
    }

    internal class LeagueSkeleton
    {
        internal string[] names;
        internal int[] parents;
        internal Matrix4x4[] rest;
        internal ushort[] influences;
        internal Dictionary<string, int> index = new Dictionary<string, int>();

        internal static LeagueSkeleton Read(byte[] d)
        {
            uint magic = BitConverter.ToUInt32(d, 4);
            if (magic != 0x22FD4FC3) throw new InvalidDataException("Unsupported .skl format");

            int jointCount = BitConverter.ToUInt16(d, 14);
            int influenceCount = (int)BitConverter.ToUInt32(d, 16);
            int jointsOffset = BitConverter.ToInt32(d, 20);
            int influencesOffset = BitConverter.ToInt32(d, 28);

            LeagueSkeleton skeleton = new LeagueSkeleton
            {
                names = new string[jointCount],
                parents = new int[jointCount],
                rest = new Matrix4x4[jointCount],
                influences = new ushort[influenceCount]
            };

            for (int i = 0; i < jointCount; i++)
            {
                int o = jointsOffset + i * 100;
                skeleton.parents[i] = BitConverter.ToInt16(d, o + 4);

                Vector3 inverseTranslation = LeagueBytes.ReadVector3(d, o + 56);
                Vector3 inverseScale = LeagueBytes.ReadVector3(d, o + 68);
                Quaternion inverseRotation = LeagueBytes.ReadQuaternion(d, o + 80);
                skeleton.rest[i] = Matrix4x4.TRS(inverseTranslation, inverseRotation, inverseScale).inverse;

                int nameField = o + 96;
                int namePosition = nameField + BitConverter.ToInt32(d, nameField);
                skeleton.names[i] = LeagueBytes.ReadString(d, namePosition, 256);
                skeleton.index[skeleton.names[i]] = i;
            }

            for (int i = 0; i < influenceCount; i++)
            {
                skeleton.influences[i] = BitConverter.ToUInt16(d, influencesOffset + i * 2);
            }

            return skeleton;
        }
    }

    internal class LeagueMeshPart
    {
        internal string name;
        internal int vertexStart;
        internal int vertexCount;
        internal int indexStart;
        internal int indexCount;
    }

    internal class LeagueMesh
    {
        internal List<LeagueMeshPart> parts = new List<LeagueMeshPart>();
        internal ushort[] indices;
        internal Vector3[] positions;
        internal Vector3[] normals;
        internal Vector2[] uvs;
        internal byte[] boneIndices;
        internal float[] weights;

        internal static LeagueMesh Read(byte[] d)
        {
            if (BitConverter.ToUInt32(d, 0) != 0x00112233) throw new InvalidDataException("Unsupported .skn format");
            int major = BitConverter.ToUInt16(d, 4);
            if (major == 0) throw new InvalidDataException("Unsupported .skn version 0");

            LeagueMesh mesh = new LeagueMesh();
            int o = 8;
            int partCount = BitConverter.ToInt32(d, o);
            o += 4;
            for (int i = 0; i < partCount; i++)
            {
                mesh.parts.Add(new LeagueMeshPart
                {
                    name = LeagueBytes.ReadString(d, o, 64),
                    vertexStart = BitConverter.ToInt32(d, o + 64),
                    vertexCount = BitConverter.ToInt32(d, o + 68),
                    indexStart = BitConverter.ToInt32(d, o + 72),
                    indexCount = BitConverter.ToInt32(d, o + 76)
                });
                o += 80;
            }

            if (major == 4) o += 4;
            int indexCount = BitConverter.ToInt32(d, o);
            int vertexCount = BitConverter.ToInt32(d, o + 4);
            o += 8;

            int vertexSize = 52;
            if (major == 4)
            {
                vertexSize = BitConverter.ToInt32(d, o);
                o += 8 + 40;
            }

            mesh.indices = new ushort[indexCount];
            for (int i = 0; i < indexCount; i++) mesh.indices[i] = BitConverter.ToUInt16(d, o + i * 2);
            o += indexCount * 2;

            mesh.positions = new Vector3[vertexCount];
            mesh.normals = new Vector3[vertexCount];
            mesh.uvs = new Vector2[vertexCount];
            mesh.boneIndices = new byte[vertexCount * 4];
            mesh.weights = new float[vertexCount * 4];
            for (int v = 0; v < vertexCount; v++)
            {
                int p = o + v * vertexSize;
                mesh.positions[v] = LeagueBytes.ReadVector3(d, p);
                for (int k = 0; k < 4; k++)
                {
                    mesh.boneIndices[v * 4 + k] = d[p + 12 + k];
                    mesh.weights[v * 4 + k] = BitConverter.ToSingle(d, p + 16 + k * 4);
                }
                mesh.normals[v] = LeagueBytes.ReadVector3(d, p + 32);
                mesh.uvs[v] = new Vector2(BitConverter.ToSingle(d, p + 44), BitConverter.ToSingle(d, p + 48));
            }

            return mesh;
        }
    }

    internal static class LeagueTextures
    {
        private const int Dxt1 = 10;
        private const int Dxt5 = 12;
        private const int Bgra8 = 20;

        private static bool ReadHeader(byte[] d, out int width, out int height, out int format, out bool hasMips)
        {
            width = height = format = 0;
            hasMips = false;
            if (d == null || d.Length < 12 || d[0] != 'T' || d[1] != 'E' || d[2] != 'X') return false;
            width = BitConverter.ToUInt16(d, 4);
            height = BitConverter.ToUInt16(d, 6);
            format = d[9];
            hasMips = d[11] != 0;
            return format == Dxt1 || format == Dxt5 || format == Bgra8;
        }

        private static int LevelSize(int format, int width, int height)
        {
            if (format == Bgra8) return width * height * 4;
            int blocks = Math.Max(1, (width + 3) / 4) * Math.Max(1, (height + 3) / 4);
            return blocks * (format == Dxt1 ? 8 : 16);
        }

        private static List<int> LevelSizes(int format, int width, int height)
        {
            List<int> sizes = new List<int>();
            while (true)
            {
                sizes.Add(LevelSize(format, width, height));
                if (width == 1 && height == 1) break;
                width = Math.Max(1, width / 2);
                height = Math.Max(1, height / 2);
            }
            return sizes;
        }

        private static byte[] TopLevel(byte[] d, int format, int width, int height, bool hasMips)
        {
            int size = LevelSize(format, width, height);
            byte[] top = new byte[size];
            Array.Copy(d, hasMips ? d.Length - size : 12, top, 0, size);
            return top;
        }

        internal static Texture2D Load(byte[] d, string name, out bool hasAlpha)
        {
            hasAlpha = false;
            if (!ReadHeader(d, out int width, out int height, out int format, out bool hasMips)) return null;

            TextureFormat unityFormat = format == Dxt1 ? TextureFormat.DXT1 : format == Dxt5 ? TextureFormat.DXT5 : TextureFormat.BGRA32;
            List<int> sizes = LevelSizes(format, width, height);
            bool fullChain = hasMips && sizes.Sum() == d.Length - 12;

            byte[] raw;
            if (fullChain)
            {
                raw = new byte[sizes.Sum()];
                int source = d.Length;
                int target = 0;
                foreach (int size in sizes)
                {
                    source -= size;
                    Array.Copy(d, source, raw, target, size);
                    target += size;
                }
            }
            else
            {
                raw = TopLevel(d, format, width, height, hasMips);
            }

            Texture2D texture = new Texture2D(width, height, unityFormat, fullChain);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.LoadRawTextureData(raw);
            texture.Apply(false, true);

            byte[] top = TopLevel(d, format, width, height, hasMips);
            hasAlpha = format == Dxt5 || format == Bgra8 || (format == Dxt1 && Dxt1HasAlpha(top));
            return texture;
        }

        private static bool Dxt1HasAlpha(byte[] data)
        {
            for (int o = 0; o + 8 <= data.Length; o += 8)
            {
                ushort c0 = BitConverter.ToUInt16(data, o);
                ushort c1 = BitConverter.ToUInt16(data, o + 2);
                if (c0 > c1) continue;
                uint bits = BitConverter.ToUInt32(data, o + 4);
                for (int i = 0; i < 16; i++)
                {
                    if (((bits >> (i * 2)) & 3) == 3) return true;
                }
            }
            return false;
        }

        internal static Sprite LoadIcon(byte[] d, string name)
        {
            if (!ReadHeader(d, out int width, out int height, out int format, out bool hasMips)) return null;
            Color32[] pixels = Decode(TopLevel(d, format, width, height, hasMips), format, width, height);

            int size = Math.Min(width, height);
            int left = (width - size) / 2;
            Color32[] square = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    square[(size - 1 - y) * size + x] = pixels[y * width + left + x];
                }
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.SetPixels32(square);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Color32 Color565(ushort c)
        {
            byte r = (byte)(((c >> 11) & 31) * 255 / 31);
            byte g = (byte)(((c >> 5) & 63) * 255 / 63);
            byte b = (byte)((c & 31) * 255 / 31);
            return new Color32(r, g, b, 255);
        }

        private static Color32 Mix(Color32 a, Color32 b, int wa, int wb)
        {
            int t = wa + wb;
            return new Color32((byte)((a.r * wa + b.r * wb) / t), (byte)((a.g * wa + b.g * wb) / t), (byte)((a.b * wa + b.b * wb) / t), 255);
        }

        private static Color32[] Decode(byte[] data, int format, int width, int height)
        {
            Color32[] pixels = new Color32[width * height];
            if (format == Bgra8)
            {
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(data[i * 4 + 2], data[i * 4 + 1], data[i * 4], data[i * 4 + 3]);
                return pixels;
            }

            int blockSize = format == Dxt1 ? 8 : 16;
            int blocksWide = Math.Max(1, (width + 3) / 4);
            int blocksHigh = Math.Max(1, (height + 3) / 4);
            Color32[] palette = new Color32[4];
            byte[] alphas = new byte[8];

            for (int by = 0; by < blocksHigh; by++)
            {
                for (int bx = 0; bx < blocksWide; bx++)
                {
                    int o = (by * blocksWide + bx) * blockSize;
                    ulong alphaBits = 0;
                    if (format == Dxt5)
                    {
                        alphas[0] = data[o];
                        alphas[1] = data[o + 1];
                        if (alphas[0] > alphas[1])
                        {
                            for (int i = 1; i < 7; i++) alphas[i + 1] = (byte)(((7 - i) * alphas[0] + i * alphas[1]) / 7);
                        }
                        else
                        {
                            for (int i = 1; i < 5; i++) alphas[i + 1] = (byte)(((5 - i) * alphas[0] + i * alphas[1]) / 5);
                            alphas[6] = 0;
                            alphas[7] = 255;
                        }
                        for (int i = 0; i < 6; i++) alphaBits |= (ulong)data[o + 2 + i] << (8 * i);
                        o += 8;
                    }

                    ushort c0 = BitConverter.ToUInt16(data, o);
                    ushort c1 = BitConverter.ToUInt16(data, o + 2);
                    palette[0] = Color565(c0);
                    palette[1] = Color565(c1);
                    if (c0 > c1 || format == Dxt5)
                    {
                        palette[2] = Mix(palette[0], palette[1], 2, 1);
                        palette[3] = Mix(palette[0], palette[1], 1, 2);
                    }
                    else
                    {
                        palette[2] = Mix(palette[0], palette[1], 1, 1);
                        palette[3] = new Color32(0, 0, 0, 0);
                    }
                    uint bits = BitConverter.ToUInt32(data, o + 4);

                    for (int py = 0; py < 4; py++)
                    {
                        for (int px = 0; px < 4; px++)
                        {
                            int x = bx * 4 + px;
                            int y = by * 4 + py;
                            if (x >= width || y >= height) continue;
                            int i = py * 4 + px;
                            Color32 color = palette[(bits >> (i * 2)) & 3];
                            if (format == Dxt5) color.a = alphas[(int)((alphaBits >> (i * 3)) & 7)];
                            pixels[y * width + x] = color;
                        }
                    }
                }
            }
            return pixels;
        }
    }
}
