using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GraphGame
{
    public partial class GameForm : Form
    {
        private List<Vertex> vertices;
        private List<Edge> edges;
        private Vertex currentVertex;
        private Timer gameTimer;
        private Timer gameDurationTimer;
        private Timer shakeTimer;
        private Player player;
        private int screenOffset;
        private int vertexCount;
        private int gameTimeLeft;
        private int score;
        private Random rand;
        private List<int> scores;
        private const string ScoresFile = "scores.txt";

        public GameForm()
        {
            InitializeComponent();
            LoadScores();
            InitializeGameComponents();
            InitializeGame();
            this.DoubleBuffered = true;
        }

        private void LoadScores()
        {
            scores = new List<int>();
            if (File.Exists(ScoresFile))
            {
                var lines = File.ReadAllLines(ScoresFile);
                scores = lines.Select(int.Parse).ToList();
            }
        }

        private void SaveScores()
        {
            File.WriteAllLines(ScoresFile, scores.Select(s => s.ToString()));
        }

        private void InitializeGameComponents()
        {
            var restartButton = new Button
            {
                Text = "Restart",
                Location = new Point(10, 50),
                Size = new Size(80, 30)
            };
            restartButton.Click += (s, e) => RestartGame();
            this.Controls.Add(restartButton);

            this.ClientSize = new Size(800, 600);
            this.Name = "GameForm";
            this.Text = "Graph Game";
        }

        private void InitializeGame()
        {
            vertices = new List<Vertex>();
            edges = new List<Edge>();
            vertexCount = 0;
            rand = new Random();
            score = 0;

            AddInitialVertices();

            gameTimer = new Timer { Interval = 20 };
            gameTimer.Tick += GameTimer_Tick;

            gameDurationTimer = new Timer { Interval = 1000 };
            gameDurationTimer.Tick += GameDurationTimer_Tick;
            gameTimeLeft = 180;

            shakeTimer = new Timer { Interval = 30000 };
            shakeTimer.Tick += ShakeTimer_Tick;

            player = new Player
            {
                Position = new Point(ClientSize.Width / 2, ClientSize.Height - 100),
                IsAttached = false
            };

            screenOffset = 0;

            this.MouseDown += GameForm_MouseDown;

            gameTimer.Start();
            gameDurationTimer.Start();
            shakeTimer.Start();
        }

        private void RestartGame()
        {
            gameTimer.Stop();
            gameDurationTimer.Stop();
            shakeTimer.Stop();

            vertices.Clear();
            edges.Clear();
            vertexCount = 0;
            score = 0;
            gameTimeLeft = 180;
            currentVertex = null;
            player.IsAttached = false;
            player.Position = new Point(ClientSize.Width / 2, ClientSize.Height - 100);

            AddInitialVertices();

            gameTimer.Start();
            gameDurationTimer.Start();
            shakeTimer.Start();

            Invalidate();
        }

        private void EndGame(string message)
        {
            gameTimer.Stop();
            gameDurationTimer.Stop();
            shakeTimer.Stop();

            scores.Add(score);
            scores = scores.OrderByDescending(s => s).ToList();
            SaveScores();

            string topScores = string.Join("\n", scores.Select((s, i) => $"{i + 1}. {s} баллов").Take(5));
            MessageBox.Show($"{message}\n\nТоп результатов:\n{topScores}");

            RestartGame();
        }
            private void AddInitialVertices()
        {
            for (int i = 0; i < 5; i++)
            {
                int x = rand.Next(50, ClientSize.Width - 50);
                int y = rand.Next(50, ClientSize.Height / 2);
                int weight = rand.Next(1, 4); // 1–зелёный, 2–красный, 3–жёлтый

                var vertex = new Vertex
                {
                    X = x,
                    Y = y,
                    Weight = weight,
                    VertexColor = GetVertexColor(weight)
                };

                vertices.Add(vertex);
            }

            GenerateEdges();
        }

        private void GenerateEdges()
        {
            edges.Clear();

            foreach (var v1 in vertices)
            {
                foreach (var v2 in vertices)
                {
                    if (v1 != v2 && rand.NextDouble() < 0.30)
                    {
                        edges.Add(new Edge { Start = v1, End = v2 });
                    }
                }
            }

            EnsureGraphIsConnected();
        }

        private void EnsureGraphIsConnected()
        {
            var visited = new HashSet<Vertex>();
            var stack = new Stack<Vertex>();

            if (vertices.Count > 0)
            {
                stack.Push(vertices[0]);
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!visited.Contains(current))
                {
                    visited.Add(current);
                    foreach (var edge in edges.Where(e => e.Start == current || e.End == current))
                    {
                        var neighbor = edge.Start == current ? edge.End : edge.Start;
                        if (!visited.Contains(neighbor))
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            }

            if (visited.Count != vertices.Count)
            {
                vertices = vertices.Where(v => visited.Contains(v)).ToList();
                edges = edges.Where(e => visited.Contains(e.Start) && visited.Contains(e.End)).ToList();
            }
        }

        private Color GetVertexColor(int weight)
        {
            return weight switch
            {
                1 => Color.Green,
                2 => Color.Red,
                3 => Color.Yellow,
                _ => Color.White
            };
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            foreach (var vertex in vertices)
            {
                vertex.Y += 2;
            }

            if (player.IsAttached && currentVertex != null)
            {
                player.Position = new Point(currentVertex.X, currentVertex.Y);
            }

            vertices.RemoveAll(v => v.Y > ClientSize.Height);

            if (vertices.Count < 5)
            {
                AddInitialVertices();
            }

            if (player.Position.Y >= ClientSize.Height)
            {
                EndGame($"Вы коснулись нижнего края! Ваш результат: {score}.");
            }

            Invalidate();
        }
        private void GameDurationTimer_Tick(object sender, EventArgs e)
        {
            gameTimeLeft--;

            if (gameTimeLeft <= 0)
            {
                EndGame($"Время вышло! Ваш результат: {score}.");
            }

            Invalidate();
        }

        private void ShakeTimer_Tick(object sender, EventArgs e)
        {
            int verticesToRemove = rand.Next(1, vertices.Count / 2);

            for (int i = 0; i < verticesToRemove; i++)
            {
                var vertexToRemove = vertices[rand.Next(vertices.Count)];
                vertices.Remove(vertexToRemove);
                edges.RemoveAll(edge => edge.Start == vertexToRemove || edge.End == vertexToRemove);
            }

            EnsureGraphIsConnected();
            Invalidate();
        }

        private void GameForm_MouseDown(object sender, MouseEventArgs e)
        {
            foreach (var vertex in vertices)
            {
                if (Math.Abs(vertex.X - e.X) < 20 && Math.Abs(vertex.Y - e.Y) < 20)
                {
                    if (currentVertex == null || edges.Any(edge => edge.Connects(currentVertex, vertex)))
                    {
                        currentVertex = vertex;
                        player.Position = new Point(vertex.X, vertex.Y);
                        player.IsAttached = true;

                        score += vertex.Weight;
                        vertexCount++;
                        break;
                    }
                }
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            g.DrawString($"Время: {gameTimeLeft} сек", Font, Brushes.Black, 10, 10);
            g.DrawString($"Счёт: {score}", Font, Brushes.Black, 10, 30);

            foreach (var edge in edges)
            {
                g.DrawLine(Pens.Black, edge.Start.X, edge.Start.Y, edge.End.X, edge.End.Y);
            }

            foreach (var vertex in vertices)
            {
                using (Brush brush = new SolidBrush(vertex.VertexColor))
                {
                    g.FillEllipse(brush, vertex.X - 15, vertex.Y - 15, 30, 30);
                }

                g.DrawString(vertex.Weight.ToString(), Font, Brushes.White, vertex.X - 5, vertex.Y - 5);
            }

            g.FillRectangle(Brushes.Red, player.Position.X - 15, player.Position.Y - 30, 30, 60);
        }
    }
}
