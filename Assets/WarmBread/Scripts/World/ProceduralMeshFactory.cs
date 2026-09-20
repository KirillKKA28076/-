using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WarmBread
{
    public static class ProceduralMeshFactory
    {
        public static Mesh CreateLathe(
            string name,
            Vector2[] profile,
            int radialSegments = 20,
            bool capBottom = true,
            bool capTop = true)
        {
            if (profile == null || profile.Length < 2)
            {
                throw new ArgumentException("Lathe profile requires at least two points.", nameof(profile));
            }

            radialSegments = Mathf.Max(3, radialSegments);
            var vertices = new List<Vector3>(profile.Length * (radialSegments + 1) + 2);
            var uvs = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>((profile.Length - 1) * radialSegments * 6 + radialSegments * 6);

            for (var ring = 0; ring < profile.Length; ring++)
            {
                var radius = Mathf.Max(0f, profile[ring].x);
                var height = profile[ring].y;
                for (var segment = 0; segment <= radialSegments; segment++)
                {
                    var normalized = segment / (float)radialSegments;
                    var angle = normalized * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius));
                    uvs.Add(new Vector2(normalized, ring / (float)(profile.Length - 1)));
                }
            }

            var stride = radialSegments + 1;
            for (var ring = 0; ring < profile.Length - 1; ring++)
            {
                for (var segment = 0; segment < radialSegments; segment++)
                {
                    var a = ring * stride + segment;
                    var b = a + 1;
                    var c = a + stride;
                    var d = c + 1;
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            if (capBottom && profile[0].x > .0001f)
            {
                var center = vertices.Count;
                vertices.Add(new Vector3(0f, profile[0].y, 0f));
                uvs.Add(new Vector2(.5f, .5f));
                for (var segment = 0; segment < radialSegments; segment++)
                {
                    triangles.Add(center);
                    triangles.Add(segment + 1);
                    triangles.Add(segment);
                }
            }

            if (capTop && profile[profile.Length - 1].x > .0001f)
            {
                var center = vertices.Count;
                var start = (profile.Length - 1) * stride;
                vertices.Add(new Vector3(0f, profile[profile.Length - 1].y, 0f));
                uvs.Add(new Vector2(.5f, .5f));
                for (var segment = 0; segment < radialSegments; segment++)
                {
                    triangles.Add(center);
                    triangles.Add(start + segment);
                    triangles.Add(start + segment + 1);
                }
            }

            return Finish(name, vertices, triangles, uvs);
        }

        public static Mesh CreateLoaf(
            string name,
            float length,
            float width,
            float height,
            int lengthSegments = 14,
            int radialSegments = 18)
        {
            length = Mathf.Max(.01f, length);
            width = Mathf.Max(.01f, width);
            height = Mathf.Max(.01f, height);
            lengthSegments = Mathf.Max(4, lengthSegments);
            radialSegments = Mathf.Max(8, radialSegments);

            var stride = radialSegments + 1;
            var vertices = new List<Vector3>((lengthSegments + 1) * stride);
            var uvs = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>(lengthSegments * radialSegments * 6);

            for (var longitudinal = 0; longitudinal <= lengthSegments; longitudinal++)
            {
                var normalizedLength = longitudinal / (float)lengthSegments;
                var xFactor = normalizedLength * 2f - 1f;
                var endRound = Mathf.Pow(Mathf.Max(0f, 1f - xFactor * xFactor), .38f);
                var x = xFactor * length * .5f;

                for (var radial = 0; radial <= radialSegments; radial++)
                {
                    var normalizedRadial = radial / (float)radialSegments;
                    var angle = normalizedRadial * Mathf.PI * 2f;
                    var sin = Mathf.Sin(angle);
                    var cos = Mathf.Cos(angle);
                    var y = sin * height * .5f * endRound;
                    var z = cos * width * .5f * endRound;

                    if (y > 0f) y *= 1.1f;
                    y += Mathf.Max(0f, 1f - xFactor * xFactor) * height * .045f;
                    vertices.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(normalizedLength, normalizedRadial));
                }
            }

            for (var longitudinal = 0; longitudinal < lengthSegments; longitudinal++)
            {
                for (var radial = 0; radial < radialSegments; radial++)
                {
                    var a = longitudinal * stride + radial;
                    var b = a + 1;
                    var c = a + stride;
                    var d = c + 1;
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            return Finish(name, vertices, triangles, uvs);
        }

        public static Mesh CreateChamferedBox(string name, Vector3 size, float bevel)
        {
            var halfX = Mathf.Max(.005f, Mathf.Abs(size.x) * .5f);
            var halfY = Mathf.Max(.005f, Mathf.Abs(size.y) * .5f);
            var halfZ = Mathf.Max(.005f, Mathf.Abs(size.z) * .5f);
            bevel = Mathf.Clamp(bevel, 0f, Mathf.Min(halfX, halfY) * .9f);

            var profile = new[]
            {
                new Vector2(-halfX + bevel, -halfY),
                new Vector2(halfX - bevel, -halfY),
                new Vector2(halfX, -halfY + bevel),
                new Vector2(halfX, halfY - bevel),
                new Vector2(halfX - bevel, halfY),
                new Vector2(-halfX + bevel, halfY),
                new Vector2(-halfX, halfY - bevel),
                new Vector2(-halfX, -halfY + bevel)
            };

            return CreateExtrudedProfile(name, profile, halfZ * 2f);
        }

        public static Mesh CreateExtrudedProfile(string name, Vector2[] profile, float depth)
        {
            if (profile == null || profile.Length < 3)
            {
                throw new ArgumentException("Extruded profile requires at least three points.", nameof(profile));
            }

            depth = Mathf.Max(.001f, Mathf.Abs(depth));
            var halfDepth = depth * .5f;
            var count = profile.Length;
            var vertices = new List<Vector3>(count * 2 + 2);
            var uvs = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>(count * 12);

            var min = profile[0];
            var max = profile[0];
            for (var i = 1; i < count; i++)
            {
                min = Vector2.Min(min, profile[i]);
                max = Vector2.Max(max, profile[i]);
            }

            var dimensions = max - min;
            dimensions.x = Mathf.Max(.001f, dimensions.x);
            dimensions.y = Mathf.Max(.001f, dimensions.y);

            for (var side = 0; side < 2; side++)
            {
                var z = side == 0 ? -halfDepth : halfDepth;
                for (var i = 0; i < count; i++)
                {
                    vertices.Add(new Vector3(profile[i].x, profile[i].y, z));
                    uvs.Add(new Vector2(
                        (profile[i].x - min.x) / dimensions.x,
                        (profile[i].y - min.y) / dimensions.y));
                }
            }

            var centerFront = vertices.Count;
            vertices.Add(new Vector3(0f, 0f, -halfDepth));
            uvs.Add(new Vector2(.5f, .5f));
            var centerBack = vertices.Count;
            vertices.Add(new Vector3(0f, 0f, halfDepth));
            uvs.Add(new Vector2(.5f, .5f));

            for (var i = 0; i < count; i++)
            {
                var next = (i + 1) % count;
                triangles.Add(centerFront);
                triangles.Add(next);
                triangles.Add(i);
                triangles.Add(centerBack);
                triangles.Add(count + i);
                triangles.Add(count + next);

                var frontA = i;
                var frontB = next;
                var backA = count + i;
                var backB = count + next;
                triangles.Add(frontA);
                triangles.Add(frontB);
                triangles.Add(backA);
                triangles.Add(frontB);
                triangles.Add(backB);
                triangles.Add(backA);
            }

            return Finish(name, vertices, triangles, uvs);
        }

        public static Mesh CreateTorus(
            string name,
            float majorRadius,
            float minorRadius,
            int majorSegments = 20,
            int minorSegments = 10)
        {
            majorRadius = Mathf.Max(.001f, majorRadius);
            minorRadius = Mathf.Max(.001f, minorRadius);
            majorSegments = Mathf.Max(6, majorSegments);
            minorSegments = Mathf.Max(4, minorSegments);

            var stride = minorSegments + 1;
            var vertices = new List<Vector3>((majorSegments + 1) * stride);
            var uvs = new List<Vector2>(vertices.Capacity);
            var triangles = new List<int>(majorSegments * minorSegments * 6);

            for (var major = 0; major <= majorSegments; major++)
            {
                var u = major / (float)majorSegments;
                var majorAngle = u * Mathf.PI * 2f;
                var majorDirection = new Vector3(Mathf.Cos(majorAngle), 0f, Mathf.Sin(majorAngle));

                for (var minor = 0; minor <= minorSegments; minor++)
                {
                    var v = minor / (float)minorSegments;
                    var minorAngle = v * Mathf.PI * 2f;
                    var ringRadius = majorRadius + Mathf.Cos(minorAngle) * minorRadius;
                    vertices.Add(new Vector3(
                        majorDirection.x * ringRadius,
                        Mathf.Sin(minorAngle) * minorRadius,
                        majorDirection.z * ringRadius));
                    uvs.Add(new Vector2(u, v));
                }
            }

            for (var major = 0; major < majorSegments; major++)
            {
                for (var minor = 0; minor < minorSegments; minor++)
                {
                    var a = major * stride + minor;
                    var b = a + 1;
                    var c = a + stride;
                    var d = c + 1;
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            return Finish(name, vertices, triangles, uvs);
        }

        private static Mesh Finish(
            string name,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            var mesh = new Mesh
            {
                name = string.IsNullOrWhiteSpace(name) ? "WarmBread Procedural Mesh" : name,
                hideFlags = HideFlags.DontSave
            };

            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            if (uvs != null && uvs.Count == vertices.Count) mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            try
            {
                mesh.RecalculateTangents();
            }
            catch (Exception)
            {
                // Tangents are optional for fallback shaders.
            }

            return mesh;
        }
    }
}
