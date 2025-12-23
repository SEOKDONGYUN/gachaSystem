# 가챠 시스템 (Gacha System)

ASP.NET Core 기반 RESTful API 가중치 가챠 시스템입니다.

## 주요 기능

- **통상 가챠**: 기본 가중치를 사용한 일반 가챠
- **픽업 가챠**: 최대 5개 아이템을 선택하여 확률을 높일 수 있는 가챠
- **확률 조회**: 통상/픽업 가챠의 확률 정보 조회
- **아이템 관리**: 가챠 풀의 모든 아이템 조회

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
│   └── GachaService.cs         # 가챠 로직 구현
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

### 1. 모든 아이템 조회
```http
GET /api/gacha/items
```

**응답 예시:**
```json
[
  {
    "id": 1,
    "name": "레전더리 소드",
    "rarity": "SSR",
    "baseWeight": 0.5,
    "description": "전설의 검",
    "imageUrl": "/images/sword_ssr.png"
  }
]
```

### 2. 특정 아이템 조회
```http
GET /api/gacha/items/{id}
```

### 3. 통상 가챠 뽑기
```http
POST /api/gacha/pull/normal
Content-Type: application/json

{
  "pullCount": 10
}
```

**응답 예시:**
```json
{
  "items": [
    {
      "id": 10,
      "name": "나무 검",
      "rarity": "N",
      "baseWeight": 37,
      "description": "기본 나무 검",
      "imageUrl": "/images/sword_n.png"
    }
  ],
  "totalPulls": 10,
  "timestamp": "2025-12-23T10:30:00Z"
}
```

### 4. 픽업 가챠 뽑기
```http
POST /api/gacha/pull/pickup
Content-Type: application/json

{
  "pullCount": 10,
  "pickupItemIds": [1, 2, 3, 4, 5],
  "pickupBoostMultiplier": 2.0
}
```

**파라미터:**
- `pullCount`: 뽑기 횟수 (1-100)
- `pickupItemIds`: 픽업할 아이템 ID 목록 (최대 5개)
- `pickupBoostMultiplier`: 픽업 가중치 배율 (1.0-10.0, 기본값 2.0)

### 5. 통상 가챠 확률 조회
```http
GET /api/gacha/rates
```

**응답 예시:**
```json
{
  "message": "통상 가챠 확률 정보",
  "totalWeight": 200,
  "rates": [
    {
      "rarity": "SSR",
      "probability": "1.00%",
      "items": [
        {
          "id": 1,
          "name": "레전더리 소드",
          "individualProbability": "0.5000%"
        }
      ]
    }
  ]
}
```

### 6. 픽업 가챠 확률 조회
```http
POST /api/gacha/rates/pickup
Content-Type: application/json

{
  "pickupItemIds": [1, 2, 3],
  "pickupBoostMultiplier": 2.0
}
```

## 확률 시스템

### 기본 확률 (통상 가챠)

| 등급 | 확률 | 설명 |
|------|------|------|
| SSR  | 1%   | 매우 희귀 |
| SR   | 5%   | 희귀 |
| R    | 20%  | 일반 |
| N    | 74%  | 흔함 |

### 픽업 가챠 시스템

- 최대 5개의 아이템을 선택하여 확률 증가
- 기본 배율: 2.0배 (픽업 아이템의 가중치가 2배로 증가)
- 배율 조정 가능 (1.0 ~ 10.0)

**예시:**
- SSR 아이템 기본 가중치: 0.5
- 픽업 시 가중치: 0.5 × 2.0 = 1.0
- 확률이 약 2배로 증가

## 가중치 알고리즘

```csharp
// WeightedRandomSelection 알고리즘
1. 모든 아이템의 가중치 합계 계산
2. 0 ~ 합계 사이의 랜덤 값 생성
3. 누적 가중치를 계산하며 랜덤 값이 속하는 구간의 아이템 선택
```

## 포트폴리오 포인트

✅ **RESTful API 설계**: HTTP 메서드와 리소스 기반 URI 구조
✅ **의존성 주입**: Interface 기반 서비스 설계
✅ **가중치 알고리즘**: 확률 기반 랜덤 선택 구현
✅ **확장성**: 픽업 시스템을 통한 동적 확률 조정
✅ **문서화**: Swagger/OpenAPI 통합
✅ **유효성 검사**: 요청 파라미터 검증 및 에러 핸들링
✅ **로깅**: ILogger를 통한 로그 관리

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
