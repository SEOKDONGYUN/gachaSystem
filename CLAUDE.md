# CLAUDE.md

이 파일은 Claude Code(claude.ai/code)가 이 저장소에서 작업할 때 참고할 가이드를 제공합니다.

## 빌드 및 실행 명령어

```bash
# 프로젝트 빌드
dotnet build

# 애플리케이션 실행 (Swagger UI는 루트 경로에서 접근)
dotnet run

# 패키지 복원
dotnet restore
```

애플리케이션은 `https://localhost:5001` 또는 `http://localhost:5000`에서 실행되며, Swagger UI는 루트 경로에서 접근할 수 있습니다.

## 아키텍처 개요

### 가챠 시스템 설계

이 시스템은 **10회 확정 보장 메커니즘을 갖춘 가중치 랜덤 가챠 시스템**입니다. 서버 시작 시 미리 계산된 가중치 랜덤 풀을 로드하여 최적의 성능을 제공합니다.

**핵심 아키텍처 패턴:**
- **싱글톤 GachaTable** (`Services/GachaTable.cs`): 서버 시작 시 `Data/` 디렉토리에서 모든 JSON 가챠 풀을 로드하고 `WeightedRandom<GachaItem>` 인스턴스를 미리 초기화
- **서비스 계층** (`IGachaService`/`GachaService`): 가챠 뽑기 비즈니스 로직
- **WeightedRandom 알고리즘** (`Services/WeightedRandom.cs`): 누적 가중치 범위와 이진 탐색 기반 가중치 랜덤 선택

### 가챠 풀 시스템

시스템은 `Data/` 디렉토리에 **4개의 JSON 풀 파일**을 사용합니다:

1. **`gacha-items-normal.json`**: 일반 가챠 (1-9회 뽑기)
2. **`gacha-items-confirm.json`**: 10회차 확정 풀 (레어리티 2 이상)
3. **`gacha-items-pickup.json`**: 픽업 가챠 (1-9회 뽑기)
4. **`gacha-items-pickup-confirm.json`**: 픽업 10회차 확정 풀 (레어리티 2 이상)

**풀 이름 규칙:**
- 파일: `gacha-items-{poolName}.json` → 풀: `{poolName}`

### 10회 확정 보장 메커니즘

일반 가챠와 픽업 가챠 모두 **항상 10회 뽑기**를 수행합니다:
- **1-9회**: 기본 풀 사용 (`normal` 또는 `pickup`)
- **10회**: 확정 풀 사용 (`confirm` 또는 `pickup-confirm`)으로 레어리티 2 이상 보장

### 픽업 가챠 시스템

**요구사항:**
- **정확히 3개의 SSR 아이템** (레어리티 3)을 선택해야 함
- 부스트: 선택된 아이템당 가중치 +100
- 총 가중치 계산: 9700 (기본) + 300 (3개 × 100 부스트) = 10000

**가중치 분포:**
- **기본 풀** (`normal`/`pickup`): 총 가중치 = 10000
  - SSR (레어리티 3): 7개 × 100 = 700 (7%)
  - SR (레어리티 2): 6개 × 500 = 3000 (30%)
  - R (레어리티 1): 11개 × 572-573 = 6300 (63%)

- **확정 풀** (`confirm`/`pickup-confirm`): 총 가중치 = 9700 (R 아이템 없음)
  - SSR (레어리티 3): 7개 × 100 = 700 (7%)
  - SR (레어리티 2): 6개 × 1500 = 9000 (93%)

### 응답 형식

**GachaResult 구조:**

일반 가챠 응답:
```json
{
  "result": [
    // 10개의 모든 뽑기 결과
    { "id": 1, "name": "유메", "rarity": 3, "pickUp": false },
    { "id": 3, "name": "노노미", "rarity": 2, "pickUp": false }
  ],
  "statistics": {
    "normal": {
      "totalPulls": 50,
      "items": [
        { "id": 1, "name": "유메", "rarity": 3, "count": 5, "percentage": 10.0 },
        { "id": 7, "name": "리오", "rarity": 3, "count": 3, "percentage": 6.0 },
        { "id": 3, "name": "노노미", "rarity": 2, "count": 15, "percentage": 30.0 }
      ]
    }
  }
}
```

픽업 가챠 응답:
```json
{
  "result": [
    // 10개의 모든 뽑기 결과
    { "id": 7, "name": "리오", "rarity": 3, "pickUp": true },
    { "id": 8, "name": "유우카", "rarity": 2, "pickUp": false }
  ],
  "statistics": {
    "pickup": {
      "totalPulls": 30,
      "items": [
        { "id": 7, "name": "리오", "rarity": 3, "count": 8, "percentage": 26.67 }
      ]
    }
  }
}
```

- **`result`**: 10개의 개별 뽑기 결과, 선택된 픽업 캐릭터가 당첨되면 `pickUp: true`
- **`statistics`**: 누적 통계 (현재 뽑기 타입만 포함)
  - **`normal`**: 일반 가챠 누적 통계 (일반 가챠 응답 시에만 포함)
    - `totalPulls`: 누적 일반 가챠 횟수
    - `items`: 아이템별 누적 개수 및 확률 (레어리티 내림차순 → ID 오름차순 정렬)
  - **`pickup`**: 픽업 가챠 누적 통계 (픽업 가챠 응답 시에만 포함)
    - `totalPulls`: 누적 픽업 가챠 횟수
    - `items`: 아이템별 누적 개수 및 확률 (레어리티 내림차순 → ID 오름차순 정렬)
  - 아이템 필드:
    - `id`, `name`, `rarity`: 아이템 정보
    - `count`: 누적 획득 개수
    - `percentage`: 전체 뽑기 대비 백분율 (소수점 2자리)

### 주요 구현 세부사항

**WeightedRandom 알고리즘:**
- 이진 탐색과 누적 가중치 범위 사용 (O(log n))
- 각 아이템 저장 정보: `Base` (누적 시작), `Weight` (아이템 가중치), `Value` (아이템)
- 랜덤 선택: [0, totalWeight) 범위에서 랜덤 값 생성 후 이진 탐색으로 매칭되는 범위 찾기

**리소스 최적화:**
- 픽업 가챠는 뽑기당 **2개의 WeightedRandom 인스턴스만** 생성 (일반 풀 + 확정 풀)
- 부스트된 가중치로 풀을 미리 구성하여 뽑기당 10개 인스턴스 생성 방지
- GachaTable 싱글톤으로 서버 시작 시 한 번만 풀 초기화

**모델의 BaseWeight:**
- `GachaItem.BaseWeight`는 **JSON 역직렬화용으로만** 사용됨
- API 응답에는 포함되지 않음 (`GachaResultItem`에서 필터링)

**누적 통계 시스템:**
- GachaService 내부 변수로 일반/픽업 가챠 통계를 별도 관리
- 각 가챠 뽑기마다 자동으로 누적 통계 업데이트
- 통계 정보:
  - 누적 가챠 횟수 (일반/픽업 구분)
  - 아이템별 누적 개수 및 확률 (개수 내림차순 정렬)
  - 확률은 전체 누적 뽑기 대비 백분율로 표시 (소수점 2자리)
- 싱글톤 서비스로 운영되므로 서버 재시작 시 통계 초기화됨

## API 엔드포인트

- `GET /api/gacha/pools` - 사용 가능한 가챠 풀 목록 조회
- `POST /api/gacha/pull` - 일반 가챠 (10회 뽑기, 파라미터 없음)
- `POST /api/gacha/pickup` - 픽업 가챠 (정확히 3개의 SSR 아이템 ID 필요)
- `GET /api/gacha/statistics` - 누적 통계 조회 (일반/픽업 가챠 구분)
- `POST /api/gacha/statistics/reset` - 누적 통계 초기화

## 중요한 제약사항

1. **픽업 아이템 검증**: SSR 아이템(레어리티 3)만 선택 가능
2. **픽업 개수**: 정확히 3개의 아이템 필수 (1-3개 아님, 최대 3개 아님, 정확히 3개)
3. **뽑기 횟수**: 항상 10회 고정 (설정 불가)
4. **부스트 배수**: 내부적으로 100 고정 (API에 노출되지 않음)
