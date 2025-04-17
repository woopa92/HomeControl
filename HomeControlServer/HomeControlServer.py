from PyQt5 import QtWidgets, uic
from PyQt5.QtGui import QIcon
from flask import Flask, request, jsonify
from threading import Thread
import requests
import sys

# Flask 서버 설정
app = Flask(__name__)
clients = []

@app.route('/register', methods=['POST'])
def register_client():
    data = request.json
    client_url = data.get("url")
    result = data.get("result")
    if client_url:
        if client_url not in clients:
            clients.append(client_url)            
            print(f"🖥️ 클라이언트 등록됨: {client_url}")
        else:
            print(f"⚠️ 이미 등록된 클라이언트: {client_url}")
    if result:        
        window.update_audio_switch_icon(result)
    return jsonify({"status": "registered"})

@app.route('/client-complete', methods=['POST'])
def client_complete():
    data = request.json
    result = data.get("result")
    if result:        
        window.update_audio_switch_icon(result)
        print(f"✅ 클라이언트 완료 신호 수신: {result}")
    else :
        print("❌ 클라이언트 완료 신호 수신 실패")

    return jsonify({"status": "received"}), 200

def send_command_to_clients(command):
    for url in clients:
        try:
            print(f"📤 {url} → 명령 전송 중: {command}")
            response = requests.post(url, json={"command": command})
            print(f"🔄 응답: {response.status_code} / {response.text}")
        except Exception as e:
            print(f"❌ 전송 실패: {e}")

def run_flask():
    app.run(host="0.0.0.0", port=5000)

# PyQt5 UI 설정
class UI(QtWidgets.QMainWindow):
    def __init__(self):
        super(UI, self).__init__()

        
        uic.loadUi("Test.ui", self)

        # 버튼 연결
        self.AudioSwitch = self.findChild(QtWidgets.QPushButton, "AudioSwitch")
        self.AudioSwitch.clicked.connect(self.AudioSwitch_clicked)

        # 전체화면으로 사용 예정
        # self.showFullScreen()

    def AudioSwitch_clicked(self):
        send_command_to_clients("AudioChange")

    def update_audio_switch_icon(self, message):
        if message == "HeadSet":
            print("헤드셋", message)
            self.AudioSwitch.setIcon(QIcon('Icons/Headset.png'))
        elif message == "Speaker":
            print("스피커", message)
            self.AudioSwitch.setIcon(QIcon('Icons/Speaker.png'))

# 실행
if __name__ == "__main__":
    flask_thread = Thread(target=run_flask, daemon=True)
    flask_thread.start()

    app = QtWidgets.QApplication(sys.argv)
    window = UI()
    window.show()
    sys.exit(app.exec_())
