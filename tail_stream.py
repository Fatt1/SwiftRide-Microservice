import pika
import sys
import json
from datetime import datetime

def log(msg=""):
    print(msg, flush=True)

def tail_stream(queue_name="payment-user-created", start_from="first"):
    log("=" * 60)
    log(f"📡 REALTIME STREAM MONITOR: '{queue_name}'")
    log(f"📍 Bắt đầu từ offset: '{start_from}'")
    log("⏳ Đang lắng nghe realtime... (Nhấn Ctrl+C để dừng)")
    log("=" * 60)

    try:
        params = pika.ConnectionParameters(
            host="localhost",
            port=5672,
            credentials=pika.PlainCredentials("guest", "guest")
        )
        conn = pika.BlockingConnection(params)
        ch = conn.channel()

        stream_offset_arg = int(start_from) if start_from.isdigit() else start_from

        def on_msg(channel, method, props, body):
            now = datetime.now().strftime("%H:%M:%S")
            headers = props.headers if props else {}
            stream_offset = headers.get("x-stream-offset", method.delivery_tag)

            log(f"\n[{now}] 🔔 NHẬN MESSAGE MỚI | Offset: {stream_offset}")
            log(f"  Exchange: {method.exchange} | RoutingKey: {method.routing_key}")
            try:
                data = json.loads(body.decode("utf-8"))
                if "message" in data:
                    log("  Payload:")
                    formatted = json.dumps(data["message"], indent=4, ensure_ascii=False)
                    for line in formatted.splitlines():
                        log(f"    {line}")
                else:
                    log(f"  Payload: {json.dumps(data, indent=4, ensure_ascii=False)}")
            except Exception:
                log(f"  Raw: {body.decode('utf-8', errors='ignore')}")
            log("-" * 60)

        ch.basic_qos(prefetch_count=100)
        ch.basic_consume(
            queue=queue_name,
            on_message_callback=on_msg,
            auto_ack=False,
            arguments={"x-stream-offset": stream_offset_arg}
        )

        ch.start_consuming()
    except KeyboardInterrupt:
        log("\n🛑 Đã dừng theo dõi stream.")
    except Exception as e:
        log(f"\n❌ Lỗi: {e}")

if __name__ == "__main__":
    q = sys.argv[1] if len(sys.argv) > 1 else "payment-user-created"
    offset = sys.argv[2] if len(sys.argv) > 2 else "first"
    tail_stream(q, offset)
