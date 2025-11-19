# RCS
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
##### ENROLL_REQ / ENROLL_RES
##### LOGIN_REQ / LOGIN_RES
##### START_REQ / START_RES
##### DOOR_REQ / DOOR_RES
##### TRUNK_REQ / TRUNK_RES
##### AIR_REQ / AIR_RES
##### CLI_REQ / CLI_RES
##### HEAT_REQ / HEAT_RES
##### LIGHT_REQ / LIGHT_RES
##### CONTROL_REQ / CONTROL_RES
##### STATUS_REQ / STATUS_RES
##### STOP_CHARGING_REQ / STOP_CHARGING_RES
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
