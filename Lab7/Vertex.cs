using System.Runtime.InteropServices;
using CGLabPlatform;

namespace Lab7
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex
    {
        public float Vx, Vy, Vz;
        public float Nx, Ny, Nz;
        public float R, G, B;

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