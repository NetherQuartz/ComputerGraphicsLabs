using CGLabPlatform;

namespace Lab4
{
    struct Polygon
    {
        public readonly DVector4 P1, P2, P3;
        public readonly DVector4 Normal;

        // вершины по часовой стрелке
        public Polygon(DVector4 p1, DVector4 p2, DVector4 p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            Normal = DVector3.CrossProduct(P3 - P1, P2 - P1)
                .Normalized();
        }

        public DVector4 Center => (P1 + P2 + P3) / 3;
    }
}