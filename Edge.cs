namespace GraphGame
{
    public class Edge
    {
        public Vertex Start { get; set; }
        public Vertex End { get; set; }

        public bool Connects(Vertex v1, Vertex v2)
        {
            return (Start == v1 && End == v2) || (Start == v2 && End == v1);
        }
    }
}
