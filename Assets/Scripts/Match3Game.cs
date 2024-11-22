using UnityEngine;

public class Match3Game : MonoBehaviour
{
    // 타일 프리팹 배열
    public GameObject tilePrefab;  // 타일 프리팹을 할당
    public int boardWidth = 8;     // 보드의 가로 크기
    public int boardHeight = 8;    // 보드의 세로 크기
    private GameObject[,] board;   // 보드를 나타낼 2D 배열

    // 게임 시작 시 실행되는 함수
    void Start()
    {
        InitializeBoard();
    }

    // 게임 보드를 초기화하는 함수
    void InitializeBoard()
    {
        // 2D 배열로 보드 생성
        board = new GameObject[boardWidth, boardHeight];

        // 보드의 모든 칸에 타일 배치
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                // 보드의 각 위치에 타일을 생성
                Vector2 position = new Vector2(x, y); // 타일의 위치 (x, y 좌표)
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity);  // 타일 인스턴스를 생성

                // 타일을 보드 배열에 저장
                board[x, y] = tile;

                // 타일의 이름을 "Tile_x_y" 형식으로 설정
                tile.name = $"Tile_{x}_{y}";
            }
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log(board[3, 4].gameObject.name);
        }
    }
}