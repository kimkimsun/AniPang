using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridManager : SingleTon<GridManager>
{
    public GameObject           itemPrefab; // 퍼즐 아이템 Prefab
    public Transform            gridParent; // Grid Layout Group이 있는 부모 오브젝트
    public Sprite[]             sprites;    // 랜덤으로 배치할 스프라이트 배열
    private GameObject[,]       board;
    private Vector3[,]          boardLocalPos;
    private RectTransform       temp;
    private List<Vector2Int>    horizontalMatch;
    private List<Vector2Int>    verticalMatch;
    private Queue<GameObject>   poolingQueue;
    private Vector2Int          firstValueStorage;
    private int                 sortIndex;
    private int                 maxIndex;
    private int                 indexY;
    private int                 indexX;
    private int                 eraseCount;

    void Start()
    {
        horizontalMatch = new List<Vector2Int>();
        verticalMatch = new List<Vector2Int>();
        poolingQueue = new Queue<GameObject>();
        GenerateGrid();
    }

    void GenerateGrid()
    {
        int gridSize = 8; // 8x8
        board = new GameObject[gridSize, gridSize];
        boardLocalPos = new Vector3[gridSize, gridSize];
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
                item.name = $"새로 생성됨{i},{j}";
                board[i, j] = item;

                // 스프라이트 랜덤 배치, 연속 타일 방지
                Image itemImage = item.GetComponent<Image>();
                if (itemImage != null && sprites.Length > 0)
                {
                    Sprite selectedSprite;
                    do
                    {
                        selectedSprite = sprites[Random.Range(0, sprites.Length)];
                    }
                    while ((i > 1 && board[i - 1, j].GetComponent<Image>().sprite == selectedSprite
                                   && board[i - 2, j].GetComponent<Image>().sprite == selectedSprite) || // 세로 검사
                           (j > 1 && board[i, j - 1].GetComponent<Image>().sprite == selectedSprite
                                   && board[i, j - 2].GetComponent<Image>().sprite == selectedSprite)); // 가로 검사

                    itemImage.sprite = selectedSprite;
                }
                boardLocalPos[i,j] = board[i, j].GetComponent<RectTransform>().localPosition;
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
            puzzle.IsMoved = true;
        }
        else
        {
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
        horizontalMatch.Clear();
        indexX = -1;
        indexY = 999;
        int horizontalCount = DFSHorizontal(startX, startY, targetType, visited, horizontalMatch);
        for (int i = 0; i < visited.GetLength(0); i++)
        {
            for (int j = 0; j < visited.GetLength(1); j++)
            {
                visited[i, j] = false;
            }
        } // 방문 배열 초기화
        verticalMatch.Clear();
        indexX = -1;
        indexY = 999;
        int verticalCount = DFSVertical(startX, startY, targetType, visited, verticalMatch);







        if (horizontalCount >= 5)
        {
            firstValueStorage = horizontalMatch[0];
            horizontalMatch.RemoveAt(0);
            gridParent.GetComponent<GridLayoutGroup>().enabled = false; // 레이아웃 갱신 일시 비활성화
            foreach (Vector2Int coord in horizontalMatch)
            {
                indexX = coord.x;
                if (indexY >= coord.y) indexY = coord.y;
                // 부셔져야 하는 좌표를 구해서 coord라는 변수에 넣어 놓는다.
                // 그리고 부셔져야 되는 놈들을 부순다. <-- 이건 추후에 SetActive로 조절할 예정
                board[coord.x, coord.y].gameObject.SetActive(false);
                poolingQueue.Enqueue(board[coord.x, coord.y]);
                Debug.Log("들어올 때" + poolingQueue.Count);
                // 기존의 것들을 부숩니다.
            }
            board[firstValueStorage.x, firstValueStorage.y].GetComponent<Image>().color = Color.red;
            FillBoard();
            horizontalMatch.Clear();
            return true;
        }







        else if (verticalCount >= 5)
        {
            firstValueStorage = verticalMatch[0];
            verticalMatch.RemoveAt(0);
            foreach (Vector2Int coord in verticalMatch)
            {
                // 첫번째로 뽑히는 놈을 찾았다. Debug.Log("첫번째" + coord.x);
                if (indexX <= coord.x) indexX = coord.x;
                indexY = coord.y;
                // 부셔져야 하는 좌표를 구해서 coord라는 변수에 넣어 놓는다.
                // 그리고 부셔져야 되는 놈들을 부순다. <-- 이건 추후에 SetActive로 조절할 예정
                board[coord.x, coord.y].gameObject.SetActive(false);
                poolingQueue.Enqueue(board[coord.x, coord.y]);
                // 기존의 것들을 부숩니다.
            }
            board[firstValueStorage.x, firstValueStorage.y].GetComponent<Image>().color = Color.red;
            FillBoard();
            verticalMatch.Clear(); // 매칭된 좌표들 초기화
            return true;
        }









        else if (verticalCount >= 3 && horizontalCount >= 3)
        {
            gridParent.GetComponent<GridLayoutGroup>().enabled = false; // 레이아웃 갱신 일시 비활성화
            firstValueStorage = horizontalMatch[0];
            horizontalMatch.RemoveAt(0);
            foreach (Vector2Int coord in horizontalMatch)
            {
                indexX = coord.x;
                if (indexY >= coord.y) indexY = coord.y;
                // 부셔져야 하는 좌표를 구해서 coord라는 변수에 넣어 놓는다.
                // 그리고 부셔져야 되는 놈들을 부순다. <-- 이건 추후에 SetActive로 조절할 예정
                board[coord.x, coord.y].gameObject.SetActive(false);
                poolingQueue.Enqueue(board[coord.x, coord.y]);
                // 기존의 것들을 부숩니다.
            }
            board[firstValueStorage.x, firstValueStorage.y].GetComponent<Image>().color = Color.green;
            Debug.Log("xxxx : " + firstValueStorage.x + "yyyyy : " + firstValueStorage.y);
            for (int i = 0; i < horizontalMatch.Count; i++)
            {
                int tempIndex = indexX;
                while (tempIndex > 0)
                {
                    if (firstValueStorage.y == indexY) indexY++;
                    board[tempIndex, indexY] = board[tempIndex - 1, indexY];
                    tempIndex--;
                }
                Vector3 newPosition = board[0, indexY].GetComponent<RectTransform>().localPosition;
                newPosition.y += 151;
                board[0, indexY] = poolingQueue.Dequeue();
                board[0, indexY].SetActive(true);
                board[0, indexY].GetComponent<RectTransform>().localPosition = newPosition;
                board[0, indexY].GetComponent<Image>().sprite = sprites[Random.Range(0, sprites.Length)];
                indexY++;
            }
            horizontalMatch.Clear();
            firstValueStorage = verticalMatch[0];
            verticalMatch.RemoveAt(0);
            indexX = -1;
            indexY = 999;
            foreach (Vector2Int coord in verticalMatch)
            {
                if (indexX <= coord.x) indexX = coord.x;
                indexY = coord.y;
                // 부셔져야 하는 좌표를 구해서 coord라는 변수에 넣어 놓는다.
                // 그리고 부셔져야 되는 놈들을 부순다. <-- 이건 추후에 SetActive로 조절할 예정
                board[coord.x, coord.y].gameObject.SetActive(false);
                poolingQueue.Enqueue(board[coord.x, coord.y]);
                // 기존의 것들을 부숩니다.
            }
            FillBoard();
            verticalMatch.Clear(); // 매칭된 좌표들 초기화
            return true;
        }
        else if (verticalCount >= 4)
        {
            return true;
        }

        else if (horizontalCount >= 4)
        {
            return true;
        }

        else if (horizontalCount >= 3)
        {
            gridParent.GetComponent<GridLayoutGroup>().enabled = false; // 레이아웃 갱신 일시 비활성화
            foreach (Vector2Int coord in horizontalMatch)
            {
                indexX = coord.x;
                if (indexY >= coord.y) indexY = coord.y;
                // 부셔져야 하는 좌표를 구해서 coord라는 변수에 넣어 놓는다.
                // 그리고 부셔져야 되는 놈들을 부순다. <-- 이건 추후에 SetActive로 조절할 예정
                board[coord.x, coord.y].gameObject.SetActive(false);
                poolingQueue.Enqueue(board[coord.x, coord.y]);
                // 기존의 것들을 부숩니다.
            }
            FillBoard();
            horizontalMatch.Clear();
            return true;
        }
        #region 세로 터트리는 if문
        else if (verticalCount >= 3)
        {
            foreach (Vector2Int coord in verticalMatch)
            {
            // 첫번째로 뽑히는 놈을 찾았다. Debug.Log("첫번째" + coord.x);
                if (indexX <= coord.x) indexX = coord.x;
                indexY = coord.y;
                // 부셔져야 하는 좌표를 구해서 coord라는 변수에 넣어 놓는다.
                // 그리고 부셔져야 되는 놈들을 부순다. <-- 이건 추후에 SetActive로 조절할 예정
                board[coord.x, coord.y].gameObject.SetActive(false);
                poolingQueue.Enqueue(board[coord.x, coord.y]);
                // 기존의 것들을 부숩니다.
            }
            FillBoard();
            verticalMatch.Clear(); // 매칭된 좌표들 초기화
            return true;
        }
        #endregion
        else
        {
            return false;
        }
    }
    #region 가로 터지는 거 체크 함수
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
        gridParent.GetComponent<GridLayoutGroup>().enabled = false; // 레이아웃 갱신 일시 비활성화
        return count;
    }
    #endregion
    #region 세로 터지는 거 체크 함수
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
        gridParent.GetComponent<GridLayoutGroup>().enabled = false; // 레이아웃 갱신 일시 비활성화
        return count;
    }
    #endregion
    #region 디버깅 함수

    private void FillBoard()
    {
        int index;
        bool nothing;
        for (int i = board.GetLength(0)-1; i >= 0; i--)
        {
            for (int j = board.GetLength(1)-1; j >= 0; j--)
            {
                if (!(board[j, i].activeSelf))
                {
                    index = j - 1;
                    nothing = false;
                    while (index >= 0)
                    {
                        if (board[index, j].activeSelf)
                        {
                            GameObject temp = board[index, j];
                            board[index, j] = board[j ,i];
                            board[j, i] = temp;
                            board[j, i].SetActive(false);
                            eraseCount++;
                            nothing = true;
                            break;
                        }
                        index--;
                    }
                    if(!nothing)
                    {
                        board[j,i] = poolingQueue.Dequeue();
                        board[j,i].SetActive(true);
                    }
                }
            }
        }
    }
                        //        board[i, j].SetActive(true);
                        //        board[i, j] = board[index, j];
                        //        board[index,j].SetActive(false);
                        //        ChangePos(board[index, j], boardLocalPos[index,j]);
                        //        nothing = true;
                        //        break;
                        //    }
                        //    index--;
                        //}
                        //if(!nothing)
                        //{
                        //    board[i,j] = poolingQueue.Dequeue();
                        //    board[i, j].SetActive(true);
                        //    board[i, j].GetComponent<RectTransform>().localPosition = boardLocalPos[0, j];
                        //    board[i, j].GetComponent<Image>().sprite = sprites[Random.Range(0, sprites.Length)];
                        //    indexY++;
                        //}

    private void ChangePos(GameObject rect1, Vector3 arrivePos)
    {
        RectTransform startPos = rect1.GetComponent<RectTransform>();
        float duration = 0.1f; // 이동 시간
        float elapsed = 0f;
        Vector3 startPosition = startPos.position;
        // 애니메이션 재생
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // 위치 보간
            startPos.position = Vector3.Lerp(startPosition, arrivePos, t);
        }
    }



    

private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log(poolingQueue.Count);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            for (int i = 0; i < poolingQueue.Count; i++)
            {
                GameObject item = poolingQueue.Dequeue();
                Debug.Log(item.name);
            }
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log(verticalMatch.Count);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log(horizontalMatch.Count);
        }
    }
    #endregion
}