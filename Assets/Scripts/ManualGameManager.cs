/* Desc: "홀로-굿즈 스택!" 게임의 핵심 매니저 스크립트.
 * 오브젝트 풀링, 랜덤 굿즈 스폰, 마우스 입력 처리, 게임 상태 관리, 사운드 재생을 담당합니다.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualGameManager : MonoBehaviour
{
    public ManualObject lastObject; // 현재 플레이어가 조작 중인 굿즈 오브젝트
    public Transform objectGroup; // 생성된 굿즈 오브젝트들의 부모 Transform
    public List<ManualObject> objectPool; // 오브젝트 풀 리스트
    public GameObject[] objectPrefabs; // 다양한 굿즈 종류별 프리팹 배열

    [Range(1, 30)]
    public int poolSize; // 오브젝트 풀의 초기 크기
    public int poolCursor; // 오브젝트 풀 커서

    // --- 사운드 관련 변수 (주석 해제) ---
    public AudioSource bgmPlayer; // 배경 음악 플레이어
    public AudioSource[] sfxPlayer; // 효과음 플레이어 배열
    public AudioClip[] sfxClip; // 효과음 클립 배열
    public enum Sfx { LevelUp, Next, Attach, Button, Over }; // 효과음 종류 열거형
    int sfxCursor; // 효과음 플레이어 커서
    // --- 사운드 관련 변수 끝 ---

    // public int objectKind = 8; // objectPrefabs.Length로 대체되므로 이제 필요 없음
    public int score; // 게임 점수
    public bool isOver; // 게임 오버 상태 플래그


    private void Awake()
    {
        Application.targetFrameRate = 60; // 목표 프레임 레이트 설정
        objectPool = new List<ManualObject>(); // 오브젝트 풀 리스트 초기화

        // 풀 초기화: 미리 설정된 poolSize만큼 랜덤한 굿즈를 생성하여 풀에 추가
        for (int index = 0; index < poolSize; index++)
        {
            MakeObject(); // MakeObject에서 랜덤 프리팹이 선택되어 생성됨
        }
    }

    void Start()
    {
        bgmPlayer.Play(); // 배경 음악 재생 시작
        NextObject(); // 다음 굿즈 준비 (게임 시작 시 첫 굿즈 스폰)
    }



    // ManualGameManager.cs 에 다음 Update 함수를 추가하거나, 기존 Update가 있다면 내용을 아래처럼 수정하세요.

    void Update()
    {
        if (isOver) return; // 게임 오버 상태일 때는 마우스 입력 처리를 중단합니다.

        // 마우스 왼쪽 버튼을 눌렀을 때 (Input.GetMouseButtonDown(0))
        // 이 코드는 마우스 왼쪽 버튼이 '누르는 순간' 단 한 번 true가 됩니다.
        if (Input.GetMouseButtonDown(0))
        {
            // Debug.Log("마우스 버튼 눌림!"); // 디버깅을 위해 추가
            if (lastObject != null) // 현재 조작할 굿즈가 존재하는지 확인 (필수)
            {
                TouchDown(); // lastObject의 Drag() 함수를 호출하여 드래그 시작
            }
        }
        // 마우스 왼쪽 버튼을 떼었을 때 (Input.GetMouseButtonUp(0))
        // 이 코드는 마우스 왼쪽 버튼이 '떼는 순간' 단 한 번 true가 됩니다.
        else if (Input.GetMouseButtonUp(0))
        {
            // Debug.Log("마우스 버튼 떼어짐!"); // 디버깅을 위해 추가
            // lastObject가 이미 Drop() 되어 null이 된 상태일 수 있으므로 null 체크는 중요합니다.
            // 하지만 TouchUp() 내부에도 null 체크가 있으니 여기서는 필요 없을 수도 있습니다.
            // 안전을 위해 if (lastObject != null) 을 넣는 것도 좋습니다.
            TouchUp(); // lastObject의 Drop() 함수를 호출하여 드롭
        }

        // ManualObject 자체의 Update 함수에서 isDrag 상태에 따라 위치를 업데이트하므로
        // GameManager에서는 별도의 드래그 중인 로직이 필요 없습니다.
    }

    // 기존 TouchDown()과 TouchUp() 함수는 그대로 두세요. 위 Update 함수에서 이들을 호출합니다.




    // 새로운 굿즈 오브젝트를 생성하고 풀에 추가하는 함수
    ManualObject MakeObject()
    {

        // objectPrefabs 배열에서 랜덤으로 하나를 선택하여 프리팹으로 사용
        GameObject selectedPrefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];

        GameObject instantObjectObj = Instantiate(selectedPrefab, objectGroup);
        instantObjectObj.name = "Object " + objectPool.Count;
        ManualObject instantObject = instantObjectObj.GetComponent<ManualObject>();
        instantObject.manager = this; // 생성된 굿즈에게 GameManager 참조 전달

        objectPool.Add(instantObject); // 생성된 굿즈를 풀에 추가


        //### 이거 지우면 물건들 바로 나옴#################

        instantObjectObj.SetActive(false); // 처음에는 비활성화 상태로 풀에 추가 (GetObject에서 활성화)

        return instantObject;
    }

    // 오브젝트 풀에서 재활용 가능한 굿즈 오브젝트를 가져오는 함수
    ManualObject GetObject()
    {
        for (int index = 0; index < objectPool.Count; index++)
        {
            poolCursor = (poolCursor + 1) % objectPool.Count; // 풀 커서 순환
            if (!objectPool[poolCursor].gameObject.activeSelf) // 비활성화된 오브젝트 발견 시
            {
                return objectPool[poolCursor]; // 해당 오브젝트 반환
            }
        }
        return MakeObject(); // 재활용할 오브젝트가 없으면 새로 생성하여 반환
    }

    // 다음 굿즈를 준비하고 화면에 표시하는 함수
    void NextObject()
    {
        if (isOver) return;

        lastObject = GetObject(); // 풀에서 다음 굿즈 가져옴

        Debug.Log("NextObject: lastObject 활성화 직전: " + lastObject.name + ", 현재 activeSelf: " + lastObject.gameObject.activeSelf);
        lastObject.gameObject.SetActive(true); // 굿즈 활성화
        Debug.Log("NextObject: lastObject 활성화 직후: " + lastObject.name + ", 활성화 상태: " + lastObject.gameObject.activeSelf);
        SfxPlay(Sfx.Next); // 다음 굿즈 사운드 재생

        StartCoroutine("WaitNext"); // 굿즈가 놓일 때까지 기다리는 코루틴 시작
    }

    // 현재 굿즈가 놓일 때까지 기다리는 코루틴
    IEnumerator WaitNext()
    {
        while (lastObject != null) // lastObject가 Drop()으로 null이 될 때까지 대기
        {
            yield return null;
        }
        yield return new WaitForSeconds(2.5f); // 굿즈 놓인 후 2.5초 대기
        NextObject(); // 다음 굿즈 준비
    }

    // 플레이어가 화면을 터치/클릭했을 때 (다운)
    public void TouchDown()
    {
        if (isOver) return;
        lastObject.Drag(); // lastObject에게 드래그 시작 명령
    }

    // 플레이어가 화면 터치를 떼거나 마우스를 놓았을 때 (업)
    public void TouchUp()
    {
        if (lastObject == null) return;
        lastObject.Drop(); // lastObject에게 드롭 명령
        lastObject = null; // lastObject 참조 끊기 (WaitNext 코루틴 진행)
    }

    // 게임 오버 상태를 처리하는 함수
    public void GameOver()
    {
        if (isOver) return;

        isOver = true;
        StartCoroutine("GameOverRoutine");
    }

    // 게임 오버 시 모든 굿즈를 정리하는 코루틴
    IEnumerator GameOverRoutine()
    {
        foreach (ManualObject obj in objectPool) // 오브젝트 풀의 모든 활성화된 굿즈 처리
        {
            if (obj != null && obj.gameObject.activeSelf)
            {
                if (obj.rigid != null)
                {
                    obj.rigid.simulated = false; // 물리 비활성화
                }
                obj.Hide(Vector3.up * 100); // 굿즈 사라지는 연출 후 비활성화
                yield return new WaitForSeconds(0.1f);
            }
        }
        yield return new WaitForSeconds(1f);
        SfxPlay(Sfx.Over); // 게임 오버 사운드 재생

        // TODO: 게임 오버 UI 표시, 재시작 버튼 등 추가 로직
    }

    // --- 사운드 재생 함수 (주석 해제) ---
    public void SfxPlay(Sfx type)
    {
        switch (type)
        {
            case Sfx.LevelUp: // 이 SfxType은 현재 게임에서 사용되지 않을 수 있습니다.
                sfxPlayer[sfxCursor].clip = sfxClip[Random.Range(0, 3)];
                break;
            case Sfx.Next:
                sfxPlayer[sfxCursor].clip = sfxClip[3];
                break;
            case Sfx.Attach: // 굿즈 충돌 시 ManualObject.OnCollisionEnter2D 등에서 호출될 수 있음
                sfxPlayer[sfxCursor].clip = sfxClip[4];
                break;
            case Sfx.Button:
                sfxPlayer[sfxCursor].clip = sfxClip[5];
                break;
            case Sfx.Over:
                sfxPlayer[sfxCursor].clip = sfxClip[6];
                break;
        }
        sfxPlayer[sfxCursor].Play();
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length; // 사운드 플레이어 순환
    }
    // --- 사운드 재생 함수 끝 ---
}