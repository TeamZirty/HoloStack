/*
 * 물건 오브젝트 생성 하는 스크립트
 */

using System.Collections.Generic; // List 사용을 위한 네임스페이스
using UnityEngine;

public class ObjectHandler : MonoBehaviour
{
   
    // 프리팹을 저장할 리스트
    public List<GameObject> goodsPrefabsToSpawn;
    public GameObject objectPrefab;
    public GameObject objectPreview;
    private GameObject currentPreviewObject;


    
    private void Start()
    {
        if (objectPreview != null)
        {
            currentPreviewObject = Instantiate(objectPreview);
            currentPreviewObject.SetActive(false); // 프리뷰 오브젝트를 비활성화 상태로 시작
        }
        else
        {
            Debug.LogError("Preview Prefab이 할당되지 않았습니다! Inspector에서 할당해주세요.");
        }
    }

    private void Update()
    {
        // 마우스의 스크린 좌표를 받아와서 변수에 저장
        Vector3 mouseScreenPos = Input.mousePosition;
                                                        
        mouseScreenPos.z = Camera.main.transform.position.z; // 카메라와의 거리 설정 (2D 게임에서는 일반적으로 0으로 설정)
        //스크린 좌표를 월드 좌표로 변환
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Debug.Log("마우스 월드 좌표: " + mouseWorldPos);
        // 2D 게임에서 정확히 X, Y 좌표만 사용하고 싶다면:
        Vector2 mouseWorldPos2D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Debug.Log("마우스 월드 좌표 (2D): " + mouseWorldPos2D);
        if (currentPreviewObject != null)
        {
          
            currentPreviewObject.SetActive(true); // 프리뷰 오브젝트를 활성화
            currentPreviewObject.transform.position = mouseWorldPos;
        }

        // 예시: 마우스 클릭 위치에 오브젝트 생성
        if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 버튼 클릭 시
        {
            Instantiate(objectPrefab, mouseWorldPos, Quaternion.identity);
            
            // 여기에 원하는 프리팹을 생성하는 코드를 넣을 수 있습니다.
            
             Debug.Log("마우스 클릭 월드 좌표: " + mouseWorldPos);

        }
    }
}


/* Memo
 * GameObject 타입으로 프리팹에 접근할 수 있습니다.
 * instantiate 함수로 프리팹을 생성할 수 있습니다.
 * 그리고 참조를 반환하여저장하기위한 변수가 currentPreviewObject입니다.
 * 그러니까 즉슨 instantiate 함수로 생성된 오브젝트를 currentPreviewObject 변수에 저장하지 않으면 그에 접근이 불가하다,
 * setactivefalse할수없다. 라는 뜻
 */