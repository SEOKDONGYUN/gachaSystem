# 가챠 시스템 (Gacha System)

ASP.NET Core 기반 RESTful API 가중치 가챠 시스템입니다.

## 주요 기능

- **일반 가챠**: 10회 고정 뽑기, 10회차 레어리티 2 이상 확정
- **픽업 가챠**: SSR 아이템 3개 선택 시 확률 상승 + 10회차 레어리티 2 이상 확정
- **가챠 풀 조회**: 사용 가능한 가챠 풀 목록 확인
- **서버 시작 시 데이터 로드**: JSON 파일 기반 가챠 풀 초기화

## 프로젝트 구조

```
GachaSystem/
├── Controllers/
│   └── GachaController.cs      # REST API 엔드포인트
├── Models/
│   ├── GachaItem.cs            # 가챠 아이템 모델
│   ├── GachaResult.cs          # 가챠 결과 모델
│   └── GachaRequest.cs         # 가챠 요청 모델
├── Services/
│   ├── IGachaService.cs        # 가챠 서비스 인터페이스
│   ├── GachaService.cs         # 가챠 로직 구현
│   ├── GachaTable.cs           # 가챠 데이터 싱글톤 관리
│   └── WeightedRandom.cs       # 가중치 랜덤 알고리즘
├── Data/
│   ├── gacha-items-normal.json          # 일반 가챠 풀
│   ├── gacha-items-confirm.json         # 일반 가챠 확정 풀
│   ├── gacha-items-pickup.json          # 픽업 가챠 풀
│   └── gacha-items-pickup-confirm.json  # 픽업 가챠 확정 풀
├── Program.cs                  # 애플리케이션 진입점
└── appsettings.json           # 설정 파일
```

## 실행 방법

### 1. 프로젝트 복원 및 실행

```bash
dotnet restore
dotnet run
```

### 2. Swagger UI 접속

브라우저에서 `https://localhost:5001` 또는 `http://localhost:5000` 접속

## API 엔드포인트

### 1. 일반 가챠 뽑기

```http
POST /api/gacha/pull
```

**특징:**
- 10회 고정 뽑기
- 1-9회: normal 풀 사용
- 10회: confirm 풀 사용 (레어리티 2 이상 확정)

**응답 예시:**
```json
{
  "result": [
    // 10개의 모든 뽑기 결과
    { 
      "id": 1,
      "name": "유메", 
      "rarity": 3, 
      "pickUp": false 
    },
    { 
      "id": 3, 
      "name": "노노미", 
      "rarity": 2, 
      "pickUp": false 
    }
  ],
  "statistics": {
    "normal": {
      "totalPulls": 50,
      "items": [
        { 
          "id": 1, 
          "name": "유메", 
          "rarity": 3, 
          "count": 5, 
          "percentage": 10 
        },
        { 
          "id": 3, 
          "name": "노노미", 
          "rarity": 2, 
          "count": 15, 
          "percentage": 30
        }
      ]
    }
  }
}
```

### 2. 픽업 가챠 뽑기

```http
POST /api/gacha/pickup
Content-Type: application/json

{
  "pickupItemIds": [7, 15, 16]
}
```

**파라미터:**
- `pickupItemIds`: 픽업할 SSR 아이템 ID 목록 (정확히 3개 필수)

**특징:**
- SSR(레어리티 3) 아이템만 픽업 가능
- 선택된 아이템의 가중치 +100 부스트
- 10회 고정 뽑기
- 1-9회: pickup 풀 사용
- 10회: pickup-confirm 풀 사용 (레어리티 2 이상 확정)

**응답 예시:**
```json
{
  "result": [
    // 10개의 모든 뽑기 결과
    { 
      "id": 7, 
      "name": "리오", 
      "rarity": 3, 
      "pickUp": true
    },
    { 
      "id": 8, 
      "name": "유우카", 
      "rarity": 2, 
      "pickUp": false 
    }
  ],
  "statistics": {
    "pickup": {
      "totalPulls": 30,
      "items": [
        { 
          "id": 7, 
          "name": "리오", 
          "rarity": 3, 
          "count": 8, 
          "percentage": 26.67
        }
      ]
    }
  }
}
```

## 확률 시스템

### 기본 가중치 분포 (일반 가챠)

**일반 풀 (normal):**
| 등급 | 개수 | 가중치 | 확률 |
|------|------|--------|------|
| SSR (레어리티 3) | 7개 | 100 | 7% |
| SR (레어리티 2) | 6개 | 500 | 30% |
| R (레어리티 1) | 11개 | 572-573 | 60% |
| **합계** | **24개** | **9700** | **97%** |

**확정 풀 (confirm):**
| 등급 | 개수 | 가중치 | 확률 |
|------|------|--------|------|
| SSR (레어리티 3) | 7개 | 100 | 7% |
| SR (레어리티 2) | 6개 | 1500 | 90% |
| **합계** | **13개** | **9700** | **97%** |

### 픽업 가챠 시스템

**픽업 부스트:**
- 선택된 SSR 아이템 3개의 가중치에 각각 +100
- 총 부스트: +300
- 기본 가중치 9700 + 부스트 300 = 10000

**예시:**
- 리오(ID: 7) 기본 가중치: 100
- 픽업 선택 시 가중치: 100 + 100 = 200
- 확률 증가: 1% → 2%

## 가중치 알고리즘

**WeightedRandom 알고리즘 (이진 탐색 기반):**

```
1. 모든 아이템의 누적 가중치 계산
   예: [100, 600, 1100, 1600, ...] (각 아이템의 Base 값)

2. 0 ~ 총 가중치 사이의 랜덤 값 생성
   예: random(0, 10000) = 3742

3. 이진 탐색으로 랜덤 값이 속하는 구간의 아이템 선택
   예: 3742는 1600과 4472 사이 → 해당 아이템 반환

시간 복잡도: O(log n)
```

## 기술 스택

✅ **ASP.NET Core 8.0**: 웹 API 프레임워크 \
✅ **의존성 주입**: Interface 기반 서비스 설계 \
✅ **싱글톤 패턴**: GachaTable을 통한 데이터 중앙 관리 \
✅ **가중치 알고리즘**: 이진 탐색 기반 O(log n) 성능 \
✅ **JSON 데이터**: 외부 파일 기반 유연한 데이터 관리 \
✅ **Swagger/OpenAPI**: API 문서화 및 테스트 UI \
✅ **로깅**: ILogger를 통한 로그 관리 

## 아키텍처 특징

### 1. 싱글톤 GachaTable

- 서버 시작 시 `Data/` 디렉토리의 모든 JSON 파일 로드
- WeightedRandom 객체 미리 생성 및 보관
- 매 요청마다 새로 생성하지 않아 성능 최적화

### 2. 리소스 최적화

- 픽업 가챠 1회당 WeightedRandom 인스턴스 2개만 생성 (일반 풀 + 확정 풀)
- 10회 뽑기를 위해 10개 생성하지 않음 (80% 리소스 절감)

### 3. 10회 확정 시스템

- 9회는 일반 풀 사용
- 10회는 자동으로 confirm 풀로 전환하여 SR 이상 확정

## 향후 개선 사항

- [ ] 데이터베이스 연동 (Entity Framework Core)
- [ ] 사용자 인벤토리 시스템
- [ ] 가챠 히스토리 저장
- [ ] 천장 시스템 (Pity System)
- [ ] JWT 인증/인가
- [ ] 단위 테스트 추가
- [ ] Redis 캐싱

## 라이선스

MIT License
