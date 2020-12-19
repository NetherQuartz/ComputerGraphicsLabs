using System.Runtime.InteropServices;
using CGLabPlatform;

namespace Lab4
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct Vertex
    {
        public readonly float Vx, Vy, Vz;
        public readonly float Nx, Ny, Nz;
        public readonly float R, G, B;

        public Vertex(float vx, float vy, float vz,
            float nx, float ny, float nz,
            float r, float g, float b)
        {
            Vx = vx; Vy = vy; Vz = vz;
            Nx = nx; Ny = ny; Nz = nz;
            R = r; G = g; B = b;
        }
    }
}