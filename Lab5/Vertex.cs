﻿using System.Runtime.InteropServices;
using CGLabPlatform;

namespace Lab5
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct Vertex
    {
        public readonly float Vx, Vy, Vz;
        public readonly float Nx, Ny, Nz;
        public readonly float R, G, B;

        public Vertex(float vx, float vy, float vz,
            float nx, float ny, float nz,
            float r = 1, float g = 1, float b = 1)
        {
            Vx = vx; Vy = vy; Vz = vz;
            Nx = nx; Ny = ny; Nz = nz;
            R = r; G = g; B = b;
        }
    }
}