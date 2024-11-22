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
    public GameObject           itemPrefab; // 퍼즐 아이템 Prefab
    public Transform            gridParent; // Grid Layout Group이 있는 부모 오브젝트
    public Sprite[]             sprites;    // 랜덤으로 배치할 스프라이트 배열
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

                // 재활용 가능한 오브젝트가 있으면 가져오기
                if (poolingQueue.Count > 0)
                {
                    item = poolingQueue.Dequeue();
                    item.SetActive(true); // 비활성화 상태 복구
                }
                else
                {
                    // 없으면 새로 생성
                    item = Instantiate(itemPrefab, gridParent);
                }

                board[i, j] = item;

                // 스프라이트 랜덤 배치
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
        // 2D 배열에서 해당 GameObject의 좌표를 찾아 반환
        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                if (board[i, j] == puzzleItem)
                {
                    return new Vector2Int(i, j); // 좌표 반환
                }
            }
        }
        return Vector2Int.one * -1; // 좌표를 찾지 못한 경우
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

        // 원래 위치 저장
        Vector3 startPosition1 = rect1.position;
        Vector3 startPosition2 = rect2.position;

        float duration = 0.3f; // 이동 시간
        float elapsed = 0f;

        // 애니메이션 재생
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 위치 보간
            rect1.position = Vector3.Lerp(startPosition1, startPosition2, t);
            rect2.position = Vector3.Lerp(startPosition2, startPosition1, t);

            yield return null; // 다음 프레임까지 대기
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
            Debug.Log("터짐");
            puzzle.IsMoved = true;
            //if (CheckMatch(changeX, changeY))
            //{
            //    Debug.Log($"터짐: {changeX}, {changeY}");
            //    puzzle.IsMoved = true;
            //}
            //if (CheckMatch(x, y))
            //{
            //    Debug.Log($"터짐: {x}, {y}");
            //    puzzle.IsMoved = true;
            //}
        }
        else
        {
            Debug.Log("안터짐");

            // 복귀용 타이머 초기화
            elapsed = 0f;

            // 원래 위치로 복귀
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 위치 보간
                rect1.position = Vector3.Lerp(startPosition2, startPosition1, t);
                rect2.position = Vector3.Lerp(startPosition1, startPosition2, t);

                yield return null; // 다음 프레임까지 대기
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
        string targetType = board[startX, startY].GetComponent<Image>().sprite.name; // 블록 타입 확인
        bool[,] visited = new bool[board.GetLength(0), board.GetLength(1)];

        // 가로와 세로 카운트 계산
        matchingCoordinates.Clear();
        int horizontalCount = DFSHorizontal(startX, startY, targetType, visited, matchingCoordinates);
        for (int i = 0; i < visited.GetLength(0); i++)
        {
            for (int j = 0; j < visited.GetLength(1); j++)
            {
                visited[i, j] = false;
            }
        } // 방문 배열 초기화
        matchingCoordinates.Clear();
        int verticalCount = DFSVertical(startX, startY, targetType, visited, matchingCoordinates);

        Debug.Log("가로 : " + horizontalCount + ", 세로 : " + verticalCount);
        if (horizontalCount >= 3)
        {
            Debug.Log("가로 매치: " + horizontalCount);
            return true;
        }
        if (verticalCount >= 3)
        {
            Debug.Log("세로 매치: " + verticalCount);
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
        if (x < 0 || x >= board.GetLength(0) || y < 0 || y >= board.GetLength(1)) return 0; // 범위 초과
        if (visited[x, y]) return 0; 
        if (board[x, y].GetComponent<Image>().sprite.name != targetType) return 0; // 다른 타입

        visited[x, y] = true;
        int count = 1;
        matchingCoordinates.Add(new Vector2Int(x, y));

        count += DFSHorizontal(x, y - 1, targetType, visited, matchingCoordinates);
        count += DFSHorizontal(x, y + 1, targetType, visited, matchingCoordinates);
        if(count >= 3)
        {
            Debug.Log("여기 몇 번 들어옴?");
            indexX = 0;
            indexY = 10;
            gridParent.GetComponent<GridLayoutGroup>().enabled = false; // 레이아웃 갱신 일시 비활성화
            foreach (Vector2Int coord in matchingCoordinates)
            {
                indexX = coord.x;
                if (indexY >= coord.y) indexY = coord.y;
                // 부셔져야 하는 좌표를 구해서 coord라는 변수에 넣어 놓는다.
                // 그리고 부셔져야 되는 놈들을 부순다. <-- 이건 추후에 SetActive로 조절할 예정
                Destroy(board[coord.x, coord.y]);
                board[coord.x, coord.y] = null;
                // 기존의 것들을 부숩니다.
            }
            //for(int i = 0; i < maxIndex; i++)
            //{
            //    board[maxIndex - i, indexY] = board[maxIndex - 1, indexY];
            //}
            for (int i = 0; i < count; i++)
            {
                Debug.Log("카운트 !!!!!!!!!!!!!!!!!!" + count);
                int tempIndex = indexX;
                while (tempIndex > 0)
                {
                    board[tempIndex, indexY] = board[tempIndex - 1, indexY];
                    tempIndex--;
                }
                // 일단 item을 만듭니다.
                GameObject item = Instantiate(itemPrefab, gridParent);
                // 어디든 상관없이 고정된 위치에 됩니다..
                RectTransform rectTransform = item.GetComponent<RectTransform>();
                Debug.Log(indexY + "너 뭔데 자꾸 하..");
                Vector3 newPosition = board[0, indexY].GetComponent<RectTransform>().localPosition;
                newPosition.y += 151; // 높이 조정 (151은 블록 간 거리)
                rectTransform.localPosition = newPosition;
                board[0, indexY] = item;
                // 기존 블록의 위치 기반으로 새 위치 설정
                Debug.Log("호출");
                item.gameObject.name = "새로 생성됨";
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
        if (x < 0 || x >= board.GetLength(0) || y < 0 || y >= board.GetLength(1)) return 0; // 범위 초과
        if (visited[x, y]) return 0;
        if (board[x, y].GetComponent<Image>().sprite.name != targetType) return 0; // 다른 타입

        visited[x, y] = true;
        int count = 1;

        matchingCoordinates.Add(new Vector2Int(x, y));
        count += DFSVertical(x - 1, y, targetType, visited, matchingCoordinates);
        count += DFSVertical(x + 1, y, targetType, visited, matchingCoordinates);

        if (count >= 3)
        {
            Debug.Log(count + "몇이 들어와았니~?");
            indexX = 0;
            indexY = 0;
            gridParent.GetComponent<GridLayoutGroup>().enabled = false; // 레이아웃 갱신 일시 비활성화
            //세로 매칭이 3개이상 돼서 터지게 된다면
            foreach (Vector2Int coord in matchingCoordinates)
            {
                if (indexX <= coord.x) indexX = coord.x;
                indexY = coord.y;
                // 부셔져야 하는 좌표를 구해서 coord라는 변수에 넣어 놓는다.
                // 그리고 부셔져야 되는 놈들을 부순다. <-- 이건 추후에 SetActive로 조절할 예정
                board[coord.x, coord.y].gameObject.SetActive(false);
                poolingQueue.Enqueue(board[coord.x, coord.y]);
                Debug.Log(poolingQueue.Count);
                // 기존의 것들을 부숩니다.
            }
            for (int i = 0; i < count; i++)
            {
                int tempIndex = indexX;
                while (tempIndex > 0)
                {
                    board[tempIndex, indexY] = board[tempIndex - 1, indexY];
                    tempIndex--;
                }
                Debug.Log("몇번째때 Empty인건데" + i);
                    board[0, indexY] = poolingQueue.Dequeue();
                    board[0, indexY].SetActive(true);

                // 일단 item을 만듭니다.
                //GameObject item = Instantiate(itemPrefab, gridParent);
                //// 어디든 상관없이 고정된 위치에 됩니다..
                //RectTransform rectTransform = item.GetComponent<RectTransform>();
                //Vector3 newPosition = board[0, indexY].GetComponent<RectTransform>().localPosition;
                ////없는데 그러라고 하니까 못하는 거지 뭐 setactive를 지금이라도 해야되나?
                //newPosition.y += 151; // 높이 조정 (151은 블록 간 거리)
                //rectTransform.localPosition = newPosition;
                //board[0, indexY] = item;
                //// 기존 블록의 위치 기반으로 새 위치 설정
                //Debug.Log("호출");
                //item.gameObject.name = "새로 생성됨";
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
                        Debug.Log($"찾았다 이놈! 위치: {i}, {j}");
                        // 여기서 재활용 로직 추가 가능
                    }
                }
            }
            matchingCoordinates.Clear(); // 매칭된 좌표들 초기화
        }
        return count;
    }
}