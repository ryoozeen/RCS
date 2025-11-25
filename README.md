# <img width="100" height="100" alt="01_표지" src="https://github.com/user-attachments/assets/1fa02af1-6272-43e2-9cb4-41a610cbb958" />  C# WPF를 활용한 차량 원격 제어 시스템 ("RCS")

데모 영상은 [여기](https://youtu.be/Q2tQWKR-kNc?si=T69-Y4o5q4e32O6-)에서 확인할 수 있습니다.

---

## 📋 1팀 GitHub 규칙

- main에 작업 금지, main은 병합용입니다.
- 각 파트별 브랜치 생성(혹은 팀원별) 후 작업해주세요.
- commit 메시지 작성 방법에 따라 commit 후 push 하세요.
- pull requests 할 때에는 팀원들에게 알리고 진행해주세요.
---

## 📝 Commit 메시지 작성 방법
- {commit 날짜}, {코드작성자}, {Commit 메시지}
---

## 🕰️ 기간
- 2025.11.14.(금) ~ 2025.11.21(금) [8일]
---

## 🌟 주제
- 사용자가 원격으로 차량 상태를 확인하고 주요 기능을 제어할 수 있는 시스템 구축
---

## ⚙️ 개발 환경
- **OS:** Windows 11
- **Language:** C#, Python
- **Framework:** WPF (.NET 8.0)
- **DB:** MySQL 8.0.43
- **Network:** TCP/IP (JSON 기반), DobotEDU (DOBOTLINK)
- **IDE:** Visual Studio 2022, DOBOTLAB, GitHub Desktop

---

## 🌐 시스템 구조
<br><br><img width="642" height="384" alt="시스템구조" src="https://github.com/user-attachments/assets/79ee7523-da02-41c5-b9ab-bc4b125d4b64" /><br><br><br>


- **RCS 클라이언트**: 사용자 인터페이스 제공, 차량 제어 및 상태 조회
- **서버**: 클라이언트 간 메시지 중계, 데이터베이스 관리
- **DOBOT 클라이언트**: 로봇 제어 및 배터리 정보 제공
---

## 📁 프로젝트 구조

```
RCS/
├── Client/          # WPF 클라이언트 (MVVM 구조)
├── Server/          # WPF 서버
└── Dobot/           # Python DOBOT 클라이언트
```

---

## 📌 구현 기능

### **[ 서버 기능 ]**

**1. 네트워크 통신 기능**
**1-1. TCP 서버**
- 포트 7000에서 TCP 서버 실행
- 다중 클라이언트 동시 연결 지원
- 비동기 클라이언트 수신
- 클라이언트별 독립 메시지 처리
- 서버 시작/중지 제어
- 이벤트 기반 통신:
  <br>－ 메시지 수신 시 발생
  <br>－ 클라이언트 연결 시 발생
  <br>－ 클라이언트 연결 해제 시 발생
<br>

**2. 클라이언트 핸들러**
- 클라이언트별 독립 스레드 처리
- 메시지 수신/송신 처리
- 클라이언트 타입 관리
- 연결 해제 시 리소스 정리
- 에러 처리 및 예외 상황 대응
<br>

**3. 메시지 직렬화**
- JSON 기반 메시지 직렬화/역직렬화
- 길이 헤더(4바이트) + JSON 본문 구조
- 최대 메시지 크기 제한 (100KB)
- 타입별 메시지 파싱 (17가지 메시지 타입 지원)
- camelCase 네이밍 정책
<br>

**4. 데이터베이스 기능**
- MySQL 연동 (회원가입, 로그인 정보 저장)
- 사용자 정보 관리
<br>

**5. 메시지 라우팅**
- RCS ↔ DOBOT 메시지 중계
- 클라이언트 타입별 메시지 분기 처리

---

### **[ 클라이언트 기능 ]**

**1. MVVM 아키텍처**
- View, ViewModel, Model 분리
- 데이터 바인딩 기반 UI 업데이트
<br>

**2. 페이지 네비게이션**
- 로그인 → 회원가입
- 로그인 → 상태 조회 → 제어 → 충전
<br>

**3. 실시간 상태 모니터링**
- 배터리 잔량 표시 (3초마다 갱신)
- 주차/주행 상태 표시
<br>

**4. 차량 제어 기능**
- 시동, 문, 트렁크, 에어컨, 온도, 열선, 라이트 제어
- 주차/출차 제어
---

### **[ DOBOT 기능 ]**

**1. 서버 통신**
- C# 서버와 TCP/IP 연결 (포트 7000)
- JSON 기반 메시지 송수신
- 길이 헤더(4바이트) + JSON 본문 구조
- 클라이언트 식별 (CLIENT_IDENTIFY_REQ)
<br>

**2. 메시지 처리**
- **START_REQ**: 시동 제어 및 로봇 이동 시퀀스 실행
- **CONTROL_REQ**: 주차/출차 제어 및 로봇 이동 시퀀스 실행
- **STATUS_REQ**: 차량 상태 요청 처리
- **BATTERY_REQ**: 배터리 정보 요청 처리
<br>

**3. 배터리 모니터링**
- 전압 기반 배터리 레벨 읽기 (0.0 ~ 1.0)
- 전압을 배터리 퍼센트로 변환
- DobotEDU 미사용 시 시뮬레이션 값 제공
<br>

**4. 로봇 제어**
- **시동 시퀀스:** 오도미터 초기화 → 이동 → 회전 → 재초기화 → 목적지 이동
- **주차 시퀀스:** 이동 → 회전 → 오도미터 초기화 → 역방향 이동
- **출차 시퀀스:** 이동 → 회전 → 오도미터 초기화 → 전방 이동
- **명령 큐 기반 비동기 처리**
---

## 🚀 실행 방법
### **[ 설정 필요 사항 ]**

**1. 서버 - DB 연결 정보 설정**
   - 파일: `Server/WPFSEVER/SERVER/ViewModels/MainWindowViewModel.cs`
   - 수정: `connectionString` 변수의 DB 정보 변경
   ```csharp
   string connectionString = "Server=localhost;Port=3306;Database=rcs;User=root;Password=1234;";
   ```
   - 설정 항목: DB IP, 포트, 데이터베이스명, 사용자명, 비밀번호
<br>

**2. 클라이언트 - 서버 IP 설정**
   - 파일: `Client/App.xaml.cs`
   - 수정: 서버 IP 주소 변경
   ```csharp
   await Network.ConnectAsync("서버 IP 주소", 7000, timeoutMs: 5000);
   ```
<br>

**3. DOBOT - 서버 IP 설정**
   - 파일: `Dobot/dobot_client.py` (18-19번 줄)
   - 수정: 서버 IP 주소 및 포트 변경
   ```python
   SERVER_HOST = "localhost"  # 서버 IP 주소로 변경
   SERVER_PORT = 7000  # 서버 포트 (변경 필요 시)
   ```
---
### **[ 실행 순서 ]**

1. **서버 실행**
 - `Server/WPFSEVER/SERVER/SERVER.sln` 열기
 - DB 연결 정보 설정 후 Visual Studio에서 실행
 - "서버 연결" 버튼 클릭 (포트 7000)
<br>

2. **클라이언트 실행**
 - `Client/DotBotCarClient.sln` 열기
 - 서버 IP 설정 후 Visual Studio에서 실행
 - 자동으로 서버에 연결 시도
<br>

3. **DOBOT 실행**
 - `Dobot/dobot_client.py` 열기
 - 서버 IP 설정 후 Python으로 실행
 - DOBOTLAB 연결 필요
---

## 프로토콜 (JSON)
### 1. 메시지 구조
- **헤더:** 항상 4바이트 (바디 크기, Little-Endian)
- **바디:** JSON 문자열 (UTF-8 인코딩)
- **바디 최대 크기:** 100KB
- **공통 필드:** 모든 메시지는 msg(메시지 타입)와 reason(선택, 에러 메시지)을 포함
<br>

### 2. 메시지 타입

**2-1. 클라이언트 식별**
- CLIENT_IDENTIFY_REQ / CLIENT_IDENTIFY_RES
<br>

**2-2. 인증**
- ENROLL_REQ / ENROLL_RES (회원가입)
- LOGIN_REQ / LOGIN_RES (로그인)
<br>

**2-3. 차량 제어**
- START_REQ / START_RES (시동)
- DOOR_REQ / DOOR_RES (문)
- TRUNK_REQ / TRUNK_RES (트렁크)
- AIR_REQ / AIR_RES (에어컨)
- CLI_REQ / CLI_RES (온도)
- HEAT_REQ / HEAT_RES (열선)
- LIGHT_REQ / LIGHT_RES (헤드 라이트)
- CONTROL_REQ / CONTROL_RES (주차/출차)
- STOP_CHARGING_REQ / STOP_CHARGING_RES (배터리 충전 종료)
<br>

**2-4. 상태 조회**
- STATUS_REQ / STATUS_RES (차량 상태)
- BATTERY_REQ / BATTERY_RES (배터리 정보)
---

### 3. 메시지 필드 상세

**3-1. 클라이언트 식별 (CLIENT_IDENTIFY)**

**REQ:**
- **msg:** "CLIENT_IDENTIFY_REQ"
- **client_name:** string (예: "RCS", "DOBOT")

**예시:**
```json
{
  "msg": "CLIENT_IDENTIFY_REQ",
  "client_name": "RCS"
}
```

**RES:**
- **msg:** "CLIENT_IDENTIFY_RES"
- **identified:** bool

**예시:**
```json
{
  "msg": "CLIENT_IDENTIFY_RES",
  "identified": true
}
```

---

**3-2. 회원가입 (ENROLL)**

**REQ:**
- **msg:** "ENROLL_REQ"
- **id:** string
- **password:** string (SHA256 해시)
- **username:** string
- **car_model:** string (예: "TESLA", "POLESTAR")

**예시:**
```json
{
  "msg": "ENROLL_REQ",
  "id": "user123",
  "password": "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8",
  "username": "홍길동",
  "car_model": "TESLA"
}
```

**RES:**
- **msg:** "ENROLL_RES"
- **registered:** bool

**예시:**
```json
{
  "msg": "ENROLL_RES",
  "registered": true
}
```
---

**3-3. 로그인 (LOGIN)**

**REQ:**
- **msg:** "LOGIN_REQ"
- **id:** string
- **password:** string (SHA256 해시)

**예시:**
```json
{
  "msg": "LOGIN_REQ",
  "id": "user123",
  "password": "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8"
}
```

**RES:**
- **msg:** "LOGIN_RES"
- **logined:** bool

**예시:**
```json
{
  "msg": "LOGIN_RES",
  "logined": true
}
```
---

**3-4. 시동 (START)**

**REQ:**
- **msg:** "START_REQ"
- **active:** bool (true: 시동 켜기, false: 시동 끄기)

**예시:**
```json
{
  "msg": "START_REQ",
  "active": true
}
```

**RES:**
- **msg:** "START_RES"
- **active_status:** bool (현재 시동 상태)

**예시:**
```json
{
  "msg": "START_RES",
  "active_status": true
}
```
---

**3-5. 문 (DOOR)**

**REQ:**
- **msg:** "DOOR_REQ"
- **door:** bool (true: 열기, false: 닫기)

**RES:**
- **msg:** "DOOR_RES"
- **door_status:** bool (현재 문 상태)
---

**3-6. 트렁크 (TRUNK)**

**REQ:**
- **msg:** "TRUNK_REQ"
- **trunk:** bool (true: 열기, false: 닫기)

**RES:**
- **msg:** "TRUNK_RES"
- **trunk_status:** bool (현재 트렁크 상태)
---

**3-7. 에어컨 (AIR)**

**REQ:**
- **msg:** "AIR_REQ"
- **air:** bool (true: 켜기, false: 끄기)

**RES:**
- **msg:** "AIR_RES"
- **air_status:** bool (현재 에어컨 상태)
---

**3-8. 온도 (CLI)**

**REQ:**
- **msg:** "CLI_REQ"
- **temp:** int (목표 온도, 예: 16~30)

**RES:**
- **msg:** "CLI_RES"
- **temp_status:** bool (설정 성공 여부)
---

**3-9. 열선 (HEAT)**

**REQ:**
- **msg:** "HEAT_REQ"
- **heat:** bool (true: 켜기, false: 끄기)

**RES:**
- **msg:** "HEAT_RES"
- **heat_status:** bool (현재 열선 상태)
---

**3-10. 헤드 라이트 (LIGHT)**

**REQ:**
- **msg:** "LIGHT_REQ"
- **light:** bool (true: 켜기, false: 끄기)

**RES:**
- **msg:** "LIGHT_RES"
- **light_status:** bool (현재 라이트 상태)
---

**3-11. 주차/출차 제어 (CONTROL)**

**REQ:**
- **msg:** "CONTROL_REQ"
- **control:** bool (true: 주차, false: 출차)

**예시:**
```json
{
  "msg": "CONTROL_REQ",
  "control": true
}
```

**RES:**
- **msg:** "CONTROL_RES"
- **control_status:** bool (주차/출차 성공 여부)
- **reason:** string (선택, 예: "주차중...", "출차중...")

**예시:**
```json
{
  "msg": "CONTROL_RES",
  "control_status": true,
  "reason": "주차중..."
}
```
---

**3-12. 배터리 충전 종료 (STOP_CHARGING)**

**REQ:**
- **msg:** "STOP_CHARGING_REQ"
- **stop:** bool (true: 충전 중지)

**RES:**
- **msg:** "STOP_CHARGING_RES"
- **stop_status:** bool (중지 성공 여부)
---

**3-13. 차량 상태 (STATUS)**

**REQ:**
- **msg:** "STATUS_REQ"
- **car_status:** bool (true: 상태 요청)

**RES:**
- **msg:** "STATUS_RES"
- **parking:** bool (주차 중 여부)
- **driving:** bool (주행 중 여부)
---

**3-14. 배터리 정보 (BATTERY)**

**REQ:**
- **msg:** "BATTERY_REQ"
- **battery:** bool (true: 배터리 정보 요청)

**예시:**
```json
{
  "msg": "BATTERY_REQ",
  "battery": true
}
```

**RES:**
- **msg:** "BATTERY_RES"
- **battery:** double (배터리 잔량, 0.0 ~ 1.0)

**예시:**
```json
{
  "msg": "BATTERY_RES",
  "battery": 0.85
}
```
---
