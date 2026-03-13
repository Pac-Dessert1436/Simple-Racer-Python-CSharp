#:property PublishTrimmed=false
#:property TargetFramework=net10.0-windows
#:property UseWindowsForms=true

namespace SimpleRacer;

public sealed class RacingCar(float x, float y, bool isPlayer)
{
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
    public bool IsPlayer { get; set; } = isPlayer;
    public int Speed { get; set; } = isPlayer ? GameForm.PLAYER_SPEED : GameForm.ENEMY_SPEED;

    public void Move(float deltaTime, int screenWidth, int screenHeight, bool leftPressed = false, 
        bool rightPressed = false, bool upPressed = false, bool downPressed = false)
    {
        if (IsPlayer)
        {
            // Player movement based on keyboard input
            if (leftPressed) X -= Speed * deltaTime;
            if (rightPressed) X += Speed * deltaTime;
            if (upPressed) Y -= Speed * deltaTime;
            if (downPressed) Y += Speed * deltaTime;

            // Keep player within screen bounds
            X = Math.Max(0, Math.Min(screenWidth - GameForm.CAR_WIDTH, X));
            Y = Math.Max(0, Math.Min(screenHeight - GameForm.CAR_HEIGHT, Y));
        }
        else
        {
            // Enemy cars move downward
            Y += Speed * deltaTime;
        }
    }

    public void Draw(Graphics g)
    {
        Brush carBrush = IsPlayer ? Brushes.Blue : Brushes.Red;
        Brush wheelBrush = Brushes.Black;

        // Draw car body
        g.FillRectangle(carBrush, X, Y, GameForm.CAR_WIDTH, GameForm.CAR_HEIGHT);

        // Draw wheels
        g.FillRectangle(wheelBrush,
            X - GameForm.WHEEL_WIDTH / 2,
            Y + GameForm.WHEEL_OFFSET,
            GameForm.WHEEL_WIDTH,
            GameForm.WHEEL_HEIGHT);

        g.FillRectangle(wheelBrush,
            X - GameForm.WHEEL_WIDTH / 2,
            Y + GameForm.WHEEL_OFFSET + GameForm.WHEEL_DISTANCE,
            GameForm.WHEEL_WIDTH,
            GameForm.WHEEL_HEIGHT);

        g.FillRectangle(wheelBrush,
            X + GameForm.CAR_WIDTH - GameForm.WHEEL_WIDTH / 2,
            Y + GameForm.WHEEL_OFFSET,
            GameForm.WHEEL_WIDTH,
            GameForm.WHEEL_HEIGHT);

        g.FillRectangle(wheelBrush,
            X + GameForm.CAR_WIDTH - GameForm.WHEEL_WIDTH / 2,
            Y + GameForm.WHEEL_OFFSET + GameForm.WHEEL_DISTANCE,
            GameForm.WHEEL_WIDTH,
            GameForm.WHEEL_HEIGHT);
    }
}

public partial class GameForm : Form
{
    // Game constants
    public const int SCREEN_WIDTH = 800;
    public const int SCREEN_HEIGHT = 600;
    public const int CAR_WIDTH = 50;
    public const int CAR_HEIGHT = 100;
    public const int WHEEL_WIDTH = 10;
    public const int WHEEL_HEIGHT = 20;
    public const int WHEEL_OFFSET = 10;
    public const int WHEEL_DISTANCE = 60;
    public const int PLAYER_SPEED = 150;
    public const int ENEMY_SPEED = 225;
    public const float ENEMY_SPAWN_INTERVAL = 0.75f; // seconds

    // Game state
    public bool gameRunning = true;
    public bool gameOver = false;
    public int score = 0;
    public float enemySpawnTimer = 0f;
    public DateTime lastUpdateTime;

    // Input state
    private bool leftPressed = false;
    private bool rightPressed = false;
    private bool upPressed = false;
    private bool downPressed = false;

    // Game objects
    private RacingCar playerCar = default!;
    private List<RacingCar> enemyCars = default!;

    // Graphics
    private BufferedGraphicsContext context = default!;
    private BufferedGraphics bufferedGraphics = default!;

    public GameForm()
    {
        InitializeComponent();
        SetupGame();
        StartGameLoop();
    }

    private void SetupGame()
    {
        // Set up form
        Text = "Simple Racer - C#";
        ClientSize = new Size(SCREEN_WIDTH, SCREEN_HEIGHT);
        DoubleBuffered = true;
        KeyPreview = true;

        // Set up buffered graphics
        context = BufferedGraphicsManager.Current;
        context.MaximumBuffer = new Size(SCREEN_WIDTH + 1, SCREEN_HEIGHT + 1);
        bufferedGraphics = context.Allocate(CreateGraphics(), new(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT));

        // Initialize game objects
        playerCar = new(SCREEN_WIDTH / 2 - CAR_WIDTH / 2, SCREEN_HEIGHT - 150, true);
        enemyCars = [];

        lastUpdateTime = DateTime.Now;

        // Start game loop timer
    }

    private void StartGameLoop()
    {
        System.Windows.Forms.Timer gameTimer = new() { Interval = 16 }; // ~60 FPS
        gameTimer.Tick += (s, e) =>
        {
            if (!IsHandleCreated) return;
            if (gameRunning) try { OnPaint(null!); } catch { }
        };
        gameTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (!gameRunning) return;

        DateTime currentTime = DateTime.Now;
        float deltaTime = (float)(currentTime - lastUpdateTime).TotalSeconds;
        lastUpdateTime = currentTime;

        UpdateGame(deltaTime);
        RenderGame();
    }

    private void UpdateGame(float deltaTime)
    {
        if (gameOver) return;

        // Spawn enemy cars
        enemySpawnTimer += deltaTime;
        if (enemySpawnTimer >= ENEMY_SPAWN_INTERVAL)
        {
            enemySpawnTimer = 0f;
            Random rand = new();
            int enemyX = rand.Next(0, SCREEN_WIDTH - CAR_WIDTH);
            enemyCars.Add(new RacingCar(enemyX, -CAR_HEIGHT, false));
        }

        // Update player car
        playerCar.Move(deltaTime, SCREEN_WIDTH, SCREEN_HEIGHT,
            leftPressed, rightPressed, upPressed, downPressed);

        // Update enemy cars and check collisions
        for (int i = enemyCars.Count - 1; i >= 0; i--)
        {
            RacingCar enemyCar = enemyCars[i];
            enemyCar.Move(deltaTime, SCREEN_WIDTH, SCREEN_HEIGHT);

            // Check collision with player
            if (CheckCollision(playerCar, enemyCar))
            {
                gameOver = true;
                break;
            }

            // Remove enemy cars that have gone off screen
            if (enemyCar.Y > SCREEN_HEIGHT)
            {
                enemyCars.RemoveAt(i);
                score++;
            }
        }
    }

    private void RenderGame()
    {
        Graphics g = bufferedGraphics.Graphics;
        g.Clear(Color.LightBlue);

        // Draw road
        g.FillRectangle(Brushes.Gray, 0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);

        // Draw lane markings
        for (int i = 0; i < SCREEN_HEIGHT; i += 40)
        {
            g.FillRectangle(Brushes.White, SCREEN_WIDTH / 2 - 5, i, 10, 20);
        }

        // Draw cars
        playerCar.Draw(g);
        foreach (RacingCar enemyCar in enemyCars)
        {
            enemyCar.Draw(g);
        }

        // Draw UI
        Font gameFont = new("Arial", 16);
        g.DrawString($"Score: {score}", gameFont, Brushes.White, 10, 10);

        if (gameOver)
        {
            g.FillRectangle(
                Brushes.Black, SCREEN_WIDTH / 2 - 150, SCREEN_HEIGHT / 2 - 10, 325, 40);
            g.DrawString("GAME OVER! Press 'R' to restart", gameFont, Brushes.Red,
                SCREEN_WIDTH / 2 - 150, SCREEN_HEIGHT / 2);
        }

        bufferedGraphics.Render();
        gameFont.Dispose();
    }

    private static bool CheckCollision(RacingCar car1, RacingCar car2)
    {
        return car1.X < car2.X + CAR_WIDTH && car1.X + CAR_WIDTH > car2.X &&
               car1.Y < car2.Y + CAR_HEIGHT && car1.Y + CAR_HEIGHT > car2.Y;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.R && gameOver) ResetGame();

        // Track arrow key states
        switch (e.KeyCode)
        {
            case Keys.Left:
                leftPressed = true;
                break;
            case Keys.Right:
                rightPressed = true;
                break;
            case Keys.Up:
                upPressed = true;
                break;
            case Keys.Down:
                downPressed = true;
                break;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        // Track arrow key states
        switch (e.KeyCode)
        {
            case Keys.Left:
                leftPressed = false;
                break;
            case Keys.Right:
                rightPressed = false;
                break;
            case Keys.Up:
                upPressed = false;
                break;
            case Keys.Down:
                downPressed = false;
                break;
        }
    }

    private void ResetGame()
    {
        gameOver = false;
        score = 0;
        enemyCars.Clear();
        playerCar = new RacingCar(SCREEN_WIDTH / 2 - CAR_WIDTH / 2, SCREEN_HEIGHT - 150, true);
        enemySpawnTimer = 0f;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        gameRunning = false;
        bufferedGraphics?.Dispose();
        base.OnFormClosing(e);
    }

    #region Windows Form Designer generated code
    private void InitializeComponent()
    {
        SuspendLayout();
        // 
        // RacingForm
        // 
        AutoScaleDimensions = new SizeF(6F, 13F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 600);
        Name = "RacingForm";
        Text = "Simple Racer";
        ResumeLayout(false);
    }
    #endregion

    [STAThread]
    internal static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new GameForm());
    }
}
