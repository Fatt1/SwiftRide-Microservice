# Payment Service - Quy tắc nghiệp vụ v1

## Phạm vi triển khai hiện tại: giữ schema cũ

Theo quyết định mới, không thêm migration PaymentProcessing. Những yêu cầu bên dưới
về giữ chỗ tiền, metadata retry/đối soát bền vững, refund key riêng và ledger RefundId
là mục tiêu cho phiên bản sau, chưa được triển khai trong schema hiện tại.
Wallet hỗ trợ thanh toán và hoàn toàn phần bằng transaction/khóa payment/ví.
Refund replay nhận diện bằng payment và reason, không lưu key riêng của refund.
Card chỉ mock charge trong bộ nhớ; mất kết quả mock khi restart và chưa hỗ trợ refund Card.
CorrelationId đầu vào dùng cho trace; event kết quả dùng payment IdempotencyKey làm
correlation ổn định vì bảng payment cũ chưa có cột CorrelationId.

Tài liệu này chốt task P01, dựa trên mục 5.3, 7 và 9 của
[RideLite-KienTrucPhanMem.md](RideLite-KienTrucPhanMem.md).
Các quyết định dưới đây là hợp đồng cho những task triển khai tiếp theo;
chúng chưa đồng nghĩa source hiện tại đã thực thi các quy tắc này.

## 1. Phạm vi và trách nhiệm

- Hỗ trợ hai phương thức: Wallet và Card. Card dùng mock gateway trong bài tập.
- Chỉ hỗ trợ VND, số tiền là số nguyên dương, lưu bằng decimal; không dùng float/double.
- Payment nhận số tiền cuối cùng từ Trip khi đã trả khách; không tự tính lại giá chuyến đi.
- Mỗi TripId có một PaymentTransaction. Retry tiếp tục giao dịch này, không tạo giao dịch mới.
- Rider và driver khác nhau; mỗi người có một ví VND, role tương ứng.
- Toàn bộ tiền chuyến được ghi có cho driver; chưa có phí nền tảng hay chia doanh thu.
- Chỉ hoàn tiền toàn phần, tối đa một lần thành công cho mỗi payment.
- Nạp tiền, rút tiền và gateway thật nằm ngoài v1. Dùng dữ liệu seed để cấp số dư demo.
- Payment không đọc database Trip. Trip chịu trách nhiệm cung cấp dữ liệu chuyến hợp lệ;
  endpoint tạo payment phải dành cho caller nội bộ được cấp quyền, không tin số tiền do rider tự gửi.

## 2. Dữ liệu đầu vào

Thanh toán cần TripId, RiderId, DriverId, Amount, Currency, PaymentMethod,
IdempotencyKey và CorrelationId. Card cần thêm GatewayToken không rỗng.
Wallet không dùng GatewayToken. Các ID và key phải khác Guid.Empty.

API POST /payments nhận IdempotencyKey qua header Idempotency-Key.
Luồng TripDropOffEvent dùng TripId làm IdempotencyKey ổn định; mọi lần gửi lại
phải giữ cùng key và cùng dữ liệu. CorrelationId dùng liên kết trace, không thay thế
idempotency key; event Id dùng nhận diện message, không nhận diện thanh toán nghiệp vụ.

## 3. Thanh toán và sổ cái

### Wallet

1. Tìm hoặc tiếp tục payment theo idempotency key và TripId.
2. Kiểm tra ví rider/driver tồn tại, đúng chủ sở hữu, role và VND.
3. Kiểm tra số dư rider đủ trả; trừ Amount từ rider, cộng Amount cho driver.
4. Ghi hai ledger entry: Debit ví rider và Credit ví driver, cùng Amount.
5. Chuyển payment sang Completed và ghi event kết quả vào Outbox.

Các thay đổi số dư, ledger, trạng thái và Outbox phải commit trong cùng transaction
PostgreSQL. Nếu lỗi, rollback toàn bộ. Cần khóa hoặc concurrency control để các
thanh toán đồng thời không ghi đè số dư hay tiêu cùng một khoản tiền hai lần.

### Card

- Kiểm tra ví driver tồn tại trước khi gọi gateway.
- Lưu payment Pending trước khi gọi mạng. Gateway nhận token, Amount, VND và cùng key.
- Mock gateway hỗ trợ approved, declined, timeout và tra cứu kết quả theo key;
  charge/refund cùng key không thực hiện tác động tiền lần thứ hai.
- Khi charge được xác nhận thành công, ghi Debit tài khoản thanh toán ngoài
  (WalletId null), Credit ví driver, cập nhật Completed và Outbox trong một transaction.
- Gateway nằm ngoài transaction PostgreSQL. Nếu charge thành công nhưng DB lỗi,
  tra cứu/tiếp tục bằng cùng key rồi ghi nhận đúng một lần, không charge bằng key mới.
- Chỉ lưu token cần thiết cho tích hợp và mã tham chiếu gateway; không lưu số thẻ/CVV,
  không log token hoặc dữ liệu gateway thô chứa thông tin nhạy cảm.

Ledger là lịch sử bất biến: không sửa/xóa bút toán đã ghi. Tổng Debit bằng tổng Credit
cho mỗi lần thanh toán hoặc hoàn tiền, cùng currency. V1 dùng tài khoản ngoài được
biểu diễn bằng WalletId null và PaymentMethod Card; chưa xây chart of accounts đầy đủ.

## 4. Trạng thái

| Đối tượng | Chuyển trạng thái | Điều kiện |
| --- | --- | --- |
| Payment | Khởi tạo -> Pending | Đầu vào hợp lệ |
| Payment | Pending -> Completed | Đã xác nhận thu tiền và commit ledger/số dư |
| Payment | Pending -> Failed | Kết quả nghiệp vụ thất bại chắc chắn, không còn tác động tiền chưa giải quyết |
| Payment | Completed -> Refunded | Hoàn toàn bộ tiền được xác nhận và ghi nhận thành công |
| Refund | Khởi tạo -> Pending | Payment Completed, chưa có refund thành công/đang xử lý |
| Refund | Pending -> Completed | Đã xác nhận hoàn tiền và commit ledger/số dư |
| Refund | Pending -> Failed | Thất bại chắc chắn, không có tác động tiền chưa giải quyết |

Pending cũng biểu diễn lỗi tạm thời hoặc kết quả gateway chưa rõ; cần lưu metadata
lần thử, lý do gần nhất và cờ cần đối soát. Hết retry không tự chuyển sang Failed.
Completed/Refunded không được chuyển sang Failed; Failed không tự mở lại để charge.
Nếu muốn thanh toán lại sau lỗi nghiệp vụ cuối cùng, cần thiết kế luồng riêng ở phiên bản sau.
Payment giữ Completed trong lúc refund Pending hoặc Failed.

## 5. Idempotency và tranh chấp đồng thời

- Cùng key và cùng payload nghiệp vụ: trả payment/refund hiện có và trạng thái hiện tại.
- Cùng key nhưng khác TripId, rider, driver, amount, currency, phương thức hoặc token:
  trả 409 Conflict, không thực hiện tác động tiền. So sánh token an toàn, không đưa vào log.
- Cùng TripId nhưng key khác: trả 409 Conflict, không tạo payment thứ hai.
- Giữ unique constraint TripId và payment IdempotencyKey; refund bổ sung key riêng và
  ràng buộc ngăn nhiều refund hoạt động/thành công trên một payment.
- Khi hai request tranh chấp unique constraint, rollback rồi đọc bản ghi thắng cuộc
  bằng context/transaction phù hợp và áp dụng các quy tắc trên.
- Message lặp được xử lý bằng cả message deduplication và idempotency nghiệp vụ.

## 6. Retry, timeout và kết quả không rõ

- Lỗi nghiệp vụ cuối cùng: thiếu tiền, ví không hợp lệ, token bị từ chối.
  Không tự retry; ghi Failed nếu đã có payment hợp lệ, phát PaymentFailedEvent khi commit.
- Đầu vào sai hoặc conflict: từ chối request; không tạo payment lỗi và không phát event thất bại.
- Lỗi tạm thời: DB unavailable, lỗi mạng, gateway tạm ngừng. Retry tối đa ba lần
  sau lần đầu, chờ cơ sở 1/2/4 giây cộng jitter 0-500 ms, giữ nguyên key.
- Mock gateway: timeout mỗi lời gọi là 5 giây, cấu hình được. Sau timeout phải
  tra cứu kết quả trước khi quyết định charge/refund tiếp.
- Hết số lần thử: giữ Pending, đánh dấu cần đối soát và đưa message không xử lý được
  vào error queue. Chỉ kết quả được xác nhận mới phát event thành công/thất bại cuối cùng.
- Dùng một ngân sách retry cho mỗi thao tác, không nhân số lần thử bằng nhiều tầng retry.

## 7. Hoàn tiền và bồi thường

POST /payments/{id}/refund nhận Reason và Idempotency-Key; Amount do server lấy từ
payment. Chỉ payment Completed được hoàn, không nhận số tiền tùy ý từ client.

- Wallet: trừ ví driver, cộng lại ví rider và ghi bút toán đảo trong cùng transaction.
- Card: trừ ví driver và hoàn về giao dịch gateway gốc. Gateway dùng refund key ổn định.
  Cần cơ chế giữ chỗ tiền driver bền vững trước lời gọi mạng; số dư khả dụng không được
  tiêu phần tiền giữ chỗ. Thành công thì ghi Debit ví driver/Credit tài khoản ngoài,
  hoàn tất refund và payment trong một transaction. Kết quả chưa rõ thì giữ chỗ;
  thất bại chắc chắn thì giải phóng giữ chỗ. Không giữ DB transaction mở khi gọi mạng.
- Nếu driver không đủ tiền khả dụng, từ chối refund trước khi gọi gateway, trả conflict;
  chuyển yêu cầu bồi thường sang xử lý thủ công. V1 không cho ví âm hay tự ứng tiền nền tảng.
- Refund dùng bút toán đảo mới; không thay đổi ledger gốc.
- Bồi thường Saga gọi cùng use case refund với key ổn định và lý do bồi thường.
  Payment cung cấp thao tác này; Trip/Saga quyết định khi nào yêu cầu bồi thường.

## 8. API và tích hợp

- POST /payments: 201 khi tạo và hoàn tất ngay; 202 khi đang xử lý/Pending;
  replay kết quả cuối trả 200 với cùng ID. Kết quả nghiệp vụ thất bại trả 422
  kèm payment ID nếu đã tạo payment; đầu vào sai 400, conflict 409.
- GET /payments/{id}: 200 kèm trạng thái, 404 nếu không tồn tại.
- POST /payments/{id}/refund: cùng quy ước 201/202/200 cho refund;
  404 nếu payment không tồn tại, 409 nếu trạng thái/số dư không cho phép.
- Mục tiêu khi tích hợp bảo mật: rider chỉ được xem payment của mình; tạo payment
  và yêu cầu refund dành cho caller nội bộ hoặc người có quyền vận hành.
  Giai đoạn hiện tại Payment chưa xử lý JWT/phân quyền; phần khác phụ trách tích hợp này.
- TripDropOffEvent consumer gọi chung use case với API, không sao chép logic thu tiền.
- Completed phát PaymentCompletedEvent; Failed phát PaymentFailedEvent qua Outbox.
  Trip chịu trách nhiệm cập nhật trạng thái chuyến khi nhận kết quả.
- Kết quả refund cần contract riêng cho bên yêu cầu bồi thường; không phát lại
  PaymentCompletedEvent để biểu diễn refund.

## 9. Tiêu chí nghiệm thu cho triển khai

1. Wallet thành công: số dư đúng, hai ledger cân bằng, một payment và một kết quả nghiệp vụ.
2. Thiếu tiền: không thay đổi ví/ledger; kết quả thất bại rõ ràng.
3. Request/message lặp hoặc đồng thời: không thu tiền hay ghi có driver lần hai.
4. Cùng key khác payload hoặc cùng TripId khác key: conflict.
5. Gateway đã charge nhưng DB lỗi: phục hồi ghi nhận đúng một lần bằng key cũ.
6. Timeout/hết retry: Pending cần đối soát, không báo thất bại chắc chắn hoặc charge key mới.
7. Refund thành công và lặp: trả tiền đúng một lần, ledger đảo cân bằng.
8. Refund Card chưa rõ kết quả: tiền driver vẫn giữ chỗ, không bị tiêu lại.
9. Refund thất bại: payment vẫn Completed; không mất tiền do cập nhật dở dang.
10. Tắt RabbitMQ sau khi commit: event còn trong Outbox và được gửi khi broker phục hồi.

## 10. Các thay đổi source cần làm ở task tiếp theo

- Bổ sung guard chuyển trạng thái, validation VND/số tiền/role trong Domain.
- Bổ sung concurrency control, metadata retry/đối soát và idempotency cho Refund.
- Bổ sung cơ chế giữ chỗ tiền cho Card refund và hỗ trợ Credit Card với WalletId null
  trong LedgerEntry (hiện entity yêu cầu mọi Credit có ví).
- Xây repository/use case, factory Wallet/Card, mock gateway, API, consumer và test.
- Propagate CorrelationId từ Trip; không dùng giá trị mặc định ngẫu nhiên mỗi lần gửi lại.

P01 hoàn thành khi các quyết định trong tài liệu này trở thành cơ sở cho P02-P12.
