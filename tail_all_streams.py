import pika
import sys
import json
import subprocess
from datetime import datetime

def log(msg=""):
    print(msg, flush=True)

def get_all_stream_queues():
    try:
        res = subprocess.check_output(
            ["docker", "exec", "rabbitmq", "rabbitmqctl", "list_queues", "name", "type"],
            text=True, stderr=subprocess.DEVNULL
        )
        queues = []
        for line in res.strip().splitlines():
            parts = line.split()
            if len(parts) >= 2 and parts[1] == "stream":
                queues.append(parts[0])
        return queues
    except Exception as e:
        log(f"Không thể lấy danh sách stream: {e}")
        return []

def main():
    target_offset = sys.argv[1] if len(sys.argv) > 1 else "next" # Mặc định 'next' để chỉ nghe event mới
    queues = get_all_stream_queues()
    if not queues:
        log("❌ Không tìm thấy stream queue nào đang chạy.")
        return

    log("=" * 70)
    log(f"📡 REALTIME MULTI-STREAM MONITOR (Đang giám sát {len(queues)} streams)")
    log(f"📍 Chế độ offset: '{target_offset}' (chỉ nhận event mới phát sinh)")
    log("📋 Danh sách streams:")
    for q in queues:
        log(f"   • {q}")
    log("⏳ Đang lắng nghe realtime trên toàn bộ hệ thống... (Ctrl+C để thoát)")
    log("=" * 70)

    params = pika.ConnectionParameters(
        host="localhost",
        port=5672,
        credentials=pika.PlainCredentials("guest", "guest")
    )
    conn = pika.BlockingConnection(params)
    ch = conn.channel()

    stream_offset_arg = int(target_offset) if target_offset.isdigit() else target_offset

    def make_callback(q_name):
        def callback(channel, method, props, body):
            now = datetime.now().strftime("%H:%M:%S")
            headers = props.headers if props else {}
            stream_offset = headers.get("x-stream-offset", method.delivery_tag)

            log(f"\n[{now}] 🚀 [{q_name}] ➜ Offset: {stream_offset}")
            log(f"  Exchange: {method.exchange}")
            try:
                data = json.loads(body.decode("utf-8"))
                payload = data.get("message", data)
                formatted = json.dumps(payload, indent=2, ensure_ascii=False)
                for line in formatted.splitlines():
                    log(f"    {line}")
            except Exception:
                log(f"    {body.decode('utf-8', errors='ignore')}")
            log("-" * 70)
        return callback

    ch.basic_qos(prefetch_count=100)
    for q in queues:
        ch.basic_consume(
            queue=q,
            on_message_callback=make_callback(q),
            auto_ack=False,
            arguments={"x-stream-offset": stream_offset_arg}
        )

    try:
        ch.start_consuming()
    except KeyboardInterrupt:
        log("\n🛑 Đã dừng realtime monitor.")

if __name__ == "__main__":
    main()
