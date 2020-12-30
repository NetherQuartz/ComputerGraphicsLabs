using CGLabPlatform;

namespace CourseWork
{
    internal readonly struct Polygon
    {
        public readonly DVector3 P1, P2, P3;
        public readonly DVector3 Normal;

        // вершины по часовой стрелке
        public Polygon(DVector3 p1, DVector3 p2, DVector3 p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            Normal = DVector3.CrossProduct(P3 - P1, P2 - P1)
                .Normalized();
        }

        public DVector3 Center => (P1 + P2 + P3) / 3;
    }
}