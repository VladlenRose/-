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
