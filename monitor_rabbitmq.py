"""Log RabbitMQ publish notifications without consuming business queues.

Install: python -m pip install pika==1.3.2
Run:     python monitor_rabbitmq.py
Stop:    Ctrl+C

Defaults to localhost:5672 and Management API http://localhost:15672,
vhost /, guest/guest. Override with RABBITMQ_HOST, RABBITMQ_PORT,
RABBITMQ_MANAGEMENT_URL, RABBITMQ_VHOST, RABBITMQ_USER, RABBITMQ_PASSWORD.
The script enables Firehose, reconnects automatically, and prints JSON to stdout.
The dedicated trace queue keeps up to 10,000 copies for up to 24 hours.
"""

import base64
import json
import logging
import os
import signal
import threading
import time
from urllib.parse import quote
from urllib.request import Request, urlopen

import pika


LOG = logging.getLogger("rabbitmq-logger")
STOP = threading.Event()


def enable_tracing(management_url, vhost, user, password):
    auth = base64.b64encode(f"{user}:{password}".encode()).decode()
    request = Request(
        f"{management_url.rstrip('/')}/api/vhosts/{quote(vhost, safe='')}",
        data=b'{"tracing":true}',
        headers={"Authorization": f"Basic {auth}", "Content-Type": "application/json"},
        method="PUT",
    )
    with urlopen(request, timeout=5) as response:
        response.read()


def log_message(channel, method, properties, body):
    headers = properties.headers or {}
    try:
        payload = json.loads(body)
    except (ValueError, UnicodeDecodeError):
        payload = body.decode("utf-8", errors="replace")

    event_type = headers.get("exchange_name") or "Unknown"
    message = payload
    if isinstance(payload, dict):
        message = payload.get("message", payload)
        message_types = payload.get("messageType")
        if isinstance(message_types, list) and message_types:
            event_type = message_types[0]
        elif isinstance(message_types, str) and message_types:
            event_type = message_types
    event_name = str(event_type).rsplit(":", 1)[-1].rsplit(".", 1)[-1]
    record = {"event": event_name, "message": message}
    # Acknowledge only our trace copy after stdout has been flushed.
    print(json.dumps(record, ensure_ascii=False, indent=2, default=str), flush=True)
    channel.basic_ack(delivery_tag=method.delivery_tag)


def main():
    logging.basicConfig(level=logging.WARNING)
    LOG.setLevel(logging.INFO)
    for signum in (signal.SIGINT, signal.SIGTERM):
        signal.signal(signum, lambda *_: STOP.set())

    host = os.getenv("RABBITMQ_HOST", "localhost")
    user = os.getenv("RABBITMQ_USER", "guest")
    password = os.getenv("RABBITMQ_PASSWORD", "guest")
    vhost = os.getenv("RABBITMQ_VHOST", "/")
    management_url = os.getenv("RABBITMQ_MANAGEMENT_URL", "http://localhost:15672")
    queue = os.getenv("RABBITMQ_LOG_QUEUE", "swiftride.message-log")

    while not STOP.is_set():
        connection = None
        try:
            connection = pika.BlockingConnection(pika.ConnectionParameters(
                host=host,
                port=int(os.getenv("RABBITMQ_PORT", "5672")),
                virtual_host=vhost,
                credentials=pika.PlainCredentials(user, password),
                heartbeat=30,
                blocked_connection_timeout=30,
                socket_timeout=5,
                stack_timeout=15,
            ))
            channel = connection.channel()
            channel.queue_declare(
                queue=queue, durable=True, auto_delete=False,
                arguments={
                    "x-queue-type": "classic",
                    "x-max-length": 10000,
                    "x-message-ttl": 86400000,
                    "x-overflow": "drop-head",
                },
            )
            channel.queue_bind(queue=queue, exchange="amq.rabbitmq.trace", routing_key="publish.#")
            channel.basic_qos(prefetch_count=100)
            channel.basic_consume(queue=queue, on_message_callback=log_message, auto_ack=False)
            enable_tracing(management_url, vhost, user, password)
            LOG.info("Listening on %s (vhost=%s, publish.#); tracing enabled", queue, vhost)
            next_check = time.monotonic() + 30
            while not STOP.is_set():
                connection.process_data_events(time_limit=1)
                if time.monotonic() >= next_check:
                    enable_tracing(management_url, vhost, user, password)
                    next_check = time.monotonic() + 30
        except (pika.exceptions.AMQPError, OSError, ValueError) as error:
            LOG.warning("Monitor disconnected (%s); retrying in 5 seconds", error)
        finally:
            if connection and connection.is_open:
                try:
                    connection.close()
                except pika.exceptions.AMQPError:
                    pass
        STOP.wait(5)
    LOG.info("Monitor stopped")


if __name__ == "__main__":
    main()
