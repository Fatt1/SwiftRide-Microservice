import pika
import sys
import json

def peek_stream(queue_name="payment-user-created", offset="first", max_count=5):
    try:
        params = pika.ConnectionParameters(
            host="localhost",
            port=5672,
            credentials=pika.PlainCredentials("guest", "guest")
        )
        conn = pika.BlockingConnection(params)
        ch = conn.channel()

        # Handle numeric offset vs keyword
        stream_offset_arg = int(offset) if offset.isdigit() else offset

        count = [0]

        def on_msg(channel, method, props, body):
            count[0] += 1
            headers = props.headers if props else {}
            stream_offset = headers.get("x-stream-offset", method.delivery_tag)
            
            print(f"\n" + "="*50)
            print(f"📦 Message #{count[0]} | Offset: {stream_offset}")
            print(f"Exchange: {method.exchange} | RoutingKey: {method.routing_key}")
            print("-" * 50)
            try:
                data = json.loads(body.decode("utf-8"))
                # Print MassTransit payload nicely
                if "message" in data:
                    print("MassTransit Message Body:")
                    print(json.dumps(data["message"], indent=2, ensure_ascii=False))
                    print("\nFull Envelope Metadata:")
                    envelope_meta = {k: v for k, v in data.items() if k != "message"}
                    print(json.dumps(envelope_meta, indent=2, ensure_ascii=False))
                else:
                    print(json.dumps(data, indent=2, ensure_ascii=False))
            except Exception:
                print(body.decode("utf-8", errors="ignore"))

            if count[0] >= max_count:
                channel.stop_consuming()

        ch.basic_qos(prefetch_count=max_count)
        ch.basic_consume(
            queue=queue_name,
            on_message_callback=on_msg,
            auto_ack=False,
            arguments={"x-stream-offset": stream_offset_arg}
        )

        print(f"🔍 Đang đọc stream '{queue_name}' bắt đầu từ offset='{stream_offset_arg}' (tối đa {max_count} messages)...")
        # Timeout after 3 seconds if no more messages
        conn.call_later(3, ch.stop_consuming)
        ch.start_consuming()
        conn.close()
        print(f"\n✅ Đã đọc xong {count[0]} message(s).")
    except Exception as e:
        print(f"❌ Lỗi: {e}")

if __name__ == "__main__":
    q = sys.argv[1] if len(sys.argv) > 1 else "payment-user-created"
    off = sys.argv[2] if len(sys.argv) > 2 else "first"
    cnt = int(sys.argv[3]) if len(sys.argv) > 3 else 5
    peek_stream(q, off, cnt)
