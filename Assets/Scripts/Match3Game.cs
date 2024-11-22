using UnityEngine;

public class Match3Game : MonoBehaviour
{
    // Ÿ�� ������ �迭
    public GameObject tilePrefab;  // Ÿ�� �������� �Ҵ�
    public int boardWidth = 8;     // ������ ���� ũ��
    public int boardHeight = 8;    // ������ ���� ũ��
    private GameObject[,] board;   // ���带 ��Ÿ�� 2D �迭

    // ���� ���� �� ����Ǵ� �Լ�
    void Start()
    {
        InitializeBoard();
    }

    // ���� ���带 �ʱ�ȭ�ϴ� �Լ�
    void InitializeBoard()
    {
        // 2D �迭�� ���� ����
        board = new GameObject[boardWidth, boardHeight];

        // ������ ��� ĭ�� Ÿ�� ��ġ
        for (int x = 0; x < boardWidth; x++)
        {
            for (int y = 0; y < boardHeight; y++)
            {
                // ������ �� ��ġ�� Ÿ���� ����
                Vector2 position = new Vector2(x, y); // Ÿ���� ��ġ (x, y ��ǥ)
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity);  // Ÿ�� �ν��Ͻ��� ����

                // Ÿ���� ���� �迭�� ����
                board[x, y] = tile;

                // Ÿ���� �̸��� "Tile_x_y" �������� ����
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