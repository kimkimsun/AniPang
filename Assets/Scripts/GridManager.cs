using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class GridManager : SingleTon<GridManager>
{
    public GameObject           itemPrefab; // ���� ������ Prefab
    public Transform            gridParent; // Grid Layout Group�� �ִ� �θ� ������Ʈ
    public Sprite[]             sprites;    // �������� ��ġ�� ��������Ʈ �迭
    private GameObject[,]       board;
    private RectTransform       temp;
    private List<Vector2Int>    matchingCoordinates;
    [SerializeField]
    private Queue<GameObject>   poolingQueue = new Queue<GameObject>();
    private int                 sortIndex;
    private int                 maxIndex;
    private int                 indexY;
    private int                 indexX;

    void Start()
    {
        GenerateGrid();
        matchingCoordinates = new List<Vector2Int>();
    }
    void GenerateGrid()
    {
        int gridSize = 8; // 8x8
        board = new GameObject[gridSize, gridSize];

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                GameObject item;

                // ��Ȱ�� ������ ������Ʈ�� ������ ��������
                if (poolingQueue.Count > 0)
                {
                    item = poolingQueue.Dequeue();
                    item.SetActive(true); // ��Ȱ��ȭ ���� ����
                }
                else
                {
                    // ������ ���� ����
                    item = Instantiate(itemPrefab, gridParent);
                }

                board[i, j] = item;

                // ��������Ʈ ���� ��ġ
                Image itemImage = item.GetComponent<Image>();
                if (itemImage != null && sprites.Length > 0)
                {
                    itemImage.sprite = sprites[Random.Range(0, sprites.Length)];
                }
            }
        }
    }
    public Vector2Int GetCoordinates(GameObject puzzleItem)
    {
        // 2D �迭���� �ش� GameObject�� ��ǥ�� ã�� ��ȯ
        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                if (board[i, j] == puzzleItem)
                {
                    return new Vector2Int(i, j); // ��ǥ ��ȯ
                }
            }
        }
        return Vector2Int.one * -1; // ��ǥ�� ã�� ���� ���
    }
    public Vector3 CheckPos(int x, int y)
    {
        return board[x, y].GetComponent<RectTransform>().position;
    }
    public void ChangePos(int x, int y, int changeX, int ChangeY)
    {
        Vector3 tempVec = board[x, y].GetComponent<RectTransform>().position;
        board[x, y].GetComponent<RectTransform>().position =
        board[changeX, ChangeY].GetComponent<RectTransform>().position;

        board[changeX, ChangeY].GetComponent<RectTransform>().position =
        tempVec;
        GameObject tempobj = board[x, y];
        board[x, y] = board[changeX, ChangeY];
        board[changeX, ChangeY] = tempobj;
    }

    public IEnumerator ChangePositionCo(int x, int y, int changeX, int changeY, Puzzle puzzle)
    {
        if (changeX < 0 || changeX >= board.GetLength(0) || changeY < 0 || changeY >= board.GetLength(1))
        {
            puzzle.IsMoved = true;
            yield break;
        }
        RectTransform rect1 = board[x, y].GetComponent<RectTransform>();
        RectTransform rect2 = board[changeX, changeY].GetComponent<RectTransform>();

        // ���� ��ġ ����
        Vector3 startPosition1 = rect1.position;
        Vector3 startPosition2 = rect2.position;

        float duration = 0.3f; // �̵� �ð�
        float elapsed = 0f;

        // �ִϸ��̼� ���
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // ��ġ ����
            rect1.position = Vector3.Lerp(startPosition1, startPosition2, t);
            rect2.position = Vector3.Lerp(startPosition2, startPosition1, t);

            yield return null; // ���� �����ӱ��� ���
        }
        rect1.position = startPosition2;
        rect2.position = startPosition1;
        GameObject tempObj = board[x, y];
        board[x, y] = board[changeX, changeY];
        board[changeX, changeY] = tempObj;

        yield return new WaitForSeconds(3f);

        bool match1 = CheckMatch(changeX, changeY);
        bool match2 = CheckMatch(x, y);

        if (match1 || match2)
        {
            Debug.Log("����");
            puzzle.IsMoved = true;
            //if (CheckMatch(changeX, changeY))
            //{
            //    Debug.Log($"����: {changeX}, {changeY}");
            //    puzzle.IsMoved = true;
            //}
            //if (CheckMatch(x, y))
            //{
            //    Debug.Log($"����: {x}, {y}");
            //    puzzle.IsMoved = true;
            //}
        }
        else
        {
            Debug.Log("������");

            // ���Ϳ� Ÿ�̸� �ʱ�ȭ
            elapsed = 0f;

            // ���� ��ġ�� ����
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // ��ġ ����
                rect1.position = Vector3.Lerp(startPosition2, startPosition1, t);
                rect2.position = Vector3.Lerp(startPosition1, startPosition2, t);

                yield return null; // ���� �����ӱ��� ���
            }
            rect1.position = startPosition1;
            rect2.position = startPosition2;
            tempObj = board[x, y];
            board[x, y] = board[changeX, changeY];
            board[changeX, changeY] = tempObj;
            puzzle.IsMoved = true;
        }
    }

    private bool CheckMatch(int startX, int startY)
    {
        string targetType = board[startX, startY].GetComponent<Image>().sprite.name; // ��� Ÿ�� Ȯ��
        bool[,] visited = new bool[board.GetLength(0), board.GetLength(1)];

        // ���ο� ���� ī��Ʈ ���
        matchingCoordinates.Clear();
        int horizontalCount = DFSHorizontal(startX, startY, targetType, visited, matchingCoordinates);
        for (int i = 0; i < visited.GetLength(0); i++)
        {
            for (int j = 0; j < visited.GetLength(1); j++)
            {
                visited[i, j] = false;
            }
        } // �湮 �迭 �ʱ�ȭ
        matchingCoordinates.Clear();
        int verticalCount = DFSVertical(startX, startY, targetType, visited, matchingCoordinates);

        Debug.Log("���� : " + horizontalCount + ", ���� : " + verticalCount);
        if (horizontalCount >= 3)
        {
            Debug.Log("���� ��ġ: " + horizontalCount);
            return true;
        }
        if (verticalCount >= 3)
        {
            Debug.Log("���� ��ġ: " + verticalCount);
            return true;
        }
        if (horizontalCount < 3 && verticalCount < 3)
        {
            return false;
        }
        else
        {
            return false;
        }
    }
    private int DFSHorizontal(int x, int y, string targetType, bool[,] visited, List<Vector2Int> matchingCoordinates)
    {
        if (x < 0 || x >= board.GetLength(0) || y < 0 || y >= board.GetLength(1)) return 0; // ���� �ʰ�
        if (visited[x, y]) return 0; 
        if (board[x, y].GetComponent<Image>().sprite.name != targetType) return 0; // �ٸ� Ÿ��

        visited[x, y] = true;
        int count = 1;
        matchingCoordinates.Add(new Vector2Int(x, y));

        count += DFSHorizontal(x, y - 1, targetType, visited, matchingCoordinates);
        count += DFSHorizontal(x, y + 1, targetType, visited, matchingCoordinates);
        if(count >= 3)
        {
            Debug.Log("���� �� �� ����?");
            indexX = 0;
            indexY = 10;
            gridParent.GetComponent<GridLayoutGroup>().enabled = false; // ���̾ƿ� ���� �Ͻ� ��Ȱ��ȭ
            foreach (Vector2Int coord in matchingCoordinates)
            {
                indexX = coord.x;
                if (indexY >= coord.y) indexY = coord.y;
                // �μ����� �ϴ� ��ǥ�� ���ؼ� coord��� ������ �־� ���´�.
                // �׸��� �μ����� �Ǵ� ����� �μ���. <-- �̰� ���Ŀ� SetActive�� ������ ����
                Destroy(board[coord.x, coord.y]);
                board[coord.x, coord.y] = null;
                // ������ �͵��� �μ��ϴ�.
            }
            //for(int i = 0; i < maxIndex; i++)
            //{
            //    board[maxIndex - i, indexY] = board[maxIndex - 1, indexY];
            //}
            for (int i = 0; i < count; i++)
            {
                Debug.Log("ī��Ʈ !!!!!!!!!!!!!!!!!!" + count);
                int tempIndex = indexX;
                while (tempIndex > 0)
                {
                    board[tempIndex, indexY] = board[tempIndex - 1, indexY];
                    tempIndex--;
                }
                // �ϴ� item�� ����ϴ�.
                GameObject item = Instantiate(itemPrefab, gridParent);
                // ���� ������� ������ ��ġ�� �˴ϴ�..
                RectTransform rectTransform = item.GetComponent<RectTransform>();
                Debug.Log(indexY + "�� ���� �ڲ� ��..");
                Vector3 newPosition = board[0, indexY].GetComponent<RectTransform>().localPosition;
                newPosition.y += 151; // ���� ���� (151�� ��� �� �Ÿ�)
                rectTransform.localPosition = newPosition;
                board[0, indexY] = item;
                // ���� ����� ��ġ ������� �� ��ġ ����
                Debug.Log("ȣ��");
                item.gameObject.name = "���� ������";
                Image itemImage = item.GetComponent<Image>();
                if (itemImage != null && sprites.Length > 0)
                {
                    itemImage.sprite = sprites[Random.Range(0, sprites.Length)];
                }
                indexY++;
            }

            matchingCoordinates.Clear();
        }
        return count;
    }

    private int DFSVertical(int x, int y, string targetType, bool[,] visited, List<Vector2Int> matchingCoordinates)
    {
        if (x < 0 || x >= board.GetLength(0) || y < 0 || y >= board.GetLength(1)) return 0; // ���� �ʰ�
        if (visited[x, y]) return 0;
        if (board[x, y].GetComponent<Image>().sprite.name != targetType) return 0; // �ٸ� Ÿ��

        visited[x, y] = true;
        int count = 1;

        matchingCoordinates.Add(new Vector2Int(x, y));
        count += DFSVertical(x - 1, y, targetType, visited, matchingCoordinates);
        count += DFSVertical(x + 1, y, targetType, visited, matchingCoordinates);

        if (count >= 3)
        {
            Debug.Log(count + "���� ���;Ҵ�~?");
            indexX = 0;
            indexY = 0;
            gridParent.GetComponent<GridLayoutGroup>().enabled = false; // ���̾ƿ� ���� �Ͻ� ��Ȱ��ȭ
            //���� ��Ī�� 3���̻� �ż� ������ �ȴٸ�
            foreach (Vector2Int coord in matchingCoordinates)
            {
                if (indexX <= coord.x) indexX = coord.x;
                indexY = coord.y;
                // �μ����� �ϴ� ��ǥ�� ���ؼ� coord��� ������ �־� ���´�.
                // �׸��� �μ����� �Ǵ� ����� �μ���. <-- �̰� ���Ŀ� SetActive�� ������ ����
                board[coord.x, coord.y].gameObject.SetActive(false);
                poolingQueue.Enqueue(board[coord.x, coord.y]);
                Debug.Log(poolingQueue.Count);
                // ������ �͵��� �μ��ϴ�.
            }
            for (int i = 0; i < count; i++)
            {
                int tempIndex = indexX;
                while (tempIndex > 0)
                {
                    board[tempIndex, indexY] = board[tempIndex - 1, indexY];
                    tempIndex--;
                }
                Debug.Log("���°�� Empty�ΰǵ�" + i);
                    board[0, indexY] = poolingQueue.Dequeue();
                    board[0, indexY].SetActive(true);

                // �ϴ� item�� ����ϴ�.
                //GameObject item = Instantiate(itemPrefab, gridParent);
                //// ���� ������� ������ ��ġ�� �˴ϴ�..
                //RectTransform rectTransform = item.GetComponent<RectTransform>();
                //Vector3 newPosition = board[0, indexY].GetComponent<RectTransform>().localPosition;
                ////���µ� �׷���� �ϴϱ� ���ϴ� ���� �� setactive�� �����̶� �ؾߵǳ�?
                //newPosition.y += 151; // ���� ���� (151�� ��� �� �Ÿ�)
                //rectTransform.localPosition = newPosition;
                //board[0, indexY] = item;
                //// ���� ����� ��ġ ������� �� ��ġ ����
                //Debug.Log("ȣ��");
                //item.gameObject.name = "���� ������";
                //Image itemImage = item.GetComponent<Image>();
                //if (itemImage != null && sprites.Length > 0)
                //{
                //    itemImage.sprite = sprites[Random.Range(0, sprites.Length)];
                //}
            }
            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    if (board[i, j] != null && !board[i, j].activeSelf)
                    {
                        Debug.Log($"ã�Ҵ� �̳�! ��ġ: {i}, {j}");
                        // ���⼭ ��Ȱ�� ���� �߰� ����
                    }
                }
            }
            matchingCoordinates.Clear(); // ��Ī�� ��ǥ�� �ʱ�ȭ
        }
        return count;
    }
}