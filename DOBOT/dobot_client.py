# version: Python3
try:
    from DobotEDU import *
    DOBOT_AVAILABLE = True
except ImportError:
    DOBOT_AVAILABLE = False

# C# 서버 연결
import socket
import json
import struct
import threading
import time
import queue
import random

# 서버 연결 설정
SERVER_HOST = "localhost"
SERVER_PORT = 7000  # C# 서버 포트
CLIENT_NAME = "DOBOT"  # 클라이언트 타입

# 전역 변수
server_socket = None
server_connected = False
robot_command_queue = queue.Queue()  # 로봇 제어 명령 큐


def serialize_message(message):
    """메시지를 C# 서버 형식으로 직렬화 (snake_case)"""
    json_str = json.dumps(message, ensure_ascii=False)
    json_bytes = json_str.encode('utf-8')
    length_bytes = struct.pack('<I', len(json_bytes))
    return length_bytes + json_bytes


def deserialize_message(sock):
    """서버로부터 메시지 역직렬화"""
    try:
        # 길이 헤더 읽기 (4바이트)
        length_bytes = sock.recv(4)
        if len(length_bytes) != 4:
            return None
        
        # 길이 파싱 (little-endian)
        message_length = struct.unpack('<I', length_bytes)[0]
        
        # 메시지 크기 검증 (최대 100KB)
        if message_length <= 0 or message_length > 1024 * 100:
            return None
        
        # 메시지 바디 읽기
        message_bytes = b''
        total_bytes_read = 0
        
        while total_bytes_read < message_length:
            chunk = sock.recv(message_length - total_bytes_read)
            if len(chunk) == 0:
                return None
            message_bytes += chunk
            total_bytes_read += len(chunk)
        
        # JSON 역직렬화
        json_str = message_bytes.decode('utf-8')
        message = json.loads(json_str)
        return message
        
    except socket.timeout:
        return None
    except Exception as e:
        print(f"[수신 오류] {e}")
        return None


def send_to_server(message):
    """서버에 메시지 전송"""
    global server_socket, server_connected
    
    if not server_connected or not server_socket:
        print("[오류] 서버에 연결되지 않았습니다.")
        return False
    
    try:
        data = serialize_message(message)
        server_socket.sendall(data)
        return True
    except Exception as e:
        print(f"[전송 실패] {e}")
        server_connected = False
        return False


def receive_from_server():
    """서버로부터 메시지 수신"""
    global server_socket, server_connected
    
    if not server_connected or not server_socket:
        return None
    
    try:
        message = deserialize_message(server_socket)
        return message
    except Exception as e:
        print(f"[수신 오류] {e}")
        return None


def get_battery_level():
    """배터리 레벨 읽기 (0.0 ~ 1.0)"""
    try:
        if DOBOT_AVAILABLE and 'go' in globals():
            # 실제 전원 전압 읽기
            voltage = go.get_power_voltage()
            print(f"[배터리 전압] {voltage}V")
            
            # 전압을 배터리 퍼센트(0.0~1.0)로 변환
            # 일반적인 리튬 배터리 기준 (로봇 사양에 따라 조정 필요)
            max_voltage = 12.6  # 최대 전압 (100%)
            min_voltage = 11.4  # 최소 전압 (0%)
            
            if voltage >= max_voltage:
                battery = 1.0  # 100%
            elif voltage <= min_voltage:
                battery = 0.0  # 0%
            else:
                # 선형 변환: (현재 전압 - 최소 전압) / (최대 전압 - 최소 전압)
                battery = (voltage - min_voltage) / (max_voltage - min_voltage)
            
            return round(battery, 2)  # 소수점 2자리까지
        else:
            # DobotEDU가 없을 경우 시뮬레이션 값
            print("[경고] DobotEDU를 사용할 수 없어 시뮬레이션 값 사용")
            return round(random.uniform(0.7, 1.0), 2)
    except Exception as e:
        print(f"[배터리 읽기 오류] {e}")
        return 0.8  # 기본값


def connect_to_server():
    """C# 서버에 연결"""
    global server_socket, server_connected
    
    try:
        print(f"[서버 연결 시도] {SERVER_HOST}:{SERVER_PORT}")
        server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_socket.settimeout(10.0)
        server_socket.connect((SERVER_HOST, SERVER_PORT))
        server_socket.settimeout(None)
        server_connected = True
        print(f"[연결 성공] C# 서버에 연결되었습니다.\n")
        
        # 클라이언트 식별 메시지 전송
        send_to_server({
            "msg": "CLIENT_IDENTIFY_REQ",
            "client_name": CLIENT_NAME
        })
        print(f"[클라이언트 식별] {CLIENT_NAME} 식별 메시지 전송 완료\n")
        
        return True
        
    except ConnectionRefusedError:
        print(f"[연결 실패] 서버가 실행 중이지 않습니다.")
        print(f"   -> C# 서버를 먼저 실행하세요.")
        server_connected = False
        return False
    except Exception as e:
        print(f"[연결 실패] {e}")
        server_connected = False
        return False


def disconnect_from_server():
    """서버 연결 해제"""
    global server_socket, server_connected
    
    if server_socket:
        try:
            server_socket.close()
        except:
            pass
        finally:
            server_socket = None
            server_connected = False


def handle_message(message):
    """수신한 메시지 처리"""
    if message is None:
        return
    
    # C# 서버 프로토콜: snake_case 사용
    msg_type = message.get('msg', '')
    reason = message.get('reason', '')
    active = message.get('active', False)
    
    if msg_type == 'START_REQ':
        print(f"\n{'='*60}")
        print(f"[시동 명령 수신]")
        if reason:
            print(f"   메시지: {reason}")
        print(f"   active: {active}")
        print(f"{'='*60}\n")
        
        # 서버에게 즉시 START_RES 전송 (운전 중 표시)
        if active:
            send_to_server({
                "msg": "START_RES",
                "active_status": True
            })
            print(f"[응답 전송] START_RES 메시지 즉시 전송 완료 (운전 중 표시)\n")
        
        # 로봇 제어 명령을 큐에 추가 (메인 스레드에서 실행되도록)
        if active:
            robot_command_queue.put(('start_req_sequence', None))
    
    elif msg_type == 'CONTROL_REQ':
        control = message.get('control', False)
        print(f"\n{'='*60}")
        if control:
            print(f"[주차 명령 수신]")
        else:
            print(f"[출차 명령 수신]")
        print(f"   control: {control}")
        print(f"{'='*60}\n")
        
        # 로봇 제어 명령을 큐에 추가 (메인 스레드에서 실행되도록)
        if control:
            robot_command_queue.put(('control_req_park_sequence', None))
        else:
            robot_command_queue.put(('control_req_drive_sequence', None))
    
    elif msg_type == 'STATUS_REQ':
        print(f"\n{'='*60}")
        print(f"[배터리 값 요청 수신]")
        print(f"{'='*60}\n")
        
        # 배터리 값 읽기
        battery_level = get_battery_level()
        
        # STATUS_RES 전송 (배터리 값만 전송)
        send_to_server({
            "msg": "STATUS_RES",
            "battery": battery_level
        })
        print(f"[응답 전송] STATUS_RES 메시지 전송 완료 (배터리: {battery_level * 100:.1f}%)\n")
    
    else:
        print(f"[메시지 수신] 타입: {msg_type}")
        if reason:
            print(f"   내용: {reason}")


def receive_loop():
    """서버로부터 메시지를 지속적으로 수신"""
    global server_connected
    
    print("[메시지 수신 대기 중...]\n")
    
    while server_connected:
        try:
            message = receive_from_server()
            
            if message is None:
                print("[연결 끊김] 서버 연결이 끊어졌습니다.")
                server_connected = False
                break
            
            handle_message(message)
            
        except Exception as e:
            print(f"[수신 루프 오류] {e}")
            server_connected = False
            break
    
    print("[수신 루프 종료]")


# 메인 실행
print("=" * 60)
print("DOBOTLAB - C# 서버 연결")
print("=" * 60)
print()

# 서버 연결
if not connect_to_server():
    print("\n[서버 연결 실패] 프로그램을 종료합니다.")
else:
    try:
        # 메시지 수신 스레드 시작
        receive_thread = threading.Thread(target=receive_loop, daemon=True)
        receive_thread.start()
        
        # 메인 루프 (연결 유지 및 로봇 제어 명령 처리)
        while server_connected:
            # 큐에서 로봇 제어 명령 확인 및 실행
            try:
                command, arg = robot_command_queue.get_nowait()
                
                if command == 'start_req_sequence':
                    try:
                        if DOBOT_AVAILABLE:
                            # go 객체가 전역 네임스페이스에 있는지 확인
                            if 'go' in globals():
                                print(f"\n[로봇 제어 시작] START_REQ 시퀀스 실행")
                                
                                # 1. 오도미터 데이터 초기화
                                go.set_odometer_data(x=0, y=0, yaw=0)
                                print(f"   [1/6] 오도미터 데이터 초기화 완료")

                                # 2. 이동 명령 (라인 따라 이동)
                                go.set_move_pos(x=250, y=10, s=50)
                                print(f"   [3/6] 이동 명령 실행 (x=250, y=10, s=50)")
                                
                                # 3. 회전 명령
                                go.set_rotate(r=90, Vr=100)
                                print(f"   [4/6] 회전 명령 실행 (r=90, Vr=100)")
                                
                                # 4. 오도미터 데이터 재초기화
                                go.set_odometer_data(x=0, y=0, yaw=0)
                                print(f"   [5/6] 오도미터 데이터 재초기화 완료")
                                
                                # 5. 목적지로 이동 (라인 따라 이동)
                                go.set_move_pos(x=100, y=0, s=50)
                                print(f"   [6/6] 이동 명령 실행 (x=100, y=0, s=50)")
                                
                                print(f"[로봇 제어 성공] START_REQ 시퀀스 완료\n")
                                
                                # 시퀀스 완료 후 추가 START_RES 전송 (이미 즉시 전송했으므로 중복이지만 로그용)
                                # 실제로는 이미 START_REQ 수신 시 즉시 전송했으므로 여기서는 전송하지 않음
                                # send_to_server({
                                #     "msg": "START_RES",
                                #     "reason": "250,100 좌표로 이동 완료 → 주차 여부 확인"
                                # })
                                print(f"[시퀀스 완료] 로봇 제어 시퀀스가 완료되었습니다.\n")
                            else:
                                print("[로봇 제어 오류] go 객체가 초기화되지 않았습니다.")
                        else:
                            print("[로봇 제어 오류] DobotEDU 모듈이 로드되지 않았습니다.")
                    except Exception as e:
                        print(f"[로봇 제어 오류] {e}")
                
                elif command == 'control_req_park_sequence':
                    try:
                        if DOBOT_AVAILABLE:
                            # go 객체가 전역 네임스페이스에 있는지 확인
                            if 'go' in globals():
                                print(f"\n[로봇 제어 시작] CONTROL_REQ 주차 시퀀스 실행")
                                
                                # 주차 시작 메시지 전송
                                send_to_server({
                                    "msg": "CONTROL_RES",
                                    "reason": "주차중...",
                                    "control_status": False  # 진행 중이므로 False
                                })
                                print(f"[상태 전송] 주차중... 메시지 전송 완료\n")
                                
                                # 1. 이동 명령
                                go.set_move_pos(x=50, y=0, s=50)
                                print(f"   [1/4] 이동 명령 실행 (x=50, y=0, s=50)")
                                
                                # 2. 회전 명령
                                go.set_rotate(r=-90, Vr=100)
                                print(f"   [2/4] 회전 명령 실행 (r=-90, Vr=100)")
                                
                                # 3. 오도미터 데이터 초기화
                                go.set_odometer_data(x=0, y=0, yaw=0)
                                print(f"   [3/4] 오도미터 데이터 초기화 완료")
                                
                                # 4. 역방향 이동
                                go.set_move_pos(x=-50, y=0, s=30)
                                print(f"   [4/4] 역방향 이동 명령 실행 (x=-50, y=0, s=30)")
                                
                                print(f"[로봇 제어 성공] CONTROL_REQ 주차 시퀀스 완료\n")
                                
                                # CONTROL_RES 메시지 전송 (주차 성공)
                                send_to_server({
                                    "msg": "CONTROL_RES",
                                    "control_status": True
                                })
                                print(f"[응답 전송] CONTROL_RES 메시지 전송 완료 (주차 성공)\n")
                                
                            else:
                                print("[로봇 제어 오류] go 객체가 초기화되지 않았습니다.")
                                # 주차 실패 응답 전송
                                send_to_server({
                                    "msg": "CONTROL_RES",
                                    "control_status": False
                                })
                        else:
                            print("[로봇 제어 오류] DobotEDU 모듈이 로드되지 않았습니다.")
                            # 주차 실패 응답 전송
                            send_to_server({
                                "msg": "CONTROL_RES",
                                "control_status": False
                            })
                    except Exception as e:
                        print(f"[로봇 제어 오류] {e}")
                        # 주차 실패 응답 전송
                        send_to_server({
                            "msg": "CONTROL_RES",
                            "control_status": False
                        })
                
                elif command == 'control_req_drive_sequence':
                    try:
                        if DOBOT_AVAILABLE:
                            # go 객체가 전역 네임스페이스에 있는지 확인
                            if 'go' in globals():
                                print(f"\n[로봇 제어 시작] CONTROL_REQ 출차 시퀀스 실행")
                                
                                # 출차 시작 메시지 전송 (동시에)
                                send_to_server({
                                    "msg": "CONTROL_RES",
                                    "reason": "출차중...",
                                    "control_status": True
                                })
                                print(f"[상태 전송] 출차중... 메시지 전송 완료\n")
                                
                                # 1. 이동 명령
                                go.set_move_pos(x=0, y=0, s=30)
                                print(f"   [1/3] 이동 명령 실행 (x=0, y=0, s=30)")
                                
                                # 2. 회전 명령
                                go.set_rotate(r=90, Vr=100)
                                print(f"   [2/3] 회전 명령 실행 (r=90, Vr=100)")
                                
                                # 3. 오도미터 데이터 초기화
                                go.set_odometer_data(x=0, y=0, yaw=0)
                                print(f"   [3/3] 오도미터 데이터 초기화 완료")
                                
                                # 4. 자동 라인 순찰
                                go.set_auto_trace(trace=1)
                                
                                print(f"[로봇 제어 성공] CONTROL_REQ 출차 시퀀스 완료\n")
                            else:
                                print("[로봇 제어 오류] go 객체가 초기화되지 않았습니다.")
                        else:
                            print("[로봇 제어 오류] DobotEDU 모듈이 로드되지 않았습니다.")
                    except Exception as e:
                        print(f"[로봇 제어 오류] {e}")
                
            except queue.Empty:
                pass
            
            time.sleep(0.1)  # CPU 사용량 감소를 위해 짧은 대기
            
    except KeyboardInterrupt:
        print("\n[사용자 중단] 프로그램을 종료합니다.")
    except Exception as e:
        print(f"[오류] {e}")
    finally:
        server_connected = False
        time.sleep(0.5)
        disconnect_from_server()
        print("\n[프로그램 종료]")