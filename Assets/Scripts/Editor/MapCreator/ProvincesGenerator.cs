using System.Collections.Generic;
using LearnLanguages.Map.Data;
using LearnLanguages.Map.Logic;
using UnityEditor;
using UnityEngine;

namespace LearnLanguages.Editor.MapCreator
{
    /// <summary>
    /// Builds <see cref="ProvincesData"/> from the provinces image: one entry per color, neighbours,
    /// sea detection from the height map and sensible default positions.
    /// </summary>
    public sealed class ProvincesGenerator
    {
        private const float SeaHeightThreshold = 0.04f;
        private const int UnitOffsetPixels = 35;
        private const int MinProvincePixels = 1000;

        private static readonly string[] Syllables =
        {
            "ar", "bel", "cor", "dun", "el", "fal", "gar", "hal", "is", "kar", "lor", "mar", "nor", "or", "pel",
            "quin", "ros", "sal", "tor", "ul", "val", "wen", "yr", "zan", "bri", "dra", "fen", "mon", "ten", "vil"
        };

        public static void ConfigureTextureImport(Map.Data.Map map)
        {
            Configure(map.Provinces, false, FilterMode.Point);
            Configure(map.HeightMap, false, FilterMode.Bilinear);
            Configure(map.VisualLandscape, true, FilterMode.Bilinear);
        }

        private static void Configure(Texture2D texture, bool mipmaps, FilterMode filter)
        {
            if (texture == null) return;
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.mipmapEnabled = mipmaps;
            importer.filterMode = filter;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 8192;
            importer.sRGBTexture = texture != null && !AssetDatabase.GetAssetPath(texture).Contains("Height");
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Synchronises entries with the image. Existing entries keep their gameplay data;
        /// <paramref name="resetGameplay"/> rerolls landscape, settlements and spawn points for all provinces.
        /// </summary>
        public void Generate(Map.Data.Map map, bool resetGameplay)
        {
            var data = map.ProvincesData;
            var texture = map.Provinces;
            var width = texture.width;
            var height = texture.height;
            var pixels = texture.GetPixels32();

            var existing = new Dictionary<int, ProvinceDataEntry>();
            foreach (var entry in data.Entries) existing[ProvinceLookup.Pack(entry.MapColor)] = entry;

            var rawIds = BuildRawColors(pixels, width, height, out var rawColors, out var rawCounts, out var borders);
            var rawToProvince = MergeSlivers(rawColors.Count, rawCounts, borders);

            var entries = new List<ProvinceDataEntry>();
            var isNew = new List<bool>();
            var provinceOfRaw = new int[rawColors.Count];
            for (var raw = 0; raw < rawColors.Count; raw++)
            {
                if (rawToProvince[raw] != raw) continue;
                var key = ProvinceLookup.Pack(rawColors[raw]);
                provinceOfRaw[raw] = entries.Count;
                var known = existing.TryGetValue(key, out var old);
                entries.Add(known ? old : new ProvinceDataEntry(rawColors[raw]));
                isNew.Add(!known);
            }

            var aliases = new List<ColorAlias>();
            for (var raw = 0; raw < rawColors.Count; raw++)
            {
                if (rawToProvince[raw] == raw) continue;
                provinceOfRaw[raw] = provinceOfRaw[rawToProvince[raw]];
                aliases.Add(new ColorAlias(rawColors[raw], provinceOfRaw[raw]));
            }

            var ids = new int[pixels.Length];
            for (var i = 0; i < pixels.Length; i++) ids[i] = provinceOfRaw[rawIds[i]];

            var count = entries.Count;
            var pixelCount = new int[count];
            var heightSum = new float[count];
            var centroid = new Vector2[count];
            var heightPixels = map.HeightMap.GetPixels32();
            var neighbours = new HashSet<int>[count];
            for (var p = 0; p < count; p++) neighbours[p] = new HashSet<int>();

            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                var x = i % width;
                var y = i / width;
                pixelCount[id]++;
                centroid[id] += new Vector2(x, y);
                heightSum[id] += SampleHeight(heightPixels, map.HeightMap.width, map.HeightMap.height, x, y, width, height);
                if (x + 1 < width && ids[i + 1] != id) Link(neighbours, id, ids[i + 1]);
                if (y + 1 < height && ids[i + width] != id) Link(neighbours, id, ids[i + width]);
            }

            for (var p = 0; p < count; p++)
            {
                centroid[p] /= Mathf.Max(1, pixelCount[p]);
                var entry = entries[p];
                if (isNew[p] || resetGameplay)
                    entry.SetType(heightSum[p] / Mathf.Max(1, pixelCount[p]) < SeaHeightThreshold ? ProvinceType.Sea : ProvinceType.Land);
                var list = new List<int>(neighbours[p]);
                list.Sort();
                entry.SetNeighbours(list.ToArray());
            }

            PlacePositions(entries, ids, width, height);

            var random = new System.Random(map.Id.GetHashCode());
            for (var p = 0; p < count; p++)
            {
                if (!isNew[p] && !resetGameplay) continue;
                RollGameplay(entries[p], heightSum[p] / Mathf.Max(1, pixelCount[p]), random, p);
            }

            Undo.RecordObject(data, "Generate provinces");
            data.SetEntries(entries.ToArray());
            data.SetColorAliases(aliases.ToArray());
            if (resetGameplay || data.PossibleSpawnPoints.Count == 0) data.SetSpawnPoints(PickSpawnPoints(entries, centroid, pixelCount, 4));
            foreach (var spawn in data.PossibleSpawnPoints)
            {
                entries[spawn].SetSettlement(SettlementType.City);
                if (entries[spawn].Production == ProductionType.None) entries[spawn].SetProduction(ProductionType.Wheat);
            }
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        public static void GenerateIcon(Map.Data.Map map)
        {
            var source = map.VisualLandscape;
            const int iconWidth = 256;
            var iconHeight = Mathf.Max(1, Mathf.RoundToInt(iconWidth * (float)source.height / source.width));
            var icon = new Texture2D(iconWidth, iconHeight, TextureFormat.RGBA32, false);
            for (var y = 0; y < iconHeight; y++)
            for (var x = 0; x < iconWidth; x++)
                icon.SetPixel(x, y, source.GetPixelBilinear((x + 0.5f) / iconWidth, (y + 0.5f) / iconHeight));
            icon.Apply();

            var folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(map));
            var path = $"{folder}/{map.Id}_Icon.png";
            System.IO.File.WriteAllBytes(path, icon.EncodeToPNG());
            Object.DestroyImmediate(icon);
            AssetDatabase.ImportAsset(path);
            Undo.RecordObject(map, "Generate map icon");
            map.SetIcon(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
        }

        private static int[] BuildRawColors(Color32[] pixels, int width, int height, out List<Color32> colors,
            out List<int> counts, out Dictionary<long, int> borders)
        {
            var colorToRaw = new Dictionary<int, int>();
            colors = new List<Color32>();
            counts = new List<int>();
            borders = new Dictionary<long, int>();
            var raw = new int[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                var key = ProvinceLookup.Pack(pixels[i]);
                if (!colorToRaw.TryGetValue(key, out var id))
                {
                    id = colors.Count;
                    colorToRaw[key] = id;
                    colors.Add(pixels[i]);
                    counts.Add(0);
                }
                raw[i] = id;
                counts[id]++;
            }

            for (var i = 0; i < raw.Length; i++)
            {
                var x = i % width;
                if (x + 1 < width && raw[i + 1] != raw[i]) AddBorder(borders, raw[i], raw[i + 1]);
                if (i + width < raw.Length && raw[i + width] != raw[i]) AddBorder(borders, raw[i], raw[i + width]);
            }
            return raw;
        }

        private static void AddBorder(Dictionary<long, int> borders, int a, int b)
        {
            var key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            borders[key] = borders.TryGetValue(key, out var length) ? length + 1 : 1;
        }

        /// <summary>
        /// Colors covering fewer than <see cref="MinProvincePixels"/> pixels are drawing artifacts: each one joins the
        /// neighbour it shares the longest border with. Returns the representative raw color for every raw color.
        /// </summary>
        private static int[] MergeSlivers(int rawCount, List<int> counts, Dictionary<long, int> borders)
        {
            var target = new int[rawCount];
            for (var raw = 0; raw < rawCount; raw++)
            {
                target[raw] = raw;
                if (counts[raw] >= MinProvincePixels) continue;

                var best = -1;
                var bestLength = 0;
                foreach (var pair in borders)
                {
                    var a = (int)(pair.Key >> 32);
                    var b = (int)(pair.Key & 0xffffffff);
                    var other = a == raw ? b : b == raw ? a : -1;
                    if (other < 0 || counts[other] < MinProvincePixels || pair.Value <= bestLength) continue;
                    best = other;
                    bestLength = pair.Value;
                }
                if (best >= 0) target[raw] = best;
            }
            return target;
        }

        private static float SampleHeight(Color32[] heights, int heightWidth, int heightHeight, int x, int y, int width, int height)
        {
            var hx = Mathf.Min(heightWidth - 1, x * heightWidth / width);
            var hy = Mathf.Min(heightHeight - 1, y * heightHeight / height);
            return heights[hy * heightWidth + hx].r / 255f;
        }

        private static void Link(HashSet<int>[] neighbours, int a, int b)
        {
            neighbours[a].Add(b);
            neighbours[b].Add(a);
        }

        /// <summary>
        /// Settlement at the pole of inaccessibility (farthest point from the borders), the army spot a bit lower,
        /// the port on the coast closest to the settlement.
        /// </summary>
        private static void PlacePositions(List<ProvinceDataEntry> entries, int[] ids, int width, int height)
        {
            var count = entries.Count;
            var distance = new int[ids.Length];
            const int infinity = int.MaxValue / 4;
            for (var i = 0; i < ids.Length; i++)
            {
                var x = i % width;
                var y = i / width;
                var id = ids[i];
                var border = x == 0 || y == 0 || x == width - 1 || y == height - 1 || ids[i - 1] != id || ids[i + 1] != id ||
                             ids[i - width] != id || ids[i + width] != id;
                distance[i] = border ? 1 : infinity;
            }

            // Two-pass chamfer distance transform restricted to each province.
            for (var y = 1; y < height - 1; y++)
            for (var x = 1; x < width - 1; x++)
            {
                var i = y * width + x;
                var id = ids[i];
                Relax(distance, ids, i, i - 1, id, 3);
                Relax(distance, ids, i, i - width, id, 3);
                Relax(distance, ids, i, i - width - 1, id, 4);
                Relax(distance, ids, i, i - width + 1, id, 4);
            }
            for (var y = height - 2; y >= 1; y--)
            for (var x = width - 2; x >= 1; x--)
            {
                var i = y * width + x;
                var id = ids[i];
                Relax(distance, ids, i, i + 1, id, 3);
                Relax(distance, ids, i, i + width, id, 3);
                Relax(distance, ids, i, i + width + 1, id, 4);
                Relax(distance, ids, i, i + width - 1, id, 4);
            }

            var pole = new int[count];
            var best = new int[count];
            for (var p = 0; p < count; p++) pole[p] = -1;
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                if (pole[id] >= 0 && distance[i] <= best[id]) continue;
                best[id] = distance[i];
                pole[id] = i;
            }

            var unitScore = new float[count];
            var unitPixel = new int[count];
            var portScore = new float[count];
            var portPixel = new int[count];
            for (var p = 0; p < count; p++)
            {
                unitScore[p] = float.MinValue;
                portScore[p] = float.MaxValue;
                unitPixel[p] = pole[p];
                portPixel[p] = pole[p];
            }

            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                var x = i % width;
                var y = i / width;
                var poleX = pole[id] % width;
                var poleY = pole[id] / width;
                var dx = x - poleX;
                var dy = y - poleY;
                var fromPole = Mathf.Sqrt(dx * dx + dy * dy);

                // Army: below the settlement (closer to the camera), still well inside the province.
                var score = Mathf.Min(distance[i], 45) - Mathf.Abs(fromPole - UnitOffsetPixels) * 1.5f - Mathf.Max(0, dy) * 2f;
                if (score > unitScore[id])
                {
                    unitScore[id] = score;
                    unitPixel[id] = i;
                }

                if (distance[i] != 1 || entries[id].Type != ProvinceType.Land) continue;
                if (!TouchesSea(entries, ids, i, width, height)) continue;
                if (fromPole < portScore[id])
                {
                    portScore[id] = fromPole;
                    portPixel[id] = i;
                }
            }

            for (var p = 0; p < count; p++)
            {
                var poleCenter = ToPixel(pole[p], width);
                entries[p].SetSettlementLocalPos(poleCenter);
                entries[p].SetLocalUnitPosition(ToPixel(unitPixel[p], width));
                var port = ToPixel(portPixel[p], width);
                entries[p].SetPortLocalPos(Vector2.Lerp(port, poleCenter, 0.2f));
            }
        }

        private static void Relax(int[] distance, int[] ids, int i, int neighbour, int id, int weight)
        {
            if (ids[neighbour] != id) return;
            var candidate = distance[neighbour] + weight;
            if (candidate < distance[i]) distance[i] = candidate;
        }

        private static bool TouchesSea(List<ProvinceDataEntry> entries, int[] ids, int i, int width, int height)
        {
            var x = i % width;
            var y = i / width;
            return (x > 0 && entries[ids[i - 1]].Type == ProvinceType.Sea) ||
                   (x < width - 1 && entries[ids[i + 1]].Type == ProvinceType.Sea) ||
                   (y > 0 && entries[ids[i - width]].Type == ProvinceType.Sea) ||
                   (y < height - 1 && entries[ids[i + width]].Type == ProvinceType.Sea);
        }

        private static Vector2 ToPixel(int index, int width) => new Vector2(index % width + 0.5f, index / width + 0.5f);

        private static void RollGameplay(ProvinceDataEntry entry, float averageHeight, System.Random random, int id)
        {
            if (entry.Type == ProvinceType.Sea)
            {
                entry.SetName($"Sea {id}");
                entry.SetSettlement(SettlementType.None);
                entry.SetProduction(ProductionType.None);
                return;
            }

            entry.SetName(GenerateName(random));
            LandscapeType landscape;
            if (averageHeight > 0.45f) landscape = LandscapeType.Mountain;
            else if (averageHeight > 0.27f) landscape = LandscapeType.Hill;
            else
            {
                var roll = random.NextDouble();
                landscape = roll < 0.4 ? LandscapeType.Meadow : roll < 0.7 ? LandscapeType.Steppe : LandscapeType.Forest;
            }
            entry.SetLandscape(landscape);

            ProductionType production;
            switch (landscape)
            {
                case LandscapeType.Mountain:
                case LandscapeType.Hill:
                    production = random.NextDouble() < 0.7 ? ProductionType.Quarry : ProductionType.Pasture;
                    break;
                case LandscapeType.Steppe:
                    production = random.NextDouble() < 0.6 ? ProductionType.Pasture : ProductionType.Vineyard;
                    break;
                case LandscapeType.Forest:
                    production = random.NextDouble() < 0.5 ? ProductionType.None : ProductionType.Pasture;
                    break;
                default:
                    production = random.NextDouble() < 0.6 ? ProductionType.Wheat : ProductionType.Vineyard;
                    break;
            }
            entry.SetProduction(production);

            var settlementRoll = random.NextDouble();
            entry.SetSettlement(settlementRoll < 0.06 ? SettlementType.City
                : settlementRoll < 0.18 ? SettlementType.Castle
                : settlementRoll < 0.5 ? SettlementType.Village
                : SettlementType.None);
            entry.SetImpassable(false);
        }

        private static string GenerateName(System.Random random)
        {
            var parts = random.Next(2, 4);
            var name = string.Empty;
            for (var i = 0; i < parts; i++) name += Syllables[random.Next(Syllables.Length)];
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>Farthest-point sampling over province centroids, land provinces of a reasonable size only.</summary>
        private static int[] PickSpawnPoints(List<ProvinceDataEntry> entries, Vector2[] centroids, int[] pixelCount, int count)
        {
            var candidates = new List<int>();
            for (var p = 0; p < entries.Count; p++)
                if (entries[p].Type == ProvinceType.Land && !entries[p].IsImpassable && pixelCount[p] > 4000) candidates.Add(p);
            if (candidates.Count == 0) return new int[0];

            var result = new List<int>();
            var first = candidates[0];
            foreach (var c in candidates)
                if (centroids[c].x < centroids[first].x) first = c;
            result.Add(first);

            while (result.Count < count && result.Count < candidates.Count)
            {
                var bestCandidate = -1;
                var bestDistance = -1f;
                foreach (var c in candidates)
                {
                    if (result.Contains(c)) continue;
                    var nearest = float.MaxValue;
                    foreach (var chosen in result) nearest = Mathf.Min(nearest, Vector2.Distance(centroids[c], centroids[chosen]));
                    if (nearest <= bestDistance) continue;
                    bestDistance = nearest;
                    bestCandidate = c;
                }
                result.Add(bestCandidate);
            }
            return result.ToArray();
        }
    }
}
