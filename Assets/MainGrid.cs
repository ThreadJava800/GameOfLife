using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Player
{
    None,
    Player1,
    Player2
}

public enum GameMode
{
    Menu,
    SinglePlayer,
    TwoPlayers
}

public enum PatternType
{
    Glider,
    LightweightSpaceship,
    Block,
    Beehive,
    Blinker
}

public class Cell
{
    public MeshRenderer renderer;
    public GameObject gameObject;
    public bool alive;
    public Player owner;

    public Cell()
    {
        alive = false;
        owner = Player.None;
    }
}

public class Pattern
{
    public PatternType type;
    public bool[,] shape;
    public int width;
    public int height;
    public string name;

    public Pattern(PatternType patternType)
    {
        type = patternType;
        InitializePattern();
    }

    private void InitializePattern()
    {
        switch (type)
        {
            case PatternType.Glider:
                width = 3; height = 3;
                shape = new bool[3, 3];
                shape[0, 1] = true;
                shape[1, 2] = true;
                shape[2, 0] = shape[2, 1] = shape[2, 2] = true;
                name = "Glider";
                break;

            case PatternType.LightweightSpaceship:
                width = 5; height = 4;
                shape = new bool[5, 4];
                shape[1, 0] = true;
                shape[2, 0] = true;
                shape[3, 0] = true;
                shape[4, 0] = true;
                shape[0, 1] = true;
                shape[4, 1] = true;
                shape[4, 2] = true;
                shape[0, 3] = true;
                shape[3, 3] = true;
                name = "Spaceship";
                break;

            case PatternType.Block:
                width = 2; height = 2;
                shape = new bool[2, 2];
                shape[0, 0] = shape[1, 0] = shape[0, 1] = shape[1, 1] = true;
                name = "Block";
                break;

            case PatternType.Beehive:
                width = 4; height = 3;
                shape = new bool[4, 3];
                shape[1, 0] = shape[2, 0] = true;
                shape[0, 1] = shape[3, 1] = true;
                shape[1, 2] = shape[2, 2] = true;
                name = "Beehive";
                break;

            case PatternType.Blinker:
                width = 3; height = 1;
                shape = new bool[3, 1];
                shape[0, 0] = shape[1, 0] = shape[2, 0] = true;
                name = "Blinker";
                break;
        }
    }
}

public class MainGrid : MonoBehaviour
{
    private const int GRID_WIDTH = 40;
    private const int GRID_HEIGHT = 30;
    private const int BASE_SPEED = 60;
    private const int INITIAL_CELLS_PER_PLAYER = 10;
    private const float RANDOM_SPAWN_CHANCE = 0.3f;

    private GameObject gridObj;
    private Cell[,] cells;
    private int frameCnt;
    private bool isPaused = false;
    private int speedMultiplier = 1;
    private readonly int[] speedMultipliers = { 1, 2, 4, 8 };

    private GameMode currentGameMode = GameMode.Menu;
    private Player currentPlayer = Player.Player1;
    private int player1Score = 0;
    private int player2Score = 0;
    private bool gameStarted = false;
    private int placementPhaseCellsLeft = INITIAL_CELLS_PER_PLAYER;

    private Color player1Color = new Color(0.2f, 0.4f, 1f);
    private Color player2Color = new Color(0.2f, 0.8f, 0.2f);
    private Color neutralColor = new Color(0.1f, 0.1f, 0.1f);

    private List<Pattern> availablePatterns;
    private Pattern selectedPattern;
    private bool patternPlacementMode = false;
    private int currentPatternIndex = 0;

    private float cameraZoomSpeed = 2f;
    private float minZoom = 2f;
    private float maxZoom = 20f;
    private Vector3 dragOrigin;

    private bool showResultsWindow = false;
    private string winnerText = "";
    private string resultsDetails = "";
    private float resultsWindowTimer = 0f;
    private const float RESULTS_DISPLAY_TIME = 8f;

    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle smallButtonStyle;
    private GUIStyle resultsTitleStyle;
    private GUIStyle resultsTextStyle;
    private GUIStyle resultsButtonStyle;
    private bool stylesInitialized = false;

    private bool randomSpawningEnabled = false;
    private int randomSpawnFrameCounter = 0;
    private const int RANDOM_SPAWN_INTERVAL = 30;

    // WebGL material fix
    private Material defaultMaterial;

    public MainGrid()
    {
        cells = new Cell[GRID_WIDTH, GRID_HEIGHT];
        frameCnt = 0;
        InitializePatterns();
    }

    void InitializePatterns()
    {
        availablePatterns = new List<Pattern>
        {
            new Pattern(PatternType.Glider),
            new Pattern(PatternType.LightweightSpaceship),
            new Pattern(PatternType.Block),
            new Pattern(PatternType.Beehive),
            new Pattern(PatternType.Blinker)
        };
        selectedPattern = availablePatterns[0];
    }

    void Start()
    {
        // Create material for WebGL compatibility
        CreateDefaultMaterial();
        CreateGrid();

        // WebGL optimization
#if UNITY_WEBGL
            Application.targetFrameRate = 60;
#endif

        Camera.main.orthographicSize = Mathf.Max(GRID_WIDTH, GRID_HEIGHT) * 0.3f;
        Camera.main.transform.position = new Vector3(0, 0, -10);
    }

    void CreateDefaultMaterial()
    {
        // Create a simple material that works in WebGL
        defaultMaterial = new Material(Shader.Find("Sprites/Default"));
        defaultMaterial.color = Color.white;
    }

    void InitializeGUIStyles()
    {
        if (stylesInitialized) return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 24;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 18;
        buttonStyle.fixedWidth = 200;
        buttonStyle.fixedHeight = 40;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 16;
        labelStyle.normal.textColor = Color.white;

        smallButtonStyle = new GUIStyle(GUI.skin.button);
        smallButtonStyle.fontSize = 14;
        smallButtonStyle.fixedWidth = 120;
        smallButtonStyle.fixedHeight = 30;

        resultsTitleStyle = new GUIStyle(GUI.skin.label);
        resultsTitleStyle.fontSize = 32;
        resultsTitleStyle.fontStyle = FontStyle.Bold;
        resultsTitleStyle.alignment = TextAnchor.MiddleCenter;
        resultsTitleStyle.normal.textColor = Color.yellow;

        resultsTextStyle = new GUIStyle(GUI.skin.label);
        resultsTextStyle.fontSize = 20;
        resultsTextStyle.alignment = TextAnchor.MiddleCenter;
        resultsTextStyle.normal.textColor = Color.white;

        resultsButtonStyle = new GUIStyle(GUI.skin.button);
        resultsButtonStyle.fontSize = 18;
        resultsButtonStyle.fixedWidth = 180;
        resultsButtonStyle.fixedHeight = 45;

        stylesInitialized = true;
    }

    void Update()
    {
        if (currentGameMode == GameMode.Menu) return;

        HandleCameraMovement();
        HandleCameraZoom();

        if (showResultsWindow)
        {
            resultsWindowTimer += Time.deltaTime;
            if (resultsWindowTimer >= RESULTS_DISPLAY_TIME)
            {
                showResultsWindow = false;
                ReturnToMenu();
            }
        }

        if (!gameStarted)
        {
            HandlePlacementPhase();
        }
        else
        {
            HandleGamePhase();
        }

        HandleCommonInput();
        HandleMouseHover();
    }

    void HandleCameraMovement()
    {
        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 difference = dragOrigin - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Camera.main.transform.position += difference;
        }
    }

    void HandleCameraZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Camera.main.orthographicSize = Mathf.Clamp(
                Camera.main.orthographicSize - scroll * cameraZoomSpeed,
                minZoom,
                maxZoom
            );
        }
    }

    void HandlePlacementPhase()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            patternPlacementMode = !patternPlacementMode;
        }

        if (patternPlacementMode)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                PreviousPattern();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                NextPattern();
            }
        }

        if (currentGameMode == GameMode.SinglePlayer)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                GenerateRandomCells();
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                StartGame();
            }
        }

        // WebGL click handling fix
#if UNITY_WEBGL
            if (Input.GetMouseButton(0))
#else
        if (Input.GetMouseButtonDown(0))
#endif
        {
            HandleCellClick();
        }
    }

    void HandleGamePhase()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
        }

        if (currentGameMode == GameMode.SinglePlayer && Input.GetKeyDown(KeyCode.R))
        {
            randomSpawningEnabled = !randomSpawningEnabled;
        }

        if (Input.GetKeyDown(KeyCode.S) && isPaused)
        {
            Step();
        }

        if (isPaused && Input.GetKeyDown(KeyCode.P))
        {
            patternPlacementMode = !patternPlacementMode;
        }

        if (isPaused && patternPlacementMode)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                PreviousPattern();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                NextPattern();
            }
        }

        // WebGL click handling fix
#if UNITY_WEBGL
            if (currentGameMode == GameMode.SinglePlayer && isPaused && Input.GetMouseButton(0))
#else
        if (currentGameMode == GameMode.SinglePlayer && isPaused && Input.GetMouseButtonDown(0))
#endif
        {
            HandleCellClick();
        }

        if (!isPaused)
        {
            if (currentGameMode == GameMode.SinglePlayer && randomSpawningEnabled)
            {
                randomSpawnFrameCounter++;
                if (randomSpawnFrameCounter >= RANDOM_SPAWN_INTERVAL)
                {
                    SpawnRandomCell();
                    randomSpawnFrameCounter = 0;
                }
            }

            if (frameCnt % (BASE_SPEED / speedMultiplier) == 0)
            {
                Step();
            }
        }
        frameCnt += 1;
    }

    void NextPattern()
    {
        currentPatternIndex = (currentPatternIndex + 1) % availablePatterns.Count;
        selectedPattern = availablePatterns[currentPatternIndex];
    }

    void PreviousPattern()
    {
        currentPatternIndex = (currentPatternIndex - 1 + availablePatterns.Count) % availablePatterns.Count;
        selectedPattern = availablePatterns[currentPatternIndex];
    }

    void HandleCellClick()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 localPos = mousePos - gridObj.transform.position;

        int clickX = Mathf.FloorToInt(localPos.x);
        int clickY = Mathf.FloorToInt(localPos.y);

        if (clickX >= 0 && clickX < GRID_WIDTH && clickY >= 0 && clickY < GRID_HEIGHT)
        {
            if (patternPlacementMode)
            {
                PlacePattern(clickX, clickY);
            }
            else
            {
                if (currentGameMode == GameMode.SinglePlayer)
                {
                    ToggleCellInGame(clickX, clickY);
                }
                else if (!gameStarted && !cells[clickX, clickY].alive)
                {
                    cells[clickX, clickY].alive = true;
                    cells[clickX, clickY].owner = currentPlayer;
                    UpdateCellColor(clickX, clickY);

                    placementPhaseCellsLeft--;

                    if (placementPhaseCellsLeft == 0)
                    {
                        if (currentPlayer == Player.Player1)
                        {
                            currentPlayer = Player.Player2;
                            placementPhaseCellsLeft = INITIAL_CELLS_PER_PLAYER;
                        }
                        else
                        {
                            StartGame();
                        }
                    }
                }
            }
        }
    }

    void PlacePattern(int centerX, int centerY)
    {
        int startX = centerX - selectedPattern.width / 2;
        int startY = centerY - selectedPattern.height / 2;

        int placedCells = 0;

        for (int x = 0; x < selectedPattern.width; x++)
        {
            for (int y = 0; y < selectedPattern.height; y++)
            {
                int gridX = startX + x;
                int gridY = startY + y;

                if (gridX >= 0 && gridX < GRID_WIDTH && gridY >= 0 && gridY < GRID_HEIGHT)
                {
                    if (selectedPattern.shape[x, y] && !cells[gridX, gridY].alive)
                    {
                        cells[gridX, gridY].alive = true;
                        cells[gridX, gridY].owner = currentPlayer;
                        placedCells++;
                    }
                }
            }
        }

        UpdateGridColors();

        if (!gameStarted && currentGameMode == GameMode.TwoPlayers)
        {
            placementPhaseCellsLeft -= placedCells;

            if (placementPhaseCellsLeft <= 0)
            {
                if (currentPlayer == Player.Player1)
                {
                    currentPlayer = Player.Player2;
                    placementPhaseCellsLeft = INITIAL_CELLS_PER_PLAYER;
                }
                else
                {
                    StartGame();
                }
            }
        }

        if (gameStarted)
        {
            if (currentPlayer == Player.Player1)
                player1Score += placedCells;
            else
                player2Score += placedCells;
        }
    }

    void ToggleCellInGame(int x, int y)
    {
        if (cells[x, y].alive)
        {
            cells[x, y].alive = false;
            cells[x, y].owner = Player.None;
            if (gameStarted) player1Score = Mathf.Max(0, player1Score - 1);
        }
        else
        {
            cells[x, y].alive = true;
            cells[x, y].owner = Player.Player1;
            if (gameStarted) player1Score++;
        }
        UpdateCellColor(x, y);
    }

    void HandleCommonInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMenu();
        }

        if (currentGameMode == GameMode.Menu) return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            IncreaseSpeed();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            DecreaseSpeed();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ResetSpeed();
        }

        if (Input.GetKeyDown(KeyCode.E) && currentGameMode == GameMode.TwoPlayers)
        {
            EndGame();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            RestartGame();
        }

        if (currentGameMode == GameMode.SinglePlayer && Input.GetKeyDown(KeyCode.C))
        {
            ClearGrid();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ResetCamera();
        }
    }

    void ResetCamera()
    {
        Camera.main.orthographicSize = Mathf.Max(GRID_WIDTH, GRID_HEIGHT) * 0.3f;
        Camera.main.transform.position = new Vector3(0, 0, -10);
    }

    void StartGame()
    {
        gameStarted = true;
        isPaused = false;
        patternPlacementMode = false;
        showResultsWindow = false;
        string modeText = currentGameMode == GameMode.SinglePlayer ? "Single Player" : "Two Players";

        CalculateInitialScores();
    }

    void EndGame()
    {
        if (currentGameMode == GameMode.SinglePlayer) return;

        gameStarted = false;
        isPaused = true;
        ShowResultsWindow();
    }

    void ShowResultsWindow()
    {
        showResultsWindow = true;
        resultsWindowTimer = 0f;
        DetermineWinner();
    }

    void RestartGame()
    {
        currentPlayer = Player.Player1;
        player1Score = 0;
        player2Score = 0;
        gameStarted = false;
        placementPhaseCellsLeft = INITIAL_CELLS_PER_PLAYER;
        isPaused = false;
        randomSpawningEnabled = false;
        randomSpawnFrameCounter = 0;
        patternPlacementMode = false;
        showResultsWindow = false;

        ClearGrid();
        ResetCamera();
    }

    void ClearGrid()
    {
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                cells[x, y].alive = false;
                cells[x, y].owner = Player.None;
            }
        }
        UpdateGridColors();
        player1Score = 0;
        player2Score = 0;
    }

    void ReturnToMenu()
    {
        currentGameMode = GameMode.Menu;
        gameStarted = false;
        isPaused = false;
        randomSpawningEnabled = false;
        patternPlacementMode = false;
        showResultsWindow = false;

        ClearGrid();
        ResetCamera();
    }

    void StartSinglePlayer()
    {
        currentGameMode = GameMode.SinglePlayer;
        currentPlayer = Player.Player1;
        placementPhaseCellsLeft = int.MaxValue;
    }

    void StartTwoPlayers()
    {
        currentGameMode = GameMode.TwoPlayers;
        currentPlayer = Player.Player1;
        placementPhaseCellsLeft = INITIAL_CELLS_PER_PLAYER;
    }

    void GenerateRandomCells()
    {
        int cellsToGenerate = Random.Range(5, 20);
        for (int i = 0; i < cellsToGenerate; i++)
        {
            int x = Random.Range(0, GRID_WIDTH);
            int y = Random.Range(0, GRID_HEIGHT);

            if (!cells[x, y].alive)
            {
                cells[x, y].alive = true;
                cells[x, y].owner = Player.Player1;
            }
        }
        UpdateGridColors();
        CalculateInitialScores();
    }

    void SpawnRandomCell()
    {
        int cellsToSpawn = Random.Range(1, 4);
        int actuallySpawned = 0;
        for (int i = 0; i < cellsToSpawn; i++)
        {
            int x = Random.Range(0, GRID_WIDTH);
            int y = Random.Range(0, GRID_HEIGHT);

            if (!cells[x, y].alive && Random.value < RANDOM_SPAWN_CHANCE)
            {
                cells[x, y].alive = true;
                cells[x, y].owner = Player.Player1;
                actuallySpawned++;
            }
        }
        if (actuallySpawned > 0)
        {
            player1Score += actuallySpawned;
            UpdateGridColors();
        }
    }

    void CreateGrid()
    {
        gridObj = new GameObject("Grid");

        for (int x = 0; x < GRID_WIDTH; ++x)
        {
            for (int y = 0; y < GRID_HEIGHT; ++y)
            {
                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cell.transform.parent = gridObj.transform;
                cell.transform.position = new Vector3(x, y, 0);
                cell.transform.localScale = Vector3.one * 0.95f;

                cells[x, y] = new Cell();
                cells[x, y].renderer = cell.GetComponent<MeshRenderer>();
                cells[x, y].gameObject = cell;

                // Use the created default material instead of external reference
                cells[x, y].renderer.material = new Material(defaultMaterial);
            }
        }

        gridObj.transform.position = new Vector3(-GRID_WIDTH / 2, -GRID_HEIGHT / 2, 0);
        UpdateGridColors();
    }

    void HandleMouseHover()
    {
        if (currentGameMode == GameMode.Menu) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 localPos = mousePos - gridObj.transform.position;

        int hoverX = Mathf.FloorToInt(localPos.x);
        int hoverY = Mathf.FloorToInt(localPos.y);

        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                UpdateCellColor(x, y);
            }
        }

        if (patternPlacementMode && hoverX >= 0 && hoverX < GRID_WIDTH && hoverY >= 0 && hoverY < GRID_HEIGHT)
        {
            int startX = hoverX - selectedPattern.width / 2;
            int startY = hoverY - selectedPattern.height / 2;

            for (int x = 0; x < selectedPattern.width; x++)
            {
                for (int y = 0; y < selectedPattern.height; y++)
                {
                    int gridX = startX + x;
                    int gridY = startY + y;

                    if (gridX >= 0 && gridX < GRID_WIDTH && gridY >= 0 && gridY < GRID_HEIGHT)
                    {
                        if (selectedPattern.shape[x, y])
                        {
                            if (cells[gridX, gridY].alive)
                            {
                                cells[gridX, gridY].renderer.material.color = Color.yellow;
                            }
                            else
                            {
                                cells[gridX, gridY].renderer.material.color =
                                    currentPlayer == Player.Player1 ?
                                    new Color(0.5f, 0.7f, 1f) :
                                    new Color(0.5f, 1f, 0.5f);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (hoverX >= 0 && hoverX < GRID_WIDTH && hoverY >= 0 && hoverY < GRID_HEIGHT)
            {
                if (currentGameMode == GameMode.SinglePlayer && (isPaused || !gameStarted))
                {
                    if (cells[hoverX, hoverY].alive)
                    {
                        cells[hoverX, hoverY].renderer.material.color = new Color(1f, 0.7f, 0.7f);
                    }
                    else
                    {
                        cells[hoverX, hoverY].renderer.material.color = new Color(0.7f, 0.7f, 1f);
                    }
                }
                else if (!gameStarted && !cells[hoverX, hoverY].alive)
                {
                    cells[hoverX, hoverY].renderer.material.color =
                        currentPlayer == Player.Player1 ?
                        new Color(0.7f, 0.7f, 1f) :
                        new Color(0.7f, 1f, 0.7f);
                }
            }
        }
    }

    void UpdateCellColor(int x, int y)
    {
        if (cells[x, y] != null && cells[x, y].renderer != null && cells[x, y].renderer.material != null)
        {
            if (!cells[x, y].alive)
            {
                cells[x, y].renderer.material.color = neutralColor;
            }
            else
            {
                cells[x, y].renderer.material.color =
                    cells[x, y].owner == Player.Player1 ?
                    new Color(0.2f, 0.4f, 1f) :
                    new Color(0.2f, 0.8f, 0.2f);
            }
        }
    }

    void UpdateGridColors()
    {
        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                UpdateCellColor(x, y);
            }
        }
    }

    void Step()
    {
        bool[,] nextState = new bool[GRID_WIDTH, GRID_HEIGHT];
        Player[,] nextOwners = new Player[GRID_WIDTH, GRID_HEIGHT];
        int player1NewCells = 0;
        int player2NewCells = 0;

        for (int x = 0; x < GRID_WIDTH; ++x)
        {
            for (int y = 0; y < GRID_HEIGHT; ++y)
            {
                int cnt = GetAliveNeighborsCnt(x, y);
                Player dominantPlayer = GetDominantNeighborPlayer(x, y);
                bool currState = cells[x, y].alive;

                if (currState && (cnt == 2 || cnt == 3))
                {
                    nextState[x, y] = true;
                    nextOwners[x, y] = cells[x, y].owner;
                }
                else if (!currState && cnt == 3)
                {
                    nextState[x, y] = true;
                    nextOwners[x, y] = dominantPlayer;

                    if (dominantPlayer == Player.Player1)
                        player1NewCells++;
                    else if (dominantPlayer == Player.Player2)
                        player2NewCells++;
                }
                else
                {
                    nextState[x, y] = false;
                    nextOwners[x, y] = Player.None;
                }
            }
        }

        for (int x = 0; x < GRID_WIDTH; ++x)
        {
            for (int y = 0; y < GRID_HEIGHT; ++y)
            {
                cells[x, y].alive = nextState[x, y];
                cells[x, y].owner = nextOwners[x, y];
            }
        }

        player1Score += player1NewCells;
        player2Score += player2NewCells;

        UpdateGridColors();

        if (currentGameMode == GameMode.TwoPlayers && player1NewCells == 0 && player2NewCells == 0)
        {
            EndGame();
        }
    }

    int GetAliveNeighborsCnt(int x, int y)
    {
        int count = 0;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int neighborX = (x + i + GRID_WIDTH) % GRID_WIDTH;
                int neighborY = (y + j + GRID_HEIGHT) % GRID_HEIGHT;

                if (cells[neighborX, neighborY].alive)
                {
                    count++;
                }
            }
        }

        return count;
    }

    Player GetDominantNeighborPlayer(int x, int y)
    {
        int player1Count = 0;
        int player2Count = 0;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int neighborX = (x + i + GRID_WIDTH) % GRID_WIDTH;
                int neighborY = (y + j + GRID_HEIGHT) % GRID_HEIGHT;

                if (cells[neighborX, neighborY].alive)
                {
                    if (cells[neighborX, neighborY].owner == Player.Player1)
                        player1Count++;
                    else if (cells[neighborX, neighborY].owner == Player.Player2)
                        player2Count++;
                }
            }
        }

        if (currentGameMode == GameMode.SinglePlayer) return Player.Player1;

        if (player1Count > player2Count) return Player.Player1;
        if (player2Count > player1Count) return Player.Player2;

        return Random.Range(0, 2) == 0 ? Player.Player1 : Player.Player2;
    }

    void CalculateInitialScores()
    {
        player1Score = 0;
        player2Score = 0;

        for (int x = 0; x < GRID_WIDTH; x++)
        {
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                if (cells[x, y].alive)
                {
                    if (cells[x, y].owner == Player.Player1)
                        player1Score++;
                    else if (cells[x, y].owner == Player.Player2)
                        player2Score++;
                }
            }
        }
    }

    void DetermineWinner()
    {
        string player1ColorHex = "#4C78FF";
        string player2ColorHex = "#33CC33";

        if (currentGameMode == GameMode.TwoPlayers)
        {
            resultsDetails = $"<color={player1ColorHex}>Player 1 (blue): {player1Score} points</color>\n" +
                           $"<color={player2ColorHex}>Player 2 (green): {player2Score} points</color>\n\n";

            if (player1Score > player2Score)
            {
                winnerText = $"<color={player1ColorHex}>WINNER: PLAYER 1!</color>";
                resultsDetails += $"<color={player1ColorHex}>Player 1 won with {player1Score - player2Score} points advantage!</color>";
            }
            else if (player2Score > player1Score)
            {
                winnerText = $"<color={player2ColorHex}>WINNER: PLAYER 2!</color>";
                resultsDetails += $"<color={player2ColorHex}>Player 2 won with {player2Score - player1Score} points advantage!</color>";
            }
            else
            {
                winnerText = "<color=yellow>DRAW!</color>";
                resultsDetails += "<color=yellow>Both players have the same score!</color>";
            }
        }
        else
        {
            winnerText = "<color=#4C78FF>SINGLE PLAYER GAME COMPLETED</color>";
            resultsDetails = $"<color=#4C78FF>Your score: {player1Score} points</color>\n\n";

            if (player1Score >= 50)
                resultsDetails += "<color=yellow>Excellent result! You are a Game of Life master!</color>";
            else if (player1Score >= 30)
                resultsDetails += "<color=green>Good result! Keep it up!</color>";
            else if (player1Score >= 15)
                resultsDetails += "<color=white>Not bad! There's room for improvement!</color>";
            else
                resultsDetails += "<color=orange>Try again! You can improve your result!</color>";
        }
    }

    void IncreaseSpeed()
    {
        if (!gameStarted) return;

        int currentIndex = System.Array.IndexOf(speedMultipliers, speedMultiplier);
        if (currentIndex < speedMultipliers.Length - 1)
        {
            speedMultiplier = speedMultipliers[currentIndex + 1];
        }
    }

    void DecreaseSpeed()
    {
        if (!gameStarted) return;

        int currentIndex = System.Array.IndexOf(speedMultipliers, speedMultiplier);
        if (currentIndex > 0)
        {
            speedMultiplier = speedMultipliers[currentIndex - 1];
        }
    }

    void ResetSpeed()
    {
        if (!gameStarted) return;

        speedMultiplier = 1;
    }

    void OnGUI()
    {
        InitializeGUIStyles();

        if (showResultsWindow)
        {
            DisplayResultsWindow();
        }
        else if (currentGameMode == GameMode.Menu)
        {
            DisplayMainMenu();
        }
        else
        {
            DisplayGameHUD();
        }
    }

    void DisplayResultsWindow()
    {
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float centerX = Screen.width / 2;
        float centerY = Screen.height / 2;
        float width = 600;
        float height = 400;

        GUILayout.BeginArea(new Rect(centerX - width / 2, centerY - height / 2, width, height));

        GUILayout.Label("MATCH RESULTS", resultsTitleStyle);
        GUILayout.Space(20);

        GUILayout.Label(winnerText, resultsTextStyle);
        GUILayout.Space(30);

        GUILayout.Label(resultsDetails, resultsTextStyle);
        GUILayout.Space(40);

        float timeLeft = RESULTS_DISPLAY_TIME - resultsWindowTimer;
        GUILayout.Label($"Auto return to menu in: {timeLeft:F1} sec.", resultsTextStyle);
        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("New Game", resultsButtonStyle))
        {
            showResultsWindow = false;
            RestartGame();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Main Menu", resultsButtonStyle))
        {
            showResultsWindow = false;
            ReturnToMenu();
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    void DisplayMainMenu()
    {
        float centerX = Screen.width / 2;
        float centerY = Screen.height / 2;

        GUI.Label(new Rect(centerX - 150, centerY - 120, 300, 50), "GAME OF LIFE", titleStyle);

        if (GUI.Button(new Rect(centerX - 100, centerY - 40, 200, 40), "Single Player", buttonStyle))
        {
            StartSinglePlayer();
        }

        if (GUI.Button(new Rect(centerX - 100, centerY + 20, 200, 40), "Two Players", buttonStyle))
        {
            StartTwoPlayers();
        }
    }

    void DisplayGameHUD()
    {
        string modeText = currentGameMode == GameMode.SinglePlayer ? "Single Player" : "Two Players";

        if (!gameStarted)
        {
            if (currentGameMode == GameMode.SinglePlayer)
            {
                GUI.Label(new Rect(10, 10, 500, 30), "Single Player: place cells without limits", labelStyle);
                GUI.Label(new Rect(10, 40, 300, 30), "R=Random cells, Enter=Start game", labelStyle);
            }
            else
            {
                GUI.Label(new Rect(10, 10, 500, 30), "Placement phase: " + currentPlayer + " places cells", labelStyle);
                GUI.Label(new Rect(10, 40, 300, 30), "Cells left to place: " + placementPhaseCellsLeft, labelStyle);
            }
        }
        else
        {
            GUI.Label(new Rect(10, 10, 300, 30), "Speed: " + speedMultiplier + "x", labelStyle);
            GUI.Label(new Rect(10, 40, 300, 30), "Status: " + (isPaused ? "Paused" : "Playing"), labelStyle);

            if (currentGameMode == GameMode.SinglePlayer)
            {
                GUI.Label(new Rect(10, 70, 300, 30), "Random spawn: " + (randomSpawningEnabled ? "ON" : "OFF"), labelStyle);
            }
        }

        if (patternPlacementMode)
        {
            GUI.Label(new Rect(Screen.width - 250, 10, 240, 30), "PATTERN MODE: " + selectedPattern.name, labelStyle);
            GUI.Label(new Rect(Screen.width - 250, 40, 240, 30), "Q/E - Select pattern", labelStyle);
            GUI.Label(new Rect(Screen.width - 250, 70, 240, 30), "LMB - Place", labelStyle);

            float buttonY = 100;
            foreach (var pattern in availablePatterns)
            {
                if (GUI.Button(new Rect(Screen.width - 250, buttonY, 120, 25), pattern.name, smallButtonStyle))
                {
                    selectedPattern = pattern;
                    currentPatternIndex = availablePatterns.IndexOf(pattern);
                }
                buttonY += 30;
            }
        }

        GUI.Label(new Rect(10, 100, 300, 30), "Mode: " + modeText, labelStyle);
        GUI.Label(new Rect(10, 130, 300, 30), "Player 1 (blue): " + player1Score, labelStyle);

        if (currentGameMode == GameMode.TwoPlayers)
        {
            GUI.Label(new Rect(10, 160, 300, 30), "Player 2 (green): " + player2Score, labelStyle);
        }

        string controls = "Controls: Space=Pause, Arrows=Speed, Tab=Restart, Esc=Menu";
        controls += ", P=Patterns, F=Reset camera";

        if (currentGameMode == GameMode.SinglePlayer)
        {
            controls += ", R=Random spawn, C=Clear";
        }
        else
        {
            controls += ", E=End game";
        }

        GUI.Label(new Rect(10, Screen.height - 30, 800, 30), controls, labelStyle);
    }
}
