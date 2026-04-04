using UnityEngine;
using System.Collections.Generic;

namespace GravityRocket.Core
{
    public static class LevelGenerator
    {
        public const int TotalLevels = 50;
        public const float WorldWidth = 10f;
        public const float WorldHeight = 20f;
        public const float GoalWidth = 4f;
        public const float GoalHeight = 0.75f;

        private const float StartBufferY = 3f;
        private const float GoalBufferY = 2f;
        private const float PlanetMinY = 3f;
        private const float PlanetMaxY = 18f;
        private const float MinPlanetEdgeGap = 1.5f;

        private const float MinRadius = 0.3f;
        private const float MaxRadius = 0.8f;
        private const float MassPerRadiusSq = 500f;

        public static LevelData[] GenerateAllLevels()
        {
            var levels = new LevelData[TotalLevels];
            for (int i = 0; i < TotalLevels; i++)
            {
                levels[i] = GenerateLevel(i);
            }
            return levels;
        }

        public static LevelData GenerateLevel(int index)
        {
            float diff = (float)index / TotalLevels;
            int layoutType = index % 5;

            float startX = WorldWidth / 2f + Mathf.Sin(index * 1.3f) * 2.5f;
            float goalX = WorldWidth / 2f + Mathf.Cos(index * 1.7f) * 2f - GoalWidth / 2f;

            if (index < 3)
            {
                startX = WorldWidth / 2f;
                goalX = WorldWidth / 2f - GoalWidth / 2f;
            }

            goalX = Mathf.Clamp(goalX, 0.5f, WorldWidth - GoalWidth - 0.5f);

            var planetList = GeneratePlanetsForLayout(layoutType, diff, index);
            var validPlanets = EnforceBuffers(planetList, new Vector2(startX, 1.5f));

            return new LevelData
            {
                LevelIndex = index,
                LayoutType = layoutType,
                StartPosition = new Vector2(startX, 1.5f),
                GoalPosition = new Vector2(goalX, 19f),
                GoalWidth = GoalWidth,
                Planets = validPlanets
            };
        }

        private static List<PlanetConfig> GeneratePlanetsForLayout(int type, float diff, int index)
        {
            var planets = new List<PlanetConfig>();

            switch (type)
            {
                case 0: // Corridor
                    int gatePairs = 1 + Mathf.FloorToInt(diff * 2.5f);
                    for (int g = 0; g < gatePairs; g++)
                    {
                        float gapY = PlanetMinY + (PlanetMaxY - PlanetMinY) * ((g + 1f) / (gatePairs + 1f));
                        float offset = Mathf.Sin(index + g) * 1.5f;
                        float r = Mathf.Lerp(MinRadius, MinRadius + 0.2f, diff);
                        planets.Add(MakePlanet(WorldWidth / 2f - 2f + offset, gapY, r));
                        planets.Add(MakePlanet(WorldWidth / 2f + 2f + offset, gapY, r));
                    }
                    break;

                case 1: // Blockade
                    int blockCount = 1 + Mathf.FloorToInt(diff * 3f);
                    for (int b = 0; b < blockCount; b++)
                    {
                        float by = PlanetMinY + (PlanetMaxY - PlanetMinY) * ((b + 1f) / (blockCount + 1f));
                        float bx = WorldWidth / 2f + Mathf.Sin(index + b * 2.1f) * 2.5f;
                        float r = Mathf.Lerp(MinRadius + 0.1f, MaxRadius - 0.1f, diff);
                        planets.Add(MakePlanet(bx, by, r));
                    }
                    break;

                case 2: // Zigzag
                    int zigCount = 3 + Mathf.FloorToInt(diff * 5f);
                    for (int z = 0; z < zigCount; z++)
                    {
                        float zy = PlanetMinY + (PlanetMaxY - PlanetMinY) * ((z + 1f) / (zigCount + 1f));
                        float zx = (z % 2 == 0) ? 2.5f : WorldWidth - 2.5f;
                        zx += Mathf.Sin(index * 0.7f + z) * 0.5f;
                        float r = Mathf.Lerp(MinRadius, MinRadius + 0.15f, diff);
                        planets.Add(MakePlanet(zx, zy, r));
                    }
                    break;

                case 3: // Slingshot
                    int slingCount = 1 + Mathf.FloorToInt(diff * 2f);
                    for (int s = 0; s < slingCount; s++)
                    {
                        float sy = PlanetMinY + (PlanetMaxY - PlanetMinY) * ((s + 1f) / (slingCount + 1f));
                        float sx = WorldWidth / 2f + ((index + s) % 2 == 0 ? 1.5f : -1.5f);
                        float r = Mathf.Lerp(0.5f, MaxRadius, diff);
                        planets.Add(MakePlanet(sx, sy, r));
                    }
                    break;

                case 4: // Field
                    int fieldCount = 3 + Mathf.FloorToInt(diff * 7f);
                    for (int f = 0; f < fieldCount; f++)
                    {
                        float fx = 1f + ((index * f * 31) % ((int)((WorldWidth - 2f) * 100))) / 100f;
                        float fy = PlanetMinY + ((index * f * 47) % ((int)((PlanetMaxY - PlanetMinY) * 100))) / 100f;
                        float r = Mathf.Lerp(MinRadius, MinRadius + 0.1f, diff) + (f % 2) * 0.05f;
                        planets.Add(MakePlanet(fx, fy, r));
                    }
                    break;
            }

            return planets;
        }

        private static PlanetConfig MakePlanet(float x, float y, float radius)
        {
            return new PlanetConfig
            {
                Position = new Vector2(x, y),
                Radius = radius,
                Mass = MassPerRadiusSq * radius * radius
            };
        }

        private static PlanetConfig[] EnforceBuffers(List<PlanetConfig> planets, Vector2 startPos)
        {
            var valid = new List<PlanetConfig>();

            foreach (var p in planets)
            {
                if (Vector2.Distance(p.Position, startPos) < 3f)
                    continue;

                if (p.Position.y > PlanetMaxY)
                    continue;

                if (p.Position.y < PlanetMinY)
                    continue;

                if (p.Position.x < p.Radius + 0.2f || p.Position.x > WorldWidth - p.Radius - 0.2f)
                    continue;

                bool tooClose = false;
                foreach (var v in valid)
                {
                    float edgeDist = Vector2.Distance(p.Position, v.Position) - p.Radius - v.Radius;
                    if (edgeDist < MinPlanetEdgeGap)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                valid.Add(p);
            }

            return valid.ToArray();
        }
    }
}
