// ManualObject.cs 수정 제안 (가장 깔끔한 형태)
using UnityEngine;
using System.Collections;
using System.Collections.Generic; // List를 사용하지 않으므로, 사실 필요 없음

public class ManualObject : MonoBehaviour
{
    public bool isDrag;
    // public int kindIndex; // 만약 프리팹마다 고유 스프라이트가 있다면 이 변수도 필요 없음

    public ManualGameManager manager;
    public Rigidbody2D rigid;
    SpriteRenderer spriteRenderer; // 이미 프리팹에 스프라이트가 할당되어 있을 것
    PolygonCollider2D polycol;

    // public Sprite[] goodsSprites; // GameManager에서 objectPrefabs를 사용하므로 이 배열은 필요 없음

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        polycol = GetComponent<PolygonCollider2D>();
    }

    // ManualObject.cs
    private void OnEnable()
    {
        // 오브젝트가 활성화될 때 물리 시뮬레이션 비활성화
        rigid.simulated = false;

        // 이 부분이 핵심입니다. 굿즈가 활성화될 때 항상 이 위치로 이동합니다.
        // X는 중앙(0), Y는 화면 상단 (예: 8)
        transform.position = new Vector3(0, 8, 0);

        // 크기 초기화 (HideRoutine에서 스케일이 0이 될 수 있으므로 필요)
        transform.localScale = Vector3.one;

        // 활성화될 때는 드래그 상태가 아닌 것으로 시작
        isDrag = false;

        if (polycol != null) polycol.enabled = true; // 폴리곤 콜라이더 활성화
    }

    private void OnDisable()
    {
        isDrag = false;
        // 풀에 들어갈 때 초기화 (크기는 1, 위치/회전은 0)
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one; // 크기 초기화 (다음 재활용을 위해)


        rigid.simulated = false; // 물리 시뮬레이션 비활성화
        rigid.linearVelocity = Vector2.zero;
        rigid.angularVelocity = 0f;
        polycol.enabled = true; // 콜라이더도 비활성화
    }

    private void Update()
    {
        if (isDrag)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
          

            // X축 경계 계산 시 transform.localScale 대신 spriteRenderer.bounds.size.x를 사용하는 것이 더 정확
            // 하지만 현재 transform.localScale.x를 사용하고 있으므로, 일관성을 위해 유지하거나 변경.
            // 여기서는 `transform.localScale.x`를 그대로 사용합니다.
            float objectHalfWidth = transform.localScale.x / 2f; 
            float leftBorder = -4.2f + objectHalfWidth;
            float rightBorder = 4.2f - objectHalfWidth;

            if (mousePos.x < leftBorder)
            {
                mousePos.x = leftBorder;
            }
            else if (mousePos.x > rightBorder)
            {
                mousePos.x = rightBorder;
            }

            mousePos.z = 0; // 2D 평면 고정
            mousePos.y = 8; // 고정된 Y 위치

            transform.position = Vector3.Lerp(transform.position, mousePos, 0.2f);
           
        }
    }

    public void Drag()
    {
        isDrag = true;
       
    }

    public void Drop()
    {
        isDrag = false;
        rigid.simulated = true; // 드롭 시 물리 시뮬레이션 활성화 (떨어지게)
    }

    public void Hide(Vector3 targetPos)
    {
        rigid.simulated = false;
        polycol.enabled = false;

        // targetPos가 Vector3.up * 100 일 때만 특별한 사라지는 연출
        
        StartCoroutine(HideRoutine(targetPos));
     
    }

    IEnumerator HideRoutine(Vector3 targetPos)
    {
        int frameCount = 0;
        while (frameCount < 20)
        {
            frameCount++;
            // 여기서는 targetPos가 Vector3.up * 100 일 때만 스케일 축소 연출
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.2f);
            yield return null;
        }
        gameObject.SetActive(false); // 연출 후 최종 비활성화
    }

    // DeadLine 감지 (게임 오버 조건)
    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("DeadLine"))
        {
            // 이 색상 변경은 한 번만 일어나야 하므로, 반복적으로 호출되지 않도록 isOver 체크 필요
            // manager.isOver가 false일 때만 색상 변경 및 GameOver 호출
            if (!manager.isOver) 
            {
                spriteRenderer.color = new Color(0, 9f, 0.2f, 0.2f); // 색상 변경 (초록색 투명)
                manager.GameOver(); 
            }
        }
    }
}