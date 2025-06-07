/*
 *  Desc: "홀로-굿즈 스택!" 게임의 전반적인 관리자 스크립트.
 * 굿즈 및 이펙트 오브젝트 풀링, 무작위 굿즈 소환, 플레이어 입력 감지 (마우스 클릭),
 * 게임 오버 로직, 사운드 관리 등을 담당합니다.
 
using UnityEngine;
using System.Collections.Generic; // List를 사용하기 위해 필요
using System.Collections; // 코루틴을 사용하기 위해 추가

public class GameManager : MonoBehaviour
{
    // --- 굿즈 소환 및 풀링 설정 ---
    // GameManager가 굿즈 프리팹을 직접 랜덤으로 선택하는 대신,
    // 여러 종류의 굿즈 프리팹 리스트를 여기에서 관리합니다.
    public List<GameObject> goodsPrefabTemplates; // 다양한 굿즈 프리팹 목록 (Inspector에서 할당)
    public Transform goodsGroup;                  // 생성된 굿즈들을 모아둘 부모 오브젝트 (Hierarchy에서 빈 오브젝트 생성 후 할당)
    public List<Goods> goodsPool;                 // 굿즈 오브젝트 풀

    [Range(1, 30)] // 풀 사이즈 범위 (1~30)
    public int poolSize = 20; // 초기 풀링 크기
    private int goodsPoolCursor = 0; // 풀링 커서 (다음에 꺼낼 오브젝트 인덱스)

    // --- 이펙트 풀링 설정 ---
    public GameObject effectPrefab; // 이펙트 프리팹 (ParticleSystem 컴포넌트 포함)
    public Transform effectGroup;   // 이펙트들을 모아둘 부모 오브젝트
    public List<ParticleSystem> effectPool; // 이펙트 오브젝트 풀

    [Range(1, 20)]
    public int effectPoolSize = 10; // 이펙트 풀 크기
    private int effectPoolCursor = 0; // 이펙트 풀 커서

    // --- 현재 플레이어 조작 중인 굿즈 ---
    public Goods lastDongle; // 현재 플레이어가 마우스로 조작 중인 (떨어뜨리기 전) 굿즈 (이름을 currentActiveGoods로 변경해도 좋음)

    // --- 사운드 관리 ---
    public AudioSource bgmPlayer; // 배경음악 플레이어
    public AudioSource[] sfxPlayer; // 효과음 플레이어 배열 (동시 재생을 위해 여러 개)
    public AudioClip[] sfxClip;     // 효과음 클립 배열 (Inspector에서 할당)
    // 오타 방지를 위해 열거형으로 미리 사운드 타입 정의
    public enum Sfx { LevelUp, Next, Attach, Button, Over, Drop }; // Drop 사운드 타입 추가
    private int sfxCursor = 0; // SFX 플레이어 커서 (어떤 플레이어 사용할지 순환)

    // --- 게임 상태 및 데이터 ---
    public int score;       // 현재 점수
    public int maxLevel;    // (수박게임 레벨 개념. 홀로-굿즈 스택에서는 최고 높이, 또는 특정 목표 달성 등으로 변경 가능)
    public bool isOver;     // 게임 오버 상태 여부

    // --- 게임 플레이 설정 ---
    public float timeBetweenGoodsPrepare = 1.5f; // 굿즈를 떨어뜨린 후 다음 굿즈 준비까지의 시간

    private void Awake()
    {
        Application.targetFrameRate = 60; // 프레임 설정 (초당 60프레임)

        // 굿즈 풀 초기화 및 생성
        goodsPool = new List<Goods>();
        for (int index = 0; index < poolSize; index++)
        {
            MakeGoodsInPool(); // 풀에 굿즈 인스턴스 미리 생성
        }

        // 이펙트 풀 초기화 및 생성
        effectPool = new List<ParticleSystem>();
        for (int index = 0; index < effectPoolSize; index++)
        {
            MakeEffectInPool(); // 풀에 이펙트 인스턴스 미리 생성
        }
    }

    void Start()
    {
        bgmPlayer.Play(); // 배경음악 재생
        PrepareNextGoods(); // 첫 굿즈 준비 및 소환
    }

    void Update()
    {
        // 게임 오버 상태일 때는 더 이상 입력 받지 않음
        if (isOver) return;

        // 마우스 입력 감지 및 굿즈 드롭 처리
        if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 버튼 클릭 시
        {
            TouchDown(); // 드래그 시작 (Goods 스크립트에서 isDrag = true)
        }
        if (Input.GetMouseButtonUp(0)) // 마우스 왼쪽 버튼 떼는 순간
        {
            TouchUp(); // 드롭 (Goods 스크립트에서 물리 활성화)
        }
    }

    // --- 굿즈 풀링 관련 함수 ---

    // 굿즈를 풀에 미리 생성하여 추가합니다.
    Goods MakeGoodsInPool()
    {
        // Instantiate 함수의 반환 타입은 GameObject입니다.
        // 따라서, 생성된 GameObject에서 GetComponent<Goods>()를 통해 Goods 타입 컴포넌트를 가져옵니다.
        GameObject instantGoodsObj = Instantiate(GetRandomGoodsPrefabTemplate(), goodsGroup); // 랜덤 프리팹 템플릿 사용
        instantGoodsObj.name = "Goods_" + goodsPool.Count;
        Goods instantGoods = instantGoodsObj.GetComponent<Goods>();
        instantGoods.manager = this; // Manager 참조 연결
        // instantGoods.effect = GetEffect(); // 각 굿즈에 이펙트를 직접 할당할 경우

        goodsPool.Add(instantGoods);
        instantGoods.gameObject.SetActive(false); // 풀에 보관될 때는 비활성화
        return instantGoods;
    }

    // 풀에서 사용 가능한 굿즈 하나를 가져옵니다.
    Goods GetGoodsFromPool()
    {
        for (int index = 0; index < goodsPool.Count; index++)
        {
            // 순환 큐 방식으로 풀에서 오브젝트를 가져옵니다.
            goodsPoolCursor = (goodsPoolCursor + 1) % goodsPool.Count;
            if (!goodsPool[goodsPoolCursor].gameObject.activeSelf) // 비활성화된 오브젝트를 찾으면
            {
                // 가져온 굿즈의 스프라이트와 레벨을 랜덤으로 설정 (만약 하나의 프리팹을 사용한다면)
                // 만약 goodsPrefabTemplates 리스트에 다양한 프리팹이 있다면,
                // MakeGoodsInPool()에서 랜덤으로 생성되므로 여기서는 필요 없을 수 있습니다.
                // goodsPool[goodsPoolCursor].level = Random.Range(0, goodsPrefabTemplates.Count); 
                // goodsPool[goodsPoolCursor].anim.SetInteger("Level", goodsPool[goodsPoolCursor].level); // 레벨 애니메이션 설정

                // 스프라이트 변경 로직 (만약 풀에 있는 모든 굿즈가 같은 프리팹에서 왔다면)
                // GameObject randomPrefab = GetRandomGoodsPrefabTemplate();
                // goodsPool[goodsPoolCursor].GetComponent<SpriteRenderer>().sprite = randomPrefab.GetComponent<SpriteRenderer>().sprite;

                return goodsPool[goodsPoolCursor];
            }
        }
        // 풀에 사용 가능한 굿즈가 없으면 새로 생성하여 반환합니다.
        return MakeGoodsInPool();
    }

    // 무작위 굿즈 프리팹 템플릿을 가져오는 도우미 함수
    GameObject GetRandomGoodsPrefabTemplate()
    {
        if (goodsPrefabTemplates == null || goodsPrefabTemplates.Count == 0)
        {
            Debug.LogError("굿즈 프리팹 템플릿 리스트가 비어있습니다!", this);
            return null;
        }
        return goodsPrefabTemplates[Random.Range(0, goodsPrefabTemplates.Count)];
    }


    // --- 이펙트 풀링 관련 함수 ---

    // 이펙트를 풀에 미리 생성하여 추가합니다.
    ParticleSystem MakeEffectInPool()
    {
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);
        instantEffectObj.name = "Effect_" + effectPool.Count;
        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffect);
        instantEffectObj.SetActive(false); // 풀에 보관될 때는 비활성화
        return instantEffect;
    }

    // 풀에서 사용 가능한 이펙트 하나를 가져옵니다.
    public ParticleSystem GetEffect() // 외부에서 호출할 수 있도록 public
    {
        for (int index = 0; index < effectPool.Count; index++)
        {
            effectPoolCursor = (effectPoolCursor + 1) % effectPool.Count;
            if (!effectPool[effectPoolCursor].gameObject.activeSelf)
            {
                return effectPool[effectPoolCursor];
            }
        }
        // 풀에 사용 가능한 이펙트가 없으면 새로 생성하여 반환합니다.
        return MakeEffectInPool();
    }

    // --- 굿즈 소환/준비 로직 ---

    // 다음 굿즈를 준비하고 활성화합니다.
    void PrepareNextGoods()
    {
        if (isOver) return; // 게임 오버 상태면 준비하지 않음

        lastDongle = GetGoodsFromPool(); // 풀에서 굿즈 가져오기

        // 가져온 굿즈 활성화 및 플레이어 조작 시작
        lastDongle.gameObject.SetActive(true);
        lastDongle.Drag(); // isDrag = true로 설정 (이제 마우스 따라 움직임)
        SfxPlay(Sfx.Next); // 다음 굿즈 소환 사운드 재생
    }

    // --- 플레이어 입력 함수 ---
    // 마우스 왼쪽 버튼을 누르는 순간 (Goods 스크립트에서 Drag 함수는 Update에서 활성화 됨)
    public void TouchDown()
    {
        if (lastDongle == null) return;
        // lastDongle.Drag(); // Goods.OnEnable에서 이미 isDrag를 true로 설정했으므로 여기서 다시 호출할 필요 없음
    }

    // 마우스 왼쪽 버튼을 떼는 순간 (굿즈를 떨어뜨림)
    public void TouchUp()
    {
        if (lastDongle == null) return;

        lastDongle.Drop(); // 굿즈 떨어뜨리기 (물리 활성화)
        lastDongle = null; // 현재 조작 중인 굿즈 참조 해제

        // 다음 굿즈 준비 코루틴 시작 (딜레이 후)
        StartCoroutine(WaitAndPrepareNextGoodsRoutine(timeBetweenGoodsPrepare));
    }

    // 다음 굿즈를 준비하기 위한 딜레이 코루틴
    IEnumerator WaitAndPrepareNextGoodsRoutine(float delay)
    {
        yield return new WaitForSeconds(delay); // 지정된 시간만큼 대기
        PrepareNextGoods(); // 다음 굿즈 준비
    }

    // --- 게임 오버 로직 ---
    public void GameOver()
    {
        if (isOver) return; // 이미 게임 오버 상태면 중복 호출 방지

        isOver = true; // 게임 오버 상태로 전환

        StartCoroutine(GameOverRoutine()); // 게임 오버 연출 코루틴 시작
    }

    IEnumerator GameOverRoutine()
    {
        // 현재 씬 내 모든 활성화된 굿즈 찾기 (활성화된 굿즈만 제거)
        Goods[] goodsOnScene = FindObjectsByType<Goods>(FindObjectsSortMode.None);

        // 모든 굿즈의 물리 효과 비활성화 (더 이상 움직이지 않도록)
        for (int index = 0; index < goodsOnScene.Length; index++)
        {
            if (goodsOnScene[index] != null && goodsOnScene[index].rigid != null)
            {
                goodsOnScene[index].rigid.simulated = false;
            }
        }
        yield return new WaitForSeconds(0.5f); // 굿즈 정지 대기

        // 목록을 하나씩 접근해서 숨기기 (풀로 반환)
        for (int index = 0; index < goodsOnScene.Length; index++)
        {
            if (goodsOnScene[index] != null) // null 체크
            {
                // Vector3.up * 100 은 수박게임의 특정 연출 (위로 날려버림) 이므로,
                // 여기서는 단순히 비활성화 (풀로 반환)하거나, 다른 종료 연출을 적용할 수 있습니다.
                // goodsOnScene[index].Hide(Vector3.up * 100); // 수박게임의 Hide 함수 사용 시
                goodsOnScene[index].gameObject.SetActive(false); // 오브젝트 풀로 반환
                yield return new WaitForSeconds(0.1f); // 굿즈 하나씩 사라지는 시차 연출
            }
        }
        yield return new WaitForSeconds(1f); // 모든 굿즈 사라진 후 대기

        SfxPlay(Sfx.Over); // 게임 오버 사운드 재생

        // TODO: 게임 오버 UI 표시, 점수 정산, 게임 재시작 버튼 활성화 등 추가 로직 구현
    }

    // --- 사운드 재생 함수 ---
    public void SfxPlay(Sfx type)
    {
        // sfxPlayer 배열이나 sfxClip 배열이 비어있으면 경고
        if (sfxPlayer.Length == 0 || sfxClip.Length == 0)
        {
            Debug.LogWarning("SFX Player 또는 SFX Clip이 할당되지 않았습니다. Inspector를 확인해주세요.", this);
            return;
        }

        AudioClip clipToPlay = null;
        // 열거형 Sfx 타입에 따라 적절한 AudioClip을 선택
        switch (type)
        {
            case Sfx.LevelUp: // 홀로-굿즈 스택에서는 사용하지 않거나 다른 용도로 변경
                // sfxClip[0], sfxClip[1], sfxClip[2] 중 랜덤 선택
                if (sfxClip.Length >= 3) clipToPlay = sfxClip[Random.Range(0, 3)];
                break;
            case Sfx.Next: // 다음 굿즈 준비 사운드
                if (sfxClip.Length > 3) clipToPlay = sfxClip[3]; // sfxClip[3] 사용
                break;
            case Sfx.Attach: // 홀로-굿즈 스택에서는 사용하지 않거나 다른 용도로 변경
                if (sfxClip.Length > 4) clipToPlay = sfxClip[4]; // sfxClip[4] 사용
                break;
            case Sfx.Button: // 버튼 클릭 사운드
                if (sfxClip.Length > 5) clipToPlay = sfxClip[5]; // sfxClip[5] 사용
                break;
            case Sfx.Over: // 게임 오버 사운드
                if (sfxClip.Length > 6) clipToPlay = sfxClip[6]; // sfxClip[6] 사용
                break;
            case Sfx.Drop: // 굿즈 드롭 사운드 (새로 추가)
                // sfxClip[7] 등 적절한 인덱스 할당
                if (sfxClip.Length > 7) clipToPlay = sfxClip[7];
                break;
        }

        if (clipToPlay != null) // 선택된 클립이 null이 아닐 경우에만 재생
        {
            sfxPlayer[sfxCursor].clip = clipToPlay; // 현재 SFX 플레이어에 클립 할당
            sfxPlayer[sfxCursor].Play(); // 사운드 재생
            sfxCursor = (sfxCursor + 1) % sfxPlayer.Length; // 다음 SFX 플레이어로 이동 (순환)
        }
        else
        {
            Debug.LogWarning($"SFX clip for type {type} is not assigned or index is out of bounds in sfxClip array.", this);
        }
    }
}
*/