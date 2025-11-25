# <img width="100" height="100" alt="01_표지" src="https://github.com/user-attachments/assets/1fa02af1-6272-43e2-9cb4-41a610cbb958" /> RCS

데모 영상은 [여기](https://youtu.be/Q2tQWKR-kNc?si=T69-Y4o5q4e32O6-)에서 확인할 수 있습니다.

---
### 🕰️ 기간
- 2025.11.14.(금) ~ 2025.11.21(금) [8일]
---

### 🌟 주제
- 사용자가 원격으로 차량 상태를 확인하고 주요 기능을 제어할 수 있는 시스템 구축
---


LMS7기, C# WPF를 활용한 차량 원격 제어 시스템

## 1팀 GitHub 규칙

- main에 작업 금지, main은 병합용입니다.
- 각 파트별 브랜치 생성(혹은 팀원별) 후 작업해주세요.
- commit 메시지 작성 방법에 따라 commit 후 push 하세요.
- pull requests 할 때에는 팀원들에게 알리고 진행해주세요.

## Commit 메시지 작성 방법

"{commit 날짜}, {코드작성자}, {Commit 메시지}"

## 프로토콜(JSON)

### - 헤더: 항상 4바이트 (바디 크기)
### - 바디: JSON 문자열 (UTF-8)
### - 바디 최대 크기: 100KB
### - 메시지 타입 :
#### ENROLL_REQ / ENROLL_RES (회원가입)
#### LOGIN_REQ / LOGIN_RES (로그인)
#### START_REQ / START_RES (시동)
#### DOOR_REQ / DOOR_RES (문)
#### TRUNK_REQ / TRUNK_RES (트렁크)
#### AIR_REQ / AIR_RES (에어컨)
#### CLI_REQ / CLI_RES (온도)
#### HEAT_REQ / HEAT_RES (열선)
#### LIGHT_REQ / LIGHT_RES (헤드 라이트)
#### CONTROL_REQ / CONTROL_RES (주차/출차)
#### STOP_CHARGING_REQ / STOP_CHARGING_RES (배터리 충전 종료)
#### STATUS_REQ / STATUS_RES (상태)
### - 메시지 필드 :
#### 1. 회원가입(ENROLL)
##### REQ : id(string), password(string), username(string), car_model(string)
##### RES : registered(bool)
#### 2. 로그인(LOGIN)
##### REQ : id(string), password(string)
##### RES : logined(bool)
#### 3. 시동(START)
##### REQ : active(bool)
##### RES : active_status(bool)
#### 4. 문(DOOR)
##### REQ : door(bool)
##### RES : door_status(bool)
#### 5. 트렁크(TRUNK)
##### REQ : trunk(bool)
##### RES : trunk_status(bool)
#### 6. 에어컨(AIR)
##### REQ : air(bool)
##### RES : air_status(bool)
#### 7. 온도(CLI)
##### REQ : temp(bool)
##### RES : temp_status
#### 8. 열선(HEAT)
##### REQ : heat(bool)
##### RES : heat_status(bool)
#### 9. 헤드라이트(LIGHT)
##### REQ : light(bool)
##### RES : light_status(bool)
#### 10. 주차제어(CONTROL)
##### REQ : control(bool)
##### RES : control_status(bool)
#### 11. 배터리 충전 제어(STOP_CHARGING)
##### REQ : stop(bool)
##### RES : stop_status(bool)
#### 12. 상태 제어(STATUS)
##### REQ : car_status(bool)
##### RES : charging(bool), parking(bool), driving(bool), battery(double) 
