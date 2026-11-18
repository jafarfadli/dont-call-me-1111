using System.Collections.Generic;
using UnityEngine;

namespace DontCallMe.Player
{
    /// <summary>
    /// A walking route across the room for a click on something out of reach: an A* search over a
    /// 10 cm grid of the floor, where a cell is free when a capsule as wide as the player (a little
    /// wider) stands there without touching a wall or a piece of furniture's knee-high box, then
    /// pulled straight wherever the way between two points is free, so the walk curves around the
    /// chair instead of zigzagging. Triggers (the clickable objects' boxes) are not in the way.
    /// </summary>
    public static class WalkPath
    {
        const float Cell = 0.1f;
        const float Margin = 0.08f;
        // The capsule's foot sits above the floor and a door's threshold (the controller steps over those).
        const float FootClearance = 0.3f;
        const float Head = 1.6f;
        // How far around the start and the target the search may go.
        const float Border = 1.5f;
        const int MaxCells = 120 * 120;

        static readonly Collider[] overlaps = new Collider[8];

        /// <summary>
        /// Points to walk through from <paramref name="from"/> (the player's feet) to the free spot
        /// nearest <paramref name="to"/>, the last one that spot; null when there is no way.
        /// </summary>
        public static List<Vector3> Find(Vector3 from, Vector3 to, float radius, Collider self)
        {
            float r = radius + Margin;
            float y = from.y;
            float minX = Mathf.Min(from.x, to.x) - Border, minZ = Mathf.Min(from.z, to.z) - Border;
            int w = Mathf.CeilToInt((Mathf.Max(from.x, to.x) + Border - minX) / Cell) + 1;
            int h = Mathf.CeilToInt((Mathf.Max(from.z, to.z) + Border - minZ) / Cell) + 1;
            if (w * h > MaxCells)
                return null;

            // 0 unknown, 1 free, 2 blocked: cells are tested only when the search reaches them.
            var state = new byte[w * h];
            Vector3 At(int i) => new Vector3(minX + (i % w) * Cell, y, minZ + (i / w) * Cell);
            bool Free(int i)
            {
                if (state[i] == 0)
                    state[i] = Blocked(At(i), r, self) ? (byte)2 : (byte)1;
                return state[i] == 1;
            }
            int CellOf(Vector3 p) => Mathf.Clamp(Mathf.RoundToInt((p.z - minZ) / Cell), 0, h - 1) * w
                                     + Mathf.Clamp(Mathf.RoundToInt((p.x - minX) / Cell), 0, w - 1);

            int start = Nearest(CellOf(from), w, h, Free, 0.4f);
            int goal = Nearest(CellOf(to), w, h, Free, Border);
            if (start < 0 || goal < 0)
                return null;

            // A*, eight neighbours, octile distance.
            var cost = new Dictionary<int, float> { [start] = 0f };
            var came = new Dictionary<int, int>();
            var open = new List<int> { start };
            var closed = new HashSet<int>();
            float Guess(int i)
            {
                int dx = Mathf.Abs(i % w - goal % w), dz = Mathf.Abs(i / w - goal / w);
                return Mathf.Max(dx, dz) + 0.414f * Mathf.Min(dx, dz);
            }
            bool found = false;
            while (open.Count > 0)
            {
                int best = 0;
                for (int k = 1; k < open.Count; k++)
                    if (cost[open[k]] + Guess(open[k]) < cost[open[best]] + Guess(open[best]))
                        best = k;
                int cur = open[best];
                open.RemoveAt(best);
                if (cur == goal)
                {
                    found = true;
                    break;
                }
                closed.Add(cur);
                int cx = cur % w, cz = cur / w;
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0)
                            continue;
                        int nx = cx + dx, nz = cz + dz;
                        if (nx < 0 || nz < 0 || nx >= w || nz >= h)
                            continue;
                        int n = nz * w + nx;
                        if (closed.Contains(n) || !Free(n))
                            continue;
                        // No cutting a corner between two blocked cells.
                        if (dx != 0 && dz != 0 && (!Free(cz * w + nx) || !Free(nz * w + cx)))
                            continue;
                        float c = cost[cur] + (dx != 0 && dz != 0 ? 1.414f : 1f);
                        if (cost.TryGetValue(n, out float old) && old <= c)
                            continue;
                        cost[n] = c;
                        came[n] = cur;
                        if (!open.Contains(n))
                            open.Add(n);
                    }
            }
            if (!found)
                return null;

            var cells = new List<Vector3>();
            for (int i = goal; ; i = came[i])
            {
                cells.Add(At(i));
                if (i == start)
                    break;
            }
            cells.Reverse();
            cells[0] = new Vector3(from.x, y, from.z);
            return Straighten(cells, r, self);
        }

        /// <summary>Keeps only the turns: from each point, the farthest later one that can be walked to in a straight line.</summary>
        static List<Vector3> Straighten(List<Vector3> cells, float r, Collider self)
        {
            var path = new List<Vector3>();
            int i = 0;
            while (i < cells.Count - 1)
            {
                int j = cells.Count - 1;
                while (j > i + 1 && !Clear(cells[i], cells[j], r, self))
                    j--;
                path.Add(cells[j]);
                i = j;
            }
            return path;
        }

        /// <summary>Whether a player of <paramref name="radius"/> can walk straight from <paramref name="a"/> to <paramref name="b"/>, with the usual margin.</summary>
        public static bool IsClear(Vector3 a, Vector3 b, float radius, Collider self)
        {
            b.y = a.y;
            return Clear(a, b, radius + Margin, self);
        }

        static bool Clear(Vector3 a, Vector3 b, float r, Collider self)
        {
            float d = Vector3.Distance(a, b);
            for (float t = Cell * 0.5f; t < d; t += Cell * 0.5f)
                if (Blocked(Vector3.Lerp(a, b, t / d), r, self))
                    return false;
            return true;
        }

        static bool Blocked(Vector3 feet, float r, Collider self)
        {
            var bottom = feet + Vector3.up * (FootClearance + r);
            var top = feet + Vector3.up * Mathf.Max(FootClearance + r, Head - r);
            int n = Physics.OverlapCapsuleNonAlloc(bottom, top, r, overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < n; k++)
                if (overlaps[k] != self)
                    return true;
            return false;
        }

        /// <summary>The free cell nearest <paramref name="cell"/>, within <paramref name="within"/> metres; -1 if none.</summary>
        static int Nearest(int cell, int w, int h, System.Func<int, bool> free, float within)
        {
            if (free(cell))
                return cell;
            int reach = Mathf.CeilToInt(within / Cell);
            int cx = cell % w, cz = cell / w, best = -1;
            float bestD = float.MaxValue;
            for (int dz = -reach; dz <= reach; dz++)
                for (int dx = -reach; dx <= reach; dx++)
                {
                    int x = cx + dx, z = cz + dz;
                    float d = dx * dx + dz * dz;
                    if (x < 0 || z < 0 || x >= w || z >= h || d >= bestD || d > reach * reach)
                        continue;
                    int i = z * w + x;
                    if (free(i))
                    {
                        best = i;
                        bestD = d;
                    }
                }
            return best;
        }
    }
}
