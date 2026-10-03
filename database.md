# SwiftRide — Database Design

## Tổng quan Polyglot Persistence

| Service            | Database      | Engine      |
|--------------------|---------------|-------------|
| Identity Service   | identity_db   | PostgreSQL  |
| Trip Service       | tripdb        | SQL Server  |
| Matching & Pricing | matchingdb    | MongoDB     |
| Payment Service    | paymentdb     | PostgreSQL  |

---

## 1. Identity Service → PostgreSQL

```sql
-- Bảng người dùng
users
├── id              UUID          PK DEFAULT gen_random_uuid()
├── email           VARCHAR(255)  UNIQUE NOT NULL
├── phone           VARCHAR(20)   UNIQUE
├── password_hash   TEXT          NOT NULL
├── role            VARCHAR(10)   NOT NULL  -- 'rider' | 'driver' | 'admin'
├── is_active       BOOLEAN       DEFAULT true
├── created_at      TIMESTAMPTZ   DEFAULT NOW()
└── updated_at      TIMESTAMPTZ   DEFAULT NOW()


-- Thông tin driver (chỉ khi role = 'driver')
driver_profiles
├── id              UUID          PK DEFAULT gen_random_uuid()
├── user_id         UUID          UNIQUE NOT NULL FK → users.id
├── license_plate   VARCHAR(20)
├── vehicle_model   VARCHAR(100)
├── is_online       BOOLEAN       DEFAULT false
└── updated_at      TIMESTAMPTZ   DEFAULT NOW()
```

---

## 2. Trip Service → SQL Server

```sql
-- Chuyến đi (trung tâm của hệ thống)
trips
├── id               UNIQUEIDENTIFIER  PK DEFAULT NEWID()
├── rider_id         UNIQUEIDENTIFIER  NOT NULL  -- ref Identity (no FK)
├── driver_id        UNIQUEIDENTIFIER  NULL       -- null cho đến khi matched
├── status           NVARCHAR(30)      NOT NULL
│                    -- 'Requested' | 'PricingPending' | 'DriverSearching'
│                    -- | 'DriverAccepted' | 'EnRoute' | 'PickedUp'
│                    -- | 'DroppedOff' | 'PaymentPending' | 'Paid'
│                    -- | 'Cancelled' | 'Failed'
├── pickup_address   NVARCHAR(500)     NOT NULL
├── dropoff_address  NVARCHAR(500)     NOT NULL
├── pickup_lat       DECIMAL(10,7)     NOT NULL
├── pickup_lng       DECIMAL(10,7)     NOT NULL
├── dropoff_lat      DECIMAL(10,7)     NOT NULL
├── dropoff_lng      DECIMAL(10,7)     NOT NULL
├── estimated_fare   DECIMAL(10,2)     NULL  -- giá báo trước từ Matching
├── final_fare       DECIMAL(10,2)     NULL  -- giá thực tế sau chuyến
├── distance_km      DECIMAL(8,2)      NULL
├── match_attempts   INT               DEFAULT 0
├── quote_id         NVARCHAR(50)      NULL  -- ref match_sessions MongoDB
├── correlation_id   UNIQUEIDENTIFIER  NOT NULL  -- idempotency / Saga
├── created_at       DATETIME2         DEFAULT GETUTCDATE()
└── updated_at       DATETIME2         DEFAULT GETUTCDATE()



-- Outbox Pattern (đảm bảo publish event tin cậy)
outbox_messages
├── id              UNIQUEIDENTIFIER  PK DEFAULT NEWID()
├── event_type      NVARCHAR(100)     NOT NULL  -- 'TripCreated' | 'RideCompleted'...
├── payload         NVARCHAR(MAX)     NOT NULL  -- JSON
├── processed_at    DATETIME2         NULL      -- NULL = chưa publish
└── created_at      DATETIME2         DEFAULT GETUTCDATE()
```

---

## 3. Matching & Pricing Service → MongoDB

### Collection: `driver_locations`

```json
{
  "_id": "UUID",
  "driver_id": "UUID",
  "location": {
    "type": "Point",
    "coordinates": [106.6297, 10.8231]
  },
  "is_available": true,
  "updated_at": "ISODate"
}
```

> Index: `{ "location": "2dsphere" }`  
> Index: `{ "driver_id": 1, "is_available": 1 }`

---

### Collection: `pricing_configs`

Một document `is_active = true` tại một thời điểm. Admin tạo config mới → cũ tự động inactive.

```json
{
  "_id": "UUID",

  "base_fare": 10000,
  "per_km_rate": 5000,
  "per_min_rate": 500,
  "tax_rate": 0.10,

  "surge_apply_mode": "multiply",

  "surge_rules": [
    {
      "rule_id": "UUID",
      "type": "time",
      "name": "Morning Rush Hour",
      "multiplier": 1.5,
      "condition": {
        "from_hour": 7,
        "to_hour": 9
      }
    },
    {
      "rule_id": "UUID",
      "type": "time",
      "name": "Evening Rush Hour",
      "multiplier": 1.3,
      "condition": {
        "from_hour": 17,
        "to_hour": 19
      }
    },
    {
      "rule_id": "UUID",
      "type": "zone",
      "name": "District 1 Premium",
      "multiplier": 1.4,
      "condition": {
        "zone_name": "District1",
        "center_lat": 10.7769,
        "center_lng": 106.7009,
        "radius_km": 3.0
      }
    },
    {
      "rule_id": "UUID",
      "type": "weather",
      "name": "Rain Surge",
      "multiplier": 1.2,
      "condition": {
        "weather_condition": "rain"
      }
    }
  ],

  "promo_codes": [
    {
      "code": "SAVE10",
      "description": "Giảm 10%",
      "factor": 0.9
    },
    {
      "code": "NEWUSER",
      "description": "Giảm 20% cho user mới",
      "factor": 0.8
    },
    {
      "code": "SWIFTRIDE50",
      "description": "Giảm 50% dịp khai trương",
      "factor": 0.5
    }
  ],
  "created_at": "ISODate"
}
```

> **Ghi chú `promo_codes`:** Mã cố định, hardcode trong config. Rider nhập mã → tìm trong mảng → lấy `factor` → áp dụng `PromoPricing` decorator. Không quản lý lịch sử sử dụng.

---

### Collection: `match_sessions`

Lưu lịch sử mỗi lần tìm tài xế + báo giá.

```json
{
  "_id": "UUID",
  "trip_id": "UUID",
  "rider_id": "UUID",
  "pickup_location": {
    "type": "Point",
    "coordinates": [106.6297, 10.8231]
  },
  "distance_km": 8.5,
  "estimated_minutes": 17,
  "pricing_breakdown": {
    "base_fare": 10000,
    "distance_fare": 42500,
    "time_fare": 8500,
    "subtotal": 61000,
    "applied_surges": [
      { "type": "time", "name": "Morning Rush Hour", "multiplier": 1.5 }
    ],
    "after_surge": 91500,
    "promo_code": "SAVE10",
    "promo_factor": 0.9,
    "after_promo": 82350,
    "tax": 8235,
    "total_fare": 90585
  },
  "matched_driver_id": "UUID",
  "driver_attempts": [
    {
      "driver_id": "UUID",
      "response": "rejected",
      "attempted_at": "ISODate"
    }
  ],
  "status": "matched",
  "created_at": "ISODate",
  "expires_at": "ISODate"
}
```

---

## 4. Payment Service → PostgreSQL

```sql
-- Ví nội bộ
wallets
├── id              UUID          PK DEFAULT gen_random_uuid()
├── user_id         UUID          NOT NULL UNIQUE  -- ref Identity (no FK)
├── user_role       VARCHAR(10)   NOT NULL  -- 'rider' | 'driver'
├── balance         DECIMAL(18,2) NOT NULL DEFAULT 0
├── currency        CHAR(3)       NOT NULL DEFAULT 'VND'
├── created_at      TIMESTAMPTZ   DEFAULT NOW()
└── updated_at      TIMESTAMPTZ   DEFAULT NOW()

-- Giao dịch thanh toán
payments
├── id                UUID          PK DEFAULT gen_random_uuid()
├── trip_id           UUID          NOT NULL UNIQUE  -- idempotency
├── rider_id          UUID          NOT NULL
├── driver_id         UUID          NOT NULL
├── amount            DECIMAL(18,2) NOT NULL
├── currency          CHAR(3)       NOT NULL DEFAULT 'VND'
├── status            VARCHAR(20)   NOT NULL
│                     -- 'pending' | 'completed' | 'refunded' | 'failed'
├── payment_method    VARCHAR(10)   NOT NULL  -- 'wallet' | 'card'
├── idempotency_key   UUID          NOT NULL UNIQUE
├── gateway_token     TEXT          NULL  -- token mock gateway (chỉ khi card)
├── gateway_response  JSONB         NULL  -- response từ mock gateway
├── processed_at      TIMESTAMPTZ   NULL
├── created_at        TIMESTAMPTZ   DEFAULT NOW()
└── updated_at        TIMESTAMPTZ   DEFAULT NOW()

-- Sổ cái double-entry
ledger_entries
├── id              UUID          PK DEFAULT gen_random_uuid()
├── payment_id      UUID          NOT NULL FK → payments.id
├── wallet_id       UUID          NULL FK → wallets.id
│                   -- NULL khi debit từ card (tiền đến từ bên ngoài)
│                   -- NOT NULL với wallet debit và mọi credit
├── payment_method  VARCHAR(10)   NOT NULL  -- 'wallet' | 'card'
├── entry_type      VARCHAR(10)   NOT NULL  -- 'debit' | 'credit'
├── amount          DECIMAL(18,2) NOT NULL
├── description     TEXT          NULL
└── created_at      TIMESTAMPTZ   DEFAULT NOW()

-- Hoàn tiền
refunds
├── id              UUID          PK DEFAULT gen_random_uuid()
├── payment_id      UUID          NOT NULL FK → payments.id
├── amount          DECIMAL(18,2) NOT NULL
├── reason          TEXT          NOT NULL
├── status          VARCHAR(20)   NOT NULL  -- 'pending' | 'completed' | 'failed'
├── processed_at    TIMESTAMPTZ   NULL
└── created_at      TIMESTAMPTZ   DEFAULT NOW()

-- Outbox Pattern
outbox_messages
├── id              UUID          PK DEFAULT gen_random_uuid()
├── event_type      VARCHAR(100)  NOT NULL  -- 'PaymentCompleted' | 'PaymentFailed'
├── payload         JSONB         NOT NULL
├── processed_at    TIMESTAMPTZ   NULL  -- NULL = chưa publish
└── created_at      TIMESTAMPTZ   DEFAULT NOW()
```

---

## Ghi chú thiết kế quan trọng

### Không foreign key chéo giữa services
Mỗi service chỉ lưu UUID tham chiếu — không có FK thực sự sang DB khác.
Tham chiếu chéo thực hiện qua REST (sync) hoặc RabbitMQ event (async).

### Idempotency
- `payments.idempotency_key` — cùng yêu cầu không trừ tiền 2 lần
- `trips.correlation_id` — theo dõi toàn bộ Saga
- `outbox_messages.processed_at` — đảm bảo event chỉ publish 1 lần

### Ledger entry cho card payment
```
Card debit:   wallet_id = NULL,              entry_type = 'debit'
Card credit:  wallet_id = driver_wallet_id,  entry_type = 'credit'

Wallet debit: wallet_id = rider_wallet_id,   entry_type = 'debit'
Wallet credit:wallet_id = driver_wallet_id,  entry_type = 'credit'
```
